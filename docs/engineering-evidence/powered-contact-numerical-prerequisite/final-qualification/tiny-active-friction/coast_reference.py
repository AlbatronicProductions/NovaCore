"""Independent following-coast references for both accepted and unrounded branches."""
from pathlib import Path
from decimal import Decimal as D,localcontext
import json, sys
from reference import exact,model,dot,norm,eliminate,clean,projection,response,Z,RESIDUAL_BAR

def solve_coast(m):
    h=D(16666)/1000000-D(2)**-1075; w=m['omega']; k=w*h*(w*h+2); alpha=1/k
    accel=[Z,exact(-9.81),Z]; K=m['K']
    free=[m['source'][j]+h*accel[j] for j in range(3)]+m['source'][3:]
    bias=[min(m['depth'][i]/h,m['depth'][i]/(h+2/w),D(2)) for i in range(4)]
    matrix=[row.copy() for row in K]
    rhs=[-dot(m['L'][i],free[:3])-dot(m['A'][i],free[3:]) for i in range(7)]
    for i in range(4):matrix[i][i]+=alpha*K[i][i];rhs[i]+=bias[i]
    impulses=eliminate(matrix,rhs)
    dv=[h*accel[j]+D('.125')*sum((m['L'][i][j]*impulses[i] for i in range(7)),Z) for j in range(3)]
    dw=[D('.5')*sum((m['A'][i][j]*impulses[i] for i in range(7)),Z) for j in range(3)]
    endpoint=[x+y for x,y in zip(m['source'],dv+dw)]
    # Independently check final-velocity rows, not the eliminated matrix residual.
    slip=[dot(m['L'][i],endpoint[:3])+dot(m['A'][i],endpoint[3:]) for i in range(7)]
    residual=[slip[i]+alpha*K[i][i]*impulses[i]-bias[i] if i<4 else slip[i] for i in range(7)]
    cap=D('.125')*sum(impulses[:4]); twcap=D('.125')*dot(m['r'],impulses[:4])
    length=norm(impulses[4:6]);tw=abs(impulses[6])
    # Large cap >=guard makes the pinned projection identity here, even if the
    # demanded tangent impulse norm happens to fall below that guard.
    passed=all(x>0 for x in impulses[:4]) and length<cap and tw<twcap and cap>m['guard'] and max(map(abs,residual))<=RESIDUAL_BAR
    assert passed,'Following coast reference not admissible'
    return dict(impulses=impulses,increment=dv+dw,endpoint=endpoint,h=h,
                check=dict(pass_=passed,normal='ALL FOUR ACTIVE',tangent='INTERIOR',twist='INTERIOR',
                           residual=residual,maxResidual=max(map(abs,residual)),tangentLength=length,
                           tangentCap=cap,twistMagnitude=tw,twistCap=twcap,guard=m['guard']))

if __name__=='__main__':
    root=Path(sys.argv[1]); out=root/'coast-reference.json'
    with localcontext() as ctx:
        ctx.prec=1100
        result={};exact_solutions=[];installed_solutions=[];exact_caches=[]
        for label in ('baseline','powered'):
            inputs=json.loads((root/'candidate-results'/(label+'-coast-input.json')).read_text())
            tiny=json.loads((root/'reference-results'/(label+'-reference.json')).read_text())
            m=model(inputs)
            installed=solve_coast(m);m['source']=list(map(D,tiny['endpoint'])); ideal=solve_coast(m)
            cache=list(map(D,tiny['impulses']));exact_caches.append(cache)
            result[label]=dict(acceptedSource=inputs,installedSourceReference=installed,unroundedSourceReference=ideal,
                               referenceTinyCache=cache,producingDuration='2^-1075',
                               previousLoadPreserved=inputs['accepted']['Load'])
            installed_solutions.append(installed);exact_solutions.append(ideal)
        result['installedBranchResponse']=response(*installed_solutions)
        result['unroundedBranchResponse']=response(*exact_solutions)
        ratio=(D(16666)/1000000-D(2)**-1075)/(D(2)**-1075)
        result['discardedCacheResponseAfterDurationScaling']=[projection((p-b)*ratio) for b,p in zip(*exact_caches)]
        result['scope']='Unique admissible current coast solution; warm cache/load provenance retained separately, not used as an oracle answer.'
        with out.open('x',encoding='utf-8') as f:json.dump(clean(result),f,indent=2);f.write('\n')
        for label in ('baseline','powered'):
            for mode in ('installedSourceReference','unroundedSourceReference'):
                x=result[label][mode];print(label,mode,'PASS','residual',format(x['check']['maxResidual'],'.8E'),
                    'tangent',format(x['check']['tangentLength'],'.8E'),'/',format(x['check']['tangentCap'],'.8E'),
                    'twist',format(x['check']['twistMagnitude'],'.8E'),'/',format(x['check']['twistCap'],'.8E'))
        print('Duration-amplified discarded cache response:',[r['doubleHex'] for r in result['discardedCacheResponseAfterDurationScaling']])
        print('Unrounded coast endpoint response:',[r['doubleHex'] for r in result['unroundedBranchResponse']['endpoint']])
