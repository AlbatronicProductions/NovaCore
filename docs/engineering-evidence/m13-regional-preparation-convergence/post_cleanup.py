"""Short normal deployed Florida smoke after retiring the private host."""
import json,pathlib,subprocess,tempfile
from composition import assess,ROOT,HERE
sha=assess.sha;verify_deployment=assess.verify_deployment

assert not (ROOT/'build/m13-regional-preparation-convergence').exists()
baseline=json.loads((HERE/'baseline.json').read_text())
deployment=verify_deployment()
assert deployment['deployment']==baseline['deployment'] and deployment['assets']==baseline['assets']
with tempfile.TemporaryDirectory(prefix='nc-m13-prep-smoke-') as temporary:
    target=pathlib.Path(temporary).resolve()
    assert target.parent==pathlib.Path(tempfile.gettempdir()).resolve() and target.name.startswith('nc-m13-prep-smoke-')
    assess.OUT=target
    env=assess.environment()
    args=['--scene=sol','--focus=earth','--surface-site=florida-launch','--physical-surface=m12d-natural-candidate','--benchmark-frames=240','--log=startup,validation,vulkan']
    result=assess.execute('post-cleanup-smoke',args,env,folder=ROOT/'samples/NovaCore.Triangle/bin/Release/net10.0')
    assert any('Average frame time:'in x and '(240 frames)'in x for x in result['lines'])
    assert any('level=17;'in x and 'physicalReady=true;'in x and 'visibleTriangles='in x and 'zeroOwner=0; overlapOwner=0; staleGenerationDraws=0;'in x for x in result['lines'])
    for p in target.rglob('*'):assert not p.lstat().st_file_attributes&0x400
assert not target.exists() and not (ROOT/'build/m13-regional-preparation-convergence').exists()
final=verify_deployment();assert final['deployment']==baseline['deployment'] and final['assets']==baseline['assets']
assert not subprocess.check_output(['git','diff','--name-only'],cwd=ROOT,text=True).strip()
assert not subprocess.check_output(['git','diff','--cached','--name-only'],cwd=ROOT,text=True).strip()
check=subprocess.run(['git','diff','--check'],cwd=ROOT,capture_output=True,text=True);assert check.returncode==0
launcher=ROOT/'tools/NovaCore.Launcher/bin/Release/net10.0-windows/NovaCore.Launcher.exe';assert launcher.is_file()
data={'classification':'M13 NOT READY â€” ONE BOUNDED BLOCKER REMAINS','productionSourceDiffEmpty':True,'stagedDiffEmpty':True,'nativeAndManagedIntermediateObjectsRebuiltFromRestoredProductionSource':True,'deploymentUnchanged':True,'all49ShadersPerConfigurationUnchanged':True,'assetsUnchanged':True,'postCleanupSmoke':'PASS','frames':240,'temporaryLoaderEnvironmentRemoved':True,'scratchAbsent':True,'removedFiles':len(json.loads((HERE/'disposable-manifest.json').read_text())['files']),'launcher':str(launcher),'launcherSha256':sha(launcher),'head':final['head'],'originMain':final['originMain'],'main':subprocess.check_output(['git','rev-parse','main'],cwd=ROOT,text=True).strip(),'tagM13_4':subprocess.check_output(['git','rev-parse','m13.4-zero-contribution-terrain-material-noise^{}'],cwd=ROOT,text=True).strip(),'tagM13_3':final['m13.3-prepared-physical-terrain'],'branch':final['branch'],'gitDiffCheck':{'exitCode':check.returncode,'stdout':check.stdout,'stderr':check.stderr},'gitStatusShort':subprocess.check_output(['git','status','--short'],cwd=ROOT,text=True),'deployment':final['deployment'],'assets':final['assets']}
(HERE/'closeout.json').write_text(json.dumps(data,separators=(',',':'))+'\n')
print('POST-CLEANUP SMOKE: PASS; 240 frames; normal deployed Florida; source/deployment/assets unchanged; scratch absent.')
