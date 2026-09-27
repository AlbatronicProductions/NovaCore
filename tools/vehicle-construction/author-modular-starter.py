"""Original six-part greybox authoring; no Blender/KSA input or runtime authority.

Reproduce with Python 3.11+: python tools/vehicle-construction/author-modular-starter.py
All outputs are limited to assets/vehicles/modular-starter. --check never writes.
Analytic dry distributions are effective assembly distributions, not material density.
JSON numbers are binary64; aggregate rounding uses exact Fractions of authored fields.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import struct
from fractions import Fraction as F

ROOT = Path(__file__).resolve().parents[2] / 'assets/vehicles/modular-starter'
I = [1., 0., 0., 0., 1., 0., 0., 0., 1.]
MATE = [-1., 0., 0., 0., -1., 0., 0., 0., 1.]
A, B = 'nc.resource.development-a', 'nc.resource.development-b'
PROVENANCE = 'Original NovaCore analytic greybox; author-modular-starter.py; SI; not flight certification'


def encoded(obj):
    return (json.dumps(obj, indent=2, allow_nan=False) + '\n').encode()


def sha(data):
    return hashlib.sha256(data).hexdigest()


def vec(p):
    return dict(zip(('x', 'y', 'z'), p))


def matrix(m):
    return dict(zip('abcdefghi', m))


def diag(v):
    return [v[0], 0., 0., 0., v[1], 0., 0., 0., v[2]]


def pose(p=(0., 0., 0.), r=I):
    return dict(position=vec(p), rotation=matrix(r))


def mul(a, b):
    return [sum(a[3*i+k]*b[3*k+j] for k in range(3)) for i in range(3) for j in range(3)]


def roll(angle):
    c, s = math.cos(angle), math.sin(angle)
    # Cardinal authored frames are exact. Diagonal frames share one sqrt(1/2).
    c, s = [0. if abs(v) < 1e-14 else (math.copysign(1., v) if abs(abs(v)-1) < 1e-14 else math.copysign(math.sqrt(.5), v)) for v in (c, s)]
    return [1., 0., 0., 0., c, -s, 0., s, c]


def primitive(name, mass, center, kind, dimensions, rotation=I):
    q = list(map(F, dimensions)); m = F(mass)
    if kind == 'box':
        x, y, z = q
        inertia = [m*(y*y+z*z)/12, m*(x*x+z*z)/12, m*(x*x+y*y)/12]
    else:
        ri, ro, length = q
        k = ri*ri+ro*ro
        inertia = [m*k/2, m*(k/4+length*length/12), m*(k/4+length*length/12)]
    tensor=[sum(F(rotation[3*i+k])*inertia[k]*F(rotation[3*j+k]) for k in range(3)) for i in range(3) for j in range(3)]
    return dict(id=name, massKg=mass, com=vec(center), inertiaAtCom=matrix(list(map(float,tensor)))), dict(id=name, massKg=mass, center=center, kind=kind, dimensions=dimensions,rotation=rotation)


def aggregate(regions):
    mass = sum(F(r['massKg']) for r in regions)
    first = [sum(F(r['massKg'])*F(list(r['com'].values())[i]) for r in regions) for i in range(3)]
    com = [x/mass for x in first]
    inertia = [F(0) for _ in range(9)]
    for r in regions:
        p = [F(x)-c for x, c in zip(r['com'].values(), com)]
        t = list(r['inertiaAtCom'].values()); m = F(r['massKg'])
        for i in range(3):
            for j in range(3):
                inertia[3*i+j] += F(t[3*i+j]) + m*((sum(x*x for x in p) if i == j else 0)-p[i]*p[j])
    return float(mass), list(map(float, com)), list(map(float, inertia))


def box_vertices(center, size):
    return [[center[i]+sgn[i]*size[i]/2 for i in range(3)] for sgn in [(x, y, z) for x in (-1, 1) for y in (-1, 1) for z in (-1, 1)]]


def rotated(points,center,rotation):
    return [[center[i]+sum(rotation[3*i+j]*(p[j]-center[j]) for j in range(3)) for i in range(3)] for p in points]


def prism_vertices(x0, x1, radius, n=8, phase=math.pi/8):
    return [[x, radius*math.cos(phase+2*math.pi*i/n), radius*math.sin(phase+2*math.pi*i/n)] for x in (x0, x1) for i in range(n)]


def volume(name, vertices, why=PROVENANCE):
    return dict(id=name, vertices=list(map(vec, vertices)), provenance=why)


def interface(name, p, r, size='NC-2', radial=False, plug=False, services='Electricity, Data'):
    family = 'nc.radial-equipment' if radial else 'nc.stack'
    mech = dict(interface=name, kind='Radial' if radial else 'Stack', family=family, revision=1, size=size,
                role='Plug' if plug else 'Socket', clockDegrees=[0] if radial else [0, 90, 180, 270])
    return dict(id=name, family=f'{family}/1/{size}', frame=pose(p, r)), mech, dict(interface=name, services=services, detachable=False)


class Services:
    def __init__(self):
        self.ports, self.routes = [], []

    def port(self, name, service, resource=None, **owner):
        p = dict(id=name, service=service, resource=resource, interface=None, store=None, consumer=None, electrical=None, command=False)
        p.update(owner); self.ports.append(p)
        return name

    def wire(self, a, b):
        self.routes.append(dict(id=a+'--'+b, **{'from':a, 'to':b}, bidirectional=True))

    def endpoint(self, name, propellant=False):
        self.port(name+'.power', 'Electricity', interface=name)
        self.port(name+'.data', 'Data', interface=name)
        if propellant:
            for label, resource in [('a', A), ('b', B)]:
                self.port(name+'.'+label, 'Propellant', resource, interface=name)


def consumer(name, point, axis, thrust, inlet, main=False):
    return dict(id=name, subpart='gimbal' if main else None,
                model='nc.actuator.main/1' if main else 'nc.actuator.attitude/1',
                totalFlowKgS=thrust/3072, mixture=[dict(resource=A, weight=2), dict(resource=B, weight=3)],
                feedStores=[], feedInterfaces=[inlet], flowRule='FarthestFirst', forcePoint=vec(point), axis=vec(axis), thrustN=thrust,
                gimbal=dict(id='gimbal', pivot=vec((0., 0., 0.)), nozzleOffset=vec((-.9, 0., 0.)), limitY=.05, limitZ=.05, slewRate=.04) if main else None)


def glb(identity, shapes, attachments, gimbal=False):
    """Flat shaded original primitive meshes, glTF=(Y,-Z,-X), no external data."""
    blob = bytearray(); views = []; accessors = []; meshes = []
    nodes = [dict(name='ROOT_'+identity, children=[])]
    if gimbal:
        nodes.append(dict(name='MODULE_gimbal', children=[])); nodes[0]['children'].append(1)

    def array(values, code, component, components):
        if code=='f':
            # Accessor bounds describe stored FLOAT data, not the higher precision authoring values.
            values=[struct.unpack('<'+'f'*components,struct.pack('<'+'f'*components,*v)) for v in values]
        while len(blob) % 4: blob.append(0)
        start = len(blob)
        for v in values: blob.extend(struct.pack('<'+code*components, *v))
        vi = len(views); views.append(dict(buffer=0, byteOffset=start, byteLength=len(blob)-start))
        ai = len(accessors); accessors.append(dict(bufferView=vi, componentType=component, count=len(values), type='VEC3' if components == 3 else 'SCALAR'))
        if components == 3:
            accessors[-1].update(min=[min(v[i] for v in values) for i in range(3)], max=[max(v[i] for v in values) for i in range(3)])
        return ai

    for shape in shapes:
        p = []; n = []; indices = []

        def face(points):
            points=rotated(points,shape['center'],shape.get('rotation',I))
            u, v = [[points[k][i]-points[0][i] for i in range(3)] for k in (1, 2)]
            normal = [u[1]*v[2]-u[2]*v[1], u[2]*v[0]-u[0]*v[2], u[0]*v[1]-u[1]*v[0]]
            length = math.sqrt(sum(x*x for x in normal)); normal = [x/length for x in normal]
            first = len(p)
            for point in points:
                p.append((point[1], -point[2], -point[0])); n.append((normal[1], -normal[2], -normal[0]))
            for k in range(1, len(points)-1): indices.extend([(first,), (first+k,), (first+k+1,)])

        center = shape['center']; dims = shape['dimensions']
        if shape['kind'] == 'box':
            vertices = box_vertices(center, dims)
            for f in [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]: face([vertices[j] for j in f])
        else:
            ri, ro, length = dims; x0, x1 = center[0]-length/2, center[0]+length/2; count = 24
            def ring(x, r, j):
                a = 2*math.pi*j/count
                return [x, center[1]+r*math.cos(a), center[2]+r*math.sin(a)]
            for j in range(count):
                k = j+1
                face([ring(x0,ro,j), ring(x0,ro,k), ring(x1,ro,k), ring(x1,ro,j)])
                if ri:
                    face([ring(x0,ri,k), ring(x0,ri,j), ring(x1,ri,j), ring(x1,ri,k)])
                    face([ring(x1,ri,j), ring(x1,ro,j), ring(x1,ro,k), ring(x1,ri,k)])
                    face([ring(x0,ri,k), ring(x0,ro,k), ring(x0,ro,j), ring(x0,ri,j)])
                else:
                    face([[x1,center[1],center[2]], ring(x1,ro,j), ring(x1,ro,k)])
                    face([[x0,center[1],center[2]], ring(x0,ro,k), ring(x0,ro,j)])
        mi = len(meshes)
        meshes.append(dict(name=shape['id'], primitives=[dict(attributes=dict(POSITION=array(p,'f',5126,3), NORMAL=array(n,'f',5126,3)), indices=array(indices,'I',5125,1), material=shape.get('material',0), mode=4)]))
        ni = len(nodes); nodes.append(dict(name='MESH_'+shape['id'], mesh=mi)); nodes[1 if shape.get('moving') else 0]['children'].append(ni)
    for a in attachments:
        ni = len(nodes); point = list(a['frame']['position'].values())
        # Socket orientation is authored physical data; visual marker has the matching outward frame.
        r = list(a['frame']['rotation'].values()); basis = [0.,1.,0.,0.,0.,-1.,-1.,0.,0.]
        rg = mul(mul(basis,r), [basis[3*j+i] for i in range(3) for j in range(3)])
        # Stable matrix to unit quaternion, using largest diagonal branch.
        t = rg[0]+rg[4]+rg[8]
        if t > 0:
            s=math.sqrt(t+1)*2; q=[(rg[7]-rg[5])/s,(rg[2]-rg[6])/s,(rg[3]-rg[1])/s,s/4]
        else:
            i=max(range(3),key=lambda k:rg[4*k]); j=(i+1)%3; k=(i+2)%3
            s=math.sqrt(1+rg[4*i]-rg[4*j]-rg[4*k])*2; q=[0.,0.,0.,(rg[3*k+j]-rg[3*j+k])/s]
            q[i]=s/4; q[j]=(rg[3*j+i]+rg[3*i+j])/s; q[k]=(rg[3*k+i]+rg[3*i+k])/s
        nodes.append(dict(name='SOCKET_'+a['id'],translation=[point[1],-point[2],-point[0]],rotation=q)); nodes[0]['children'].append(ni)
    while len(blob)%4: blob.append(0)
    d=dict(asset=dict(version='2.0',generator='NovaCore original analytic greybox author'), scene=0, scenes=[dict(nodes=[0])], nodes=nodes,meshes=meshes,
           materials=[dict(name='Greybox',pbrMetallicRoughness=dict(baseColorFactor=[.53,.59,.66,1.],metallicFactor=.15,roughnessFactor=.7)),dict(name='Actuator exits',pbrMetallicRoughness=dict(baseColorFactor=[.95,.42,.08,1.],metallicFactor=.15,roughnessFactor=.7))],
           buffers=[dict(byteLength=len(blob))],bufferViews=views,accessors=accessors)
    js=json.dumps(d,separators=(',',':'),allow_nan=False).encode(); js+=b' '*((-len(js))%4)
    return struct.pack('<5I',0x46546c67,2,28+len(js)+len(blob),len(js),0x4e4f534a)+js+struct.pack('<2I',len(blob),0x004e4942)+blob,[n['name'] for n in nodes]


def author():
    outputs={}; definitions=[]
    for kind in ['core','short','long','adapter','engine','block']:
        ids={'core':'nc.core.command-2','short':'nc.tank.short-2','long':'nc.tank.long-2','adapter':'nc.mount.single-2to1','engine':'nc.engine.main-1','block':'nc.rcs.block-r1'}
        names={'core':'Command core','short':'Short tank','long':'Long tank','adapter':'Engine adapter and feet','engine':'Main engine','block':'Attitude block'}
        id=ids[kind]; regions=[]; primitives=[]; shapes=[]; endpoints=[]; collision=[]; clearance=[]; supports=[]; stores=[]; laws=[]; geometries=[]; consumers=[]; electrical=[]; groups=[]; subparts=[]; services=Services()

        def part(name,m,c,k,d,rotation=I):
            region,source=primitive(name,m,c,k,d,rotation); regions.append(region); primitives.append(source); shapes.append(source.copy())

        def end(name,p,r,size='NC-2',radial=False,plug=False,propellant=False):
            endpoints.append(interface(name,p,r,size,radial,plug,'Propellant, Electricity, Data' if propellant else 'Electricity, Data')); services.endpoint(name,propellant)

        def load(watts):
            electrical.append(dict(id='controller',role='Load',capacityJ=0.,watts=watts,driver=None)); services.port('controller.power','Electricity',electrical='controller')

        if kind=='core':
            part('core',120.,(.3,0.,0.),'annulus',(0.,.5,.6)); shapes[0]['dimensions']=(0.,.6,.6)
            end('aft',(0.,0.,0.),MATE)
            collision.append(volume('body',prism_vertices(0.,.6,.6/math.cos(math.pi/8))))
            electrical.append(dict(id='battery',role='Battery',capacityJ=90000.,watts=0.,driver=None)); load(30.)
            services.port('battery.power','Electricity',electrical='battery'); services.port('command.data','Data',command=True)
            services.wire('battery.power','controller.power'); services.wire('battery.power','aft.power'); services.wire('command.data','aft.data')
        elif kind in ('short','long'):
            length=1.5 if kind=='short' else 3.; multiplier=length/1.5
            part('shell',120. if kind=='short' else 180.,(length/2,0.,0.),'annulus',(.5,.6,length))
            end('fore',(length,0.,0.),I,plug=True); end('aft',(0.,0.,0.),MATE,propellant=True)
            collision.append(volume('body',prism_vertices(0.,length,.6/math.cos(math.pi/8)),PROVENANCE+'; circumscribed socket-aligned octagon, max radius .649436 m'))
            for i in range(8):
                r=roll(i*math.pi/4); c,s=r[4],r[7]
                end('radial-'+str(i),(length/2,.6*c,.6*s),mul(r,[0.,-1.,0.,1.,0.,0.,0.,0.,1.]),'R-1',True,False,True)
            sockets=['radial-'+str(i) for i in range(8)]
            groups=[dict(id='attitude-ring',axis=pose((length/2,0.,0.)),sockets=sockets,
                         placements=[dict(anchor=sockets[i],count=count,sockets=[sockets[(i+j*8//count)%8] for j in range(count)]) for i in range(8) for count in (1,2,4,8)])]
            span=.32*multiplier/(math.pi*.125); inner=math.sqrt(.125)
            for label,resource,density,mass,ri,ro in [('a',A,1000.,320.*multiplier,0.,inner),('b',B,1500.,480.*multiplier,inner,.5)]:
                stores.append(dict(id=label,species=resource,resourceIdentity=resource,datum=vec((length/2,0.,0.)),capacityKg=mass))
                laws.append(dict(store=label,law='ProportionalSpatial',densityKgM3=density,innerRadiusM=ri,outerRadiusM=ro,lengthM=span,provenance=PROVENANCE+'; proportional co-moving removal; no slosh'))
                rad=F(ri)**2+F(ro)**2
                tensor=diag([float(rad/2),float(rad/4+F(span)**2/12),float(rad/4+F(span)**2/12)])
                geometries.append(dict(store=label,usableVolumeM3=.32*multiplier,inertiaPerKg=matrix(tensor)))
                services.port('store.'+label,'Propellant',resource,store=label)
                for target in ['aft']+sockets:
                    services.routes.append(dict(id='store.'+label+'--'+target,**{'from':'store.'+label,'to':target+'.'+label},bidirectional=False))
            for target in ['aft']+sockets:
                for service in ['power','data']: services.wire('fore.'+service,target+'.'+service)
        elif kind=='adapter':
            # Effective ring/frame/leg distributions remain one spacecraft definition.
            part('ring',20.,(-.3,0.,0.),'annulus',(.1,.25,.2))
            for s in (-1,1):
                part('beam-y-'+str(len(regions)),5.,(-.2,0.,s*.95),'box',(.4,2.,.1))
                part('beam-z-'+str(len(regions)),5.,(-.2,s*.95,0.),'box',(.4,.1,1.8))
            c,s=math.cos(math.pi/8),math.sin(math.pi/8); reach=.95/c; inside=.22
            for j,(cy,cz) in enumerate([(c,s),(-s,c),(-c,-s),(s,-c)]):
                rotation=[1.,0.,0.,0.,cy,-cz,0.,cz,cy]
                part('brace-'+str(j),5.,(-.2,cy*(inside+reach)/2,cz*(inside+reach)/2),'box',(.4,reach-inside,.1),rotation)
            for y in (-.95,.95):
                for z in (-.95,.95):
                    name='leg-'+str(len(supports)); part(name,10.,(-1.1,y,z),'box',(1.4,.08,.08))
                    supports.append(dict(id='foot-'+str(len(supports)),frame=pose((-1.8,y,z)),halfWidthY=.06,halfWidthZ=.06,maximumLoadN=15000.,massRegion=name,provenance=PROVENANCE+'; integral fixed foot load path'))
            end('fore',(0.,0.,0.),I,plug=True,propellant=True); end('engine',(-.4,0.,0.),MATE,'NC-1',propellant=True)
            for s in ['a','b','power','data']: services.wire('fore.'+s,'engine.'+s)
            # Ring hollow is represented by separate circumferential convex wedges.
            for j in range(8):
                angles=[j*math.pi/4,(j+1)*math.pi/4]
                vertices=[[x,r*math.cos(a),r*math.sin(a)] for x in (-.4,-.2) for r in (.1,.25/math.cos(math.pi/8)) for a in angles]
                collision.append(volume('ring-'+str(j),vertices))
            for s in shapes[1:]: collision.append(volume(s['id'],rotated(box_vertices(s['center'],s['dimensions']),s['center'],s.get('rotation',I))))
            # Foot pads are included in each leg's effective 10 kg distribution, not extra mass.
            for foot in supports:
                p=list(foot['frame']['position'].values()); p[0]+=.02
                shape=dict(id=foot['id'],center=p,kind='box',dimensions=(.04,.12,.12)); shapes.append(shape)
                collision.append(volume(foot['id'],box_vertices(p,shape['dimensions'])))
        elif kind=='engine':
            part('engine',100.,(-.45,0.,0.),'annulus',(0.,.25,.9))
            # Fixed effective dry law follows canonical rigid-body gimbal convention.
            shapes=[dict(id='flange',center=(-.01,0.,0.),kind='annulus',dimensions=(0.,.12,.02)),dict(id='neck',center=(-.04,0.,0.),kind='annulus',dimensions=(0.,.08,.08)),dict(id='head',center=(-.09,0.,0.),kind='annulus',dimensions=(0.,.25,.02),moving=True),dict(id='bell',center=(-.50,0.,0.),kind='annulus',dimensions=(.20,.25,.8),moving=True)]
            end('fore',(0.,0.,0.),I,'NC-1',plug=True,propellant=True); load(10.)
            consumers=[consumer('main',(0.,0.,0.),(1.,0.,0.),30720.,'fore',True)]
            subparts=[dict(id='gimbal',parent=None,pose=pose(),visualNode='MODULE_gimbal',kind='Gimbal',massRegions=[],actuators=['main'])]
            collision.append(volume('neck',prism_vertices(-.08,0.,.08/math.cos(math.pi/8))))
            collision.append(volume('flange',prism_vertices(-.02,0.,.12/math.cos(math.pi/8))))
            # Whole two-axis swept bell conservative hull; swept physical profile, not mesh inference.
            tilt=math.acos(math.cos(.05)**2); r=.9*math.sin(tilt)+.25; bottom=-math.sqrt(.9**2+.25**2)
            clearance.append(volume('gimbal-sweep',prism_vertices(bottom,-.08*math.cos(tilt)+.25*math.sin(tilt),r/math.cos(math.pi/8))))
            # Engine hard body uses the same conservative sweep for future collision preparation.
            collision.append(volume('bell-sweep',prism_vertices(bottom,-.08*math.cos(tilt)+.25*math.sin(tilt),r/math.cos(math.pi/8))))
            beta=tilt+math.pi/60
            clearance.append(volume('main-exhaust',prism_vertices(-1.53,-.88,(.25/math.cos(beta)+1.53*math.tan(beta))/math.cos(math.pi/8)),PROVENANCE+'; radius >= .25/cos(tilt+3deg)+distance*tan(tilt+3deg), through foot plane; no thermal/plume certification'))
        else:
            part('block',8.,(0.,.15,0.),'box',(.25,.3,.2)); end('mount',(0.,0.,0.),[0.,1.,0.,-1.,0.,0.,0.,0.,1.],'R-1',True,True,True); load(2.)
            collision.append(volume('body',box_vertices((0.,.15,0.),(.25,.3,.2))))
            for name,p,axis in [('axial-positive',(-.145,.15,0.),(1.,0.,0.)),('axial-negative',(.145,.15,0.),(-1.,0.,0.)),('tangent-positive',(0.,.15,-.12),(0.,0.,1.)),('tangent-negative',(0.,.15,.12),(0.,0.,-1.))]:
                consumers.append(consumer(name,p,axis,15.,'mount'))
                center=[p[i]+axis[i]*.01 for i in range(3)]
                rotation=I if axis[0] else [0.,0.,-1.,0.,1.,0.,1.,0.,0.]
                shapes.append(dict(id='exit-'+name,center=center,kind='annulus',dimensions=(.01,.015,.02),rotation=rotation,material=1))
                vertices=rotated(prism_vertices(-.01,.01,.015/math.cos(math.pi/8)),(0.,0.,0.),rotation)
                collision.append(volume('nozzle-'+name,[[v[i]+center[i] for i in range(3)] for v in vertices]))
                # Ordinary RCS attachment checks solid body/nozzle hardware. Exhaust
                # is actuator behavior, not an expanding editor placement obstacle.
                # Current KSA 5482 ToSurface placement has the same responsibility split.
        if consumers:
            inlet='fore' if kind=='engine' else 'mount'; services.wire(inlet+'.power','controller.power')
            for c in consumers:
                for label,res in [('a',A),('b',B)]:
                    services.port(c['id']+'.'+label,'Propellant',res,consumer=c['id']); services.wire(inlet+'.'+label,c['id']+'.'+label)
                services.port(c['id']+'.data','Data',consumer=c['id']); services.wire(inlet+'.data',c['id']+'.data')
        attachments=[a[0] for a in endpoints]; assetId='nc.greybox.'+kind
        assetBytes,nodes=glb(assetId,shapes,attachments,kind=='engine'); outputs[kind+'.glb']=assetBytes
        physical=dict(schema='novacore.greybox-analytic-source/1',definition=id,provenance=PROVENANCE,primitives=primitives,
                      assumptions=['Effective dry distributions are fixed in the material frame; gimbal motion changes actuator direction and visual geometry only.',
                                   'No plume heating/aerodynamics/stress certification; support load and geometric keep-out qualification only.'])
        if kind=='block':
            physical['assumptions'].append('RCS placement uses authored mount and solid body/nozzle collision volumes; exhaust location/direction remain actuator facts. No expanding plume placement veto or plume impingement certification.')
        physicsBytes=encoded(physical); outputs[kind+'.physics.json']=physicsBytes
        mass,com,tensor=aggregate(regions)
        construction=dict(name=names[kind],development=True,asset=dict(id=assetId,revision=1,sha256=sha(assetBytes),relativePath=kind+'.glb',units='m',basis='gltf=(E.Y,-E.Z,-E.X)',materialOrigin=vec((0.,0.,0.)),requiredNodes=nodes,provenance=PROVENANCE),
                          physicalSource=dict(id=id+'.physics',revision=2 if kind=='block' else 1,sha256=sha(physicsBytes),provenance=kind+'.physics.json; '+PROVENANCE),massRegions=regions,storeGeometry=geometries,subparts=subparts,consumers=consumers,
                          interfaces=[a[2] for a in endpoints],electrical=electrical,command=kind=='core',unqualifiedHardware=[])
        standard=dict(schema='novacore.part-standard/1',rootEligible=kind=='core',category='Propulsion' if consumers else 'Structure and systems',purpose=names[kind]+' for the supported ascent development slice',mechanical=[a[1] for a in endpoints],socketGroups=groups,
                      collision=collision,clearance=clearance,support=supports,storeLaws=laws,ports=services.ports,routes=services.routes,requiredCommandLoads=['controller'] if electrical else [],
                      configuration=dict(fillStores=bool(stores),chargeBattery=kind=='core',enableStores=bool(stores),enableElectrical=bool(electrical)))
        definitions.append(dict(id=id,revision=2 if kind=='block' else 1,visualReference=assetId,role='Component',dryMassKg=mass,localCom=vec(com),localInertia=matrix(tensor),attachments=attachments,stores=stores,propulsion=None,gimbal=None,construction=construction,standard=standard))
    outputs['catalog.json']=encoded(dict(schema='novacore.construction-catalog/2',resources=[dict(id=A,revision=1,name='Development propellant A'),dict(id=B,revision=1,name='Development propellant B')],definitions=definitions))
    outputs['content-provenance.json']=encoded(dict(schema='novacore.original-greybox-provenance/1',generator='tools/vehicle-construction/author-modular-starter.py',source='Original NovaCore accepted planning inputs and analytic authoring',ksaInputs=0,blenderFiles=0,
        files={p:dict(bytes=len(data),sha256=sha(data)) for p,data in sorted(outputs.items())}))
    return outputs


if __name__=='__main__':
    parser=argparse.ArgumentParser(); parser.add_argument('--check',action='store_true'); args=parser.parse_args()
    outputs=author()
    if args.check:
        for name,data in outputs.items():
            if not (ROOT/name).is_file() or (ROOT/name).read_bytes()!=data: raise SystemExit('Authoring mismatch: '+name)
        print(f'Authoring reproducibility PASS: {len(outputs)} files, {sum(map(len,outputs.values()))} bytes')
    else:
        ROOT.mkdir(parents=True,exist_ok=True)
        for name,data in outputs.items(): (ROOT/name).write_bytes(data)
        print(f'Authored six greybox definitions: {len(outputs)} files, {sum(map(len,outputs.values()))} bytes')
