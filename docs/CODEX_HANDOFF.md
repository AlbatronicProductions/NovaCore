# Instructions to Codex

Before editing, read `/ENGINEERING_RULES.md`, `/docs/NOVACORE_CURRENT_STATE.md`, and `/docs/architecture.md`; inspect `git status --short`; then inspect the files named below. Repository contents win if this document becomes stale. Never discard current uncommitted sampler work.

## Immediate task

Continue the uncommitted adaptive sampler. First separate accepted interpolation knots from cached source/probe states. Keep probe cache internal. Returned knots must contain only accepted interval boundaries, retain exact start/end, be strictly increasing and deduplicated, and equal accepted interval count plus one. The proven Moon case has four accepted intervals and must return five knots, not 21 cached timestamps. Do not add RMS or cadence search in that first task.

Then add RMS/worst-error reporting, then deterministic seed-cadence search, then Earth/Sun validation. Use focused adapter build/tests and `git diff --check`; defer full regression to a checkpoint.

`external/naif/` is a required local ignored integration dependency. Never stage it. Do not reset, restore, stage, commit, tag, or push unless explicitly asked.
