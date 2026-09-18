"""Read-only mission/containment feasibility. No successor profile, density or inertia authored."""
from pathlib import Path
import math, json, hashlib

E=Path(__file__).parent; R=Path('E:/NovaCore'); C=E.parent/'rotating-florida-contact-closure'
mu=398600435507022.6; radius=6371008.8; orbit=radius+400000; higher=radius+500000
dry=630; standard_g=9.80665; ve=3072; twr=1.5; margin=.10
# NASA-STD-8719.12A Table 5-28: kg/litre at20C, used for feasibility only.
fuel_density=870.; oxidizer_density=1450.
specific_volume=.4/fuel_density+.6/oxidizer_density
vc=math.sqrt(mu/orbit)
ascent=math.sqrt(2*mu*orbit/(radius*(radius+orbit)))+vc-math.sqrt(2*mu*radius/(orbit*(radius+orbit)))
up=math.sqrt(mu/orbit)*(math.sqrt(2*higher/(orbit+higher))-1)+math.sqrt(mu/higher)*(1-math.sqrt(2*orbit/(orbit+higher)))
roundtrip=2*up; plane=2*vc*math.sin(math.radians(.5)/2)
maneuver=math.ceil(roundtrip+plane)
perigee=radius-1000
deorbit=vc-math.sqrt(mu*(2/orbit-2/(orbit+perigee)))
deorbit_allowance=math.ceil(deorbit)
base=ascent+maneuver+deorbit_allowance
failed=json.loads((C/'failed-witness.json').read_text())
site_radius=math.sqrt((radius+15.134892258793116)**2+48**2)
g_launch=mu/site_radius**2; g_max=mu/radius**2
spin_credit=math.sqrt(sum(failed['input']['f0']['Velocity'][k]**2 for k in 'XYZ'))
children=failed['input']['children']
tank=next(c for c in children if c['Part']=='tank_01')
tank_gross=math.prod(tank['Dimensions'][k] for k in 'XYZ')
all_gross=sum(math.prod(c['Dimensions'][k] for k in 'XYZ') for c in children)

def envelope_performance(exhaust):
    max_prop=tank_gross/specific_volume
    max_dv=exhaust*math.log1p(max_prop/dry)
    ideal_prop=dry*math.expm1(ascent/exhaust)
    optimistic_prop=dry*math.expm1((ascent-spin_credit)/exhaust)
    return dict(exhaustMps=exhaust,ispSeconds=exhaust/standard_g,ascentOnlyPropellantKg=ideal_prop,
        ascentOnlyVolumeM3=ideal_prop*specific_volume,ascentOnlyTankMultiple=ideal_prop*specific_volume/tank_gross,
        optimisticFullSpinCreditMps=spin_credit,optimisticAscentPropellantKg=optimistic_prop,
        optimisticAscentVolumeM3=optimistic_prop*specific_volume,
        grossTankMaxPropellantKg=max_prop,grossTankMaxIdealDeltaV=max_dv)

# Constant-thrust conservative gravitational-impulse ceiling for total burn time:
# t_b = ve/(TWR*g_launch)*(1-exp(-D/ve)); G <= g_max*t_b.
# This is an allowance, not an ascent trajectory or optimized gravity turn.
dv=base
for _ in range(100):
    grav=g_max*ve/(twr*g_launch)*(-math.expm1(-dv/ve))
    nxt=(base+grav)*(1+margin)
    if abs(nxt-dv)<1e-10: break
    dv=nxt
grav=g_max*ve/(twr*g_launch)*(-math.expm1(-dv/ve))
dev_margin=(base+grav)*margin
prop_exact=dry*math.expm1(dv/ve); prop=math.ceil(prop_exact/5)*5
fuel=prop*2//5; oxidizer=prop*3//5; wet=dry+prop
extent=math.ceil(twr*wet*g_launch/(5*ve)*16)/16
flow=5*extent; thrust=flow*ve
volume=fuel/fuel_density+oxidizer/oxidizer_density
fit_ratio=1+tank_gross/(specific_volume*dry)
required_ideal_ve=ascent/math.log(fit_ratio)
required_optimistic_ve=(ascent-spin_credit)/math.log(fit_ratio)
# Recompute required exhaust for same finite sizing rule at an impossible all-volume tank limit.
# D=ve*ln(R), so the mass-dependent burn fraction is fixed; solve algebraically.
denominator=math.log(fit_ratio)-(1+margin)*g_max/(twr*g_launch)*(1-1/fit_ratio)
required_budget_ve=(1+margin)*base/denominator
result=dict(status='FEASIBILITY ONLY — NO PROFILE INSTALLED',source=dict(mu=mu,meanRadius=radius,dryMassKg=dry,
    orbitAltitudeM=400000,referenceHigherAltitudeM=500000,planeChangeDegrees=.5,
    deorbitOsculatingPerigeeAltitudeM=-1000,launchGravity=g_launch,gravityUpperBound=g_max,
    source='Current SolAnalyticalDefinition, authored SRV01 dry parts, original Florida graded-site geometry; no atmosphere.'),
    budget=dict(ascentInsertionMps=ascent,roundtrip400To500KmMps=roundtrip,planeChangeMps=plane,
        maneuverUnroundedMps=roundtrip+plane,maneuverAllowanceMps=maneuver,deorbitUnroundedMps=deorbit,
        deorbitAllowanceMps=deorbit_allowance,gravityAllowanceMps=grav,developmentMarginFraction=margin,
        developmentMarginMps=dev_margin,totalIdealDeltaVMps=dv,
        interpretation='One bounded reference mission, not a global minimum or qualified complete ascent/return. No drag, propulsive descent, landing, thermal budget or repeated cycles.'),
    feasibilityProfile=dict(exhaustMps=ve,ispSeconds=ve/standard_g,unroundedPropellantKg=prop_exact,
        finiteMixtureUnitKg=5,propellantKg=prop,fuelKg=fuel,oxidizerKg=oxidizer,wetMassKg=wet,
        fuelDensityKgM3=fuel_density,oxidizerDensityKgM3=oxidizer_density,temperatureC=20,
        fuelVolumeM3=fuel/fuel_density,oxidizerVolumeM3=oxidizer/oxidizer_density,totalLiquidVolumeM3=volume,
        weightN=wet*g_launch,extentKgS=extent,totalMassFlowKgS=flow,thrustN=thrust,
        initialTWR=thrust/(wet*g_launch),dryThrustAccelerationMps2=thrust/dry,
        dryThrustAccelerationStandardG=thrust/dry/standard_g,burnDurationS=prop/flow,
        actualRoundedIdealDeltaVMps=ve*math.log(wet/dry)),
    containment=dict(tankGrossEnvelopeM3=tank_gross,allEightGrossEnvelopesSumM3=all_gross,
        mixDensityKgM3=1/specific_volume,requiredTankMultiple=volume/tank_gross,
        requiredWholePartsMultiple=volume/all_gross,fittedCapacityUsesNoWallsUllageOrDryHardware=True,
        use='Necessary generous fit ceiling only; neither envelope is promoted to storage/density/inertia authority.',
        requiredExhaustForAscentOnlyMps=required_ideal_ve,requiredIspForAscentOnlyS=required_ideal_ve/standard_g,
        requiredExhaustWithFullSpinCreditMps=required_optimistic_ve,
        requiredExhaustForReferenceBudgetMps=required_budget_ve,requiredIspForReferenceBudgetS=required_budget_ve/standard_g),
    comparisons=[envelope_performance(ve),envelope_performance(330*standard_g)],
    conclusion='Current tank envelope fails even reference ascent-only before loss/reserve/margin; modest330s sensitivity does not resolve. No engine technology, container geometry, tensor, mixture/density or successor version chosen.')
(E/'results.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(result,indent=2))
