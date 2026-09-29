"""Standing helpers use interruptible, bounded waits; zero means no deadline."""
import time


def wait_until_stopped(stop, duration=0, clock=time.monotonic):
    if duration < 0:
        raise ValueError('Negative duration')
    end = clock() + duration if duration else None
    while not stop.is_set():
        remaining = .5 if end is None else min(.5, end - clock())
        if remaining <= 0:
            return
        stop.wait(remaining)
