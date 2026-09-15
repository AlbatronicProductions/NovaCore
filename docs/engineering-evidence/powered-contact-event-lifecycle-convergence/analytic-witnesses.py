"""Data-only exact arithmetic checks for the architecture report; no candidate imports."""
from fractions import Fraction as Q
from decimal import Decimal, localcontext
import json

def f(x):
    return str(x)

# Unilateral contact; positive velocity separates from the plane.
before = max(Q(0) - Q(1), Q(0))
after = max(Q(0), Q(0)) - Q(1)
assert before == 0 and after == -1

# A fixed, zero-bias compliant scalar map is not a semigroup in its dt.
h, H, omega = Q(1, 4), Q(1), Q(1)
s = lambda t: 1 / (1 + omega*t)**2
split_decay = s(h)*s(H-h)
merged_decay = s(H)
assert split_decay != merged_decay

# Unilateral departure: unit m,g,H; thrust=4 for H/4, then zero.
v1 = 3*h
y1 = Q(3, 2)*h*h
y2 = y1 + v1*(H-h) - (H-h)**2/2
v2 = v1 - (H-h)
assert y2 == Q(3, 8) and v2 == 0
average_thrust = 4*h/H
assert average_thrust == 1  # All-interval supported solution stays at zero.

# Constant normal load and Coulomb cap k=1, mass=1; F=2 for h=1,
# then zero for 2h. Slide during burn; stop one h later; hold thereafter.
k, m, burn, total = Q(1), Q(1), Q(1), Q(3)
v_peak = k*burn/m
distance = k*burn*burn/m
work = k*k*burn*burn/m
average_tangent = 2*k*burn/total
assert average_tangent < k and distance == 1 and work == 1

# Constant body-aligned force, central point reservoir, fixed direction.
# dv = F/q log(m0/m1), not F*h divided by endpoint mass.
with localcontext() as ctx:
    ctx.prec = 80
    force, flow, duration = Decimal(32), Decimal(1), Decimal(1)/128
    m1 = Decimal(8)
    m0 = m1 + flow*duration
    exact_dv = force/flow*(m0/m1).ln()
    source_dv = force*duration/m0
    end_dv = force*duration/m1
    mid_dv = force*duration/((m0+m1)/2)
    source_bound = force*flow*duration**2/(2*m1**2)
    midpoint_bound = force*flow**2*duration**3/(12*m1**3)
    assert abs(source_dv-exact_dv) <= source_bound
    assert abs(end_dv-exact_dv) <= source_bound
    assert abs(mid_dv-exact_dv) <= midpoint_bound
    mass = {"reference_dv":str(exact_dv),"source_mass_dv":str(source_dv),
            "successor_mass_dv":str(end_dv),"midpoint_mass_dv":str(mid_dv),
            "source_or_successor_error_bound":str(source_bound),
            "midpoint_error_bound":str(midpoint_bound)}

tiny = Q(1, 2**1075)
assert tiny > 0 and 16*tiny/8 == Q(1, 2**1074)
assert float(tiny) == 0.0

print(json.dumps({
    "scope":"Analytical architecture witnesses only; not BEPU or candidate qualification",
    "unilateral_order":{"impulse_then_contact":f(before),"contact_then_impulse":f(after)},
    "softness":{"split_decay":f(split_decay),"merged_decay":f(merged_decay),
                "same":False,"model":"fixed-geometry zero-bias scalar discrete map"},
    "departure":{"event_impulse":f(4*h),"averaged_impulse":f(average_thrust*H),
                 "ordered_y":f(y2),"ordered_v":f(v2),"averaged_y":"0","averaged_v":"0"},
    "friction":{"ordered_distance":f(distance),"ordered_final_velocity":"0",
                "engine_work":f(work),"friction_dissipation":f(work),
                "averaged_force":f(average_tangent),"averaged_distance":"0",
                "averaged_work":"0","same_total_engine_impulse":True},
    "changing_mass":mass,
    "tiny":{"duration":"2^-1075 s","positive_exact":True,"double_duration":float(tiny),
            "force":"16 N","mass":"8 kg","free_delta_v":"2^-1074 m/s",
            "resource":"2^-1074 kg at 2 kg/s","does_not_prove_contact_response":True}
}, indent=2))
