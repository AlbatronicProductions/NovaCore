"""Independent 60-digit co-moving finite-volume mass oracle; stdin is the
--modular-gate8-oracle JSON. No candidate integration code is imported."""
from decimal import Decimal as D, getcontext
import json
import sys

getcontext().prec = 60


def check(raw):
    def d(x): return D.from_float(float(x))
    def vec(v): return list(map(d, v))
    def add(a, b): return [x+y for x, y in zip(a, b)]
    def scale(a, k): return [x*k for x in a]
    def cross(a, b): return [a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0]]
    def dot(a, b): return sum((x*y for x, y in zip(a, b)), D(0))
    def mv(a, v): return [dot(a[3*i:3*i+3], v) for i in range(3)]
    def parallel(v): return [(dot(v, v) if i == j else D(0))-v[i]*v[j] for i in range(3) for j in range(3)]
    def solve(matrix, b):
        a, c, e = matrix[0], matrix[4], matrix[8]
        u, v, w = matrix[1], matrix[2], matrix[5]
        co = [c*e-w*w, v*w-u*e, u*w-v*c, v*w-u*e, a*e-v*v, u*v-a*w, u*w-v*c, u*v-a*w, a*c-u*u]
        determinant = a*co[0]+u*co[1]+v*co[2]
        return scale(mv(co, b), 1/determinant)

    dry = raw['dry']; dm = d(dry['mass']); dc = vec(dry['com']); di = vec(dry['inertia'])
    stores = raw['stores']; initial = vec(raw['source']); dt = d(raw['dt'])
    before = vec(raw['before']); after = vec(raw['after']); force = vec(raw['force']); gravity = vec(raw['gravity'])

    def physical(t):
        q = [a+(b-a)*t/dt for a, b in zip(before, after)]
        mass = dm+sum(q, D(0)); first = scale(dc, dm)
        for qi, store in zip(q, stores): first = add(first, scale(vec(store['com']), qi))
        com = scale(first, 1/mass)
        tensor = add(di, scale(parallel(add(dc, scale(com, -1))), dm))
        for qi, store in zip(q, stores):
            tensor = add(tensor, scale(add(vec(store['inertiaPerKg']), parallel(add(vec(store['com']), scale(com, -1)))), qi))
        return mass, com, tensor

    def derivative(t, state):
        mass, com, inertia = physical(t); omega = state[10:13]; q = state[6:9]; qw = state[9]
        torque = scale(cross(com, force), -1)  # axial main/pivot: zero applied moment at O
        alpha = solve(inertia, add(torque, scale(cross(omega, mv(inertia, omega)), -1)))
        body = add(scale(force, 1/mass), scale(add(cross(alpha, com), cross(omega, cross(omega, com))), -1))
        acceleration = add(add(body, scale(add(scale(cross(q, body), qw), cross(q, cross(q, body))), 2)), gravity)
        qv = scale(add(scale(omega, qw), cross(q, omega)), D('.5'))
        return state[3:6]+acceleration+qv+[-dot(q, omega)/2]+alpha

    def integrate(count):
        h = dt/count; state = initial[:]; t = D(0)
        for _ in range(count):
            k1 = derivative(t, state)
            k2 = derivative(t+h/2, add(state, scale(k1, h/2)))
            k3 = derivative(t+h/2, add(state, scale(k2, h/2)))
            k4 = derivative(t+h, add(state, scale(k3, h)))
            state = add(state, scale(add(add(k1, scale(k2, 2)), add(scale(k3, 2), k4)), h/6)); t += h
        return state

    coarse = integrate(128); fine = integrate(256); candidate = vec(raw['result'])
    convergence = max(abs(x-y) for x, y in zip(coarse, fine))
    errors = [max(abs(candidate[i]-fine[i]) for i in group) for group in (range(3), range(3, 6), range(6, 10), range(10, 13))]
    assert convergence < D('1e-18')
    assert max(errors) < D('2e-12')
    return dict(craft=raw['craft'], selfConvergence=float(convergence), positionVelocityQuaternionRateErrors=list(map(float, errors)))


print(json.dumps(dict(judgment='PASS', precision=60, substeps=[128, 256], rows=[check(row) for row in json.load(sys.stdin)]), indent=2))
