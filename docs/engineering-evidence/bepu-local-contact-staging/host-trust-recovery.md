# Windows trusted-root servicing: AuthRoot access failure

2026-09-12. **WINDOWS TRUST SERVICING — FOLLOW-UP REQUIRED**.
Lead judgment: **ESCALATE TO PROJECT CONTROL**. UNBANKED.
Architecture remains **ADAPT BEPU BEHIND NOVACORE AUTHORITY**; no adapter work resumed.

## Baseline and scope

`E:\NovaCore`, branch `codex/bepu-local-contact-staging`.
HEAD/main/origin/main remain `3933d832b73511a8d08468b7b27a9b2820e2f8ee`.
M14.17 remains `m14.17-certified-continuation-publication`, target
`a890a12c5cfb6c1eb4ee15906a21161bc4930988`. All branch, remote and tag refs match
the retained baseline. Codex's automatic capture/checkpoint refs are excluded.

All 501 baseline implementation/project/test/native/sample/tool fingerprints were
checked: only the already-proposed Simulation project file differs from the
banked baseline. Its SHA-256 is
`B36A1385F5B603AEFAE79FF910A895AD68F4856B7509DA8EFBB530845563712D`.
No implementation or permanent test changed. Nothing staged, committed or banked.
No solution/test/performance/GPU campaign was run.

Host ALBATRONIC007: Windows build **26200.9445**, display version 25H2; registry
ProductName reports `Windows 10 Pro` (not used to reinterpret the build).
SDK 10.0.303, runtime 10.0.12, NuGet 7.6.0.0, PowerShell 7.6.6.

## Trust policy and service state

No AuthRoot `DisableRootAutoUpdate` or `EnableDisallowedCertAutoUpdate` policy
override, and no `RootDirUrl` redirect was found. Trusted/disallowed automatic
updates therefore retain Microsoft's enabled defaults and Windows Update source.
No policy was edited. See [Microsoft's documented defaults](https://learn.microsoft.com/en-us/windows-server/identity/ad-cs/configure-trusted-roots-disallowed-certificates#registry-settings-modified).

Before and after: CryptSvc running/automatic; BITS, wuauserv and TrustedInstaller
stopped/manual. CryptSvc PID 5196 runs as `NT Authority\NetworkService` in
`C:\WINDOWS\system32\svchost.exe -k NetworkService -p`. Its configured service SID
type is UNRESTRICTED; `sc showsid CryptSvc` resolves the SID below. No service was
restarted or reconfigured.

CBS and Windows Update reboot-required indicators were absent. Pending file
renames exist for gaming/print-driver files; they do not establish a trust-related
reboot requirement. C: had approximately 83.4 GB free at preflight. No reboot was
performed. Prior clock/proxy/package identity evidence remains in
[trust-recovery.md](trust-recovery.md).

## Supported CTL operations and exact outcomes

Commands ran from the clean ignored directory
`E:\NovaCore\build\bepu-trust-recovery\host-recovery`.
Times below are EDT on September 12, 2026.

| Operation | Elevation | Time | Result |
|---|---|---|---|
| `certutil -verifyctl -f AuthRootWU` | elevated | 12:28:56.633–12:30:16.371 | process exit 0; retrieved current CTL |
| original explicit-directory command below | ordinary caller | before matched elevation, and again 12:40:25 | `0x80070005`, Access denied |
| same explicit-directory command | elevated | 12:37:42.221–12:37:59.359 | exit 0; current downloaded CTL and exact G4 entry match |
| ordinary signer-chain verification below | ordinary caller | 12:40:25.082–12:40:25.540 | **chain failed**, `0x800B0109`; process exit 0 is not chain success |

```powershell
certutil -verifyCTL -f AuthRootWU 'http://ctldl.windowsupdate.com/msdownload/update/v3/static/trustedr/en/' 'E:\NovaCore\build\bepu-trust-recovery\root-g4.cer'
certutil -verify -urlfetch 'E:\NovaCore\build\bepu-trust-recovery\signer.cer'
```

These use the [installed and Microsoft-documented certutil interface](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/certutil).
The public `.cer` files are inspection inputs, not certificates imported into a
trust store. No import occurred.

Two preliminary Windows PowerShell 5.1 script starts did not run the CTL operation:
that shell's default Restricted execution policy blocked the script. The diagnostic
then ran through installed PowerShell 7 with its existing RemoteSigned policy.
No execution-policy override or change was used.

## WindowsUpdate/CBS and certificate-log evidence

`Get-WindowsUpdateLog` completed. No relevant denial was found in the bounded
CTL-operation window. The CBS tail contained two **02:04:17** Access Denied
messages unloading SOFTWARE/COMPONENTS hives from `HarddiskVolumeShadowCopy5`.
These occurred hours earlier and are **not causal witnesses for this failure**.

CAPI2 Operational logging was initially disabled. It was enabled temporarily for
the two short command windows, then restored to disabled. No log ACL or security
audit policy was changed. The first busy capture retained the latter part of the
operation; it is not claimed to contain the initial registry update. The second
window supplied the decisive witnesses, read back from Windows' retained log.

At **12:40:25.167410**, certutil PID 32300 recorded CAPI2 event **20**, successful
automatic-root retrieval against `authrootstl.cab` for DigiCert Trusted Root G4.
Between **12:40:25.171619 and 12:40:25.505477**, CryptSvc PID **5196** recorded six
CAPI2 event **60** failures:

- provider: `Microsoft-Windows-CAPI2`;
- operation: `CertificateStore`;
- store provider: `CERT_STORE_PROV_SYSTEM_REGISTRY_W` (13);
- store location: `CERT_SYSTEM_STORE_LOCAL_MACHINE_ID`;
- store: **AuthRoot**;
- flags: **0x20000**, without the read-only flag;
- principal: **S-1-5-20**, Network Service;
- result: **5 — Access is denied**.

The ordinary signer chain then remained `CERT_E_UNTRUSTEDROOT`; its leaf revocation
check passed. `host-trust-witness.json` retains the exact store-event XML, all six
timestamps, the successful automatic-root retrieval witness, service identity,
ACLs, command results and source-log hashes.

## Exact denied owner, ACL findings and classification

**D. AUTHROOT REGISTRY ACCESS FAILURE** is the current causal winner.

The denied logical resource is the machine AuthRoot physical registry store,
backed by `HKLM\SOFTWARE\Microsoft\SystemCertificates\AuthRoot`.
This is a live CryptSvc servicing failure, beyond merely running administrative
certutil without elevation.

| Key beneath AuthRoot | Owner | Inheritance protected | CryptSvc FullControl grant |
|---|---|---|---|
| parent | SYSTEM | no | explicit |
| AutoUpdate | SYSTEM | no | inherited |
| Certificates | SYSTEM | no | inherited |
| CRLs | SYSTEM | **yes** | **absent** |
| CTLs | SYSTEM | **yes** | **absent** |

CryptSvc SID:
`S-1-5-80-242729624-280608522-2219052887-3187409060-2225943459`.
The protected CRLs/CTLs keys grant SYSTEM and Administrators FullControl, but not
CryptSvc; their protected ACLs prevent inheritance of the parent's service grant.

Microsoft documents that the registry store provider opens/creates the
Certificates, CRLs and CTLs subkeys with **KEY_ALL_ACCESS** unless opened read-only.
This makes those two protected children concrete suspects for the observed
writable-store failure. This requested access follows the
[documented provider behavior](https://learn.microsoft.com/en-us/windows/win32/api/wincrypt/nf-wincrypt-certopenstore),
not a captured individual RegCreateKey/RegOpenKey access mask.

**Limits:** CAPI2 identifies the store operation, not the first failing child
registry syscall. The configured service SID was checked; its live effective
thread token was not captured. The original non-elevated force-command's exact
failing syscall was not traced and is not asserted to be identical to the later
CryptSvc failure. The precise provenance of the protected ACLs is unknown.
An authoritative clean-build ACL template was not established: the targeted
installed component manifests are compressed. No speculative ACL was prepared
for execution, and no protected ACL was changed.

## ProcMon and next review boundary

The official Microsoft Process Monitor download was obtained; its executable's
Authenticode status is **Valid**, signer Microsoft Corporation. Automated capture
was rejected **before execution** by automatic approval review: **blocked by
policy**. No ProcMon driver/capture was started and the action was not retried.
CAPI2 supplied a safer native-log substitute for store-level localization.

If Project Control requires syscall-level proof before approving a repair, use one
manual elevated ProcMon capture, with filters for CryptSvc's current PID, certutil,
the AuthRoot path and Access Denied. Reproduce only the ordinary signer verification
shown above, stop capture immediately, and preserve the denied child path, access
mask, principal and relevant stack. Re-query CryptSvc PID; 5196 is this observation's
identity, not a hard-coded future PID. Do not capture an extended session.

Then compare only the implicated child ACLs against an authoritative same-build
baseline or supported Microsoft restoration guidance. Any protected-ACL restoration
requires Project Control approval under this ticket's explicit Case D boundary.
Do not grant broad Users/Everyone access or reset the Windows registry tree.

## Repair and DISM/SFC disposition

No service reset, SoftwareDistribution/catroot2 rename, DISM repair, SFC, ACL,
ownership, GPO or security-product change occurred. Such repairs are not justified
by the localized evidence. DISM/SFC were not run. Elevated supported certutil
diagnostics successfully retrieved current trust data and changed synchronization
fields, but **did not restore ordinary dynamic trust servicing**.

## CTL and root state after the attempt

Do not conflate downloaded CTL data with the registry CTL used by the machine:

| Data | Result |
|---|---|
| downloaded AuthRoot CTL | ThisUpdate **2026-08-25 03:24 EDT**, sequence `1401dd3462c779aec7`; G4 entry matches |
| actual registry EncodedCtl | **still 2023-11-07 16:36 EST**, sequence `1401da11ba19606c20`, 175,647 bytes |
| registry CTL signer | expired **2024-03-14**; dump reports time-invalid chain |
| AuthRoot LastSyncTime before | **2024-01-29T11:54:37.0902125Z** |
| AuthRoot LastSyncTime after | **2026-08-29T16:37:42.2480511Z** |

The changed LastSyncTime value is reported literally, **not proof of successful
synchronization**. Decoding the actual EncodedCtl disproved an interim inference
that its content had become current. Its SHA-256 is
`4276AF3669A141A59388BC56A87F6614D9A9BDDDF560636C264219A7EB11256F`.
The current downloaded CTL output also contains `Strong signatures: 0` and a signer
expiry warning; process exit 0 alone is not used as a complete trust-health verdict.

DigiCert Trusted Root G4 (`DDFB16CD4931C973A2037D3FC83A4D7D775D05E4`) and Assured ID
Root CA (`0563B8630D62D75ABBC8AB1E4BDFB5A899B24D43`) remain absent from the inspected
CurrentUser/LocalMachine Root/AuthRoot stores. Their public validity and complete
package-chain identities remain in the prior trust evidence. Most decisively,
ordinary signer-chain construction still fails; materialized-store count is not
the acceptance test. Timestamp-chain recovery has not been established.

## Bepu verification, restores and security

New two-pass package verification and both clean restores: **NOT RUN**, because
the prerequisite Windows trust-servicing gate is still failed. Prior two-pass
failures remain valid historical evidence, not post-repair results. Retained exact
package SHA-256 identities were rechecked and remain unchanged:

- Physics: `D4A952F5417B53B08DE20D507C21745C9DD004E2529864098DFEC7A2CFF11E42`.
- Utilities: `8168EC4905F3AD5F923564557DAAB2BAB1100EE46B424CA5F1C97BD7684DE19F`.

No signature/timestamp/revocation disablement, untrusted-root exception, warning
suppression, package/version substitution, manual root/leaf import, DLL vendoring,
AV disabling, ownership takeover or permission broadening was used. The existing
exact-version project proposal remains the sole tracked modification.

## Retention and reproduction

Retain this report and `host-trust-witness.json`, linked to the existing package
identity/chain/baseline evidence. They preserve the decisive CAPI2 event, ACL
descriptors, timestamps, data identities and command reproduction instructions.
Bulk WindowsUpdate/CBS/CAPI diagnostic material and downloaded tools stay in the
ignored `build\bepu-trust-recovery` tree while the blocker is unresolved; none is
copied into permanent evidence. No cleanup was attempted in this ticket.

To inspect the actual CTL without importing anything, read `EncodedCtl` bytes from
the AuthRoot AutoUpdate registry key into a disposable `.stl` file and use
`certutil -dump <file>`. Inspect its ThisUpdate/signature dates rather than relying
on LastSyncTime. CAPI2 collection must preserve its prior enabled state and restore
that state afterward; use parsed XML fields to filter, not serialized quote style.

Final result: **WINDOWS TRUST SERVICING — FOLLOW-UP REQUIRED**.
**ESCALATE TO PROJECT CONTROL.** Adapter remains blocked and UNBANKED.
