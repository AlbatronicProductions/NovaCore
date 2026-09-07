"""Consolidate bounded evidence before scratch retirement; never delete files."""
import hashlib,json,pathlib,re,shutil,struct,subprocess,xml.etree.ElementTree as ET
ROOT=pathlib.Path(__file__).resolve().parents[3];HERE=pathlib.Path(__file__).resolve().parent;OUT=ROOT/'build/ksa-terrain-convergence'
def sha(p):
    with p.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def save(name,obj):(HERE/name).write_text(json.dumps(obj,indent=2)+'\n',encoding='utf-8')
def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT,text=True).strip()
def summarize(p):
    text=p.read_text(encoding='utf-8-sig');lines=text.splitlines()
    return dict(path=str(p.relative_to(ROOT)),bytes=p.stat().st_size,sha256=sha(p),summary=[x for x in lines if any(k in x for k in ['PASS','FAIL','Exception','selected=','Native identity:','Window deployment:','Status:','SHA-256:','Error(s)','Warning(s)','Build succeeded','support:','VUID-'])])
if __name__=='__main__':
    for p in HERE.glob('gate*.json'):
        d=json.loads(p.read_text());rows=d.get('frameRows',[])
        if rows:
            same={k:v for k,v in rows[0].items() if all(r.get(k)==v for r in rows)}
            d.setdefault('frameConstants',{}).update(same)
            d['frameRows']=[{k:v for k,v in r.items() if k not in same} for r in rows]
            d['rowEncoding']='Full sample = frameConstants merged with each frameRows entry. All original samples retained.'
            save(p.name,d)
    names=['headless-release.log','headless-release-bounded.log','headless-debug.log','gpu-release.log','gpu-debug.log','residency-regression-release.log','residency-regression-debug.log','launcher-regressions.log','live-florida-validation.log','native-test-build.log','native-gpu-Release-failed.log','native-gpu-release-complete.log','native-cpu-release.log','native-cpu-debug.log','asset-earth.log','asset-florida.log','tests-release-rebuild.log','tests-debug-rebuild.log']
    save('validation-results.json',dict(records=[summarize(OUT/n) for n in names],final=json.loads((OUT/'final-validation.json').read_text()),notes=[
        'First Release headless run deliberately stopped only its parent and NCSM1 generation child; no PASS claimed. Bounded Debug/Release runs used existing production-library input mode without changing topology assertions.',
        'Both broad headless runs completed 78/79 because a pre-existing M13.2 source assertion omitted ordinaryNcsm1. Focused migrated assertion passed both configurations. Combined final result79/79; full suites were not redundantly rerun.',
        'First Release native GPU invocation could not start missing EXCLUDE_FROM_ALL test executable. Explicit targets then built and both strict native GPU tests passed; no Vulkan error was filtered.',
        'Initial launcher invocation used the wrong net10.0 directory; corrected net10.0-windows invocation passed15.',
        'Moving Florida actual contact:9frames81points atL16/L17; collection43frames spansL8-L17. Other native GPU/managed GPU tests retain distinct purposes.'
    ]))
    shutil.copy2(OUT/'launcher-route-probe.json',HERE/'launcher-route-probe.json')
    shutil.copy2(OUT/'smoke-result.json',HERE/'smoke-result.json')
    shutil.copy2(OUT/'live-florida/facility-contact.json',HERE/'moving-florida-contact.json')
    # Keep the small physical replay collection: actual GPU triangles used by
    # permanent moving contact checks, about0.6MB rather than full attachments.
    live=HERE/'moving-contact-frames';live.mkdir(exist_ok=True)
    for p in (OUT/'live-florida').glob('frame-*.json'):shutil.copy2(p,live/p.name)
    shader_names=[pathlib.PureWindowsPath(e.attrib['Include']).name for e in ET.parse(ROOT/'samples/NovaCore.Triangle/NovaCore.Triangle.csproj').iter('RuntimeShader')]
    deployments={}
    for config,native in [('Debug','native-ninja'),('Release','native-ninja-release')]:
        sample=ROOT/f'samples/NovaCore.Triangle/bin/{config}/net10.0';build=ROOT/'build'/native
        shaders={name:sha(sample/'shaders'/name) for name in shader_names}
        assert all(shaders[name]==sha(build/'shaders'/name) for name in shader_names)
        assert sha(sample/'NovaCore.Native.dll')==sha(build/'NovaCore.Native.dll')
        deployments[config]=dict(sample=str(sample/'NovaCore.Triangle.exe'),native=sha(sample/'NovaCore.Native.dll'),managed=sha(sample/'NovaCore.Triangle.dll'),executable=sha(sample/'NovaCore.Triangle.exe'),shaders=shaders)
    save('deployment.json',deployments)
    isa={}
    for mode in ['baseline','candidate']:
        runtime=OUT/f'gate4-active-{mode}';data={}
        for stage in [1,2,4,16]:
            p=runtime/f'stage-{stage}.isa';text=p.read_text();ops=re.findall(r'^\s+([sv]_[a-zA-Z0-9_]+)\b',text,re.M)
            words=re.findall(r';\s+([0-9a-f]{8}(?: [0-9a-f]{8})?)\s*$',text,re.M)
            data[stage]=dict(sha256=sha(p),instructions=len(ops),fp64Named=sum('_f64' in op for op in ops),fp32Named=sum('_f32' in op for op in ops),wave32='UC_VERSION_W32_BIT' in text,machineWordsSha256=hashlib.sha256('\n'.join(words).encode()).hexdigest())
        isa[mode]=data
    sections={}
    for mode in ['baseline','candidate']:
        p=OUT/f'gate4-active-{mode}/NovaCore.Native.dll';data=p.read_bytes();offset=struct.unpack_from('<I',data,0x3c)[0];count=struct.unpack_from('<H',data,offset+6)[0];size=struct.unpack_from('<H',data,offset+20)[0];start=offset+24+size;items={}
        for i in range(count):
            row=start+i*40;name=data[row:row+8].rstrip(b'\0').decode();length,position=struct.unpack_from('<II',data,row+16);items[name]=hashlib.sha256(data[position:position+length]).hexdigest()
        sections[mode]=items
    isa['nativeSections']=sections
    save('compiler-summary.json',isa)
    source_lists=json.loads((HERE.parent/'ksa-terrain-architecture/source-files.json').read_text())
    paths=[pathlib.Path('E:/Kitten Space Agency/KSA.dll')]+[pathlib.Path('E:/Kitten Space Agency')/p for p in source_lists['ksaInstalled']]+[ROOT/p for p in source_lists['ksaDecompiled']]
    for p in ['assembly-source/KSA/Celestial.cs','assembly-source/KSA/TerrainPatch.cs','assembly-source/KSA/DecalModifierReference.cs','assembly-source/KSA/PlanetMeshCollection.cs']:
        paths.append(ROOT/'build/ksa-residency-reference'/p)
    save('ksa-provenance.json',dict(version='2026.9.7.5402',revision='487c3f340de24c6a81037120b6d1129c045c5400',files=[dict(path=str(p),bytes=p.stat().st_size,sha256=sha(p)) for p in dict.fromkeys(paths) if p.exists()]))
    poses=[]
    for pose in ['orbital','factor1','florida','active','grazing']:
        a=json.loads((HERE/f'gate4-{pose}-baseline.json').read_text())['analysis']['metrics'];b=json.loads((HERE/f'gate4-{pose}-candidate.json').read_text())['analysis']['metrics']
        gain=a['gpuDetailedDrawMs']['median']-b['gpuDetailedDrawMs']['median']
        poses.append(dict(pose=pose,baselineTerrain=a['gpuDetailedDrawMs'],candidateTerrain=b['gpuDetailedDrawMs'],baselineTotal=a['gpuTotalMs'],candidateTotal=b['gpuTotalMs'],gainMs=gain,gainPercent=100*gain/a['gpuDetailedDrawMs']['median'],candidateDistanceTo833=b['gpuTotalMs']['median']-8.33))
    save('performance-summary.json',poses)
    print('Bounded evidence consolidated; no files deleted.')
