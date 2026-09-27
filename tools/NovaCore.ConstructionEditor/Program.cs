using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.ConstructionEditor;
internal static class Program
{
    [STAThread]
    internal static void Main(string[] args)
    {
        if(NovaCore.Launcher.RepositoryLocator.TryFindRoot(AppContext.BaseDirectory,out var repository))Environment.CurrentDirectory=repository;
        if(args.FirstOrDefault()=="--diagnostic-browser"){DiagnosticBrowser.Run(args.Skip(1).ToArray());return;}
        string? catalog=null,root=null,qualification=null;var tankDefinition="nc.tank.short-2";
        for(var i=0;i<args.Length;i++){
            if(i+1>=args.Length)throw new ArgumentException("Expected a path after editor option.");
            switch(args[i]){case "--qualify-recorder":qualification=args[++i];tankDefinition="minimum-recorder";break;case "--catalog":catalog=args[++i];break;case "--asset-root":root=args[++i];break;case "--qualify-editor":qualification=args[++i];break;case "--qualify-launch":qualification=args[++i];break;case "--qualify-persistence":qualification=args[++i];break;case "--qualification-tank":tankDefinition=args[++i];break;default:throw new ArgumentException("Unknown editor option.");}
        }
        root??=Path.Combine(AppContext.BaseDirectory,"assets","vehicles","modular-starter");catalog??=Path.Combine(root,"catalog.json");
        PlayerApplication.InitializeUi();
        try{Application.Run(new DesktopEditorForm(AssemblyDefinitionCatalog.Load(File.ReadAllBytes(catalog)),root,qualification,tankDefinition));}
        catch(Exception e){Environment.ExitCode=1;Console.Error.WriteLine(e);if(qualification is null)MessageBox.Show(e.Message,"NovaCore construction could not start",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    }
}
