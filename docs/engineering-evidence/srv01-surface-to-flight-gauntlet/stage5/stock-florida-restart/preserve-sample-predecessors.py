"""Recover the three exact predecessor sample inputs without writing candidate source.
Only mechanically undo this restart's additions, then require the pre-edit SHA-256.
"""
from pathlib import Path
import hashlib, json, re, zipfile

root=Path('E:/NovaCore'); evidence=Path(__file__).parent
seals=json.loads((evidence/'preflight.json').read_text())['sourceSeals']
expected={s['path']:s['sha256'] for s in seals}
recovered={}
for name in ['Program.cs','SampleOptions.cs','StockAssemblyDevelopmentScene.cs']:
    path='samples/NovaCore.Triangle/'+name
    text=(root/path).read_text()
    if name=='Program.cs':
        text=text.replace(' or "srv01-florida-support"','').replace(',options.Scene=="srv01-florida-support"','')
    elif name=='SampleOptions.cs':
        text=text.replace('or"srv01-florida-support"','')
    else:
        text=text.replace('using NovaCore.Core.Surface;\n','')
        text=text.replace('    private readonly bool floridaSupport;\n','')
        text=text.replace(',bool floridaSupport=false','')
        text=text.replace('        if(floridaSupport&&(!supportedContact||poweredSupport))throw new ArgumentException("Florida qualification requires stock engine-OFF contact.");\n','')
        text=text.replace('        this.floridaSupport=floridaSupport;\n','')
        text=text.replace('var launch=floridaSupport?AssemblyLaunch.CreateFloridaSupported(d,definition,"florida-ground",PrepareFlorida()):\n            poweredSupport?', 'var launch=poweredSupport?')
        text=re.sub(r'Console.WriteLine\(floridaSupport\?"Stock SRV-01 Florida graded ground:[^\n]+\n            poweredSupport\?', 'Console.WriteLine(poweredSupport?',text)
        text=re.sub(r'    private static AssemblyFloridaSite PrepareFlorida\(\)\n    \{.*?\n    \}\n','',text,flags=re.S)
        text=text.replace('floridaSupport?"Florida graded ground (site-local)":','')
    choices=[text.encode(),text.replace('\n','\r\n').encode()]
    matches=[b for b in choices if hashlib.sha256(b).hexdigest().upper()==expected[path]]
    assert len(matches)==1, 'Predecessor mismatch: '+path
    recovered[path]=matches[0]
archive=evidence/'prior-sample-inputs.zip'
if archive.exists():
    with zipfile.ZipFile(archive) as z:
        assert set(z.namelist())==set(recovered)
        for path,data in recovered.items(): assert z.read(path)==data
else:
    with zipfile.ZipFile(archive,'x',compression=zipfile.ZIP_DEFLATED) as z:
        for path,data in recovered.items(): z.writestr(path,data)
print('PASS: 3 exact pre-edit sample files retained; current source not modified.')
