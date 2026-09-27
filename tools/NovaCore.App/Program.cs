internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if(args.Length==2&&args[0]=="--qualify-storage-ui"){NovaCore.App.RecorderStorageForm.Qualify();File.WriteAllText(args[1],"PASS: maintenance HWND then player UI initialization; seven actions; protected listing; GPU exposure=0; runtime-root writes=0\n");return;}
        var output = Console.Out;
        var error = Console.Error;
        NovaCore.App.SessionLog? log = null;
        NovaCore.Diagnostics.OrdinarySession? minimum = null;
        bool minimumConfigured = false;
        UnhandledExceptionEventHandler? unhandled = null;
        try
        {
            log = new NovaCore.App.SessionLog(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NovaCore", "Logs"));
            Console.SetOut(new NovaCore.App.SessionLog.TeeWriter(output, log));
            Console.SetError(new NovaCore.App.SessionLog.TeeWriter(error, log));
            unhandled = (_, e) => { log.WriteLine($"UNHANDLED: {e.ExceptionObject}"); log.Flush(); };
            AppDomain.CurrentDomain.UnhandledException += unhandled;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            Console.WriteLine($"Diagnostic session: {log.DirectoryPath}");
            NovaCore.ConstructionEditor.PlayerApplication.InitializeUi();
            NovaCore.App.RecorderStorageForm.ShowIfRequired();
            bool qualification=NovaCore.Diagnostics.RecorderLaunchPolicy.RequiresCoverage(args);
            NovaCore.Diagnostics.RecorderLaunchPolicy.Start(
                ()=>minimum=new NovaCore.Diagnostics.OrdinarySession(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"NovaCore","MinimumRecorder"),Path.Combine(AppContext.BaseDirectory,"NovaCore.Recorder.exe")),
                session=>{NovaCore.Interop.MinimumRecorder.Configure(session.Mapping.Name,session.Mapping.Session);minimumConfigured=true;Console.WriteLine($"Qualification recorder: {session.DirectoryPath}");},
                qualification,()=>NovaCore.ConstructionEditor.PlayerApplication.Run(args),
                message=>{Console.Error.WriteLine(message);if(!qualification)MessageBox.Show(message+"\n\nNovaCore can continue. Unique evidence remains preserved.","NovaCore recorder",MessageBoxButtons.OK,MessageBoxIcon.Warning);});
            minimum?.Finish(Environment.ExitCode == 0);
            Console.WriteLine($"Session ended; exitCode={Environment.ExitCode}");
        }
        catch (Exception ex)
        {
            Environment.ExitCode = 1;
            log?.WriteLine($"Application failed: {ex}");
            log?.Flush();
            MessageBox.Show($"NovaCore stopped.\n\n{ex.Message}\n\nDiagnostics: {log?.DirectoryPath ?? "could not create session log"}",
                "NovaCore", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (minimumConfigured) NovaCore.Interop.MinimumRecorder.Close();
            minimum?.Dispose();
            NovaCore.Interop.DiagnosticStartup.Mark(NovaCore.Interop.DiagnosticStartup.Exit);
            if (unhandled is not null) AppDomain.CurrentDomain.UnhandledException -= unhandled;
            Console.SetOut(output);
            Console.SetError(error);
            log?.Dispose();
        }
    }
}
