"""Independent bounded CPU responder. No recorder, sampler, or journal dependency."""
import argparse
import ctypes as C
from ctypes import wintypes as W
import http.server
import json
import os
from pathlib import Path
import secrets
import threading
import time
from lifetime import wait_until_stopped


def process_start_ticks():
    k=C.WinDLL('kernel32',use_last_error=True)
    k.GetCurrentProcess.restype=W.HANDLE
    k.GetProcessTimes.argtypes=[W.HANDLE]+[C.POINTER(W.FILETIME)]*4
    values=[W.FILETIME() for _ in range(4)]
    if not k.GetProcessTimes(k.GetCurrentProcess(),*(C.byref(v) for v in values)): raise OSError(C.get_last_error())
    return str(((values[0].dwHighDateTime<<32)|values[0].dwLowDateTime)+504911232000000000)


def make_server(bind,port,token,witness,probe,origin):
    identity=dict(schema='NovaCore.CpuBeacon/1',witness=witness,probeId=probe,pid=os.getpid(),startUtcTicks=process_start_ticks())
    class Handler(http.server.BaseHTTPRequestHandler):
        def setup(self): self.request.settimeout(3);super().setup()
        def do_GET(self):
            parts=self.path.split('?',1)[0].split('/')
            if (len(parts)!=4 or not parts[1].isascii() or not secrets.compare_digest(parts[1],token)
                    or parts[2]!='sample' or not parts[3].isascii() or not parts[3].isalnum() or not 1<=len(parts[3])<=96):
                self.send_error(404);return
            payload=json.dumps(dict(identity,nonce=parts[3],serverUtcNs=str(time.time_ns()),serverMonotonicNs=str(time.perf_counter_ns()))).encode()
            self.send_response(200);self.send_header('Content-Type','application/json');self.send_header('Content-Length',str(len(payload)))
            self.send_header('Cache-Control','no-store');self.send_header('Access-Control-Allow-Origin',origin)
            self.send_header('X-Content-Type-Options','nosniff');self.end_headers()
            try:self.wfile.write(payload)
            except (BrokenPipeError,ConnectionResetError):pass
        def log_message(self,*_):pass
    class Server(http.server.ThreadingHTTPServer):
        allow_reuse_address=False
        def server_bind(self):
            import socket
            if hasattr(socket,'SO_EXCLUSIVEADDRUSE'):self.socket.setsockopt(socket.SOL_SOCKET,socket.SO_EXCLUSIVEADDRUSE,1)
            super().server_bind()
        daemon_threads=True;request_queue_size=8
        def __init__(self,*args):self.slots=threading.BoundedSemaphore(8);super().__init__(*args)
        def process_request(self,request,address):
            if not self.slots.acquire(False):self.shutdown_request(request);return
            try:super().process_request(request,address)
            except BaseException:self.slots.release();raise
        def process_request_thread(self,request,address):
            try:super().process_request_thread(request,address)
            finally:self.slots.release()
    server=Server((bind,port),Handler);identity['port']=server.server_port
    return server,identity


def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ('bind','token','witness','probe','origin','output'):p.add_argument('--'+name,required=True)
    p.add_argument('--port',type=int,required=True);p.add_argument('--duration',type=int,required=True)
    a=p.parse_args()
    if a.duration != 0 and not 10<=a.duration<=1800:p.error('Duration must be zero (standing) or 10..1800 fixture seconds')
    server,identity=make_server(a.bind,a.port,a.token,a.witness,a.probe,a.origin)
    destination=Path(a.output);temporary=destination.with_suffix('.pending')
    with temporary.open('xb') as file:file.write(json.dumps(identity).encode());file.flush();os.fsync(file.fileno())
    # The parent never observes an empty/partially written startup publication.
    temporary.rename(destination)
    thread=threading.Thread(target=server.serve_forever,daemon=True);thread.start()
    try:wait_until_stopped(threading.Event(),a.duration)
    except KeyboardInterrupt:pass
    finally:server.shutdown();server.server_close()


if __name__=='__main__':main()
