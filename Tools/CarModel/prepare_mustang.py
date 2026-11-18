"""Prepare _the.render.box_'s CC BY Mustang for Unity. Run mustang_parts.py first."""
import bpy,json
from pathlib import Path
from mathutils import Vector,Matrix
root=Path(__file__).resolve().parents[2]
folder=root/'Assets/ThirdParty/Mustang';folder.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(root/'Backups/MustangSource/parts.blend'),use_scripts=False)
groups={}
for o in list(bpy.context.scene.objects):
 if o.type!='MESH':continue
 bounds=[o.matrix_world@Vector(v) for v in o.bound_box];lo=[min(v[i] for v in bounds) for i in range(3)];hi=[max(v[i] for v in bounds) for i in range(3)]
 mat=o.data.materials[0].name
 if mat in ['Wheel1A','Wheel2A']:
  c=(Vector(lo)+Vector(hi))*.5
  group=('WheelF' if c.y<0 else 'WheelR')+('L' if c.x>0 else 'R')
 elif lo[0]>.11 and hi[0]<.56 and lo[1]>-.31 and hi[1]<-.10 and lo[2]>.55 and hi[2]<1:
  group='SteeringWheel'
 else:group='Body_'+mat
 # Bake all source transforms so each group has a simple local pivot.
 o.data.transform(o.matrix_world);o.matrix_world=Matrix.Identity(4)
 groups.setdefault(group,[]).append(o)
for group,objects in groups.items():
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=group
 bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY',center='BOUNDS')
materials=[]
for m in bpy.data.materials:
 bsdf=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None) if m.use_nodes else None
 materials.append(dict(name=m.name,color=list(bsdf.inputs['Base Color'].default_value) if bsdf else list(m.diffuse_color),metallic=bsdf.inputs['Metallic'].default_value if bsdf else 0,roughness=bsdf.inputs['Roughness'].default_value if bsdf else .5,baseMap='',normalMap=''))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(folder/'Mustang.fbx'),object_types={'MESH'},bake_anim=False,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
(folder/'UnityMaterials.json').write_text(json.dumps(dict(materials=materials),indent=2))
print('MUSTANG_READY',len(groups),'groups')
