using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynPad
{
    class PathExtension
    {
        /// <summary>
        /// Returns a relative path from one path to another.
        /// </summary>
        /// <param name="relativeTo">The source path the result should be relative to. This path is always considered to be a directory</param>
        /// <param name="path">The destination path</param>
        /// <returns>The relative path, or path if the paths don't share the same root.</returns>
        public static string RelativePath(string relativeTo, string path)
        {
            string[] absDirs = relativeTo.Split('\\');
            string[] relDirs = path.Split('\\');

            // Get the shortest of the two paths
            int len = absDirs.Length < relDirs.Length ? absDirs.Length :
            relDirs.Length;

            // Use to determine where in the loop we exited
            int lastCommonRoot = -1;
            int index;

            // Find common root
            for (index = 0; index < len; index++)
            {
                if (absDirs[index] == relDirs[index]) lastCommonRoot = index;
                else break;
            }

            // If we didn't find a common prefix then throw
            if (lastCommonRoot == -1)
            {
                return path;
            }

            // Build up the relative path
            StringBuilder relativePath = new StringBuilder();

            // Add on the ..
            for (index = lastCommonRoot + 1; index < absDirs.Length; index++)
            {
                if (absDirs[index].Length > 0) relativePath.Append("..\\");
            }

            // Add on the folders
            for (index = lastCommonRoot + 1; index < relDirs.Length - 1; index++)
            {
                relativePath.Append(relDirs[index] + "\\");
            }
            relativePath.Append(relDirs[relDirs.Length - 1]);

            return relativePath.ToString();
        }
        /// <summary>
        /// get absolute path for a relative path
        /// </summary>
        /// <param name="relativeTo">base path</param>
        /// <param name="path">relative path</param>
        /// <returns></returns>
        public static string GetAbsolutePath(string relativeTo, string path)
        {
            return Path.GetFullPath(Path.Combine(relativeTo, path));
        }
    }
}
