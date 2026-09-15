"""Create-only isolated twelve-sweep source. Never edits prior candidate or evidence."""
from pathlib import Path
import hashlib,json,sys
r=Path(sys.argv[1]); out=Path(__file__).parent
b=r/'tests/NovaCore.ContactNumerics.IntegratedProbe'
def once(s,a,b):
    assert s.count(a)==1,a
    return s.replace(a,b)
def save(name,s):
    with (out/name).open('x',encoding='utf-8',newline='\n') as f:f.write(s)
old=(b/'RetainedOperator.cs').read_text(); s=old
s=once(s,'if(!GeneralizedTransport.Feasible(current,work))return OperatorStatus.LoadRefusal;',
       'if(!FinalPolicy.ArithmeticValid(work))return OperatorStatus.LoadRefusal;')
s=once(s,'Scaled numerator=default,delta;double denominator=0;','Scaled numerator=default,delta=default;double denominator=0;')
start=s.index('        if(ordinary)\n');end=s.index('        proof=new(moved,transported,scaled,shift,beforeD,delta,numerator,denominator,Vector(work),0,0,0);')
s=s[:start]+s[end:]
s=once(s,'if(!GeneralizedTransport.Feasible(current,work))return OperatorStatus.DRefusal;',
       'if(!FinalPolicy.CurrentClosed(current,work))return OperatorStatus.DRefusal;\n        BoundaryPolicy.SolverAdmissions++;')
s=once(s,'sweep<8','sweep<12')
a=old[old.index('int nc=0,tc=0,wc=0;'):old.index('internal OperatorStatus BeginInstall')]
c=s[s.index('int nc=0,tc=0,wc=0;'):s.index('internal OperatorStatus BeginInstall')].replace('sweep<12','sweep<8')
assert a==c,'Solver/endpoint arithmetic changed'
save('TwelveOperator.cs',s)
g=(b/'GeneralizedTransport.cs').read_text()
g=once(g,'!Feasible(old,cache)','!FinalPolicy.HistoricalClosed(old,cache)')
g=once(g,'&&Feasible(next,result)','&&FinalPolicy.ArithmeticValid(result)')
save('TwelveTransport.cs',g)
q=(b/'Qualification.cs').read_text().replace('sweeps=8','sweeps=12')
q=once(q,'        Gate="lifecycle";', '''        RegressionCase("07-composition",g,g,cache,Binary(f.GetProperty("PreviousH").GetDouble()),
            Binary(f.GetProperty("PreviousH").GetDouble()/8),source,MidMass,Gravity,new(Gravity.Gravity,new(0,64,0),default));
        Gate="lifecycle";''')
q=once(q,'cases=7','cases=8')
save('GateQualification.cs',q)
old_dir=out.parent/'friction-boundary-continuation'
bd=(old_dir/'BoundaryDriver.cs').read_text().replace('sweeps=8','sweeps=12')
bd=once(bd,'var b=Run(baseline);var p=Run(powered);Pair(baseline,powered,b,p);',
'''var b=Run(baseline);
            if(args.Length>2&&args[2]=="baseline"){Console.WriteLine("BASELINE_TWELVE_PASS");return;}
            var p=Run(powered);Pair(baseline,powered,b,p);''')
save('CoastDriver.cs',bd)
project=(out.parent/'active-set-convergence/ConvergenceStudy.csproj').read_text()
project=project.replace('<StartupObject>StudyDriver</StartupObject>','<StartupObject>Qualification</StartupObject>')
project=once(project,'Probe/GeneralizedTransport.cs" Link=', 'Probe/GeneralizedTransport.cs;$(RepoRoot)tests/NovaCore.ContactNumerics.IntegratedProbe/Qualification.cs" Link=')
save('TwelveGate.csproj',project)
files=[dict(path=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest().upper()) for p in out.iterdir() if p.suffix in ('.cs','.csproj','.py')]
save('generated-identity.json',json.dumps(dict(mechanism='NO-D + 12',solverArithmeticVerbatim=True,files=files),indent=2)+'\n')
