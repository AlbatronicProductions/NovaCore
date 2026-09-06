# Compatibility contract evidence

**PARTIAL RESOLUTION — PROJECT CONTROL DECISION REQUIRED.**

- [Report](../../compatibility-contract-resolution.md)
- [Primary contract matrix](contract-matrix.csv): 25 rows, all 13 requested columns.
- [Evidence](evidence.json): baseline, 11 on-disk pack header inventories, two verified production assets, 26 qualified authoring call sites, exact publication checks, KSA identities and source links.

Permanent package budget: **128 KiB including the report**. Retain the conclusions,
matrix, input identities and reproduction references. No videos, GPU captures,
runtime binaries, terrain assets or KSA source are included.

Original tracked-file SHA-256 comparison: **PASS (540 files unchanged)**.
Current production global/Florida SHA-256 and header checks: **PASS**.
CSV parsing/column completeness/unique IDs: **PASS**.
`git diff --check`: **PASS**. No staged changes; HEAD and milestone tag unchanged.
No code retirement, fixture migration, build or runtime validation was performed.

The audit used source inspection and bounded header/hash reads; no asset was
regenerated or removed. SHA-256 of a current pack is checked against its tracked
manifest; the inventory does not trust content-addressed filenames as proof.
Other old cache entries are header observations only and carry no cleanup decision.

To reproduce: inspect matrix source anchors at the recorded commit, enumerate
NCPE/NCCUBE/package files while excluding `.git` and reparse directories, read
format/payload/terrain identities, hash the two current manifest-selected packs,
and trace current producers/readers and internal friend-assembly callers. Repeat
public release/registry observations when resolving external support; they cannot
prove all external consumers absent.

Final status is recorded after this file exists. No stage/commit/push/tag operation.

```text
?? docs/compatibility-contract-resolution.md
?? docs/engineering-evidence/compatibility-contract-resolution/
```
