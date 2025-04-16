using System;
using System.IO;
using System.Timers;

namespace TestTask
{
    public class SyncProgram
    {
        private const string Log_File = "Sync.Log";
        private const int Sync_Interval_Seconds = 5;

        public static void Main()
        {
            string projectDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            string sourceFolder = Path.Combine(projectDir, "Source");
            string replicaFolder = Path.Combine(projectDir, "Replica");
            Console.WriteLine("Folder Sync");

            using (var timer = new System.Timers.Timer(Sync_Interval_Seconds * 1000))
            {
                timer.Elapsed += (s, e) => SyncFolders(sourceFolder, replicaFolder);
                timer.AutoReset = true;
                timer.Start();

                Console.WriteLine($"Syncing Every {Sync_Interval_Seconds} seconds.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
        }

        private static void SyncFolders(string sourceFolder, string replicaFolder)
        {
            try
            {
                if (!Directory.Exists(sourceFolder))
                {
                    Log($"Source folder not found: {Path.GetFullPath(sourceFolder)}");
                }

                //Copy or Update files
                foreach (var sourceFile in Directory.EnumerateFiles(sourceFolder, "*", SearchOption.AllDirectories))
                {
                    var destFile = sourceFile.Replace(sourceFolder, replicaFolder);
                    var destFolder = Path.GetDirectoryName(destFile);

                    if (!Directory.Exists(destFolder))
                        Directory.CreateDirectory(destFolder);

                    if (!File.Exists(destFile) || File.GetLastWriteTimeUtc(sourceFile) > File.GetLastWriteTimeUtc(destFile))
                    {
                        File.Copy(sourceFile, destFile, overwrite: true);
                        Log($"Copied: {Path.GetFileName(sourceFile)}");
                    }
                }

                //Delete files in the replica folder that do not exist in the source folder
                foreach (var replicaFile in Directory.EnumerateFiles(replicaFolder, "*", SearchOption.AllDirectories))
                {
                    var sourceFile = replicaFile.Replace(replicaFolder, sourceFolder);

                    if (!File.Exists(sourceFile))
                    {
                        File.Delete(replicaFile);
                        Log($"Deleted: {Path.GetFileName(replicaFile)}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
        }
        private static void Log(string message)
        {
            string LogEntry = $"{DateTime.Now:u} {message}";
            Console.WriteLine(LogEntry);
            File.AppendAllText(Log_File, LogEntry + Environment.NewLine);
        }
    }
}