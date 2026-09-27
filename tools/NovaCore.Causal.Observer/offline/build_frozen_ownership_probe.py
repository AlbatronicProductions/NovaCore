"""Extract exact NovaCore functions for CPU-only link-time-stub qualification."""
import argparse, hashlib, json, re
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('output',type=Path);p.add_argument('--baseline-source',type=Path);p.add_argument('--current-unmarked-baseline',action='store_true');a=p.parse_args()
repo=Path(__file__).resolve().parents[3]; native=repo/'native/NovaCore.Native';a.output.mkdir(parents=True,exist_ok=True)
source=(native/'NovaCoreNative.cpp').read_text()
def struct(name):
    start=source.index('struct '+name+' {'); opening=source.index('{',start);depth=1;end=opening+1
    while depth:
        if source[end]=='{':depth+=1
        elif source[end]=='}':depth-=1
        end+=1
    assert source[end]==';';return source[start:end+1]
types='\n'.join(struct(n) for n in ['ProductionBillboardTopologyResource','RegionalDemandBuffer','RegionalDependencyJob','RegionalPreparationControl','RegionalPreparationJob'])
functions=(native/'FrozenCaptureNative.inl').read_text();functions=functions[functions.index('void BeginRecordingCallTrace'):]
scope=re.search(r'constexpr uint32_t Width = \d+, Height = \d+;',source).group()
# Preserve the production namespace lookup environment. Omitting these symbols
# previously concealed header keys being shadowed by window dimensions.
functions='namespace production_capture_scope {\n'+scope+'\n'+functions+'\n}\n'+''.join('using production_capture_scope::'+n+';\n' for n in ['BeginRecordingCallTrace','FrozenFrameAuthority','RecordFrozenCapture','CompleteFrozenCapture'])
(a.output/'FrozenTypes.generated.inl').write_text(types+'\n')
(a.output/'FrozenTopology.generated.inl').write_text((native/'FrozenTopologyNative.inl').read_text())
(a.output/'FrozenControl.generated.inl').write_text((native/'FrozenControlNative.inl').read_text())
packing=(native/'FrozenPackingNative.inl').read_text();packing=packing[packing.index('void RecordFrozenPacking'):]
(a.output/'FrozenPacking.generated.inl').write_text(packing)
shader=(native/'shaders/frozen_capture_pack.comp').read_text()
(a.output/'FrozenPackShader.generated.inl').write_text(shader[shader.index('uint PackPhysical'):shader.index('#include "frozen_capture_pack_shared.glsl"')])
(a.output/'FrozenFunctions.generated.inl').write_text(functions)
causal=(native/'CausalNative.inl').read_text()
(a.output/'FrozenLabel.generated.inl').write_text(causal[causal.index('struct CausalGpuLabel'):causal.index('void CausalFault')])
(a.output/'FrozenDraw.generated.inl').write_text(causal[causal.index('void CausalDraw('):causal.index('void CausalDrawIndexed(')])
tail=source[source.index('  // First eligible capture only;'):source.index('\nvoid Recreate(App &a)')]
(a.output/'FrozenTail.generated.inl').write_text('void ProbeRecordTail(App& a,VkCommandBuffer c){\n'+tail)
paths=[native/n for n in ['NovaCoreNative.cpp','NovaCoreNative.h','FrozenCaptureNative.inl','FrozenCapture.h','FrozenTopologyNative.inl','RegionalPhysicalResidency.h','CausalRecorder.h','RecordingCallTrace.h','CausalNative.inl']]
paths.extend(native/n for n in ['FrozenPacking.h','FrozenPackingNative.inl','shaders/frozen_capture_pack.comp','shaders/frozen_capture_pack_shared.glsl'])
paths.extend(native/n for n in ['FrozenControlNative.inl','StartupLifecycle.h'])
ops=(native/'RecordingCallTrace.h').read_text().split('enum class RecordingOp:uint64_t {',1)[1].split('};',1)[0]
names=['None']+[part.strip().split('=')[0] for part in ops.split(',')]
consumer=repo/'tools/NovaCore.Causal.Observer/RecordingCallProgress.cs'
decoded=re.findall(r'"([A-Za-z]+)"',consumer.read_text().split('Names=[',1)[1].split('];',1)[0])
assert names==decoded, 'Native/observer operation names differ'
paths.append(consumer)
(a.output/'recording-operations.json').write_text(json.dumps(dict(enumerate(names)),indent=2)+'\n')
if a.current_unmarked_baseline:
    # Compare this transport revision with itself without the one-shot markers.
    # The archived old transport remains separate historical evidence.
    functions=(native/'FrozenCaptureNative.inl').read_text();functions=functions[functions.index('void BeginRecordingCallTrace'):]
    functions=functions.replace('auto header=FrozenFrameAuthority(a);','auto header=baseline::FrozenFrameAuthority(a);')
    tail=source[source.index('  // First eligible capture only;'):source.index('\nvoid Recreate(App &a)')]
    tail=re.sub(r'^.*BeginRecordingCallTrace\(a,c\);.*$', '', tail, flags=re.M)
    tail=tail.replace('RecordFrozenCapture(a,c);','baseline::RecordFrozenCapture(a,c);')
    (a.output/'FrozenBaseline.generated.inl').write_text('namespace baseline {\n'+scope+'\n'+functions+'\nvoid ProbeRecordTail(App& a,VkCommandBuffer c){\n'+tail+'\n}\n')
elif a.baseline_source:
    old=a.baseline_source/'native/NovaCore.Native'
    functions=(old/'FrozenCaptureNative.inl').read_text();functions=functions[functions.index('nc::frozen::Header FrozenFrameAuthority'):]
    # Parity reference is the pre-marker sequence with the independently proven
    # header-index correction. Executing the known out-of-bounds original here
    # would be unsafe, especially in Release. The original has a separate witness.
    functions=functions.replace('h[Width]','h[nc::frozen::Width]').replace('h[Height]','h[nc::frozen::Height]')
    # Namespace qualification only, to avoid ADL finding the marked function
    # through the shared fixture App type. Call bodies/arguments are unchanged.
    functions=functions.replace('auto header=FrozenFrameAuthority(a);','auto header=baseline::FrozenFrameAuthority(a);')
    prior=(old/'NovaCoreNative.cpp').read_text();prior=prior[:prior.index('\nvoid Recreate(App &a)')]
    tail=prior[prior.rindex('  vkCmdDraw(c,3,1,0,0);'):]
    tail=tail.replace('RecordFrozenCapture(a,c);','baseline::RecordFrozenCapture(a,c);')
    (a.output/'FrozenBaseline.generated.inl').write_text('namespace baseline {\n'+scope+'\n'+functions+'\nvoid ProbeRecordTail(App& a,VkCommandBuffer c){\n'+tail+'\n}\n')
    paths.extend([old/'FrozenCaptureNative.inl',old/'NovaCoreNative.cpp'])
manifest=dict(gpuExecution=False,scope='Production authority construction, recording and completion functions; local Vulkan fakes; synthetic CPU memory only',sources=[dict(path=str(f),sha256=hashlib.sha256(f.read_bytes()).hexdigest()) for f in paths],generated=[dict(path=str(f),sha256=hashlib.sha256(f.read_bytes()).hexdigest()) for f in a.output.glob('*.generated.inl')])
(a.output/'extraction.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('Extracted exact production capture functions; no renderer launched.')
