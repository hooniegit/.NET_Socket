using System;
using System.IO;
using System.Text;

namespace LogFileHandlerApp
{
    public class LogFileHandler
    {
        string defaultPath = @"C:\Users\dhkim\Desktop\TestFolder\";
        
        // private const long MaxLogFileSize = 10 * 1024 * 1024; // 10MB
        private const long MaxLogFileSize = 3 * 1024; // 3KB

        public void WriteLog(int partition, long offset, string data, DateTime dataTransferTime)
        {
            string partitionPath = defaultPath + partition.ToString();
            if (!Directory.Exists(partitionPath))
            {
                Directory.CreateDirectory(partitionPath);
            }

            string latestLogFilePath = GetLatestLogFilePath(partitionPath);

            if (latestLogFilePath == null || new FileInfo(latestLogFilePath).Length >= MaxLogFileSize)
            {
                latestLogFilePath = CreateNewLogFile(partitionPath);
            }

            AppendDataToLogFile(latestLogFilePath, offset, data);
            AppendOffsetToOffsetFile(partitionPath, offset, latestLogFilePath);
        }

        private string GetLatestLogFilePath(string partitionPath)
        {
            var logFiles = Directory.GetFiles(partitionPath, "*.log");
            if (logFiles.Length == 0) return null;

            Array.Sort(logFiles);
            return logFiles[^1]; // Get the last file in the sorted array
        }

        private string CreateNewLogFile(string partitionPath)
        {
            var logFiles = Directory.GetFiles(partitionPath, "*.log");
            int newLogFileIndex = logFiles.Length;
            string newLogFileName = newLogFileIndex.ToString("D10") + ".log";
            string newLogFilePath = Path.Combine(partitionPath, newLogFileName);

            using (File.Create(newLogFilePath)) { }

            return newLogFilePath;
        }

        private void AppendDataToLogFile(string logFilePath, long offset, string data)
        {
            using (StreamWriter writer = new StreamWriter(logFilePath, true, Encoding.UTF8))
            {
                writer.WriteLine($"[{offset}] [{data}]");
            }
        }

        private void AppendOffsetToOffsetFile(string partitionPath, long offset, string logFilePath)
        {
            string offsetFilePath = Path.Combine(partitionPath, "offsets.txt");

            using (StreamWriter writer = new StreamWriter(offsetFilePath, true, Encoding.UTF8))
            {
                writer.WriteLine($"{offset} {logFilePath}");
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {

            LogFileHandler logFileHandler = new LogFileHandler();
            int partition = 0;

            long initialOffset = 0;
            do
            {
                // Test data
                string[] testData = new string[]
                {
                    "First log entry",
                    "Second log entry",
                    "Third log entry",
                    "Fourth log entry",
                    "Fifth log entry"
                };

                // Writing test data to log files
                for (int i = 0; i < testData.Length; i++)
                {
                    logFileHandler.WriteLog(partition, initialOffset + i, testData[i], DateTime.Now);
                    Console.WriteLine($"Written: Offset = {initialOffset + i}, Data = {testData[i]}");
                }

                // Verify the logs and offset files
                VerifyLogFiles(partition);

                initialOffset += 5;
            } while (initialOffset < 1000000);
        }

        private static void VerifyLogFiles(int partition)
        {
            string defaultPath = @"C:\Users\dhkim\Desktop\TestFolder\";
            string partitionPath = defaultPath + partition.ToString();
            var logFiles = Directory.GetFiles(partitionPath, "*.log");
            var offsetFilePath = Path.Combine(partitionPath, "offsets.txt");

            Console.WriteLine("\nLog Files:");
            foreach (var logFile in logFiles)
            {
                Console.WriteLine($"\nContents of {logFile}:");
                Console.WriteLine(File.ReadAllText(logFile));
            }

            Console.WriteLine("\nOffsets File:");
            if (File.Exists(offsetFilePath))
            {
                Console.WriteLine(File.ReadAllText(offsetFilePath));
            }
            else
            {
                Console.WriteLine("Offsets file not found.");
            }
        }
    }

}
