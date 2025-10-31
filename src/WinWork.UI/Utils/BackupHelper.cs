using System;
using System.IO;
using System.Threading.Tasks;
namespace WinWork.UI.Utils
{
    public static class BackupHelper
    {
        /// <summary>
        /// Create a copy of the application's SQLite DB into the backup folder and record the last backup time via settingsService.
        /// Returns the destination file path on success, or null on failure.
        /// </summary>
        public static async Task<string?> CreateBackupAsync(string? explicitFolder = null)
        {
            try
            {
                // Determine backup folder: priority - explicit param, saved setting, default Documents\WinWork\Backups
                string backupFolder = string.Empty;
                if (!string.IsNullOrWhiteSpace(explicitFolder))
                {
                    backupFolder = explicitFolder!;
                }

                if (string.IsNullOrWhiteSpace(backupFolder))
                {
                    backupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WinWork", "Backups");
                }

                if (!Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                }

                // Locate current DB
                var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinWork", "winwork.db");
                if (!File.Exists(dbPath))
                {
                    return null;
                }

                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var destFile = Path.Combine(backupFolder, $"winwork_backup_{timestamp}.db");

                File.Copy(dbPath, destFile, overwrite: true);

                // Caller is responsible for recording LastBackupUtc in settings if desired

                return destFile;
            }
            catch
            {
                return null;
            }
        }
    }
}
