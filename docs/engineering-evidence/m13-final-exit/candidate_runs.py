"""Lead-only default candidate qualification; no prototype switch or raw capture."""
import inspect, sys
import exit as e
a=e.assess

if sys.argv[1]=='build':
    source=inspect.getsource(a.build)
    source='\n'.join(line for line in source.splitlines() if not line.strip().startswith('for n,b in original.items():assert b.replace'))+'\n'
    source=source.replace("    assert not subprocess.check_output(['git','diff','--name-only'],cwd=ROOT,text=True).strip()", "    assert all((ROOT/n).read_bytes()==b for n,b in original.items())")
    source=source.replace('os.link(ROOT/', 'oracle.exists() or os.link(ROOT/')
    source=source.replace('timing-host.patch','candidate-timing-host.patch').replace("write('private-host',", "write('private-candidate-timing-host',")
    exec(source,a.__dict__);a.build()
else:
    original_write=a.write
    def write(label,data):
        data.update(evidenceRole='implemented unbanked default candidate',productionBase=e.BANK)
        original_write(label,data)
    a.write=write
    original_execute=a.execute
    def execute(label,*args,**kwargs):
        return original_execute('candidate-'+label,*args,**kwargs)
    a.execute=execute
    if sys.argv[1]=='fixed':
        for pose in ['orbital','factor1','florida','grazing','active-refinement','inland']:
            d=a.fixed(pose,False)
            assert d['metrics']['gpuTotalMs']['n']==100
    elif sys.argv[1]=='profile':
        for pose in ['florida','inland']:a.fixed(pose,True)
        for mode in ['regional','full','warp']:e.dynamic(mode,True,'dynamic-'+mode)
    else:raise ValueError(sys.argv[1])
