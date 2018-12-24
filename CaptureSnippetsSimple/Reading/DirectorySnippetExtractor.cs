﻿using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CaptureSnippets
{
    public class DirectorySnippetExtractor
    {
        FileFinder fileFinder;

        internal DirectorySnippetExtractor() : this(path => true, path => true)
        {
        }

        public DirectorySnippetExtractor(DirectoryFilter directoryFilter, FileFilter fileFilter)
        {
            Guard.AgainstNull(directoryFilter, nameof(directoryFilter));
            Guard.AgainstNull(fileFilter, nameof(fileFilter));
            fileFinder = new FileFinder(directoryFilter, fileFilter);
        }

        public ReadSnippets ReadSnippets(string directory)
        {
            var snippetExtractor = new FileSnippetExtractor();
            var snippets = ReadSnippets(directory, snippetExtractor).ToList();
            return new ReadSnippets(directory, snippets);
        }

        IEnumerable<Snippet> ReadSnippets(string directory, FileSnippetExtractor snippetExtractor)
        {
            return fileFinder.FindFiles(directory)
                .SelectMany(file =>
                {
                    using (var reader = File.OpenText(file))
                    {
                        return snippetExtractor.Read(reader, file).ToList();
                    }
                });
        }
    }
}