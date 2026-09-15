"""Reproduce this bounded proof in a NEW output directory. Does not bank or correct code."""
from pathlib import Path
import hashlib,json,subprocess,sys

here=Path(__file__).resolve().parent
repo=Path(sys.argv[1]).resolve();out=Path(sys.argv[2]).resolve()
if out.exists():raise SystemExit('Refuse existing output directory')
identity=json.loads((here/'identity.json').read_text())
for record in identity['candidateInputs']:
    actual=hashlib.sha256((repo/record['path']).read_bytes()).hexdigest().upper()
    if actual!=record['sha256']:raise SystemExit('Input fingerprint mismatch: '+record['path'])
out.mkdir(parents=True)

def run(args,log):
    with (out/log).open('x',encoding='utf-8') as f:
        r=subprocess.run(args,cwd=repo,stdout=f,stderr=subprocess.STDOUT)
    if r.returncode:raise RuntimeError('STOP: '+log)

run([sys.executable,str(here/'reference.py'),str(repo),str(out/'reference-results')],'reference.txt')
run(['dotnet','build',str(here/'TinyActiveCandidate.csproj'),'-c','Debug','-p:ContinuousIntegrationBuild=true','--nologo','-v:minimal'],'build.txt')
dll=here/'bin/Debug/net10.0/TinyActiveCandidate.dll'
with (out/'candidate.txt').open('x',encoding='utf-8') as log:
    p=subprocess.Popen(['dotnet',str(dll),str(repo),str(out/'candidate-results')],cwd=repo,
                       stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True)
    try:
        for line in p.stdout:
            log.write(line);log.flush();print(line.rstrip())
            command=None
            if line.startswith('TINY_PROPOSALS_READY'):
                run([sys.executable,str(here/'compare_tiny.py'),str(out/'reference-results'),str(out/'candidate-results'),str(out/'tiny-comparison.json')],'tiny-check.txt')
                command='INSTALL'
            elif line.startswith('COAST_INPUTS_READY'):
                run([sys.executable,str(here/'coast_reference.py'),str(out)],'coast-reference.txt')
                command='COAST'
            elif line.startswith('COAST_PROPOSALS_READY'):
                # The retained source should refuse. An unexpected result is not permission
                # to continue or change the declared acceptance bars.
                command='STOP'
            if command:p.stdin.write(command+'\n');p.stdin.flush()
        code=p.wait()
    except BaseException:
        if p.poll() is None:p.stdin.write('STOP\n');p.stdin.flush();p.wait(timeout=30)
        raise
failure=json.loads((out/'candidate-results/failure.json').read_text())
if code!=1 or failure['error']!='coast candidate admission baseline: BasisRefusal':
    raise SystemExit('Unexpected outcome; stop for Project Control')
run([sys.executable,str(here/'admission_witness.py'),str(out)],'admission-witness.txt')
print('Reproduced: tiny pair/install PASS, first coast BasisRefusal. No retry or correction.')
