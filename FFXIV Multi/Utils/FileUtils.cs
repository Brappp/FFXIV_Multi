using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FFXIVClientManager.Utils
{
    /// <summary>
    /// Utility methods for file operations
    /// </summary>
    public static class FileUtils
    {
        /// <summary>
        /// Recursively copies a directory and its contents
        /// </summary>
        public static void CopyDirectory(string sourceDir, string destDir)
        {
            // Create destination directory if it doesn't exist
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            // Copy files
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destDir, fileName);
                File.Copy(file, destFile, true);
            }

            // Copy subdirectories recursively
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string subDirName = Path.GetFileName(subDir);
                string destSubDir = Path.Combine(destDir, subDirName);
                CopyDirectory(subDir, destSubDir);
            }
        }

        /// <summary>
        /// Calculates the total size of a directory including subdirectories
        /// </summary>
        public static long GetDirectorySize(string directory)
        {
            if (!Directory.Exists(directory))
                return 0;

            long size = 0;

            // Add size of files
            foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            {
                try
                {
                    FileInfo fileInfo = new FileInfo(file);
                    size += fileInfo.Length;
                }
                catch
                {
                    // Ignore files that can't be accessed
                }
            }

            return size;
        }

        /// <summary>
        /// Finds files matching a pattern in a directory
        /// </summary>
        public static List<string> FindFiles(string directory, string searchPattern, bool recursive = true)
        {
            if (!Directory.Exists(directory))
                return new List<string>();

            try
            {
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                return Directory.GetFiles(directory, searchPattern, searchOption).ToList();
            }
            catch
            {
                // Return empty list if there's an error
                return new List<string>();
            }
        }

        /// <summary>
        /// Creates a directory if it doesn't exist
        /// </summary>
        public static bool EnsureDirectoryExists(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Safely deletes a directory and its contents
        /// </summary>
        public static bool SafeDeleteDirectory(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return false;

            try
            {
                Directory.Delete(path, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Converts file size to a human-readable string
        /// </summary>
        public static string FormatFileSize(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (bytes < KB)
                return $"{bytes} B";
            if (bytes < MB)
                return $"{bytes / KB:F2} KB";
            if (bytes < GB)
                return $"{bytes / MB:F2} MB";
            return $"{bytes / GB:F2} GB";
        }

        /// <summary>
        /// Gets a list of all files in a directory with their relative paths
        /// </summary>
        public static Dictionary<string, string> GetFilesWithRelativePaths(string directory)
        {
            var result = new Dictionary<string, string>();

            if (!Directory.Exists(directory))
                return result;

            try
            {
                string[] files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);

                foreach (string file in files)
                {
                    string relativePath = file.Substring(directory.Length).TrimStart('\\', '/');
                    result[relativePath] = file;
                }
            }
            catch
            {
                // Ignore errors
            }

            return result;
        }

        /// <summary>
        /// Creates a SHA256 hash of a file
        /// </summary>
        public static string CalculateFileHash(string filePath)
        {
            if (!File.Exists(filePath))
                return string.Empty;

            try
            {
                using (var sha256 = SHA256.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the last modified date of a file or directory
        /// </summary>
        public static DateTime GetLastModifiedDate(string path)
        {
            try
            {
                if (File.Exists(path))
                    return File.GetLastWriteTime(path);

                if (Directory.Exists(path))
                    return Directory.GetLastWriteTime(path);
            }
            catch
            {
                // Ignore errors
            }

            return DateTime.MinValue;
        }

        /// <summary>
        /// Tries to create a backup of a file before modifying it
        /// </summary>
        public static bool CreateFileBackup(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            try
            {
                string backupPath = $"{filePath}.bak";
                File.Copy(filePath, backupPath, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Restores a file from its backup
        /// </summary>
        public static bool RestoreFileFromBackup(string filePath)
        {
            string backupPath = $"{filePath}.bak";

            if (!File.Exists(backupPath))
                return false;

            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);

                File.Copy(backupPath, filePath);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}