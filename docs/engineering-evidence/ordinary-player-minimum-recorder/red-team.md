# Independent adversarial review

Root was the sole production writer. `ordinary_recorder_durability` reviewed
protocol/state/storage; `ordinary_recorder_native_review` reviewed native ownership,
hooks and measurement fidelity. Both were read-only and reported no GPU exposure.

The bounded recorder package addressed their demonstrated findings:

- Binding/mapping had overwritten birth provenance: retained separate records and
  retirement tombstones, with exact per-pending role associations.
- A retained resource plus its allocation could exceed decode capacity by one:
  admission now checks each distinct insertion; encoder/decoder limits agree.
- Newest bank-incarnation corruption could be skipped silently: validate metadata
  before supersession, expose corrupted fallback.
- A truncated final page discarded a stronger prefix: salvage complete records.
- Concurrent startup could overrun retention: startup-only admission file lease.
- Recording identity was lost after scope closure: retained command incarnation
  state in checkpoints; End correlates to Begin.
- Queue alias/device retirement and resource-entry attribution were incomplete:
  registered aliases, explicit retirement, correct unmap action.
- Scope returns reused entry progress fields: refresh return context while
  preserving operation/resource identity. Completion deliberately uses saved
  submitted context instead.
- Initial benchmark omitted actual role lookups: replacement includes the exact
  production wrapper file, all seeded roles, repeated physical/scratch swaps,
  source-generated Vulkan fakes and a raw-call forwarding oracle.
- Corrupted shared watermarks could reach ring arithmetic: native and managed
  producers now refuse negative/future counters before indexing; the observer
  rejects backward/excess-capacity publication. Permanent witnesses exercise
  both actual producers, followed by complete Debug/Release requalification.

Persistence reviewer: renewed final PASS for the corrected protocol, watermark
guards and inspected offline tests; updated native/diagnostics hashes independently
match the renewed candidate identity. No blocking finding.
Native reviewer: final PASS after independently matching all 951 source files,
134 canonical package files, aggregate identities and Git HEAD. Renewed Debug
and Release gauntlets each pass 8,284 checks; all three exact-wrapper CPU runs
recover 35,303 produced/durable records with zero faults and one rotation each.
Failed submit -4 preserves successful/completed submission 1799.
Those final outputs are recorded in qualification.json. Static review found no
additional Vulkan API version, extension/feature, buffer usage, queue submission,
synchronization or rendering-quality requirement from minimum recording.

No review claims exact terminal API recovery, present-to-scanout proof, GPU
qualification, blackout closure or Player PASS. Unpersisted faults remain part of
the explicitly uncertain suffix.
