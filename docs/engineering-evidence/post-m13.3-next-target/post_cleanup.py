"""Normal deployed Florida smoke after private scratch removal; no raw capture."""
import hashlib,json,os,pathlib,re,subprocess,sys
sys.dont_write_bytecode=True
from prepare import ROOT,HERE,OUT,verify,verify_source,sha
def main():
    assert not OUT.exists(),'Private scratch should already be absent'
    verify_source();deployment=verify()
    env={k:v for k,v in os.environ.items() if not k.startswith(('NOVACORE_','VK_LAYER','VK_ADD_LAYER','VK_IMPLICIT_LAYER','VK_ADD_IMPLICIT_LAYER','VK_LOADER','VK_VALIDATION','VK_INSTANCE_LAYERS'))}
    # A nonexistent process-local implicit search path prevents machine registry
    # discovery. Explicit Khronos comes from the SDK; loader logging verifies it.
    env.update(VK_LAYER_PATH=str(pathlib.Path(os.environ['VULKAN_SDK'])/'Bin'),VK_IMPLICIT_LAYER_PATH=str(OUT/'absent-implicit'),VK_LAYER_SETTINGS_PATH=str(OUT/'absent-settings'),VK_INSTANCE_LAYERS='VK_LAYER_KHRONOS_validation',VK_LOADER_DEBUG='layer',NOVACORE_WINDOW_CLIENT_WIDTH='3440',NOVACORE_WINDOW_CLIENT_HEIGHT='1440',NOVACORE_WINDOW_BORDERLESS='1')
    args=[ROOT/'samples/NovaCore.Triangle/bin/Release/net10.0/NovaCore.Triangle.exe','--scene=sol','--focus=earth','--surface-site=florida-launch','--physical-surface=m12d-natural-candidate','--benchmark-frames=500','--log=startup,validation,vulkan']
    result=subprocess.run(list(map(str,args)),cwd=ROOT,env=env,capture_output=True,text=True,timeout=120)
    log=result.stdout+result.stderr
    errors=[x for x in log.splitlines() if any(t in x for t in ['VUID-','Validation Error','ERROR','VK_ERROR'])]
    frames=re.search(r'Average frame time:.*\((\d+) frames\)',log)
    layerLines=[x for x in log.splitlines() if any(t in x for t in ['Loading layer library','Insert instance layer','Insert device layer','Khronos_validation','khronos_validation'])]
    passed=result.returncode==0 and not errors and frames and int(frames[1])==500 and 'regional ready:' in log and 'tesDrawReady=true' in log and 'zeroOwner=0; overlapOwner=0; staleGenerationDraws=0' in log and any('Insert instance layer' in x and 'KHRONOS_validation' in x for x in layerLines)
    record={'pass':bool(passed),'exitCode':result.returncode,'frames':int(frames[1]) if frames else None,'args':list(map(str,args)),'env':{k:v for k,v in env.items() if k.startswith(('NOVACORE_','VK_'))},'logSha256':hashlib.sha256(log.encode()).hexdigest(),'logBytes':len(log.encode()),'errors':errors,'layers':layerLines,'lines':[x for x in log.splitlines() if any(t in x for t in ['Runtime fingerprint:','runtime candidate:','spherical billboard publication:','regional ready:','regional physical totals:','seating stability:','Frame pacing:','Average frame time:'])],'deployment':deployment['deployment'],'assets':deployment['assets']}
    (HERE/'post-cleanup-smoke.json').write_text(json.dumps(record,indent=2)+'\n')
    if not passed:(HERE/'post-cleanup-failure.txt').write_text(log)
    assert not OUT.exists()
    print('POST-CLEANUP SMOKE: '+('PASS' if passed else 'FAIL'))
    if not passed:raise SystemExit(1)
if __name__=='__main__':main()
