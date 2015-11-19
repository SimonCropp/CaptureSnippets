using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NuGet.Versioning;

namespace CaptureSnippets
{
    /// <summary>
    /// Allows <see cref="SnippetSource"/>s to be grouped by their <see cref="VersionRange"/>.
    /// </summary>
    public class VersionGroup : IEnumerable<SnippetSource>
    {
        /// <summary>
        /// Initialise a new insatnce of <see cref="VersionGroup"/>.
        /// </summary>
        public VersionGroup(VersionRange version, string value, IEnumerable<SnippetSource> sources)
        {
            Guard.AgainstNull(version,"version");
            Guard.AgainstNull(sources, "sources");
            Value = value;
            Version = version;
            Sources = sources.ToList();
        }

        /// <summary>
        ///  The version that all the child <see cref="SnippetSource"/>s have.
        /// </summary>
        public readonly VersionRange Version;

        /// <summary>
        /// The contents of the snippet
        /// </summary>
        public readonly string Value;

        /// <summary>
        /// All the snippets with a common <see cref="VersionRange"/>.
        /// </summary>
        public readonly IEnumerable<SnippetSource> Sources;

        /// <summary>
        /// Enumerates over <see cref="Sources"/>.
        /// </summary>
        public IEnumerator<SnippetSource> GetEnumerator()
        {
            return Sources.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}