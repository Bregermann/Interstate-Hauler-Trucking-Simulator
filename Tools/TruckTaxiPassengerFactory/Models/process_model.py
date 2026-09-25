"""Blender-only model preparation. Source files are never overwritten."""
import argparse
import json
from pathlib import Path
import sys

import bpy
import bmesh
from mathutils import Vector


def process(job):
    source = Path(job['source']).resolve()
    output = Path(job['output']).resolve()
    if source == output or output.suffix.lower() != '.fbx':
        raise ValueError('Processed output must be a separate FBX.')
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    suffix = source.suffix.lower()
    if suffix == '.fbx':
        bpy.ops.import_scene.fbx(filepath=str(source))
    elif suffix in ('.glb', '.gltf'):
        bpy.ops.import_scene.gltf(filepath=str(source))
    elif suffix == '.obj':
        bpy.ops.import_scene.obj(filepath=str(source))
    else:
        raise ValueError('Supported source formats: FBX, GLB/GLTF, OBJ.')
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    rigs = [obj for obj in bpy.context.scene.objects if obj.type == 'ARMATURE']
    if not meshes:
        raise ValueError('Model contains no mesh.')
    warnings = []
    for obj in list(bpy.context.scene.objects):
        if obj.type in ('LIGHT', 'CAMERA'):
            bpy.data.objects.remove(obj, do_unlink=True)
    for obj in meshes:
        mesh = bmesh.new()
        mesh.from_mesh(obj.data)
        bmesh.ops.delete(mesh, geom=[v for v in mesh.verts if not v.link_edges], context='VERTS')
        bmesh.ops.recalc_face_normals(mesh, faces=list(mesh.faces))
        mesh.to_mesh(obj.data)
        mesh.free()
        if not obj.data.uv_layers:
            warnings.append(obj.name + ': no UV layer; material-only fallback')
        if not obj.data.materials:
            material = bpy.data.materials.new('Taxi_Fallback')
            material.diffuse_color = (0.12, 0.55, 0.65, 1)
            obj.data.materials.append(material)
        if len(obj.data.materials) > 8:
            warnings.append(obj.name + ': more than eight materials; manual atlas recommended')
        for index, material in enumerate(obj.data.materials):
            if material:
                material.name = 'Taxi_' + job['passengerId'] + '_M' + str(index)
        if any(abs(value) < 0.000001 for value in obj.scale):
            raise ValueError(obj.name + ': zero scale')
    corners = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
    minimum = Vector(tuple(min(point[i] for point in corners) for i in range(3)))
    maximum = Vector(tuple(max(point[i] for point in corners) for i in range(3)))
    height = maximum.z - minimum.z
    if height <= 0.001:
        raise ValueError('Collapsed model bounds')
    scale = float(job.get('heightMeters', 1.75)) / height
    center = Vector(((minimum.x + maximum.x) / 2, (minimum.y + maximum.y) / 2, minimum.z))
    # Transform root objects together, preserving armature/mesh relationships.
    for obj in bpy.context.scene.objects:
        if obj.parent is None:
            obj.location = (obj.location - center) * scale
            obj.scale *= scale
    bpy.context.view_layer.update()
    if not rigs:
        warnings.append('No armature: generic/static fallback, NOT a valid humanoid')
        bpy.ops.object.select_all(action='DESELECT')
        for obj in meshes:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = meshes[0]
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
        if len(meshes) > 1:
            bpy.ops.object.join()
            meshes = [bpy.context.view_layer.objects.active]
        polygon_count = sum(len(obj.data.polygons) for obj in meshes)
        limit = max(500, int(job.get('targetPolygons', 12000)))
        if polygon_count > limit:
            for obj in meshes:
                modifier = obj.modifiers.new('Taxi_PolygonBudget', 'DECIMATE')
                modifier.ratio = limit / polygon_count
                bpy.context.view_layer.objects.active = obj
                bpy.ops.object.modifier_apply(modifier=modifier.name)
    else:
        for rig in rigs:
            if not rig.data.bones:
                raise ValueError('Empty armature: ' + rig.name)
        warnings.append('Existing bone names preserved; Unity humanoid mapping must validate')
    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=str(output), use_selection=True, add_leaf_bones=False,
                             axis_forward='-Z', axis_up='Y', bake_anim=False, path_mode='COPY', embed_textures=True)
    # Separate LOD file avoids rendering high/low copies simultaneously in Unity.
    lod_output = None
    if not rigs and float(job.get('lodReduction', .5)) > 0:
        for obj in meshes:
            modifier = obj.modifiers.new('Taxi_LOD1', 'DECIMATE')
            modifier.ratio = max(.1, 1 - float(job.get('lodReduction', .5)))
        lod_output = output.with_name(output.stem + '_LOD1.fbx')
        bpy.ops.export_scene.fbx(filepath=str(lod_output), use_selection=True, add_leaf_bones=False,
                                 axis_forward='-Z', axis_up='Y', bake_anim=False, path_mode='COPY', embed_textures=True)
    result = dict(status='processed', output=str(output), lod=str(lod_output) if lod_output else None,
                  armatures=len(rigs), meshes=len(meshes), warnings=warnings)
    output.with_suffix('.validation.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--job', type=Path, required=True)
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:])
    process(json.loads(args.job.read_text(encoding='utf-8-sig')))
