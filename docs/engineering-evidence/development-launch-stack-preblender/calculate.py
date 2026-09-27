"""DLV-B1-R1 engineering evidence only. No NovaCore or Blender imports.
Constituents are authority; aggregates/witnesses are derived, never input masses.
SI, engineering right-handed +X axial, +Y/+Z radial. Standard-library Python.
"""
import math as m
import json
import hashlib
from pathlib import Path

OUT=Path(__file__).resolve().parent
G0=9.80665; MU=3.986004355070226e14; RE=6371008.8; GRAVITY=MU/RE**2
RHO={'RP1':810.,'LOX':1141.,'MMH':880.,'NTO':1440.}
O_F={'Booster':2.6,'Upper':2.6,'Capsule':1.65}
LOAD={'Booster':185000.,'Upper':42000.,'Capsule':1000.}
HE={'Booster':240.,'Upper':56.,'Capsule':8.}
HE_VOLUME={'Booster':.75,'Upper':.75,'Capsule':.12}
HE_COUNT={'Booster':8,'Upper':2,'Capsule':2}
BODY_R=1.8; TANK_R=1.7; AL_RHO=2840.
ID='DLV-B1-R1'

def terminal_nozzle():
    # Conservative perfect-gas screening with explicit delivered losses.
    gamma=1.22;epsilon=1.5;pc=1.5e6;pa=101325.;cstar=1700*.95
    lo=1.;hi=10.
    for _ in range(80):
        mach=(lo+hi)/2
        area=(2/(gamma+1)*(1+(gamma-1)*mach*mach/2))**((gamma+1)/(2*(gamma-1)))/mach
        if area<epsilon:lo=mach
        else:hi=mach
    pressure_ratio=(1+(gamma-1)*mach*mach/2)**(-gamma/(gamma-1))
    cf_momentum=.97*m.sqrt(2*gamma**2/(gamma-1)*(2/(gamma+1))**((gamma+1)/(gamma-1))*(1-pressure_ratio**((gamma-1)/gamma)))
    cf_vac=cf_momentum+pressure_ratio*epsilon
    at=10000/(pc*cf_vac-pa*epsilon);ae=epsilon*at
    pc_min=(2000+pa*ae)/(at*cf_vac)
    return dict(chamber_pressure_full_Pa=pc,feed_pressure_Pa=2e6,gamma=gamma,expansion_ratio=epsilon,
        ideal_cstar_m_s=1700,combustion_efficiency=.95,momentum_efficiency=.97,delivered_cstar_m_s=cstar,
        throat_area_m2=at,exit_area_m2=ae,exit_diameter_m=m.sqrt(4*ae/m.pi),
        exit_pressure_full_Pa=pc*pressure_ratio,sea_level_thrust_N=10000,vacuum_thrust_N=pc*at*cf_vac,
        Isp_full_SL_s=10000/(pc*at/cstar*G0),Isp_full_vac_s=cf_vac*cstar/G0,
        chamber_pressure_at_20pct_SL_thrust_Pa=pc_min,exit_pressure_at_20pct_SL_thrust_Pa=pc_min*pressure_ratio,
        Isp_min_SL_s=2000/(pc_min*at/cstar*G0),minimum_axial_Isp_with_15deg_cant_s=2000/(pc_min*at/cstar*G0)*m.cos(m.radians(15)))

TERMINAL=terminal_nozzle()

def vacuum_coefficient(epsilon):
    gamma=1.22;lo=1.;hi=30.
    for _ in range(90):
        mach=(lo+hi)/2
        area=(2/(gamma+1)*(1+(gamma-1)*mach*mach/2))**((gamma+1)/(2*(gamma-1)))/mach
        if area<epsilon:lo=mach
        else:hi=mach
    pr=(1+(gamma-1)*mach*mach/2)**(-gamma/(gamma-1))
    cf=.97*m.sqrt(2*gamma**2/(gamma-1)*(2/(gamma+1))**((gamma+1)/(gamma-1))*(1-pr**((gamma-1)/gamma)))+pr*epsilon
    return cf,pr

def orbital_nozzles():
    pc=1.5e6;pa=101325.;oms_epsilon=80.;cf,pr=vacuum_coefficient(oms_epsilon)
    at=500/(pc*cf);ae=at*oms_epsilon
    oms=dict(chamber_pressure_Pa=pc,feed_pressure_Pa=2e6,expansion_ratio=oms_epsilon,gamma=1.22,
        ideal_cstar_m_s=1700,combustion_efficiency=.98,momentum_efficiency=.97,
        throat_area_m2=at,exit_area_m2=ae,exit_diameter_m=m.sqrt(4*ae/m.pi),vacuum_thrust_N=500,
        Isp_vacuum_screen_s=cf*1700*.98/G0,Isp_budget_vacuum_s=315,sea_level_operation=False)
    ae=15/pa;lo=1.;hi=20.
    for _ in range(90):
        eps=(lo+hi)/2;cf,pr=vacuum_coefficient(eps)
        if pc*ae/eps*cf>115:lo=eps
        else:hi=eps
    at=ae/eps;mdot=pc*at/(1700*.95)
    rcs=dict(chamber_pressure_Pa=pc,feed_pressure_Pa=2e6,expansion_ratio=eps,gamma=1.22,
        ideal_cstar_m_s=1700,combustion_efficiency=.95,momentum_efficiency=.97,
        throat_area_m2=at,exit_area_m2=ae,exit_diameter_m=m.sqrt(4*ae/m.pi),
        sea_level_thrust_N=100,vacuum_thrust_N=115,Isp_SL_screen_s=100/(mdot*G0),Isp_vacuum_screen_s=115/(mdot*G0),
        Isp_budget_s=210,scope='Screened steady engine;210s conservative pulsed budget, not qualified valve transients')
    return oms,rcs

OMS,RCS=orbital_nozzles()

def mm_ceil(x): return m.ceil(x*1000)/1000
def dump(name,obj): (OUT/name).write_text(json.dumps(obj,indent=2,allow_nan=False)+'\n',encoding='utf-8')
def sphere_r(v): return (3*v/(4*m.pi))**(1/3)
def vecadd(a,b): return [a[i]+b[i] for i in range(3)]
def path_measure(paths):
    total=0.;moment=[0.,0.,0.]
    for path in paths:
        for a,b in zip(path,path[1:]):
            length=m.sqrt(sum((a[j]-b[j])**2 for j in range(3)))
            total+=length
            for j in range(3):moment[j]+=length*(a[j]+b[j])/2
    return total,[v/total for v in moment]
def q_from_x(d):
    if d[0]<-.999999:return [0,0,1,0]
    q=[0,-d[2],d[1],1+d[0]];n=m.sqrt(sum(x*x for x in q));return [x/n for x in q]
def tensor_rotate(diag,q):
    x,y,z,w=q
    r=[[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],
       [2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],
       [2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]]
    return [[sum(r[i][k]*diag[k]*r[j][k] for k in range(3)) for j in range(3)] for i in range(3)]
def radial(x,r,phi): return [x,r*m.cos(phi),r*m.sin(phi)]
def polyint(co,a,b): return sum(c*(b**(i+1)-a**(i+1))/(i+1) for i,c in enumerate(co))
def polymul(a,b):
    c=[0.]*(len(a)+len(b)-1)
    for i,x in enumerate(a):
        for j,y in enumerate(b): c[i+j]+=x*y
    return c

def sections(r,lc):
    a=r/2; total=lc+2*a
    # squared disc radius polynomial in axial coordinate q.
    return [(0,a,[0,2*r*r/a,-r*r/a**2]),(a,a+lc,[r*r]),
            (a+lc,total,[r*r*(2*total/a-total**2/a**2),r*r*(-2/a+2*total/a**2),-r*r/a**2])]

def tank_integrals(r,lc,height):
    v=q=q2=ir=0.
    for lo,hi,p in sections(r,lc):
        hi=min(hi,height)
        if hi<=lo: continue
        v+=m.pi*polyint(p,lo,hi)
        q+=m.pi*polyint([0]+p,lo,hi)
        q2+=m.pi*polyint([0,0]+p,lo,hi)
        ir+=m.pi/2*polyint(polymul(p,p),lo,hi)
    return v,q,q2,ir

def fill_properties(tank,volume):
    r=tank['internal_radius_m'];lc=tank['cylinder_length_m'];length=tank['length_m']
    lo=0.;hi=length
    for _ in range(65):
        h=(lo+hi)/2
        if tank_integrals(r,lc,h)[0]<volume:lo=h
        else:hi=h
    h=(lo+hi)/2;v,q,q2,ir=tank_integrals(r,lc,h)
    return dict(height_m=h,centroid_from_bottom_m=q/v,axial_specific_inertia_m2=ir/v,
                transverse_specific_inertia_m2=(q2-q*q/v+ir/2)/v)

def tank_design(stage,species,load,base):
    volume=load/RHO[species]/.95
    heads=2*m.pi*TANK_R**3/3
    lc=mm_ceil((volume-heads)/(m.pi*TANK_R**2)); length=lc+TANK_R
    actual=m.pi*TANK_R**2*lc+heads
    e=m.sqrt(.75)
    area=2*m.pi*TANK_R*lc+2*m.pi*TANK_R**2*(1+(1-e*e)*m.atanh(e)/e)
    thickness=.006 if stage=='Booster' else .005
    return dict(id=f'{stage}.{species}.Tank',stage=stage,species=species,internal_radius_m=TANK_R,
                cylinder_length_m=lc,length_m=length,capacity_m3=actual,maximum_liquid_fill=.95,
                x_bottom_m=base,wall_thickness_m=thickness,installation_radius_m=1.76,
                shell_area_m2=area,shell_mass_kg=area*thickness*AL_RHO)

components=[];tanks=[];feeds=[];engines=[];gasbottles=[];stage_origin={};stage_length={};actuators=[]

def add(stage,name,mass,center,shape,basis,**extra):
    assert mass>0
    row=dict(id=f'{stage}.{name}',stage=stage,dry_mass_kg=mass,centroid_local_m=center,
             orientation_quaternion_xyzw=[0,0,0,1],shape=shape,mass_basis=basis,**extra)
    components.append(row);return row

def ring(radius,length): return dict(kind='annular_cylinder',outer_radius_m=radius,inner_radius_m=max(0,radius-.06),length_m=length)
def box(a,b,c):return dict(kind='box',dimensions_m=[a,b,c])
def cyl(r,length):return dict(kind='solid_cylinder',radius_m=r,length_m=length)
def sphere(r,thin=False):return dict(kind='spherical_shell' if thin else 'solid_sphere',radius_m=r)
def annulus(ri,ro,length):return dict(kind='annular_cylinder',outer_radius_m=ro,inner_radius_m=ri,length_m=length)

def dry_equipment(stage,name,base,center,shape,growth=.1):
    return add(stage,name,base*(1+growth),center,shape,'hardware allowance with component-owned growth',baseline_kg=base,growth_fraction=growth)

def build_launch(stage):
    prop=LOAD[stage];ox=prop*2.6/3.6;fuel=prop/3.6
    bay=4.0 if stage=='Booster' else 3.0
    tx=tank_design(stage,'LOX',ox,bay+.1)
    he_bay_start=mm_ceil(tx['x_bottom_m']+tx['length_m']+.15)
    he_bay_length=2.60 if stage=='Booster' else 1.35
    tf=tank_design(stage,'RP1',fuel,he_bay_start+he_bay_length+.15)
    length=mm_ceil(tf['x_bottom_m']+tf['length_m']+(2.6 if stage=='Booster' else 1.0))
    stage_length[stage]=length
    tanks.extend([tx,tf])
    for t in [tx,tf]:
        name=t['species']+'.TankShell'
        add(stage,name,t['shell_mass_kg'],[t['x_bottom_m']+t['length_m']/2,0,0],
            dict(kind='ellipsoidal_tank_shell',radius_m=TANK_R,cylinder_length_m=t['cylinder_length_m'],length_m=t['length_m']),
            'derived shell area Ã— thickness Ã— aluminum density2840')
        dry_equipment(stage,t['species']+'.WeldRingsBafflesPorts',220 if stage=='Booster' else 80,
                      [t['x_bottom_m']+t['length_m']/2,0,0],ring(1.75,t['length_m']))
        dry_equipment(stage,t['species']+'.InsulationSupports',150 if stage=='Booster' else 55,
                      [t['x_bottom_m']+t['length_m']/2,0,0],ring(1.78,t['length_m']))
    # Explicit separate feed regions. Loaded liquid is deducted from tank content.
    # Route fuel outside LOX. RP1 annular manifold avoids the central LOX feed.
    fuel_end_radius=.70 if stage=='Booster' else .40
    fuel_path=[[tf['x_bottom_m']-.05,0,0],[tf['x_bottom_m']-.05,2,0],
               [bay-.45,2,0],[bay-.45,fuel_end_radius,0]]
    ox_path=[[tx['x_bottom_m']-.05,0,0],[bay-.90,0,0]]
    specs=[('RP1',.25 if stage=='Booster' else .11,[fuel_path]),
           ('LOX',.35 if stage=='Booster' else .15,[ox_path])]
    if stage=='Booster':
        # 96-sided centerline polygon is the explicit engineering manifold model.
        manifold=[radial(bay-.45,.70,2*m.pi*j/96) for j in range(97)]
        specs.append(('RP1',.25,[manifold]))
        specs.append(('RP1',.17,[[radial(bay-.45,.70,2*m.pi*j/3),radial(3.25,1.03,2*m.pi*j/3)] for j in range(3)]))
        specs.append(('LOX',.20,[[[bay-.90,0,0],radial(3.00,.87,2*m.pi*j/3)] for j in range(3)]))
    else:
        specs.append(('RP1',.09,[[[bay-.45,.40,0],[2.50,.40,0]]]))
    for i,(species,di,paths) in enumerate(specs):
        l,center=path_measure(paths)
        v=m.pi*di*di/4*l
        feeds.append(dict(id=f'{stage}.Feed.{i}',stage=stage,species=species,internal_diameter_m=di,developed_length_m=l,
                          volume_m3=v,loaded_mass_kg=v*RHO[species],centroid_local_m=center,
                          centerline_paths_local_m=paths,
                          geometry='swept polyline capacity model, specified fitting envelope; all line liquid retained at cutoff'))
        wall=.003 if stage=='Booster' else .002
        shell=m.pi*((di/2+wall)**2-(di/2)**2)*l*2700
        add(stage,f'Feed.{i}.PipeShell',shell,center,cyl(di/2+wall,l),'derived pipe metal volumeÃ—2700; routing tensor surrogate')
    dry_equipment(stage,'Feed.ValvesManifoldsPumpsInterfaces',160 if stage=='Booster' else 85,[bay-.6,0,0],ring(1.1,.4))
    dry_equipment(stage,'Feed.ExternalRaceway',190 if stage=='Booster' else 65,
                  [(bay+tf['x_bottom_m'])/2,2.0,0],box(tf['x_bottom_m']-bay,.4,.4))
    dry_equipment(stage,'Pressurization.RegulatorsHeatExchangers',180 if stage=='Booster' else 80,[he_bay_start+he_bay_length/2,0,0],cyl(.4,.5))
    for i in range(HE_COUNT[stage]):
        if stage=='Booster':x=he_bay_start+.65+1.30*(i//4);phi=m.pi/4+(i%4)*m.pi/2;radius=1.02
        else:x=he_bay_start+.675;phi=m.pi/2+i*m.pi;radius=.8
        pos=radial(x,radius,phi);ri=sphere_r(HE_VOLUME[stage]);install=ri+.035
        gasbottles.append(dict(id=f'{stage}.He.{i}',stage=stage,internal_volume_m3=HE_VOLUME[stage],internal_radius_m=ri,
                              installation_radius_m=install,centroid_local_m=pos,rated_pressure_Pa=30e6))
        dry_equipment(stage,f'He.{i}.COPV',125,pos,sphere(ri+.02,True),growth=.05)
    n=3 if stage=='Booster' else 1
    for i in range(n):
        phi=2*m.pi*i/n;rad=.95 if n==3 else 0
        pivot=radial(2.10 if n==3 else 2.15,rad,phi)
        exitp=radial(.45,rad,phi)
        thrust=1100000. if n==3 else 450000.
        isp_sl=295 if n==3 else None;isp_vac=335 if n==3 else 350
        exit_area=(thrust*(335/295-1)/101325) if n==3 else m.pi*1.35**2/4
        bell_radius=m.sqrt(exit_area/m.pi)+.02
        engines.append(dict(id=f'{stage}.Engine.{i}',stage=stage,sea_level_thrust_N=thrust if n==3 else None,
            vacuum_thrust_N=thrust*335/295 if n==3 else thrust,Isp_sea_level_s=isp_sl,Isp_vacuum_s=isp_vac,
            force_point_local_m=pivot,gimbal_pivot_local_m=pivot,mount_point_local_m=radial(3.2 if n==3 else 2.8,rad,phi),
            nozzle_exit_local_m=exitp,nominal_force_direction=[1,0,0],plume_direction=[-1,0,0],
            exit_radius_m=m.sqrt(exit_area/m.pi),bell_outer_radius_m=bell_radius,
            gimbal_half_angle_deg=3 if n==3 else 4,throttle_min_fraction=.35,engine_top_local_x_m=3.35 if n==3 else 2.9))
        dry_equipment(stage,f'Engine.{i}.Hardware',900 if n==3 else 600,
                      radial(2.2,rad,phi),cyl(bell_radius,2.9 if n==3 else 2.45),growth=0)
        dry_equipment(stage,f'Engine.{i}.TVC',45 if n==3 else 40,pivot,box(.3,.35,.35))
    for name,mass,x,shape in [
        ('ThrustFrame',550 if stage=='Booster' else 200,bay-.5,ring(1.65,.7)),
        ('EngineSkirtSupport',420 if stage=='Booster' else 150,bay/2,dict(kind='annular_cylinder',outer_radius_m=1.8,inner_radius_m=1.775,length_m=bay)),
        ('IntertankFrame',180 if stage=='Booster' else 90,he_bay_start+he_bay_length/2,ring(BODY_R,he_bay_length)),
        ('TopAdapterInterstage',330 if stage=='Booster' else 120,length-.8,ring(BODY_R,1.3)),
        ('SeparationHardware',140 if stage=='Booster' else 90,length-.10,ring(1.6,.2)),
        ('AvionicsHarnessSensors',150 if stage=='Booster' else 100,length-.5,ring(1.1,.35)),
        ('BatteryPower',100 if stage=='Booster' else 75,length-.5,ring(.65,.3)),
        ('UmbilicalsService',50 if stage=='Booster' else 30,bay,box(.3,.4,.3)),
        ('PassiveAeroFins',110 if stage=='Booster' else 40,bay,ring(1.82,1.2))]:
        dry_equipment(stage,name,mass,[x,0,0],shape)
    if stage=='Upper':
        # Cup collar surrounds the capsule shield without cutting through it.
        separation=next(c for c in components if c['id']=='Upper.SeparationHardware')
        separation['centroid_local_m']=[length+.31,0,0]
        separation['shape']=annulus(1.775,1.825,.10)
        for i in range(8):
            x=.8 if i<4 else length-.5;phi=(i%4)*m.pi/2
            dry_equipment(stage,f'ColdGasAttitude.{i}',4,radial(x,1.83,phi),cyl(.055,.18))
        for i in range(2):dry_equipment(stage,f'ColdGasSettling.{i}',4,radial(.25,1.6,i*m.pi),cyl(.065,.20))
        dry_equipment(stage,'ColdGasFeed',20,[length/2,1.70,0],box(length-1,.10,.10))
        dry_equipment(stage,'ColdGasControl',15,[length-.5,1.1,0],box(.20,.20,.20))
    # No free growth ballast; every allowance belongs to a component above.

def build_capsule():
    stage='Capsule';stage_length[stage]=3.05
    rows=[('PrimaryShellFrames',380,1.25,ring(1.62,2.4)),('HeatShieldCarrier',180,.18,ring(1.70,.12)),
        ('BaseTPS',200,.07,dict(kind='spherical_cap_shell',sphere_radius_m=6.25,base_radius_m=1.75,rise_m=.25,thickness_m=.075)),
        ('BackshellTPS',75,1.55,ring(1.65,2.4)),('FeedRegulation',85,1.36,annulus(.5,.70,.08)),
        ('AvionicsHarness',90,1.625,annulus(1.3,1.4,.15)),
        ('ThermalService',50,1.40,annulus(1.5,1.55,.10)),('RecoveryAssembly',220,2.40,cyl(.95,.9)),
        ('PayloadSupport',60,.85,box(1.0,1.10,1.10)),('Payload',250,.85,box(.9,1.,1.)),
        ('AttachmentSeparation',80,.39,annulus(1.72,1.81,.06)),('RecoveryCoverMechanisms',30,2.78,ring(.9,.25))]
    for name,mass,x,shape in rows:dry_equipment(stage,name,mass,[x,0,0],shape,growth=0 if name=='Payload' else .15)
    for i in range(2):dry_equipment(stage,f'BatteryPower.{i}',37.5,[.65,.85*(-1)**i,.85*(-1)**i],box(.30,.35,.35),growth=.15)
    for i in range(4):
        phi=i*m.pi/2;species='MMH' if i%2==0 else 'NTO';mass=LOAD[stage]*(1 if species=='MMH' else 1.65)/2/2.65
        pos=radial(.83,1.12,phi)
        tanks.append(dict(id=f'Capsule.{species}.{i//2}',stage=stage,species=species,internal_radius_m=.38,
            capacity_m3=4*m.pi*.38**3/3,maximum_liquid_fill=.95,loaded_mass_kg=mass,
            centroid_local_m=pos,shape='sphere with positive acquisition device',installation_radius_m=.4,wall_thickness_m=.003))
        dry_equipment(stage,f'Tank.{i}.Hardware',32,pos,sphere(.383,True),growth=.15)
        # Convert specialist local Z axial to canonical engineering X.
        for name,mass,x,r,shape in [
            ('PodTPS',15,.70,2.0,box(1.38,.45,.65)),('TerminalEngine',45,.71,1.95,cyl(.17,.75)),
            ('OMS',7,.375,2.02,cyl(.07,.35)),('PodStructure',20,.85,2.0,box(1.38,.45,.65)),
            ('PodCaps',10,.45,2.05,box(.15,.36,.7))]:
            pos=radial(x,r,phi)
            if name=='OMS':pos=vecadd(pos,[0,-.28*(-1)**i*m.sin(phi),.28*(-1)**i*m.cos(phi)])
            dry_equipment(stage,f'{name}.{i}',mass,pos,shape,growth=.15)
        for j in range(4):
            x=[.1,1.40,.75,.75][j];r=2.08 if j==0 else 2.10
            t=[-.30*(-1)**i,0,-.30,.30][j]
            pos=vecadd(radial(x,r,phi),[0,-t*m.sin(phi),t*m.cos(phi)])
            dry_equipment(stage,f'RCS.{i}.{j}',3,pos,cyl(.045,.18),growth=.15)
        legphi=phi+m.pi/4
        leg=dry_equipment(stage,f'Leg.{i}',40,radial(.85,1.595,legphi),box(.8,.23,.24),growth=.15)
        leg['deployed_centroid_local_m']=radial(.08,1.98,legphi)
    for i in range(2):
        pos=radial(1.72,.85,m.pi/4+i*m.pi)
        gasbottles.append(dict(id=f'Capsule.He.{i}',stage=stage,internal_volume_m3=.12,internal_radius_m=sphere_r(.12),installation_radius_m=.34,centroid_local_m=pos,rated_pressure_Pa=30e6))
        dry_equipment(stage,f'He.{i}.COPV',20,pos,sphere(sphere_r(.12)+.01,True),growth=.15)

def resource_regions(stage,fraction):
    rows=[];ullage=[]
    for t in (x for x in tanks if x['stage']==stage):
        species=t['species']
        f=fraction[species] if isinstance(fraction,dict) else fraction
        if stage=='Capsule':
            mass=t['loaded_mass_kg']*f
            # PMD guarantees acquisition; centroid displacement is deliberately bounded later.
            center=t['centroid_local_m'];spec=[.4*t['internal_radius_m']**2]*3
            gasv=t['capacity_m3']-mass/RHO[species];gascenter=center
        else:
            total=LOAD[stage]*(2.6 if species=='LOX' else 1)/3.6
            line_mass=sum(f['loaded_mass_kg'] for f in feeds if f['stage']==stage and f['species']==species)
            mass=(total-line_mass)*f
            fp=fill_properties(t,mass/RHO[species]);center=[t['x_bottom_m']+fp['centroid_from_bottom_m'],0,0]
            spec=[fp['axial_specific_inertia_m2'],fp['transverse_specific_inertia_m2'],fp['transverse_specific_inertia_m2']]
            full=tank_integrals(TANK_R,t['cylinder_length_m'],t['length_m'])
            used=tank_integrals(TANK_R,t['cylinder_length_m'],fp['height_m'])
            gasv=full[0]-used[0];gascenter=[t['x_bottom_m']+(full[1]-used[1])/gasv,0,0]
        rows.append(dict(id=t['id']+'.Liquid',stage=stage,resource=species,mass_kg=mass,centroid_local_m=center,specific_inertia_diag=spec))
        temp=90 if species=='LOX' else 225 if species=='RP1' else 273
        pressure=.25e6 if stage!='Capsule' else 2e6
        gasmass=pressure*gasv/(2077.1*temp)
        ullage.append(dict(id=t['id']+'.HeUllage',stage=stage,resource='He',mass_kg=gasmass,centroid_local_m=gascenter,
                           specific_inertia_diag=[.5*TANK_R*TANK_R]*3 if stage!='Capsule' else [.4*.38**2]*3))
    for f in (x for x in feeds if x['stage']==stage):
        rows.append(dict(id=f['id']+'.Liquid',stage=stage,resource=f['species'],mass_kg=f['loaded_mass_kg'],
                         centroid_local_m=f['centroid_local_m'],specific_inertia_diag=[.01,f['developed_length_m']**2/12,f['developed_length_m']**2/12]))
    bottle_mass=HE[stage]-sum(g['mass_kg'] for g in ullage)
    assert bottle_mass>0,(stage,bottle_mass)
    rows+=ullage
    for b in (x for x in gasbottles if x['stage']==stage):
        rows.append(dict(id=b['id']+'.Gas',stage=stage,resource='He',mass_kg=bottle_mass/HE_COUNT[stage],
                         centroid_local_m=b['centroid_local_m'],specific_inertia_diag=[.4*b['internal_radius_m']**2]*3))
    return rows

def shape_diag(c):
    s=c['shape'];k=s['kind'];mass=c['dry_mass_kg']
    if k=='box':a,b,d=s['dimensions_m'];return [mass*(b*b+d*d)/12,mass*(a*a+d*d)/12,mass*(a*a+b*b)/12]
    if k in ('solid_sphere','spherical_shell'):
        value=mass*s['radius_m']**2*(2/3 if k=='spherical_shell' else .4);return [value]*3
    if k=='spherical_cap_shell':
        # Explicit inertia surrogate only; cap shape is retained for later exact integration.
        r=s['base_radius_m'];l=s['rise_m'];r2=r*r
    elif k=='annular_cylinder':r2=s['outer_radius_m']**2+s['inner_radius_m']**2;l=s['length_m']
    else:r2=s['radius_m']**2*(2 if k=='ellipsoidal_tank_shell' else 1);l=s['length_m']
    return [mass*r2/2,mass*(3*r2+l*l)/12,mass*(3*r2+l*l)/12]

def actuator_layout():
    for e in engines:
        actuators.append(dict(id=e['id'],stage=e['stage'],type='launch_main',force_point_local_m=e['force_point_local_m'],
                             force_direction=e['nominal_force_direction'],nozzle_exit_local_m=e['nozzle_exit_local_m']))
    length=stage_length['Upper']
    for i in range(10):
        if i<8:
            phi=(i%4)*m.pi/2;x=.8 if i<4 else length-.5;sign=1 if i%2==0 else -1
            point=radial(x,1.83,phi);direction=[0,-sign*m.sin(phi),sign*m.cos(phi)]
            key=f'Upper.ColdGasAttitude.{i}';thrust=50
        else:
            point=radial(.25,1.6,(i-8)*m.pi);direction=[1,0,0];key=f'Upper.ColdGasSettling.{i-8}';thrust=100
        actuators.append(dict(id=key,stage='Upper',type='helium_cold_gas',force_point_local_m=point,
                             nozzle_exit_local_m=point,force_direction=direction,vacuum_thrust_N=thrust,
                             Isp_budget_s=100,resource='Upper.He',total_shared_gas_budget_kg=6))
        c=next(c for c in components if c['id']==key);c['orientation_quaternion_xyzw']=q_from_x(direction)
        c['centroid_local_m']=vecadd(point,[v*(.09 if i<8 else .10) for v in direction])
    for i in range(4):
        phi=i*m.pi/2;rad=[0,m.cos(phi),m.sin(phi)];tan=[0,-m.sin(phi),m.cos(phi)]
        direction=[m.cos(m.radians(15)),-m.sin(m.radians(15))*rad[1],-m.sin(m.radians(15))*rad[2]]
        for name,x,r,t,thrust,kind in [('TerminalEngine',.35,2.05,0,10000,'terminal_SL'),('OMS',.20,2.04,.28*(-1)**i,500,'orbital_vacuum')]:
            point=vecadd(radial(x,r,phi),[v*t for v in tan]);key=f'Capsule.{name}.{i}'
            c=next(c for c in components if c['id']==key);c['orientation_quaternion_xyzw']=q_from_x(direction)
            actuators.append(dict(id=key,stage='Capsule',type=kind,force_point_local_m=point,nozzle_exit_local_m=point,
                force_direction=direction,thrust_N=thrust,resource='Capsule.MMH/NTO',mixture_ratio=1.65,
                exit_diameter_m=TERMINAL['exit_diameter_m'] if name=='TerminalEngine' else OMS['exit_diameter_m'],
                hardware_cowl_diameter_m=.30 if name=='TerminalEngine' else .14,
                Isp_SL_s=TERMINAL['Isp_full_SL_s'] if name=='TerminalEngine' else None,Isp_vacuum_s=TERMINAL['Isp_full_vac_s'] if name=='TerminalEngine' else 315,
                throttle_min_fraction=.2 if name=='TerminalEngine' else 1,entry_protection='individually owned retained closure'))
        for j in range(4):
            key=f'Capsule.RCS.{i}.{j}';c=next(c for c in components if c['id']==key)
            direction=[1,0,0] if j==0 else [-1,0,0] if j==1 else tan if j==2 else [-v for v in tan]
            c['orientation_quaternion_xyzw']=q_from_x(direction)
            actuators.append(dict(id=key,stage='Capsule',type='bipropellant_RCS',force_point_local_m=c['centroid_local_m'],
                nozzle_exit_local_m=c['centroid_local_m'],force_direction=direction,sea_level_thrust_N=100,vacuum_thrust_N=115,
                Isp_SL_s=RCS['Isp_SL_screen_s'],Isp_vacuum_s=RCS['Isp_vacuum_screen_s'],Isp_budget_s=210,
                exit_diameter_m=RCS['exit_diameter_m'],resource='Capsule.MMH/NTO'))
            c['centroid_local_m']=vecadd(c['centroid_local_m'],[v*.09 for v in direction])
        for name in ['PodTPS','PodStructure','PodCaps']:
            c=next(c for c in components if c['id']==f'Capsule.{name}.{i}')
            c['orientation_quaternion_xyzw']=[m.sin(phi/2),0,0,m.cos(phi/2)]
        c=next(c for c in components if c['id']==f'Capsule.Leg.{i}')
        c['orientation_quaternion_xyzw']=[m.sin((phi+m.pi/4)/2),0,0,m.cos((phi+m.pi/4)/2)]

def witness(stages,fractions,local_origin=0,gear=False,dry_capsule=False):
    rows=[]
    for c in components:
        if c['stage'] not in stages:continue
        center=c.get('deployed_centroid_local_m',c['centroid_local_m']) if gear else c['centroid_local_m']
        pos=vecadd(center,[stage_origin[c['stage']]-local_origin,0,0])
        rows.append((c['dry_mass_kg'],pos,tensor_rotate(shape_diag(c),c['orientation_quaternion_xyzw'])))
    if not dry_capsule:
        for stage in stages:
            for r in resource_regions(stage,fractions.get(stage,1.)):
                pos=vecadd(r['centroid_local_m'],[stage_origin[stage]-local_origin,0,0])
                rows.append((r['mass_kg'],pos,([[r['mass_kg']*v for v in row] for row in r['specific_inertia_tensor']] if 'specific_inertia_tensor' in r else tensor_rotate([r['mass_kg']*v for v in r['specific_inertia_diag']],[0,0,0,1]))))
    mass=sum(v[0] for v in rows);com=[sum(a*p[i] for a,p,d in rows)/mass for i in range(3)]
    tensor=[[0.]*3 for _ in range(3)]
    for a,p,d in rows:
        q=[p[i]-com[i] for i in range(3)];q2=sum(x*x for x in q)
        for i in range(3):
            for j in range(3):tensor[i][j]+=d[i][j]+a*((q2 if i==j else 0)-q[i]*q[j])
    return dict(mass_kg=mass,COM_m=com,inertia_about_COM_kg_m2=tensor,origin_stack_x_m=local_origin,
                fidelity='COM from component placements and settled launch tank fill; tensor uses declared shape surrogates, no runtime authority')

def mass_summary():
    out={}
    for stage in LOAD:
        dry=sum(c['dry_mass_kg'] for c in components if c['stage']==stage)
        feed=sum(f['loaded_mass_kg'] for f in feeds if f['stage']==stage)
        cutoff={}
        if stage!='Capsule':
            tank_fuel=LOAD[stage]/3.6-sum(f['loaded_mass_kg'] for f in feeds if f['stage']==stage and f['species']=='RP1')
            tank_ox=LOAD[stage]*2.6/3.6-sum(f['loaded_mass_kg'] for f in feeds if f['stage']==stage and f['species']=='LOX')
            burn_fuel=min(.995*tank_fuel,.995*tank_ox/2.6)
            cutoff={'RP1':(tank_fuel-burn_fuel)/tank_fuel,'LOX':(tank_ox-2.6*burn_fuel)/tank_ox}
            residual=LOAD[stage]-3.6*burn_fuel
        else:residual=50
        out[stage]=dict(dry_including_payload_kg=dry,payload_kg=250 if stage=='Capsule' else 0,
            helium_kg=HE[stage],propellant_kg=LOAD[stage],wet_kg=dry+HE[stage]+LOAD[stage],
            trapped_feed_kg=feed,cutoff_tank_fraction_by_species=cutoff,
            propellant_retained_at_ascent_cutoff_kg=residual if stage!='Capsule' else LOAD[stage])
    return out

def mission(summary,dry_factor=1,isp_change=0,loss_extra=0,return_extra=0,return_split=None):
    dry={k:(v['dry_including_payload_kg']-v['payload_kg'])*dry_factor+v['payload_kg'] for k,v in summary.items()}
    capsule=dry['Capsule']+HE['Capsule']+LOAD['Capsule']
    upper=dry['Upper']+HE['Upper']+LOAD['Upper']+capsule
    gross=dry['Booster']+HE['Booster']+LOAD['Booster']+upper
    mf1=dry['Booster']+HE['Booster']+summary['Booster']['propellant_retained_at_ascent_cutoff_kg']+upper
    mf2=dry['Upper']+HE['Upper']+summary['Upper']['propellant_retained_at_ascent_cutoff_kg']+capsule
    d1=G0*(310+isp_change)*m.log(gross/mf1);d2=G0*(350+isp_change)*m.log(upper/mf2)
    req=m.sqrt(MU/(RE+300000))-7.292115e-5*RE*m.cos(m.radians(28.6084))+1600+150+100+300+loss_extra
    scale=(500+return_extra)/500
    allocation=return_split if return_split else [350*scale,50*scale,100*scale]
    plan=[('OMS including allocated contingency',allocation[0],(315+isp_change)*m.cos(m.radians(15))),
          ('RCS equivalent allowance',allocation[1],210+isp_change),
          ('terminal landing',allocation[2],TERMINAL['minimum_axial_Isp_with_15deg_cant_s']+isp_change)]
    remaining=capsule;segments=[]
    for name,dv,isp in plan:
        after=remaining*m.exp(-dv/(G0*isp))
        segments.append(dict(role=name,delta_v_m_s=dv,axial_or_effective_Isp_s=isp,m0_kg=remaining,mf_kg=after,consumed_kg=remaining-after))
        remaining=after
    used=capsule-remaining;return_margin=950-used
    return dict(gross_kg=gross,stage1_m0_kg=gross,stage1_mf_kg=mf1,stage2_m0_kg=upper,stage2_mf_kg=mf2,
                stage1_delta_v_m_s=d1,stage2_delta_v_m_s=d2,combined_m_s=d1+d2,required_m_s=req,margin_m_s=d1+d2-req,
                return_segments=segments,return_required_m_s=sum(allocation),return_propellant_used_kg=used,
                return_propellant_margin_above_50kg_floor_kg=return_margin,
                additional_terminal_delta_v_m_s=G0*plan[-1][2]*m.log(remaining/(dry['Capsule']+HE['Capsule']+50)),
                initial_TW=3300000/(gross*GRAVITY),payload_retained_kg=250)

def main():
    build_launch('Booster');build_launch('Upper');build_capsule()
    actuator_layout()
    stage_origin.update(Booster=0.,Upper=stage_length['Booster'],Capsule=stage_length['Booster']+stage_length['Upper'])
    import sys
    from internal_feed_geometry import apply
    apply(sys.modules[__name__])
    summary=mass_summary();nominal=mission(summary)
    assert abs(summary['Capsule']['dry_including_payload_kg']-2882.35)<1e-6
    cases={'nominal':{},'dry_plus_10pct':dict(dry_factor=1.10),'dry_plus_15pct':dict(dry_factor=1.15),
           'Isp_minus_10s':dict(isp_change=-10),'losses_plus_300':dict(loss_extra=300),
           'return_plus_100':dict(return_extra=100),'joint_useful':dict(dry_factor=1.10,isp_change=-5,loss_extra=150,return_extra=100),
           'joint_harsh':dict(dry_factor=1.15,isp_change=-10,loss_extra=300,return_extra=100)}
    sens={name:mission(summary,**args) for name,args in cases.items()}
    for stage in LOAD:
        resources=resource_regions(stage,1)
        assert abs(sum(r['mass_kg'] for r in resources)-LOAD[stage]-HE[stage])<1e-6
        for t in (x for x in tanks if x['stage']==stage):
            resource=next(r for r in resources if r['id']==t['id']+'.Liquid')
            assert resource['mass_kg']/RHO[t['species']]<=t['capacity_m3']*.95+1e-10
        assert HE[stage]<=HE_COUNT[stage]*HE_VOLUME[stage]*40
    witnesses={
      'full_stack_wet':witness(list(LOAD),{}),
      'first_stage_cutoff_attached':witness(list(LOAD),{'Booster':summary['Booster']['cutoff_tank_fraction_by_species']}),
      'upper_capsule_after_first_separation':witness(['Upper','Capsule'],{},stage_origin['Upper']),
      'upper_cutoff_attached_capsule':witness(['Upper','Capsule'],{'Upper':summary['Upper']['cutoff_tank_fraction_by_species']},stage_origin['Upper']),
      'loaded_capsule':witness(['Capsule'],{},stage_origin['Capsule']),
      'return_capsule_50kg_prop_gear_deployed':witness(['Capsule'],{'Capsule':.05},stage_origin['Capsule'],gear=True),
      'dry_capsule_gear_stowed':witness(['Capsule'],{},stage_origin['Capsule'],dry_capsule=True)}
    dryjson=dict(article=ID,components=[{k:c[k] for k in ['id','stage','dry_mass_kg','mass_basis']+([ 'baseline_kg','growth_fraction'] if 'baseline_kg' in c else [])} for c in components],derived_stages=summary,
                 spatial_definitions='stage2-mass-regions.json')
    dump('internal-routing.json',internal_routing_report)
    dump('stage1-mass-budget.json',dryjson)
    dump('stage1-tanks.json',dict(liquid_tanks=tanks,helium_COPVs=gasbottles,feed_geometry_reference='stage2-tank-layout.json',
                               feed_inventory=[{k:f[k] for k in ['id','stage','species','volume_m3','loaded_mass_kg']} for f in feeds]))
    dump('stage1-propellant.json',dict(loaded_by_stage_kg=LOAD,helium_by_stage_kg=HE,density_kg_m3=RHO,mixture_ratios=O_F,
                                    full_loaded_regions=[r for stage in LOAD for r in resource_regions(stage,1)]))
    dump('stage1-propulsion.json',dict(launch_engines=engines,terminal_pressure_nozzle_screen=TERMINAL,
         OMS_pressure_nozzle_screen=OMS,RCS_pressure_nozzle_screen=RCS,
         return_model='Separate OMS/RCS/landing rocket-equation burns; no uniform270s return floor'))
    dump('stage1-delta-v.json',nominal);dump('stage1-sensitivity.json',sens)
    dump('stage1-return.json',dict(nominal_contingency_spent_on_landing=mission(summary,return_split=[300,50,150]),
         useful_degradation_extra_100_only_landing=mission(summary,dry_factor=1.1,isp_change=-5,loss_extra=150,return_split=[350,50,200]),
         scope='Allocation sensitivity; do not represent600m/s as a universal return capacity.'))
    dump('stage2-layout.json',dict(article=ID,body_OD_m=3.6,maximum_enclosing_diameter_m=4.44,stage_origins_x_m=stage_origin,
         stage_origin_spacings_m=stage_length,total_stack_length_m=sum(stage_length.values()),upper_physical_reach_m=stage_length['Upper']+.36,
         separation_planes_stack_x_m=[stage_origin['Upper'],stage_origin['Capsule']+.36],occupied_geometry_authority='blender-handoff.md'))
    dump('stage2-engine-layout.json',dict(article=ID,launch_engines=engines,actuators=actuators))
    dump('stage2-tank-layout.json',dict(article=ID,liquid_tanks=tanks,helium_COPVs=gasbottles,feed_routes=feeds))
    dump('stage2-com-witnesses.json',witnesses)
    dump('stage2-inertia-inputs.json',dict(article=ID,component_inputs='stage2-mass-regions.json:dry_regions',
         fields=['dry_mass_kg','centroid_local_m','orientation_quaternion_xyzw','shape','deployed_centroid_local_m when applicable'],
         resource_inputs='stage1-propellant.json:full_loaded_regions; resource_regions() for depletion states',
         derivation='shape_diag() then tensor_rotate() and parallel-axis sum about derived COM; do not use complete-stack tensor as input',
         fidelity='Analytic settled launch-liquid sections; dry shell/pipe/recovery/structure shape surrogates. Capsule liquid centered PMD proxy, not guaranteed fixed COM. No runtime authority.'))
    dump('stage2-mass-regions.json',dict(article=ID,stage_origins_x_m=stage_origin,dry_regions=components,
         loaded_resource_regions=[r for stage in LOAD for r in resource_regions(stage,1)]))
    print(json.dumps(dict(masses=summary,mission=nominal,sensitivity={k:round(v['margin_m_s'],2) for k,v in sens.items()},
                          lengths=stage_length,origins=stage_origin,COM={k:v['COM_m'] for k,v in witnesses.items()}),indent=2))

if __name__=='__main__':main()
