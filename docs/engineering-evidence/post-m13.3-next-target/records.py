"""Lossless column storage for bounded numerical evidence; no sample removal."""
import json,pathlib
ROW_FIELDS=['frameRows','hostRows','gpuRows','cpuRows','drawRows','clippingRows']
def unpack(value):
    if not isinstance(value,dict) or value.get('_columnar')!=1:return value
    return [{**value['constants'],**{k:v[i] for k,v in value['columns'].items()}} for i in range(value['count'])]
def read(path):
    data=json.loads(pathlib.Path(path).read_text(encoding='utf-8'))
    if isinstance(data,dict):
        for key in ROW_FIELDS:
            if key in data:data[key]=unpack(data[key])
    return data
def pack(rows):
    if not isinstance(rows,list) or not rows or not all(isinstance(r,dict) for r in rows):return rows
    keys=list(rows[0])
    if not all(set(r)==set(keys) for r in rows):return rows
    constant={k:rows[0][k] for k in keys if all(r[k]==rows[0][k] for r in rows)}
    encoded={'_columnar':1,'count':len(rows),'constants':constant,'columns':{k:[r[k] for r in rows] for k in keys if k not in constant}}
    assert unpack(encoded)==rows
    return encoded
if __name__=='__main__':
    folder=pathlib.Path(__file__).resolve().parent
    before=after=0
    for path in folder.glob('*.json'):
        data=read(path)
        if not isinstance(data,dict) or 'frameRows' not in data:continue
        before+=path.stat().st_size
        for key in ROW_FIELDS:
            if key in data:data[key]=pack(data[key])
        # Compact JSON whitespace only; full sample identity and values retained.
        path.write_text(json.dumps(data,separators=(',',':'))+'\n',encoding='utf-8')
        assert read(path)=={**data,**{k:unpack(data[k]) for k in ROW_FIELDS if k in data}}
        after+=path.stat().st_size
    print({'recordBytesBefore':before,'recordBytesAfter':after,'samplesRemoved':0})
