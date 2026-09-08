"""One client-only screenshot of banked Florida after its supported return route."""
import ctypes,hashlib,json,subprocess,sys,time
from ctypes import wintypes as w
from PIL import ImageGrab
from assess import ROOT,HERE,environment,sha,write

label='florida-current' if '--current' in sys.argv else 'florida-return'

def capture(pid):
    u=ctypes.windll.user32;u.SetProcessDPIAware()
    callback=ctypes.WINFUNCTYPE(w.BOOL,w.HWND,w.LPARAM)
    found=[]
    def visit(hwnd,param):
        owner=w.DWORD();u.GetWindowThreadProcessId(hwnd,ctypes.byref(owner))
        if owner.value==pid and u.IsWindowVisible(hwnd):
            rect=w.RECT();u.GetClientRect(hwnd,ctypes.byref(rect))
            if rect.right>=1000 and rect.bottom>=700:found.append((hwnd,rect))
        return True
    u.EnumWindows(callback(visit),0);assert len(found)==1,found
    hwnd,rect=found[0];u.GetForegroundWindow.restype=w.HWND
    assert u.GetForegroundWindow()==hwnd,'Target is obscured; do not capture another application'
    point=w.POINT(0,0);u.ClientToScreen(hwnd,ctypes.byref(point))
    box=(point.x,point.y,point.x+rect.right,point.y+rect.bottom)
    assert (rect.right,rect.bottom)==(3440,1440),box
    path=HERE/(label+'.png');assert not path.exists()
    ImageGrab.grab(bbox=box,all_screens=True).save(path,optimize=True)
    return {'image':path.name,'sha256':sha(path),'clientBounds':box,'nativeClientSize':[rect.right,rect.bottom],'method':'Win32 process-owned foreground client bounds; desktop screenshot; no GPU readback','trigger':'Florida seating stability frame=440, after return and L17 publication; screenshot is visual context, not exact GPU-frame identity'}

folder=ROOT/'samples/NovaCore.Triangle/bin/Release/net10.0'
args=['--scene=sol','--focus=earth','--surface-site=florida-launch','--physical-surface=m12d-natural-candidate','--solar-epoch=j2000','--benchmark-frames=500','--log=startup,validation,vulkan']
if '--current' in sys.argv:args.remove('--solar-epoch=j2000')
env=environment();env['NOVACORE_EARTH_ROUTE_VALIDATION']='florida'
process=subprocess.Popen([str(folder/'NovaCore.Triangle.exe'),*args],cwd=ROOT,env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True)
lines=[];visual=None
try:
    for line in process.stdout:
        lines.append(line)
        if 'Florida seating stability: frame=440;' in line:
            visual=capture(process.pid)
    code=process.wait(timeout=90)
finally:
    if process.poll() is None:process.terminate();process.wait(timeout=30)
log=''.join(lines);errors=[x.strip() for x in lines if any(t in x for t in ['VUID-','Validation Error','VK_ERROR','FAIL:'])]
assert code==0 and not errors and visual,(code,errors,visual)
write(label+'-visual',{'exitCode':code,'errors':errors,'visual':visual,'executable':str(folder/'NovaCore.Triangle.exe'),'nativeHash':sha(folder/'NovaCore.Native.dll'),'managedHash':sha(folder/'NovaCore.Triangle.dll'),'shaderHashes':{p.name:sha(p) for p in (folder/'shaders').glob('*.spv')},'args':args,'env':{k:v for k,v in env.items() if k.startswith(('NOVACORE_','VK_'))},'logBytes':len(log.encode()),'logSha256':hashlib.sha256(log.encode()).hexdigest(),'lines':[x.strip() for x in lines if any(t in x for t in ['Solar startup:', 'seating stability:','Earth route validation:','scenario validation:','GPU timings:','Frame pacing:','Average frame time:','regional physical totals:','level=17;','NCSM1 regional ready:'])]})
print(json.dumps(visual))
