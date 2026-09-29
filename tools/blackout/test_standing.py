"""Standing lifetime/process tests; synthetic sources only, never NovaCore."""
import json
import os
from pathlib import Path
import subprocess
import sys
import time
import urllib.request
import uuid
from lifetime import wait_until_stopped

HERE=Path(__file__).resolve().parent
OUT=HERE.parents[1]/'build/blackout-post-m16'/('standing-host-'+uuid.uuid4().hex)
OUT.mkdir(parents=True)
checks=[]


def check(name,value):
    assert value,name
    checks.append(name)


class ClockStop:
    def __init__(self,stop_at):self.now=0;self.stop_at=stop_at;self.waits=[]
    def is_set(self):return self.now>=self.stop_at
    def wait(self,seconds):
        assert 0<seconds<=.5
        self.waits.append(seconds);self.now+=seconds


def launch(name,duration=0):
    output=OUT/name;log=(OUT/(name+'.log')).open('wb')
    command=[sys.executable,str(HERE/'witness.py'),'--fixture','--bind','127.0.0.1','--port','0','--beacon-port','0','--duration',str(duration),'--output',str(output)]
    p=subprocess.Popen(command,stdout=log,stderr=subprocess.STDOUT,creationflags=subprocess.CREATE_NO_WINDOW)
    end=time.monotonic()+10
    while not (output/'startup.json').exists():
        if p.poll() is not None or time.monotonic()>end:raise AssertionError('No startup: '+str(output))
        time.sleep(.02)
    return p,log,output,json.loads((output/'startup.json').read_bytes())


def sample(url):
    nonce=uuid.uuid4().hex
    with urllib.request.urlopen(url+'sample/'+nonce,timeout=3) as r:value=json.load(r)
    assert value['nonce']==nonce
    return value


def stop(path,beacon_only=False):
    return subprocess.run(['pwsh','-NoProfile','-File',str(HERE/'Stop-Witness.ps1'),'-Startup',str(path)]+(['-BeaconOnly'] if beacon_only else []),capture_output=True,text=True,timeout=10)


try:
    mock=ClockStop(86401);wait_until_stopped(mock,0,lambda:mock.now)
    check('standing loop passes 1200, 1800 and 86400 seconds with interruptible positive waits',mock.now==86401)
    mock=ClockStop(86401);wait_until_stopped(mock,10,lambda:mock.now)
    check('finite fixture deadline still ends at ten seconds',mock.now==10)
    forbidden=OUT/'forbidden-real'
    r=subprocess.run([sys.executable,str(HERE/'witness.py'),'--output',str(forbidden),'--duration','1200'],capture_output=True,text=True)
    check('finite real duration rejected before touching runtime or output',r.returncode!=0 and not forbidden.exists())
    p,log,folder,info=launch('standing')
    try:
        first=sample(info['url']);time.sleep(1.2);second=sample(info['url'])
        check('standing packets publish no expiry and advancing sampler/writer',second['remainingSeconds'] is None and second['sample']['serial']>first['sample']['serial'] and second['localCommittedSerial']>first['localCommittedSerial'])
        forged=dict(info,startUtcTicks=str(int(info['startUtcTicks'])+1));wrong=OUT/'wrong-startup.json';wrong.write_text(json.dumps(forged))
        r=stop(wrong);check('PID birth mismatch refuses stop and leaves both helpers alive',r.returncode!=0 and p.poll() is None and sample(info['beacon']['url'])['pid']==info['beacon']['pid'])
        # An occupied primary port must never adopt/kill the existing owner.
        port=info['url'].split(':')[2].split('/')[0]
        r=subprocess.run([sys.executable,str(HERE/'witness.py'),'--fixture','--output',str(OUT/'occupied'),'--port',port],capture_output=True,text=True,timeout=5)
        check('occupied port fails without replacing the standing owner',r.returncode!=0 and sample(info['url'])['witness']==info['witness'])
        p.kill();p.wait(5)
        one=sample(info['beacon']['url']);time.sleep(.05);two=sample(info['beacon']['url'])
        check('abrupt primary loss leaves exact independent CPU incarnation answering fresh nonces',one['startUtcTicks']==two['startUtcTicks']==info['beacon']['startUtcTicks'] and int(two['serverMonotonicNs'])>int(one['serverMonotonicNs']))
        forged['pid']=os.getpid();wrong.write_text(json.dumps(forged))
        r=stop(wrong);check('reused primary identity refuses to kill unrelated current process',r.returncode!=0)
        r=stop(wrong,True);check('explicit beacon-only stop bypasses reused primary while checking beacon birth',r.returncode==0)
        try:sample(info['beacon']['url']);stopped=False
        except Exception:stopped=True
        check('orphan endpoint no longer answers after explicit stop',stopped)
    finally:
        if p.poll() is None:p.kill();p.wait(5)
        stop(folder/'startup.json');log.close()
    p,log,folder,info=launch('startup-window')
    try:
        p.kill();p.wait(5)
        r=stop(folder/'beacon-startup.json',True)
        check('standalone beacon publication suffices for a startup-window orphan',r.returncode==0)
    finally:
        if p.poll() is None:p.kill();p.wait(5)
        stop(folder/'beacon-startup.json',True);log.close()
    p,log,folder,info=launch('finite',10)
    try:
        p.wait(15);check('finite fixture exits and closes owned helpers normally',p.returncode==0 and (folder/'finished.json').exists())
        try:sample(info['beacon']['url']);closed=False
        except Exception:closed=True
        check('normal completion closes independent beacon',closed)
    finally:
        if p.poll() is None:p.kill();p.wait(5)
        stop(folder/'startup.json');log.close()
    result=dict(passed=True,checks=checks,scope='CPU synthetic sources and accelerated lifetime clock; no NovaCore launch')
    (OUT/'results.json').write_text(json.dumps(result,indent=2));print(json.dumps(dict(output=str(OUT),**result),indent=2))
except BaseException:
    print('Evidence retained at',OUT)
    raise
