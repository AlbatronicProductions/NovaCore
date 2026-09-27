using System.Text.Json;
using NovaCore.Core;

namespace NovaCore.ConstructionEditor;
internal sealed unsafe partial class DesktopEditorForm
{
    private readonly List<string> connectorTanks=new();
    private string? connectorHost;
    // Opt-in native-window engineering driver; never manual Player PASS.
    private void QualifyConnectorFrame()
    {
        switch(applicationStep++){
            case 0: ClickButton("New vehicle");ChooseCard("nc.core.command-2");Hover(Double3.Zero);break;
            case 1: RequireQualification(session.Preview is not null,"root ghost");WindowClick(Double3.Zero);break;
            case 2: RequireQualification(session.Current!.Design.Parts.Length==1,"root committed");connectorHost="core";count.SelectedItem=8;ChooseCard("nc.tank.short-2");camera.Pitch=.2;SocketHover(connectorHost,"aft");break;
            case 3: case 5: case 7: ExpectPreview(2+connectorTanks.Count);SocketClick(connectorHost!,"aft");break;
            case 4: case 6: case 8:
                ExpectCommit(2+connectorTanks.Count);
                connectorHost=session.Current!.Design.Data.Instances.Single(p=>p.Definition.Id=="nc.tank.short-2"&&!connectorTanks.Contains(p.Id)).Id;connectorTanks.Add(connectorHost);
                FocusCraft();camera.Pitch=.2;
                ChooseCard(connectorTanks.Count==3?"nc.mount.single-2to1":"nc.tank.short-2");SocketHover(connectorHost,"aft");break;
            case 9: ExpectPreview(5);SocketClick(connectorHost!,"aft");break;
            case 10: ExpectCommit(5);connectorHost=session.Current!.Design.Data.Instances.Single(p=>p.Definition.Id=="nc.mount.single-2to1").Id;ChooseCard("nc.engine.main-1");SocketHover(connectorHost,"engine");break;
            case 11: ExpectPreview(6);SocketClick(connectorHost!,"engine");break;
            case 12: ExpectCommit(6);qualificationSaved=session.Save(session.Revision);ClickButton("Undo");ClickButton("Redo");RequireQualification(session.Save(session.Revision).SequenceEqual(qualificationSaved),"nested engine undo/redo exact");ChooseCard("nc.rcs.block-r1");FocusCraft();camera.Pitch=0;SocketHover(connectorTanks[0],"radial-1");break;
            case 13: case 15: case 17:
                var index=(applicationStep-14)/2;
                RequireQualification(activeSocketParent==connectorTanks[index],"radial preview targets correct host");
                ExpectPreview(6+8*(index+1));VerifyRenderedPreview();SocketClick(connectorTanks[index],"radial-1");break;
            case 14: case 16: case 18:
                var completed=(applicationStep-13)/2;ExpectCommit(6+8*completed);
                qualificationSaved=session.Save(session.Revision);ClickButton("Undo");ClickButton("Redo");RequireQualification(session.Save(session.Revision).SequenceEqual(qualificationSaved),"nested radial group undo/redo exact");
                if(completed<3){ChooseCard("nc.rcs.block-r1");SocketHover(connectorTanks[completed],"radial-1");}break;
            default:
                File.WriteAllText(qualificationPath!,JsonSerializer.Serialize(new{judgment="CONNECTOR_RECIPE_PASS",completeStockRecipe="PASS_THREE_TANKS_24_RCS_ADAPTER_ENGINE",playerPass=false,checks=qualificationChecks,frames=applicationFrames,parts=session.Current!.Design.Parts.Length,source=session.Current.Design.Digest,native=NativeRuntimeLibraryIdentity()},new JsonSerializerOptions{WriteIndented=true}));
                approvedClose=true;native->Stop=1;break;
        }
    }
}
