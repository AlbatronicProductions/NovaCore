# One manual AuthRoot denial capture

Purpose: identify the exact registry path, operation and requested access. No ACL
or trust-policy change is part of this procedure. Automated ProcMon launch was
previously rejected by approval review; it will not be retried automatically.

1. Open this existing Microsoft-signed executable in Explorer, choose **Run as
   administrator**, and accept its EULA if prompted:

   `E:\NovaCore\build\bepu-trust-recovery\host-recovery\procmon\Procmon64.exe`

2. In ProcMon, press **Ctrl+E** to stop any initial capture, then **Ctrl+X** to
   clear it. Open **Filter** with **Ctrl+L**. Remove any old Include rules and add
   these three Include rules:

   - **Process Name** is `svchost.exe`
   - **Result** is `ACCESS DENIED`
   - **Path** contains `\SOFTWARE\Microsoft\SystemCertificates\AuthRoot`

   Apply the filters. Ensure registry activity is enabled on the toolbar.

3. In a separate **ordinary, non-administrator PowerShell**, have this block ready.
   Start capture with **Ctrl+E**, then run the block **once**:

   ```powershell
   $captureDir = 'E:\NovaCore\build\bepu-trust-recovery\host-recovery\phase2'
   Get-CimInstance Win32_Service -Filter "Name='CryptSvc'" |
       Select-Object Name,ProcessId,StartName,State |
       ConvertTo-Json | Set-Content -LiteralPath "$captureDir\cryptsvc-at-capture.json"
   Get-Date -Format o | Set-Content -LiteralPath "$captureDir\attempt-start.txt"
   certutil -verify -urlfetch 'E:\NovaCore\build\bepu-trust-recovery\signer.cer' *> "$captureDir\one-attempt.txt"
   $LASTEXITCODE | Set-Content -LiteralPath "$captureDir\attempt-process-exit.txt"
   Get-Date -Format o | Set-Content -LiteralPath "$captureDir\attempt-end.txt"
   ```

   This is the same single ordinary chain/open attempt that produced the retained
   CryptSvc denial. It does not import a certificate. A certutil process exit of
   zero does not establish chain success; preserve its output.

4. **Immediately press Ctrl+E to stop capture.** Do not repeat the command if no
   row appears. Save **Events displayed using current filter** as CSV to:

   `E:\NovaCore\build\bepu-trust-recovery\host-recovery\phase2\authroot-denial.csv`

   Also save only those displayed events as native PML to the same directory with
   the name `authroot-denial.pml`; this preserves the short event stack/context.
   Do not choose All events or leave ProcMon capturing.

5. Open the first relevant denied registry event. Preserve its **Path**,
   **Operation**, **Detail / Desired Access**, **Result**, **PID**, and process
   **User**. The filtered PML preserves event properties and stack if needed.
   Close ProcMon after saving. Reply with the saved path (or that no row appeared).

The first matched syscall and the expected ACL must still be qualified before any
repair is proposed. Do not change permissions or import roots during this capture.
