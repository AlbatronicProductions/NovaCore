"""Independent convex SAT audit of the authored six-part short/long assemblies.

Requires numpy; uses no generator helpers and never edits content. Projection
tolerance 1e-9 m treats shared mating planes as contact, not positive overlap.
All convex face normals and all edge-pair cross products are covered. Extra
diagonals are harmless separating axes, not a mesh-derived physics authority.
"""
import argparse
from itertools import combinations
import json
import math
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[2]
TOL=1e-9


def vectors(vertices):
    return np.array([[v['x'],v['y'],v['z']] for v in vertices],dtype=float)


def directions(values):
    lengths=np.linalg.norm(values,axis=1); values=values[lengths>1e-12]/lengths[lengths>1e-12,None]
    if len(values)==0: return np.empty((0,3))
    # Sign and approximate deduplication only reduce equivalent direction work.
    # Axis set is separately checked by manufactured intersect/separate cases.
    return np.unique(np.round(values,12),axis=0)


def hull(points):
    normals=[]
    for a,b,c in combinations(range(len(points)),3):
        normal=np.cross(points[b]-points[a],points[c]-points[a]); length=np.linalg.norm(normal)
        if length<1e-14: continue
        normal/=length; side=(points-points[a])@normal
        if side.max()<1e-10 or side.min()>-1e-10: normals.append(normal)
    edges=directions(np.array([points[b]-points[a] for a,b in combinations(range(len(points)),2)]))
    return points,directions(np.array(normals)),edges


def transformed(local,rotation,position):
    p,n,e=local
    return p@rotation.T+position,n@rotation.T,e@rotation.T


def overlap(a,b):
    pa,na,ea=a; pb,nb,eb=b
    if np.any(pa.max(0)-pb.min(0)<=TOL) or np.any(pb.max(0)-pa.min(0)<=TOL): return False
    def separates(axes):
        if len(axes)==0: return False
        aa=pa@axes.T; bb=pb@axes.T
        return bool(np.any(aa.max(0)-bb.min(0)<=TOL) or np.any(bb.max(0)-aa.min(0)<=TOL))
    if separates(np.concatenate((na,nb))): return False
    cross=np.cross(ea[:,None,:],eb[None,:,:]).reshape(-1,3)
    return not separates(directions(cross))


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--output'); args=parser.parse_args()
    catalog=json.loads((ROOT/'assets/vehicles/modular-starter/catalog.json').read_text())
    definitions={p['id']:p for p in catalog['definitions']}
    local={id:{kind:[(v['id'],hull(vectors(v['vertices']))) for v in d['standard'][kind]] for kind in ('collision','clearance')} for id,d in definitions.items()}
    results=[]
    # Manufactured SAT witnesses: intersection, face contact, tiny positive overlap,
    # oblique separation despite overlapping world AABBs, and oblique intersection.
    box=np.array([(x,y,z) for x in (-1.,1.) for y in (-.1,.1) for z in (-.1,.1)])
    a=hull(box); assert overlap(a,a)
    assert overlap(a,hull(box*np.array([1e-10,.1,.1])))
    assert not overlap(a,transformed(a,np.eye(3),np.array([2.,0.,0.])))
    assert overlap(a,transformed(a,np.eye(3),np.array([2.-1e-7,0.,0.])))
    c=math.sqrt(.5); r=np.array([[c,-c,0.],[c,c,0.],[0.,0.,1.]])
    rotated=transformed(a,r,np.zeros(3)); assert not overlap(rotated,transformed(a,r,np.array([-.3*c,.3*c,0.])))
    assert overlap(rotated,transformed(a,r,np.array([-.1*c,.1*c,0.])))
    for longer,tanks in ((False,1),(True,1),(False,3)):
        length=3. if longer else 1.5
        instances=[('core','nc.core.command-2',np.zeros(3),np.eye(3)),
                   ('tank','nc.tank.long-2' if longer else 'nc.tank.short-2',np.array([-length,0.,0.]),np.eye(3)),
                   ('adapter','nc.mount.single-2to1',np.array([-length*tanks,0.,0.]),np.eye(3)),
                   ('engine','nc.engine.main-1',np.array([-length*tanks-.4,0.,0.]),np.eye(3))]
        for j in range(1,tanks):
            instances.append(('tank-'+str(j),'nc.tank.short-2',np.array([-length*(j+1),0.,0.]),np.eye(3)))
        for j in range(tanks):
            for i in range(8):
                c,s=math.cos(i*math.pi/4),math.sin(i*math.pi/4)
                rotation=np.array([[1.,0.,0.],[0.,c,-s],[0.,s,c]])
                instances.append((f'block-{j}-{i}','nc.rcs.block-r1',np.array([-length*(j+.5),.6*c,.6*s]),rotation))
        shapes={kind:[(name,shape,transformed(h,r,p)) for name,id,p,r in instances for shape,h in local[id][kind]] for kind in ('collision','clearance')}
        violations=[]; physical=0; keepouts=0
        for a,b in combinations(shapes['collision'],2):
            if a[0]==b[0]: continue
            physical+=1
            if overlap(a[2],b[2]): violations.append(['collision',a[:2],b[:2]])
        for a in shapes['clearance']:
            for b in shapes['collision']:
                if a[0]==b[0]: continue
                keepouts+=1
                if overlap(a[2],b[2]): violations.append(['clearance',a[:2],b[:2]])
        # Solid nozzle obstruction remains a physical collision. The provisional
        # expanding RCS plume is not a placement volume; main clearance is retained.
        nozzle=next(x[2] for x in shapes['collision'] if x[0].startswith('block') and x[1].startswith('nozzle-'))
        blocker=hull(np.array([(x,y,z) for x in (-.01,.01) for y in (-.01,.01) for z in (-.01,.01)]))
        assert overlap(nozzle,transformed(blocker,np.eye(3),nozzle[0].mean(0)))
        results.append(dict(craft='three-short' if tanks==3 else 'long' if longer else 'short',collisionShapes=len(shapes['collision']),clearanceShapes=len(shapes['clearance']),physicalPairs=physical,clearancePairs=keepouts,violations=violations))
    result=dict(schema='novacore.modular-geometry-audit/1',method='independent convex SAT; solid body/nozzle collision and retained main-engine clearance; no thermal/plume certification',toleranceM=TOL,satManufacturedChecks=9,results=results)
    output=json.dumps(result,indent=2)
    if args.output:
        p=ROOT/args.output;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(output+'\n')
    print(output)
    if any(r['violations'] for r in results): raise SystemExit(1)


if __name__=='__main__': main()
