using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Eclipse.Helpers
{
    /// <summary>
    /// Reads and writes one JSON settings file in a way that survives being interrupted.
    ///
    /// Both of Eclipse's settings files used to be written with <c>File.WriteAllText</c>, which
    /// truncates the target and then rewrites it - so a crash, a power cut or a full disk
    /// part-way through left a file that would not parse. Reading had no error handling at all,
    /// and the read is reached from field initialisers and constructors, so that file then threw
    /// from somewhere with no connection to the cause. Backups were written on every save, kept
    /// forever, and never once read.
    ///
    /// Writes now land on a temp file and replace the target in one step. Reads fall back to the
    /// newest backup that parses, then to defaults, and never throw.
    /// </summary>
    public static class JsonFileStore
    {
        /// <summary>
        /// How many timestamped backups to keep per file. They used to accumulate forever - one
        /// per save, for the life of the installation.
        /// </summary>
        public const int BackupsToKeep = 10;

        /// <summary>
        /// Writes <paramref name="value"/> to <paramref name="filePath"/>, backing up whatever
        /// was there first. The target is either the old contents or the new contents; it is
        /// never a half-written file.
        /// </summary>
        public static void Write<T>(string filePath, T value, string backupPrefix)
        {
            DirectoryInfoHelper.CreateDirectoryIfNotExists(filePath);

            Backup(filePath, backupPrefix);

            string json = JsonConvert.SerializeObject(value, Formatting.Indented);

            // The temp file has to sit on the same volume as the target for the swap to be
            // atomic, so it goes in the same directory rather than in %TEMP%.
            string tempPath = filePath + ".tmp";
            File.WriteAllText(tempPath, json);

            ReplaceFile(tempPath, filePath);

            PruneBackups(backupPrefix);
        }

        /// <summary>
        /// The file's contents; or the newest backup that parses, if it does not; or
        /// <paramref name="fallback"/>, if none of them do. Never throws.
        ///
        /// A file that is simply absent goes straight to <paramref name="fallback"/> - that is a
        /// first run, not a loss, and silently resurrecting a deleted file from a backup would
        /// be a surprise.
        /// </summary>
        public static T Read<T>(string filePath, string backupPrefix, Func<T> fallback)
        {
            if (!File.Exists(filePath))
            {
                return fallback();
            }

            T value;
            if (TryRead(filePath, out value))
            {
                return value;
            }

            LogHelper.Log($"{filePath} could not be read - looking for a usable backup");

            foreach (string backupPath in BackupsNewestFirst(backupPrefix))
            {
                if (TryRead(backupPath, out value))
                {
                    LogHelper.Log($"Recovered from backup {backupPath}");
                    return value;
                }
            }

            LogHelper.Log($"No usable backup for {filePath} - falling back to defaults");
            return fallback();
        }

        private static bool TryRead<T>(string path, out T value)
        {
            value = default(T);

            try
            {
                string json = File.ReadAllText(path);
                value = JsonConvert.DeserializeObject<T>(json);

                // "null" is valid JSON and parses without throwing, but it is not a settings file
                return value != null;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, $"read {path}");
                return false;
            }
        }

        private static void ReplaceFile(string tempPath, string filePath)
        {
            if (!File.Exists(filePath))
            {
                File.Move(tempPath, filePath);
                return;
            }

            try
            {
                File.Replace(tempPath, filePath, null);
            }
            catch (Exception ex) when (ex is PlatformNotSupportedException || ex is IOException)
            {
                // Replace is not supported on every file system - some network shares and
                // non-NTFS volumes among them. Delete-then-move is not atomic, but it is the
                // best available there, and it is still a smaller window than rewriting in place.
                LogHelper.LogException(ex, $"atomically replace {filePath} - falling back to move");

                File.Delete(filePath);
                File.Move(tempPath, filePath);
            }
        }

        private static void Backup(string filePath, string backupPrefix)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            try
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupPath = Path.Combine(DirectoryInfoHelper.Instance.SettingsBackupPath,
                                                 $"{backupPrefix}{timestamp}.json");

                // Two saves inside the same second would collide. One backup for that second is
                // enough - the previous copy is the same file either way.
                if (File.Exists(backupPath))
                {
                    return;
                }

                DirectoryInfoHelper.CreateDirectoryIfNotExists(backupPath);
                File.Copy(filePath, backupPath);
            }
            catch (Exception ex)
            {
                // A backup that cannot be taken must not stop the save. The write below is
                // atomic, so the user is not risking the file by continuing.
                LogHelper.LogException(ex, $"back up {filePath}");
            }
        }

        private static void PruneBackups(string backupPrefix)
        {
            try
            {
                foreach (string stale in BackupsNewestFirst(backupPrefix).Skip(BackupsToKeep))
                {
                    File.Delete(stale);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, $"prune {backupPrefix} backups");
            }
        }

        /// <summary>
        /// Ordered by write time, not by name. The original timestamp format used a single-digit
        /// hour, so "…_9_05_00" sorts after "…_10_05_00" - and old backups in that format are
        /// still on disk in existing installations.
        /// </summary>
        private static IEnumerable<string> BackupsNewestFirst(string backupPrefix)
        {
            string backupFolder = DirectoryInfoHelper.Instance.SettingsBackupPath;

            if (!Directory.Exists(backupFolder))
            {
                return Enumerable.Empty<string>();
            }

            try
            {
                return Directory.GetFiles(backupFolder, $"{backupPrefix}*.json")
                                .OrderByDescending(path => File.GetLastWriteTimeUtc(path))
                                .ToList();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, $"list {backupPrefix} backups");
                return Enumerable.Empty<string>();
            }
        }
    }
}
