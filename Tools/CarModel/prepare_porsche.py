"""Adapt n.brizitskaya's CC BY Porsche. Run porsche_parts.py first."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector,Matrix
root=Path(__file__).resolve().parents[2];source=root/'Backups/PorscheSource'
folder=root/'Assets/ThirdParty/Porsche';(folder/'Textures').mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source/'parts.blend'),use_scripts=False)
# Source faces +X with the axles offset from the origin. Normalize to Blender -Y forward.
normalize=Matrix.Rotation(-math.pi/2,4,'Z')@Matrix.Translation((2.27,0,.16))
groups={}
for o in list(bpy.context.scene.objects):
 if o.type!='MESH':continue
 b=[o.matrix_world@Vector(v) for v in o.bound_box];lo=[min(v[i] for v in b) for i in range(3)];hi=[max(v[i] for v in b) for i in range(3)]
 if o.name.startswith(('wheel_rim_','AO_tire_main_','discs_Discs_')):
  c=(Vector(lo)+Vector(hi))*.5;group=('WheelF' if c.x>-2.27 else 'WheelR')+('L' if c.y>0 else 'R')
 elif lo[0]>-2.12 and hi[0]<-1.94 and lo[1]>.14 and hi[1]<.54 and lo[2]>.45 and hi[2]<.83:
  group='SteeringWheel'
 else:group='Body_'+o.data.materials[0].name
 o.data.transform(normalize@o.matrix_world);o.parent=None;o.matrix_world=Matrix.Identity(4)
 groups.setdefault(group,[]).append(o)
for group,objects in groups.items():
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=group
 bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY',center='BOUNDS')
gltf=json.loads((source/'scene.gltf').read_text());cache={}
def texture(info):
 if not info:return ''
 image=gltf['images'][gltf['textures'][info['index']]['source']]['uri'];path=source/image
 if image not in cache:
  img=bpy.data.images.load(str(path),check_existing=True)
  w,h=img.size
  if max(w,h)>1024:img.scale(max(1,round(w*1024/max(w,h))),max(1,round(h*1024/max(w,h))))
  filename=path.stem+'.png';img.filepath_raw=str(folder/'Textures'/filename);img.file_format='PNG';img.save();cache[image]=filename
 return cache[image]
materials=[]
for m in gltf['materials']:
 p=m.get('pbrMetallicRoughness',{})
 materials.append(dict(name=m['name'],color=p.get('baseColorFactor',[1,1,1,1]),metallic=p.get('metallicFactor',1),roughness=p.get('roughnessFactor',1),baseMap=texture(p.get('baseColorTexture')),normalMap=texture(m.get('normalTexture')),metalRoughMap=texture(p.get('metallicRoughnessTexture')),emissionMap=texture(m.get('emissiveTexture')),emission=m.get('emissiveFactor',[0,0,0]),alphaMode=m.get('alphaMode','OPAQUE')))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(folder/'Porsche.fbx'),object_types={'MESH'},bake_anim=False,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
(folder/'UnityMaterials.json').write_text(json.dumps(dict(materials=materials),indent=2))
print('PORSCHE_READY',len(groups),'groups',len(cache),'textures')
