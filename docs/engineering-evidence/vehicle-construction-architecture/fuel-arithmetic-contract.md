# Exact generic consumption contract

The current integer quantity Q=1/(1000000*2^1074) kg cannot represent every ordinary mixture endpoint. F:O rates 2:3 kg/s with one kg of each exhaust oxygen at 1/3 second, leaving 1/3 kg fuel. Rounding that state or rejecting ordinary unmatched fills would not satisfy this campaign.

For S explicit stores, use integer mixture weights c_jk with sum C_j. Compile L=lcm(C_j)*lcm(1..S), covering all equal-level tank-share divisors. Decoded total-flow rate R_j is exact binary64 in existing Q/tick units. Per-store integer rate A_i is the sum of R_j*c_jk*L/(C_j*n_jk). Store inventory is N_i/(L*d) Q, initially N_i=M_i*L,d=1.

Within a command interval at offset p/d ticks, store i depletes after N_i/(d*A_i) ticks. Choose the earliest by exact cross-products. At an event update all inventories simultaneously: N'_j=N_j*A_i-A_j*N_i, d'=d*A_i, p'=p*A_i+N_i. At interval end T instead use N'_j=N_j-A_j*(T*d-p), without denominator growth. Normalize common factors. Resolve all components of each mixture before adding its demand; no single-component nominal engine operation. Recompute eligible positive levels after depletion.

Each genuine depletion empties at least one previously positive store. Pure-consumption lifetime therefore has at most S events; d<=Amax^S, where Amax<=L*sum(max admitted total rates). Capacity, rate, coefficient and store-count bounds determine required bit capacity at construction. Integer-tick command endpoints do not independently grow the denominator. Temporary cross-products require a separately bounded intermediate width.

This is an exact-authority adaptation, not KSA balance values or a chemistry simulation. No fuel manufacture, replenishment or arbitrary mixture editing during runtime is included. Such operations require a renewed bound and lifecycle proof. Logical partition is a separately compiled test design, never an in-flight separation event.

Initial implementation costs must be measured honestly: bounded exact-arithmetic temporaries may allocate during active service evolution; topology, asset work and editor work remain cold. Existing banked zero-allocation contracts must remain unchanged. A no-demand observation claim must not be presented as active-update allocation evidence.
