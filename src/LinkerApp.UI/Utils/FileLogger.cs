using System;
using System.IO;

namespace LinkerApp.UI.Utils
{
    public static class FileLogger
    {
        private static readonly string LogsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "debug");
        private static readonly string LogFileName = $"debug_{DateTime.Now:yyyyMMdd}.log";
        private static readonly string LogFilePath = Path.Combine(LogsDirectory, LogFileName);
        private static readonly object LockObject = new object();

        static FileLogger()
        {
            // Ensure the logs directory exists
            EnsureLogDirectoryExists();
            
            // Clean up old log files
            CleanupOldLogs();
        }

        public static void Log(string message)
        {
            lock (LockObject)
            {
                try
                {
                    EnsureLogDirectoryExists();
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logEntry = $"[{timestamp}] {message}";
                    File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                }
                catch
                {
                    // Ignore logging errors to prevent crashes
                }
            }
        }

        public static void Clear()
        {
            lock (LockObject)
            {
                try
                {
                    if (File.Exists(LogFilePath))
                    {
                        File.Delete(LogFilePath);
                    }
                }
                catch
                {
                    // Ignore errors
                }
            }
        }

        private static void EnsureLogDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(LogsDirectory))
                {
                    Directory.CreateDirectory(LogsDirectory);
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        private static void CleanupOldLogs()
        {
            try
            {
                if (!Directory.Exists(LogsDirectory))
                    return;

                var cutoffDate = DateTime.Now.AddDays(-3);
                var logFiles = Directory.GetFiles(LogsDirectory, "debug_*.log");

                foreach (var logFile in logFiles)
                {
                    var fileInfo = new FileInfo(logFile);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(logFile);
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        public static string GetLogFilePath()
        {
            return LogFilePath;
        }

        public static string GetLogsDirectory()
        {
            return LogsDirectory;
        }
    }
}