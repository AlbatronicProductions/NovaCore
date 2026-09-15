# Rank admission before construction

Four normal rows J_i=(0,1,0,-z_i,0,x_i) have rank 3 for a,b>0.
The seven-row rigid normal+tangent+twist inverse is singular and is NOT used.
Use the normalized independent velocity basis (vy,b*wx,a*wz):
K=diag(1/m,b^2/Ix,a^2/Iz), a=1,b=.5,Ix=Iz=2.
Rank=3; all eigenvalues strictly positive for m>0. Dimensionless length
normalization is declared; kappa2=max(diagonal)/min(diagonal)=m/2 here,
between 4 and m0/2. The proof refuses nonpositive mass/inertia/levers.

Normal reactions need a constitutive null-mode choice, not a pseudoinverse:
sum(sx*sz*N_i)=0, equal pad stiffness rigid limit. Together with resultant
normal force and moments this is a normalized 4x4 Hadamard system,
rank 4, condition 1. Different pad compliance may choose different loads.

N=mg-Fy*chi; Mx=e*Fy*chi; Mz=d*k*N.
N_i=N/4-z_i*Mx/(4*b^2)+x_i*Mz/(4*a^2).
Required powered margin: N*(1-d*k/a)>abs(e*Fy)/b. Coast also N>0.
N and each N_i are affine in time in each phase, so checking both phase
ends proves the whole phase. Tangent acceleration during power is monotone
in mass with positive numerator; its sole possible interior velocity minimum
is checked analytically. Yaw acceleration is affine; check its sole possible
interior zero plus endpoints. Coast deceleration is constant.

This is a supported sliding/twisting reduced model, not BEPU's compliant
solver matrix. Rank does not establish a bridge to that solver.
