"""Reproduce the permanent angle oracle with only Python's decimal library.

100-digit Gauss-Legendre pi and positive arcsine series are independent of
the production binary64 interval atan/Machin implementation.
"""
from decimal import Decimal as D, getcontext
import json
import math
from pathlib import Path
import struct

getcontext().prec = 100
a, b, t, p = D(1), D(1) / D(2).sqrt(), D(1) / 4, D(1)
for _ in range(10):
    an = (a + b) / 2
    b = (a * b).sqrt()
    t -= p * (a - an) ** 2
    a, p = an, 2 * p
pi = (a + b) ** 2 / (4 * t)

def asin(z):
    term = total = z
    n = 0
    while abs(term) > D("1e-95"):
        term *= z*z*D((2*n+1)**2)/D(2*(n+1)*(2*n+3))
        total += term
        n += 1
    return total

def atan(x):
    sign = -1 if x < 0 else 1
    x = abs(x)
    if x > 1:
        return sign * (pi / 2 - atan(1/x))
    return sign * asin(x / (1+x*x).sqrt())

def acos(x):
    v = 2 * asin(((1-abs(x))/2).sqrt())
    return pi-v if x < 0 else v

def atan2(y, x):
    if x == 0 and y == 0:
        v = pi if x.is_signed() else D(0)
    elif x == 0:
        v = pi/2
    else:
        v = atan(abs(y/x))
        if x < 0:
            v = pi-v
    return -v if y.is_signed() else v

def bits(x):
    return struct.pack(">d", x).hex().upper()

rows = []
for x in [-1.0, math.nextafter(-1.0, 0), -.9, -.5, -0.0, 0., .1, .4788205718227514, .5, .9, math.nextafter(1.,0), 1.]:
    rows.append(dict(kind="acos", x=bits(x), y=bits(0.), value=format(acos(D.from_float(x)), ".80f")))
for x in [-1e100, -7., -1., -.1, -0., 0., .1, 1., 7., 1e100]:
    rows.append(dict(kind="atan", x=bits(x), y=bits(0.), value=format(atan(D.from_float(x)), ".80f")))
for k in range(16):
    boundary=(k+.5)/16
    for x in [math.nextafter(boundary,0),boundary,math.nextafter(boundary,1)]:
        rows.append(dict(kind="atan",x=bits(x),y=bits(0.),value=format(atan(D.from_float(x)),".80f")))
for y,x in [(0.,0.),(-0.,0.),(0.,-0.),(-0.,-0.),(0.,-1.),(-0.,-1.),(1.,0.),(-1.,0.),
            (1.,1.),(-1.,1.),(1.,-1.),(-1.,-1.),(-.8661348234979923,.1433224599406355),
            (1e-100,1e-100),(-1e100,1e100),(1.,1e-100),(1e-100,-1.)]:
    rows.append(dict(kind="atan2",x=bits(x),y=bits(y),value=format(atan2(D.from_float(y),D.from_float(x)), ".80f")))
destination = Path(__file__).resolve().parents[2] / "tests/NovaCore.Graphics.Tests/fixtures/contact-angles.json"
destination.parent.mkdir(parents=True, exist_ok=True)
destination.write_text(json.dumps(rows, indent=2)+"\n", encoding="utf-8")
print(f"Wrote {len(rows)} independent angle witnesses")
