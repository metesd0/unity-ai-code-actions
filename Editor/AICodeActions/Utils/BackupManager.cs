using System;
using System.IO;
using UnityEngine;

namespace AICodeActions.Utils
{
    /// <summary>
    /// Manages file backups before modifications
    /// </summary>
    public static class BackupManager
    {
        private static readonly string BackupFolder = Path.Combine(Application.dataPath, "..", "Temp", "AICodeActions_Backups");

        static BackupManager()
        {
            if (!Directory.Exists(BackupFolder))
                Directory.CreateDirectory(BackupFolder);
        }

        /// <summary>
        /// Creates a backup of a file before modification
        /// </summary>
        public static string CreateBackup(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Cannot backup non-existent file: {filePath}");

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = Path.GetFileName(filePath);
            var backupName = $"{fileName}.{timestamp}.bak";
            var backupPath = Path.Combine(BackupFolder, backupName);

            File.Copy(filePath, backupPath, true);
            Debug.Log($"[AI Code Actions] Backup created: {backupPath}");

            return backupPath;
        }

        /// <summary>
        /// Restores a file from backup
        /// </summary>
        public static void RestoreBackup(string backupPath, string targetPath)
        {
            if (!File.Exists(backupPath))
                throw new FileNotFoundException($"Backup file not found: {backupPath}");

            File.Copy(backupPath, targetPath, true);
            Debug.Log($"[AI Code Actions] Restored from backup: {targetPath}");
        }

        /// <summary>
        /// Gets all backup files for a specific file
        /// </summary>
        public static string[] GetBackupsForFile(string fileName)
        {
            if (!Directory.Exists(BackupFolder))
                return new string[0];

            var pattern = $"{fileName}.*.bak";
            return Directory.GetFiles(BackupFolder, pattern);
        }

        /// <summary>
        /// Cleans up old backups (keeps last 10)
        /// </summary>
        public static void CleanupOldBackups()
        {
            if (!Directory.Exists(BackupFolder))
                return;

            var backups = Directory.GetFiles(BackupFolder, "*.bak");
            if (backups.Length <= 10)
                return;

            // Sort by creation time and delete oldest
            Array.Sort(backups, (a, b) => File.GetCreationTime(a).CompareTo(File.GetCreationTime(b)));

            for (int i = 0; i < backups.Length - 10; i++)
            {
                File.Delete(backups[i]);
            }

            Debug.Log($"[AI Code Actions] Cleaned up {backups.Length - 10} old backups");
        }
    }
}

