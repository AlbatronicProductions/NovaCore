using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Win32.SafeHandles;

namespace NovaCore.Graphics;

/// <summary>A reviewed publication temporary; the complete content-addressed object is always retained.</summary>
public sealed record TerrainCacheCleanupCandidate(string Path, string FinalPath, long Bytes,
    string Sha256, DateTime LastWriteUtc, int OwnerProcessId, string ContentSha256);

public sealed record TerrainCacheCleanupReport(IReadOnlyList<TerrainCacheCleanupCandidate> Candidates,
    IReadOnlyList<string> Retained)
{
    public long ReclaimableBytes => Candidates.Sum(candidate => candidate.Bytes);
}

/// <summary>
/// Explicit maintenance only. Never prunes final assets or scans outside the SHA-256 namespace.
/// Unknown owners and missing/corrupt final copies are retained, irrespective of age.
/// </summary>
public static class TerrainCacheCleanup
{
    private static readonly Regex PublicationName = new(
        @"^([0-9a-f]{64})\.nccube\.incomplete-([1-9][0-9]*)-([0-9a-f]{32})$",
        RegexOptions.CultureInvariant);

    public static TerrainCacheCleanupReport Inspect(string cacheRoot, TimeSpan minimumAge)
    {
        if (minimumAge < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(minimumAge));
        var candidates = new List<TerrainCacheCleanupCandidate>();
        var retained = new List<string>();
        var root = System.IO.Path.GetFullPath(cacheRoot);
        if (!Directory.Exists(root)) return new(candidates, retained);
        try
        {
            RequirePlainPath(root);
            var shaRoot = System.IO.Path.Combine(root, "sha256");
            if (!Directory.Exists(shaRoot)) return new(candidates, retained);
            RequirePlainPath(shaRoot);
            foreach (var prefix in Directory.EnumerateDirectories(shaRoot).Order(StringComparer.Ordinal))
            {
                var name = System.IO.Path.GetFileName(prefix);
                if (name.Length != 2 || !name.All(IsHex)) continue;
                try
                {
                    RequirePlainPath(prefix);
                    foreach (var path in Directory.EnumerateFiles(prefix).Order(StringComparer.Ordinal))
                    {
                        if (!System.IO.Path.GetFileName(path).Contains(".incomplete", StringComparison.Ordinal)) continue;
                        try
                        {
                            var match = PublicationName.Match(System.IO.Path.GetFileName(path));
                            if (!match.Success || !match.Groups[1].Value.StartsWith(name, StringComparison.Ordinal) ||
                                !int.TryParse(match.Groups[2].Value, out var owner))
                                throw new IOException("unrecognized publication identity/owner");
                            RequirePlainPath(path);
                            var info = new FileInfo(path);
                            if ((info.Attributes & FileAttributes.ReadOnly) != 0) throw new IOException("read-only temporary");
                            if (OwnerMayBeAlive(owner)) throw new IOException("owner is active or cannot be ruled out");
                            if (info.LastWriteTimeUtc > DateTime.UtcNow - minimumAge) throw new IOException("minimum age not reached");
                            var final = System.IO.Path.Combine(prefix, match.Groups[1].Value + ".nccube");
                            RequirePlainPath(final);
                            // A stable complete duplicate is the recovery authority for this cleanup.
                            using var complete = OpenRead(final);
                            if (Hash(complete) != match.Groups[1].Value) throw new IOException("final object hash mismatch");
                            using var temporary = OpenRead(path);
                            candidates.Add(new(path, final, temporary.Length, Hash(temporary), info.LastWriteTimeUtc, owner, match.Groups[1].Value));
                        }
                        catch (Exception exception) when (ExpectedFailure(exception)) { retained.Add($"{path}: {exception.Message}"); }
                    }
                }
                catch (Exception exception) when (ExpectedFailure(exception)) { retained.Add($"{prefix}: {exception.Message}"); }
            }
        }
        catch (Exception exception) when (ExpectedFailure(exception)) { retained.Add($"{root}: {exception.Message}"); }
        return new(candidates, retained);
    }

    /// <summary>Applies only a previously reported set, rechecking every file. No rescan expands deletion.</summary>
    public static bool TryRemove(string cacheRoot, TerrainCacheCleanupCandidate candidate, out string reason)
    {
        reason = string.Empty;
        if (!OperatingSystem.IsWindows()) { reason = "Atomic handle deletion is supported only on Windows."; return false; }
        try
        {
            var root = System.IO.Path.GetFullPath(cacheRoot);
            var match = PublicationName.Match(System.IO.Path.GetFileName(candidate.Path));
            if (!match.Success || !int.TryParse(match.Groups[2].Value, out var owner) || owner != candidate.OwnerProcessId ||
                match.Groups[1].Value != candidate.ContentSha256) throw new IOException("candidate identity changed");
            var expectedFinal = System.IO.Path.Combine(root, "sha256", candidate.ContentSha256[..2], candidate.ContentSha256 + ".nccube");
            if (!System.IO.Path.GetFullPath(candidate.FinalPath).Equals(expectedFinal, StringComparison.OrdinalIgnoreCase) ||
                !System.IO.Path.GetFullPath(candidate.Path).Equals(expectedFinal + ".incomplete-" + match.Groups[2].Value + "-" + match.Groups[3].Value, StringComparison.OrdinalIgnoreCase))
                throw new IOException("candidate is outside the exact cache namespace");
            RequirePlainPath(candidate.FinalPath); RequirePlainPath(candidate.Path);
            if (OwnerMayBeAlive(owner)) throw new IOException("owner is active or cannot be ruled out");
            var info = new FileInfo(candidate.Path);
            if (info.Length != candidate.Bytes || info.LastWriteTimeUtc != candidate.LastWriteUtc ||
                (info.Attributes & FileAttributes.ReadOnly) != 0) throw new IOException("temporary changed since report");
            // Keep the recovery object open without delete sharing throughout removal.
            using var complete = OpenRead(candidate.FinalPath);
            if (Hash(complete) != candidate.ContentSha256) throw new IOException("recovery object changed since report");
            // Exclusive handle includes DELETE access; delete this verified file by handle, not by a re-resolved path.
            using var handle = CreateFileW(candidate.Path, 0x80000000u | 0x00010000u, 0, IntPtr.Zero, 3, 0x00200000u, IntPtr.Zero);
            if (handle.IsInvalid) throw new IOException($"temporary unavailable/in use ({Marshal.GetLastWin32Error()})");
            using var temporary = new FileStream(handle, FileAccess.Read);
            if (temporary.Length != candidate.Bytes || Hash(temporary) != candidate.Sha256) throw new IOException("temporary bytes changed since report");
            // OPEN_REPARSE_POINT plus handle attributes prevents a substituted link from being followed.
            if (!GetFileInformationByHandle(handle, out var information) || (information.Attributes & 0x400u) != 0)
                throw new IOException("temporary handle is not a plain file");
            var disposition = 1;
            if (!SetFileInformationByHandle(handle, 4, ref disposition, sizeof(int)))
                throw new IOException($"temporary cannot be deleted ({Marshal.GetLastWin32Error()})");
            return true;
        }
        catch (Exception exception) when (ExpectedFailure(exception)) { reason = exception.Message; return false; }
    }

    private static FileStream OpenRead(string path) => new(path, FileMode.Open, FileAccess.Read, FileShare.Read,
        TerrainAssetCache.VerificationBufferBytes, FileOptions.SequentialScan);
    private static string Hash(Stream stream) => Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    private static bool IsHex(char value) => value is >= '0' and <= '9' or >= 'a' and <= 'f';
    private static bool ExpectedFailure(Exception exception) => exception is IOException or UnauthorizedAccessException or ArgumentException or System.ComponentModel.Win32Exception;
    private static bool OwnerMayBeAlive(int id)
    {
        try { using var process = Process.GetProcessById(id); return !process.HasExited; }
        catch (ArgumentException) { return false; }
        catch { return true; } // PID reuse and inaccessible owners cause retention, never deletion.
    }
    private static void RequirePlainPath(string path)
    {
        for (var current = System.IO.Path.GetFullPath(path); !string.IsNullOrEmpty(current); current = System.IO.Path.GetDirectoryName(current))
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new IOException("reparse-point path is excluded");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FileInformation
    {
        internal uint Attributes;
        internal System.Runtime.InteropServices.ComTypes.FILETIME Creation, Access, Write;
        internal uint Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
    }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetFileInformationByHandle(SafeFileHandle handle, int kind, ref int information, int bytes);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle handle, out FileInformation information);
}
