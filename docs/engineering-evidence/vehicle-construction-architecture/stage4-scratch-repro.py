"""Read-only exact-integer witness for the uncorrected Stage4 temporary-width gate."""
import hashlib
import json
import math
from pathlib import Path

root = Path(__file__).resolve().parents[3]
source = root / 'src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyConstructionPower.cs'
code = source.read_text(encoding='utf-8')
first = code.index('var denominator=source.Denominator/')
third = code.index('denominator=denominator/BigInteger.GreatestCommonDivisor(denominator,time.Denominator)', first)
check = code.index('Require(denominator.GetBitLength()', first)
assert first < third < check, 'Source ordering changed; this witness must be reviewed again.'

# Four initially positive stores; one 1 kg/s consumer; L=lcm(1)*lcm(1..4)=12.
# Three stores are now empty, so Events=3 and one positive store are individually legal.
positive = 4
events = 3
rate_bits = (12 * (1 << 1074)).bit_length()
denominator_limit = 1 + events * rate_bits
energy_d = (1 << (3 * rate_bits - 2)) + 1
fuel_d = (1 << (3 * rate_bits - 2)) - 1
activity_d = 1 << (3 * rate_bits - 3)
assert all(d.bit_length() <= denominator_limit for d in (energy_d, fuel_d, activity_d))
assert math.gcd(energy_d, fuel_d) == 1
assert events <= positive - 1
# Unit numerators normalize neither denominator away; quantity/energy are below
# an ordinary 1 kg / 1 J initial/capacity ceiling. Activity=1/activity_d <=1 tick.
initial_lcm = math.lcm(energy_d, fuel_d)
next_lcm = math.lcm(initial_lcm, activity_d)
integer_bits = 2200 + positive * rate_bits
scratch_bits = max(integer_bits, 2 * (1 + positive * rate_bits))
assert initial_lcm.bit_length() > denominator_limit
assert next_lcm.bit_length() > scratch_bits
print(json.dumps({
    'judgment': 'REPRODUCED: declared temporary-width bound exceeded before refusal',
    'source_sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
    'initially_positive_stores': positive, 'events': events, 'rate_bits': rate_bits,
    'individual_denominator_limit_bits': denominator_limit,
    'energy_denominator_bits': energy_d.bit_length(), 'fuel_denominator_bits': fuel_d.bit_length(),
    'activity_denominator_bits': activity_d.bit_length(),
    'initial_joint_lcm_bits': initial_lcm.bit_length(), 'third_factor_lcm_bits': next_lcm.bit_length(),
    'declared_scratch_bits': scratch_bits,
    'canonical_mutation': False,
    'scope': 'exact reproduction of current expression ordering; no production correction or runtime publication'
}, indent=2))
