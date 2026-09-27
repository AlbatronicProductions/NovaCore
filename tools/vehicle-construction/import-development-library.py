"""Offline admission of accepted NovaCore-owned development content. Never runs Blender.

No runtime dependency on engineering Python, authoring files, or asset-side DTOs.
The output is ordinary generic construction data; DLV names belong only to content.
"""
import argparse
import hashlib
import importlib.util
import json
import math
import pathlib


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def vector(v):
    return dict(zip(('x', 'y', 'z'), v))


def matrix(m):
    return dict(zip('abcdefghi', (x for row in m for x in row)))


def tensor_matrix(m):
    # The analytical tensor is symmetric; canonicalize multiplication roundoff.
    assert max(abs(m[i][j]-m[j][i]) for i in range(3) for j in range(3)) < 1e-8
    return matrix([[(m[i][j]+m[j][i])/2 for j in range(3)] for i in range(3)])


def transpose(a):
    return list(map(list, zip(*a)))


def multiply(a, b):
    return [[sum(a[i][k]*b[k][j] for k in range(3)) for j in range(3)] for i in range(3)]


def apply(a, v):
    return [sum(a[i][j]*v[j] for j in range(3)) for i in range(3)]


def subtract(a, b):
    return [x-y for x, y in zip(a, b)]


def diagonal(d):
    return [[d[i] if i == j else 0.0 for j in range(3)] for i in range(3)]


IDENTITY = diagonal([1., 1., 1.])


def rigid(m):
    # Remove only FP32 transport error in an accepted proper orthogonal frame.
    a = [m[i][0] for i in range(3)]
    norm = math.sqrt(sum(x*x for x in a))
    a = [x/norm for x in a]
    b = [m[i][1] for i in range(3)]
    dot = sum(x*y for x, y in zip(a, b))
    b = [b[i]-dot*a[i] for i in range(3)]
    norm = math.sqrt(sum(x*x for x in b))
    b = [x/norm for x in b]
    c = [a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0]]
    result = transpose([a, b, c])
    assert max(abs(result[i][j]-m[i][j]) for i in range(3) for j in range(3)) < 1e-6
    return result


def pose(m):
    return dict(position=vector([m[i][3] for i in range(3)]), rotation=matrix(rigid(m)))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--accepted', type=pathlib.Path, required=True)
    parser.add_argument('--engineering', type=pathlib.Path, required=True)
    parser.add_argument('--asset-root', type=pathlib.Path, required=True)
    parser.add_argument('--out', type=pathlib.Path, required=True)
    args = parser.parse_args()
    asset_defs = read(args.accepted/'definitions.json')
    assembly = read(args.accepted/'DLV-B1-R1.assembly.json')
    assignment = read(args.accepted/'physical-authority-reference-map.json')
    mass_input = read(args.engineering/'stage2-mass-regions.json')
    propellant = read(args.engineering/'stage1-propellant.json')
    tanks = read(args.engineering/'stage2-tank-layout.json')
    engine_input = read(args.engineering/'stage2-engine-layout.json')
    spec = importlib.util.spec_from_file_location('accepted_engineering', args.engineering/'calculate.py')
    engineering = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(engineering)
    physical_files = ['stage2-mass-regions.json', 'stage1-propellant.json', 'stage2-tank-layout.json', 'stage2-engine-layout.json', 'calculate.py']
    physical_seal = {name: sha(args.engineering/name) for name in physical_files}
    physical_hash = hashlib.sha256(json.dumps(physical_seal, sort_keys=True).encode()).hexdigest()
    expected_assets = read(args.accepted/'reproduction.json')['GLBs']
    definitions = []
    instances = []
    provenance = []
    for old_id, visual in sorted(asset_defs.items()):
        logical = old_id.removesuffix('.r1')
        example = next(i for i in assembly['instances'] if i['definition'] == old_id)
        # Origins are engineering material origins; authored exports are a transport witness.
        old_pose = example['pose_E']
        rotation = rigid(old_pose)
        stage = 'Booster' if 'booster' in old_id else 'Upper' if 'upper' in old_id else 'Capsule'
        if 'load-ring' in old_id:
            stage = 'Booster'
        elif 'capsule-collar' in old_id:
            stage = 'Upper'
        if '.Engine.' in example['id']:
            engine = next(x for x in engine_input['launch_engines'] if x['id'] == example['id'])
            local_origin = engine['force_point_local_m']
        elif 'load-ring' in old_id:
            local_origin = [31.667, 0, 0]
        elif 'capsule-collar' in old_id:
            local_origin = [43.664-31.667, 0, 0]
        else:
            local_origin = [0, 0, 0]
        inv = transpose(rotation)
        def local(v):
            return apply(inv, subtract(v, local_origin))
        selected = {x['record'] for x in assignment if x['proposed_part_definition'] == old_id}
        if 'booster-engine' in old_id:
            selected = {x for x in selected if 'Engine.0.' in x}
        regions = []
        for r in mass_input['dry_regions']:
            if r['id'] not in selected:
                continue
            tensor = engineering.tensor_rotate(engineering.shape_diag(r), r['orientation_quaternion_xyzw'])
            tensor = multiply(multiply(inv, tensor), rotation)
            regions.append(dict(id=r['id'], massKg=r['dry_mass_kg'], com=vector(local(r['centroid_local_m'])), inertiaAtCom=tensor_matrix(tensor)))
        regions.sort(key=lambda x: x['id'])
        total = sum(x['massKg'] for x in regions)
        center = [sum(r['massKg']*r['com'][k] for r in regions)/total for k in ('x', 'y', 'z')]
        tensor = [[0.0]*3 for _ in range(3)]
        for r in regions:
            q = subtract(list(r['com'].values()), center)
            norm = sum(x*x for x in q)
            own = list(r['inertiaAtCom'].values())
            for i in range(3):
                for j in range(3):
                    tensor[i][j] += own[i*3+j]+r['massKg']*((norm if i == j else 0)-q[i]*q[j])
        stores = []
        geometry = []
        for r in propellant['full_loaded_regions']:
            if r['id'] not in selected:
                continue
            # These are the admitted loaded-region capacities, not future refill maxima.
            stores.append(dict(id=r['id'], species=r['resource'], resourceIdentity='nc.resource.'+r['resource'], datum=vector(local(r['centroid_local_m'])), capacityKg=r['mass_kg']))
            if r['resource'] != 'He':
                volume = r['mass_kg']/propellant['density_kg_m3'][r['resource']]
            elif r['id'].endswith('.Gas'):
                volume = next(x['internal_volume_m3'] for x in tanks['helium_COPVs'] if x['id']+'.Gas' == r['id'])
            else:
                tank_id = r['id'].removesuffix('.HeUllage')
                tank = next(x for x in tanks['liquid_tanks'] if x['id'] == tank_id)
                liquid = next(x for x in propellant['full_loaded_regions'] if x['id'] == tank_id+'.Liquid')
                volume = tank['capacity_m3']-liquid['mass_kg']/propellant['density_kg_m3'][liquid['resource']]
            inertia = r.get('specific_inertia_tensor', diagonal(r['specific_inertia_diag']))
            geometry.append(dict(store=r['id'], usableVolumeM3=volume, inertiaPerKg=tensor_matrix(multiply(multiply(inv, inertia), rotation))))
        consumers = []
        subparts = []
        for actuator in engine_input['actuators']:
            owned = actuator['id'] == example['id'] if '.Engine.' in example['id'] else actuator['stage'] == stage and actuator['type'] != 'launch_main' and ('body' in old_id or 'capsule.return' in old_id)
            if not owned:
                continue
            kind = actuator['type']
            launch = next((e for e in engine_input['launch_engines'] if e['id'] == actuator['id']), None)
            if launch:
                thrust = launch['vacuum_thrust_N']; isp = launch['Isp_vacuum_s']; weights = [('RP1', 5), ('LOX', 13)]
            elif kind == 'helium_cold_gas':
                thrust = actuator['vacuum_thrust_N']; isp = actuator['Isp_budget_s']; weights = [('He', 1)]
            else:
                thrust = actuator.get('thrust_N', actuator.get('vacuum_thrust_N'))
                # Bind the matching physical operating point, not the lower mission-budget Isp.
                isp = actuator['Isp_SL_s'] if kind == 'terminal_SL' else actuator['Isp_vacuum_s']
                weights = [('MMH', 20), ('NTO', 33)]
            consumer_id = 'Main' if launch else actuator['id']
            subpart = 'Engine' if launch else ('Pod.'+actuator['id'].split('.')[2] if stage == 'Capsule' and kind != 'bipropellant_RCS' else 'Pod.'+actuator['id'].split('.')[2] if kind == 'bipropellant_RCS' else None)
            consumers.append(dict(id=consumer_id, subpart=subpart, model='nc.development.'+kind, totalFlowKgS=thrust/(isp*engineering.G0),
                mixture=[dict(resource='nc.resource.'+r, weight=w) for r, w in weights],
                feedStores=[] if launch else [s['id'] for s in stores if s['species'] in dict(weights) and ('Feed.' not in s['id']) and (not s['id'].endswith('.HeUllage'))],
                feedInterfaces=['ENGINE_MOUNT'] if launch else [], flowRule='FarthestFirst' if launch else 'NearestFirst',
                forcePoint=vector(local(actuator['force_point_local_m'])), axis=vector(apply(inv, actuator['force_direction'])), thrustN=thrust,
                gimbal=dict(id='Gimbal', pivot=vector([0,0,0]), nozzleOffset=vector(local(launch['nozzle_exit_local_m'])), limitY=math.radians(launch['gimbal_half_angle_deg']), limitZ=math.radians(launch['gimbal_half_angle_deg']), slewRate=0) if launch else None))
        nodes = ['ROOT']
        if '.Engine.' in example['id']:
            nodes += ['Engine', 'Engine.Mount']
            subparts.append(dict(id='Engine', parent=None, pose=dict(position=vector([0,0,0]),rotation=matrix(IDENTITY)), visualNode='Engine', kind='Gimbal', massRegions=[r['id'] for r in regions], actuators=['Main']))
        if 'capsule.return' in old_id:
            for i in range(4):
                node = 'Capsule.Pod.'+str(i); nodes.append(node)
                subparts.append(dict(id='Pod.'+str(i), parent=None, pose=pose(visual['nodes'][node]['local']), visualNode=node, kind='Physical', massRegions=[], actuators=[a['id'] for a in consumers if a['subpart'] == 'Pod.'+str(i)]))
                # Leg geometry is currently an excluded authoring reservation.
                # Keep its physical region; do not turn a mount EMPTY into a visual leg.
        asset_file = args.asset_root/'Library'/(old_id+'.glb')
        asset_hash = sha(asset_file)
        assert asset_hash == expected_assets['Library\\'+old_id+'.glb']
        attachments = []
        capabilities = []
        for name, interface in sorted(visual['interfaces'].items()):
            attachments.append(dict(id=name, family=interface['compatibility_key'], frame=pose(interface['frame_E'])))
            engine_interface = name.startswith('ENGINE')
            detachable = name == 'TOP' or name == 'BOTTOM_RECEIVER'
            capabilities.append(dict(interface=name, services='Propellant, Data' if engine_interface else 'Data', detachable=detachable))
            nodes.append('SOCKET_'+name)
        def exported_node(local_name):
            return 'Part.'+old_id+('' if local_name == 'ROOT' else '.'+local_name)
        nodes = [exported_node(n) for n in nodes]
        for sub in subparts:
            sub['visualNode'] = exported_node(sub['visualNode'])
        definitions.append(dict(id=logical,revision=1,visualReference=visual['asset_id'].removesuffix('.r1'),role='Component',dryMassKg=total,localCom=vector(center),localInertia=tensor_matrix(tensor),attachments=attachments,stores=stores,propulsion=None,gimbal=None,
            construction=dict(name=logical,development=True,asset=dict(id=visual['asset_id'].removesuffix('.r1'),revision=1,sha256=asset_hash,relativePath='Library/'+old_id+'.glb',units='m',basis='gltf=(E.Y,-E.Z,-E.X)',materialOrigin=vector([0,0,0]),requiredNodes=nodes,provenance='accepted reusable-part asset digest '+visual['content_digest']),
                physicalSource=dict(id='nc.development.engineering.regions',revision=1,sha256=physical_hash,provenance='development-launch-stack-preblender constituent files; not mesh mass'),massRegions=regions,storeGeometry=geometry,subparts=subparts,consumers=consumers,interfaces=capabilities,electrical=[],command='capsule.return' in old_id,
                unqualifiedHardware=[r['id'] for r in regions if any(s in r['id'] for s in ('Battery','Avionics','Harness','Capsule.Leg.'))])))
        provenance.append(dict(definition=logical,sourceDefinition=old_id,sourceAssetDigest=visual['content_digest'],ownedRegions=sorted(selected)))
    output = dict(schema='novacore.construction-catalog/1', resources=[dict(id='nc.resource.'+r,revision=1,name=r) for r in ['He','LOX','MMH','NTO','RP1']], definitions=definitions)
    configurations = []
    for placed in assembly['instances']:
        transform = pose(placed['pose_E'])
        if placed['id'] in mass_input['stage_origins_x_m']:
            transform['position'] = vector([mass_input['stage_origins_x_m'][placed['id']],0,0])
        elif '.Engine.' in placed['id']:
            e = next(x for x in engine_input['launch_engines'] if x['id'] == placed['id'])
            p = list(e['force_point_local_m']); p[0] += mass_input['stage_origins_x_m'][e['stage']]
            transform['position'] = vector(p)
        elif 'BoosterUpper' in placed['id']:
            transform['position'] = vector([31.667,0,0])
        else:
            transform['position'] = vector([43.664,0,0])
        definition = next(d for d in definitions if d['id'] == placed['definition'].removesuffix('.r1'))
        instances.append(dict(id=placed['id'],order=placed['order'],definition=dict(id=definition['id'],revision=1,digest='AUTHOR'),pose=transform))
        configurations.append(dict(part=placed['id'],stores=[dict(store=s['id'],quantityKg=s['capacityKg'],enabled=('Feed.' not in s['id'] and not s['id'].endswith('.HeUllage') and not (placed['id']=='Booster' and s['species']=='He'))) for s in definition['stores']],electrical=[]))
    connections = [dict(parent=e['parent'],parentEndpoint=e['parent_interface'],child=e['child'],childEndpoint=e['child_interface'],construction=dict(id=e['id'],detachable=e['detachable'],services='Propellant, Data' if '.Engine.' in e['child'] else 'Data')) for e in assembly['connections']]
    # AUTHOR markers are consumed only by the explicit cold authoring command,
    # never by normal saved-design validation or runtime loading.
    draft = dict(schema='novacore.construction-authoring-template/1',id='nc.development.dlv-b1-r1',revision=1,dependencyDigest='AUTHOR',root='Booster',controlPart='Capsule',instances=instances,connections=connections,serviceLinks=[],configuration=configurations,symmetry=[],actions=[])
    args.out.mkdir(parents=True, exist_ok=True)
    (args.out/'development-catalog.json').write_text(json.dumps(output,indent=2,allow_nan=False)+'\n',encoding='utf-8')
    (args.out/'development-design.authoring.json').write_text(json.dumps(draft,indent=2,allow_nan=False)+'\n',encoding='utf-8')
    (args.out/'content-provenance.json').write_text(json.dumps(dict(physicalFiles=physical_seal,physicalHash=physical_hash,definitions=provenance,
        sourceAssemblyHash=sha(args.accepted/'DLV-B1-R1.assembly.json'),assetRootRequired='exported GLB library; no BLEND runtime dependency'),indent=2)+'\n',encoding='utf-8')
    print('Generated generic development catalog:', len(definitions), 'definitions;', sum(len(d['stores']) for d in definitions), 'store definitions')


if __name__ == '__main__':
    main()
