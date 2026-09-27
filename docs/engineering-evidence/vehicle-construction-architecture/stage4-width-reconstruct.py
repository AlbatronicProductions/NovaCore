"""Independent Python integer reconstruction; no production calls or repository writes."""
import hashlib
import json
import math
from pathlib import Path

source = Path(__file__).resolve().parents[3] / 'src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyConstructionPower.cs'
code = source.read_text(encoding='utf-8')
start = code.index('var denominator=source.Denominator/')
validation = code.index('"Invalid generator active duration."', start)
precheck = code.index('Require(denominator.GetBitLength()', validation)
compose = code.index('denominator=denominator/BigInteger.GreatestCommonDivisor(denominator,time.Denominator)', start)
postcheck = code.index('Require(denominator.GetBitLength()', compose)
assert start < validation < precheck < compose < postcheck

p, r = 4, (12 * 2**1074).bit_length()
d, old_d = 1 + p*r, 1 + (p-1)*r
i = d + 2199
w = 4*(i//4+2)
m, old_m = 2**d-1, 2**old_d-1
pair = math.lcm(m, m-1)
triple = math.lcm(old_m, old_m-1, old_m-2)
assert pair.bit_length() == 2*d == 8626
assert triple.bit_length() == 3*old_d == 9705
assert math.lcm(old_m, old_m-1).bit_length() > old_d  # precheck rejects before third factor
assert pair.bit_length()+1 == max(2*d+1, w) == 8627

rate = (2**53-1) * 2**2045
capacity, ticks = rate*1_000_000, 2**63-1
request = rate*(ticks*m)
assert rate.bit_length() == 2098 and capacity.bit_length() == 2118
assert request.bit_length() == d+2161 < i
# Independent Fraction identity for capped battery generation from unit/m charge.
from fractions import Fraction
delivered = min(Fraction(request,m), Fraction(capacity)-Fraction(1,m))
assert delivered+Fraction(1,m) == capacity

rows = []
for denominator_bits in (1,499999,500000,500001):
    integer_bits = denominator_bits+2199
    parser_width = 4*(integer_bits//4+2)
    magnitude = max(2*denominator_bits,parser_width)
    signed = max(2*denominator_bits+1,parser_width)
    rows.append(dict(D=denominator_bits,I=integer_bits,W=parser_width,
                     magnitude=magnitude,signed=signed,admitted=magnitude<=1_000_000))
assert [x['admitted'] for x in rows] == [True,True,True,False]
assert rows[0]['magnitude'] == rows[0]['signed'] == 2208
assert rows[2]['magnitude'] == 1_000_000 and rows[2]['signed'] == 1_000_001
print(json.dumps(dict(judgment='PASS',source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(),
    initially_positive=p,rate_bits=r,denominator_bits=d,
    historical_triple_bits=triple.bit_length(),corrected_pair_bits=pair.bit_length(),
    magnitude_bits=max(2*d,w),signed_bits=max(2*d+1,w),
    maximum_valid_correlated_request_bits=request.bit_length(),boundaries=rows,
    scope='Independent integer/Fraction reconstruction and corrected operation-order check; no runtime publication'),indent=2))
