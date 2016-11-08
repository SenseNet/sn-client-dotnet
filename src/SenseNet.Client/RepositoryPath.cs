using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SenseNet.Client
{
    /// <summary>
    /// Client side helper methods for Content Repository paths.
    /// </summary>
    public static class RepositoryPath
    {
        /// <summary>
        /// Separator character in paths.
        /// </summary>
        public static readonly string PathSeparator = "/";

        /// <summary>
        /// Gets the content name (the last segment) from a Content Repository path.
        /// </summary>
        /// <param name="path">Content Repository path.</param>
        public static string GetFileName(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var index = path.LastIndexOf(PathSeparator, StringComparison.Ordinal);
            return index < 0 ? path : path.Substring(index + 1);
        }
        /// <summary>
        /// Gets the parent path from a Content Repository path.
        /// </summary>
        /// <param name="path">Content Repository path.</param>
        public static string GetParentPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            var index = path.LastIndexOf(PathSeparator, StringComparison.Ordinal);
            return index <= 0 ? string.Empty : path.Substring(0, index);
        }
        /// <summary>
        /// Combines two Content Repository path segments.
        /// </summary>
        /// <param name="path1">Left segment.</param>
        /// <param name="path2">Right segment.</param>
        public static string Combine(string path1, string path2)
        {
            if (path1 == null)
                throw new ArgumentNullException(nameof(path1));
            if (path2 == null)
                throw new ArgumentNullException(nameof(path2));
            if (path1.Length == 0)
                return path2;
            if (path2.Length == 0)
                return path1;

            var x = 0;
            if (path1[path1.Length - 1] == '/')
                x += 2;
            if (path2[0] == '/')
                x += 1;
            switch (x)
            {
                case 0:    //  path1,   path2
                    return string.Concat(path1, "/", path2);
                case 1:    //  path1,  /path2
                case 2:    //  path1/,  path2
                    return string.Concat(path1, path2);
                case 3:    //  path1/, /path2
                    var sb = new StringBuilder(path1);
                    sb.Length--;
                    sb.Append(path2);
                    return sb.ToString();
            }
            return null;
        }

        /// <summary>
        /// Determines whether a character is allowed in a content name or not.
        /// </summary>
        /// <param name="c">A character to check.</param>
        public static bool IsInvalidNameChar(char c)
        {
            //TODO: make IsInvalidNameChar configurable
            return new Regex("[\\$&\\+\\\\,/:;=?@\"<>\\#%{}|^~\\[\\u005D'’`\\*\t\r\n]").IsMatch(c.ToString());
        }

        /// <summary>
        /// Converts a local file system path (e.g. c:\temp\abc) to a repository path (/Root/ParentContainer/abc)
        /// based on the provided local and repository base paths.
        /// </summary>
        /// <param name="fileSystemPath">File system path to convert.</param>
        /// <param name="localBasePath">Base path in the file system (e.g. c:\temp).</param>
        /// <param name="repositoryBasePath">Base path in the Content Repository (e.g. /Root/ParentContainer).</param>
        /// <returns>A content repository path computed from the provided file system path.</returns>
        public static string ConvertFromLocalPath(string fileSystemPath, string localBasePath, string repositoryBasePath)
        {
            if (string.IsNullOrEmpty(fileSystemPath))
                return string.Empty;
            if (string.IsNullOrEmpty(localBasePath))
                throw new ArgumentNullException(nameof(localBasePath));
            if (string.IsNullOrEmpty(repositoryBasePath))
                throw new ArgumentNullException(nameof(repositoryBasePath));

            if (!repositoryBasePath.Equals("/Root", StringComparison.InvariantCulture) && !repositoryBasePath.StartsWith("/Root/"))
                throw new InvalidOperationException($"Invalid repository base path ({repositoryBasePath}).");

            // consolidate path values
            fileSystemPath = fileSystemPath.TrimEnd('\\');
            localBasePath = localBasePath.TrimEnd('\\');
            repositoryBasePath = repositoryBasePath.TrimEnd(PathSeparator.ToCharArray());

            if (string.CompareOrdinal(fileSystemPath, localBasePath) == 0)
                return repositoryBasePath;
            if (!fileSystemPath.StartsWith(localBasePath + "\\", StringComparison.InvariantCultureIgnoreCase))
                throw new InvalidOperationException($"File system path ({fileSystemPath}) must start with the local base path ({localBasePath}).");

            var sourceRelativePath = fileSystemPath.Substring(localBasePath.Length + 1);
            var repoRelativePath = sourceRelativePath.Replace("\\", PathSeparator);

            return Combine(repositoryBasePath, repoRelativePath);
        }
        /// <summary>
        /// Converts a repository path (e.g. /Root/ParentContainer/abc) to a local file system path (c:\temp\abc)
        /// based on the provided local and repository base paths.
        /// </summary>
        /// <param name="repositoryPath">Content Repository path to convert.</param>
        /// <param name="localBasePath">Base path in the file system (e.g. c:\temp).</param>
        /// <param name="repositoryBasePath">Base path in the Content Repository (e.g. /Root/ParentContainer).</param>
        /// <returns>A local file system path computed from the provided Content Repository path.</returns>
        public static string ConvertToLocalPath(string repositoryPath, string localBasePath, string repositoryBasePath)
        {
            if (string.IsNullOrEmpty(repositoryPath))
                return string.Empty;
            if (string.IsNullOrEmpty(localBasePath))
                throw new ArgumentNullException(nameof(localBasePath));
            if (string.IsNullOrEmpty(repositoryBasePath))
                throw new ArgumentNullException(nameof(repositoryBasePath));

            if (!repositoryPath.Equals("/Root", StringComparison.InvariantCulture) && !repositoryPath.StartsWith("/Root/"))
                throw new InvalidOperationException($"Invalid repository path ({repositoryPath}).");
            if (!repositoryBasePath.Equals("/Root", StringComparison.InvariantCulture) && !repositoryBasePath.StartsWith("/Root/"))
                throw new InvalidOperationException($"Invalid repository base path ({repositoryBasePath}).");

            // consolidate path values
            repositoryPath = repositoryPath.TrimEnd(PathSeparator.ToCharArray());
            localBasePath = localBasePath.TrimEnd('\\');
            repositoryBasePath = repositoryBasePath.TrimEnd(PathSeparator.ToCharArray());

            if (string.CompareOrdinal(repositoryPath, repositoryBasePath) == 0)
                return localBasePath;
            if (!repositoryPath.StartsWith(repositoryBasePath + PathSeparator, StringComparison.InvariantCultureIgnoreCase))
                throw new InvalidOperationException($"Repository path ({repositoryPath}) must start with the repository base path ({repositoryBasePath}).");

            var sourceRelativePath = repositoryPath.Substring(repositoryBasePath.Length + 1);
            var localRelativePath = sourceRelativePath.Replace(PathSeparator, "\\");

            return Path.Combine(localBasePath, localRelativePath);
        }
    }
}
