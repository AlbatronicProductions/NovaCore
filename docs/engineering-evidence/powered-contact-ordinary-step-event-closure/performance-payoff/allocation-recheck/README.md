# Active diagnostic contract migration and recheck

Project Control subsequently accepted the ownership audit's contract B. The
previous stopped correction is unchanged historical evidence, not rewritten PASS.

Start with [the final bounded result](../qualification-result.md).

- [Accepted contract and allocation plan](contract.md)
- [FP32 and exact-ledger comparison](fp32-equivalence.json)
- [Allocation result](allocation.json), [raw counts](allocation-witness.txt)
- [Single native witness comparison](native-equivalence.json)
- [Timing preregistration](timing-plan.md)
- [Reproduction](reproduce.md), [identity](identity.json), [cleanup](cleanup.md)

No production or permanent test change. No new mapper/arithmetic, cache, pool,
KSA mechanism or milestone. The reused Map remains unchanged; the new active
acceptance logic exists only in generated diagnostic scratch from recheck.ps1.

Exact-zero allocation and native equivalence PASS. Three-process results are
retained in the parent directory. Overall judgment:
**PASS — PERFORMANCE / PAYOFF UNCERTAIN**, due to repeating preparation-side
tails and the explicitly bounded measurement scope. Stop for Project Control.
