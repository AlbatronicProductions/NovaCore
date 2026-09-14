# Independent red-team result

**PASS — implementation and final ratio-oracle correction approved.**

Separate read-only reviewers inspected arithmetic/capacity and the complete source/lifetime/test boundary. Only the lead wrote candidate files. Reviewers did not execute tests; measured results in validation.json were executed by the lead.

| # | Attack | Evidence / disposition |
|---:|---|---|
| 1 | Floating equality enters authority | Integer U/V/C decisions; false floating equality permanent witness |
| 2 | Tiny debit disappears | Exact 60-interval epsilon ledger and 4,096 tiny proposed successor chain |
| 3 | Interior duration rounds to zero | Exact positive ratio retained; optional FP64 duration API omitted |
| 4 | Endpoint/interior confused | Quantum C-1/C/C+1 and independent zero-first classification |
| 5 | Limb capacity exceeded | 2,176-bit buffer versus proved 2,161-bit largest demand; long.MaxValue tests |
| 6 | Carry/borrow wrong | UInt128 intermediates, every limb-boundary chain, seeded independent multipliers |
| 7 | Negative successor | Compare/exact subtract; underflow returns default unusable result |
| 8 | Positive demand with zero flow | Genuine underflowed M14.23 source refuses IncompatibleFlow; parent retained |
| 9 | Stale engine proposal | Genuine active parent/source reader; external state/clock/timeline/command changes refuse |
| 10 | Stale resource proposal | Private seal/generation; resource source/revision/model corruption test refuses |
| 11 | Double spend | No spending method; one issued resource and one outstanding conditional lease |
| 12 | Starvation rewrites intent | NoFeed output preserves copied Enabled latch and canonical command snapshots |
| 13 | Proposal presented as consumption | ProposedIfApplied enum, no canonical fuel writer, exact before/after snapshots |
| 14 | Wet/dry mass double counted | Explicit dry point model; cold mass/inertia mismatch refuses before binding |
| 15 | Contact owns resource | No contact calls in resource production; article uses a zero-resource adapter only |
| 16 | Managed allocation | Eight exact zero gates in Debug/Release; 152-byte positive control |
| 17 | Parent lifetime loses authority | Retirement stales dependent preview; resource cancellation preserves parent and fresh lease rule |
| 18 | Qualification overclaimed | Nonphysical only; cost excludes retirement; no changing-mass/powered-contact claim |

## One bounded verifier correction

Initial test review found that conservation and duration bounds alone did not independently prove which duration was powered. An implementation swapping powered and remainder could satisfy those checks.

The final helper now independently derives BigInteger min(U,C), successor and classification, and checks both powered and remainder ratios by cross multiplication, including zero-flow behavior. Both focused Debug/Release correctness groups passed after rebuilding. The verifier re-read the changed helper and closed the finding: PASS, no remaining blocker. No production correction was required.

Earlier arithmetic review also recommended sign/exponent-bit rejection for negative subnormal/nonfinite decode; the implementation includes it, avoiding dependence on a floating negative comparison. Canonical ticks-per-second consistency is permanently asserted.

## Limits of the evidence

The M14.21 witness has zero resource and nonzero conditional engine demand; it proves resource preparation/retirement leaves the original physical trajectory untouched. Synthetic positive-resource fixtures independently prove nonmutation and conditional mass/segment results.

The first resource source is cold immutable. Reflection-only tests corrupt source facts to exercise future staleness checks; no mutation/failure framework was added to production. Pure repeated successor arithmetic is an oracle workload, not a canonical burn API.

Full suites and all allocation gates passed before the final oracle-only strengthening; that complete focused group passed afterward in both configurations. Production DLL and measurement-source identities remain unchanged. No performance rerun or forensic campaign was needed.

No banked M14.21/22 behavior, M14.23 proposal math/capture semantics, BEPU binary, public time, renderer, Blender or physical contact definition changed.
