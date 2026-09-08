"""Short final deployed Florida smoke; no diagnostic recapture or private host."""
import gzip,hashlib,json,pathlib,subprocess,tempfile
import whole
ROOT,HERE,SCRATCH=whole.ROOT,whole.HERE,whole.OUT
def read(name):
    p=HERE/name
    return json.loads(p.read_bytes() if p.exists() else gzip.decompress(p.with_suffix(p.suffix+'.gz').read_bytes()))
sha=whole.assess.sha
assert not SCRATCH.exists()
candidate=read('candidate-deployment-final.json')
before=whole.assess.verify_deployment()
assert before['deployment']==candidate['deployment'] and before['assets']==candidate['assets']
sourceNames=['native/NovaCore.Native/NovaCoreNative.cpp','native/NovaCore.Native/RegionalPhysicalPreparation.inl','native/NovaCore.Native/RegionalPhysicalProbe.inl','native/NovaCore.Native/MappedBufferMemory.h','native/NovaCore.Native/MappedBufferMemoryTests.cpp','native/NovaCore.Native/CMakeLists.txt']
sourceHashes={n:sha(ROOT/n) for n in sourceNames}
with tempfile.TemporaryDirectory(prefix='nc-m13-whole-smoke-') as temporary:
    target=pathlib.Path(temporary).resolve()
    assert target.parent==pathlib.Path(tempfile.gettempdir()).resolve() and target.name.startswith('nc-m13-whole-smoke-')
    whole.assess.OUT=target
    env=whole.assess.environment()
    args=['--scene=sol','--focus=earth','--surface-site=florida-launch','--physical-surface=m12d-natural-candidate','--benchmark-frames=240','--log=startup,validation,vulkan']
    result=whole.assess.execute('post-cleanup-smoke',args,env,folder=ROOT/'samples/NovaCore.Triangle/bin/Release/net10.0')
    assert result['exitCode']==0 and not result['errors']
    assert any('Average frame time:' in x and '(240 frames)' in x for x in result['lines'])
    assert any('level=17;' in x and 'physicalReady=true;' in x and 'visibleTriangles=' in x and 'zeroOwner=0; overlapOwner=0; staleGenerationDraws=0;' in x for x in result['lines'])
    temporaryFiles=[dict(name=p.relative_to(target).as_posix(),bytes=p.stat().st_size) for p in target.rglob('*') if p.is_file()]
    for p in target.rglob('*'):assert not p.lstat().st_file_attributes&0x400
assert not target.exists() and not SCRATCH.exists()
after=whole.assess.verify_deployment()
assert after['deployment']==candidate['deployment'] and after['assets']==candidate['assets']
assert all(sha(ROOT/n)==h for n,h in sourceHashes.items())
assert not subprocess.check_output(['git','diff','--cached','--name-only'],cwd=ROOT).strip()
check=subprocess.run(['git','diff','--check'],cwd=ROOT,capture_output=True,text=True);assert check.returncode==0
for ref in ['HEAD','main','origin/main','m13.4-zero-contribution-terrain-material-noise^{}']:
    assert subprocess.check_output(['git','rev-parse',ref],cwd=ROOT,text=True).strip()=='047ae479b33831eae1c0dfa3f37c657a7f70148f'
launcher=ROOT/'tools/NovaCore.Launcher/bin/Release/net10.0-windows/NovaCore.Launcher.exe';assert launcher.is_file()
data=dict(leadJudgment='PASS',classification='M13.5 CANDIDATE — READY FOR PROJECT CONTROL ACCEPTANCE',recommendation='KEEP M13 OPEN FOR ONE PROVEN RESPONSIBILITY',sourceHashes=sourceHashes,sourceUnchangedByCleanup=True,stagedDiffEmpty=True,postCleanupSmoke='PASS',frames=240,scratchAbsent=True,shaderAndAssetHashesUnchanged=True,temporarySmokeOutputRemoved=temporaryFiles,launcher=str(launcher),launcherSha256=sha(launcher),gitDiffCheck=dict(exitCode=check.returncode,stdout=check.stdout,stderr=check.stderr),gitStatusShort=subprocess.check_output(['git','status','--short'],cwd=ROOT,text=True),deployment=after['deployment'],assets=after['assets'],head=after['head'],originMain=after['originMain'],branch=after['branch'])
(HERE/'closeout.json').write_text(json.dumps(data,indent=2)+'\n')
print('POST-CLEANUP SMOKE: PASS; normal deployed Florida; 240 frames; source/deployment/assets unchanged; scratch absent.')
