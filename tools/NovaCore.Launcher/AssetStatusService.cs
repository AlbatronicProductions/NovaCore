using System.Diagnostics;

namespace NovaCore.Launcher;

public enum LauncherAssetState
{
    Ready,
    Missing,
    StatusUnavailable
}

public sealed record LauncherAssetStatus(string AssetId, LauncherAssetState State, string Detail);

public static class AssetStatusService
{
    public static async Task<LauncherAssetStatus> QueryAsync(
        string repositoryRoot,
        string assetId,
        CancellationToken cancellationToken)
    {
        var executable = Path.Combine(repositoryRoot, "tools", "NovaCore.AssetTool", "bin", "Debug", "net10.0", "NovaCore.AssetTool.exe");
        if (!File.Exists(executable))
        {
            return new(assetId, LauncherAssetState.StatusUnavailable,
                "Build NovaCore.AssetTool to query the existing asset authority.");
        }

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    WorkingDirectory = repositoryRoot,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            process.StartInfo.ArgumentList.Add("status");
            process.StartInfo.ArgumentList.Add(assetId);
            if (!process.Start())
            {
                return new(assetId, LauncherAssetState.StatusUnavailable, "Asset status process did not start.");
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var output = (await outputTask).Trim();
            var error = (await errorTask).Trim();
            return process.ExitCode == 0
                ? new(assetId, LauncherAssetState.Ready, LastUsefulLine(output, "Ready"))
                : new(assetId, LauncherAssetState.Missing, LastUsefulLine(error.Length > 0 ? error : output, "Missing"));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return new(assetId, LauncherAssetState.StatusUnavailable, exception.Message);
        }
    }

    private static string LastUsefulLine(string text, string fallback) =>
        text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? fallback;
}
