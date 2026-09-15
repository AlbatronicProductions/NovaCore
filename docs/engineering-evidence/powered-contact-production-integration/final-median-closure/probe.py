"""One diagnostic process only. Adapt retained timestamp observer, restore exact corrected bytes."""
from pathlib import Path
import sys

root = Path('E:/NovaCore')
base = (root/'docs/engineering-evidence/powered-contact-production-integration/performance-revision/instrument.py').read_text()

def replace(old, new):
    global base
    if base.count(old) != 1:
        raise RuntimeError(f'Probe template mismatch: {old!r}: {base.count(old)}')
    base = base.replace(old, new)

replace("S = R/'build/powered-contact-performance-revision'", "S = R/'build/powered-contact-final-median-closure'")
replace("('private EnginePreparationStatus ReadSingleEngineActuationInOwnedPhase(',1)",
        "('private EnginePreparationStatus ReadSingleEngineActuationInOwnedPhase(',12)")
replace("('private PropellantPreparationStatus PrepareFinitePropellantInOwnedPhase(',1)",
        "('private PropellantPreparationStatus PrepareFinitePropellantInOwnedPhase(',13)")
replace("('private PropellantPreparationStatus ReadFinitePropellantInOwnedPhase(',1)",
        "('private PropellantPreparationStatus ReadFinitePropellantInOwnedPhase(',14)")
replace("[('internal static bool TryMap(',2)]", "[('internal bool TryMap(',2)]")
replace('internal const int Count = 12;', 'internal const int Count = 15;')
replace('"L_PoweredSource"};', '"L_PoweredSource","M_EnginePreviewRead","N_ResourcePrepare","O_ResourcePreviewRead"};')
replace('var parents=new long[144]; var calls=new long[12];',
        'var parents=new long[225]; var calls=new long[15]; var edges=new long[1024][];')
replace('for(var i=0;i<1024;i++) phases[i]=new double[12];',
        'for(var i=0;i<1024;i++) { phases[i]=new double[15]; edges[i]=new long[225]; }')
replace('for(var j=0;j<12;j++)', 'for(var j=0;j<15;j++)')
replace('for(var j=0;j<144;j++) parents[j]+=PoweredContactCostProbe.Parents[j];',
        'for(var j=0;j<225;j++) { var edge=PoweredContactCostProbe.Parents[j]; parents[j]+=edge; edges[i][j]=edge; }')
replace('total_ms=parents[j*12+k]*1000d/Stopwatch.Frequency',
        'total_ms=parents[j*15+k]*1000d/Stopwatch.Frequency,statistics=Stats(edges.Select(a=>a[j*15+k]*1000d/Stopwatch.Frequency).ToArray())')
replace('complete=Stats(total),phases=', '''
            groups=new[]{
                new {name="AllEngineResourceUpperBound",statistics=Stats(phases.Select(a=>a[1]+a[12]+a[13]+a[14]).ToArray()),
                    residual=Stats(total.Select((t,i)=>t-phases[i][1]-phases[i][12]-phases[i][13]-phases[i][14]).ToArray())},
                new {name="AllPreviewReadersUpperBound",statistics=Stats(phases.Select(a=>a[12]+a[14]).ToArray()),
                    residual=Stats(total.Select((t,i)=>t-phases[i][12]-phases[i][14]).ToArray())}
            },complete=Stats(total),phases=''')
# Use a distinct route so this cannot accidentally be mistaken for qualification.
base = base.replace('--powered-contact-cost-probe', '--powered-contact-final-median-probe')
exec(compile(base, __file__, 'exec'))
