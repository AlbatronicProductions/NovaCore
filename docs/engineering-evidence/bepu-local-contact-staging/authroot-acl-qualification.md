# AuthRoot ACL qualification: exact denial proven, baseline unqualified

2026-09-12. UNBANKED. No ACL repair was proposed or performed.

## Baseline

Branch: `codex/bepu-local-contact-staging`.
HEAD/main/origin/main: `3933d832b73511a8d08468b7b27a9b2820e2f8ee`.
M14.17 target: `a890a12c5cfb6c1eb4ee15906a21161bc4930988`.
Host ALBATRONIC007: Windows 11 Pro, 25H2, build 26200.9445, x64,
WORKGROUP; no domain/Azure AD/workplace join reported. The registry ProductName
still says Windows 10 Pro; build and DisplayVersion identify the current release.
SDK 10.0.303, runtime 10.0.12. CryptSvc is running/automatic as NETWORK SERVICE.

## Proven CryptSvc denial

The prior [CAPI2 witness](host-trust-witness.json) records six event-60 failures
at 12:40:25 EDT: PID 5196, NETWORK SERVICE (`S-1-5-20`), machine AuthRoot,
`CERT_STORE_PROV_SYSTEM_REGISTRY_W`, flags `0x20000`, result 5.

Project Control manually performed one additional ordinary chain/open attempt
with ProcMon capturing. Attempt logs span 13:03:22.8589864 to
13:03:35.0172345 EDT. The service snapshot identifies CryptSvc PID 5196 and
NETWORK SERVICE at capture time. The [unchanged filtered CSV](phase2-authroot-denial.csv)
contains 12 matching denials, all on the same key, from 13:03:23.1050813 through
13:03:23.4841331 EDT. CSV SHA-256:
`984BC270C0F4EDF4B76DA79632B6F43CBA16597D7A44067772D0CAD0C6594A49`.
Size: 2,116 bytes. No bulk PML was retained in this evidence package.

The attempt still reports `CERT_E_UNTRUSTEDROOT (0x800b0109)` and a passed
leaf revocation check. Its reported process exit is zero; that is not trusted
chain success. [Structured phase-2 witness](phase2-authroot-witness.json).

## Exact denied registry path

`HKLM\SOFTWARE\Microsoft\SystemCertificates\AuthRoot\CRLs`

This is the first denied AuthRoot operation in the requested filtered capture.
Every captured denial names CRLs. No CTLs denial was captured, so CTLs is neither
cleared nor independently proven to require the same correction.

## Exact registry operation

ProcMon operation: `RegCreateKey`. Process: `svchost.exe`, PID 5196.
Result: `ACCESS DENIED`. The key already exists; the operation name does not
establish that a new key was created. No successful mutation is claimed.

The CSV has no stack or per-event user field. Service identity is supported by
the capture-time service snapshot and earlier CAPI2 record, not invented from
the CSV. No broader process/stack capture was needed to identify this denial.

## Desired access

Exact ProcMon text: **Read/Write, Delete**. A raw numeric mask was not exported.
Do not replace this observed request with a presumed FullControl request.
[Microsoft registry-rights documentation](https://learn.microsoft.com/en-us/windows/win32/sysinfo/registry-key-security-and-access-rights)
distinguishes these rights from KEY_ALL_ACCESS.

## Store mapping

Machine AuthRoot maps to `HKLM\SOFTWARE\Microsoft\SystemCertificates\AuthRoot`.
Certificates, CRLs and CTLs hold the physical store's respective object kinds;
AutoUpdate contains automatic-root update state, including EncodedCtl.
[CertOpenStore](https://learn.microsoft.com/en-us/windows/win32/api/wincrypt/nf-wincrypt-certopenstore)
documents the registry provider and these object subkeys;
[Microsoft CTL documentation](https://learn.microsoft.com/en-us/windows-server/identity/ad-cs/certificate-trust)
documents automatic-root CTL storage. Neither establishes every child's default ACL.
The trace proves CRLs access participates in this failing writable store open.

## Current ACL SDDL

[Exact five-key export](phase2-authroot-acls.json) retains owner/group, protection,
ACE order, SIDs, masks, inheritance/propagation flags and SDDL. It covers
owner/group/DACL; SACL was not requested. All five descriptors remained identical
on final readback.

| Key under AuthRoot | Owner/group | DACL protected | ACEs | CryptSvc service SID grant |
|---|---|---|---:|---|
| AuthRoot itself | SYSTEM/SYSTEM | No | 13 | Present |
| AutoUpdate | SYSTEM/SYSTEM | No | 13 | Present, inherited |
| Certificates | SYSTEM/SYSTEM | No | 13 | Present, inherited |
| CRLs | SYSTEM/SYSTEM | Yes | 6 | Absent |
| CTLs | SYSTEM/SYSTEM | Yes | 6 | Absent |

Exact current CRLs descriptor:

```text
O:SYG:SYD:PAI(A;CIIO;KA;;;CO)(A;CI;KA;;;SY)(A;CI;KA;;;BA)(A;CI;KR;;;BU)(A;CI;KR;;;AC)(A;CI;KR;;;S-1-15-3-1024-1065365936-1281604716-3511738428-1654721687-432734479-3232135806-4053264122-3456934681)
```

There is no explicit deny ACE, nor an ACE for NETWORK SERVICE or the CryptSvc
service SID. The descriptor is canonical and protected. These are current facts,
not a proven healthy-default delta.

## Expected/default ACL SDDL

**UNQUALIFIED for the exact CRLs child.** No substitute SDDL is offered.

## Expected ACL provenance

Official store/CTL documentation explains access and location but no exact
healthy CRLs descriptor was located. Windows INF/default-template searches did
not yield one. No validated equivalent healthy 25H2 machine/VM was available;
common VM tooling was absent and the local Hyper-V data directory was not
readable non-elevated. This does not establish that no VM exists anywhere.

Read-only inspection decoded 20 targeted installed CAPI2/cryptography/component
manifests. [Manifest identities and findings](phase2-manifest-witness.json) retain
source and decoded SHA-256 values. Two manifests bind this same descriptor to
the **AuthRoot parent**:

```text
D:AI(A;CIOI;GA;;;S-1-5-80-242729624-280608522-2219052887-3187409060-2225943459)
```

Sources: Microsoft-Windows-CAPI2-certs and Microsoft-Windows-SF-CAPI2,
amd64, component version 10.0.26100.1, installed on this 26200.9445 host.
The service SID matches `sc showsid CryptSvc`; `sc qsidtype CryptSvc` reports
UNRESTRICTED. SF-CAPI2 describes its parent grant as Full Control for CryptSvc.
Neither manifest defines the CRLs or CTLs child's complete default descriptor.
CAPI2-certs names Certificates without a local descriptor.

The individual manifest-to-Microsoft-catalog binding was not verified. XML
identity and signed Windows decoding DLLs alone do not authenticate a manifest.
Even authenticated parent-only declarations would not qualify an exact protected
child baseline. Parent copying or speculative inheritance remains prohibited.
No forum ACL was used as repair input.

## ACL delta

**Not qualified.** The observed child lacks the parent's service grant, but no
healthy-child evidence establishes the required ACE, mask, owner, protection or
inheritance state. Requested access is not itself the default security policy.

## Root cause refinement

The machine AuthRoot store failure is narrowed to a denied CRLs key open/create
request by the CryptSvc process. The trace resolves which child fails; it does
not identify who changed its ACL, when it changed, whether policy intentionally
protected it, or the correct default repair. No component-store corruption or
other protected child's failure is established.

## Minimum repair proposal

**Withheld.** Both required proofs are not complete. Next responsibility: obtain
a provenance-qualified CRLs owner/group/DACL from a healthy equivalent Windows
11 Pro 25H2 x64 installation, recording exact build and domain/MDM state and
checking that its normal AuthRoot servicing works. Alternatively qualify a
Microsoft default resource that explicitly determines this child descriptor.
Compare before designing any one-key correction. No further reproduction of the
already-proven denial is needed for that step.

## Rollback proposal

No mutation command or rollback command is offered without a qualified target.
The current owner/group/DACL export is retained as pre-change evidence. A future
authorized repair must refresh that backup and preserve unrelated rights and
owner/protection state unless their defect is independently established.

## Security posture

No ACL, ownership, inheritance, trust root, policy, signature/revocation setting,
or production dependency verification setting was changed. No CryptSvc reset,
DISM repair, SFC, catroot2/SoftwareDistribution reset or registry-tree reset ran.
The earlier policy-blocked automated ProcMon launch was not retried; Project
Control manually captured the one attempt. No security bypass was introduced.

## CTL validation plan

Only after separate repair authorization: apply the proven one-key correction,
verify its descriptor, run normal CTL servicing, inspect actual registry
EncodedCtl freshness and chain trust, and check that the denied operation is
resolved. Restart CryptSvc only if supported behavior requires it.

Current registry EncodedCtl remains 175,647 bytes, SHA-256
`4276AF3669A141A59388BC56A87F6614D9A9BDDDF560636C264219A7EB11256F`,
unchanged from the previously decoded November 7, 2023 CTL. The earlier elevated
download of a newer CTL and changed LastSyncTime did not prove registry refresh.

## Bepu verification plan

After Windows trust works normally: verify exact BepuPhysics 2.5.0-beta.29 twice
and BepuUtilities 2.5.0-beta.29 twice with normal signature/revocation behavior;
then perform two independent clean exact-version restores and validate the
complete lock/deployment identity. Stop on failure. None ran in this phase.

## NovaCore repository state

501 retained baseline fingerprints were compared. The only baseline difference
is the already-authorized `NovaCore.Simulation.csproj` proposal, SHA-256
`B36A1385F5B603AEFAE79FF910A895AD68F4856B7509DA8EFBB530845563712D`.
No implementation or permanent test changed. Branch/remote/tag refs unchanged;
automatic Codex capture refs are excluded from that comparison. Nothing staged.
`git diff --check` passes; only an existing line-ending warning is reported.

```text
 M src/NovaCore.Simulation/NovaCore.Simulation.csproj
?? docs/engineering-evidence/bepu-local-contact-staging/
```

## Architecture status

**ADAPT BEPU BEHIND NOVACORE AUTHORITY** remains unchanged. Adapter work remains
blocked and UNBANKED. No milestone assigned.

## Reproduction and evidence lifecycle

[Manual procedure](authroot-manual-capture.md) preserves the one-attempt capture
steps; it need not be rerun. The filtered CSV is the decisive syscall witness.
The ACL and manifest JSON files preserve exact current descriptors and provenance
limitations. Decoding used installed MSDelta ApplyDeltaB with servicing-stack
wcp.dll dictionary resource type 614; only ignored scratch XML was written.
Source manifest paths/hashes are retained rather than bulk Windows resources.
The ignored `build/bepu-trust-recovery/host-recovery/phase2` directory contains
the small decoder, XML and manual-attempt logs for current reproduction.
No scratch cleanup or retry of previously blocked deletion was performed.

## Lead judgment

**ESCALATE TO PROJECT CONTROL**

## Final classification

**AUTHROOT ACL DEFECT — EXACT DENIAL PROVEN, REPAIR BASELINE UNQUALIFIED**

STOP FOR PROJECT CONTROL.
