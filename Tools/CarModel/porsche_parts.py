import bpy,json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(root/'Backups/PorscheSource/scene.gltf'))
bpy.data.objects['door_2'].matrix_basis = bpy.data.objects['door_1'].matrix_basis.copy()
bpy.context.view_layer.update()
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.context.scene.objects:
 if o.type=='MESH':o.select_set(True);bpy.context.view_layer.objects.active=o
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.separate(type='LOOSE');bpy.ops.object.mode_set(mode='OBJECT')
report=[]
for o in bpy.context.scene.objects:
 if o.type!='MESH':continue
 b=[o.matrix_world@Vector(v) for v in o.bound_box];lo=[min(v[i] for v in b) for i in range(3)];hi=[max(v[i] for v in b) for i in range(3)]
 if lo[0]>-2.4 and hi[0]<-1.8 and lo[1]>.08 and hi[1]<.65 and lo[2]>.35 and hi[2]<.95:
  report.append(dict(name=o.name,lo=lo,hi=hi,materials=[m.name for m in o.data.materials if m],faces=len(o.data.polygons)))
(root/'Logs/porsche-steering-parts.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(root/'Backups/PorscheSource/parts.blend'))
