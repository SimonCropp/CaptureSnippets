﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using CaptureSnippets;
using NuGet.Versioning;
using ObjectApproval;
using Xunit;

public class MarkdownProcessorTests : TestBase
{
    [Fact]
    public void Simple()
    {
        var availableSnippets = new List<Snippet>
        {
            SnippetBuild(
                language: "cs",
                key: "snippet1",
                package: "package1",
                version: CreateVersionRange(5)
            ),
            SnippetBuild(
                language: "cs",
                key: "snippet1",
                package: "package1",
                version: CreateVersionRange(4)
            ),
            SnippetBuild(
                language: "cs",
                key: "snippet2",
                package: "package1",
                version: CreateVersionRange(3)
            ),
            SnippetBuild(
                language: "cs",
                key: "snippet2",
                package: "package1",
                version: CreateVersionRange(4)),
        };
        var markdownContent = @"
snippet: snippet1

some text

snippet: snippet2

some other text

";
        Verify(markdownContent, availableSnippets.ToDictionary());
    }

    Snippet SnippetBuild(string language, string key, string package, VersionRange version)
    {
        return Snippet.Build(
            language: language,
            startLine: 1,
            endLine: 2,
            value: "Snippet_v" + version,
            key: key,
            path: "thePath",
            version: version,
            package: package,
            isCurrent: false,
            includes: null);
    }

    static void Verify(string markdownContent, IReadOnlyDictionary<string, IReadOnlyList<Snippet>> availableSnippets)
    {
        var markdownProcessor = new MarkdownProcessor(
            snippets: availableSnippets,
            appendSnippetGroup: SimpleSnippetMarkdownHandling.AppendGroup);
        var stringBuilder = new StringBuilder();
        using (var reader = new StringReader(markdownContent))
        using (var writer = new StringWriter(stringBuilder))
        {
            var processResult = markdownProcessor.Apply(reader, writer);
            var output = new
            {
                processResult.MissingSnippets,
                processResult.UsedSnippets,
                content = stringBuilder.ToString()
            };
            ObjectApprover.VerifyWithJson(output, s => s.Replace("\\r\\n", "\r\n"));
        }
    }

    static VersionRange CreateVersionRange(int version)
    {
        return new VersionRange(minVersion: new NuGetVersion(version, 0, 0));
    }
}