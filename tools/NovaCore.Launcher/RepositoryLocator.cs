namespace NovaCore.Launcher;

public static class RepositoryLocator
{
    private static readonly string SampleProjectRelativePath = Path.Combine(
        "samples", "NovaCore.Triangle", "NovaCore.Triangle.csproj");

    public static bool TryFindRoot(string startDirectory, out string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startDirectory);
        var current = new DirectoryInfo(Path.GetFullPath(startDirectory));
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")) &&
                File.Exists(Path.Combine(current.FullName, SampleProjectRelativePath)))
            {
                repositoryRoot = current.FullName;
                return true;
            }

            current = current.Parent;
        }

        repositoryRoot = string.Empty;
        return false;
    }
}
