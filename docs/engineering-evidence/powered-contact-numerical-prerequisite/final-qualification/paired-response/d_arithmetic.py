"""Reconstruct D row arithmetic from captured values; create-only result."""
import json,math,sys
from pathlib import Path
from fractions import Fraction as F
here=Path(__file__).resolve().parent
j=json.loads((here/'diagnostics/stage-delta-inputs.json').read_text())
e=j['baseline']['Equations'];k=e['K'];h=e['h'];w=e['geometry']['Omega'];alpha=1/(w*h*(w*h+2));out={}
for name in ('baseline','powered'):
    s=j[name];work=[math.ldexp(x['Mantissa'],x['Exponent']) for x in s['Stages'][3]['values']];num=den=0.;rows=[]
    for i in range(4):
        bias=min(e['geometry']['Depth'+str(i)]/h,min(e['geometry']['Depth'+str(i)]/(h+2/w),2));r=e['freeJ'][i]-bias
        for z in range(7):
            c=k[i][z]+(alpha*k[i][i] if i==z else 0);r+=c*work[z]
            if z<4:den+=c
        rows.append(r);num+=r
    pr=s['Summary']['proof'];pn=math.ldexp(pr['DNumerator']['Mantissa'],pr['DNumerator']['Exponent']);pd=math.ldexp(pr['DDelta']['Mantissa'],pr['DDelta']['Exponent'])
    assert num==pn and den==pr['DDenominator'] and -num/den==pd
    out[name]={'rowResiduals':rows,'numerator':num,'denominator':den,'delta':pd,'numeratorHex':num.hex(),'deltaHex':pd.hex()}
a=j['baseline'];b=j['powered'];v=lambda x:F(x['Mantissa'])*F(2)**x['Exponent']
s=v(b['Summary']['proof']['LoadShift']);d=v(b['Stages'][3]['values'][0])-v(a['Stages'][3]['values'][0]);dd=v(b['Summary']['proof']['DDelta'])-v(a['Summary']['proof']['DDelta']);r=v(b['Stages'][5]['values'][0])-v(a['Stages'][5]['values'][0])
out['exactCancellation']={'requestedLoadShift':str(s),'representedNormalShift':str(d),'additionDifferenceMinusRequestedShift':str(d-s),'deltaDifference':str(dd),'representedShiftPlusDeltaDifference':str(d+dd),'finalNormalDifference':str(r)}
with Path(sys.argv[1]).open('x',encoding='utf-8') as f:json.dump(out,f,indent=2);f.write('\n')
print('Captured D numerator/denominator/delta reproduced exactly for both branches.')
