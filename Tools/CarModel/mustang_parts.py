import bpy,json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(root/'Backups/MustangSource/scene.gltf'))
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.context.scene.objects:
 if o.type=='MESH':o.select_set(True);bpy.context.view_layer.objects.active=o
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.separate(type='LOOSE');bpy.ops.object.mode_set(mode='OBJECT')
report=[]
for o in bpy.context.scene.objects:
 if o.type!='MESH':continue
 b=[o.matrix_world@Vector(v) for v in o.bound_box];lo=[min(v[i] for v in b) for i in range(3)];hi=[max(v[i] for v in b) for i in range(3)]
 if lo[0]>.05 and hi[0]<.7 and lo[1]>-.9 and hi[1]<.1 and lo[2]>.4:
  report.append(dict(name=o.name,lo=lo,hi=hi,materials=[m.name for m in o.data.materials if m],faces=len(o.data.polygons)))
(root/'Logs/mustang-steering-parts.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(root/'Backups/MustangSource/parts.blend'))
