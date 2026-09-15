"""Freeze D+12 after the protected no-D refusal regression; create-only files."""
from pathlib import Path
import json,sys
r=Path(sys.argv[1]);out=Path(__file__).parent
def once(s,a,b):
    assert s.count(a)==1,a
    return s.replace(a,b)
def save(name,s):
    with (out/name).open('x',encoding='utf-8',newline='\n') as f:f.write(s)
old=(r/'tests/NovaCore.ContactNumerics.IntegratedProbe/RetainedOperator.cs').read_text()
s=once(old,'if(!GeneralizedTransport.Feasible(current,work))return OperatorStatus.LoadRefusal;',
       'if(!FinalPolicy.ArithmeticValid(work))return OperatorStatus.LoadRefusal;')
s=once(s,'if(!GeneralizedTransport.Feasible(current,work))return OperatorStatus.DRefusal;',
       'if(!FinalPolicy.CurrentClosed(current,work))return OperatorStatus.DRefusal;\n        BoundaryPolicy.SolverAdmissions++;')
s=once(s,'sweep<8','sweep<12')
assert s[s.index('int nc=0,tc=0,wc=0;'):s.index('internal OperatorStatus BeginInstall')].replace('sweep<12','sweep<8')==old[old.index('int nc=0,tc=0,wc=0;'):old.index('internal OperatorStatus BeginInstall')]
save('SelectedOperator.cs.txt',s)
q=(out/'GateQualification.cs').read_text()
q=once(q,'            var status=Prepare(owner,ng,h,8,load,default,out _,out var proof);',
       '            int admissionsBefore=BoundaryPolicy.SolverAdmissions;\n            var status=Prepare(owner,ng,h,8,load,default,out _,out var proof);')
q=once(q,'solverCalls=0,referenceCalls=0','solverCalls=BoundaryPolicy.SolverAdmissions-admissionsBefore,referenceCalls=0')
save('SelectedQualification.cs.txt',q)
p=(out/'TwelveGate.csproj').read_text()
p=once(p,'  <ItemGroup>','  <ItemGroup>\n    <Compile Remove="TwelveOperator.cs;GateQualification.cs" />\n    <Compile Include="SelectedOperator.cs.txt" />\n    <Compile Include="SelectedQualification.cs.txt" />')
save('SelectedGate.csproj',p)
save('candidate-selection.json',json.dumps(dict(candidate='D + 12',frozen=True,
    reason='No-D + 12 changed the protected moderate-normal expected refusal to Ready. Ticket requires every expected refusal to remain a refusal, so D retirement fails.',
    noDPhysicalResults='Original32N, actual1/128, captured first coast pass. Compatible generalized transport passes.',
    noDLimitation='No claim no-D violates its final current-feasibility check: its own prepared guess is closed-feasible. The protected admission outcome changes when D is omitted.',
    sourceOfSelection='Phase2 simplification decision: retain D+12 on any protected outcome regression; do not rescue no-D.',
    reportingCorrection='no-d-regressions/05-moderate-normal.json solverCalls:0 was inherited hard-coded reporting for the expected refusal. Ready and the solver source prove one admitted Prepare and twelve sweeps. Its referenceCalls:0 remains correct.',
    noDRemainingGates='Not run after first regression. Composition, second known refusal, lifecycle, actual retained sequence and tiny baseline not reached in no-D arm.',
    adoptedProductionCorrection=False,changedOriginalCandidate=False),indent=2)+'\n')
