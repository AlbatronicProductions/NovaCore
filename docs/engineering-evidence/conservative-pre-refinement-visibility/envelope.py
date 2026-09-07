"""Analytical field value envelope; conservative binary32 error even for FP64 extended operations."""
import math,json,struct
U=2**-23
def gamma(n):return n*U/(1-n*U)
c=1/math.sqrt(5)
fade=31*gamma(7)
weight_error=3*fade*(1+fade)**2+gamma(3)*(1+fade)**3
corner_max=3*c*(1+gamma(4))
corner_error=3*c*gamma(4)
unit_error=8*(weight_error*corner_max+corner_error)+gamma(16)*8*(1+weight_error)*corner_max
unit_bound=math.sqrt(3)/2+unit_error
near=11*unit_bound*(1+gamma(1))
# Blend formula a+(b-a)*t, t in [0,1] +/- polynomial error.
blended=(near*(1+2*fade)+gamma(3)*4*near)
# Facility weight is the product of two (1-fade) polynomial values.
support_error=(1+fade)**2-1+gamma(3)*(1+fade)**2
attenuated=blended*(1+support_error)*(1+gamma(2))
# SmoothStep clamps its division result before evaluating t*t*(3-2*t).
range_fade_error=5*gamma(5)
final=attenuated*(1+range_fade_error)*(1+gamma(2))
bits=struct.unpack('<I',struct.pack('<f',final))[0]+1
upper=struct.unpack('<f',struct.pack('<I',bits))[0]
RESULT=dict(u=U,fadeError=fade,weightError=weight_error,unitFieldBound=unit_bound,nearAmplitude=11,blendedBound=blended,facilityWeightError=support_error,rangeFadeError=range_fade_error,nearDisplacementBeforeBaseArithmetic=final,upwardFloat=upper)
if __name__=='__main__':print(json.dumps(RESULT,indent=2))
