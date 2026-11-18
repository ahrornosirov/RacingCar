import bpy,json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(root/'Backups/MustangSource/scene.gltf'))
report=[]
for o in bpy.context.scene.objects:
    if o.type!='MESH': continue
    bounds=[o.matrix_world@Vector(v) for v in o.bound_box]
    report.append(dict(name=o.name,position=list(o.matrix_world.translation),lo=[min(v[i] for v in bounds) for i in range(3)],hi=[max(v[i] for v in bounds) for i in range(3)],materials=[m.name for m in o.data.materials if m]))
materials=[]
for m in bpy.data.materials:
    materials.append(dict(name=m.name,nodes=[dict(type=n.type,name=n.name,image=n.image.filepath if n.type=='TEX_IMAGE' and n.image else '') for n in m.node_tree.nodes] if m.use_nodes else []))
(root/'Logs/mustang-inspection.json').write_text(json.dumps(dict(objects=report,materials=materials),indent=2))
