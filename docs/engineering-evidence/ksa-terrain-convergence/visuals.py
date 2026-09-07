"""Reduce exact same-pose GPU pixel readbacks to bounded comparisons."""
import hashlib,json,pathlib,sys
import numpy as np
from PIL import Image,ImageDraw
ROOT=pathlib.Path(__file__).resolve().parents[3];HERE=pathlib.Path(__file__).resolve().parent
OUT=ROOT/'build/ksa-terrain-convergence';N=3440*1440
sys.path.insert(0,str(HERE.parent/'near-surface-performance'))
from analyze import fields
def read(label):
    p=OUT/label/'pixels.bin';b=p.read_bytes()
    return (np.frombuffer(b,np.float32,count=N).copy(),np.frombuffer(b,np.float16,count=N*4,offset=N*4).astype(np.float32),np.frombuffer(b,np.uint8,count=N*4,offset=N*12).reshape(1440,3440,4).copy(),hashlib.sha256(b).hexdigest())
def picture(p):return Image.fromarray(p[:,:,[2,1,0]]).resize((1376,576),Image.Resampling.LANCZOS)
rows=[];sheet=Image.new('RGB',(2752,1872),(22,24,30));draw=ImageDraw.Draw(sheet)
for index,(name,left,right) in enumerate([
    ('Colorado coarse land / daylight','gate4-land-coarse-control','gate4-land-coarse-candidate'),
    ('Florida authored facility / daylight','gate3-florida-day-baseline','gate3-florida-day-candidate'),
    ('Colorado near land / daylight','gate4-land-day-control','gate4-land-day-candidate')]):
    a,b=read(left),read(right);pix=np.abs(a[2][:,:,:3].astype(np.int16)-b[2][:,:,:3].astype(np.int16));hd=np.abs(a[1]-b[1]);dd=np.abs(a[0]-b[0])
    ca=fields(next(x for x in (OUT/left/'runtime.log').read_text().splitlines() if 'P2S5F directional visibility: submittedFrame=175;' in x));cb=fields(next(x for x in (OUT/right/'runtime.log').read_text().splitlines() if 'P2S5F directional visibility: submittedFrame=175;' in x))
    rows.append(dict(name=name,baseline=left,candidate=right,hashes=[a[3],b[3]],camera=[ca.get('cameraBody'),cb.get('cameraBody')],levels=[ca.get('level'),cb.get('level')],depthChanged=int(np.count_nonzero(dd)),depthAbsMax=float(dd.max()),hdrChanged=int(np.count_nonzero(hd)),hdrAbsMax=float(hd.max()),hdrRms=float(np.sqrt(np.mean(hd*hd))),rgbChangedPixels=int(np.count_nonzero(np.any(pix>0,axis=2))),rgbMeanAbs=float(pix.mean()),rgbAbsMax=int(pix.max()),rgbP99=float(np.quantile(pix,.99))))
    y=index*624;draw.text((8,y+8),name+' | BANKED SHADERS',(240,240,245));draw.text((1384,y+8),'CANDIDATE',(240,240,245));sheet.paste(picture(a[2]),(0,y+36));sheet.paste(picture(b[2]),(1376,y+36))
sheet.save(HERE/'representative-comparison.jpg',quality=91)
(HERE/'visual-comparison.json').write_text(json.dumps(rows,indent=2)+'\n')
print(json.dumps(rows,indent=2))
