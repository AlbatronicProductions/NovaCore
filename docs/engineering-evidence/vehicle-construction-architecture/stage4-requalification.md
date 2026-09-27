# Stage4 authorized arithmetic-bound correction

Project Control authorized one focused correction after the historical Stage4 stop. The original candidate and passing Stage0–3 evidence are retained. [Analytical derivation](stage4-width-derivation.md) was written before the code correction. [Independent reconstruction](stage4-width-reconstruct.py) confirms it.

The correction shares the parser length with the derived width contract and checks the accumulated denominator after existing activity validation, before the next LCM. Existing post-composition/final guards remain. Stored-integer limits, magnitude admission cap, exact results, diagnostics and source immutability are preserved. No fuel, topology, ownership, allocation or power-order semantics changed.

For D=1+P*r, I=D+2199 and W=4*(floor(I/4)+2): maximum magnitude is max(2D,W); minimum uniform signed representation is max(2D+1,W). The original 9695-bit witness was not maximal: the old order could reach9705 bits. The corrected four-store fixture reaches exactly8626 magnitude bits before refusal, requiring8627 signed bits. At zero fuel events the parser dominates at2208 bits. This is an expression-value contract, not a claim about CLR temporary buffer allocation.

Requalification: **PASS — PROMOTE**. Existing48 semantic checks plus42 adversarial width checks pass. Stage1 25, Stage2 28, Stage3 2075/400 independent fraction-oracle cases, SRV assembly41634/24 independent trajectories and live-control1557 all pass. Managed Debug test-project build:0 warnings/errors; Release test route builds and passes. Independent architecture_verifier and ksa_frames reviews PASS; ksa_frames independently executed90 checks. No second arithmetic defect found.

Production Power.cs SHA256: `7230e85e06dcafe3cb6b4ba6cfbad78fd61e5ebf604bb4728d3ae64e05fc302d`.

A solution-wide Debug build was attempted additionally and failed in existing native-copy targets because build/native-ninja shaders and NovaCore.Native.dll are absent. Managed compilation succeeded. No source workaround, deployment or hidden qualification claim was made. This is separate from the targeted managed Stage4 qualification.

The first new test compilation found only a local-variable naming collision, corrected in the test before execution. No further production arithmetic revision was needed. Stages5–8 resume under the existing architecture. Manual acceptance remains pending.
