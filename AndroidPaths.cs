using System;
using System.IO;

namespace RageCoop.Client
{
    // The PC client used paths relative to the GTA V folder with Windows separators ("Scripts\\RageCoop\\Data").
    // On the phone everything lives below /storage/emulated/0/games/gtav, so make every path absolute.
    internal static class AndroidPaths
    {
        public const string GameRoot = "/storage/emulated/0/games/gtav";
        public const string DataDirectory = GameRoot + "/scripts/RageCoop/Data";

        public static string Normalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return DataDirectory;
            path = path.Replace('\\', '/');
            if (path.StartsWith("/"))
                return path;
            if (path.StartsWith("Scripts/", StringComparison.OrdinalIgnoreCase))
                path = "scripts/" + path.Substring(8);
            return GameRoot + "/" + path.TrimStart('/');
        }

        // Resource names arrive from the PC server and may contain Windows backslashes.
        // Normalize them to Android path separators and reject rooted/traversal paths.
        public static string NormalizeRelativePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("Resource path is empty.", "relativePath");

            string portable = relativePath.Replace('\\', '/');
            if (portable.StartsWith("/", StringComparison.Ordinal) || Path.IsPathRooted(portable))
                throw new ArgumentException("Rooted resource paths are not allowed.", "relativePath");

            string[] parts = portable.Split('/');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 0 || parts[i] == "." || parts[i] == ".." ||
                    parts[i].IndexOf(':') >= 0 || parts[i].IndexOf('\0') >= 0)
                    throw new ArgumentException("Invalid resource path segment.", "relativePath");
            }
            return Path.Combine(parts);
        }

        public static string CombineUnder(string root, string relativePath)
        {
            string fullRoot = Path.GetFullPath(root);
            string fullPath = Path.GetFullPath(Path.Combine(fullRoot, NormalizeRelativePath(relativePath)));
            string prefix = fullRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(prefix, StringComparison.Ordinal))
                throw new ArgumentException("Resource path escapes its destination folder.", "relativePath");
            return fullPath;
        }
    }
}
