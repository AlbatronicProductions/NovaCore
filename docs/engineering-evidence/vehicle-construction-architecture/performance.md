# Final performance, allocation and storage

Final candidate measurements ran serially without concurrent builds/stress; the local editor server was idle. Raw summaries are performance-final-stage1.json through performance-final-stage6.json. They retain samples, median/P95/P99/max, bytes per operation, GC counts and longest consecutive run strictly above P95. No time or allocation budget was invented. Earlier stage-specific files are retained historical observations, not substituted for these final measurements.

|Workload|Median|P95|P99|max|Bytes/op|
|---|---:|---:|---:|---:|---:|
|Cold DLV runtime construction|525µs|676.1µs|3511.1µs|3511.1µs|400800|
|Cold capsule runtime construction|146.1µs|149.3µs|176.3µs|176.3µs|105488|
|Joint active fuel/power interval, precreated owner|12.9µs|13.9µs|14.5µs|14.6µs|8656|
|Fuel-off owner step with enabled electrical load, window0|2.3µs|2.6µs|2.8µs|7.4µs|1744|
|Observation only|0.2µs|0.2µs|0.2µs|4.6µs|0|
|Replay128 journal commands|708.9µs|750.3µs|893.6µs|893.6µs|389680|
|Cached editor inspection|below0.1µs|0.1µs|0.1µs|0.1µs|0|

The active owner interval uses a small exact bipropellant/power fixture and includes preparation/publication but excludes cold session creation/disposal. Fuel-off stepping still drains an enabled1W load; it is not idle work. Three windows have stable2.3µs medians and1744B/op, no collections, longest P95 run1. Active joint interval has no collections and longest run2. Cold DLV construction sees2/1/0 collections and an isolated3.5111ms tail. Populated replay sees1/0/0 collections. These are measured workload limits, not a guarantee for every admitted graph or maximum-width number.

Cold catalog median3.94–4.20ms; cold asset resolution median21.30ms. Cached asset validation still allocates616B/op and is deliberately a load/session boundary operation. It is not called every display frame. Cold DLV graph compile medians1.75–3.00ms, with observed tails; no hard real-time construction claim. Editor rotate/reconnect medians approximately256/258µs with approximately97KB allocations; edits compile new immutable facts. Display inspection, unchanged topology and runtime observation reuse retained facts. Exact arithmetic intentionally allocates for active changes; legacy pilot service/allocation gates remain0B.

Fuel evolution services a whole interval with a bounded depletion-event loop. No asset, graph, editor or render work is multiplied by simulation microticks. Generic static service admission is rate1 only; physical flight warp remains outside this qualification.

## Serialized-size bounds

Shared canonical design/standalone Load cap:4,000,000 bytes; checked before compiled result/editor installation. Runtime journal capacity:1..4096. Runtime document cap:268,435,456 bytes. Independent conservative proof: fuel QuantityBits<=999998 yields fuel JSON<=1024+257×250001=64,251,281 bytes. Power D<=500000, I<=502199 and HexDigits<=125551 yield power JSON<=16384+513×125551=64,424,047 bytes. Both base64 payloads occupy at most171,567,108 bytes. Overbounding each command by128 fixed bytes+256×6 demand bytes+512×32 load-setting bytes gives73,924,608 bytes for4096 commands. Add4,000,000 design bytes and4096 outer bytes: **249,495,812 <268,435,456**. This safely overcounts mutually exclusive module roles. A separate role-aware proof tightens this to140,574,396 bytes; either is sufficient for output size.

Observed small fixture: design812 bytes, empty-journal runtime2487 bytes,128-command runtime8121 bytes. identity.json records actual retained candidate source/content/evidence and build-output sizes. External accepted GLBs remain external, verified content; no copied KSA implementation/assets. Serializer buffers, base64 copies, object graphs and BigInteger working storage coexist: **worst-case peak heap at maximum admitted capacities is not qualified by the serialized-size proof**. No broad-scale performance or flight guarantee follows from the bounded development fixtures.
