"""Bounded reduced-model feasibility screen; NOT a Florida fixture or certificate.

Predeclared grid: eta=h/x in (.05,.1,.2,.3), terminal angle in (.2,.4,.6,.8).
Exactly 16 combinations. No production calls, optimizer, or fixture execution.
Zero initial spin, spherical inertia, zero restitution, fixed plane, zero force.
The current moving-Earth model and represented M14.13 tuple are NOT replaced by
this model: their perturbations must be independently bounded before admission.
"""
import json
import math

ETAS = (.05, .1, .2, .3)
TERMINAL_ANGLES = (.2, .4, .6, .8)


def gap(theta, eta):
    return math.sin(theta) - theta + eta * (1 - math.cos(theta))


def derivative(theta, eta):
    return math.cos(theta) - 1 + eta * math.sin(theta)


def beta(eta):
    low, high = 2 * math.atan(eta), 4 * eta
    assert gap(low, eta) > 0 and gap(high, eta) < 0
    # Fixed count, diagnostic scalar root only. No authoritative time selection.
    for _ in range(64):
        middle = (low + high) / 2
        if gap(middle, eta) > 0:
            low = middle
        else:
            high = middle
    return (low + high) / 2


rows = []
for eta in ETAS:
    root = beta(eta)
    peak = 2 * math.atan(eta)
    for terminal in TERMINAL_ANGLES:
        has_beta = root < terminal
        rows.append(dict(
            eta=eta, terminal_angle=terminal, beta_angle=root,
            beta_fraction_of_remainder=root / terminal,
            peak_angle=peak, peak_gap_over_x=gap(peak, eta),
            terminal_gap_over_x=gap(terminal, eta),
            beta_approach_over_x_omega=-derivative(root, eta),
            initial_curvature_over_x_omega_squared=eta,
            beta_before_target=has_beta,
            terminal_rotation_remainder_majorant=(1.5 * terminal) ** 12 / math.factorial(12),
            classification=("NO_BETA_BEFORE_TARGET" if not has_beta else
                            "REDUCED_MODEL_BETA_NEAR_TARGET" if root / terminal > .95 else
                            "REDUCED_MODEL_BETA_WITH_TIME_SEPARATION")))

print(json.dumps(dict(
    model="fixed-plane spherical inertia exact zero-restitution reduced model",
    limitation="Not current moving-Earth / represented M14.13 same-model certification; no fixture proposed.",
    combinations=len(rows), root_bisections_per_eta=64,
    root_tolerance="No production tolerance; diagnostic binary64 scalar output only",
    region_label_note="The .95 display label does not change a production threshold or establish numerical robustness.",
    rows=rows), indent=2))
