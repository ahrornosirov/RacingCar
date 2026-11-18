"""Adapt Armored Wave's Corolla FBX for Unity; preserve credit in ASSET_CREDITS.md."""
import bpy,bmesh,json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2]
folder=root/'Assets/ThirdParty/Corolla';(folder/'Textures').mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root/'Backups/CorollaSource/source/Corolla_E180.fbx'))
materials=[]
for m in bpy.data.materials:
    bsdf=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None) if m.use_nodes else None
    color=list(bsdf.inputs['Base Color'].default_value) if bsdf else list(m.diffuse_color)
    spec=dict(name=m.name,color=color,metallic=bsdf.inputs['Metallic'].default_value if bsdf else 0,roughness=bsdf.inputs['Roughness'].default_value if bsdf else .5,baseMap='',normalMap='')
    if m.use_nodes:
        for n in m.node_tree.nodes:
            if n.type!='TEX_IMAGE' or not n.image:continue
            name=Path(n.image.filepath.replace('\\','/')).name
            src=root/'Backups/CorollaSource/textures'/name
            if not src.exists():continue
            # Preserve UVs and shader bindings; ship 1K versions of the source textures.
            n.image.filepath=str(src);n.image.reload()
            if max(n.image.size)>1024:n.image.scale(*[max(1,round(v*1024/max(n.image.size))) for v in n.image.size])
            n.image.file_format='PNG'; n.image.filepath_raw=str(folder/'Textures'/(src.stem+'.png'));n.image.save()
            if 'normal' in name.lower():spec['normalMap']=src.stem+'.png'
            elif 'albedo' in name.lower() or 'color' in name.lower() or name in ['corolla_dashboard.png','small_screen1.png']:spec['baseMap']=src.stem+'.png'
    materials.append(spec)
wheels=bpy.data.objects['corolla_e180_wheels']
for name,sx,sy in [('WheelFL',1,-1),('WheelFR',-1,-1),('WheelRL',1,1),('WheelRR',-1,1)]:
    obj=wheels.copy();obj.data=wheels.data.copy();bpy.context.collection.objects.link(obj);obj.name=name
    bm=bmesh.new();bm.from_mesh(obj.data)
    bmesh.ops.delete(bm,geom=[v for v in bm.verts if (obj.matrix_world@v.co).x*sx<0 or (obj.matrix_world@v.co).y*sy<0],context='VERTS')
    bm.to_mesh(obj.data);bm.free()
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
    bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY',center='BOUNDS')
bpy.data.objects.remove(wheels,do_unlink=True)
# Plate source images are missing from the original archive: omit only the plates.
for name in ['Cube.002','Cube.003']:
    bpy.data.objects.remove(bpy.data.objects[name],do_unlink=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(folder/'Corolla.fbx'),object_types={'MESH','EMPTY'},bake_anim=False,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
(folder/'UnityMaterials.json').write_text(json.dumps(dict(materials=materials),indent=2))
