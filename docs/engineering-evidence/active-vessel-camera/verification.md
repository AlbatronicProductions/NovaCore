> SUPERSEDED after Project Control manual acceptance FAILED at Step 5 HOME. FREE is now DEFERRED; HOME is unbound. This file records the prior candidate, not current acceptance. Use [the correction package](../active-vessel-camera-free-correction/README.md) and [revised manual route](../active-vessel-camera-free-correction/manual-route.md).

# Independent verification

Read-only workers `/root/novacore_audit` and `/root/ksa_commands` reviewed source, tests, input ownership and measurement design. The lead was the sole production/test writer. Workers did not execute competing builds or performance runs. Their source/test review passed; execution results are separately retained in validation/regression/performance records.

| Attack | Independent disposition |
| --- | --- |
| 1. Second rocket camera | PASS: existing Solar owner and existing free controller |
| 2. Render ID becomes canonical ID | PASS: session launch spacecraft ID plus publisher generation |
| 3. View changes control ownership | PASS: presentation-only mutations |
| 4. Celestial focus regresses | PASS source/coverage: Earth, Moon, Sun, Mars |
| 5. Earth terrain context lost | PASS: independent environment binding restored on refocus |
| 6. Private solver read | PASS: copied canonical observation only |
| 7. Display epoch mismatch | PASS: post-Solar refresh and explicit epoch refusal |
| 8. COM target jump | PASS: actual changing-COM episode follows material O |
| 9. Display anchor confused | PASS: mesh material origin and focus root equality |
| 10. Orbit rotates craft | PASS: eye placement and aim only |
| 11. Zoom changes physics | PASS: orbit demand only; canonical nonmutation assertion |
| 12. Free retires spacecraft | PASS: independent active binding retained |
| 13. Refocus resets vehicle | PASS: only bounded saved view tuple restored |
| 14. Target growth/leak | PASS: fixed slot; 10,000-action witness |
| 15. Dangling retirement | PASS: body fallback, no delayed resurrection, disposal proof |
| 16. Early FP32 world conversion | PASS: FP64 subtraction before relative transport |
| 17. Large-coordinate instability | PASS source/coverage: numerical assertions through 7e12 m |
| 18. Hot allocation | PASS applicable paths; inherited celestial-focus allocation separately reported |
| 19. Per-frame scan | PASS: only cold binding scans existing ten-body presentation |
| 20. Render scales with simulation servicing | PASS: one refresh per display callback outside service loop |
| 21. Launcher/manual divergence | PASS source/coverage: existing launcher mapping and Florida Solar consumer; UI remains manual |
| 22. Key conflict | PASS source: separate F/Home camera mask; E remains free movement when detached |
| 23. Launch physics creep | PASS: no physics/command/resource implementation change |
| 24. SAS/navball creep | PASS: no new implementation |
| 25. Milestone/bank | PASS source scope; final identity record checks refs and index |

The live-rebinding defect found during review was corrected within the existing architecture and permanently covered. No remaining causal camera defect was reported.

Measurement review identified and retained these distinctions: 48 B inherited celestial selection allocation; mixed-frame P99 cannot stand in for switch-event latency; live physical servicing is wall-clock driven; startup/transition costs must remain visible; native key edges and launcher UI require manual acceptance. The final observer adds exact fixed physical cadence, post-callback GC endpoints, per-route transition samples and individual switch/next-frame costs. Both ordinary live runs and controlled fixed-cadence runs are retained.

This review does not declare Project Control manual acceptance.
