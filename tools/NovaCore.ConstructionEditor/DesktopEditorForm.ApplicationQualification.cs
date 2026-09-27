using NovaCore.Interop;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using System.Text.Json;

namespace NovaCore.ConstructionEditor;
internal sealed unsafe partial class DesktopEditorForm
{
    private string? expectedPreview;
    private bool RcsScalabilityQualification=>qualificationTankDefinition is "rcs64" or "rcs96";
    private string InitialQualificationTank=>qualificationTankDefinition=="deep"||RcsScalabilityQualification?"nc.tank.short-2":qualificationTankDefinition;
    private int padWarmPhase;
    private long padWarmUntil;
    private readonly HashSet<string> padObservedRoutes=new();
    private nint padCasterIdentity;
    private NovaCore.Interop.NativeFacilityCasterDefinition padCasterDefinition;
    private long retainedEpoch;
    private ConstructionFlightScene? retainedFlight;
    private int applicationStep,applicationFrames;
    private string? applicationSave;
    private Point? qualificationHover;
    private int hoverWaitFrames;
    private long poweredStart;
    private int lastLoggedStep=-1;
    private long applicationHandle,childHandle;
    private string? qualificationName;
    private void QualifyApplicationFrame(in NativeInputState input)
    {
        if(qualificationPath is null)return;
        if(SurfaceRetryQualification){SurfaceRetryBegin();return;}
        if(RecorderQualification){RecorderBeginFrame();return;}
        if(qualificationHover is {} aim&&(native->PointerX!=aim.X||native->PointerY!=aim.Y)){
            ViewportMessage(0x200,0,aim.X,aim.Y);
            if(++hoverWaitFrames>600){FailQualification(new InvalidDataException("Host pointer did not settle at the requested hover."));native->Stop=1;}
            return;
        }
        hoverWaitFrames=0;
        if(++applicationFrames%8!=0)return;
        qualificationStep=applicationStep;
        try{
            if(applicationStep!=lastLoggedStep){lastLoggedStep=applicationStep;Console.WriteLine($"APPLICATION_QUALIFY step={applicationStep} frame={applicationFrames} parts={session.Current?.Design.Parts.Length??0}");}
            if(qualificationTankDefinition=="connector-graph"){QualifyConnectorFrame();return;}
            if(qualificationTankDefinition=="stabilization"){QualifyStabilizationFrame();return;}
            switch(applicationStep++){
                case 0:
                    if(padWarmPhase==0){PreparePadDaylight();RequireQualification(solar!.TryStartAtFloridaLaunchSite(flightCamera!),"preconstruction Florida navigation");padWarmPhase=1;padWarmUntil=System.Diagnostics.Stopwatch.GetTimestamp()+20*System.Diagnostics.Stopwatch.Frequency;applicationStep--;break;}
                    if(System.Diagnostics.Stopwatch.GetTimestamp()<padWarmUntil){applicationStep--;break;}
                    if(padWarmPhase==1){ClickButton("New vehicle");padWarmPhase=2;padWarmUntil=System.Diagnostics.Stopwatch.GetTimestamp()+System.Diagnostics.Stopwatch.Frequency;applicationStep--;break;}
                    if(padWarmPhase==2){ClickButton("Return to flight");padWarmPhase=3;padWarmUntil=System.Diagnostics.Stopwatch.GetTimestamp()+System.Diagnostics.Stopwatch.Frequency;applicationStep--;break;}
                    RequireQualification(padObservedRoutes.Contains("game")&&padObservedRoutes.Contains("editor")&&padObservedRoutes.Contains("game-return"),"editor return without launch restores canonical pad/caster");
                    applicationHandle=Handle.ToInt64();childHandle=GetWindow(viewport.Handle,5).ToInt64();RequireQualification(childHandle!=0&&!editing&&solar is not null,"one native renderer begins in the Solar game");ClickButton("New vehicle");ChooseCard("nc.core.command-2");Hover(Double3.Zero);break;
                case 1: RequireQualification(session.Current is null&&session.Preview is not null,"card creates uncommitted root ghost");qualificationTarget=session.Preview!.Design.Parts[0].Instance.Pose.Position;Hover(new(.25,0,0));break;
                case 2: RequireQualification((session.Preview!.Design.Parts[0].Instance.Pose.Position-qualificationTarget).LengthSquared>.01,"held root follows pointer: "+session.Preview.Design.Parts[0].Instance.Pose.Position+" from "+qualificationTarget);WindowClick(Double3.Zero);break;
                case 3: RequireQualification(session.Current?.Design.Parts.Length==1&&mode.SelectedIndex==0,"root click commits");ChooseCard(InitialQualificationTank);SocketHover("core","aft");break;
                case 4: ExpectPreview(2);SocketClick("core","aft");break;
                case 5: ExpectCommit(2);qualificationTank=session.Current!.Design.Data.Instances.Single(p=>p.Definition.Id==InitialQualificationTank).Id;ChooseCard("nc.mount.single-2to1");camera.Pitch=-.4;SocketHover(qualificationTank,"aft");break;
                case 6: ExpectPreview(3);SocketClick(qualificationTank!,"aft");break;
                case 7: ExpectCommit(3);qualificationAdapter=session.Current!.Design.Data.Instances.Single(p=>p.Definition.Id=="nc.mount.single-2to1").Id;ChooseCard("nc.engine.main-1");camera.Pitch=-1.4;SocketHover(qualificationAdapter,"engine");break;
                case 8: ExpectPreview(4);SocketClick(qualificationAdapter!,"engine");break;
                case 9: ExpectCommit(4);FocusCraft();camera.Pitch=-.2;ChooseCard("nc.rcs.block-r1");count.SelectedItem=8;SocketHover(qualificationTank!,"radial-1");break;
                case 10: ExpectPreview(12);RequireQualification(session.Preview!.Design.Data.Symmetry.Single().Members.Length==8,"generic eight-member ghost");VerifyRenderedPreview();SocketClick(qualificationTank!,"radial-1");break;
                case 11: ExpectCommit(12);qualificationSaved=session.Save(session.Revision);ClickButton("Undo");RequireQualification(session.Current!.Design.Parts.Length==4,"one undo removes group");ClickButton("Redo");RequireQualification(session.Save(session.Revision).SequenceEqual(qualificationSaved),"exact atomic redo");ClickButton("Undo");ChooseCard("nc.rcs.block-r1");count.SelectedItem=1;SocketClick(qualificationTank!,"radial-0");break;
                case 12: RequireQualification(session.Current!.Design.Parts.Length==5,"single blocker committed");qualificationSaved=session.Save(session.Revision);ChooseCard("nc.rcs.block-r1");count.SelectedItem=8;SocketHover(qualificationTank!,"radial-1");break;
                case 13: RequireQualification(session.Preview is null&&refusedGhost is not null&&session.Save(session.Revision).SequenceEqual(qualificationSaved!),"one invalid member refuses complete eight without source mutation");ClickButton("Bin");ClickButton("Undo");ChooseCard("nc.rcs.block-r1");SocketClick(qualificationTank!,"radial-1");break;
                case 14: RequireQualification(session.Current!.Design.Parts.Length==12,"eight restored");if(qualificationTankDefinition=="deep"){
                    session.Load(session.Revision,AssemblyJson.Write(StabilizationCraftFixture.Create(session.Catalog,2,shortOnly:true)),true);
                    // Retain the previous two-tank/one-ring preservation witness.
                    session.Remove(session.Revision,"r1-0");qualificationTank="t0-0";qualificationAdapter="adapter-0";
                    RequireQualification(session.Current!.Design.Parts.Length==13,"two-short-tank flight witness admitted structurally");
                }
                if(RcsScalabilityQualification){
                    var large=qualificationTankDefinition=="rcs96";
                    session.Load(session.Revision,AssemblyJson.Write(StabilizationCraftFixture.Create(session.Catalog,large?3:2,shortOnly:true,shortTankDefinition:large?"nc.test.tank.short-half":"nc.tank.short-2")),true);
                    qualificationTank="t0-0";qualificationAdapter="adapter-0";
                    RequireQualification(session.Current!.Design.Parts.Length==(large?30:21),"complete multi-ring construction witness");
                }
                var emptySource=session.Save(session.Revision);var emptyRevision=session.Revision;var emptyHistory=session.UndoCount;
                ClickButton("Launch vehicle");
                RequireQualification(editing&&flight is null&&overlay is not null&&diagnostic!.Contains("Propellant is unavailable",StringComparison.Ordinal)&&diagnostic.Contains("Electrical power is unavailable",StringComparison.Ordinal),"fresh empty craft refuses with both actual service blockers");
                RequireQualification(session.Save(session.Revision).SequenceEqual(emptySource)&&session.Revision==emptyRevision&&session.UndoCount==emptyHistory,"refused fresh launch preserves exact draft and history");
                ClickButton("Back to construction");ClickButton("Fill consumables");
                RequireQualification(diagnostic is null&&message.StartsWith("Consumables filled",StringComparison.Ordinal),"fill clears stale refusal and reports preparation");
                compiled=CraftCompiler.Compile(session.Catalog,session.Current!.Design.Data,assetRoot);RequireQualification(compiled.Function&&compiled.AdmissionDiagnostics.IsEmpty,"exact source compiles and admits");qualificationSaved=session.Save(session.Revision);ClickButton("Save / Load");break;
                case 15: applicationSave="Application-"+qualificationTankDefinition+"-"+Guid.NewGuid().ToString("N")[..8];overlay!.Controls.OfType<TextBox>().Single().Text=applicationSave;ClickButton("Save new");RequireQualification(!session.Dirty&&File.ReadAllBytes(savePath!).SequenceEqual(qualificationSaved!),"in-product save exact bytes");ClickButton("New vehicle");RequireQualification(session.Current is null,"new draft");ClickButton("Save / Load");break;
                case 16: var list=overlay!.Controls.OfType<ListBox>().Single();list.SelectedItem=list.Items.Cast<SavedEntry>().Single(x=>x.Text==applicationSave);ClickButton("Load selected");RequireQualification(session.Save(session.Revision).SequenceEqual(qualificationSaved!),"in-product reload exact craft");qualificationSaved=session.Save(session.Revision);launchedDigest=session.Current!.Design.Digest;launchedRevision=session.Revision;launchedDirty=session.Dirty;ClickButton("Launch vehicle");break;
                case 17: RequireQualification(flight is not null&&!editing&&flight.Craft.Design.Digest==launchedDigest,"same-application exact craft launch");retainedFlight=flight;ViewportMessage(0x201,1);ViewportMessage(0x202);break;
                case 18: if(padWarmPhase==3){padWarmPhase=4;padWarmUntil=System.Diagnostics.Stopwatch.GetTimestamp()+20*System.Diagnostics.Stopwatch.Frequency;}if(System.Diagnostics.Stopwatch.GetTimestamp()<padWarmUntil){applicationStep--;break;}if(!flight!.Measurements.PhaseComplete(0)){applicationStep--;break;}RequireQualification(status.Text.Contains("SUPPORTED",StringComparison.Ordinal),"supported feedback remains visible inside the product");poweredStart=flight.State.Epoch.Ticks;PostFlightKey('Z');break;
                case 19: RequireQualification(flight!.Actuation.Main&&!flight.Failed,"hosted Z ignition");if(!flight.Measurements.PhaseComplete(1)||flight.State.Epoch.Ticks-poweredStart<(RcsScalabilityQualification?16_000_000:8_000_000)){applicationStep--;break;}PostFlightKey('W',false);break;
                case 20: RequireQualification(flight!.PlayerInput.Observation.Requested!=default,"hosted physical attitude request");ViewportMessage(0x101,'W');PostFlightKey('X');if(RcsScalabilityQualification)PostFlightKey('Q',false);break;
                case 21: RequireQualification(!flight!.Actuation.Main&&!flight.Failed,"hosted X cutoff");if(RcsScalabilityQualification)RequireQualification(!flight.Actuation.Jets.IsEmpty&&Enumerable.Range(32,flight.Actuation.Jets.Length-32).Any(flight.Actuation.Jets.Contains),"high-index RCS realized through native flight");if(!flight.Measurements.PhaseComplete(2)){applicationStep--;break;}if(RcsScalabilityQualification)ViewportMessage(0x101,'Q');RequireQualification(status.Text.Contains("COAST",StringComparison.Ordinal)&&status.Text.Contains("main OFF",StringComparison.Ordinal),"coast and cutoff feedback remain visible without a title bar");ClickButton("Menu");ClickButton("New vehicle / retained design");retainedEpoch=flight.State.Epoch.Ticks;break;
                case 22: RequireQualification(editing&&ReferenceEquals(flight,retainedFlight)&&session.Current!.Design.Digest==launchedDigest&&session.Revision==launchedRevision&&session.Dirty==launchedDirty,"return retains owners, draft, history and dirty baseline");RequireQualification(flight!.State.Epoch.Ticks==retainedEpoch&&solar!.CurrentTime.Ticks==retainedEpoch,"editor preserves single physical/Solar epoch");ClickButton("Return to flight");break;
                case 23: RequireQualification(ReferenceEquals(flight,retainedFlight)&&flight!.State.Epoch.Ticks>=retainedEpoch&&!flight.Failed,"resume continues canonical flight");ClickButton("Menu");ClickButton("New vehicle / retained design");break;
                case 24: RunLaunchRefusalChecks();RunApplicationAdversarialChecks();qualificationName=craftName.Text;ClientSize=new(1100,740);break;
                case 25: RequireQualification(native->Width==(uint)viewport.ClientSize.Width&&native->Height==(uint)viewport.ClientSize.Height,"resize keeps native child extent");craftName.Focus();PostMessageW(craftName.Handle,0x102,'Z',1);PostMessageW(craftName.Handle,0x100,9,1);break;
                case 26: RequireQualification(native->Focused==0&&!craftName.Focused&&craftName.Text.Contains('Z')&&flight!.PlayerInput.Observation.Requested==default,"text and Tab remain isolated from physical controls");craftName.Text=qualificationName;ValidateChildren();count.SelectedItem=8;count.Focus();PostMessageW(count.Handle,0x100,0x26,1);break;
                case 27: RequireQualification(Convert.ToInt32(count.SelectedItem)==4,"combo keyboard input survives native message pump");qualificationSaved=session.Save(session.Revision);qualificationYaw=camera.Yaw;ViewportMessage(0x204,2);break;
                case 28: ViewportMessage(0x200,2,240,200);break;
                case 29: RequireQualification(camera.Yaw!=qualificationYaw&&session.Save(session.Revision).SequenceEqual(qualificationSaved!),"right drag orbits without editing");ViewportMessage(0x205);qualificationTarget=camera.Target;ViewportMessage(0x207,16);break;
                case 30: ViewportMessage(0x200,16,220,240);break;
                case 31: RequireQualification(camera.Target!=qualificationTarget&&session.Save(session.Revision).SequenceEqual(qualificationSaved!),"middle drag pans without editing");ViewportMessage(0x208);qualificationDistance=camera.Distance;ViewportMessage(0x20a,120u<<16);break;
                case 32: RequireQualification(camera.Distance<qualificationDistance&&session.Save(session.Revision).SequenceEqual(qualificationSaved!),"wheel zoom is presentation only");ViewportMessage(0x201,1);PostMessageW(Handle,0x8001,0,0);break;
                case 33: RequireQualification(native->Pressed==0&&native->Buttons==0&&native->Focused==0&&session.Save(session.Revision).SequenceEqual(qualificationSaved!),"foreign capture clears pending click and held input");ReleaseCapture();ShowStartup();overlay!.Controls.OfType<CheckBox>().Single().Checked=true;ClickButton("Apply display settings");break;
                case 34: RequireQualification(Handle.ToInt64()==applicationHandle&&GetWindow(viewport.Handle,5).ToInt64()==childHandle&&native->Stop==0,"fullscreen preserves parent, child and renderer lease");ShowStartup();overlay!.Controls.OfType<CheckBox>().Single().Checked=false;overlay.Controls.OfType<ComboBox>().Single().SelectedItem=NovaCore.Launcher.NovaCoreResolutionPreset.Resolution1280x720;ClickButton("Apply display settings");break;
                case 35: RequireQualification(Handle.ToInt64()==applicationHandle&&GetWindow(viewport.Handle,5).ToInt64()==childHandle&&native->Stop==0,"windowed return preserves parent, child and renderer lease");RequireQualification(session.Save(session.Revision).SequenceEqual(qualificationSaved!)&&ReferenceEquals(flight,retainedFlight),"display transitions preserve both document and physical owner");FocusCraft();editorMeasuring=true;break;
                default:
                    if(warmMeasurements.Count<360){applicationStep--;break;}
                    RequireQualification(padObservedRoutes.Contains("flight")&&padObservedRoutes.Contains("flight-return"),"flight return restores canonical pad/caster");
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(qualificationPath))!);
                    File.WriteAllText(qualificationPath,JsonSerializer.Serialize(new{judgment="APPLICATION_INTEGRATION_PASS",playerPass=false,padRoutes=padObservedRoutes.Order(StringComparer.Ordinal).ToArray(),checks=qualificationChecks,tank=qualificationTankDefinition,process=Environment.ProcessId,window=Handle.ToInt64(),parts=session.Current!.Design.Parts.Length,source=launchedDigest,compiled=flight!.Craft.Digest,retainedEpoch,finalEpoch=flight.State.Epoch.Ticks,frames=applicationFrames,warmWindows,native=NativeRuntimeLibraryIdentity()},new JsonSerializerOptions{WriteIndented=true}));
                    approvedClose=true;native->Stop=1;break;
            }
        }catch(Exception ex){FailQualification(ex);native->Stop=1;}
    }
    private void PreparePadDaylight()
    {
        // Only the opt-in visual witness chooses a daytime browsing epoch. Launch
        // still admits the selected Solar epoch through the ordinary physical owner.
        var start=solar!.CurrentTime;
        for(var hour=0;hour<24;hour++){
            var epoch=new NovaCore.Simulation.Time.SimulationInstant(start.Ticks+hour*3_600_000_000L);
            RequireQualification(solar.TryPresentPhysicalEpoch(epoch,flightCamera!,out _),"visual witness epoch");
            RequireQualification(solar.Presentation.TryGetBody(NovaCore.Simulation.Celestial.SolarSystemBodyIds.Earth.Value,out var earth)&&solar.Presentation.TryGetBody(NovaCore.Simulation.Celestial.SolarSystemBodyIds.Sun.Value,out _),"daylight body authority");
            solar.Presentation.TryGetBody(NovaCore.Simulation.Celestial.SolarSystemBodyIds.Sun.Value,out var sun);
            var cosine=Double3.Dot((sun.Position.Value-earth.Position.Value).Normalized(),earth.BodyFixedToRoot.Rotate(solar.FloridaLaunchSite.Object.Anchor.NormalizedBodyFixedDirection));
            if(cosine>.5)return;
        }
        throw new InvalidDataException("No daytime Florida witness in the next 24 hours.");
    }
    void IApplicationPresentation.ObservePresentation(NativeFrameSubmission* frame)
    {
        if(SurfaceRetryQualification){SurfaceRetryObserve();return;}
        if(RecorderQualification){RecorderObserve(frame);return;}
        if(qualificationPath is null||qualificationTankDefinition is "connector-graph" or "stabilization")return;
        var route=editing?"editor":flight is null?(padWarmPhase>=3?"game-return":"game"):(applicationStep>=23?"flight-return":"flight");
        if(padObservedRoutes.Contains(route))return;
        // The first callback moves the camera to Florida before this observation.
        if(route=="game"&&padWarmPhase==0)return;
        var slabs=0;for(var i=0;i<frame->ObjectCount;i++){
            RequireQualification(frame->Objects[i].Mesh.Value is not (3 or 4),"canonical submission has no legacy site meshes");
            if(frame->Objects[i].Mesh.Value==7)slabs++;
        }
        if(editing){RequireQualification(frame->FacilityCaster is null&&slabs==0,"editor suppresses world site");}
        else{
            RequireQualification(slabs==1&&frame->FacilityCaster is not null,"game/flight submits one slab and its caster");
            var caster=*frame->FacilityCaster;
            RequireQualification(caster.GeometrySet==7&&caster.Equals(solar!.FacilityCaster),"canonical caster matches current slab");
            if(padCasterIdentity==0){padCasterIdentity=(nint)frame->FacilityCaster;padCasterDefinition=caster;}
            RequireQualification(padCasterIdentity==(nint)frame->FacilityCaster&&padCasterDefinition.Equals(caster),"stable immutable caster across lifecycle");
        }
        padObservedRoutes.Add(route);Console.WriteLine($"FLORIDA_LIFECYCLE_PASS route={route} slabs={slabs} legacyMeshes=0 caster={(editing?0:7)}");
    }
    private void ChooseCard(string id){category="All";RefreshCatalog();partCards[id].PerformClick();}
    private void Hover(Double3 position){var point=camera.Project(position,viewport.ClientSize.Width,viewport.ClientSize.Height);qualificationHover=new((int)Math.Round(point.X),(int)Math.Round(point.Y));ViewportMessage(0x200,0,qualificationHover.Value.X,qualificationHover.Value.Y);}
    private void SocketHover(string instance,string socket){var p=session.Current!.Design.Parts.Single(p=>p.Instance.Id==instance);Hover(p.Instance.Pose.Then(p.Definition.Attachments.Single(a=>a.Id==socket).Frame).Position);}
    private void ExpectPreview(int parts){RequireQualification(session.Preview?.Design.Parts.Length==parts,$"hover snaps all preview members before click; mode={mode.SelectedIndex} chosen={Chosen.Id} pointer={native->PointerX},{native->PointerY} sockets="+string.Join(";",sockets.Select(s=>$"{s.Target}@{s.X:F1},{s.Y:F1},visible={s.Visible}")));expectedPreview=session.Preview!.Design.Digest;}
    private void ExpectCommit(int parts){RequireQualification(session.Current?.Design.Parts.Length==parts&&session.Current.Design.Digest==expectedPreview&&session.Preview is null,"click commits exact shown transform and membership");}
    private void PostFlightKey(char key,bool release=true){ViewportMessage(0x100,key);if(release)ViewportMessage(0x101,key);}
    private void VerifyRenderedPreview()
    {
        var expected=session.Preview!.Design.Data.Instances.Where(p=>!session.Current!.Design.Data.Instances.Any(x=>x.Id==p.Id)).SelectMany(p=>assets[p.Definition.Id].Meshes.Select(m=>(m.Handle.Value,p.Pose.Point(m.Gimballed?assets[p.Definition.Id].GimbalPivot:Double3.Zero),p.Pose.Rotation))).ToArray();
        var shown=renderEntries.Where(e=>e.Tint==1).Select(e=>(e.Mesh,e.Position,e.Rotation)).ToArray();
        RequireQualification(expected.SequenceEqual(shown)&&expected.Length>=8,"every actual member mesh and transform is submitted by the preview renderer");
        RequireQualification(sockets.Any(s=>s.Available&&s.Parent==activeSocketParent&&s.Target==activeSocketTarget&&s.Visible),"snapped candidate has a visible authored compatible marker");
    }
}
