"""Explicit, bounded M13.2 reproduction in a disposable checkout of the candidate.

prepare instruments only the two files named by instrumentation.patch, builds
baseline/candidate snapshots, and deploys the instrumented Release sample.
restore reverses that patch; validate.py build then restores normal deployment.
Never run this over unrelated modifications. Timing and capture are separate.
"""
import sys
sys.dont_write_bytecode = True
import json, os, shutil, subprocess
from pathlib import Path
from run import ROOT, HERE, OUT, SOURCE, run

BASELINE = 'fade1384c1c7df93d954e7223b1cc8f17db17f98'
NATIVE = ROOT/'native/NovaCore.Native/NovaCoreNative.cpp'
START = '    // Context diagnostics are immutable.'
END = '    a.Log(NC_LOG_VULKAN,ordinaryShading?'

def command(args): subprocess.run(args, cwd=ROOT, check=True)

def build_native():
    command(['pwsh', '-NoProfile', '-Command',
        "& 'C:/Program Files/Microsoft Visual Studio/18/Community/Common7/Tools/Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation\n"
        'cmake --build build/native-ninja-release\nexit $LASTEXITCODE'])

def prepare():
    OUT.mkdir(parents=True, exist_ok=True)
    patch = str(HERE/'instrumentation.patch')
    command(['git','apply','--check',patch]); command(['git','apply',patch])
    candidate = NATIVE.read_text(encoding='utf-8')
    start = candidate.index(START); end = candidate.index('\n', candidate.index(END, start)) + 1
    try:
        NATIVE.write_text(candidate[:start]+candidate[end:], encoding='utf-8')
        build_native()
        shutil.copy2(ROOT/'build/native-ninja-release/NovaCore.Native.dll', OUT/'baseline-native.dll')
    finally:
        # A fresh write is intentional: restoring an old mtime can fool Ninja.
        NATIVE.write_text(candidate, encoding='utf-8')
    source = subprocess.check_output(['git','show',BASELINE+':native/NovaCore.Native/shaders/planetary_production.frag'],cwd=ROOT)
    baseline = OUT/'baseline.frag'; baseline.write_bytes(source)
    command([str(Path(os.environ['VULKAN_SDK'])/'Bin/glslc.exe'),'-I',str(SOURCE),str(baseline),'-o',str(OUT/'baseline-fragment.spv')])
    build_native()
    command(['dotnet','build',str(ROOT/'samples/NovaCore.Triangle/NovaCore.Triangle.csproj'),'-c','Release','--no-restore'])

if __name__ == '__main__':
    mode = sys.argv[1]
    if mode == 'prepare': prepare()
    elif mode == 'restore':
        patch = str(HERE/'instrumentation.patch')
        command(['git','apply','--reverse','--check',patch]); command(['git','apply','--reverse',patch])
    elif mode == 'timing':
        for pose in 'ABCDE':
            for probe in ('baseline','candidate'): run(pose,probe,f'timing-{pose}-{probe}')
    elif mode == 'repeats':
        for repeat in (2,3):
            for probe in (('candidate','baseline') if repeat==2 else ('baseline','candidate')):
                run('A',probe,f'timing-A-{probe}-repeat{repeat}')
        for probe in ('candidate','baseline'): run('B',probe,f'timing-B-{probe}-repeat2')
    elif mode == 'parity':
        from capture_analysis import Parity
        for pose in 'ABCDE':
            compare=Parity()
            for probe in ('baseline','candidate'): run(pose,probe,f'parity-{pose}-{probe}',capture='tes',callback=compare)
        for diagnostic in ('owners','boundaries'):
            compare=Parity()
            for probe in ('baseline','candidate','negative'):
                run('E',probe,f'diagnostic-{diagnostic}-{probe}',capture='tes',callback=compare,diagnostic=diagnostic)
        compare=Parity()
        for probe in ('baseline','candidate'):
            run('A',probe,f'bootstrap-{probe}',capture='tes',callback=compare,capture_frame=8)
        compare=Parity()
        for probe in ('baseline','candidate'):
            run('A',probe,f'physical-modifier-{probe}',capture='tes',callback=compare,diagnostic='physical-modifier')
    else: raise ValueError(mode)
