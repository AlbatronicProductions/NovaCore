# Prospective-bank whitespace and provenance closure

This ticket changes whitespace/provenance only. Production physics, test tokens,
numerical expectations, accepted performance/manual results, public capability
claims and all five withheld legal drafts are unchanged.

## Exact scope

The 19 paths were read directly from final-review.md, verified present in the
589-path bank proposal, and checked against Git's prospective-addition diagnostics
before editing. The original report remains below its new current-status note as
historical evidence. No file was removed from the bank proposal to evade a check.

`whitespace-identity-migration.json` records each exact path, purpose, old/new
SHA-256 and byte count, BOM/newline inventory, removed byte offsets/hex, semantic
disposition and reverse proof. Only 84 bytes were removed:

- Observer: 65 spaces in the ten identified trailing sequences, at lines
  11, 22, 23, 48, 51, 53, 86, 88, 93 and 97.
- Seventeen retained documents/source files: one final LF each.
- PieceKernel.cs: one final CRLF blank line (two bytes); its remaining LF source
  bytes are unchanged. This is deletion of the identified line, not newline conversion.

All original encoding and BOM states are preserved. No formatter, reflow, JSON
sort or unrelated whitespace correction was used. Reverse insertion at the
recorded byte offsets produces all 19 exact old hashes. Thus no comment text,
source token, arithmetic, ordering or meaningful Markdown text was changed.

## Observer identity chain

1. Original historical:
   `FD96C55C31A3961D8597E42DCC4F3F30A5FD7881BC095DA4FDC6FE79E90035A4`
2. Accepted Apache attribution successor:
   `FE52EEB23E4F4A1411B809AACE29BC86B8C500B541BE741C3D6B6D53194E9D66`
3. Whitespace-normalized bank candidate:
   `6A81D91522B6C5E833A8377E62AFF9D3E87D752EB18119A56ABD6E3247609803`

Reinsert the recorded 65 space bytes to recover stage 2; then reverse only the
accepted Apache-2.0-to-MIT attribution replacement to recover stage 1. Both exact
hashes were recovered in memory. The first attribution migration JSON is untouched.

## Directly dependent generator change

`powered-contact-production-integration/generate-work-observer.ps1` is outside
the 19-file inventory but its supporting edit is expressly authorized: form the
same output string, require exactly ten trailing-whitespace matches, remove those
matches and write the output. No extraction, row selection, recorder placement,
upstream source recovery or attribution logic changed. The strict recovery runs
**before** output normalization. The generator's before/after identity and prior
write statement are retained in whitespace-generator-migration.json.
Replacing only that final output block with the retained prior write statement
reconstructs the exact prior generator SHA-256, independently verified in memory.

Pinned upstream commit remains `f73164bb3c9ca733eb3329f1f6b1cea4e216ece7`;
raw ContactConvexTypes.cs SHA-256 remains
`B259BD56D6EF9405FC98A38E97A9D6CA154CDA11CB908C901800298B7D771DA5`.
Apache-2.0 provenance is unchanged. Native arithmetic recovery PASS; twelve native
row call sites unchanged. Generated output equals the normalized observer
byte-for-byte. The final observer is intentionally whitespace-normalized, not
claimed to be an unmodified byte copy of upstream source.

## Current seals and historical reproduction

Only two current input seals required updating:

- qualified-source-identity.json: observer successor hash and byte count.
- ordinary-step-event-closure/candidate-a-reproduction-inputs.json: PieceKernel
  successor hash. All 51 current input seals now match. The wrapper's numerical
  result hashes, baseline guard and clean-source guard are unchanged.

The exact seal migrations are in whitespace-supporting-migrations.json.
Historical numerical identity.json files, event-lifecycle closing.json, earlier
consolidation manifests and the first attribution migration retain their hashes.
Current bank status metadata is updated separately; old qualification outcomes
are not reissued as new measurements.

The historical-only tiny-active-friction/reproduce.py reads its historical
candidateInputs, including old PieceKernel bytes. It is not a current production
reproduction route. To rerun it, use an isolated historical-baseline checkout and
restore that input's recorded historical bytes there before executing its unchanged
guard. Do not run it against the normalized current source and assume the old
hash will match. No broad numerical campaign was rerun here.

Cheap exact historical reconstruction (in memory, no source write):

```powershell
$root = 'E:\NovaCore'
$rows = Get-Content -LiteralPath "$root\docs\engineering-evidence\powered-contact-bank-candidate\whitespace-identity-migration.json" -Raw | ConvertFrom-Json
foreach ($row in $rows) {
    $path = Join-Path $root $row.path
    if ((Get-FileHash -LiteralPath $path).Hash -ne $row.newSha256) { throw 'Successor mismatch' }
    $text = [Text.Encoding]::Latin1.GetString([IO.File]::ReadAllBytes($path))
    foreach ($removed in ($row.removedByteRanges | Sort-Object byteOffset)) {
        $text = $text.Insert($removed.byteOffset, [Text.Encoding]::Latin1.GetString([Convert]::FromHexString($removed.hex)))
    }
    $bytes = [Text.Encoding]::Latin1.GetBytes($text)
    $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes))
    if ($hash -ne $row.oldSha256) { throw 'Historical reconstruction failed' }
}
```

For the isolated historical tiny-active input, use the PieceKernel row's resulting
`$bytes` as that checkout's PieceKernel file after verifying its old hash. This
restores exact historical input, not a change to current production or a weakening
of the historical guard. No old identity file needs editing.

## Cheap compile validation

Both commands passed with zero warnings/errors:

```powershell
dotnet build tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj -c Debug --artifacts-path E:\NovaCore\build\powered-contact-whitespace-closure\artifacts --nologo
dotnet build tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj -c Release --artifacts-path E:\NovaCore\build\powered-contact-whitespace-closure\artifacts --nologo
```

No full suite, allocation campaign, physics campaign, timing or manual qualification
was rerun. Their accepted evidence remains applicable to unchanged executable tokens.

## Prospective staging and cleanup

The exact current bank list is tested using a temporary GIT_INDEX_FILE, initialized
from HEAD, adding only bank-paths.json members through a NUL-delimited pathspec.
No real-index stage, commit, tag or ref operation is performed. The real index's
SHA-256 is checked before/after, alongside zero real staged paths.

Current results and exact bank count are in whitespace-result.json. Added closure
files are ENGINEERING EVIDENCE only; the previous 589 members remain included.
The five legal drafts stay outside the list; root LICENSE remains absent.
Temporary paths/counts and exact cleanup commands are in whitespace-cleanup.md.

Milestone M15.1 and its existing title/tag remain recommendations only.
No banking, departure or Florida work is authorized.

## Independent red team

A read-only independent verifier reconstructed all 19 old hashes and the prior
generator hash, verified 26/26 qualified source seals, 51/51 current reproduction
seals and 8/8 protected public/legal hashes, and checked historical PieceKernel
identities and the documented isolated reconstruction route. It found no scope,
semantic, attribution, legal or reproduction blocker. It also confirmed the real
index hash, zero staged paths, unchanged HEAD and all 65 historical tag refs.
The complete prospective-bank check and disposable cleanup are lead-owned checks,
reported separately; the verifier did not claim pending work had passed.
