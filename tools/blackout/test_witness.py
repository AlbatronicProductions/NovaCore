"""CPU-only tests. All artifacts stay in a new caller-owned build directory."""
import ctypes as C
from ctypes import wintypes as W
import json
import os
from pathlib import Path
import statistics
import struct
import subprocess
import sys
import threading
import time
import unittest
import urllib.error
import urllib.request
import uuid
from unittest.mock import patch

import witness as w

ROOT = Path(__file__).resolve().parents[2] / 'build/blackout-post-m16'
OUT = ROOT / ('witness-tests-' + uuid.uuid4().hex)
MEASURES = {}


def newdir(label):
    return OUT / (label + '-' + uuid.uuid4().hex)


def event(sequence=1):
    words = [0] * 32
    words[0] = sequence; words[4] = 8; words[5] = 1; words[19] = w.MASK
    data = struct.pack('<32Q', *words)
    return data[:240] + struct.pack('<QQ', w.checksum(data[:240]), sequence)


def mapping_bytes(sequence=1):
    data = bytearray(w.PAGE + w.CAPACITY * 256)
    header = [w.MAGIC, 1, 256, w.CAPACITY] + [0] * 15
    header[8] = sequence; header[9] = max(0, sequence - 1); header[10] = sequence
    data[:152] = struct.pack('<19q', *header)
    offset = w.PAGE + ((sequence - 1) % w.CAPACITY) * 256
    data[offset:offset + 256] = event(sequence)
    return data


def child(args):
    return subprocess.Popen([sys.executable, __file__, *args], creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))


def wait_file(path, process, seconds=8):
    end = time.monotonic() + seconds
    while not path.exists() and time.monotonic() < end:
        if process.poll() is not None: raise AssertionError('Child exited before marker')
        time.sleep(.01)
    assert path.exists(), path


def process_ticks(pid):
    k = C.WinDLL('kernel32', use_last_error=True)
    k.OpenProcess.argtypes = [W.DWORD, W.BOOL, W.DWORD]; k.OpenProcess.restype = W.HANDLE
    k.GetProcessTimes.argtypes = [W.HANDLE] + [C.POINTER(W.FILETIME)] * 4
    k.CloseHandle.argtypes = [W.HANDLE]
    handle = k.OpenProcess(0x1000, False, pid)
    values = [W.FILETIME() for _ in range(4)]
    assert handle and k.GetProcessTimes(handle, *(C.byref(v) for v in values))
    k.CloseHandle(handle)
    return ((values[0].dwHighDateTime << 32) | values[0].dwLowDateTime) + 504911232000000000


def mapping_fixture(path, observer_pid):
    path.mkdir(parents=True)
    session = uuid.UUID(path.name) if len(path.name)==32 else uuid.uuid4(); pid = os.getpid(); ticks = process_ticks(pid)
    k = C.WinDLL('kernel32', use_last_error=True)
    k.CreateFileMappingW.argtypes = [W.HANDLE, C.c_void_p, W.DWORD, W.DWORD, W.DWORD, W.LPCWSTR]
    k.CreateFileMappingW.restype = W.HANDLE
    k.MapViewOfFile.argtypes = [W.HANDLE, W.DWORD, W.DWORD, W.DWORD, C.c_size_t]; k.MapViewOfFile.restype = C.c_void_p
    data = mapping_bytes(); data[32:48] = session.bytes_le
    struct.pack_into('<qq', data, 48, pid, ticks)
    handle = k.CreateFileMappingW(W.HANDLE(-1), None, 4, 0, len(data), 'Local\\NovaCoreMinimum-' + session.hex)
    address = k.MapViewOfFile(handle, 0xF001F, 0, 0, len(data)); assert address
    C.memmove(address, bytes(data), len(data))
    meta = dict(session=str(session), producer=pid, startUtcTicks=ticks,
                openedUtcTicks=time.time_ns() // 100 + 621355968000000000,
                openedQpc=1, qpcFrequency=10000000)
    (path / 'session.json').write_text(json.dumps(meta))
    (path / 'observer.json').write_text(json.dumps(dict(session=str(session), pid=observer_pid, startUtcTicks=process_ticks(observer_pid))))
    (path / 'ready.txt').write_text('ready')
    while True: time.sleep(1)


class Tests(unittest.TestCase):
    def test_discovery_snapshot_and_ambiguity(self):
        root=newdir('discovery');root.mkdir();old=root/uuid.uuid4().hex;old.mkdir()
        (old/'session.json').write_text(json.dumps(dict(openedUtcTicks=999999999999999999)))
        (old/'observer.json').write_text('{}')
        witness=w.Witness(newdir('discovery-witness'),root=root)
        try:
            with patch.object(w,'WindowsSource') as source:
                witness.attach();source.assert_not_called()
                first=root/uuid.uuid4().hex;first.mkdir();(first/'session.json').write_text(json.dumps(dict(openedUtcTicks=1)));(first/'observer.json').write_text('{}')
                witness.attach();source.assert_called_once_with(first,None)
                witness.source=None
                second=root/uuid.uuid4().hex;second.mkdir();(second/'session.json').write_text(json.dumps(dict(openedUtcTicks=2)));(second/'observer.json').write_text('{}')
                with self.assertRaisesRegex(ValueError,'Multiple'):witness.attach()
        finally:witness.journal.close()

    @unittest.skipUnless(os.name=='nt','Windows process identities')
    def test_sealed_contract_branches(self):
        path=newdir('contract')/uuid.uuid4().hex
        observer=subprocess.Popen([sys.executable,'-c','import time;time.sleep(60)'],creationflags=subprocess.CREATE_NO_WINDOW)
        producer=child(['--mapping',str(path),str(observer.pid)])
        try:
            wait_file(path/'ready.txt',producer)
            meta=json.loads((path/'session.json').read_text());owner=json.loads((path/'observer.json').read_text())
            hashes={str((path/f'image{i}.dll').resolve()).lower():str(i)*64 for i in range(5)}
            contract=dict(producerImage=sys.executable,observerImage=sys.executable,incidentIdentity=hashes,sha256='a'*64)
            meta.update(schema=2,reservedBytes=w.RESERVED_BYTES,identity=[dict(path=p,sha256=h) for p,h in hashes.items()]);owner['schema']=1
            def publish():
                (path/'session.json').write_text(json.dumps(meta));(path/'observer.json').write_text(json.dumps(owner))
            publish()
            for name,size in [('bank0.bin',41947136),('bank1.bin',41947136),('head0.bin',4096),('head1.bin',4096)]:
                with (path/name).open('xb') as file:file.truncate(size)
            source=w.WindowsSource(path,contract)
            try:self.assertTrue(source.sample()['admissionVerified'])
            finally:source.close()
            for field,value in [('schema',3),('reservedBytes',1),('identity',[])]:
                original=meta[field];meta[field]=value;publish()
                with self.assertRaises(ValueError):w.WindowsSource(path,contract)
                meta[field]=original;publish()
            owner['schema']=2;publish()
            with self.assertRaises(ValueError):w.WindowsSource(path,contract)
            owner['schema']=1;publish()
            with self.assertRaisesRegex(ValueError,'image mismatch'):w.WindowsSource(path,dict(contract,producerImage=str(path/'wrong.exe')))
            with (path/'head1.bin').open('r+b') as file:file.truncate(4095)
            with self.assertRaisesRegex(ValueError,'allocation mismatch'):w.WindowsSource(path,contract)
        finally:
            for process in (producer,observer):
                if process.poll() is None:process.kill();process.wait(5)

    @unittest.skipUnless(os.name=='nt','Windows process identities')
    def test_independent_beacon_survives_primary_loss(self):
        path=newdir('independent-beacon');bookmark=OUT/'test-bookmark.json'
        primary=subprocess.Popen([sys.executable,str(Path(w.__file__)),'--fixture','--bind','127.0.0.1','--port','0','--beacon-port','0','--duration','10','--output',str(path),'--bookmark-file',str(bookmark)],creationflags=subprocess.CREATE_NO_WINDOW,stdout=subprocess.DEVNULL)
        try:
            wait_file(path/'startup.json',primary);info=json.loads((path/'startup.json').read_bytes())
            token=json.loads(bookmark.read_bytes())['token'];self.assertEqual(w.bookmark_token(bookmark),token)
            with urllib.request.urlopen(info['url']) as response:html=response.read().decode('utf-8-sig')
            self.assertIn(info['receiverBuild'],html);self.assertNotIn('__RECEIVER_BUILD__',html)
            for asset in ['receiver_core.js','receiver_app.js']:
                with urllib.request.urlopen(info['url']+asset) as response:self.assertGreater(len(response.read()),1000)
            nonce=uuid.uuid4().hex
            with urllib.request.urlopen(info['beacon']['url']+'sample/'+nonce) as response:
                before=json.load(response);self.assertEqual(response.headers['Access-Control-Allow-Origin'],info['url'].split('/'+token)[0])
            self.assertEqual(before['nonce'],nonce);self.assertEqual(before['pid'],info['beacon']['pid'])
            primary.kill();primary.wait(5)
            nonce=uuid.uuid4().hex
            with urllib.request.urlopen(info['beacon']['url']+'sample/'+nonce,timeout=2) as response:after=json.load(response)
            self.assertEqual(after['nonce'],nonce);self.assertGreater(int(after['serverMonotonicNs']),int(before['serverMonotonicNs']))
            with self.assertRaises(urllib.error.URLError):urllib.request.urlopen(info['url']+'sample/'+nonce,timeout=1)
            MEASURES['independentBeaconAfterPrimaryKill']=dict(witness=info['witness'],before=before,after=after)
            # Own child has a ten-second bound. Preserve its startup identity and let it exit itself.
        finally:
            if primary.poll() is None:primary.kill();primary.wait(5)

    def test_exact_mapping_and_overflow_semantics(self):
        data = mapping_bytes()
        result = w.sample_mapping(lambda a, n: bytes(data[a:a + n]))
        self.assertTrue(result['eventValid']); self.assertEqual(result['latestEventWords'][19], str(w.MASK))
        self.assertEqual(result['durable'], '1'); self.assertEqual(result['consumed'], '0')
        # Durable published before Consumed is legal. Overflow is evidence, not a clean state.
        struct.pack_into('<q', data, 12 * 8, 1); struct.pack_into('<q', data, 16 * 8, 3383)
        result = w.sample_mapping(lambda a, n: bytes(data[a:a + n]))
        self.assertEqual(result['producerFault'], '1'); self.assertEqual(result['dropped'], '3383')
        data[w.PAGE + 80] ^= 1
        self.assertFalse(w.sample_mapping(lambda a, n: bytes(data[a:a + n]))['eventValid'])

    def test_concurrent_record_and_header(self):
        data = mapping_bytes(); calls = 0
        def read(a, n):
            nonlocal calls
            value = bytes(data[a:a + n]); calls += 1
            if calls == 3: struct.pack_into('<Q', data, w.PAGE + 248, 8193)
            return value
        self.assertFalse(w.sample_mapping(read)['eventValid'])
        data = mapping_bytes()
        def advancing(a, n):
            value = bytes(data[a:a + n])
            if a == w.PAGE: struct.pack_into('<q', data, 64, 2)
            return value
        result = w.sample_mapping(advancing)
        self.assertFalse(result['stableProduced']); self.assertEqual(result['producedAfter'], '2')
        self.assertTrue(result['eventValid'])

    def test_slots_corruption_identity_conflict(self):
        path = newdir('slots'); journal = w.SlotJournal(path)
        sample = dict(schema=w.SCHEMA, witness='a', serial=1, observation={'value': str(w.MASK)})
        journal.write(sample); journal.write(dict(sample, serial=2)); journal.close()
        self.assertEqual(w.SlotJournal.recover(path, 'a')['sample']['serial'], 2)
        self.assertIsNone(w.SlotJournal.recover(path, 'wrong')['sample'])
        damaged = bytearray((path / 'slot0.bin').read_bytes()); damaged[200] ^= 1
        (path / 'slot0.bin').write_bytes(damaged)
        recovered = w.SlotJournal.recover(path, 'a')
        self.assertEqual(recovered['sample']['serial'], 1); self.assertTrue(recovered['errors'])
        # Truncation cannot be mistaken for a fully durable slot.
        (path / 'slot1.bin').write_bytes(b'torn')
        self.assertIsNone(w.SlotJournal.recover(path, 'a')['sample'])
        conflict = newdir('conflict'); journal = w.SlotJournal(conflict)
        journal.write(sample); journal.close()
        page = (conflict / 'slot1.bin').read_bytes(); original = json.loads(page[8:8 + struct.unpack_from('<I', page, 4)[0]])
        original['observation'] = {'value': 'other'}; payload = w.canonical(original)
        modified = struct.pack('<II', 1, len(payload)) + payload
        modified += bytes(w.PAGE - 32 - len(modified)); modified += bytes.fromhex(w.digest(modified))
        (conflict / 'slot0.bin').write_bytes(modified)
        self.assertIsNone(w.SlotJournal.recover(conflict, 'a')['sample'])

    def test_abrupt_writer_termination(self):
        for stage in ('before-write', 'after-write', 'before-fsync', 'after-fsync'):
            path = newdir(stage); process = child(['--cut', str(path), stage])
            wait_file(path / 'cut.txt', process); process.kill(); process.wait(5)
            recovered = w.SlotJournal.recover(path, 'cut')
            self.assertIsNotNone(recovered['sample'])
            self.assertIn(recovered['sample']['serial'], (1, 3))
            if stage == 'after-fsync': self.assertEqual(recovered['sample']['serial'], 3)
            # Coalescing 1 -> 3 must not overwrite the last good slot.
            self.assertEqual(json.loads((path / 'slot1.bin').read_bytes()[8:8 + struct.unpack_from('<I', (path / 'slot1.bin').read_bytes(), 4)[0]])['serial'], 1)

    def test_blocked_disk_independent_http_and_bound(self):
        path = newdir('blocked'); witness = w.Witness(path, w.FixtureSource())
        entered = threading.Event(); release = threading.Event(); write = witness.journal.write
        def blocking(sample): entered.set(); release.wait(8); write(sample)
        witness.journal.write = blocking
        server = w.make_server(witness, '127.0.0.1', 0)
        thread = threading.Thread(target=server.serve_forever, daemon=True); thread.start(); witness.start()
        self.assertTrue(entered.wait(3))
        for _ in range(100): witness.sample_once()
        before = time.perf_counter()
        with urllib.request.urlopen(f'http://127.0.0.1:{server.server_port}/{witness.token}/sample/abc', timeout=2) as response: data = json.load(response)
        self.assertEqual(data['nonce'], 'abc'); self.assertEqual(data['localCommittedSerial'], 0)
        self.assertGreaterEqual(data['sample']['serial'], 100)
        self.assertLess(time.perf_counter() - before, 2)
        with self.assertRaises(urllib.error.HTTPError): urllib.request.urlopen(f'http://127.0.0.1:{server.server_port}/wrong/sample/abc')
        release.set(); time.sleep(.1); result = witness.finish(); server.shutdown(); server.server_close()
        self.assertFalse(result['writerStillRunning']); self.assertGreater(result['skipped'], 0)
        self.assertLess(sum(p.stat().st_size for p in path.iterdir()), 10000)

    def test_disk_failure_and_stale_sampler_are_visible(self):
        witness = w.Witness(newdir('disk-failure'), w.FixtureSource())
        def failure(_): raise OSError('synthetic disk unavailable')
        witness.journal.write = failure
        witness.start()
        deadline = time.monotonic() + 2
        while witness.disk_error is None and time.monotonic() < deadline: time.sleep(.005)
        self.assertIn('synthetic disk unavailable', witness.response('1')['diskError'])
        self.assertEqual(witness.committed, 0)
        first = witness.response('2'); second = witness.response('3')
        self.assertGreater(int(second['serverMonotonicNs']), int(first['serverMonotonicNs']))
        witness.sample_once()
        self.assertGreater(witness.response('4')['sample']['serial'], first['sample']['serial'])
        self.assertFalse(witness.finish()['writerStillRunning'])
        # A live HTTP responder can truthfully expose an unchanged sampler serial.
        witness = w.Witness(newdir('stale-sampler'), w.FixtureSource())
        witness.sample_once()
        first = witness.response('a'); second = witness.response('b')
        self.assertEqual(first['sample']['serial'], second['sample']['serial'])
        self.assertGreater(int(second['serverMonotonicNs']), int(first['serverMonotonicNs']))
        witness.journal.close()

    def test_flush_cost_and_fixed_storage(self):
        path = newdir('flush-cost'); journal = w.SlotJournal(path); durations = []
        cpu = time.process_time(); wall = time.perf_counter()
        for serial in range(1, 301):
            sample = dict(schema=w.SCHEMA, witness='cost', serial=serial,
                          observation=w.sample_mapping(lambda a, n, data=mapping_bytes(): bytes(data[a:a + n])))
            started = time.perf_counter_ns(); journal.write(sample)
            durations.append((time.perf_counter_ns() - started) / 1e6)
        cpu = time.process_time() - cpu; wall = time.perf_counter() - wall
        journal.close(); ordered = sorted(durations)
        self.assertEqual(w.SlotJournal.recover(path, 'cost')['sample']['serial'], 300)
        self.assertEqual(sum(p.stat().st_size for p in path.iterdir()), 8192)
        MEASURES['localSlotCommitMs'] = dict(count=len(ordered), median=statistics.median(ordered),
                    p95=ordered[284], p99=ordered[296], maximum=max(ordered), totalCpuSeconds=cpu, totalWallSeconds=wall,
                    semantics='Synthetic back-to-back writes on output volume; not a native frame-cost or power-loss guarantee')

    @unittest.skipUnless(os.name == 'nt', 'Windows process/mapping contract')
    def test_real_mapping_incarnation_and_process_death(self):
        path = newdir('mapping'); observer = subprocess.Popen([sys.executable, '-c', 'import time; time.sleep(60)'], creationflags=subprocess.CREATE_NO_WINDOW)
        producer = child(['--mapping', str(path), str(observer.pid)])
        source = None
        try:
            wait_file(path / 'ready.txt', producer); source = w.WindowsSource(path)
            initial = source.read(0, w.PAGE + w.CAPACITY * 256)
            durations = []
            for _ in range(250):
                start = time.perf_counter_ns(); result = source.sample(); durations.append((time.perf_counter_ns() - start) / 1e6)
            self.assertEqual(source.read(0, len(initial)), initial, 'Witness must never mutate mapping')
            self.assertTrue(result['eventValid']); self.assertEqual(result['producerProcess'], 'alive')
            packet = dict(schema=w.SCHEMA, witness='size', serial=1, observation=result)
            self.assertLess(len(w.canonical(packet)), w.PAGE - 40)
            original = (path / 'session.json').read_text()
            meta = json.loads(original); meta['startUtcTicks'] += 1
            (path / 'session.json').write_text(json.dumps(meta))
            with self.assertRaisesRegex(ValueError, 'incarnation mismatch'): w.WindowsSource(path)
            meta['session'] = str(uuid.uuid4())
            (path / 'session.json').write_text(json.dumps(meta))
            with self.assertRaisesRegex(ValueError, 'session mismatch'): w.WindowsSource(path)
            (path / 'session.json').write_text(original)
            original_read = source.read
            def corrupt_identity(a, n):
                data = original_read(a, n)
                return bytes([data[0] ^ 1]) + data[1:] if a == 0 and n == 64 else data
            source.read = corrupt_identity
            with self.assertRaisesRegex(ValueError, 'immutable identity changed'): source.sample()
            source.read = original_read
            observer.kill(); observer.wait(5)
            self.assertEqual(source.sample()['observerProcess'], 'exited')
            self.assertEqual(source.sample()['producerProcess'], 'alive')
            producer.kill(); producer.wait(5)
            self.assertEqual(source.sample()['producerProcess'], 'exited')
            ordered = sorted(durations)
            MEASURES['realMappingSampleMs'] = dict(count=len(ordered), median=statistics.median(ordered), p95=ordered[237], p99=ordered[247], maximum=max(ordered))
        finally:
            if source: source.close()
            for process in (producer, observer):
                if process.poll() is None: process.kill(); process.wait(5)


if __name__ == '__main__':
    if len(sys.argv) > 1 and sys.argv[1] == '--cut':
        path = Path(sys.argv[2]); stage = sys.argv[3]; journal = w.SlotJournal(path)
        sample = dict(schema=w.SCHEMA, witness='cut', serial=1)
        journal.write(sample)
        def cut(at):
            if at == stage:
                (path / 'cut.txt').write_text(stage)
                while True: time.sleep(1)
        journal.write(dict(sample, serial=3), cut)
    elif len(sys.argv) > 1 and sys.argv[1] == '--mapping':
        mapping_fixture(Path(sys.argv[2]), int(sys.argv[3]))
    else:
        OUT.mkdir(parents=True)
        suite = unittest.defaultTestLoader.loadTestsFromTestCase(Tests)
        result = unittest.TextTestRunner(verbosity=2).run(suite)
        (OUT / 'results.json').write_text(json.dumps(dict(passed=result.wasSuccessful(), tests=result.testsRun,
             failures=len(result.failures), errors=len(result.errors), skipped=len(result.skipped), measurements=MEASURES), indent=2))
        print('Evidence:', OUT)
        raise SystemExit(not result.wasSuccessful())
