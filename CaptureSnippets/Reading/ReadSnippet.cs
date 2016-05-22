using System;
using System.Diagnostics;
using NuGet.Versioning;

namespace CaptureSnippets
{
    /// <summary>
    /// A sub item of <see cref="ReadSnippets"/>.
    /// </summary>
    [DebuggerDisplay("Key={Key}, FileLocation={FileLocation}, Error={Error}, Package={Package.ValueOrNone}")]
    public class ReadSnippet
    {

        /// <summary>
        /// Initialise a new instance of <see cref="ReadSnippet"/>.
        /// </summary>
        public ReadSnippet(string key, int lineNumberInError, string path, string error)
        {
            Guard.AgainstNegativeAndZero(lineNumberInError, "lineNumberInError");
            Guard.AgainstNullAndEmpty(key, "key");
            Guard.AgainstNullAndEmpty(error, "error");
            Key = key;
            StartLine = lineNumberInError;
            EndLine = lineNumberInError;
            IsInError = true;
            Path = path;
            Error = error;
        }

        /// <summary>
        /// Initialise a new instance of <see cref="ReadSnippet"/>.
        /// </summary>
        public ReadSnippet(int startLine, int endLine, string value, string key, string language, string path, VersionRange version, Package package)
        {
            Guard.AgainstNullAndEmpty(key, "key");
            Guard.AgainstUpperCase(key, "key");
            Guard.AgainstNull(language, "language");
            Guard.AgainstNull(package, "package");
            Guard.AgainstUpperCase(language, "language");
            Guard.AgainstNegativeAndZero(startLine, "startLine");
            Guard.AgainstNegativeAndZero(endLine, "endLine");

            StartLine = startLine;
            EndLine = endLine;
            Value = value;
            ValueHash = value.RemoveWhitespace().GetHashCode();
            Key = key;
            Language = language;
            Path = path;
            this.version = version;
            this.package = package;
        }

        public readonly string Error;

        public readonly bool IsInError;


        /// <summary>
        /// A hash of the <see cref="Value"/>.
        /// </summary>
        public readonly int ValueHash;

        /// <summary>
        /// The line the snippets started on
        /// </summary>
        public readonly int StartLine;

        /// <summary>
        /// The line the snippet ended on.
        /// </summary>
        public readonly int EndLine;

        /// <summary>
        /// The contents of the snippet
        /// </summary>
        public readonly string Value;

        /// <summary>
        /// The key used to identify the snippet
        /// </summary>
        public readonly string Key;

        /// <summary>
        /// The language of the snippet, extracted from the file extension of the input file.
        /// </summary>
        public readonly string Language;

        /// <summary>
        /// The path the snippet was read from.
        /// </summary>
        public readonly string Path;

        VersionRange version;

        Package package;


        /// <summary>
        /// The <see cref="Path"/>, <see cref="StartLine"/> and <see cref="EndLine"/> concatenated.
        /// </summary>
        public string FileLocation => $"{Path}({StartLine}-{EndLine})";

        /// <summary>
        /// The <see cref="VersionRange"/> that was inferred for the snippet.
        /// </summary>
        public VersionRange Version
        {
            get
            {
                if (IsInError)
                {
                    throw new Exception("Cannot access Version when IsInError.");
                }
                return version;
            }
        }

        /// <summary>
        /// The Package that was inferred for the snippet.
        /// </summary>
        public Package Package
        {
            get
            {
                if (IsInError)
                {
                    throw new Exception("Cannot access Package when IsInError.");
                }
                return package;
            }
        }
    }
}