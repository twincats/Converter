using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Converter
{
    public class FolderSizeCalculator
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll")]
        private static extern bool FindClose(IntPtr hFindFile);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WIN32_FIND_DATA
        {
            public FileAttributes dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        public static async Task<long> GetFolderSizeAsync(string folderPath)
        {
            // Thread-safe collection to accumulate sizes
            var folderSizes = new ConcurrentBag<long>();
            var tasks = new ConcurrentBag<Task>(); // To track tasks

            // Define a recursive helper method
            Task ProcessFolderAsync(string path)
            {
                WIN32_FIND_DATA findData;
                IntPtr findHandle = FindFirstFile(Path.Combine(path, "*"), out findData);

                if (findHandle != IntPtr.Zero)
                {
                    try
                    {
                        do
                        {
                            string currentFileName = findData.cFileName;

                            // Skip "." and ".." directories
                            if (currentFileName == "." || currentFileName == "..")
                                continue;

                            string fullPath = Path.Combine(path, currentFileName);

                            if ((findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
                            {
                                // Process subdirectories asynchronously
                                tasks.Add(ProcessFolderAsync(fullPath)); // Add the task to the collection
                            }
                            else
                            {
                                // Calculate file size
                                long fileSize = ((long)findData.nFileSizeHigh << 32) | findData.nFileSizeLow;
                                folderSizes.Add(fileSize);
                            }

                        } while (FindNextFile(findHandle, out findData));
                    }
                    finally
                    {
                        FindClose(findHandle);
                    }
                }

                return Task.CompletedTask;
            }

            // Start processing the root folder
            await ProcessFolderAsync(folderPath);

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            // Sum up all file sizes from the ConcurrentBag
            return folderSizes.Sum();
        }

        public static long GetDirectorySizeParallel(string path)
        {
            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                            .AsParallel()
                            .Sum(file => new FileInfo(file).Length);
        }

        public static double CalculatePercentChange(long sizeBefore, long sizeAfter)
        {
            if (sizeBefore == 0)
            {
                throw new DivideByZeroException("Size before cannot be zero.");
            }

            double percentChange = ((double)(sizeAfter - sizeBefore) / sizeBefore) * 100;
            return percentChange;
        }

        public static string FormatSize(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;
            const long TB = GB * 1024;

            if (bytes >= TB)
            {
                return $"{(double)bytes / TB:0.##} TB";
            }
            else if (bytes >= GB)
            {
                return $"{(double)bytes / GB:0.##} GB";
            }
            else if (bytes >= MB)
            {
                return $"{(double)bytes / MB:0.##} MB";
            }
            else if (bytes >= KB)
            {
                return $"{(double)bytes / KB:0.##} KB";
            }
            else
            {
                return $"{bytes} Bytes";
            }
        }

        public static string FormatSizeSingle(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;
            const long TB = GB * 1024;

            if (bytes >= TB)
            {
                return $"{(double)bytes / TB:0.#} TB";
            }
            else if (bytes >= GB)
            {
                return $"{(double)bytes / GB:0.#} GB";
            }
            else if (bytes >= MB)
            {
                return $"{(double)bytes / MB:0.#} MB";
            }
            else if (bytes >= KB)
            {
                return $"{(double)bytes / KB:0.#} KB";
            }
            else
            {
                return $"{bytes} Bytes";
            }
        }

    }
}
