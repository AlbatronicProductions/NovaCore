# Exact Bepu dependency trust recovery

2026-09-12. **DEPENDENCY TRUST BLOCKER — UNRESOLVED**. UNBANKED.
Architecture remains **ADAPT**; no adapter implementation has resumed.

## Baseline

Branch `codex/bepu-local-contact-staging`; HEAD/main/origin/main remain
`3933d832b73511a8d08468b7b27a9b2820e2f8ee`. M14.17 remains
`a890a12c5cfb6c1eb4ee15906a21161bc4930988`.
The sole tracked candidate change is the pre-existing exact-version
PackageReference proposal in `src/NovaCore.Simulation/NovaCore.Simulation.csproj`.
No implementation, permanent test, branch, tag or staging change occurred.

## Toolchain / OS

Windows 10.0.26200 x64; PowerShell 7.6.6; SDK 10.0.303; NuGet 7.6.0.0;
.NET host 10.0.12. Only SDK 10.0.303 is installed. Also installed are runtime
6.0.36, 8.0.31, 9.0.20 and 10.0.11; no runtime selection change was made.
Timezone is Eastern Standard Time (EDT on observation date).
At observation, the NuGet HTTPS Date header was 2026-09-12 16:15:00 UTC and
the local UTC clock was 16:15:01.1144494. No material clock skew was observed.
Windows Time was stopped/manual, but this alone does not establish a time fault.
Cryptographic Services was running/automatic; Windows Update stopped/manual.
No services were changed.

## Effective NuGet trust policy

The only found effective configuration file is
`C:\Users\Tyler\AppData\Roaming\NuGet\NuGet.Config`, containing enabled
`https://api.nuget.org/v3/index.json`. No repository/ancestor configuration,
machine NuGet config file, trustedSigners section, source mapping or explicit
signatureValidationMode was found in the inspected hierarchy.
Default signature mode remains accept; normal signature checks still fail.

The process variables DOTNET_NUGET_SIGNATURE_VERIFICATION,
NUGET_EXPERIMENTAL_CHAIN_BUILD_RETRY_POLICY, NUGET_CERT_REVOCATION_MODE,
HTTP_PROXY, HTTPS_PROXY, ALL_PROXY and NO_PROXY were unset. WinHTTP reports
direct access. No credentials or certificate private keys were collected.

## Exact package identities

Both packages were acquired directly from NuGet.org's v3 flat container.
Version remains **2.5.0-beta.29**. DLL hashes matched the retained assessment.
Nuspec license/source identity remains Apache-2.0 and Bepu source commit
`f73164bb3c9ca733eb3329f1f6b1cea4e216ece7`.

| Package | File bytes | Package SHA-256 |
|---|---:|---|
| BepuPhysics | 442721 | D4A952F5417B53B08DE20D507C21745C9DD004E2529864098DFEC7A2CFF11E42 |
| BepuUtilities | 149735 | 8168EC4905F3AD5F923564557DAAB2BAB1100EE46B424CA5F1C97BD7684DE19F |

NuGet's emitted content hashes (distinct from SHA-512 of the signed zip):

- Physics: `+/MSXnRpK3dPWQmVKL+dfO05gXcbqDlOUmHIvy2sAUiLFuSOga7E9AfcEwMMHn76Rdlz9ZKWYE+WyJJLIqmGbg==`
- Utilities: `FF+QQpNQ3bP1PBzTp/rbRgIcSLNTbUjNU0KQAjl9aQ/zXGnwkh9oGcMFCO5Q/E9cMr0HOZlGkvhs4+SIgXFq5g==`

The previous assessment recorded DLL identities, not full signed-package hashes.
Therefore full-package hash continuity with that earlier download is not claimed.
Current package identity, embedded DLL equality and observed signing identity are
recorded separately; none substitutes for a passing trust check.

## Direct signature verification

Two normal detailed verify processes per exact package, with no bypass flags:

| Package | First verify | Bounded retry |
|---|---|---|
| BepuPhysics | exit 1; NU3018 + NU3028 | exit 1; same errors |
| BepuUtilities | exit 1; NU3018 + NU3028 | exit 1; same errors |

Each reports repository primary signing by NuGet.org, owner
`bepuentertainment`, SHA256 signature algorithm and timestamp
2026-04-18 18:40:39 local time. Both return two errors and four revocation
warnings. This was not resolved by a normal first-seen-root retry.

## Signing certificate chain

| Role | Certificate | SHA-256 |
|---|---|---|
| Leaf | NuGet.org Repository by Microsoft | 1F4B311D9ACC115C8DC8018B5A49E00FCE6DA8E2855F9F014CA6F34570BC482D |
| Intermediate | DigiCert Trusted G4 Code Signing RSA4096 SHA384 2021 CA1 | 46011EDE1C147EB2BC731A539B7C047B7EE93E48B9D3C3BA710CE132BBDFAC6B |
| Root, direct chain | DigiCert Trusted Root G4 | 552F7BDCF1A7AF9E6CE672017F4F12ABF77240C78E761AC203D1D9D20AC89988 |

Leaf validity: 2024-02-23 through 2027-05-18/19, depending on timezone;
EKU Code Signing; certificate signature sha384RSA. Current NuGet detailed
verification constructs the chain but reports UntrustedRoot,
RevocationStatusUnknown and OfflineRevocation.

## Timestamp certificate chain

| Role | Certificate | SHA-256 |
|---|---|---|
| Leaf | DigiCert SHA256 RSA4096 Timestamp Responder 2025 1 | 4AA03FA22CD75C84C55C938F828E676B9CAECAB33FE36D269AA334F146110A33 |
| Intermediate | DigiCert Trusted G4 TimeStamping RSA4096 SHA256 2025 CA1 | CA0B1554ECD901EA19DCAD8749E9F2648C8D6DFCEA1ADD9D2C2109415BB82CCD |
| Cross certificate | DigiCert Trusted Root G4, issued by Assured ID Root CA | 33846B545A49C9BE4903C60E01713C1BD4E4EF31EA65CD95D69E62794F30B941 |
| Root | DigiCert Assured ID Root CA | 3E9099B5015E8F486C00BCEA9D111EE721FABA355A89BCF1DF69561E3DC6325C |

Timestamp leaf validity: 2025-06-04 through 2036-09-03/04;
EKU Time Stamping; certificate signature sha256RSA. Normal trust fails here too.
`trust-evidence.json` records exact local ISO validity intervals, element statuses,
EKUs, signature algorithms and endpoint extensions for both online X509 chains.
The general X509 signer build selected the cross-signed route; NuGet also showed
the direct G4 route. Neither was trusted. Package-embedded certificates were
provided only as untrusted chain candidates, never installed in a trust store.

## Revocation / network

The precise HTTP CRL URLs extracted from the certificates all returned 200:
G4 CodeSigning 2021 CA1 (crl3 and crl4), G4 TimeStamping 2025 CA1,
TrustedRootG4 and AssuredIDRootCA. The signer CRL was current:
ThisUpdate 2026-09-12 04:04 local; NextUpdate 2026-09-19 04:04 local.
The G4 issuer download and Windows authroot CAB also returned 200. DNS resolved.
GET of the OCSP host returned 200, which is only reachability evidence, not an
OCSP certificate-status proof.

Native `certutil -verify -urlfetch` independently reached CRL/OCSP URLs and
reported verified responses for the signing chain; its leaf revocation check
passed. Timestamp verification still reported CRYPT_E_REVOCATION_OFFLINE
0x80092013. Both native chains reported CERT_E_UNTRUSTEDROOT 0x800b0109.
Native certutil's process exit 0 is NOT a successful trust result: the textual
chain errors govern. No revoked status was established, nor was revocation
fully cleared. Endpoint-wide outage is not supported by these observations.

No proxy variables or WinHTTP proxy were configured. A TLS interception source
was not identified; no claim of exhaustive endpoint-security inspection is made.

## Root / intermediate trust state and winning cause

**B. MISSING OR STALE TRUSTED ROOT** is the best-supported classification.

Both relevant roots are absent from CurrentUser and LocalMachine Root/AuthRoot
stores; neither was found in the corresponding Disallowed stores. The signing
intermediate exists in CurrentUser CA. Timestamp intermediates are available
from embedded signature material, and the observed chains are complete, so a
missing intermediate is not the demonstrated primary blocker.

`certutil -verifyCTL AuthRoot` for the exact G4 root found its CTL entry, but:

- LastSyncTime: **2024-01-29 07:54 local**.
- CTL ThisUpdate: **2023-11-07 16:36 local**.
- CTL signer expiration: **2024-03-14 14:04 local**.
- Error: **CERT_E_EXPIRED, 0x800b0101** for SignerExpiration.

The root certificates themselves are within their validity intervals. This is
stale local Windows trusted-root material, not an expired Bepu signing leaf.
The inspected policy keys contain no DisableRootAutoUpdate override; automatic
root discovery did not repair the problem during these observations. The
underlying reason servicing stopped is not yet proven.

## Remediation attempted and remaining review

Only Microsoft's documented trust-list servicing was attempted, without trust
policy changes or certificate imports. A local-directory `-f -verifyCTL AuthRoot`
attempt retained the stale CTL and reported missing local CTL certificate files;
it did not prove a refresh. An empty-directory argument attempt returned path
not found and is excluded as a remediation result.

The explicit Windows Update path was then tested:

```powershell
certutil -f -verifyCTL AuthRootWU `
  'http://ctldl.windowsupdate.com/msdownload/update/v3/static/trustedr/en/' `
  'E:\NovaCore\build\bepu-trust-recovery\root-g4.cer'
```

Result: **0x80070005 ERROR_ACCESS_DENIED**. No elevation was initiated. The
exact denied resource is not established; elevation alone is not claimed to
resolve it. Project Control should review normal Windows root-certificate
servicing/access before another repair attempt. Do not import the exported
package leaf or root as an inferred workaround, change registry/Group Policy,
or disable revocation checking.

Official references:
[NuGet trust diagnosis](https://github.com/NuGet/Home/wiki/Signing-certificate-is-not-trusted-by-the-trust-provider),
[certutil servicing options](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/certutil#-verifyctl),
[NU3028](https://learn.microsoft.com/en-us/nuget/reference/errors-and-warnings/nu3028).

## Fresh restore / cache control

Normal fresh restore used only NuGet.org, `--no-http-cache --force`, and a new
task-owned package directory. It failed with NU3018/NU3028 for both packages,
exit 1. No successful exact-package extraction/cache entry was produced there.
This reproduces outside the prior cache; corruption of the old cache is not the
winning explanation. No existing developer cache was cleared.

The failed restore's incomplete lock (project references only) was removed.
Ignored project.assets/restore metadata may reflect the failed attempt and is
not accepted deployment state. No resolved runtime DLL deployment is claimed.

Recovery bar remains **NOT MET**: zero successful direct verifications, zero
successful fresh Bepu restores; two clean successful repetitions are outstanding.
No additional retry loop or adapter implementation was started.

## Reproduction / evidence

Current exact packages and narrow raw diagnostics remain temporarily in
`build/bepu-trust-recovery` for the unresolved servicing handoff. No private key,
auth token or full certificate-store dump was retained. The redundant verbose
local-directory CTL output is disposable; its useful outcome is summarized above.
Automatic approval review rejected deletion of that one reviewed file with
`blocked by policy`. Nothing was deleted and no cleanup retry was made. Its exact
path is `E:\NovaCore\build\bepu-trust-recovery\authroot-refresh.txt` (153800 bytes).
Do not retain this scratch in Git. Concise evidence budget for the combined
candidate dependency reports is now 192 KiB, including baseline fingerprints and
certificate-chain evidence.

Run normal direct verification with:

```powershell
dotnet nuget verify build/bepu-trust-recovery/BepuPhysics.2.5.0-beta.29.nupkg --all --verbosity detailed
dotnet nuget verify build/bepu-trust-recovery/BepuUtilities.2.5.0-beta.29.nupkg --all --verbosity detailed
dotnet restore src/NovaCore.Simulation/NovaCore.Simulation.csproj --source https://api.nuget.org/v3/index.json --packages build/bepu-trust-recovery/fresh-packages --no-http-cache --force --verbosity minimal
```

After a reviewed repair, use a distinct empty package directory for each of two
normal verification/restore repetitions; retain exact hashes and complete lock
results. Only then resume the existing adapter ticket on the current branch.

## Security posture / judgment

No signature bypass, allow-untrusted-root option, warning suppression, package
substitution, policy edit, certificate import, cache purge, endpoint-protection
change or direct DLL workaround was used. Ordinary chain/URL validation may
update normal cryptographic caches; no successful root-store installation was
observed and relevant roots remained absent.

Architecture: **ADAPT**. Resume status: **NOT RESUMED**.

Lead judgment: **ESCALATE TO PROJECT CONTROL**.

Final classification: **DEPENDENCY TRUST BLOCKER — UNRESOLVED**.

UNBANKED. STOP FOR PROJECT CONTROL.
