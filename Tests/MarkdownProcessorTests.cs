﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaptureSnippets;
using NuGet.Versioning;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class MarkdownProcessorTests
{

    [Test]
    public async void Simple()
    {
        var availableSnippets = new List<SnippetGroup>
        {
            new SnippetGroup(
                language:"cs",
                key: "versionedSnippet1",
                versions: new List<VersionGroup>
                {
                    CreateVersionGroup(5),
                    CreateVersionGroup(4),
                }),
            new SnippetGroup(
                language:"cs",
                key: "versionedSnippet2",
                versions: new List<VersionGroup>
                {
                    CreateVersionGroup(3),
                    CreateVersionGroup(2),
                }),
            new SnippetGroup(
                language:"cs",
                key: "nonVersionedSnippet1",
                versions: new List<VersionGroup>
                {
                    CreateVersionGroup(5),
                }),
            new SnippetGroup(
                language:"cs",
                key: "nonVersionedSnippet2",
                versions: new List<VersionGroup>
                {
                    CreateVersionGroup(5),
                }),
        };
        var markdownContent = @"
<!-- import versionedSnippet1 -->

some text

<!-- import versionedSnippet2 -->

some other text

<!-- import nonVersionedSnippet1 -->

even more text

<!-- import nonVersionedSnippet2 -->

";
        await Verify(markdownContent, availableSnippets);
    }

    static async Task Verify(string markdownContent, List<SnippetGroup> availableSnippets)
    {
        var processor = new MarkdownProcessor();
        var stringBuilder = new StringBuilder();
        using (var reader = new StringReader(markdownContent))
        using (var writer = new StringWriter(stringBuilder))
        {
            var processResult = await processor.Apply(availableSnippets, reader, writer);
            var output = new object[]
            {
                processResult.MissingSnippets, processResult.UsedSnippets, stringBuilder.ToString()
            };
            ObjectApprover.VerifyWithJson(output, s => s.Replace("\\r\\n", "\r\n"));
        }
    }


    static VersionGroup CreateVersionGroup(int version)
    {
        return new VersionGroup(
            version: new VersionRange(minVersion: new SemanticVersion(version, 0, 0)),
            value: "Snippet_v" + version,
            sources: new List<SnippetSource>
            {
                new SnippetSource(
                    startLine: 1,
                    endLine: 2,
                    file: null
                    )
            });
    }

    [Test]
    public async void MissingKey()
    {
        var snippets = new List<ReadSnippet>
        {
            new ReadSnippet(
                key: "foundkey1",
                version: VersionRange.All,
                startLine: 1,
                endLine: 1,
                value: " ",
                language: "c",
                file: "unknown"),
            new ReadSnippet(
                key: "foundkey2",
                version: VersionRange.All,
                startLine: 1,
                endLine: 1,
                value: " ",
                language: "c",
                file: "unknown"),
        };
        var snippetGroups = SnippetGrouper.Group(snippets).ToList();
        await Verify("<!-- import MissingKey -->", snippetGroups);
    }

    [Test]
    public async void MissingMultipleKeys()
    {
        var snippets = new List<ReadSnippet>
        {
            new ReadSnippet(key: "foundkey1",
                version: VersionRange.All,
                startLine: 1,
                endLine: 1,
                value: " ",
                language: "c",
                file: "unknown"),
            new ReadSnippet(key: "foundkey2",
                version: VersionRange.All,
                startLine: 1,
                endLine: 1,
                value: " ",
                language: "c",
                file: "unknown"),
        };
        var snippetGroups = SnippetGrouper.Group(snippets).ToList();
        await Verify("<!-- import MissingKey1 -->\r\n\r\n<!-- import MissingKey2 -->", snippetGroups);
    }


    [Test]
    public async void LotsOfText()
    {
        var snippets = new List<ReadSnippet>
        {
            new ReadSnippet(key: "foundkey1",
                value: "Value1",
                version: VersionRange.All,
                startLine: 1,
                endLine: 1,
                language: "c",
                file: null),
            new ReadSnippet(
                key: "foundkey2",
                value: "Value2",
                version: VersionRange.All,
                startLine: 1,
                endLine: 1,
                language: "c",
                file: null),
            new ReadSnippet(key: "foundkey3",
                value: "Value3",
                version: VersionRange.All,
                startLine: 1,
                endLine: 1,
                language: "c",
                file: null),
            new ReadSnippet(key: "foundkey4",
                value: "Value4",
                version: VersionRange.All,
                startLine: 1,
                endLine: 1,
                language: "c",
                file: null),
        };
        var snippetGroups = SnippetGrouper.Group(snippets).ToList();
        var markdownContent = @"
<!-- import FoundKey2 -->\r\b\n<!-- import FoundKey1 -->
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmgfkgjnfdjkgn
dflkgmxdklfmgkdflxmgfkgjnfdjkgn
dflkgmxdklfmgkdflxmg
dflkgmxdklfmdfgkjndfkjgngkdflxmg
dflkgmxdklfmdfgkjndfkjgngkdflxmg
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
kdjrngkjfncgdflkgmxdklfmgkdflxmg
kdjrngkjfncgdflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmgfkgjnfdjkgn<!-- import FoundKey1 -->
dflkgmxdklfmgkdflxmgfkgjnfdjkgn
dflkgmxdklfmgkdflxmg
dflkgmxdklfmdfgkjndfkjgngkdflxmg
dflkgmxdklfmdfgkjndfkjgngkdflxmg
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
kdjrngkjfncgdflkgmxdklfmgkdflxmg
kdjrngkjfncgdflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmgfkgjnfdjkgn
dflkgmxdklfmgkdflxmgfkgjnfdjkgn
dflkgmxdklfmgkdflxmg
dflkgmxdklfmdfgkjndfkjgngkdflxmg
dflkgmxdklfmdfgkjndfkjgngkdflxmg<!-- import FoundKey1 -->
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
kdjrngkjfncgdflkgmxdklfmgkdflxmg
kdjrngkjfncgdflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmgfkgjnfdjkgn
dflkgmxdklfmgkdflxmgfkgjnfdjkgn
dflkgmxdklfmgkdflxmg
dflkgmxdklfmdfgkjndfkjgngkdflxmg
dflkgmxdklfmdfgkjndfkjgngkdflxmg
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
kdjrngkjfncgdflkgmxdklfmgkdflxmg
kdjrngkjfncgdflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmgfkgjnfdjkgn
dflkgmxdklfmgkdflxmgfkgjnfdjkgn
dflkgmxdklfmgkdflxmg<!-- import FoundKey1 -->
dflkgmxdklfmdfgkjndfkjgngkdflxmg
dflkgmxdklfmdfgkjndfkjgngkdflxmg
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
kdjrngkjfncgdflkgmxdklfmgkdflxmg
kdjrngkjfncgdflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmgfkgjnfdjkgn
dflkgmxdklfmgkdflxmgfkgjnfdjkgn
dflkgmxdklfmgkdflxmg
dflkgmxdklfmdfgkjndfkjgngkdflxmg
dflkgmxdklfmdfgkjndfkjgngkdflxmg
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
kdjrngkjfncgdflkgmxdklfmgkdflxmg<!-- import FoundKey1 -->
kdjrngkjfncgdflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmgfkgjnfdjkgn
dflkgmxdklfmgkdflxmgfkgjnfdjkgn
dflkgmxdklfmgkdflxmg
dflkgmxdklfmdfgkjndfkjgngkdflxmg
dflkgmxdklfmdfgkjndfkjgngkdflxmg
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
kdjrngkjfncgdflkgmxdklfmgkdflxmg
kdjrngkjfncgdflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmgfkgjnfdjkgn
dflkgmxdklfmgkdflxmgfkgjnfdjkgn
dflkgmxdklfmgkdflxmg
dflkgmxdklfmdfgkjndfkjgngkdflxmg
dflkgmxdklfmdfgkjndfkjgngkdflxmg<!-- import FoundKey1 -->
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
kdjrngkjfncgdflkgmxdklfmgkdflxmg
kdjrngkjfncgdflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmgfkgjnfdjkgn
dflkgmxdklfmgkdflxmgfkgjnfdjkgn
dflkgmxdklfmgkdflxmg
dflkgmxdklfmdfgkjndfkjgngkdflxmg
dflkgmxdklfmdfgkjndfkjgngkdflxmg
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
kdjrngkjfncgdflkgmxdklfmgkdflxmg<!-- import FoundKey1 -->
kdjrngkjfncgdflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmgfkgjnfdjkgn
dflkgmxdklfmgkdflxmgfkgjnfdjkgn
dflkgmxdklfmgkdflxmg
dflkgmxdklfmdfgkjndfkjgngkdflxmg
dflkgmxdklfmdfgkjndfkjgngkdflxmg
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
kdjrngkjfncgdflkgmxdklfmgkdflxmg
kdjrngkjfncgdflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmgfkgjnfdjkgn
dflkgmxdklfmgkdflxmgfkgjnfdjkgn<!-- import FoundKey1 --><!-- import FoundKey1 -->
dflkgmxdklfmgkdflxmg
dflkgmxdklfmdfgkjndfkjgngkdflxmg
dflkgmxdklfmdfgkjndfkjgngkdflxmg
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
kdjrngkjfncgdflkgmxdklfmgkdflxmg
kdjrngkjfncgdflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmgfkgjnfdjkgn
dflkgmxdklfmgkdflxmgfkgjnfdjkgn
dflkgmxdklfmgkdflxmg
dflkgmxdklfmdfgkjndfkjgngkdflxmg
dflkgmxdklfmdfgkjndfkjgngkdflxmg
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
kdjrngkjfncgdflkgmxdklfmgkdflxmg<!-- import FoundKey1 -->
kdjrngkjfncgdflkgmxdklfmgkdflxmg
dflkgmxdklfmgkdflxmg
lkmdflkgmxdklfmgkdflxmg
";
        await Verify(markdownContent, snippetGroups);
    }
}