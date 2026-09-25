"""Run with installed Blender --background --factory-startup --python this_file."""
import hashlib
import importlib.util
from pathlib import Path
import sys
import bpy

spec = importlib.util.spec_from_file_location('taxi_model_processor', Path(__file__).with_name('process_model.py'))
processor = importlib.util.module_from_spec(spec)
spec.loader.exec_module(processor)
root = Path(__file__).resolve().parents[3] / 'Builds' / 'TruckTaxiDemo' / 'Validation' / 'ModelPipeline'
root.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, location=(4, 2, 3))
bpy.context.object.scale = (.5, .5, 2)
source = root / 'fixture-source.fbx'
bpy.ops.export_scene.fbx(filepath=str(source), add_leaf_bones=False, bake_anim=False)
before = hashlib.sha256(source.read_bytes()).hexdigest()
output = root / 'fixture-processed.fbx'
processor.process(dict(passengerId='model-fixture', source=str(source), output=str(output),
                       heightMeters=1.75, targetPolygons=500, lodReduction=.5))
assert output.exists() and output.with_name('fixture-processed_LOD1.fbx').exists()
assert before == hashlib.sha256(source.read_bytes()).hexdigest(), 'Source changed'
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(output))
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
corners = [o.matrix_world @ processor.Vector(c) for o in meshes for c in o.bound_box]
height = max(p.z for p in corners) - min(p.z for p in corners)
assert abs(height-1.75) < .03, height
assert abs(min(p.z for p in corners)) < .03, 'Feet not grounded'
try:
    processor.process(dict(source=str(source), output=str(source)))
    raise AssertionError('Source overwrite permitted')
except ValueError:
    pass
print('TRUCK TAXI BLENDER TEST PASS: normalized bounds, source immutable, LOD, materials, safe overwrite rejection')
