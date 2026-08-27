using System.Text;
using System.Text.Json;

namespace WormholeWorlds.Infrastructure.Persistence;

/// <summary>
/// Temp-write, validate, backup-primary, then atomic replace.
/// </summary>
public static class AtomicJsonFileStore
{
    public static void WriteAtomically(
        string path,
        string json,
        Func<string, bool> validateJson,
        string? backupSuffix = ".bak")
    {
        string fullPath = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!validateJson(json))
        {
            throw new InvalidDataException("JSON document failed in-memory validation.");
        }

        string temporary = fullPath + ".tmp";
        File.WriteAllText(temporary, json, new UTF8Encoding(false));
        string written = File.ReadAllText(temporary);
        if (!validateJson(written))
        {
            TryDelete(temporary);
            throw new InvalidDataException("Campaign save temporary file failed validation.");
        }

        if (File.Exists(fullPath) && !string.IsNullOrWhiteSpace(backupSuffix))
        {
            string backupPath = fullPath + backupSuffix;
            File.Copy(fullPath, backupPath, overwrite: true);
        }

        File.Move(temporary, fullPath, overwrite: true);
    }

    public static string? Quarantine(string path, string label)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            string quarantinePath = path + $".{label}-{DateTime.UtcNow:yyyyMMddHHmmss}.json";
            File.Copy(path, quarantinePath, overwrite: true);
            return quarantinePath;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static string? BackupBeforeMigrate(string path, int fromVersion)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        string backupPath = path + $".pre-migrate-v{fromVersion}.json";
        File.Copy(path, backupPath, overwrite: true);
        return backupPath;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Best-effort cleanup of a failed temp write.
        }
    }
}
