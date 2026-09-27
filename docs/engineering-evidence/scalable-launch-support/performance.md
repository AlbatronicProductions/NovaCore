# Final support performance measurements

Release, current canonical candidate. CPU qualification ran alongside native
application qualification; this is not an idle-machine microbenchmark.
Machine-readable measurements are in `performance.json` after final sealing.

| Stock article | Mass kg | Cold admission ms (one actual-site sample) | Supported transaction median / P95 / P99 ms | Observed worst ms | Release transaction ms | Retained native pool bytes |
|---|---:|---:|---:|---:|---:|---:|
| Short | 1,304 | 237.116 | 1.834 / 2.124 / 2.327 | 2.542 | 1.387 | 507,904 |
| Long | 2,164 | 67.383 | 0.656 / 0.761 / 0.866 | 0.907 | 0.730 | 507,904 |
| Two short / 64 jets | 2,288 | 62.475 | 0.522 / 0.679 / 0.809 | 3.952 | 0.922 | 573,440 |
| Two long / 64 jets | 4,008 | 46.832 | 0.776 / 0.892 / 1.082 | 4.770 | 0.536 | 573,440 |
| Two short above long / 96 jets | 4,132 | 65.087 | 0.940 / 1.065 / 1.255 | 2.895 | 0.761 | 655,360 |

Transactions advance the existing 64 Hz simulation. These timings include
existing resource/control work and are not isolated contact-solver timings.
Median allocated bytes per supported transaction respectively were 51,952;
51,976; 65,960; 89,776; 113,616. Cold actual-site allocations were 431,584;
430,480; 539,432; 540,520; 651,088 bytes. Retained pool values are capacity before
release, not evidence of a leak or total process working set. No zero-allocation
flight claim is made.

Repeated constant-gravity cold preparation uses 12 samples per recipe:

| Article | Median ms | P95 / P99 / observed worst ms | Median allocated bytes |
|---|---:|---:|---:|
| Short | 56.857 | 87.701 | 120,320 |
| Long | 40.676 | 41.729 | 120,320 |
| Two short | 53.352 | 58.876 | 155,192 |
| Two long | 55.444 | 61.637 | 155,184 |
| Two long + short (support-only) | 64.002 | 69.391 | 191,592 |

With N=12, nearest-rank P95/P99 are the observed maximum; there is no fitted
tail or claim that every maximum repeats. The first actual-site admission also
includes cold runtime costs. Native application repeated frame windows, active
RCS allocations/GC and editor windows are preserved separately in
`native-routes.json`. Editor zero-allocation windows do not describe flight.

The 64/96-jet canonical native routes used a 3440×1440 borderless application
with a 3440×1322 rendering viewport. Across their nine 256-frame windows each,
64-jet median frame times were 7.535–10.394 ms (largest window P99 12.689 ms);
96-jet medians were 7.846–10.468 ms (largest window P99 12.500 ms). Each included
three sustained active-RCS coast windows. The 96-jet native route retains its
existing test-only half-capacity catalog; stock 96-jet support/flight is proved
separately by the 4,132 kg lifecycle recipe above. No quality reduction was made.
