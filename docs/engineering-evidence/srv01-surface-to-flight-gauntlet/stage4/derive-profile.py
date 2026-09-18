"""Independent cold mission/profile arithmetic. No production evaluator or runtime state."""
import json, math
from fractions import Fraction
from pathlib import Path
MU=3.986004355070226e14  # current SolAnalyticalDefinition Earth, SI
R=6371008.8
target=R+400000
semi=(R+target)/2
ascent=math.sqrt(MU*(2/R-1/semi))+math.sqrt(MU/target)-math.sqrt(MU*(2/target-1/semi))
budget=2*ascent+2500+2500+500+750  # return transfer includes deorbit; no double-counted impulse
required=1.1*budget
propellant=5*math.ceil(630*math.expm1(required/3072)/5)
wet=630+propellant
extent=math.ceil(1.5*wet*9.81/(5*3072)*16)/16
thrust=extent*5*3072
duration=Fraction(propellant)/Fraction(extent*5)
answer=dict(earthMu=MU,earthRadius=R,referenceAltitude=400000,idealAscent=ascent,idealReturn=ascent,
    sizingBudget=budget,requiredDeltaV=required,fuelKg=propellant*2//5,oxidizerKg=propellant*3//5,
    dryMass=630,propellantMass=propellant,wetMass=wet,massRatio=wet/630,exhaustSpeed=3072,
    isp=3072/9.80665,extentRate=extent,totalFlow=extent*5,thrust=thrust,weight=wet*9.81,
    tw=thrust/(wet*9.81),initialNetUp=thrust/wet-9.81,deltaV=3072*math.log(wet/630),
    burnSeconds=float(duration),burnExact=str(duration),dryFullAcceleration=thrust/630,dryFullLocalG=thrust/630/9.81)
root=Path(__file__).resolve().parents[4]
data=json.loads((root/'src/NovaCore.Simulation/Spacecraft/Assemblies/Data/SRV01-Development-Propulsion-v1.json').read_text())
assert (data['initialFuelKg'],data['initialOxidizerKg'],data['thrustN'],data['extentRateKgS'])==(answer['fuelKg'],answer['oxidizerKg'],thrust,extent)
print(json.dumps(answer,indent=2))
