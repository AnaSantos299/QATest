using System;
using System.IO;
using System.Timers;
using System.Security.Cryptography;
using System.Linq;

namespace TestTask
{
    public class SyncProgram
    {
        //Log file path
        private const string log_File = "Sync.log";
        private const int syncIntervalSeconds = 5;

        public static void Main()
        {
            //Gets project root directory 
            string projectDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            //Path for source and replica folders
            string sourceFolder = Path.Combine(projectDir, "Source");
            string replicaFolder = Path.Combine(projectDir, "Replica");
            Console.WriteLine("Folder Sync");

            //Timer that triggers SyncFolders() at specific intervals
            using (var timer = new System.Timers.Timer(syncIntervalSeconds * 1000))
            {
                timer.Elapsed += (s, e) => SyncFolders(sourceFolder, replicaFolder);
                timer.AutoReset = true;
                timer.Start();

                Console.WriteLine($"Syncing Every {syncIntervalSeconds} seconds.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
        }

        //List to track the state of the source folder files between Synchronizations
        private static List<string> previousFiles = new();

        private static void SyncFolders(string sourceFolder, string replicaFolder)
        {
            try
            {
                //detects new files on the source folder
                var currentFiles = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories);
                foreach (var file in currentFiles)
                {
                    if (previousFiles.Count > 0 && !previousFiles.Contains(file))
                    {
                        Log($"New file detected: {Path.GetRelativePath(sourceFolder, file)}");
                    }
                }
                previousFiles = currentFiles.ToList();

                //Validates if source folder exists
                if (!Directory.Exists(sourceFolder))
                {
                    Log($"Source folder not found: {Path.GetFullPath(sourceFolder)}");
                    return;
                }

                //Copy/Update files
                foreach (var sourceFile in Directory.EnumerateFiles(sourceFolder, "*", SearchOption.AllDirectories))
                {
                    var destFile = sourceFile.Replace(sourceFolder, replicaFolder);
                    var destFolder = Path.GetDirectoryName(destFile);


                    if (!Directory.Exists(destFolder))
                        Directory.CreateDirectory(destFolder);

                    if (!File.Exists(destFile) || File.GetLastWriteTimeUtc(sourceFile) > File.GetLastWriteTimeUtc(destFile) || !FilesAreEqual(sourceFile, destFile))
                    {
                        File.Copy(sourceFile, destFile, overwrite: true);
                        Log($"File copied to replica folder: {Path.GetFileName(sourceFile)}");
                    }
                }

                //Delete files in the replica folder that do not exist in the source folder
                foreach (var replicaFile in Directory.EnumerateFiles(replicaFolder, "*", SearchOption.AllDirectories))
                {
                    var sourceFile = replicaFile.Replace(replicaFolder, sourceFolder);

                    if (!File.Exists(sourceFile))
                    {
                        File.Delete(replicaFile);
                        Log($"File Deleted: {Path.GetFileName(replicaFile)}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
        }
        //Logs messages to both console and log file with utc timestamps
        private static void Log(string message)
        {
            string logEntry = $"{DateTime.Now:u} {message}";
            Console.WriteLine(logEntry);
            File.AppendAllText(log_File, logEntry + Environment.NewLine);
        }

        private static bool FilesAreEqual(String file1, string file2)
        {
            //checks file existence and size
            var info1 = new FileInfo(file1);
            var info2 = new FileInfo(file2);
            if (!info1.Exists || !info2.Exists || info1.Length != info2.Length)
                return false;

            //comparison using SHA256
            using var sha256 = SHA256.Create();
            byte[] hash1, hash2;

            //Stream based reading for memory efficiency
            using (var stream1 = File.OpenRead(file1))
                hash1 = sha256.ComputeHash(stream1);

            using (var stream2 = File.OpenRead(file2))
                hash2 = sha256.ComputeHash(stream2);

            return hash1.SequenceEqual(hash2);
        }
    }
}