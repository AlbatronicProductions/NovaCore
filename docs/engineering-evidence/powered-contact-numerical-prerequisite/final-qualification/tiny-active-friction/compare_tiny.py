"""Read-only candidate/reference comparison followed by a create-new concise result."""
from pathlib import Path
from decimal import Decimal as D, localcontext
import json, struct, sys
from reference import exact, scale53, scaled_value, clean

def dec(x): return D(x)
def candidate_scaled(x): return exact(x['Mantissa'])*D(2)**x['Exponent']
def values(s): return [exact(s[k][a]) for k in ('Linear','Angular') for a in ('X','Y','Z')]
def bits(v): return struct.pack('<d',float(v)).hex()
def f32(v): return struct.unpack('<f',struct.pack('<f',float(v)))[0]

def compare(reference_dir,candidate_dir):
    refs=[]; candidates=[]; reports=[]; passed=True
    for label in ('baseline','powered'):
        r=json.loads((reference_dir/(label+'-reference.json')).read_text())
        c=json.loads((candidate_dir/(label+'-tiny.json')).read_text())
        assert c['status']=='Ready'
        rv=list(map(dec,r['endpoint'])); cv=values(c['target']['Endpoint'])
        ri=list(map(dec,r['increment'])); ci=[candidate_scaled(x) for x in c['increments']]
        rc=list(map(dec,r['impulses'])); cc=[candidate_scaled(c['target']['Cache'][k]) for k in ('N0','N1','N2','N3','T0','T1','Twist')]
        er=[abs(x-y)/abs(y) for x,y in zip(ci,ri)]
        ec=[abs(x-y)/abs(y) for x,y in zip(cc,rc)]
        endpoint_bits=[bits(x)==bits(y) for x,y in zip(cv,rv)]
        absolute=[abs(x-y) for x,y in zip(cv,rv)]
        normal=abs(sum(cc[:4])-sum(rc[:4]))
        native_cache=[f32(x)==f32(y) for x,y in zip(cc,rc)]
        ok=all(endpoint_bits) and max(er+ec)<=D('2e-6') and max(absolute+[normal])<=D('1e-4') and all(native_cache)
        reports.append(dict(branch=label,pass_=ok,endpointBits=endpoint_bits,absoluteEndpointErrors=absolute,
            normalSumError=normal,incrementRelativeErrors=er,cacheRelativeErrors=ec,nativeCacheProjectionMatches=native_cache))
        passed &= ok; refs.append((rv,ri,rc));candidates.append((cv,ci,cc))
    differences={}
    for j,name in enumerate(('endpoint','increment','cache')):
        rb,rp=refs[0][j],refs[1][j];cb,cp=candidates[0][j],candidates[1][j]
        rd=[p-b for p,b in zip(rp,rb)];cd=[p-b for p,b in zip(cp,cb)]
        rounded_ref=[scaled_value(scale53(p))-scaled_value(scale53(b)) for p,b in zip(rp,rb)]
        if name=='increment':
            checks=[x==0 for x in cd];checks[1]=abs(cd[1]-rounded_ref[1])/abs(rounded_ref[1])<=D('2e-6') and bits(cd[1])==bits(float.fromhex('0x0.0000000000001p-1022'))
        else: checks=[x==0 for x in cd]
        passed &= all(checks)
        differences[name]=dict(referenceHighPrecision=rd,referenceRoundedScaledFieldDifference=rounded_ref,
                              candidateFieldDifference=cd,checks=checks)
    return dict(status='PASS' if passed else 'FAIL',branches=reports,differences=differences,
                installationsAuthorized=passed,coastNotYetQualified=True)

if __name__=='__main__':
    with localcontext() as c:
        c.prec=1200
        result=compare(Path(sys.argv[1]),Path(sys.argv[2]))
        with Path(sys.argv[3]).open('x',encoding='utf-8') as f:json.dump(clean(result),f,indent=2);f.write('\n')
        print(result['status'])
        for b in result['branches']:
            print(b['branch'],'max cache relative',format(max(b['cacheRelativeErrors']),'.12E'),
                  'max increment relative',format(max(b['incrementRelativeErrors']),'.12E'))
        for k,v in result['differences'].items():print(k,v['checks'])
        sys.exit(0 if result['status']=='PASS' else 1)
