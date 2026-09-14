"""Architecture algebra only; not a dynamics implementation or qualification runner.
Python 3 standard library. No repository/runtime mutation or benchmark.
"""
from decimal import Decimal, localcontext
from fractions import Fraction
import json

S = 1_000_000
F = Fraction

def dec(x):
    return Decimal(x.numerator) / Decimal(x.denominator)

with localcontext() as ctx:
    ctx.prec = 400
    # One legal interior event: a constant-direction analytical oracle.
    dry, fuel, flow, thrust = F(8), F(1, 128), F(3), F(9000)
    interval = F(16667, S)
    powered = fuel / flow
    remainder = interval - powered
    assert 0 < powered < interval and powered + remainder == interval
    m0, m1 = dec(dry + fuel), dec(dry)
    q, t = dec(flow), dec(thrust)
    h, coast = dec(powered), dec(remainder)
    log_ratio = (m0 / m1).ln()
    dv = t / q * log_ratio
    dx_powered = t / (q * q) * (dec(fuel) - m1 * log_ratio)
    dx_endpoint = dx_powered + dv * coast
    old_mass_dv = t * h / m0
    old_mass_dx = t / m0 * h * h / 2 + old_mass_dv * coast
    assert dv > old_mass_dv and dx_endpoint > old_mass_dx

    # h underflows to binary64 zero although its weighted effect is representable.
    eps = F(float.fromhex('0x0.0000000000001p-1022'))
    tiny_h = eps / 2
    tiny_dv = Decimal(3000) * (Decimal(1) + dec(eps)).ln()
    tiny_impulse = F(6000) * tiny_h
    assert tiny_h > 0 and float(tiny_h) == 0
    assert float(tiny_impulse) > 0 and float(tiny_dv) > 0

    # Constant I does not imply zero outgoing angular momentum flux.
    # A hypothetical rotating exit r=(1,0,0), omega=(0,0,1), q=2:
    # q * r cross (omega cross r) = (0,0,2). This is a counterexample,
    # NOT an additional term to append to the proposed net-wrench closure.
    flux_z = 2
    assert flux_z != 0

    # Scaling must precede an overflowing gyro intermediate. I=(1,2,3),
    # omega=(1e160,1e160,0): the Z derivative is -omega_x*omega_y/3.
    spin = 1e160
    naive_gyro = -(spin * spin) / 3
    scaled_gyro = float(-F(spin) * F(spin) * tiny_h / 3)
    assert naive_gyro == float('-inf') and scaled_gyro < 0

    # A finite subdivision count alone cannot qualify arbitrary mass ratios.
    # Fuel1kg, q=T=64, h=1/64s: a legal sub-tick exhaustion in16667ticks.
    small_dry = F(1e-12)
    analytic_fast_burn = ((Decimal(1) + dec(small_dry)) / dec(small_dry)).ln()
    terminal_rk_term = dec(F(1, 2**20) / (6 * small_dry))
    assert F(1, 64) < interval and terminal_rk_term > analytic_fast_burn * 1000

    results = {
        'scope': 'Independent algebra counterexamples; no production trajectory qualification',
        'interior_oracle': {
            'dry_kg': str(dry), 'fuel_kg': str(fuel), 'flow_kgps': str(flow),
            'thrust_N': str(thrust), 'canonical_ticks': 16667,
            'exact_powered_ticks': str(powered * S),
            'exact_unpowered_ticks': str(remainder * S),
            'delta_v_mps': str(dv)[:45], 'delta_position_m': str(dx_endpoint)[:45],
            'constant_source_mass_delta_v_mps': str(old_mass_dv)[:45],
            'constant_source_mass_delta_position_m': str(old_mass_dx)[:45],
            'rounded_duration_sum_error_seconds': str(
                F(float(powered)) + F(float(remainder)) - interval)},
        'tiny_duration': {
            'exact_seconds': '2^-1075', 'binary64_seconds': float(tiny_h),
            'rounded_impulse_Ns': float(tiny_impulse),
            'rounded_delta_v_at_1kg_dry_mps': float(tiny_dv)},
        'angular_closure_counterexample': {
            'constant_inertia_is_insufficient': True,
            'hypothetical_outgoing_spin_flux_z_Nm': flux_z},
        'weighted_gyro_counterexample': {
            'naive_derivative': '-infinity',
            'finite_scaled_increment_z_radps': scaled_gyro},
        'bounded_work_counterexample': {
            'dry_kg': 1e-12, 'fuel_kg': 1, 'flow_kgps': 64, 'thrust_N': 64,
            'powered_seconds': '1/64', 'illustrative_dyadic_depth': 20,
            'analytic_delta_v_mps': str(analytic_fast_burn)[:45],
            'terminal_rk_contribution_alone_mps': str(terminal_rk_term)[:45],
            'meaning': 'A universal fixed depth is insufficient; not a recommended depth'},
        'tick_lattice': {
            'counts_16666_16667': [sum(((n+1)*S//60 - n*S//60) == k
                                      for n in range(1200)) for k in (16666,16667)],
            'total_ticks': 1200*S//60}
    }
    assert results['tick_lattice']['counts_16666_16667'] == [400, 800]
    print(json.dumps(results, indent=2))
