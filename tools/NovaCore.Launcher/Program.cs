namespace NovaCore.Launcher;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        if (!RepositoryLocator.TryFindRoot(AppContext.BaseDirectory, out var repositoryRoot) &&
            !RepositoryLocator.TryFindRoot(Environment.CurrentDirectory, out repositoryRoot))
        {
            MessageBox.Show(
                "NovaCore.Launcher could not locate the NovaCore repository and sample project.",
                "NovaCore Launcher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        Application.Run(new MainForm(repositoryRoot));
    }
}
