"""Losslessly consolidate this completed proof's scalar/constant journals."""
import pathlib,json,gzip,hashlib
HERE=pathlib.Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()
names=['A','B','C','D','D-oracle','D-invalid-source','D-mapped','D-mapped-oracle']
rows=[]
for name in names:
    p=HERE/(name+'.json');raw=p.read_bytes();json.loads(raw)
    compressed=gzip.compress(raw,compresslevel=9,mtime=0)
    assert gzip.decompress(compressed)==raw
    output=p.with_suffix('.json.gz');assert not output.exists();output.write_bytes(compressed)
    rows.append(dict(original=p.name,compressed=output.name,originalBytes=len(raw),compressedBytes=len(compressed),originalSha256=sha(raw),compressedSha256=sha(compressed),losslessVerified=True))
(HERE/'journal-manifest.json').write_text(json.dumps(dict(files=rows,originalBytes=sum(r['originalBytes'] for r in rows),compressedBytes=sum(r['compressedBytes'] for r in rows),reason='Full scalar timing and exact input journals support continued Project Control review; gzip is lossless; no depth/HDR/frame buffers retained.'),indent=2)+'\n')
print('Verified lossless journals:',len(rows),'original bytes',sum(r['originalBytes'] for r in rows),'retained bytes',sum(r['compressedBytes'] for r in rows))
