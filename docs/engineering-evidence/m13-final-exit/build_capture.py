"""Lead-only private parity host; restores source and normal deployment exactly."""
import inspect, importlib.util, sys
import poll

a = poll.a
spec = importlib.util.spec_from_file_location('exit_capture_hooks', poll.e.HERE/'capture.py')
capture = importlib.util.module_from_spec(spec); spec.loader.exec_module(capture)
candidate = len(sys.argv)>1 and sys.argv[1]=='candidate'
base = poll.e.instrument if candidate else a.instrument
a.instrument = lambda s: capture.instrument_capture(base(s))
if candidate:
    def instrument(s):
        s=capture.instrument_capture(base(s))
        anchor='      if(result!=VK_SUCCESS){vkFreeMemory(a.device,tentative,nullptr);tentative={};pointer=nullptr;}\n      return result;'
        assert s.count(anchor)==1
        s=s.replace(anchor,anchor.replace('      return result;', '''      if(result==VK_SUCCESS&&use==nc::MappedBufferUse::TerrainRequestKeys){char row[256];std::snprintf(row,sizeof row,"Poll memory type: index=%u; flags=%u; heap=%u; compatible=1; selected=%u; bytes=%llu",type,properties.memoryTypes[type].propertyFlags,properties.memoryTypes[type].heapIndex,type,(unsigned long long)size);a.Log(NC_LOG_ALWAYS,row);}
      return result;'''))
        assert 'NOVACORE_EXIT_CACHED_KEYS' not in s
        return s
    a.instrument=instrument
a.capture = capture
source = inspect.getsource(a.build)
source = source.replace("'samples/NovaCore.Triangle/ProductionBillboardDesktopTraversal.cs']", "'samples/NovaCore.Triangle/ProductionBillboardDesktopTraversal.cs','samples/NovaCore.Triangle/Program.cs']")
source = source.replace('os.link(ROOT/', 'oracle.exists() or os.link(ROOT/')
anchor = "        (HERE/'timing-host.patch')"
assert source.count(anchor) == 1
source = source.replace(anchor, "        path=ROOT/names[2];path.write_text(capture.instrument_program(path.read_text()))\n" + anchor)
source = source.replace('timing-host.patch','capture-host.patch').replace("write('private-host',", "write('private-capture-host',")
source = source.replace("'noCaptureInstrumentation':True", "'captureFrame':175,'rawCaptureSlot':str(capture.RAW),'captureTimingExcluded':True")
if candidate:
    source='\n'.join(line for line in source.splitlines() if not line.strip().startswith('for n,b in original.items():assert b.replace'))+'\n'
    source=source.replace("    assert not subprocess.check_output(['git','diff','--name-only'],cwd=ROOT,text=True).strip()", "    assert all((ROOT/n).read_bytes()==b for n,b in original.items())")
    source=source.replace('capture-host.patch','candidate-capture-host.patch').replace('private-capture-host','private-candidate-capture-host')
exec(source, a.__dict__)
a.build()
