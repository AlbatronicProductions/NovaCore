using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.ConstructionEditor;
internal sealed unsafe partial class DesktopEditorForm
{
    private string SaveDirectory=>qualificationPath is null?Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"NovaCore","Craft"):
        Path.Combine(Path.GetDirectoryName(Path.GetFullPath(qualificationPath))!,"craft-library");
    private void ShowSaveBrowser(bool creating,Action? afterSave=null)
    {
        var panel=OpenOverlay("VEHICLES · SAVE / LOAD",570,530);
        var entries=new ListBox(){Width=515,Height=250,DisplayMember="Text",BackColor=BackColor,ForeColor=ForeColor};panel.Controls.Add(entries);
        if(Directory.Exists(SaveDirectory))foreach(var path in Directory.EnumerateFiles(SaveDirectory,"*.craft.json").Order(StringComparer.Ordinal))entries.Items.Add(new SavedEntry(path));
        var name=new TextBox(){Width=515,Text=session.Current?.Design.Data.Craft?.Name??"Development craft"};panel.Controls.Add(name);
        var actions=new FlowLayoutPanel(){Width=515,Height=80};panel.Controls.Add(actions);
        ActionButton(actions,"Save new",()=>{
            var text=name.Text.Trim();if(text.Length is 0 or >80||text.IndexOfAny(Path.GetInvalidFileNameChars())>=0||text is "." or ".."||text.EndsWith('.')||text.EndsWith(' '))throw new InvalidDataException("Choose a vehicle name without path characters.");
            var path=Path.Combine(SaveDirectory,text+".craft.json");if(File.Exists(path))throw new IOException("That vehicle already exists. Select it and choose Overwrite.");
            SaveEntry(path,afterSave);
        },160);
        ActionButton(actions,"Overwrite selected",()=>{
            if(entries.SelectedItem is not SavedEntry entry)throw new InvalidDataException("Select a saved vehicle first.");
            var confirm=OpenOverlay("Overwrite vehicle?",400,210);confirm.Controls.Add(new Label(){Text=entry.Text,Width=340,Height=48});
            ActionButton(confirm,"Overwrite",()=>SaveEntry(entry.Path,afterSave),340);ActionButton(confirm,"Cancel",()=>ShowSaveBrowser(creating,afterSave),340);
        },160);
        ActionButton(actions,"Load selected",()=>{
            if(entries.SelectedItem is not SavedEntry entry)throw new InvalidDataException("Select a saved vehicle first.");
            RequestLeave(()=>{
                session.LoadFrom(session.Revision,entry.Path,true);RememberPath(entry.Path);selection=null;refusedGhost=null;targetKey=null;freeGhost=null;editorIntent=0;
                craftName.Text=session.Current!.Design.Data.Craft!.Name;FocusCraft();RefreshInspector();CloseOverlay();message="Vehicle loaded.";
            });
        },160);
        ActionButton(panel,"Back",CloseOverlay,515);
    }
    private sealed record SavedEntry(string Path){public string Text=>System.IO.Path.GetFileName(Path)[..^11];}
    private void SaveEntry(string path,Action? afterSave)
    {
        if(session.Current is null)throw new InvalidDataException("Place a root before saving.");
        if(session.Preview is not null||editorIntent!=0)throw new InvalidDataException("Place or cancel the held part before saving.");
        Directory.CreateDirectory(SaveDirectory);session.SaveTo(session.Revision,path);RememberPath(path);CloseOverlay();message="Vehicle saved.";afterSave?.Invoke();
    }
    private void RequestLeave(Action continuation)
    {
        if(!session.HasUnsavedWork){continuation();return;}
        var panel=OpenOverlay("Unsaved construction",400,290);
        panel.Controls.Add(new Label(){Text="Keep your current changes before continuing?",Width=340,Height=50});
        ActionButton(panel,"Save and continue",()=>ShowSaveBrowser(true,continuation),340);
        ActionButton(panel,"Discard changes",()=>{CloseOverlay();continuation();},340);
        ActionButton(panel,"Cancel",CloseOverlay,340);
    }
}
