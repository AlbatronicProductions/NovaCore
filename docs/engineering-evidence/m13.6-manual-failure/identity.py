"""Read-only source/deployment/display inventory. Writes only this evidence package."""
import ctypes as c
from ctypes import wintypes as w
import datetime, hashlib, json, pathlib, subprocess

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[2]
PRIOR = HERE.parent / 'm13-final-exit'

def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda: f.read(1048576), b''):
            h.update(block)
    return h.hexdigest()

def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT).decode().strip()

class DisplayDevice(c.Structure):
    _fields_ = [('cb', w.DWORD), ('DeviceName', w.WCHAR * 32),
                ('DeviceString', w.WCHAR * 128), ('StateFlags', w.DWORD),
                ('DeviceID', w.WCHAR * 128), ('DeviceKey', w.WCHAR * 128)]

class DevMode(c.Structure):
    _fields_ = [('dmDeviceName', w.WCHAR * 32), ('dmSpecVersion', w.WORD),
                ('dmDriverVersion', w.WORD), ('dmSize', w.WORD), ('dmDriverExtra', w.WORD),
                ('dmFields', w.DWORD), ('dmPositionX', w.LONG), ('dmPositionY', w.LONG),
                ('dmDisplayOrientation', w.DWORD), ('dmDisplayFixedOutput', w.DWORD),
                ('dmColor', w.SHORT), ('dmDuplex', w.SHORT), ('dmYResolution', w.SHORT),
                ('dmTTOption', w.SHORT), ('dmCollate', w.SHORT), ('dmFormName', w.WCHAR * 32),
                ('dmLogPixels', w.WORD), ('dmBitsPerPel', w.DWORD), ('dmPelsWidth', w.DWORD),
                ('dmPelsHeight', w.DWORD), ('dmDisplayFlags', w.DWORD),
                ('dmDisplayFrequency', w.DWORD), ('dmICMMethod', w.DWORD),
                ('dmICMIntent', w.DWORD), ('dmMediaType', w.DWORD), ('dmDitherType', w.DWORD),
                ('dmReserved1', w.DWORD), ('dmReserved2', w.DWORD),
                ('dmPanningWidth', w.DWORD), ('dmPanningHeight', w.DWORD)]

def displays():
    api = c.WinDLL('user32', use_last_error=True)
    api.EnumDisplayDevicesW.argtypes = [w.LPCWSTR, w.DWORD, c.POINTER(DisplayDevice), w.DWORD]
    api.EnumDisplaySettingsW.argtypes = [w.LPCWSTR, w.DWORD, c.POINTER(DevMode)]
    result = []
    for i in range(64):
        d = DisplayDevice(cb=c.sizeof(DisplayDevice))
        if not api.EnumDisplayDevicesW(None, i, c.byref(d), 0):
            break
        m = DevMode(dmSize=c.sizeof(DevMode))
        ok = bool(api.EnumDisplaySettingsW(d.DeviceName, 0xffffffff, c.byref(m)))
        row = dict(name=d.DeviceName, description=d.DeviceString, flags=d.StateFlags,
                   attached=bool(d.StateFlags & 1), primary=bool(d.StateFlags & 4), modeAvailable=ok)
        if ok:
            row.update(width=m.dmPelsWidth, height=m.dmPelsHeight, hz=m.dmDisplayFrequency,
                       x=m.dmPositionX, y=m.dmPositionY, bpp=m.dmBitsPerPel)
        row['monitors'] = []
        for j in range(32):
            mon = DisplayDevice(cb=c.sizeof(DisplayDevice))
            if not api.EnumDisplayDevicesW(d.DeviceName, j, c.byref(mon), 0):
                break
            row['monitors'].append(dict(name=mon.DeviceName, description=mon.DeviceString,
                                        id=mon.DeviceID, flags=mon.StateFlags))
        result.append(row)
    return result

def main():
    prior = json.loads((PRIOR / 'candidate-deployment-final.json').read_text())
    seal = json.loads((PRIOR / 'closeout.json').read_text())
    paths = git('diff', '--name-only').splitlines()
    out = dict(capturedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
               refs={r: git('rev-parse', r) for r in ['HEAD', 'main', 'origin/main',
                    'm13.5-local-gpu-terrain-working-data', 'm13.5-local-gpu-terrain-working-data^{}']},
               branch=git('branch', '--show-current'), status=git('status', '--short'),
               staged=git('diff', '--cached'), source={p: sha(ROOT / p) for p in paths},
               trackedDiffSha256=hashlib.sha256(subprocess.check_output(['git', 'diff', '--binary'], cwd=ROOT)).hexdigest(),
               previousPackage={p.name: dict(bytes=p.stat().st_size, sha256=sha(p)) for p in sorted(PRIOR.iterdir()) if p.is_file()},
               deployment={}, assets={}, displays=displays(),
               displayLimit='Post-restart desktop enumeration; not proof of failed window placement, driver override, VRR state or displayed cadence.')
    out['sourceMatchesPriorSeal'] = out['source'] == seal['sourceSha256']
    for config in ['Debug', 'Release']:
        folder = ROOT / f'samples/NovaCore.Triangle/bin/{config}/net10.0'
        row = dict(native=sha(folder / 'NovaCore.Native.dll'), managed=sha(folder / 'NovaCore.Triangle.dll'),
                   executable=sha(folder / 'NovaCore.Triangle.exe'), shaders={})
        for p in folder.rglob('*.spv'):
            if p.name in row['shaders']:
                raise RuntimeError('Ambiguous shader location: ' + str(p))
            row['shaders'][p.name] = sha(p)
        row['matchesPriorDeployment'] = all(row[k] == prior['deployment'][config][k] for k in ['native', 'managed', 'executable', 'shaders'])
        out['deployment'][config] = row
    for rel, expected in prior['assets'].items():
        p = ROOT / rel
        actual = dict(bytes=p.stat().st_size, sha256=sha(p))
        out['assets'][rel] = dict(**actual, matchesPrior=actual == expected)
    p = ROOT / 'tools/NovaCore.Launcher/bin/Release/net10.0-windows/NovaCore.Launcher.exe'
    out['launcher'] = dict(path=str(p), bytes=p.stat().st_size, sha256=sha(p))
    (HERE / 'identity.json').write_text(json.dumps(out, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({k: out[k] for k in ['refs', 'branch', 'sourceMatchesPriorSeal', 'displays']}, indent=2))
    print('Deployment matches:', {k: v['matchesPriorDeployment'] for k,v in out['deployment'].items()})

if __name__ == '__main__':
    main()
