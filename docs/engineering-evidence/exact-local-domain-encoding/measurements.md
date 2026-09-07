# CPU encoding measurements

All values below are generated from `results.json`. Each encoding has 8,884 anchor observations. Counts include repeated topology contexts; collisions mean distinct source point bits sharing the same anchor/payload. They are not counts of unique mathematical equivalence classes.

| Encoding | Collisions | Collisions changing full or near H | Anchor disagreements | Exact H input | Exact full H | Exact final position |
|---|---:|---:|---:|---:|---:|---:|
| global-fp64-control | 0 | 0 | 0 | 8884 | 8884 | 8884 |
| single-fp32-control | 4406 | 2718 | 4766 | 2509 | 3872 | 2509 |
| split-fp32-local | 1248 | 237 | 3350 | 4943 | 6611 | 4943 |
| exact-two-diff-local | 0 | 0 | 0 | 8884 | 8884 | 8884 |
| fixed64-q2^-40 | 1567 | 414 | 1436 | 5795 | 7933 | 5800 |
| ksa-pattern | 3092 | 1902 | 5428 | 1074 | 2659 | 928 |

## Error magnitudes

Units: input/final/height/near in metres; angular in radians. Large full-H extrema occur at the polar geographic singularity described in the main report. Near field is evaluated as an authority sensitivity test even for broad-patch stress points; this is not a rendered displacement measurement.

| Encoding | Input maximum / RMS | Angular maximum / RMS | Full H maximum / RMS | Near H maximum / RMS | Final position maximum / RMS |
|---|---:|---:|---:|---:|---:|
| global-fp64-control | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 |
| single-fp32-control | 0.107982650449 / 0.0146842375604 | 1.69490662843e-08 / 2.30485281542e-09 | 3.35698481724 / 0.317140679355 | 0.00164117960322 / 7.86914926224e-05 | 3.3587000306 / 0.317480471054 |
| split-fp32-local | 2.98168707519e-09 / 2.17386424466e-10 | 3.40992390686e-16 / 2.97500758848e-17 | 3.35698481727 / 0.29900332154 | 4.15923961938e-11 / 1.33955132093e-12 | 3.35698481742 / 0.299003321546 |
| exact-two-diff-local | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 |
| fixed64-q2^-40 | 9.37485680337e-13 / 1.50060013801e-13 | 1.14143042449e-19 / 2.35001950741e-20 | 3.35698481727 / 0.197772107495 | 2.2815083156e-14 / 1.33322494569e-15 | 3.35698481742 / 0.197772107498 |
| ksa-pattern | 0.16082400654 / 0.0158031492808 | 2.52430991633e-08 / 2.48047833439e-09 | 3.35698481718 / 0.317140854991 | 0.0017680945711 / 8.58830631175e-05 | 3.3570401476 / 0.317534349856 |

## Near-camera subset

1,024 observations, split camera and FP32 view at approximately 16 m, including a transverse axis crossing.

| Encoding | Collisions | H-input mismatches | Full H mismatches | Final-position mismatches |
|---|---:|---:|---:|---:|
| global-fp64-control | 0 | 0 | 0 | 0 |
| single-fp32-control | 1014 | 1024 | 1024 | 1024 |
| split-fp32-local | 254 | 256 | 0 | 256 |
| exact-two-diff-local | 0 | 0 | 0 | 0 |
| fixed64-q2^-40 | 724 | 736 | 12 | 736 |
| ksa-pattern | 1014 | 1024 | 1024 | 1024 |

## Repetition

Canonical JSON SHA-256, identical in three processes: `62b3b6fb139ea15bda3a00d342157ef40f08e9ea4d0147445f1f851905e5f35e`.

| Encoding | Encoded + reconstructed stream SHA-256 |
|---|---|
| global-fp64-control | `2860b9d00a126b199785b6905fadec3b84165e8f5ef28028360dede2e90a8aee` |
| single-fp32-control | `ffd7f89c5982265c80d0b95fc038f1a5f4e29691948f6d4e5b8cedc6c598850a` |
| split-fp32-local | `cfa7d6f188bbbd124db48a13a70c6c70f64559dac03dd94bfe9f3a369eacd7c1` |
| exact-two-diff-local | `5e50e19aeeda797736208a72ec3d22e27cf08f5cae885bbfa044cd0209b82167` |
| fixed64-q2^-40 | `b0ad1e585b8d568daa8850b463ac9fb71e8b26c5946091059e7af6974c11b9d6` |
| ksa-pattern | `91f35b834b28d53edde367df56f70e4792bb4acbe956919c64457df29da01123` |
