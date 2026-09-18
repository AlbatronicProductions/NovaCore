# Resolution declaration correction after A1

The predeclared plan incorrectly described the existing default window as 1280 x 720. A1's native log and direct source inspection establish the actual unchanged window client/swapchain size as **960 x 540** (`NovaCoreNative.cpp`, `Width=960, Height=540`; `Window` uses these dimensions; native exhaust target log records `extent=960x540`). No resolution argument or code change was made. All remaining captures keep the same existing default, and every log's extent will be checked.

This corrects a mistaken environment description, not the workload, count, order, or result-selection rule. A1 remains evidence; it is not replaced. The retained plan is not silently rewritten. Report this protocol deviation and do not claim testing at 1280 x 720.

The native overall-average wall-time report includes post-loop diagnostic output and is excluded. Existing per-frame samples and their distribution remain valid. No additional captures are authorized by either clarification.
