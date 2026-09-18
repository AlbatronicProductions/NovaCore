"""Generate observation-only copies; never modify production source."""
from pathlib import Path
import shutil,hashlib,json,difflib,sys
r=Path.cwd();e=r/'docs/engineering-evidence/player-flight-controls-gauntlet/stage5-player-wasdqe';variant=sys.argv[1] if len(sys.argv)>1 else 'native-hold-observer';assert variant in ['native-hold-observer','native-hold-observer-stage6'];o=r/'build/player-flight-controls-gauntlet'/variant;o.mkdir(parents=True,exist_ok=True)
shutil.copytree(r/'native/NovaCore.Native',o/'native-source',dirs_exist_ok=True)
changes=[]
def patch(rel,transform,dest):
    before=(r/rel).read_text();after=transform(before);assert after!=before
    dest.write_text(after)
    changes.append(dict(path=rel,sourceSha256=hashlib.sha256((r/rel).read_bytes()).hexdigest(),overlaySha256=hashlib.sha256(dest.read_bytes()).hexdigest()))
    (o/(dest.stem+'.diff')).write_text(''.join(difflib.unified_diff(before.splitlines(True),after.splitlines(True),fromfile=rel,tofile=str(dest))))
def native(s):
    s=s.replace('App *gApp{};', 'App *gApp{};\n'+(e/'native-trace.cpp.txt').read_text())
    s=s.replace('LRESULT CALLBACK Proc(HWND h, UINT m, WPARAM w, LPARAM l) {','LRESULT CALLBACK Proc(HWND h, UINT m, WPARAM w, LPARAM l) {\n  const auto holdBefore=gApp?gApp->pilotKeys:0u;')
    s=s.replace('  if (m == WM_CLOSE)', '''  if(gApp&&(m==WM_KEYDOWN||m==WM_KEYUP||m==WM_SYSKEYUP||m==WM_KILLFOCUS||m==WM_SETFOCUS||m==WM_CAPTURECHANGED||m==WM_ENTERMENULOOP||m==WM_ENTERSIZEMOVE||m==WM_CANCELMODE||m==WM_DESTROY))
    HoldTrace(1,m,(uint32_t)w,holdBefore,gApp->pilotKeys,GetForegroundWindow()==h&&GetFocus()==h?1u:0u);
  if(m==WM_DESTROY)HoldTraceFlush();
  if (m == WM_CLOSE)''')
    s=s.replace('  NcHostEvent e{NC_UPDATE_FRAME, NC_LOG_NONE, nullptr, in, a.submission};','  ++holdFrame;HoldTrace(2,0,in.engineActions,a.pilotKeys,in.pilotKeys,in.controlInputActive);\n  NcHostEvent e{NC_UPDATE_FRAME, NC_LOG_NONE, nullptr, in, a.submission};')
    return s
patch('native/NovaCore.Native/NovaCoreNative.cpp',native,o/'native-source/NovaCoreNative.cpp')
def managed(s):
    s=s.replace('finally{state.Assembly?.Dispose();','finally{NativeHoldProbe.Flush(state);state.Assembly?.Dispose();')
    s=s.replace('    ApplyViewport(s,e->Input);','    NativeHoldProbe.Capture(s,e->Input);\n    ApplyViewport(s,e->Input);')
    probe=(e/'managed-observer.cs.txt').read_text()
    if variant.endswith('stage6'):
        probe=probe.replace('DoubleQuaternion CameraOrientation);','DoubleQuaternion CameraOrientation,uint Look,float MouseX,float MouseY,int Wheel,uint Focus,uint CameraAction,double OrbitDistance);')
        probe=probe.replace('s.Camera.Orientation);count++;','s.Camera.Orientation,input.LookActive,input.MouseDeltaX,input.MouseDeltaY,input.MouseWheelDetents,(uint)input.PresentationFocus,(uint)input.CameraActions,s.Sol.OrbitDistance);count++;')
    return s+'\n'+probe
patch('samples/NovaCore.Triangle/Program.cs',managed,o/'Program.cs')
(o/'observer.targets').write_text('<Project><ItemGroup><Compile Remove="$(MSBuildProjectDirectory)/Program.cs"/><Compile Include="'+str(o/'Program.cs').replace('\\','/')+'"/></ItemGroup></Project>')
(o/'source.json').write_text(json.dumps(changes,indent=2))
print(o)
