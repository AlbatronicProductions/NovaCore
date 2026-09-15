"""Create isolated admission-only diagnostic copies; never edits original sources."""
from pathlib import Path
import hashlib,json,sys

root=Path(sys.argv[1]);out=Path(sys.argv[2]);out.mkdir(parents=True,exist_ok=True)
base=root/'tests/NovaCore.ContactNumerics.IntegratedProbe'
def replace_once(s,a,b):
    if s.count(a)!=1:raise RuntimeError('Unexpected source occurrence: '+a)
    return s.replace(a,b)

g=(base/'GeneralizedTransport.cs').read_text()
g=replace_once(g,'!Feasible(old,cache)','!BoundaryPolicy.HistoricalClosed(old,cache)')
g=replace_once(g,'&&Feasible(next,result)','&&BoundaryPolicy.ArithmeticValid(result)')
o=(base/'RetainedOperator.cs').read_text()
o=replace_once(o,'InstallFailure }','InstallFailure,CurrentInitializationRefusal,DiagnosticStopped }')
o=replace_once(o,'var transported=Vector(work);','BoundaryPolicy.Observe("basis-transport",current,work);\n        var transported=Vector(work);')
o=replace_once(o,'for(int i=0;i<7;i++)work[i]=scaled.At(i);','for(int i=0;i<7;i++)work[i]=scaled.At(i);\n        BoundaryPolicy.Observe("duration-transform",current,work);')
o=replace_once(o,'if(!GeneralizedTransport.Feasible(current,work))return OperatorStatus.LoadRefusal;',
    'BoundaryPolicy.Observe("load-correction",current,work);\n        if(!BoundaryPolicy.ArithmeticValid(work))return OperatorStatus.LoadRefusal;')
o=replace_once(o,'if(!GeneralizedTransport.Feasible(current,work))return OperatorStatus.DRefusal;',
    'BoundaryPolicy.Observe("after-D",current,work);\n        if(!BoundaryPolicy.CurrentClosed(current,work))return OperatorStatus.CurrentInitializationRefusal;\n        if(!BoundaryPolicy.BeforeSolve())return OperatorStatus.DiagnosticStopped;')

records=[]
for source,name,content in [('GeneralizedTransport.cs','DiagnosticTransport.cs',g),('RetainedOperator.cs','DiagnosticOperator.cs',o)]:
    target=out/name
    with target.open('x',encoding='utf-8',newline='\n') as f:f.write(content)
    records.append(dict(source=str((base/source).relative_to(root)),sourceSha256=hashlib.sha256((base/source).read_bytes()).hexdigest().upper(),
                        diagnostic=name,diagnosticSha256=hashlib.sha256(target.read_bytes()).hexdigest().upper()))
# Direct byte equality of the arithmetic portions after newline normalization.
original=(base/'RetainedOperator.cs').read_text()
solver=original[original.index('int nc=0,tc=0,wc=0;'):original.index('internal OperatorStatus BeginInstall')]
assert solver==o[o.index('int nc=0,tc=0,wc=0;'):o.index('internal OperatorStatus BeginInstall')]
transport=(base/'GeneralizedTransport.cs').read_text()
assert transport[transport.index('double length=0;'):transport.index('return double.IsFinite(screen)')]==g[g.index('double length=0;'):g.index('return double.IsFinite(screen)')]
with (out/'diagnostic-copy-identity.json').open('x',encoding='utf-8') as f:
    json.dump(dict(files=records,solverAndBiasVerbatim=True,transportAlgebraVerbatim=True,
                   scope='Historical/current admission separation and diagnostics only'),f,indent=2);f.write('\n')
