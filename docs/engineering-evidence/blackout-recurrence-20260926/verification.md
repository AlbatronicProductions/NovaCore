# Verification and adversarial review

**Forensic preservation PASS; blackout causal closure ESCALATE.** These are different judgments. No new runtime qualification is claimed.

Phase0 verified the repaired seal, captured current Git/index/refs and all nonignored entry-file identities, and compared every frozen source/package file without a build. Incident runtime fingerprints and App PE metadata independently link the session to the preserved candidate. Final preservation is recorded in `verification.json`; the final check rehashes the seal, all entry files, source/package, index and refs. New files are confined to this report package and the separate ignored analysis directory.

Independent questions and findings:

1. **GPU / lifetime reviewer:** traced successful fence/query semantics; established frame 33978 as completed; rejected assigning 3.308 ms to an exact frame; distinguished present return from scanout; checked publication and mapping ownership after the fence; found no terminal handle/dependency evidence.
2. **Windows reviewer:** falsified Event 6008 as incident onset using independent RestartManager records; interpreted zero Event 41 fields without assigning PSU causality; mapped PnP warnings to virtual devices; compared WHEA boot data; identified LiveKernelReports access denial.
3. **Route reviewer:** compared the old Debug observer route, current Release route and successful ordinary session; falsified factor 64, near-ground camera, resizes, 4,008 kg mass or package alone as sufficient causes; separated the first clock refusal from the second launch and from blackout.

Lead checks reproduced the log extraction, hash/MVID comparisons, raw event preservation, source call ordering, and lifecycle source-hash attribution. A progress update initially called two launches refused; the source-hash join shows **one proven refusal** and a second attempt with no retained final state. The final record uses that corrected distinction.

The final adversarial read corrected one inference from process counters: declining NovaCore private bytes/working set do not measure total system commit or GPU pressure. The reviewer confirmed the corrected scope and final preservation record, with no remaining finding from that narrow review. This does not resolve blackout causality.

No verifier proposed a positively supported blackout owner. The lead did not turn consensus or absent alternatives into a causal PASS. No renderer, shader, support, RCS, construction or simulation change was made. Debug/Release builds and live regressions were deliberately not rerun because no correction was justified and the first forensic phase prohibited builds.

Raw/derived preservation details:

- `sealed-verification.json`: every manifest row, original length/hash, measured length/hash.
- `git-entry.json`, `git-index.bin`, `git-head`, `git-refs`, `git-working-tree.diff`, `git-index.diff`, `repository-entry-files.json`: current entry preservation; no Git state change.
- `current-source-manifest.json`, `current-candidate-package.json`, `candidate-identity.json`: 926 source / 127 package matches, no package extras.
- `session-analysis.json`: exact source lines, batch times, sampled metrics, overflow tally, lifecycle and fingerprints; raw log stays in the seal.
- `additional-os/System.evtx`, `Application.evtx`, paired raw XML / formatted JSON: independently preserved OS records; both EVTX exports exit 0.
- `additional-os/channels/`: relevant raw channel XML / formatted JSON and query/access results.
- `additional-os/whea-paired-332-347.xml-fragments`: prior/current boot WHEA comparison.
- `additional-os/dump-wer-inventory.json`: empty/absent/inaccessible states distinguished. Initial broad channel metadata collection encountered a null LastWriteTime in some channels; supplemental explicit-channel collection handles null and retains access errors. That collector issue is not an OS failure.

No dump, sealed log, source, cache or build output was deleted. Unresolved evidence remains retained. Project Control must decide the next proof; this review does not authorize an ordinary player run.
