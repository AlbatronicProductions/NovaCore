from pathlib import Path
import json,hashlib,sys
r=Path(sys.argv[1]);out=Path(__file__).parent
base=r/'tests/NovaCore.ContactNumerics.IntegratedProbe'
def once(s,a,b):
    assert s.count(a)==1,a
    return s.replace(a,b)
s=(base/'RetainedOperator.cs').read_text()
s=once(s,'if(!GeneralizedTransport.Feasible(current,work))return OperatorStatus.LoadRefusal;','if(!StudyControl.ArithmeticValid(work))return OperatorStatus.LoadRefusal;')
s=once(s,'Scaled numerator=default,delta;double denominator=0;','Scaled numerator=default,delta=default;double denominator=0;')
s=once(s,'if(ordinary)\n        {','if(StudyControl.UseD)\n        {\n        if(ordinary)\n        {')
s=once(s,'for(int i=0;i<4;i++)work[i]=ScaleMath.Add(work[i],delta);','for(int i=0;i<4;i++)work[i]=ScaleMath.Add(work[i],delta);\n        }')
s=once(s,'if(!GeneralizedTransport.Feasible(current,work))return OperatorStatus.DRefusal;',
'''StudyControl.Override(work);
        if(!StudyControl.CurrentFeasible(current,work))return OperatorStatus.DRefusal;
        StudyControl.Begin(patch,free,ordinaryH,work);
        StudyControl.Observe("initial",0,-1,work,0,0,0);''')
s=once(s,'sweep<8','sweep<StudyControl.Sweeps')
s=once(s,'if(next.Mantissa<0){nc++;next=default;}work[i]=next;','if(next.Mantissa<0){nc++;next=default;}work[i]=next;\n                StudyControl.Observe("normal-row",sweep+1,i,work,nc,tc,wc);')
s=once(s,'var v0=ordinary?', 'StudyControl.Observe("before-tangent",sweep+1,-1,work,nc,tc,wc);\n            var v0=ordinary?')
s=once(s,'work[4]=x.Times(scale);work[5]=y.Times(scale);','work[4]=x.Times(scale);work[5]=y.Times(scale);\n            StudyControl.Observe("after-tangent",sweep+1,-1,work,nc,tc,wc);')
s=once(s,'}work[6]=twist;','}work[6]=twist;\n            StudyControl.Observe("after-sweep",sweep+1,-1,work,nc,tc,wc);')
# Verify every solver arithmetic line remains verbatim after removing observation lines/count.
old=(base/'RetainedOperator.cs').read_text()
a=old[old.index('int nc=0,tc=0,wc=0;'):old.index('internal OperatorStatus BeginInstall')]
b=s[s.index('int nc=0,tc=0,wc=0;'):s.index('internal OperatorStatus BeginInstall')]
b='\n'.join(l for l in b.split('\n') if 'StudyControl.Observe(' not in l).replace('sweep<StudyControl.Sweeps','sweep<8')
assert a==b
g=(base/'GeneralizedTransport.cs').read_text()
g=once(g,'!Feasible(old,cache)','!StudyControl.HistoricalFeasible(old,cache)')
g=once(g,'&&Feasible(next,result)','&&StudyControl.ArithmeticValid(result)')
records=[]
for name,text in [('StudyOperator.cs',s),('StudyTransport.cs',g)]:
    p=out/name
    with p.open('x',encoding='utf-8',newline='\n') as f:f.write(text)
    records.append(dict(path=name,sha256=hashlib.sha256(p.read_bytes()).hexdigest().upper()))
with (out/'generated-identity.json').open('x') as f:json.dump(dict(solverArithmeticVerbatim=True,files=records),f,indent=2)
