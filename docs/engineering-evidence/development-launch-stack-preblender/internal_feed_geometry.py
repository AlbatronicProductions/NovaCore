"""Development internal dry-tunnel geometry. No thermal performance authority.
Pinned numerical dependency: NumPy 2.3.5. Explicit wet-volume subtraction;
same stage owners, finite pipe inventories, immutable material datums.
"""
import math,copy
import numpy as np

def apply(c):
    baseline=copy.deepcopy(dict(components=c.components,tanks=c.tanks,feeds=c.feeds,gas=c.gasbottles))
    before_summary=c.mass_summary()
    old_resource_regions=c.resource_regions
    CFG={'Booster':dict(offset=.43,tunnel_ri=.150,tunnel_wall=.014,insulation=.012,pipe_ri=.125,pipe_wall=.003,boss_ro=.235,boss_t=.008,guide_pitch=1.0),
         'Upper':dict(offset=.40,tunnel_ri=.077,tunnel_wall=.006,insulation=.010,pipe_ri=.055,pipe_wall=.002,boss_ro=.135,boss_t=.006,guide_pitch=.75)}

    def disk_rule(radius,offset,n=64):
        # Deterministic Gauss radial quadrature plus periodic midpoint angular rule.
        x,w=np.polynomial.legendre.leggauss(n);rho=(x+1)*radius/2;rw=w*radius/2*rho
        phi=(np.arange(n*2)+.5)*math.pi/n
        y=offset+rho[:,None]*np.cos(phi);z=rho[:,None]*np.sin(phi)
        weights=np.broadcast_to(rw[:,None]*math.pi/n,y.shape)
        return y.ravel(),z.ravel(),weights.ravel()

    def tunnel_moments(t,height=None,n=64,radius=None):
        cfg=CFG[t['stage']];r=radius or cfg['tunnel_ri']+cfg['tunnel_wall'];y,z,w=disk_rule(r,cfg['offset'],n)
        h=.85;R=1.7;lc=t['cylinder_length_m']
        f=np.sqrt(1-(y*y+z*z)/(R*R));lo=h*(1-f);hi=h+lc+h*f
        if height is not None:hi=np.maximum(lo,np.minimum(hi,height))
        length=hi-lo;sx=(hi*hi-lo*lo)/2;sxx=(hi**3-lo**3)/3
        # Raw volume, first and second moments in tank-local coordinates.
        first=np.array([np.sum(w*sx),np.sum(w*length*y),np.sum(w*length*z)])
        second=np.array([[np.sum(w*sxx),np.sum(w*sx*y),np.sum(w*sx*z)],
                         [np.sum(w*sx*y),np.sum(w*length*y*y),np.sum(w*length*y*z)],
                         [np.sum(w*sx*z),np.sum(w*length*y*z),np.sum(w*length*z*z)]])
        return float(np.sum(w*length)),first,second

    def liquid_integrals(t,height=None,n=64):
        height=t['length_m'] if height is None else height
        v,q,q2,ir=c.tank_integrals(1.7,t['cylinder_length_m'],height)
        first=np.array([q,0,0]);second=np.diag([q2,ir/2,ir/2])
        dv,df,ds=tunnel_moments(t,height,n)
        return v-dv,first-df,second-ds

    def head_patch(radius,offset,n=64):
        y,z,w=disk_rule(radius,offset,n);r2=y*y+z*z
        jac=np.sqrt(1+.85**2*r2/(1.7**4*(1-r2/1.7**2)))
        return float(np.sum(w*jac))

    def new_component(stage,name,mass,center,shape,basis):
        return c.add(stage,name,mass,center,shape,basis)

    def dogleg(x0,y0,delta,R=.4,down=False,n=64):
        # Two circular arcs: parallel axial inlet/outlet, exact offset, radius R.
        theta=math.acos(1-abs(delta)/(2*R));sign=1 if delta>0 else -1
        result=[]
        for k in range(n+1):
            a=theta*k/n;result.append([x0+R*math.sin(a),y0+sign*R*(1-math.cos(a)),0])
        xm=x0+R*math.sin(theta);ym=y0+sign*R*(1-math.cos(theta))
        for k in range(1,n+1):
            a=theta*k/n;result.append([xm+R*(math.sin(theta)-math.sin(theta-a)),ym+sign*R*(math.cos(theta-a)-math.cos(theta)),0])
        return result

    def new_route(st,t):
        g=CFG[st];rp=next(t for t in c.tanks if t['stage']==st and t['species']=='RP1')
        d=g['offset'];outlet=rp['x_bottom_m']+.85*(1-math.sqrt(1-(d/1.7)**2))
        if st=='Upper':return [[outlet,d,0],[2.55,d,0]]
        # Lower bend radius 270 mm (same 250 mm bore), joins existing ring tangential inlet node.
        arc=[[3.82-.27*math.sin(a),.70-.27*math.cos(a),0] for a in np.linspace(0,math.pi/2,129)]
        lower=list(reversed(arc))
        first=dogleg(18.994,.43,.17)
        second=dogleg(19.901,.60,-.17)
        ascending=lower+[[18.994,.43,0]]+first[1:]+[[19.901,.60,0]]+second[1:]+[[outlet,.43,0]]
        return list(reversed(ascending))

    rows=[]
    for st,g in CFG.items():
        old=next(t for t in baseline['tanks'] if t['stage']==st and t['species']=='LOX')
        t=next(t for t in c.tanks if t['stage']==st and t['species']=='LOX')
        ro=g['tunnel_ri']+g['tunnel_wall'];oldlc=t['cylinder_length_m']
        while True:
            t['length_m']=t['cylinder_length_m']+1.7
            if liquid_integrals(t)[0]>=old['capacity_m3']:break
            t['cylinder_length_m']=round(t['cylinder_length_m']+.001,6)
        extension=t['cylinder_length_m']-oldlc
        displaced=tunnel_moments(t)[0];recovered=math.pi*1.7**2*extension
        t['capacity_m3']=liquid_integrals(t)[0];t['dry_tunnel']=dict(**g,outer_radius_m=ro)
        t['shell_area_m2']=old['shell_area_m2']+2*math.pi*1.7*extension-2*head_patch(ro,g['offset'])
        t['shell_mass_kg']=t['shell_area_m2']*t['wall_thickness_m']*c.AL_RHO
        comp=next(x for x in c.components if x['id']==st+'.LOX.TankShell')
        comp['dry_mass_kg']=t['shell_mass_kg'];comp['centroid_local_m'][0]=t['x_bottom_m']+t['length_m']/2
        comp['shape'].update(cylinder_length_m=t['cylinder_length_m'],length_m=t['length_m'])
        # Removing off-axis head patches also shifts shell lateral mass center.
        patchmass=2*head_patch(ro,g['offset'])*t['wall_thickness_m']*c.AL_RHO
        comp['centroid_local_m'][1]=-patchmass*g['offset']/t['shell_mass_kg']
        for suffix in ('WeldRingsBafflesPorts','InsulationSupports'):
            o=next(x for x in c.components if x['id']==st+'.LOX.'+suffix)
            o['centroid_local_m'][0]=t['x_bottom_m']+t['length_m']/2;o['shape']['length_m']=t['length_m']
            # Preserve original per-length allowance, count the extension explicitly.
            o['dry_mass_kg']*=t['length_m']/old['length_m']
        center=[t['x_bottom_m']+t['length_m']/2,g['offset'],0]
        sleeve_length=t['length_m']+.080
        wallmass=math.pi*(ro**2-g['tunnel_ri']**2)*sleeve_length*c.AL_RHO
        new_component(st,'Feed.DryTunnelWall',wallmass,center,c.annulus(g['tunnel_ri'],ro,sleeve_length),'candidate Al sleeve volume x2840; 40mm axial extension each end')
        pipe_ro=g['pipe_ri']+g['pipe_wall'];ins_ro=pipe_ro+g['insulation']
        insmass=math.pi*(ins_ro**2-pipe_ro**2)*sleeve_length*120
        new_component(st,'Feed.DryTunnelInsulation',insmass,center,c.annulus(pipe_ro,ins_ro,sleeve_length),'candidate insulation density120kg/m3; excluded from liquid with whole sleeve')
        bossmass=(head_patch(g['boss_ro'],g['offset'])-head_patch(ro,g['offset']))*g['boss_t']*c.AL_RHO
        for end in (0,1):
            x=t['x_bottom_m']+(t['length_m'] if end else 0)
            new_component(st,f'Feed.TunnelHeadReinforcement.{end}',bossmass,[x,g['offset'],0],c.annulus(ro,g['boss_ro'],g['boss_t']),'contoured annular head doubler, volume x2840; external to liquid')
        guides=math.ceil(sleeve_length/g['guide_pitch'])+1
        # 3 low-conductivity guide shoes per station, fully inside the dry annulus.
        guide_mass=guides*3*.020*.020*(g['tunnel_ri']-ins_ro)*1800
        new_component(st,'Feed.DryTunnelGuides',guide_mass,center,c.annulus(ins_ro,g['tunnel_ri'],sleeve_length),'3 shoes/station,20x20mm,radial dry gap,density1800; distributed proxy')
        # Dedicated end fittings, flex isolation and seals, explicitly additional allowance.
        new_component(st,'Feed.TunnelEndFittings',18. if st=='Booster' else 6.,center,c.annulus(pipe_ro,ro,sleeve_length),'candidate owned allowance: end collars,seals,axial slip isolation; not flight qualified')
        c.components[:]=[x for x in c.components if x['id']!=st+'.Feed.ExternalRaceway']
        feed=next(f for f in c.feeds if f['id']==st+'.Feed.0');path=new_route(st,t)
        length,cent=c.path_measure([path]);feed.update(centerline_paths_local_m=[path],developed_length_m=length,volume_m3=math.pi*g['pipe_ri']**2*length,centroid_local_m=cent)
        feed['loaded_mass_kg']=feed['volume_m3']*810
        pipe=next(x for x in c.components if x['id']==st+'.Feed.0.PipeShell')
        pipe.update(dry_mass_kg=math.pi*(pipe_ro**2-g['pipe_ri']**2)*length*2700,centroid_local_m=cent,shape=c.cyl(pipe_ro,length))
        # Two explicit reinforced off-axis RP1 outlet components replace no existing allowance.
        outlet_reinforcement=math.pi*((pipe_ro+.045)**2-pipe_ro**2)*(.012 if st=='Booster' else .008)*c.AL_RHO
        new_component(st,'Feed.RP1OutletReinforcement',outlet_reinforcement,path[0],c.annulus(pipe_ro,pipe_ro+.045,.012 if st=='Booster' else .008),'candidate additional off-axis outlet doubler; original port allowance retained conservatively')
        rows.append(dict(stage=st,geometry=copy.deepcopy(t),original_capacity_m3=old['capacity_m3'],dry_displacement_m3=displaced,recovered_gross_volume_m3=recovered,new_capacity_m3=t['capacity_m3'],old_max_usable_m3=.95*old['capacity_m3'],usable_loss_m3=.95*displaced,usable_recovery_m3=.95*recovered,new_max_usable_m3=.95*t['capacity_m3'],cylinder_extension_m=extension,original_top_m=old['x_bottom_m']+old['length_m'],new_top_m=t['x_bottom_m']+t['length_m'],tunnel_wall_mass_kg=wallmass,insulation_mass_kg=insmass,head_reinforcement_each_kg=bossmass,guide_mass_kg=guide_mass,rp1_route=feed,quadrature_capacity_delta_64_to_128_m3=liquid_integrals(t,n=128)[0]-t['capacity_m3']))

    def corrected_resources(st,fraction):
        # Baseline functions see temporary original LOX tank to obtain independent RP1/He/line records.
        target=next((t for t in c.tanks if t['stage']==st and t.get('dry_tunnel')),None)
        if target is None:return old_resource_regions(st,fraction)
        idx=c.tanks.index(target);old=copy.deepcopy(next(t for t in baseline['tanks'] if t['id']==target['id']))
        c.tanks[idx]=old
        try:rr=old_resource_regions(st,fraction)
        finally:c.tanks[idx]=target
        r=next(r for r in rr if r['id']==target['id']+'.Liquid');volume=r['mass_kg']/1141
        lo=0;hi=target['length_m']
        for _ in range(55):
            h=(lo+hi)/2
            if liquid_integrals(target,h)[0]<volume:lo=h
            else:hi=h
        h=(lo+hi)/2;v,f,s=liquid_integrals(target,h);com=f/v;cov=s/v-np.outer(com,com)
        tensor=np.eye(3)*np.trace(cov)-cov
        r.update(centroid_local_m=[target['x_bottom_m']+com[0],com[1],com[2]],specific_inertia_diag=np.diag(tensor).tolist(),specific_inertia_tensor=tensor.tolist(),settled_height_m=h)
        full,ff,ss=liquid_integrals(target);gv=full-v;gc=(ff-f)/gv
        gas=next(r for r in rr if r['id']==target['id']+'.HeUllage')
        previous=gas['mass_kg'];gm=.25e6*gv/(2077.1*90)
        gas.update(mass_kg=gm,centroid_local_m=[target['x_bottom_m']+gc[0],gc[1],gc[2]])
        gascov=(ss-s)/gv-np.outer(gc,gc);gast=np.eye(3)*np.trace(gascov)-gascov
        gas['specific_inertia_tensor']=gast.tolist();gas['specific_inertia_diag']=np.diag(gast).tolist()
        for b in rr:
            if '.He.' in b['id'] and b['id'].endswith('.Gas'):b['mass_kg']-=(gm-previous)/c.HE_COUNT[st]
        return rr
    c.resource_regions=corrected_resources
    # Account for real flexible engine-side paths from the authored correction.
    def update_feed(identifier,paths):
        feed=next(f for f in c.feeds if f['id']==identifier)
        length,center=c.path_measure(paths);di=feed['internal_diameter_m'];st=feed['stage'];wall=.003 if st=='Booster' else .002
        feed.update(centerline_paths_local_m=paths,developed_length_m=length,centroid_local_m=center,volume_m3=math.pi*(di/2)**2*length)
        feed['loaded_mass_kg']=feed['volume_m3']*c.RHO[feed['species']]
        pipe=next(p for p in c.components if p['id']==identifier+'.PipeShell')
        pipe.update(dry_mass_kg=math.pi*((di/2+wall)**2-(di/2)**2)*length*2700,centroid_local_m=center,shape=c.cyl(di/2+wall,length))
    paths=[]
    for i in range(3):
        phi=2*math.pi*i/3;rad=np.array([0,math.cos(phi),math.sin(phi)]);tan=np.array([0,-rad[2],rad[1]])
        p0=np.array([3.55,0,0])+rad*.7;p1=np.array([3.80,0,0])+rad*.7+tan*.25
        p2=np.array([3.80,0,0])+rad*1.03+tan*.72;p3=np.array([3.62,0,0])+rad*1.03+tan*.72;end=np.array([3.25,0,0])+rad*1.03
        path=[]
        for j in range(41):
            t=j/40;s=1-t;path.append((s**3*p0+3*s*s*t*p1+3*s*t*t*p2+t**3*p3).tolist())
        for j in range(1,17):path.append((p3+(end-p3)*j/16).tolist())
        paths.append(path)
    update_feed('Booster.Feed.3',paths)
    update_feed('Upper.Feed.2',[[[2.55,.4,0],[2.5,.4,0],[2.5,.13,0]]])
    for st,bottom,end in [('Booster',4.1,2.985),('Upper',3.1,2.1)]:
        update_feed(st+'.Feed.1',[[[bottom,0,0],[end,0,0]]])
        c.dry_equipment(st,'Feed.FlexibleThermalInterfaces',42 if st=='Booster' else 8,[3.5 if st=='Booster' else 2.5,0,0],c.ring(1.0 if st=='Booster' else .4,.35),growth=.15)
        c.dry_equipment(st,'Feed.InsulationJacketsAndSupports',35 if st=='Booster' else 10,[3.8 if st=='Booster' else 2.8,0,0],c.ring(1.1 if st=='Booster' else .5,.5),growth=.15)
    c.internal_routing_report=dict(status='DEVELOPMENT_GEOMETRY_SELECTED',thermal_performance='FUTURE QUALIFICATION REQUIRED',
        design_context=dict(RP1_loading_min_K=283.15,LOX_loaded_hold_hours=6,RP1_floor_K=239.15,qualification_claimed=False),
        geometry=rows,stage_owned=True,external_envelope_changed=False,
        thermal_envelopes=dict(downcomer_insulation_m={'Booster':.012,'Upper':.010},dry_gap_m=.010,
            sleeve_end_axial_reservation_each_m=.040,flexible_fuel_interface_radial_reservation_m=.012,
            basis='Physical installation reservations and owned conservative hardware allowances only; no transient thermal, recirculation or operations design.'),
        accounting='Exact declared pipe developed-polyline volumes; liquid removed from tanks. Added jackets/support/flexible interfaces owned allowances include15% growth. No complete-vehicle mass input.')
    return c.internal_routing_report
