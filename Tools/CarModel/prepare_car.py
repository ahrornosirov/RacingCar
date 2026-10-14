"""Convert the downloaded CarConcept glTF asset to an editable Unity FBX.

Run with Blender --background --python Tools/CarModel/prepare_car.py.
Only imports geometry; no downloaded scripts are executed.
"""
import bpy
import json
from pathlib import Path
from mathutils import Vector

root = Path(__file__).resolve().parents[2]
folder = root / 'Assets/ThirdParty/CarConcept'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(folder / 'CarConcept.glb'))
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
details = []
for o in meshes:
    bounds = [o.matrix_world @ Vector(v) for v in o.bound_box]
    lo = [min(v[i] for v in bounds) for i in range(3)]
    hi = [max(v[i] for v in bounds) for i in range(3)]
    details.append({'name': o.name, 'boundsMin': lo, 'boundsMax': hi,
                    'triangles': sum(len(p.vertices)-2 for p in o.data.polygons),
                    'materials': [m.name for m in o.data.materials if m]})
print('MODEL_DETAILS', json.dumps(details))
# FBX preserves named nodes, wheel and steering pivots and both UV channels.
bpy.ops.export_scene.fbx(filepath=str(folder / 'CarConcept.fbx'), use_selection=False,
    object_types={'MESH','EMPTY'}, bake_anim=False, add_leaf_bones=False,
    axis_forward='-Z', axis_up='Y', path_mode='STRIP', use_mesh_modifiers=True)
(folder / 'GeometryReport.json').write_text(json.dumps(details, indent=2))
print('CAR_FBX_READY', len(meshes), 'meshes')
