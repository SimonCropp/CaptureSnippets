using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NuGet.Versioning;

namespace CaptureSnippets
{
    /// <summary>
    /// Extracts <see cref="ReadSnippet"/>s from a given input.
    /// </summary>
    public class FileSnippetExtractor
    {
        TranslatePackage translatePackage;
        ExtractVersion extractVersion;
        ExtractPackage extractPackage;

        static char[] invalidCharacters = {'�', '�', '�', '`'};
        const string LineEnding = "\r\n";

        /// <summary>
        /// Initialise a new instance of <see cref="FileSnippetExtractor"/>.
        /// </summary>
        /// <param name="extractVersion">How to extract a <see cref="VersionRange"/> from a given path.</param>
        /// <param name="extractPackage">How to extract a package from a given path. Return null for unknown.</param>
        /// <param name="translatePackage">How to translate a package alias to the full package name.</param>
        public FileSnippetExtractor(ExtractVersion extractVersion, ExtractPackage extractPackage, TranslatePackage translatePackage = null)
        {
            Guard.AgainstNull(extractVersion, "extractVersion");
            Guard.AgainstNull(extractPackage, "extractPackage");
            if (translatePackage == null)
            {
                this.translatePackage = alias => alias;
            }
            else
            {
                this.translatePackage = alias => InvokeTranslatePackage(translatePackage, alias);
            }
            this.extractVersion = (path, parent) => InvokeExtractVersion(extractVersion, path, parent);
            this.extractPackage = (path, parent) => InvokeExtractPackage(extractPackage, path, parent);
        }

        static Result<string> InvokeExtractPackage(ExtractPackage extractPackage, string path, string parent)
        {
            var result = extractPackage(path, parent);
            if (result.Success && result.Value.IsEmptyOrWhiteSpace())
            {
                return Result<string>.Failed($"ExtractPackage returned empty string for '{path}'.");
            }
            return result;
        }

        static Result<VersionRange> InvokeExtractVersion(ExtractVersion extractVersion, string path, VersionRange parent)
        {
            path = path.Substring(0, path.LastIndexOf('.'));
            var result = extractVersion(path, parent);
            if (result.Success && result.Value == null)
            {
                return Result<VersionRange>.Failed($"ExtractVersion supplied null version for '{path}'.");
            }
            return result;
        }

        static Result<string> InvokeTranslatePackage(TranslatePackage translatePackage, string alias)
        {
            var result = translatePackage(alias);
            if (result.Success && string.IsNullOrWhiteSpace(result.Value))
            {
                return  Result<string>.Failed($"TranslatePackage supplied an empty package for '{alias}'.");
            }
            return result;
        }


        /// <summary>
        /// Read from a <see cref="TextReader"/>.
        /// </summary>
        /// <param name="textReader">The <see cref="TextReader"/> to read from.</param>
        /// <param name="path">The current path so extract <see cref="ReadSnippet"/>s from.</param>
        /// <param name="parentVersion">The inherited <see cref="VersionRange"/>.</param>
        /// <param name="parentPackage">The inherited package name.</param>
        public async Task AppendFromReader(TextReader textReader, string path, VersionRange parentVersion, string parentPackage, Action<ReadSnippet> callback)
        {
            Guard.AgainstNull(textReader, "textReader");
            using (var reader = new IndexReader(textReader))
            {
                await GetSnippets(reader, path, parentVersion, parentPackage, callback)
                    .ConfigureAwait(false);
            }
        }

        static string GetLanguageFromPath(string path)
        {
            var extension = Path.GetExtension(path);
            return extension?.TrimStart('.') ?? string.Empty;
        }


        async Task GetSnippets(IndexReader stringReader, string path, VersionRange parentVersion, string parentPackage, Action<ReadSnippet> callback)
        {
            var language = GetLanguageFromPath(path);
            var loopState = new LoopState();
            while (true)
            {
                var line = await stringReader.ReadLine()
                    .ConfigureAwait(false);
                if (line == null)
                {
                    if (loopState.IsInSnippet)
                    {
                        callback(new ReadSnippet(
                            error: "Snippet was not closed",
                            path: path,
                            lineNumberInError: loopState.StartLine.Value + 1,
                            key: loopState.CurrentKey,
                            version: null,
                            package: null));
                    }
                    break;
                }

                var trimmedLine = line.Trim()
                    .Replace("  ", " ")
                    .ToLowerInvariant();
                if (loopState.IsInSnippet)
                {
                    if (!loopState.EndFunc(trimmedLine))
                    {
                        loopState.SnippetLines.Add(line);
                        continue;
                    }

                    var snippet = BuildSnippet(stringReader, path, loopState, language, parentVersion, parentPackage);

                    callback(snippet);
                    loopState.Reset();
                    continue;
                }
                StartEndTester.IsStart(stringReader, trimmedLine, loopState);
            }
        }


        ReadSnippet BuildSnippet(IndexReader stringReader, string path, LoopState loopState, string language, VersionRange parentVersion, string parentPackage)
        {
            VersionRange parsedVersion;
            var startRow = loopState.StartLine.Value + 1;

            string package;
            string error;
            if (!TryParseVersionAndPackage(path, loopState, parentVersion, parentPackage, out parsedVersion, out package, out error))
            {
                return new ReadSnippet(
                    error: error,
                    path: path,
                    lineNumberInError: startRow,
                    key: loopState.CurrentKey,
                    version: null,
                    package: package);
            }
            var value = ConvertLinesToValue(loopState.SnippetLines);
            if (value.IndexOfAny(invalidCharacters) > -1)
            {
                var joinedInvalidChars = $@"'{string.Join("', '", invalidCharacters)}'";
                return new ReadSnippet(
                    error: $"Snippet contains invalid characters ({joinedInvalidChars}). This was probably caused by copying code from MS Word or Outlook. Dont do that.",
                    path: path,
                    lineNumberInError: startRow,
                    key: loopState.CurrentKey,
                    version: parsedVersion,
                    package: package);
            }

            string translatedPackage = null;
            if (package != null)
            {
                var translateResult = translatePackage(package);
                if (!translateResult.Success)
                {
                    return new ReadSnippet(
                        error: $"Failed to translate package. Error: {translateResult.ErrorMessage}.",
                        path: path,
                        lineNumberInError: startRow,
                        key: loopState.CurrentKey,
                        version: parsedVersion,
                        package: package);
                }
                translatedPackage = translateResult.Value;
            }
            return new ReadSnippet(
                startLine: startRow,
                endLine: stringReader.Index,
                key: loopState.CurrentKey,
                version: parsedVersion,
                value: value,
                path: path,
                package: translatedPackage,
                language: language.ToLowerInvariant());
        }

        bool TryParseVersionAndPackage(string path, LoopState loopState, VersionRange parentVersion, string parentPackage, out VersionRange parsedVersion, out string package, out string error)
        {
            if (loopState.Suffix1 == null)
            {
                parsedVersion = extractVersion(path, parentVersion);
                package = extractPackage(path, parentPackage);
                error = null;
                return true;
            }

            if (loopState.Suffix2 == null)
            {
                // Suffix1 must be a version
                if (VersionRangeParser.TryParseVersion(loopState.Suffix1, out parsedVersion))
                {
                    package = extractPackage(path, parentPackage);
                    error = null;
                    return true;
                }

                // Suffix1 must be a package
                if (loopState.Suffix1.StartsWithLetter())
                {
                    parsedVersion = extractVersion(path, parentVersion);
                    package = loopState.Suffix1;
                    error = null;
                    return true;
                }
                package = null;
                error = $"Expected '{loopState.Suffix2}' to be either parsable as a version or a package (starts with a letter).";
                return false;
            }

            if (VersionRangeParser.TryParseVersion(loopState.Suffix1, out parsedVersion))
            {
                if (loopState.Suffix2.StartsWithLetter())
                {
                    package = loopState.Suffix2;
                    error = null;
                    return true;
                }
                error = $"Was able to parse '{loopState.Suffix1}' as a version. But a '{loopState.Suffix2}' is not a package, must starts with a letter.";
                package = null;
                return false;
            }

            if (VersionRangeParser.TryParseVersion(loopState.Suffix2, out parsedVersion))
            {
                if (loopState.Suffix1.StartsWithLetter())
                {
                    package = loopState.Suffix1;
                    error = null;
                    return true;
                }
                error = $"Was able to parse '{loopState.Suffix2}' as a version. But a '{loopState.Suffix1}' is not a package, must starts with a letter.";
                package = null;
                return false;
            }
            error = $"Was not able to parse either '{loopState.Suffix1}' or '{loopState.Suffix2}' as a version.";
            package = null;
            return false;
        }

        static string ConvertLinesToValue(List<string> snippetLines)
        {
            var snippetValue = snippetLines
                .ExcludeEmptyPaddingLines()
                .TrimIndentation();
            return string.Join(LineEnding, snippetValue);
        }

    }
}