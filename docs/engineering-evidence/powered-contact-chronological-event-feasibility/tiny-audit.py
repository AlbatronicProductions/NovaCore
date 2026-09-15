"""Supplementary tiny/zero audit: same frozen two inputs, no backend or files outside evidence.

Declared before this supplementary execution: require relative error <=1e-40
for resolved high-precision positive-minus-zero moments, exact positive ledger,
and matching rounded binary64 body endpoints. Ordinary absolute bars alone are
not evidence of tiny preservation. This strengthens reporting after the first
nine cases passed; no input or materiality threshold is changed.
"""
import runpy
from decimal import Decimal, localcontext
from pathlib import Path
import json

root=Path(__file__).resolve().parent
ns=runpy.run_path(str(root/'chronology.py'))
with localcontext() as ctx:
    ctx.prec=700
    pos,zero=ns['CASES'][3:5]
    cp,cz=ns['candidate'](pos),ns['candidate'](zero)
    rp=ns['ref'](pos)[0]; rz=ns['ref'](zero)[0]
    rows={}
    for key in ('vx','x','wy','theta','jn','jt','jtw','work_t','work_twist','support_mx'):
        rd=ns['dec'](rp[key]-rz[key]); cd=cp[key]-cz[key]
        assert rd!=0 and cd!=0, key
        relative=abs((cd-rd)/rd)
        assert relative<=Decimal('1e-40'), (key,relative)
        rows[key]=dict(reference_delta=ns['fmt'](rd),candidate_delta=ns['fmt'](cd),relative_error=ns['fmt'](relative))
    body=('vx','x','wy','theta','vy','y','wx')
    assert all(float(cp[k])==float(cz[k])==float(ns['dec'](rp[k]))==float(ns['dec'](rz[k])) for k in body)
    assert pos['h']>0 and float(pos['h'])==0 and pos['q']*pos['h']==pos['fuel']
    result=dict(scope='two supplementary evaluations of the SAME frozen tiny/zero cases; no retuning',
                h='2^-1075 seconds',fuel='2^-1074 kg = one exact resource unit',
                positive_authority=True,zero_authority_distinct=True,backend_calls=0,
                h_binary64_hex=float(pos['h']).hex(),
                force_impulse_exact=str(pos['force']*pos['h']),
                force_impulse=ns['fmt'](pos['force']*pos['h']),
                body_binary64_equal=True,differences=rows)
    (root/'tiny-differential.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(result,indent=2))
