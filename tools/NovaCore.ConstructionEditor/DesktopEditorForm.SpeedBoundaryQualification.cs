using System.Diagnostics;
using System.Text.Json;
using NovaCore.Simulation.Time;

namespace NovaCore.ConstructionEditor;

// Opt-in native-message regression. It measures authoritative epochs across real
// rendered frames; it does not claim held hardware input or physical DPI coverage.
internal sealed unsafe partial class DesktopEditorForm
{
    private int speedProofStep;
    private long speedProofNext, speedProofEpoch;
    private readonly List<object> speedProofSamples=new();
    private SolarSystemScene? speedProofOwner;
    private TextBox? speedProofText;
    private byte[]? speedProofCraft;
    private void SpeedProofState(string label)
    {
        RefreshHud(true);
        speedProofSamples.Add(new{label,frame=playerFrameCounter,utc=DateTimeOffset.UtcNow,
            epoch=solar!.CurrentTime.Ticks,rate=solar.Rate,preset=solar.SpeedPresetIndex,
            userPaused=UserPaused,hostHold=((IApplicationPresentation)this).Paused,
            effectiveSimulationPause=UserPaused||((IApplicationPresentation)this).Paused,
            editing,physical=flight is not null,hud=timeHud.Text});
        Console.WriteLine($"SPEED_BOUNDARY step={speedProofStep} label={label} epoch={solar.CurrentTime.Ticks} preset={solar.SpeedPresetIndex} userPaused={UserPaused}");
    }
    private void SpeedProofFrozen(string label)
    {
        RequireQualification(solar!.CurrentTime.Ticks==speedProofEpoch,label+" preserves exact authoritative epoch");
        SpeedProofState(label);
    }
    private void QualifySpeedBoundary()
    {
        if(Stopwatch.GetTimestamp()<speedProofNext)return;
        speedProofNext=Stopwatch.GetTimestamp()+Stopwatch.Frequency;
        try
        {
            qualificationStep=speedProofStep;
            switch(speedProofStep++)
            {
                case 0:
                    SurfaceQualificationFocus();speedProofOwner=solar;
                    RequireQualification(!UserPaused&&solar!.SpeedPresetIndex==1,"fresh exploration default 1x");
                    SpeedProofState("initial");PostFlightKey((char)Keys.Oemcomma);break;
                case 1:
                    RequireQualification(!UserPaused&&solar!.SpeedPresetIndex==0,"native comma reaches 0.1x");
                    SpeedProofState("minimum");PostFlightKey((char)Keys.Oemcomma);break;
                case 2:
                    RequireQualification(UserPaused&&solar!.IsPaused&&solar.SpeedPresetIndex==0,"native comma at 0.1x requests authoritative pause");
                    speedProofEpoch=solar!.CurrentTime.Ticks;
                    RequireQualification(solar.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(3600),flightCamera!,out _),"paused host sample accepted safely");
                    SpeedProofFrozen("paused ignores one hour host sample");PostFlightKey((char)Keys.Oemcomma);break;
                case 3:
                    RequireQualification(UserPaused,"repeated comma is idempotent");SpeedProofFrozen("repeated comma");
                    PostFlightKey((char)Keys.OemPeriod);break;
                case 4:
                    RequireQualification(!UserPaused&&solar!.SpeedPresetIndex==0&&solar.CurrentTime.Ticks>speedProofEpoch,"period resumes at retained 0.1x");
                    var before=solar!.CurrentTime.Ticks;
                    RequireQualification(solar.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(10),flightCamera!,out _)&&solar.CurrentTime.Ticks-before==1_000_000,"resume adds exactly supplied host credit at 0.1x with no paused catch-up");
                    SpeedProofState("resumed minimum");PostFlightKey((char)Keys.OemPeriod);break;
                case 5:
                    RequireQualification(!UserPaused&&solar!.SpeedPresetIndex==1,"next period follows existing ladder");
                    SpeedProofState("1x");PostFlightKey(' ');break;
                case 6:
                    RequireQualification(!UserPaused&&solar!.SpeedPresetIndex==1,"Space remains unbound");
                    ExecuteCommand("speed0");ExecuteCommand("slower");speedProofEpoch=solar!.CurrentTime.Ticks;PostFlightKey((char)Keys.Escape);break;
                case 7:
                    RequireQualification(overlay is not null&&UserPaused,"Esc opens independently of user pause");SpeedProofFrozen("Esc open");
                    PostFlightKey((char)Keys.OemPeriod);break;
                case 8:
                    RequireQualification(UserPaused&&solar!.SpeedPresetIndex==0,"overlay rejects speed input");SpeedProofFrozen("overlay rejects period");
                    CloseOverlay();break;
                case 9:
                    RequireQualification(UserPaused&&solar!.SpeedPresetIndex==0,"Resume releases overlay only and queues no rate command");SpeedProofFrozen("Esc resume");
                    topBar.Alpha=255;topBar.Show();topBar.BringToFront();((ToolStripMenuItem)menus.Items[1]).ShowDropDown();break;
                case 10:
                    RequireQualification(menuOpen,"Speed menu owns independent hold");
                    ExecuteCommand("faster");RequireQualification(!UserPaused&&solar!.SpeedPresetIndex==0&&((IApplicationPresentation)this).Paused,"increase releases only user pause under menu hold");
                    speedProofEpoch=solar!.CurrentTime.Ticks;break;
                case 11:
                    SpeedProofFrozen("menu hold after user resume");
                    SetUserPause(true);ExecuteCommand("speed2");RequireQualification(UserPaused&&solar!.SpeedPresetIndex==2,"menu selects retained preset while paused without resume or loop");
                    ExecuteCommand("faster");RequireQualification(UserPaused&&solar!.SpeedPresetIndex==3,"higher-preset increase preserves existing user pause");
                    ExecuteCommand("slower");RequireQualification(UserPaused&&solar!.SpeedPresetIndex==2,"higher-preset decrease preserves existing user pause");
                    ExecuteCommand("speed0");RequireQualification(UserPaused&&solar!.SpeedPresetIndex==0,"menu restores minimum while keeping pause");
                    RefreshCommandItems(menus.Items);
                    var speedMenu=((ToolStripMenuItem)((ToolStripMenuItem)menus.Items[1]).DropDownItems[0]);
                    RequireQualification(speedMenu.DropDownItems.OfType<ToolStripMenuItem>().Single(x=>Equals(x.Tag,"pause")).Checked&&speedMenu.DropDownItems.OfType<ToolStripMenuItem>().Single(x=>Equals(x.Tag,"speed0")).Checked,"Speed menu reflects authoritative pause and retained preset");
                    PostFlightKey((char)Keys.OemPeriod);break;
                case 12:
                    RequireQualification(UserPaused&&solar!.SpeedPresetIndex==0,"menu rejects keyboard period");
                    ((ToolStripMenuItem)menus.Items[1]).HideDropDown();ShowSaveHudLayout();
                    speedProofText=overlay!.Controls.OfType<TextBox>().Single();speedProofText.Focus();
                    PostMessageW(speedProofText.Handle,0x100,(nuint)Keys.OemPeriod,0);PostMessageW(speedProofText.Handle,0x102,'.',0);PostMessageW(speedProofText.Handle,0x101,(nuint)Keys.OemPeriod,0);break;
                case 13:
                    RequireQualification(speedProofText!.Text.Contains('.')&&UserPaused&&solar!.SpeedPresetIndex==0,"text input receives punctuation without gameplay speed dispatch");
                    SpeedProofFrozen("text entry");CloseOverlay();break;
                case 14:
                    RequireQualification(UserPaused&&solar!.SpeedPresetIndex==0,"closing text modal queues no command");
                    EnterEditor();break;
                case 15:
                    RequireQualification(editing&&ReferenceEquals(solar,speedProofOwner)&&UserPaused,"editor retains exploration authority");
                    SpeedProofFrozen("editor hold");
                    session.LoadFrom(session.Revision,Path.Combine(SaveDirectory,"fixture.craft.json"),true);speedProofCraft=session.Save(session.Revision);
                    var saved=Path.Combine(SaveDirectory,"speed-boundary-roundtrip.craft.json");SaveEntry(saved,null);
                    session.Clear(session.Revision,true);session.LoadFrom(session.Revision,saved,true);
                    RequireQualification(session.Save(session.Revision).SequenceEqual(speedProofCraft)&&ReferenceEquals(solar,speedProofOwner)&&UserPaused,"draft save/reload preserves exact craft and exploration pause");
                    craftName.Focus();RequireQualification(craftName.Visible&&craftName.Enabled&&GetFocus()==craftName.Handle,"visible craft text owns focus");
                    RequireQualification(!DispatchPlayerKey(GetWindow(viewport.Handle,5),(ulong)Keys.OemPeriod)&&UserPaused,"foreign control focus rejects speed command");
                    LeaveEditor();break;
                case 16:
                    SpeedProofFrozen("editor return");RequireQualification(UserPaused&&solar!.SpeedPresetIndex==0,"editor return keeps pause and rate");
                    ShowFlightBrowser();RequireQualification(!AllControls(overlay!).OfType<Button>().Single(x=>x.Text=="SAVE NEW FLIGHT").Enabled,"exploration session saves remain unavailable");CloseOverlay();
                    EnterEditor();LaunchCraft();break;
                case 17:
                    RequireQualification(flight is {Failed:false}&&!editing&&!ReferenceEquals(solar,speedProofOwner)&&!UserPaused&&solar!.SpeedPresetIndex==1,"launch intentionally replaces exploration pause with fresh physical 1x context");
                    RequireQualification(commands["slower"].Refusal() is not null&&commands["faster"].Refusal() is not null&&commands["speed0"].Refusal() is not null&&commands["speed2"].Refusal() is not null,"physical rate limits unchanged");
                    SetUserPause(true);speedProofEpoch=flight!.State.Epoch.Ticks;PostFlightKey((char)Keys.Oemcomma);PostFlightKey((char)Keys.OemPeriod);PostFlightKey(' ');break;
                case 18:
                    RequireQualification(UserPaused&&flight!.State.Epoch.Ticks==speedProofEpoch&&solar!.SpeedPresetIndex==1,"physical hotkeys and Space leave qualified pause untouched");
                    EnterEditor();LeaveEditor();RequireQualification(physicalUserPaused,"physical editor return retains user pause");
                    var flightPath=Path.Combine(Path.GetDirectoryName(qualificationPath!)!,"roundtrip.ncflight.json");
                    flight!.SuspendLive();File.WriteAllBytes(flightPath,flight.SaveFlight());RestoreFlightPath(flightPath);
                    RequireQualification(!physicalUserPaused&&solar!.SpeedPresetIndex==1&&flight!.State.Epoch.Ticks==speedProofEpoch,"existing flight reload resets UI pause and retains saved physical epoch at 1x");break;
                case 19:
                    RequireQualification(flight is {Failed:false}&&!UserPaused&&flight.State.Epoch.Ticks>speedProofEpoch,"restored physical flight advances normally");
                    RefreshHud(true);RequireQualification(timeHud.Text=="Flight · 1×"&&!timeHud.Text.Contains("Epoch"),"one retained HUD reflects physical state without Epoch");
                    SpeedProofState("physical reload");
                    File.WriteAllText(qualificationPath!,JsonSerializer.Serialize(new{judgment="SPEED_BOUNDARY_PASS",checks=qualificationChecks,hardwareKeys=false,realOsDpiTransition=false,source=typeof(DesktopEditorForm).Assembly.Location,native=NativeRuntimeLibraryIdentity(),samples=speedProofSamples},new JsonSerializerOptions{WriteIndented=true}));CloseApproved();break;
            }
        }
        catch(Exception ex){FailQualification(ex);CloseApproved();}
    }
}
