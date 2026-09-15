# Reproduce the bounded review

Do not run old solver qualification or change a candidate from these instructions. This ticket uses read-only source/history inspection and one standalone arithmetic witness.

## Repository identity

In PowerShell 7:

~~~powershell
Set-Location -LiteralPath 'E:\NovaCore'
git rev-parse HEAD main origin/main
git ls-remote origin refs/heads/main
git branch --show-current
git status --short
git diff --check
git diff --cached --check
git for-each-ref --format='%(refname) %(objectname)' refs/tags
~~~

Expected shared commit: 49057fecceb0f725d5f551ec40e2780971b0d81d. Compare 65 tag refs in identity.json; do not move them.

Protected starting file identity (excludes only this new evidence directory; no output file is written):

~~~powershell
$protected = @(git ls-files --cached --others --exclude-standard |
  Where-Object { -not $_.StartsWith('docs/engineering-evidence/powered-contact-event-lifecycle-convergence/') } |
  Sort-Object -Unique)
$rows = foreach ($p in $protected) {
  $p + ' ' + (Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash
}
[pscustomobject]@{
  Files = $protected.Count
  AggregateSHA256 = [Convert]::ToHexString(
    [Security.Cryptography.SHA256]::HashData(
      [Text.Encoding]::UTF8.GetBytes(($rows -join [char]10))))
}
~~~

Expected 2370 / 92FB58DA0CEE9CDDBBF7F42EAFB90DA1D10B02C252535BA86410A5EC74042A4D. This excludes ignored build products; it is not a claim to inventory all unrelated machine scratch.

## Actual installed KSA

~~~powershell
@('KSA.dll','BepuPhysics.dll','BepuUtilities.dll','KSA.deps.json') |
  ForEach-Object {
    $p = Join-Path 'E:\Kitten Space Agency' $_
    Get-FileHash -LiteralPath $p -Algorithm SHA256
  }
(Get-Item -LiteralPath 'E:\Kitten Space Agency\KSA.dll').VersionInfo.ProductVersion
~~~

Match currentKsa in identity.json and ksa-current-lifecycle.md: current5438 KSA.dll A03E9815... . Existing decompilation E:\NovaCore\build\ksa-residency-reference\assembly-source\KSA and its 24 hashes belong to initial5402 only, retained as readable historical evidence. Do not relabel that source as5438 or substitute it for current binary method/operand inspection.

Example of the same read-only installed PE/IL hash inspection used by the reviewer; repeat with listed method tokens as needed:

~~~powershell
$assemblyPath = 'E:\Kitten Space Agency\KSA.dll'
$methodToken = 0x0600153B # Current5438 Combustor.ConsumePropellant
$stream = [System.IO.File]::OpenRead($assemblyPath)
$pe = [System.Reflection.PortableExecutable.PEReader]::new($stream)
try {
  $metadata = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
  $handle = [System.Reflection.Metadata.Ecma335.MetadataTokens]::MethodDefinitionHandle(
    $methodToken -band 0x00FFFFFF)
  $method = $metadata.GetMethodDefinition($handle)
  [byte[]]$il = [System.Reflection.Metadata.PEReaderExtensions]::GetMethodBody(
    $pe, $method.RelativeVirtualAddress).GetILBytes()
  [pscustomobject]@{
    Method = $metadata.GetString($method.Name)
    Token = '{0:X8}' -f $methodToken
    ILBytes = $il.Length
    ILSHA256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($il))
  }
} finally {
  $pe.Dispose()
  $stream.Dispose()
}
~~~

Expected current5438 ConsumePropellant / 0600153B / 18 IL bytes / A826E9D5BC486ED5370CC993C00C22E05DEC9B551185A3D6C2B13D954E35C659. This does not execute KSA. Use metadata operand/token resolution for call/field meaning; raw byte hashing alone is identity, not semantic proof. Decisive current call order is retained in ksa-current-lifecycle.md; old5402 tokens remain separate. Another changed binary requires affected-method inspection, not broad automatic decompilation.

## Actual retained engineering history

Open message links in ksa-live-changelog.md in the authenticated KSA live-changelog channel. Read date, revision and relevant content directly. Do not rely on a copied NovaCore summary. Compare history with actual5438 methods; revision5434 discussion is not itself proof of every fix or exact shortage. Preserve the old5402/current5438 distinction. No export is needed.

## Single data-only arithmetic witness

~~~powershell
python -B 'E:\NovaCore\docs\engineering-evidence\powered-contact-event-lifecycle-convergence\analytic-witnesses.py'
~~~

Standard library Fraction and Decimal only; no candidate/runtime imports, no file creation, no native solver. Compare stdout with analytical-results.json. One run was executed for this ticket; this command is retained for reproduction, not permission to resume a numerical campaign.

## Evidence and cleanup

README.md indexes the complete lifecycle, 14 outcome rows, 12 convergence rows, 10 mechanism dispositions, test categories, mathematical proof/limits, 3 architectures and independent 24-attack review. Old paired-response evidence is referenced in place.

No new disposable build output. Previously reviewed paired bin/obj are absent; cleanup.md retains exact historical manual command and non-destructive verification. Do not run deletion on absent paths or broaden it to retained evidence.
