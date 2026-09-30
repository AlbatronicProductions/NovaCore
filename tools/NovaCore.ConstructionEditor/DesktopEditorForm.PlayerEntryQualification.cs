using System.Diagnostics;
using System.Text.Json;
using NovaCore.Core.Camera;
using NovaCore.Interop;

namespace NovaCore.ConstructionEditor;

// Bounded engineering route. All actions enter the actual player command/UI owners.
// OS held-state/DPI and visual acceptance are reported separately, never inferred.
internal sealed unsafe partial class DesktopEditorForm
{
    private bool PlayerEntryQualification=>qualificationTankDefinition is "player-entry" or "player-entry-flight" or "speed-boundary";
    private long entryProofStart,playerFrameCounter,entryTransitionFrame;
    private int entryProofPhase=-1;
    private CameraProjection entryProjection;
    private long entryPausedEpoch;
    private readonly List<object> entryProofExtents=new();
    private void QualifyPlayerEntry(in NativeInputState input)
    {
        if(qualificationTankDefinition=="speed-boundary"){QualifySpeedBoundary();return;}
        if(qualificationTankDefinition=="player-entry-flight"){QualifyPlayerFlightEntry(input);return;}
        try
        {
            if(entryProofStart==0){
                entryProofStart=Stopwatch.GetTimestamp();applicationHandle=Handle.ToInt64();childHandle=GetWindow(viewport.Handle,5).ToInt64();
                SurfaceQualificationFocus();SetUserPause(true);entryPausedEpoch=solar!.CurrentTime.Ticks;entryProjection=flightCamera!.Projection;
                RequireQualification(commands.Values.Where(c=>c.Shortcut!=Keys.None).GroupBy(c=>c.Shortcut).All(g=>g.Count()==1),"one handler per shortcut");
                RequireQualification(commands.Values.Count(c=>c.Shortcut==(Keys.Shift|Keys.C))==1,"one camera cycle handler");
                RequireQualification(commands["Staging"].Refusal() is not null,"staging explicitly unavailable");
                RequireQualification(commands["vessel"].Refusal() is not null,"no fabricated active vessel");
            }
            var elapsed=Stopwatch.GetElapsedTime(entryProofStart).TotalSeconds;
            var phase=elapsed<2?0:elapsed<6?1:elapsed<9?2:elapsed<13?3:elapsed<16?4:elapsed<19?5:elapsed<23?6:elapsed<27?7:8;
            if(playerFrameCounter-entryTransitionFrame>2){
                RequireQualification(input.ViewportWidthPixels==(uint)ClientSize.Width&&input.ViewportHeightPixels==(uint)ClientSize.Height,"full client/native target extent");
                if(phase<6&&!editing)RequireQualification(flightCamera!.Projection==entryProjection,"UI preserves projection and framing");
            }
            RequireQualification(Handle.ToInt64()==applicationHandle&&GetWindow(viewport.Handle,5).ToInt64()==childHandle,"stable application/child lifetime");
            RequireQualification(solar!.IsPaused&&solar.CurrentTime.Ticks==entryPausedEpoch,"user pause survives modal/editor transitions");
            if(phase!=entryProofPhase)
            {
                entryProofPhase=phase;entryTransitionFrame=playerFrameCounter;
                entryProofExtents.Add(new{phase,elapsed,loop=playerFrameCounter,parent=new{ClientSize.Width,ClientSize.Height},host=new{viewport.Width,viewport.Height},render=new{input.ViewportWidthPixels,input.ViewportHeightPixels},parentHwnd=applicationHandle,childHwnd=childHandle,dpi=DeviceDpi,projection=flightCamera!.Projection});
                Console.WriteLine($"PLAYER_ENTRY_PROOF phase={phase} loop={playerFrameCounter} render={input.ViewportWidthPixels}x{input.ViewportHeightPixels} hwnd={childHandle}");
                switch(phase){
                    case 0: PostFlightKey(' ');break;
                    case 1: ShowPausePanel();break;
                    case 2: CloseOverlay();RequireQualification(UserPaused,"Resume retains selected user pause");break;
                    case 3: topBar.Alpha=255;topBar.Show();topBar.BringToFront();((ToolStripMenuItem)menus.Items[1]).ShowDropDown();break;
                    case 4: ((ToolStripMenuItem)menus.Items[1]).HideDropDown();EnterEditor();break;
                    case 5: LeaveEditor();break;
                    case 6: FormBorderStyle=FormBorderStyle.Sizable;ClientSize=new(1920,1080);break;
                    case 7: FormBorderStyle=FormBorderStyle.None;Bounds=Screen.FromControl(this).Bounds;break;
                    case 8:
                        RequireQualification(input.ViewportWidthPixels==3440&&input.ViewportHeightPixels==1440,"native desktop restored");
                        File.WriteAllText(qualificationPath!,JsonSerializer.Serialize(new{judgment="ENGINEERING_TRANSITIONS_PASS",visualAcceptance=false,realOsDpiTransition=false,heldHardwareKeys=false,checks=qualificationChecks,extents=entryProofExtents},new JsonSerializerOptions{WriteIndented=true}));
                        CloseApproved();break;
                }
            }
        }
        catch(Exception ex){FailQualification(ex);CloseApproved();}
    }
    private void QualifyPlayerFlightEntry(in NativeInputState input)
    {
        try{
            if(entryProofStart==0){
                RequireQualification(flight is {Failed:false}&&!loading&&spawnApplied&&loadingNativeReady,"selected saved physical session is ready");
                RequireQualification(flight!.Craft.Design.Save().SequenceEqual(startupCraftBytes!),"spawn consumes exact validated bytes");
                RequireQualification(input.ViewportWidthPixels==3440&&input.ViewportHeightPixels==1440,"physical startup full native extent");
                SurfaceQualificationFocus();SetUserPause(true);entryPausedEpoch=flight.State.Epoch.Ticks;entryProofStart=Stopwatch.GetTimestamp();
                RequireQualification(commands["speed0"].Refusal() is not null&&commands["speed2"].Refusal() is not null&&commands["speed1"].Refusal() is null,"only qualified physical pause and 1x exposed");
            }
            var elapsed=Stopwatch.GetElapsedTime(entryProofStart).TotalSeconds;
            var phase=Math.Min(5,(int)elapsed);
            if(phase==entryProofPhase)return;entryProofPhase=phase;
            switch(phase){
                case 0:PostFlightKey(' ');break;
                case 1:
                    RequireQualification(flight!.State.Epoch.Ticks==entryPausedEpoch&&physicalUserPaused,"physical pause and unbound Space");SetUserPause(false);
                    PostFlightKey('Z',false);PostFlightKey((char)27);PostFlightKey((char)27);ViewportMessage(0x101,'Z');break;
                case 2:RequireQualification(!flight!.Actuation.Main,"queued ignition does not survive same-pump modal open/close");PostFlightKey('Z');break;
                case 3:RequireQualification(flight!.Actuation.Main,"fresh ignition remains available after released input");ShowPausePanel();entryPausedEpoch=flight.State.Epoch.Ticks;break;
                case 4:RequireQualification(flight!.Actuation.Main&&flight.State.Epoch.Ticks==entryPausedEpoch,"modal hold retains persistent engine state and physical epoch");CloseOverlay();PostFlightKey('X');break;
                case 5:
                    RequireQualification(!flight!.Failed,"physical state remains healthy");
                    RequireQualification(gpuMemoryReading is {IsLive:true,UsageBytes:>0},"live memory reflects actual renderer allocations");
                    File.WriteAllText(qualificationPath!,JsonSerializer.Serialize(new{judgment="STARTUP_FLIGHT_AND_INPUT_PASS",checks=qualificationChecks,source=flight.Craft.Design.Digest,epoch=flight.State.Epoch.Ticks,extent=new{input.ViewportWidthPixels,input.ViewportHeightPixels},loadingStages=completedLoadingStages,gpuMemory=gpuMemoryReading,hardwareHeldKeys=false},new JsonSerializerOptions{WriteIndented=true}));CloseApproved();break;
            }
        }catch(Exception ex){FailQualification(ex);CloseApproved();}
    }

}
