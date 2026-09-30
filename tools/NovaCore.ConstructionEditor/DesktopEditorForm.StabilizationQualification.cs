using System.Text.Json;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.ConstructionEditor;
internal sealed unsafe partial class DesktopEditorForm
{
    private readonly int[] stabilizationDepths=[1,2,1,4,16,64];
    private readonly List<object> stabilizationResults=new();
    private int stabilizationProfile,stabilizationPhase,stabilizationPlaced,stabilizationRadial,stabilizationHold;
    private string[] stabilizationHosts=[];
    private string stabilizationParent="core";
    private long stabilizationStarted;
    private void AimSocket(string host,string endpoint)
    {
        var p=session.Current!.Design.Parts.Single(p=>p.Instance.Id==host);
        camera.Target=p.Instance.Pose.Then(p.Definition.Attachments.Single(a=>a.Id==endpoint).Frame).Position;
        camera.Distance=8;camera.Pitch=.2;selection=host;SocketHover(host,endpoint);
    }
    // Actual native hover/click, production cards/transactions and renderer. The
    // dense capacity fixture bypasses repetitive clicks only, never admission.
    private void QualifyStabilizationFrame()
    {
        applicationStep++;
        if(stabilizationProfile>=stabilizationDepths.Length){
            if(stabilizationPhase==0){
                CancelGhost();freeGhost=null;editorIntent=0;selection=null;qualificationHover=null;
                session.Load(session.Revision,AssemblyJson.Write(StabilizationCraftFixture.Create(session.Catalog,100,true)),true);
                FocusCraft();camera.Pitch=.2;stabilizationPhase=1;editorMeasuring=true;stabilizationStarted=System.Diagnostics.Stopwatch.GetTimestamp();return;
            }
            RequireQualification(renderEntries.Count==4101&&renderCapacity>=renderEntries.Count&&native->Stop==0,"901-part craft continuously renders beyond retired 4096-object limit");
            stabilizationHold++;if(warmMeasurements.Count<360)return;
            File.WriteAllText(qualificationPath!,JsonSerializer.Serialize(new{judgment="STABILIZATION_NATIVE_PASS",playerPass=false,checks=qualificationChecks,frames=applicationFrames,profiles=stabilizationResults,dense=new{parts=901,renderEntries=renderEntries.Count,renderCapacity,heldFrames=stabilizationHold*8,seconds=System.Diagnostics.Stopwatch.GetElapsedTime(stabilizationStarted).TotalSeconds,warmWindows},native=NativeRuntimeLibraryIdentity()},new JsonSerializerOptions{WriteIndented=true}));
            approvedClose=true;native->Stop=1;return;
        }
        var depth=stabilizationDepths[stabilizationProfile];
        switch(stabilizationPhase){
            case 0:
                if(!editing)ClickButton("New vehicle");else{CancelGhost();session.Clear(session.Revision,true);selection=null;editorIntent=0;RefreshInspector();}
                connectorTanks.Clear();stabilizationPlaced=stabilizationRadial=0;stabilizationParent="core";count.SelectedItem=8;
                ChooseCard("nc.core.command-2");camera.Target=Double3.Zero;camera.Distance=8;Hover(Double3.Zero);stabilizationPhase=1;break;
            case 1: RequireQualification(session.Preview is not null,"fresh root preview");WindowClick(Double3.Zero);stabilizationPhase=2;break;
            case 2:
                RequireQualification(session.Current!.Design.Parts.Length==1+stabilizationPlaced,"axial chain committed");
                var next=stabilizationProfile==2||stabilizationProfile>=3&&stabilizationPlaced%2==1?"nc.tank.long-2":"nc.tank.short-2";
                ChooseCard(next);AimSocket(stabilizationParent,"aft");stabilizationPhase=3;break;
            case 3: ExpectPreview(2+stabilizationPlaced);SocketClick(stabilizationParent,"aft");stabilizationPhase=4;break;
            case 4:
                ExpectCommit(2+stabilizationPlaced);
                stabilizationParent=session.Current!.Design.Data.Instances.Single(p=>p.Definition.Id.StartsWith("nc.tank.",StringComparison.Ordinal)&&!connectorTanks.Contains(p.Id)).Id;
                connectorTanks.Add(stabilizationParent);stabilizationPlaced++;
                if(stabilizationPlaced<depth){stabilizationPhase=2;break;}
                ChooseCard("nc.mount.single-2to1");AimSocket(stabilizationParent,"aft");stabilizationPhase=5;break;
            case 5: ExpectPreview(depth+2);SocketClick(stabilizationParent,"aft");stabilizationPhase=6;break;
            case 6:
                ExpectCommit(depth+2);stabilizationParent=session.Current!.Design.Data.Instances.Single(p=>p.Definition.Id=="nc.mount.single-2to1").Id;
                ChooseCard("nc.engine.main-1");AimSocket(stabilizationParent,"engine");stabilizationPhase=7;break;
            case 7: ExpectPreview(depth+3);SocketClick(stabilizationParent,"engine");stabilizationPhase=8;break;
            case 8:
                ExpectCommit(depth+3);stabilizationHosts=new[]{connectorTanks[0],connectorTanks[depth/2],connectorTanks[^1]}.Distinct().ToArray();
                ChooseCard("nc.rcs.block-r1");AimSocket(stabilizationHosts[0],"radial-1");stabilizationPhase=9;break;
            case 9:
                RequireQualification(activeSocketParent==stabilizationHosts[stabilizationRadial],"early/middle/later host identity");ExpectPreview(depth+3+8*(stabilizationRadial+1));VerifyRenderedPreview();SocketClick(stabilizationHosts[stabilizationRadial],"radial-1");stabilizationPhase=10;break;
            case 10:
                ExpectCommit(depth+3+8*(stabilizationRadial+1));qualificationSaved=session.Save(session.Revision);ClickButton("Undo");ClickButton("Redo");RequireQualification(session.Save(session.Revision).SequenceEqual(qualificationSaved),"native nested undo/redo exact");
                if(++stabilizationRadial<stabilizationHosts.Length){ChooseCard("nc.rcs.block-r1");AimSocket(stabilizationHosts[stabilizationRadial],"radial-1");stabilizationPhase=9;break;}
                var save=Path.Combine(Path.GetDirectoryName(qualificationPath!)!,"depth-"+stabilizationProfile+".nccraft");File.WriteAllBytes(save,qualificationSaved);
                session.Load(session.Revision,File.ReadAllBytes(save),true);RequireQualification(session.Save(session.Revision).SequenceEqual(qualificationSaved),"native save/reload exact");
                stabilizationResults.Add(new{profile=stabilizationProfile,depth,parts=session.Current!.Design.Parts.Length,radialHosts=stabilizationHosts.Length,source=session.Current.Design.Digest});stabilizationProfile++;stabilizationPhase=0;qualificationHover=null;break;
        }
    }
}
