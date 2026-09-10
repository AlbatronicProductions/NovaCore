## Complete accounting timelines

Total is approximate process-wide GetTotalAllocatedBytes(false), not an owner-thread allocation total. Objects/bytes are exact observer counts while a measured window is active; UNAVAILABLE outside those windows. Background-GC flag is UNAVAILABLE. Region is caller-observed successful entry/unreturned exit, not an independently queried runtime flag. Full pointer identities and suspension samples are in timelines.json.

### Sequence 1: normal, PID 13308

| Checkpoint | Thread counter | Total approximate | Objects/bytes | Unused | GC counts 0/1/2 | GC callbacks started/finished | Managed/OS thread | Caller region |
|---|---:|---:|---|---:|---|---|---|---|
| A before GC entry | 5531320 | 5534344 | UNAVAILABLE | 3024 | 0/0/0 | 0/0 | 2/20664 | 0 |
| B after GC entry | 5531320 | 5534344 | UNAVAILABLE | 3024 | 1/1/1 | 0/0 | 2/20664 | 1 |
| C before epoch workload | 5531320 | 5534344 | 0/0 | 3024 | 1/1/1 | 0/0 | 2/20664 | 1 |
| D after epoch workload | 5531320 | 5534344 | 0/0 | 3024 | 1/1/1 | 0/0 | 2/20664 | 1 |
| E before GC exit | 5531320 | 5534344 | UNAVAILABLE | 3024 | 1/1/1 | 0/0 | 2/20664 | 1 |
| F after GC exit | 5531320 | 5534344 | UNAVAILABLE | 3024 | 1/1/1 | 0/0 | 2/20664 | 0 |
| G before servicing | 26997968 | 27006952 | 0/0 | 8160 | 3/3/3 | 2/1 | 2/20664 | 0 |
| H after servicing | 27006128 | 27015152 | 0/0 | 0 | 3/3/3 | 2/2 | 2/20664 | 0 |

### Sequence 2: omitted, PID 24848

| Checkpoint | Thread counter | Total approximate | Objects/bytes | Unused | GC counts 0/1/2 | GC callbacks started/finished | Managed/OS thread | Caller region |
|---|---:|---:|---|---:|---|---|---|---|
| A before GC entry | 5531424 | 5534344 | UNAVAILABLE | 2920 | 0/0/0 | 0/0 | 2/21928 | 0 |
| B after GC entry | 5531424 | 5534344 | UNAVAILABLE | 2920 | 0/0/0 | 0/0 | 2/21928 | 0 |
| C before epoch workload | 5531424 | 5534344 | 0/0 | 2920 | 0/0/0 | 0/0 | 2/21928 | 0 |
| D after epoch workload | 5531424 | 5534344 | 0/0 | 2920 | 0/0/0 | 0/0 | 2/21928 | 0 |
| E before GC exit | 5531424 | 5534344 | UNAVAILABLE | 2920 | 0/0/0 | 0/0 | 2/21928 | 0 |
| F after GC exit | 5531424 | 5534344 | UNAVAILABLE | 2920 | 0/0/0 | 0/0 | 2/21928 | 0 |
| G before servicing | 27006096 | 27015008 | 0/0 | 7792 | 2/2/2 | 2/2 | 2/21928 | 0 |
| H after servicing | 27006096 | 27015008 | 0/0 | 7792 | 2/2/2 | 2/2 | 2/21928 | 0 |

### Sequence 3: normal, PID 39068

| Checkpoint | Thread counter | Total approximate | Objects/bytes | Unused | GC counts 0/1/2 | GC callbacks started/finished | Managed/OS thread | Caller region |
|---|---:|---:|---|---:|---|---|---|---|
| A before GC entry | 5531320 | 5534344 | UNAVAILABLE | 3024 | 0/0/0 | 0/0 | 2/46700 | 0 |
| B after GC entry | 5531320 | 5534344 | UNAVAILABLE | 3024 | 1/1/1 | 0/0 | 2/46700 | 1 |
| C before epoch workload | 5531320 | 5534344 | 0/0 | 3024 | 1/1/1 | 0/0 | 2/46700 | 1 |
| D after epoch workload | 5531320 | 5534344 | 0/0 | 3024 | 1/1/1 | 0/0 | 2/46700 | 1 |
| E before GC exit | 5531320 | 5534344 | UNAVAILABLE | 3024 | 1/1/1 | 0/0 | 2/46700 | 1 |
| F after GC exit | 5531320 | 5534344 | UNAVAILABLE | 3024 | 1/1/1 | 0/0 | 2/46700 | 0 |
| G before servicing | 26997968 | 27006952 | 0/0 | 8160 | 3/3/3 | 2/1 | 2/46700 | 0 |
| H after servicing | 27006128 | 27015152 | 0/0 | 0 | 3/3/3 | 2/2 | 2/46700 | 0 |

### Sequence 4: omitted, PID 14420

| Checkpoint | Thread counter | Total approximate | Objects/bytes | Unused | GC counts 0/1/2 | GC callbacks started/finished | Managed/OS thread | Caller region |
|---|---:|---:|---|---:|---|---|---|---|
| A before GC entry | 5531424 | 5534344 | UNAVAILABLE | 2920 | 0/0/0 | 0/0 | 2/7332 | 0 |
| B after GC entry | 5531424 | 5534344 | UNAVAILABLE | 2920 | 0/0/0 | 0/0 | 2/7332 | 0 |
| C before epoch workload | 5531424 | 5534344 | 0/0 | 2920 | 0/0/0 | 0/0 | 2/7332 | 0 |
| D after epoch workload | 5531424 | 5534344 | 0/0 | 2920 | 0/0/0 | 0/0 | 2/7332 | 0 |
| E before GC exit | 5531424 | 5534344 | UNAVAILABLE | 2920 | 0/0/0 | 0/0 | 2/7332 | 0 |
| F after GC exit | 5531424 | 5534344 | UNAVAILABLE | 2920 | 0/0/0 | 0/0 | 2/7332 | 0 |
| G before servicing | 27006096 | 27022776 | 0/0 | 7792 | 2/2/2 | 2/2 | 2/7332 | 0 |
| H after servicing | 27006096 | 27022776 | 0/0 | 7792 | 2/2/2 | 2/2 | 2/7332 | 0 |
