namespace NovaCore.ConstructionEditor;

/// <summary>The product and diagnostic entry points host the same frontend.</summary>
public static class PlayerApplication
{
    static bool uiInitialized;
    public static void InitializeUi(){if(uiInitialized)return;ApplicationConfiguration.Initialize();uiInitialized=true;}
    public static void Run(string[] args)=>Program.Main(args);
}
