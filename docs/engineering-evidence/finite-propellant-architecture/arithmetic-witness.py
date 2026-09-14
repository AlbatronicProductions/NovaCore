"""Small architecture counterexamples only; not a resource segmenter or production prototype.
Run with Python 3 standard library. No filesystem mutation, engine state, dynamics or benchmarking.
"""
from fractions import Fraction as F
import json
import sys

S = 1_000_000
n = 16_666
eps = float.fromhex("0x0.0000000000001p-1022")
q = 2.0 ** -60
fp_cost = q * (n / S)
exact_cost = F(q) * F(n, S)
assert fp_cost > 0 and 1.0 - fp_cost == 1.0

tiny_seconds = F(eps) / 2
tiny_ticks = tiny_seconds * S
assert tiny_seconds > 0 and eps / 2 == 0
assert float(tiny_seconds * 6000) > 0

rounded_source = F(n / S)
exact_demand = F(n, S)
assert rounded_source > exact_demand
assert F(260.40625) == F(15625) * F(n, S)
assert F(1, 128) / 3 * S == F(15625, 6)

max_scaled = F(sys.float_info.max) * 2 ** 1074
max_source_units = max_scaled * S
max_demand_units = max_scaled * (2 ** 63 - 1)
assert max_scaled.denominator == max_source_units.denominator == max_demand_units.denominator == 1
assert int(max_demand_units).bit_length() <= 34 * 64

# Independent exact arithmetic conservation witness, not a canonical commit simulation.
source_units = S * 2 ** 1074
debit_units = int(F(q) * 2 ** 1074) * n
successor_units = source_units - 1000 * debit_units
assert 0 < successor_units < source_units
assert successor_units + 1000 * debit_units == source_units

print(json.dumps({
    "kind": "architecture arithmetic witnesses only",
    "rounded_subtraction_stall": {
        "source_kg": 1, "flow_kgps": q, "ticks": n,
        "fp64_cost_kg": fp_cost, "fp64_successor_equals_source": True,
        "exact_positive_cost_kg": str(exact_cost), "exact_1000_debits_conserved": True},
    "tiny_duration": {
        "source_kg": "2^-1074", "flow_kgps": 2, "exact_seconds": "2^-1075",
        "exact_ticks": "15625/2^1069", "denominator_bits": tiny_ticks.denominator.bit_length(),
        "fp64_seconds": eps / 2, "rounded_nonzero_impulse_at_6000N_Ns": float(tiny_seconds * 6000)},
    "false_endpoint_equality": {
        "flow_kgps": 1, "ticks": n, "source_kg": float(rounded_source),
        "exact_positive_successor_kg": str(rounded_source - exact_demand)},
    "true_endpoint_equality": {"mass_kg": 260.40625, "flow_kgps": 15625, "ticks": n},
    "ordinary_fractional_exhaustion": {"mass_kg": "1/128", "flow_kgps": 3, "ticks": "15625/6"},
    "positive_thrust_zero_flow": {"thrust_N": "2^-1074", "exhaust_mps": 2,
                                 "fp64_flow": eps / 2, "resource_compatibility": "REFUSE"},
    "bit_bounds": {"flow": int(max_scaled).bit_length(), "source": int(max_source_units).bit_length(),
                   "long_tick_demand": int(max_demand_units).bit_length(),
                   "capacity": 2176, "limbs": 34, "bytes_per_integer": 272}
}, indent=2))
