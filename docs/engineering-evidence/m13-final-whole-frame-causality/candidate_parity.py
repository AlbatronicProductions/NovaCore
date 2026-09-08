"""Compare the implemented default allocator with sealed pre-change captures."""
import hashlib,json,sys
import numpy as np
import placement_parity as p

pose=sys.argv[1]
baseline=json.loads((p.whole.HERE/('placement-'+pose+'-parity.json')).read_bytes())['baseline']
actual,pixels=p.run_capture(pose,'candidate-'+pose+'-capture',False)
differences={}
for category in ['inputs','capture','geometry']:
    for key,value in baseline[category].items():
        if actual[category].get(key)!=value:differences[category+'.'+key]=[value,actual[category].get(key)]
assert actual['runtime']['shaderHashes']==baseline['runtime']['shaderHashes']
assert 'NOVACORE_WHOLE_BULK_LOCAL' not in actual['runtime']['env']
old=json.loads((p.whole.HERE/('placement-'+pose+'-parity.json')).read_bytes())['ab']['attachments']
n=p.WIDTH*p.HEIGHT;attachments={}
for name,begin,end,dtype in [('depth',0,n*4,'<f4'),('hdr',n*4,n*12,'<f2'),('image',n*12,n*16,'u1')]:
    data=memoryview(pixels)[begin:end];digest=hashlib.sha256(data).hexdigest()
    attachments[name]=dict(equalToPreChange=digest==old[name]['beforeSha256'],sha256=digest,finite=bool(np.isfinite(np.frombuffer(data,dtype=dtype)).all()))
prepared=actual['files']['prepared.bin']==baseline['files']['prepared.bin']
selected=actual['selectedOrientedMultisetSha256']==baseline['selectedOrientedMultisetSha256']
passed=not differences and prepared and selected and all(r['equalToPreChange'] and r['finite'] for r in attachments.values())
report=dict(passResult=passed,actual=actual,preChangeReport='placement-'+pose+'-parity.json',inputDifferences=differences,preparedFull64ByteExact=prepared,selectedOrientedMultisetExact=selected,attachments=attachments,
    implementationDefault=True,performanceTimingsExcluded=True,comparison='SHA256 of the complete retained pre-change attachment bytes; no raw archive required')
(p.whole.HERE/('candidate-'+pose+'-parity.json')).write_text(json.dumps(report,indent=2)+'\n')
print(pose,'production-default exact parity','PASS' if passed else 'FAIL',flush=True)
assert passed
