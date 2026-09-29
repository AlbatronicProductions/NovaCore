"""Developer-only independent CPU witness. Never launches or modifies NovaCore.

Reads the existing minimum-recorder mapping; owns only its output directory and
token-gated read-only HTTP endpoint. Local samples are not recorder ACKs. Missing
samples or network replies never prove a causal owner. No GPU APIs are loaded.
"""
import argparse
import base64
import ctypes as C
from ctypes import wintypes as W
import hashlib
import http.server
import json
import os
from pathlib import Path
import secrets
import shutil
import subprocess
import struct
import threading
import time
import uuid
import sys

PAGE = 4096
CAPACITY = 8192
MASK = (1 << 64) - 1
MAGIC = 0x314D554D494D434E
SCHEMA = 'NovaCore.BlackoutWitness/1'
RESERVED_BYTES = 87138304


def digest(data):
    return hashlib.sha256(data).hexdigest()


def canonical(value):
    return json.dumps(value, sort_keys=True, separators=(',', ':')).encode()


def checksum(data):
    value = 14695981039346656037
    for byte in data:
        value = ((value ^ byte) * 1099511628211) & MASK
    return value


def valid_event(data, sequence):
    return (len(data) == 256 and struct.unpack_from('<Q', data)[0] == sequence
            and struct.unpack_from('<Q', data, 248)[0] == sequence
            and struct.unpack_from('<Q', data, 240)[0] == checksum(data[:240]))


def sample_mapping(read):
    """Bounded read, no spin/retry/write. Header fields are observational brackets."""
    before = struct.unpack('<q', read(8 * 8, 8))[0]
    header = struct.unpack('<19q', read(0, 19 * 8))
    if header[:4] != (MAGIC, 1, 256, CAPACITY):
        raise ValueError('Mapping schema mismatch')
    offset = PAGE + ((before - 1) % CAPACITY) * 256
    commit_before = read(offset + 248, 8) if before > 0 else b''
    raw = read(offset, 256) if before > 0 else b''
    commit_after = read(offset + 248, 8) if before > 0 else b''
    after = struct.unpack('<q', read(8 * 8, 8))[0]
    if before < 0 or after < before:
        raise ValueError('Producer watermark invalid/regressed')
    valid = bool(before and commit_before == commit_after == raw[248:] and valid_event(raw, before))
    return dict(producedBefore=str(before), producedAfter=str(after),
                headerProduced=str(header[8]), consumed=str(header[9]), durable=str(header[10]),
                observerHeartbeat=str(header[11]), producerFault=str(header[12]),
                observerFault=str(header[13]), done=str(header[14]), ready=str(header[15]),
                dropped=str(header[16]), producerHeartbeat=str(header[17]),
                headerWithinBracket=(before <= header[8] <= after),
                stableProduced=(before == after), eventValid=valid,
                latestEventWords=[str(w) for w in struct.unpack('<32Q', raw)[:30]] if valid else None,
                latestEventBase64=base64.b64encode(raw).decode() if valid else None,
                eventObservation='checksum-valid copy at producedBefore; not a durable commit' if valid else 'no validated event copy')


class WindowsSource:
    def __init__(self, directory, contract=None):
        self.directory = Path(directory)
        meta_raw = (self.directory / 'session.json').read_bytes()
        observer_raw = (self.directory / 'observer.json').read_bytes()
        meta = json.loads(meta_raw)
        observer = json.loads(observer_raw)
        self.session = str(uuid.UUID(meta['session']))
        if str(uuid.UUID(observer['session'])) != self.session:
            raise ValueError('Observer session mismatch')
        self.kernel = C.WinDLL('kernel32', use_last_error=True)
        k = self.kernel
        k.OpenFileMappingW.argtypes = [W.DWORD, W.BOOL, W.LPCWSTR]; k.OpenFileMappingW.restype = W.HANDLE
        k.MapViewOfFile.argtypes = [W.HANDLE, W.DWORD, W.DWORD, W.DWORD, C.c_size_t]; k.MapViewOfFile.restype = C.c_void_p
        k.UnmapViewOfFile.argtypes = [C.c_void_p]
        k.CloseHandle.argtypes = [W.HANDLE]
        k.OpenProcess.argtypes = [W.DWORD, W.BOOL, W.DWORD]; k.OpenProcess.restype = W.HANDLE
        k.GetProcessTimes.argtypes = [W.HANDLE] + [C.POINTER(W.FILETIME)] * 4
        k.WaitForSingleObject.argtypes = [W.HANDLE, W.DWORD]; k.WaitForSingleObject.restype = W.DWORD
        k.QueryPerformanceCounter.argtypes = [C.POINTER(C.c_int64)]
        k.QueryFullProcessImageNameW.argtypes = [W.HANDLE, W.DWORD, W.LPWSTR, C.POINTER(W.DWORD)]
        self.handle = None; self.address = None; self.processes = []
        try:
            for role, pid, expected in (('producer', meta['producer'], meta['startUtcTicks']),
                                        ('observer', observer['pid'], observer['startUtcTicks'])):
                handle = k.OpenProcess(0x100000 | 0x1000, False, pid)
                if not handle:
                    raise OSError(C.get_last_error(), f'{role} process unavailable')
                self.processes.append((role, handle, pid, expected))
                times = [W.FILETIME() for _ in range(4)]
                if not k.GetProcessTimes(handle, *(C.byref(v) for v in times)):
                    raise OSError(C.get_last_error(), 'GetProcessTimes')
                actual = ((times[0].dwHighDateTime << 32) | times[0].dwLowDateTime) + 504911232000000000
                if actual != expected:
                    raise ValueError(f'{role} process incarnation mismatch')
                if contract:
                    buffer=C.create_unicode_buffer(32768); length=W.DWORD(len(buffer))
                    if not k.QueryFullProcessImageNameW(handle,0,buffer,C.byref(length)):
                        raise OSError(C.get_last_error(),'Process image query failed')
                    if Path(buffer.value).resolve()!=Path(contract[role+'Image']).resolve():
                        raise ValueError(f'{role} canonical process image mismatch')
            self.handle = k.OpenFileMappingW(4, False, 'Local\\NovaCoreMinimum-' + uuid.UUID(self.session).hex)
            if not self.handle:
                raise OSError(C.get_last_error(), 'Mapping unavailable')
            self.address = k.MapViewOfFile(self.handle, 4, 0, 0, PAGE + CAPACITY * 256)
            if not self.address:
                raise OSError(C.get_last_error(), 'Read-only mapping view failed')
            header = self.read(0, 19 * 8)
            if str(uuid.UUID(bytes_le=header[32:48])) != self.session:
                raise ValueError('Mapping session mismatch')
            pid, ticks = struct.unpack_from('<qq', header, 48)
            if pid != meta['producer'] or ticks != meta['startUtcTicks']:
                raise ValueError('Mapping producer incarnation mismatch')
            self.immutable_header = header[:64]
            self.identity = dict(session=self.session, producerPid=pid, producerStartUtcTicks=str(ticks),
                                 observerPid=observer['pid'], observerStartUtcTicks=str(observer['startUtcTicks']),
                                 sessionMetadataSha256=digest(meta_raw), observerMetadataSha256=digest(observer_raw),
                                 openedQpc=str(meta['openedQpc']), openedUtcTicks=str(meta['openedUtcTicks']),
                                 qpcFrequency=meta['qpcFrequency'])
            self.identity['attachedUtcNs'] = str(time.time_ns())
            self.identity['admissionVerified'] = False
            if contract:
                if meta.get('schema')!=2 or observer.get('schema')!=1 or self.directory.name!=uuid.UUID(self.session).hex or meta.get('reservedBytes')!=RESERVED_BYTES:
                    raise ValueError('Session metadata/admission identity mismatch')
                seen={str(Path(item['path']).resolve()).lower():item['sha256'].lower() for item in meta.get('identity',[])}
                if seen != contract['incidentIdentity']:
                    raise ValueError('Session executable identity does not match sealed preflight')
                for filename,size in (('bank0.bin',41947136),('bank1.bin',41947136),('head0.bin',4096),('head1.bin',4096)):
                    if (self.directory/filename).stat().st_size!=size: raise ValueError('Recorder journal allocation mismatch')
                self.identity.update(admissionVerified=True,reservedBytes=str(RESERVED_BYTES),contractSha256=contract['sha256'])
        except BaseException:
            self.close(); raise

    def read(self, offset, count):
        if offset < 0 or count < 0 or offset + count > PAGE + CAPACITY * 256:
            raise ValueError('Mapping read bounds')
        return C.string_at(self.address + offset, count)

    def sample(self):
        if self.read(0, 64) != self.immutable_header:
            raise ValueError('Mapping immutable identity changed')
        result = sample_mapping(self.read)
        if self.read(0, 64) != self.immutable_header:
            raise ValueError('Mapping immutable identity changed during sample')
        qpc = C.c_int64(); self.kernel.QueryPerformanceCounter(C.byref(qpc))
        result.update(self.identity, qpc=str(qpc.value), fixture=False)
        for role, handle, _, _ in self.processes:
            state = self.kernel.WaitForSingleObject(handle, 0)
            result[role + 'Process'] = {0: 'exited', 258: 'alive'}.get(state, 'query-failed')
        return result

    def close(self):
        if self.address: self.kernel.UnmapViewOfFile(self.address); self.address = None
        if self.handle: self.kernel.CloseHandle(self.handle); self.handle = None
        for _, handle, _, _ in self.processes: self.kernel.CloseHandle(handle)
        self.processes.clear()


class FixtureSource:
    """Labeled CPU-only synthetic phases for phone transport qualification."""
    def __init__(self, output=None):
        self.start = time.monotonic(); self.session = str(uuid.uuid4()); self.output=Path(output) if output else None
        self.phase='healthy'; self.count=0; self.produced=1; self.durable=1; self.worker=1; self.producer=1
    def sample(self):
        elapsed = time.monotonic() - self.start
        if self.output and (self.output/'fixture-control.json').exists():
            raw=(self.output/'fixture-control.json').read_bytes()
            if len(raw)>1024: raise ValueError('Fixture control size')
            phase=json.loads(raw)['phase']
            if phase not in ('healthy','renderer-static','recorder-static','observer-exited'): raise ValueError('Fixture phase')
            self.phase=phase
        qpc=time.perf_counter_ns()//100; self.count+=1
        if self.phase!='renderer-static': self.produced+=19; self.producer=qpc
        if self.phase not in ('recorder-static','observer-exited'): self.durable=self.produced; self.worker=qpc
        words=[0]*32; words[0]=self.produced; words[1]=self.producer; words[4]=8; words[5]=1; words[19]=MASK
        raw=struct.pack('<32Q',*words); raw=raw[:240]+struct.pack('<QQ',checksum(raw[:240]),self.produced)
        return dict(fixture=True, session=self.session, elapsedSeconds=elapsed,phase=self.phase,
                    producerPid=1,producerStartUtcTicks='639261612505478296',observerPid=2,observerStartUtcTicks='639261612520224860',
                    qpc=str(qpc),qpcFrequency=10000000,producedBefore=str(self.produced),producedAfter=str(self.produced),headerProduced=str(self.produced),
                    consumed=str(self.durable),durable=str(self.durable),observerHeartbeat=str(self.worker),producerHeartbeat=str(self.producer),
                    producerProcess='alive',observerProcess='exited' if self.phase=='observer-exited' else 'alive',
                    producerFault='0',observerFault='0',dropped='0',done='0',ready='1',headerWithinBracket=True,stableProduced=True,eventValid=True,
                    latestEventWords=[str(word) for word in struct.unpack('<32Q',raw)[:30]],latestEventBase64=base64.b64encode(raw).decode(),
                    sessionMetadataSha256='a'*64,observerMetadataSha256='b'*64,admissionVerified=True,reservedBytes=str(RESERVED_BYTES),
                    syntheticPadding='.'*500)
    def close(self): pass


class SlotJournal:
    """Two bounded copies. A recovered valid slot is independent of volatile ACK."""
    def __init__(self, directory):
        self.directory = Path(directory); self.directory.mkdir(parents=True, exist_ok=False)
        self.files = [(self.directory / f'slot{i}.bin').open('x+b') for i in range(2)]
        for file in self.files: file.write(bytes(PAGE)); file.flush(); os.fsync(file.fileno())
        self.next_slot = 1

    def write(self, sample, cut=lambda stage: None):
        data = canonical(sample)
        if len(data) > PAGE - 40: raise ValueError('Witness sample exceeds slot capacity')
        page = struct.pack('<II', 1, len(data)) + data
        page += bytes(PAGE - 32 - len(page)); page += hashlib.sha256(page).digest()
        # Samples can be coalesced. Serial parity is NOT a commit slot selector.
        file = self.files[self.next_slot]
        cut('before-write'); file.seek(0); file.write(page); cut('after-write')
        file.flush(); cut('before-fsync'); os.fsync(file.fileno()); cut('after-fsync')
        self.next_slot = 1 - self.next_slot

    def close(self):
        for file in self.files: file.close()

    @staticmethod
    def recover(directory, witness):
        valid = []; errors = []
        for i in range(2):
            try:
                page = (Path(directory) / f'slot{i}.bin').read_bytes()
                if page == bytes(PAGE): continue
                if len(page) != PAGE or hashlib.sha256(page[:-32]).digest() != page[-32:]:
                    raise ValueError('Invalid/torn slot digest')
                version, length = struct.unpack_from('<II', page)
                if version != 1 or not 0 < length <= PAGE - 40: raise ValueError('Slot schema')
                value = json.loads(page[8:8 + length])
                if (not isinstance(value, dict) or value.get('schema') != SCHEMA or value.get('witness') != witness
                        or type(value.get('serial')) is not int or value['serial'] <= 0):
                    raise ValueError('Wrong witness/serial')
                valid.append(value)
            except (OSError, ValueError, KeyError) as error: errors.append(f'slot{i}: {error}')
        if len(valid) == 2 and valid[0]['serial'] == valid[1]['serial'] and valid[0] != valid[1]:
            errors.append('Conflicting sample authority for equal serial'); valid = []
        return dict(sample=max(valid, key=lambda s: s['serial']) if valid else None, errors=errors,
                    terminal='Witness suffix uncertain; recovered sample does not advance recorder durability or prove physical display completion')


class Witness:
    def __init__(self, output, source=None, root=None, contract=None, token=None):
        self.id = str(uuid.uuid4()); self.token = secrets.token_urlsafe(24)
        if token: self.token=token
        self.journal = SlotJournal(output); self.output = Path(output)
        self.source = source; self.root = Path(root) if root else None
        self.contract=contract; self.initial_directories={p.name for p in self.root.iterdir() if p.is_dir()} if self.root and self.root.exists() else set()
        self.start_ticks = time.time_ns() // 100 + 621355968000000000
        self.latest = None; self.serial = 0; self.committed = 0; self.disk_error = None
        self.samples_skipped = 0; self.lock = threading.Lock(); self.stop = threading.Event()
        self.wake = threading.Event(); self.threads = []; self.max_sample_ms = 0
        self.last_flush_ms = 0; self.max_flush_ms = 0
        self.committed_sample=None; self.beacon=None; self.mode='fixture' if isinstance(source,FixtureSource) else 'live'
        self.preflight=None; self.receiver_build=None; self.end_monotonic_ns=None

    def attach(self):
        if self.source or not self.root: return
        candidates = []
        for path in self.root.glob('*/session.json'):
            try:
                value = json.loads(path.read_text())
                if path.parent.name not in self.initial_directories and (path.parent / 'observer.json').is_file():
                    candidates.append((value['openedUtcTicks'], path.parent))
            except (OSError, ValueError): continue
        if len(candidates)>1: raise ValueError('Multiple new recorder sessions; attachment refused')
        if candidates: self.source = WindowsSource(candidates[0][1],self.contract)

    def sample_once(self):
        start = time.perf_counter_ns()
        try:
            self.attach()
            value = self.source.sample() if self.source else dict(status='waiting-for-new-session', fixture=False)
        except Exception as error:
            value = dict(status='sample-error', error=f'{type(error).__name__}: {error}', fixture=False)
        self.serial += 1
        sample = dict(schema=SCHEMA, witness=self.id, serial=self.serial, utcNs=str(time.time_ns()),
                      sampleMonotonicNs=str(time.perf_counter_ns()), observation=value)
        self.max_sample_ms = max(self.max_sample_ms, (time.perf_counter_ns() - start) / 1e6)
        with self.lock: self.latest = sample
        self.wake.set()
        return sample

    def run_sampler(self):
        while not self.stop.is_set():
            self.sample_once()
            self.stop.wait(1)

    def run_writer(self):
        while not self.stop.is_set() or self.committed < self.serial:
            self.wake.wait(1); self.wake.clear()
            with self.lock: sample = self.latest
            if sample and sample['serial'] > self.committed:
                try:
                    started = time.perf_counter_ns()
                    self.journal.write(sample)
                    self.last_flush_ms = (time.perf_counter_ns() - started) / 1e6
                    self.max_flush_ms = max(self.max_flush_ms, self.last_flush_ms)
                    with self.lock:
                        self.samples_skipped += max(0, sample['serial'] - self.committed - 1)
                        self.committed = sample['serial']; self.committed_sample=sample
                except Exception as error:
                    self.disk_error = f'{type(error).__name__}: {error}'
                    return

    def response(self, request_nonce):
        with self.lock:
            sample = self.latest; committed=self.committed_sample
            committed_serial=self.committed; skipped=self.samples_skipped
        return dict(schema=SCHEMA, witness=self.id, nonce=request_nonce,
                    serverUtcNs=str(time.time_ns()), serverMonotonicNs=str(time.perf_counter_ns()),
                    sample=sample, localCommittedSerial=committed_serial,
                    committedSession=committed['observation'].get('session') if committed else None,
                    committedMonotonicNs=committed['sampleMonotonicNs'] if committed else None,
                    localSkippedSamples=skipped, diskError=self.disk_error,
                    mode=self.mode,beacon=self.beacon,preflight=self.preflight,receiverBuild=self.receiver_build,
                    remainingSeconds=max(0,(self.end_monotonic_ns-time.perf_counter_ns())/1e9) if self.end_monotonic_ns else None,
                    maxSampleMs=self.max_sample_ms,
                    lastFlushMs=self.last_flush_ms, maxFlushMs=self.max_flush_ms,
                    semantics='Independent host response; per-field observations only. No causal owner or scanout proof.')

    def start(self):
        self.threads = [threading.Thread(target=self.run_sampler, daemon=True), threading.Thread(target=self.run_writer, daemon=True)]
        for thread in self.threads: thread.start()

    def finish(self):
        self.stop.set(); self.wake.set()
        for thread in self.threads: thread.join(timeout=3)
        if not any(thread.is_alive() for thread in self.threads):
            self.journal.close()
            if self.source: self.source.close()
        return dict(writerStillRunning=any(t.is_alive() for t in self.threads), committed=self.committed,
                    sampled=self.serial, skipped=self.samples_skipped, diskError=self.disk_error)


def make_server(witness, bind, port):
    page = Path(__file__).with_name('receiver.html').read_bytes()
    assets={name:Path(__file__).with_name(name).read_bytes() for name in ('receiver_core.js','receiver_store.js','receiver_app.js')}
    witness.receiver_build=digest(page+b''.join(assets.values()))
    page=page.replace(b'__RECEIVER_BUILD__',witness.receiver_build.encode())
    class Handler(http.server.BaseHTTPRequestHandler):
        def setup(self):
            self.request.settimeout(3)
            super().setup()
        def do_GET(self):
            parts = self.path.split('?', 1)[0].split('/')
            if len(parts) < 2 or not parts[1].isascii() or not secrets.compare_digest(parts[1], witness.token):
                self.send_error(404); return
            if len(parts) == 3 and parts[2] == '':
                payload = page; kind = 'text/html; charset=utf-8'
            elif len(parts)==3 and parts[2] in assets:
                payload=assets[parts[2]];kind='text/javascript; charset=utf-8'
            elif len(parts) == 4 and parts[2] == 'sample' and len(parts[3]) <= 96 and parts[3].isascii() and parts[3].isalnum():
                payload = canonical(witness.response(parts[3])); kind = 'application/json'
            else: self.send_error(404); return
            self.send_response(200); self.send_header('Content-Type', kind)
            self.send_header('Content-Length', str(len(payload))); self.send_header('Cache-Control', 'no-store')
            self.send_header('X-Content-Type-Options', 'nosniff'); self.end_headers()
            try: self.wfile.write(payload)
            except (BrokenPipeError, ConnectionResetError): pass
        def log_message(self, *_): pass
    class Server(http.server.ThreadingHTTPServer):
        allow_reuse_address=False
        def server_bind(self):
            import socket
            if hasattr(socket,'SO_EXCLUSIVEADDRUSE'):self.socket.setsockopt(socket.SOL_SOCKET,socket.SO_EXCLUSIVEADDRUSE,1)
            super().server_bind()
        daemon_threads = True
        request_queue_size = 8
        def __init__(self, *args):
            self.slots = threading.BoundedSemaphore(8)
            super().__init__(*args)
        def process_request(self, request, address):
            if not self.slots.acquire(blocking=False):
                self.shutdown_request(request)
                return
            try: super().process_request(request, address)
            except BaseException:
                self.slots.release(); raise
        def process_request_thread(self, request, address):
            try: super().process_request_thread(request, address)
            finally: self.slots.release()
    return Server((bind, port), Handler)


def read_contract(root):
    package=Path(__file__).resolve().parents[1]/'NovaCore.App/bin/Release/net10.0-windows'
    names=('NovaCore.exe','NovaCore.dll','NovaCore.Native.dll','NovaCore.Diagnostics.dll','NovaCore.Recorder.exe')
    values={str((package/name).resolve()).lower():digest((package/name).read_bytes()) for name in names}
    result=dict(producerImage=str(package/'NovaCore.exe'),observerImage=str(package/'NovaCore.Recorder.exe'),incidentIdentity=values)
    result['sha256']=digest(canonical(result))
    existing=root if root.exists() else root.parent
    free=shutil.disk_usage(existing).free
    preflight=dict(utcNs=str(time.time_ns()),monotonicNs=str(time.perf_counter_ns()),freeBytes=str(free),requiredSessionBytes=str(RESERVED_BYTES),
                   capacitySnapshotPass=False,contractSha256=result['sha256'],
                   semantics='Read-only free-space snapshot. No reservation or retention maintenance. Actual admission is verified after session attachment.')
    try:
        completed=subprocess.run(['pwsh','-NoProfile','-File',str(Path(__file__).with_name('Inspect-Capacity.ps1')),
                                  '-Diagnostics',str(package/'NovaCore.Diagnostics.dll')],capture_output=True,text=True,
                                 timeout=10,check=True,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
        admission=json.loads(completed.stdout)
        if (admission.get('schema')!='NovaCore.ReadOnlyAdmission/1' or Path(admission['root']).resolve()!=root.resolve()
                or admission['diagnosticsSha256'].lower()!=values[str((package/'NovaCore.Diagnostics.dll').resolve()).lower()]):
            raise ValueError('Admission snapshot identity mismatch')
        preflight.update(admission=admission,capacitySnapshotPass=(admission.get('available') is True and free>=RESERVED_BYTES+1048576))
    except Exception as error:preflight['admissionError']=str(error)[:1000]
    return result,preflight


def bookmark_token(path):
    if path is None:return secrets.token_urlsafe(24)
    if path.exists():
        value=json.loads(path.read_bytes());token=value.get('token')
        if value.get('schema')!='NovaCore.WitnessBookmark/1' or not isinstance(token,str) or len(token)!=32 or not all(c.isalnum() or c in '-_' for c in token):
            raise ValueError('Invalid bookmark identity; existing file preserved')
        return token
    path.parent.mkdir(parents=True,exist_ok=True);token=secrets.token_urlsafe(24)
    with path.open('xb') as file:file.write(canonical(dict(schema='NovaCore.WitnessBookmark/1',token=token)));file.flush();os.fsync(file.fileno())
    return token


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--bind', default='127.0.0.1'); parser.add_argument('--port', type=int, default=58761)
    parser.add_argument('--duration', type=int, default=0)
    parser.add_argument('--fixture', action='store_true')
    parser.add_argument('--recover',action='store_true')
    parser.add_argument('--bookmark-file',type=Path)
    parser.add_argument('--beacon-port',type=int,default=58762)
    parser.add_argument('--root', type=Path, default=Path(os.environ.get('LOCALAPPDATA', '.')) / 'NovaCore/MinimumRecorder')
    args = parser.parse_args()
    if args.duration != 0 and (not args.fixture or not 10 <= args.duration <= 1800):
        parser.error('Real/recovery sessions are standing; only fixtures accept a finite 10..1800 second duration')
    if args.output.resolve().is_relative_to(args.root.resolve()): parser.error('Output must be outside recorder runtime storage')
    if args.fixture and args.recover:parser.error('Fixture and recovery are separate modes')
    if args.bookmark_file and args.bookmark_file.resolve().is_relative_to(args.root.resolve()):parser.error('Bookmark must be outside runtime storage')
    contract,preflight=(None,None) if args.fixture or args.recover else read_contract(args.root)
    witness = Witness(args.output, FixtureSource(args.output) if args.fixture else None,
                      None if args.fixture or args.recover else args.root,contract,bookmark_token(args.bookmark_file))
    witness.preflight=preflight
    if args.recover:witness.mode='recovery'
    server=None;child=None;serving=False
    try:
        server = make_server(witness, args.bind, args.port)
        if not args.recover:
            origin=f'http://{args.bind}:{server.server_port}';probe=str(uuid.uuid4());beacon_output=witness.output/'beacon-startup.json'
            command=[sys.executable,str(Path(__file__).with_name('beacon.py')),'--bind',args.bind,'--port',str(args.beacon_port),
                     '--token',witness.token,'--witness',witness.id,'--probe',probe,'--origin',origin,
                     '--output',str(beacon_output),'--duration',str(args.duration)]
            child=subprocess.Popen(command,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
            deadline=time.monotonic()+5
            while not beacon_output.exists():
                if child.poll() is not None or time.monotonic()>deadline:raise RuntimeError('Independent CPU beacon failed startup')
                time.sleep(.02)
            beacon_identity=json.loads(beacon_output.read_bytes())
            if beacon_identity.get('witness')!=witness.id or beacon_identity.get('probeId')!=probe or beacon_identity.get('pid')!=child.pid:
                raise ValueError('CPU beacon startup incarnation mismatch')
            witness.beacon=dict(beacon_identity,url=f'http://{args.bind}:{beacon_identity["port"]}/{witness.token}/')
        from beacon import process_start_ticks
        witness.end_monotonic_ns=time.perf_counter_ns()+args.duration*1000000000 if args.duration else None
        info = dict(schema=SCHEMA,witness=witness.id,pid=os.getpid(),startUtcTicks=process_start_ticks(),fixture=args.fixture,mode=witness.mode,
                    url=f'http://{args.bind}:{server.server_port}/{witness.token}/',durationSeconds=args.duration,
                    inputRoot=str(args.root),sourceSha256=digest(Path(__file__).read_bytes()),beacon=witness.beacon,
                    receiverBuild=witness.receiver_build,receiverSha256=digest(Path(__file__).with_name('receiver.html').read_bytes()),preflight=preflight)
        with (witness.output/'startup.pending').open('xb') as file:file.write(canonical(info));file.flush();os.fsync(file.fileno())
        (witness.output/'startup.pending').rename(witness.output/'startup.json')
        print(json.dumps(info),flush=True)
        if not args.recover:witness.start()
        thread=threading.Thread(target=server.serve_forever,daemon=True);thread.start();serving=True
        from lifetime import wait_until_stopped
        wait_until_stopped(witness.stop,args.duration)
    except KeyboardInterrupt:pass
    finally:
        if serving:server.shutdown()
        if server:server.server_close()
        if child and child.poll() is None:child.terminate();child.wait(5)
        result = witness.finish(); (witness.output / 'finished.json').write_bytes(canonical(result))


if __name__ == '__main__': main()
