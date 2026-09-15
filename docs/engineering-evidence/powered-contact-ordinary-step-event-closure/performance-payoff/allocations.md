# Exact-zero allocation recheck

Project Control changed only the diagnostic contract after the ownership audit.
Historical allocation-closure JSON/reports remain unchanged; its 1,936-byte
Rational result and then-required binary64 stop remain historical failures.

One fresh Release allocation process, unchanged checked 1 MiB helper, 128 warm
calls. [Raw transcript](allocation-recheck/allocation-witness.txt) and
[result](allocation-recheck/allocation.json):

| Window | Raw bytes | Result |
|---|---:|---|
| Immutable input read | 0 | PASS |
| Deliberate byte[128] | 152 | PASS |
| Warm single 1 | 0 | PASS |
| Warm single 2 | 0 | PASS |
| Warm single 3 | 0 | PASS |
| Warm repeated 128 | 0 | PASS |

Entry/exit PASS for every region. All six floats match the retained off-COM
witness. Corrected outputs additionally match their own initial binary64 result
through warmup/repetition; old-vs-new inverseMass/H double checks remain intact.
Only the four accepted temporary double comparisons cease to fail the gate.
No tolerance, averaging, subtraction, pooling or retry.

After normal-runtime timings, each of the three processes used separate
equivalent prepared fixtures for A/B and a C128 series. Raw bytes:

| Process | A ordinary step | B map + ordinary step | C map ×128 |
|---|---:|---:|---:|
| 1 | 0 | 0 | 0 |
| 2 | 0 | 0 | 0 |
| 3 | 0 | 0 | 0 |

All checked entries/exits passed. Timing never ran inside a no-GC region.
Cold JSON parsing/raw source representation import and world/shape preparation
are excluded intentionally; no derived per-event answer is cached by B/C.
This does not qualify future canonical debit, publication or owner admission.
