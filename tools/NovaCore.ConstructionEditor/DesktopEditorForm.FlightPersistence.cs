using NovaCore.Simulation.Transactions;

namespace NovaCore.ConstructionEditor;

internal sealed unsafe partial class DesktopEditorForm
{
    private static string FlightDirectory=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"NovaCore","Flights");
    private void SaveFlight()
    {
        var active=flight??throw new InvalidDataException("There is no active flight to save.");
        active.SuspendLive();var bytes=active.SaveFlight();Directory.CreateDirectory(FlightDirectory);
        using var dialog=new SaveFileDialog{Title="Save flight",Filter="NovaCore flight (*.ncflight.json)|*.ncflight.json",InitialDirectory=FlightDirectory,FileName="flight.ncflight.json",OverwritePrompt=true,AddExtension=true};
        if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        var target=Path.GetFullPath(dialog.FileName);var temporary=target+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllBytes(temporary,bytes);File.Move(temporary,target,true);}
        finally{if(File.Exists(temporary))File.Delete(temporary);}
        message="Flight saved.";UpdateStatus();
    }
    private void LoadFlight()
    {
        if(solar is null)throw new InvalidDataException("The game session is not ready.");
        flight?.SuspendLive();
        using var dialog=new OpenFileDialog{Title="Load flight",Filter="NovaCore flight (*.ncflight.json)|*.ncflight.json",InitialDirectory=FlightDirectory,CheckFileExists=true};
        if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        var length=new FileInfo(dialog.FileName).Length;
        if(length is <=0 or >SimulationTransactionEngine.ConstructionSaveMaximumBytes)throw new InvalidDataException("Flight save exceeds its bounded capacity.");
        // Prepare and validate the complete physical successor before swapping
        // the live scene. Refusal preserves the prior craft and editor draft.
        var next=ConstructionFlightScene.RestoreFlight(session.Catalog,File.ReadAllBytes(dialog.FileName),assetRoot,solar,visuals);
        try{next.FloridaView.Solar.RetainRendererBuffers(solar);}
        catch{next.Dispose();throw;}
        var previous=flight;solar=next.FloridaView.Solar;flight=next;compiled=next.Craft;previous?.Dispose();
        LeaveEditor();message="Flight loaded. Z ignite · X cutoff · WASD/QE attitude · F craft";UpdateStatus();
    }
}
