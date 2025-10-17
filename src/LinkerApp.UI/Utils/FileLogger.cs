using System;
using System.IO;

namespace LinkerApp.UI.Utils
{
    public static class FileLogger
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");
        private static readonly object LockObject = new object();

        public static void Log(string message)
        {
            lock (LockObject)
            {
                try
                {
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
    }
}