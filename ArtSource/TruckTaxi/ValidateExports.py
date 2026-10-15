"""Actual Blender FBX round-trip checks; not Unity or driving acceptance."""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

root=Path(__file__).resolve().parent
results=[]
for path in sorted((root/'Exports').glob('*.fbx')):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(path))
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    bounds=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
    low=[min(v[i] for v in bounds) for i in range(3)]
    high=[max(v[i] for v in bounds) for i in range(3)]
    errors=[]
    if not meshes: errors.append('no meshes')
    for o in meshes:
        if 'REFERENCE' in o.name: errors.append('vendor reference leaked into original body export')
        if not o.data.uv_layers: errors.append(o.name+': missing UVs')
        if not o.data.materials or any(m is None for m in o.data.materials): errors.append(o.name+': missing material')
        if min(abs(v) for v in o.scale)<.0001: errors.append(o.name+': singular scale')
        if any(not math.isfinite(value) for row in o.matrix_world for value in row): errors.append(o.name+': non-finite transform')
        if any(not math.isfinite(value) for v in o.data.vertices for value in v.co): errors.append(o.name+': non-finite vertex')
        if any(not math.isfinite(value) for p in o.data.polygons for value in p.normal): errors.append(o.name+': non-finite normal')
        if any(p.area<1e-12 for p in o.data.polygons): errors.append(o.name+': degenerate face')
    names=[o.name for o in meshes]
    if '_LOD' not in path.stem:
        for group in ('CabExterior','CabInterior','Chassis','SteeringWheel','GaugeSpeed','GaugeRpm','Wheel_FL','Wheel_FR','Wheel_RL','Wheel_RR','MirrorLeft','MirrorRight','LampHeadlamp','LampBrake'):
            if not any(n.startswith(group+'__') for n in names): errors.append('missing required binding group '+group)
    elif any(n.startswith(('CabInterior__','SteeringWheel__','GaugeSpeed__','GaugeRpm__')) for n in names):
        errors.append('distant LOD retains unnecessary instrument/interior meshes')
    result=dict(file=path.name,meshes=len(meshes),triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes),
        blenderBoundsMin=low,blenderBoundsMax=high,dimensions=[high[i]-low[i] for i in range(3)],errors=errors,
        status='PASS' if not errors else 'FAIL')
    results.append(result)
    print('FBX_ROUND_TRIP '+json.dumps(result))
(root/'ExportValidation.json').write_text(json.dumps(results,indent=2))
if len(results)!=15 or any(r['errors'] for r in results):
    raise RuntimeError('Export acceptance failed; inspect ExportValidation.json')
print('FIFTEEN_FBX_ROUND_TRIPS_PASS')
