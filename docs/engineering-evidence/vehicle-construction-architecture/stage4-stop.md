# Stage4 stop: transient arithmetic bound

Judgment: REVISE REQUIRED — STOP FOR PROJECT CONTROL. The one authorized Stage4 correction has been used. No additional production correction was performed.

Independent correction review found that AssemblyConstructionPower.cs computes LCM(energy denominator, fuel denominator) at line133, but checks its width only after combining the first activity denominator at lines138–139. ScratchBits at line49 covers twice the admitted denominator width. A malformed composition of individually admissible snapshots can transiently require three such factors before refusal.

Reproduced current source SHA256:6bb12ca08e20b995aad1f54e868bab2a7351266f0a9f1e30fe5d1edadcc28608.

Four initially positive stores, one1kg/s consumer, Scale12 and Events3 give RateBits1078. Individual denominator admission is3235bits. Use fuel inventory[0,0,0,1] with denominator2^3232−1, unit battery numerator with denominator2^3232+1, and activity1/2^3231ticks over a1tick interval. Each individual value fits; three stores are empty as the event check requires.

The initial LCM has6464bits and already exceeds the joint bound. The next expression reaches9695bits before rejecting it, exceeding declared ScratchBits8626. [Read-only reproducer](stage4-scratch-repro.py) checks the source expression ordering and derives these widths without retaining bulk integers.

This is a temporary-width proof failure during rejection, not demonstrated negative energy, incorrect mixture accounting or canonical corruption. The pure solver still throws Electrical lifetime arithmetic bound exceeded. Refusal occurs before writable copies and sources remain immutable. Nevertheless the stage's bounded allocation/arithmetic proof is false and cannot pass.

The completed bounded Stage4 correction restored installed KSA engine-generator→solar-generator→ordinary-load phases, added the distinguishing battery/cursor test, explicitly budgeted two-factor scratch width, and tested a joint-bound refusal.48 functional checks pass. Independent re-review then exposed the unchecked initial combination; another correction is required.

Project Control's explicit stop condition is “more than one bounded revision is required in any stage.” Therefore Stage4 is not promoted; Stages5–8 remain unopened. The likely next technical action, if separately authorized, is to validate the initial joint denominator immediately before any activity combination and add this exact adversarial witness. That change was NOT made.

No editor/manual route, generic canonical runtime, DLV runtime proof, launch dynamics or banking is claimed. Return the partial candidate UNBANKED. STOP FOR PROJECT CONTROL.
