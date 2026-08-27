using System.Security.Cryptography;
using WormholeWorlds.Application.Updates;

namespace WormholeWorlds.Infrastructure.Updates;

public sealed class VerifiedInstallerDownloader : IInstallerDownloader
{
    private readonly HttpClient _httpClient;

    public VerifiedInstallerDownloader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> DownloadAndVerifyAsync(
        ReleaseDescriptor release,
        string destinationDirectory,
        CancellationToken cancellationToken = default)
    {
        string safeFileName = Path.GetFileName(release.InstallerFileName);
        if (!string.Equals(safeFileName, release.InstallerFileName, StringComparison.Ordinal)
            || !safeFileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The release contains an unsafe Windows installer filename.");
        }

        Directory.CreateDirectory(destinationDirectory);
        string checksums = await _httpClient.GetStringAsync(release.ChecksumsUri, cancellationToken);
        string? expected = ParseExpectedChecksum(checksums, safeFileName);
        if (expected is null)
        {
            throw new InvalidDataException("The release checksum manifest does not list the Windows installer.");
        }

        string destination = Path.Combine(destinationDirectory, safeFileName);
        string temporary = destination + ".download";
        try
        {
            await using (Stream input = await _httpClient.GetStreamAsync(release.InstallerUri, cancellationToken))
            await using (FileStream output = File.Create(temporary))
            {
                await input.CopyToAsync(output, cancellationToken);
            }

            if (release.InstallerSizeBytes > 0
                && new FileInfo(temporary).Length != release.InstallerSizeBytes)
            {
                throw new InvalidDataException("The downloaded installer size does not match the release metadata.");
            }

            string actual;
            await using (FileStream downloaded = File.OpenRead(temporary))
            {
                actual = Convert.ToHexString(await SHA256.HashDataAsync(
                    downloaded,
                    cancellationToken)).ToLowerInvariant();
            }
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The downloaded installer failed SHA-256 verification.");
            }

            File.Move(temporary, destination, overwrite: true);
            return destination;
        }
        finally
        {
            if (File.Exists(temporary))
            {
                TryDelete(temporary);
            }
        }
    }

    public static string? ParseExpectedChecksum(string manifest, string fileName)
    {
        foreach (string line in manifest.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2
                && parts[0].Length == 64
                && parts[0].All(Uri.IsHexDigit)
                && string.Equals(parts[^1].Replace('\\', '/'), fileName.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
            {
                return parts[0];
            }
        }

        return null;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Best-effort cleanup of an incomplete download.
        }
    }
}
