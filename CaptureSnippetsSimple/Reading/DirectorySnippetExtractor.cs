﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Versioning;

namespace CaptureSnippets
{
    public class DirectorySnippetExtractor
    {
        DirectoryFilter directoryFilter;
        FileFilter fileFilter;
        GetPackageOrderForComponent packageOrder;
        TranslatePackage translatePackage;

        public DirectorySnippetExtractor(
            DirectoryFilter directoryFilter,
            FileFilter fileFilter,
            GetPackageOrderForComponent packageOrder,
            TranslatePackage translatePackage = null)
        {
            Guard.AgainstNull(directoryFilter, nameof(directoryFilter));
            Guard.AgainstNull(fileFilter, nameof(fileFilter));
            this.directoryFilter = directoryFilter;
            this.fileFilter = fileFilter;
            this.packageOrder = packageOrder;
            if (translatePackage == null)
            {
                this.translatePackage = alias => alias;
            }
            else
            {
                this.translatePackage = translatePackage;
            }
        }

        public ReadSnippets ReadSnippets(string directory)
        {
            var snippetExtractor = FileSnippetExtractor.BuildShared();
            var packages = ReadSnippets(directory, snippetExtractor).ToList();
            return new ReadSnippets(directory, packages);
        }

        class PackageVersionCurrent
        {
            public NuGetVersion Version;
            public string Package;
            public string PackageAlias;
            public string Directory;
            public bool IsCurrent;
        }

        IEnumerable<PackageVersionCurrent> GetOrderedPackages(string component, IEnumerable<PackageVersionCurrent> package)
        {
            if (packageOrder == null)
            {
                return package.OrderBy(_ => _.Package);
            }
            List<string> result;
            try
            {
                result = packageOrder(component).ToList();
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error getting package order. Component='{component}'.";
                throw new Exception(errorMessage, exception);
            }

            return package.OrderBy(_ =>
            {
                try
                {
                    return result.IndexOf(_.Package);
                }
                catch (Exception exception)
                {
                    var errorMessage = $"Error getting package index. Component='{component}', Package='{_.Package}'.";
                    throw new Exception(errorMessage, exception);
                }
            });
        }

        IEnumerable<Package> EnumeratePackages(string directory, string component)
        {
            var packageDirectories = Directory.EnumerateDirectories(directory, "*_*")
                .Where(s => !IsShared(s) &&
                            directoryFilter(s));

            var packageVersionList = new List<PackageVersionCurrent>();
            foreach (var packageAndVersionDirectory in packageDirectories)
            {
                var name = Path.GetFileName(packageAndVersionDirectory);
                var index = name.IndexOf('_');
                if (index < 1)
                {
                    throw new Exception($"Expected the directory name '{name}' to be split by a '_'.");
                }
                var packageAlias = name.Substring(0, index);
                var package = translatePackage(packageAlias);
                var versionPart = name.Substring(index + 1, name.Length - index - 1);

                var pretext = PreTextReader.GetPretext(packageAndVersionDirectory);
                var version = VersionRangeParser.ParseVersion(versionPart, pretext);
                var item = new PackageVersionCurrent
                {
                    Version = version,
                    Package = package,
                    PackageAlias = packageAlias,
                    Directory = packageAndVersionDirectory,
                };
                packageVersionList.Add(item);
            }

            if (!packageVersionList.Any())
            {
                yield break;
            }
            packageVersionList = packageVersionList
                .OrderByDescending(x => x.Version)
                .ToList();
            packageVersionList = GetOrderedPackages(component, packageVersionList).ToList();

            SetCurrent(packageVersionList);
            foreach (var group in packageVersionList.GroupBy(
                keySelector: _ => _.Package,
                comparer: StringComparer.InvariantCultureIgnoreCase))
            {
                var versions = new List<VersionGroup>();
                NuGetVersion previous = null;
                foreach (var packageAndVersion in group)
                {
                    VersionRange versionRange;
                    var minVersion = packageAndVersion.Version;

                    if (previous == null)
                    {
                        versionRange = new VersionRange(
                            minVersion: minVersion,
                            includeMinVersion: true,
                            maxVersion: new NuGetVersion(minVersion.Major + 1, 0, 0),
                            includeMaxVersion: false
                        );
                    }
                    else
                    {
                        versionRange = new VersionRange(
                            minVersion: minVersion,
                            includeMinVersion: true,
                            maxVersion: new NuGetVersion(previous.Major, previous.Minor, previous.Patch),
                            includeMaxVersion: false
                        );
                    }
                    previous = minVersion;

                    var versionGroup = ReadVersion(
                        versionDirectory: packageAndVersion.Directory,
                        version: versionRange,
                        package: packageAndVersion.Package,
                        packageAlias: packageAndVersion.PackageAlias,
                        isCurrent: packageAndVersion.IsCurrent);
                    versions.Add(versionGroup);
                }
                yield return new Package(group.Key, versions);
            }
        }

        static bool IsShared(string directory)
        {
            var directorySuffix = Path.GetFileName(directory);
            return string.Equals(directorySuffix, "Shared", StringComparison.OrdinalIgnoreCase) ||
                   directorySuffix.EndsWith("_All",StringComparison.OrdinalIgnoreCase);
        }

        static void SetCurrent(List<PackageVersionCurrent> packageVersionList)
        {
            var firstStable = packageVersionList.FirstOrDefault(_ => !_.Version.IsPrerelease);
            if (firstStable != null)
            {
                firstStable.IsCurrent = true;
                return;
            }
            packageVersionList.First().IsCurrent = true;
        }

        IEnumerable<Component> EnumerateComponents(string directory)
        {
            return Directory.EnumerateDirectories(directory)
                .Where(s => !IsShared(s) && directoryFilter(s))
                .Select(ReadComponent);
        }

        Component ReadComponent(string componentDirectory)
        {
            var name = Path.GetFileName(componentDirectory);
            var packages = EnumeratePackages(componentDirectory, name).ToList();
            return new Component(
                identifier: name,
                packages: packages,
                directory: componentDirectory
            );
        }

        VersionGroup ReadVersion(
            string versionDirectory,
            VersionRange version,
            string package,
            string packageAlias,
            bool isCurrent)
        {
            var snippetExtractor = FileSnippetExtractor.Build(version, package, isCurrent);
            return new VersionGroup(
                version: version,
                snippets: ReadSnippets(versionDirectory, snippetExtractor)
                    .ToList(),
                directory: versionDirectory,
                isCurrent: isCurrent,
                package: package,
                packageAlias: packageAlias);
        }

        void FindFiles(string directoryPath, List<string> files)
        {
            foreach (var file in Directory.EnumerateFiles(directoryPath)
                .Where(s => fileFilter(s)))
            {
                files.Add(file);
            }
            foreach (var subDirectory in Directory.EnumerateDirectories(directoryPath)
                .Where(s => directoryFilter(s)))
            {
                FindFiles(subDirectory, files);
            }
        }

        IEnumerable<Snippet> ReadSnippets(string directory, FileSnippetExtractor snippetExtractor)
        {
            var files = new List<string>();
            FindFiles(directory, files);
            return files
                .SelectMany(file =>
                {
                    using (var reader = File.OpenText(file))
                    {
                        return snippetExtractor.AppendFromReader(reader, file).ToList();
                    }
                });
        }
    }
}