"""Bounded extraction of owned, substantial original demo compositions.

This is source/provenance intake, not placement or native validation. Original
parent transforms and ALL serialized prefab modification records are retained.
No old Finish layout, material recipe, scene or prefab is rewritten here.
"""
from pathlib import Path
import hashlib
import math
import re

CASTLE_PARTS = frozenset(('1xWall','2xWall','25DegreeWall','45DegreeWall','BlacksmithBase',
    'Bridge','CastleStairs','Chimney','ChurchBase','ChurchDoor','ChurchExtensionSide',
    'ChurchExtensionTop','ChurchSideEntrance','ChurchTower','ChurchWindow','CircleTower',
    'Dormer','Forge','HouseHangingRoof','HouseTower','HouseWindow','LargeHouseBase',
    'OpenGate','OpenGateBridge','SmallHouseBase','SquareTower','TowerBridge','WindmillBase',
    'StairsConnector','StairsEnd','StairsStart'))
TOWN_PARENTS = frozenset(('1670102749','443108605','1248777690','10139350','1851259282',
    '1097724891','1657498149','374927441','1332317192','201847186','1596488962','9399197'))
SPECS = (
    dict(name='Ventuar Authored Fortified Quarter v1',pack='Ventuar3D/MedievalCastlePack',
         scene='Scenes/Demo.unity',sha='c327a0d114742c0636ef8ba95af88d2faac8c3480e8205defec429dd4f19499d',
         selection='CASTLE_CONNECTED_ARCHITECTURAL_SOURCE_SET',instances=110,prefabs=31),
    dict(name='Tarbo Authored Town and Market v1',pack='Tarbo Studio/TARBO-FantasyVillage',
         scene='Demo/Village.unity',sha='7a83f5f156448a3e2b2f2ccd2c9a0f06ea815f02a7aae00fd902c187e4bcab06',
         selection='TWELVE_COMPLETE_ORIGINAL_TOWN_PARENT_GROUPS',instances=673,prefabs=196),
    dict(name='Tarbo Authored Lumber Warehouse Yard v1',pack='Tarbo Studio/TARBO-FantasyVillage',
         scene='Demo/LumberMill.unity',sha='9820a811d63b37600426d321f27f653e8c1eb214194cd026e3df503395cfb709',
         selection='COMPLETE_BUILDINGS_PROPS_TIMBER_AND_AUTHORED_GROUNDS',instances=156,prefabs=79),
    dict(name='Fantastic Authored Walled Town v1',pack='Tidal Flask Studios/Fantastic Village Pack',
         scene='scenes/demoscene_village_day.unity',sha='d65f8a0eb8f553e68e00913fa59170471a2dabeace8cbdf0f0d456de972a49bd',
         selection='COMPLETE_ORIGINAL_BUILDINGS_WALLS_AND_FENCES',instances=382,prefabs=40),
    dict(name='Tarbo Authored Fishing Warehouse Yard v1',pack='Tarbo Studio/TARBO-FantasyVillage',
         scene='Demo/Village.unity',sha='7a83f5f156448a3e2b2f2ccd2c9a0f06ea815f02a7aae00fd902c187e4bcab06',
         selection='COMPLETE_FISHING_PARENT_PLUS_ORIGINAL_NEARBY_CARGO_B',instances=30,prefabs=14))
COMPOSITION_KEYS = ('fortified-quarter','town-market','lumber-warehouse','walled-town','fishing-warehouse')


def sha(data):return hashlib.sha256(data).hexdigest()


def source_bytes(entries,path):
    supplier=entries[path][0]
    return supplier if isinstance(supplier,bytes) else supplier.read_bytes()


def blocks(data):
    text=data.decode('utf-8-sig');result={}
    for match in re.finditer(r'(?m)^--- !u!(\d+) &(-?\d+)([^\r\n]*)\r?\n',text):
        if match[2] in result:raise RuntimeError('Duplicate original Unity local ID')
        result[match[2]]=[int(match[1]),match[3],match.start(),match.end()]
    rows=list(result.items())
    return {key:dict(kind=row[0],stripped='stripped' in row[1],raw=text[row[2]:rows[i+1][1][2]if i+1<len(rows)else len(text)])for i,(key,row)in enumerate(rows)}


def reference(text,name):
    match=re.search(r'(?m)^\s*'+re.escape(name)+r': \{fileID: (-?\d+)\}',text)
    if not match:raise RuntimeError('Complete original '+name+' reference required')
    return match[1]


def trs(text):
    result={}
    for kind,axes in (('Position','xyz'),('Rotation','xyzw'),('Scale','xyz')):
        match=re.search(r'm_Local'+kind+r': \{([^}]+)\}',text)
        if not match:raise RuntimeError('Complete original local '+kind+' required')
        values={axis:float(value)for axis,value in re.findall(r'([xyzw]): ([^, }]+)',match[1])}
        if set(values)!=set(axes)or not all(math.isfinite(value)for value in values.values()):raise RuntimeError('Nonfinite or incomplete active source TRS')
        result[kind]=values
    return result


def selected(spec,path,parent):
    if spec['selection']=='CASTLE_CONNECTED_ARCHITECTURAL_SOURCE_SET':return Path(path).stem in CASTLE_PARTS
    if spec['selection']=='TWELVE_COMPLETE_ORIGINAL_TOWN_PARENT_GROUPS':return parent in TOWN_PARENTS
    if spec['selection']=='COMPLETE_BUILDINGS_PROPS_TIMBER_AND_AUTHORED_GROUNDS':return parent in {'1636395541','505850194','486136141','817735140'}
    if spec['selection']=='COMPLETE_ORIGINAL_BUILDINGS_WALLS_AND_FENCES':return parent in {'1303764443','1251519707','699177662'}
    if spec['selection']=='COMPLETE_FISHING_PARENT_PLUS_ORIGINAL_NEARBY_CARGO_B':return parent=='1851259282'or parent=='1670102749'and Path(path).stem=='TB_Bd_Cargo_B'
    raise RuntimeError('Unreviewed architecture selection')


def extract_composition(spec,entries,index,owned,destination,root):
    scene=Path(owned)/spec['pack']/spec['scene'];raw=scene.read_bytes()
    if sha(raw)!=spec['sha']:raise RuntimeError('Changed owned architecture scene needs a separately reviewed recipe: '+str(scene))
    scene_blocks=blocks(raw);parents={};instances=[];seeds=set();prefab_cache={}
    for ident,block in scene_blocks.items():
        if block['kind']!=1001:continue
        source=re.search(r'm_SourcePrefab: \{fileID: \d+, guid: ([a-f0-9]{32})',block['raw'])
        parent=reference(block['raw'],'m_TransformParent')
        if not source or source[1]not in index:continue
        path=index[source[1]]
        if not path.startswith('Assets/Plugins/'+spec['pack']+'/')or not path.endswith('.prefab')or not selected(spec,path,parent):continue
        if path not in prefab_cache:
            data=source_bytes(entries,path);original=blocks(data)
            roots=[(key,row)for key,row in original.items()if row['kind']==4 and not row['stripped']and reference(row['raw'],'m_Father')=='0']
            if len(roots)!=1:raise RuntimeError('Exactly one real original prefab root required: '+path)
            prefab_cache[path]=(data,roots[0][0],trs(roots[0][1]['raw']))
        data,root_id,original_pose=prefab_cache[path]
        pose={kind:dict(value)for kind,value in original_pose.items()};modifications=[]
        pattern=r'- target: \{fileID: (-?\d+), guid: ([a-f0-9]{32}),\s+type: (\d+)\}\s+propertyPath: ([^\r\n]+)\s+value: ([^\r\n]*)\r?\n\s+objectReference: \{([^}]+)\}'
        for target,guid,target_type,prop,value,object_ref in re.findall(pattern,block['raw']):
            modifications.append(dict(targetId=target,targetGuid=guid,targetType=int(target_type),property=prop,sourceValue=value,objectReference=object_ref))
            match=re.fullmatch(r'm_Local(Position|Rotation|Scale)\.([xyzw])',prop)
            if target==root_id and guid==source[1]and match:
                number=float(value)
                if not math.isfinite(number):raise RuntimeError('Nonfinite active original instance root TRS')
                if match[2]not in pose[match[1]]:raise RuntimeError('Invalid active source root axis')
                pose[match[1]][match[2]]=number
            for asset_guid in re.findall(r'guid: ([a-f0-9]{32})',object_ref):
                if asset_guid not in index:raise RuntimeError('Unknown original authored modification asset GUID: '+asset_guid)
                seeds.add(index[asset_guid])
        if len(modifications)!=len(re.findall(r'(?m)^\s*propertyPath:',block['raw'])):raise RuntimeError('Unparsed original modification; retain source without guessing')
        ancestor=parent;seen=set()
        while ancestor!='0':
            if ancestor in seen or ancestor not in scene_blocks:raise RuntimeError('Missing/cyclic actual source parent chain')
            seen.add(ancestor);node=scene_blocks[ancestor]
            if node['kind']!=4 or node['stripped']:raise RuntimeError('Nested stripped source parent requires a separately reviewed native composition recipe')
            game_object=reference(node['raw'],'m_GameObject');go=scene_blocks[game_object]['raw'];name=re.search(r'(?m)^  m_Name: ([^\r\n]*)',go);active=re.search(r'(?m)^  m_IsActive: ([01])\s*$',go)
            if not name or not active:raise RuntimeError('Complete original parent identity/activation required')
            parent_id=reference(node['raw'],'m_Father');parent_pose=trs(node['raw'])
            parents[ancestor]=dict(id=ancestor,parent=parent_id,name=name[1],active=active[1]=='1',position=parent_pose['Position'],rotation=parent_pose['Rotation'],scale=parent_pose['Scale'],sourceTransformSha256=sha(node['raw'].encode()),sourceGameObjectSha256=sha(go.encode()))
            ancestor=parent_id
        instances.append(dict(sceneInstanceId=ident,prefabGuid=source[1],prefab=(Path(destination)/path.removeprefix('Assets/')).relative_to(root).as_posix(),sourceSha256=sha(data),sourceMetaSha256=sha(entries[path][1]),sourceRootTransformId=root_id,sourceParent=parent,position=pose['Position'],rotation=pose['Rotation'],scale=pose['Scale'],modifications=modifications,serializedInstanceSha256=sha(block['raw'].encode()),serializedInstance=block['raw'],nativeOverridesReconciled=False))
        seeds.add(path)
    if len(instances)!=spec['instances']or len({row['prefab']for row in instances})!=spec['prefabs']:raise RuntimeError('Exact reviewed substantial architecture count differs: '+spec['name']+' '+str((len(instances),len({row['prefab']for row in instances}))))
    source_meta=Path(str(scene)+'.meta').read_bytes()
    return sorted(seeds),dict(schemaVersion=1,name=spec['name'],scope='EXACT_SUBSTANTIAL_OWNED_DEMO_COMPOSITION_SOURCE_INTAKE_NOT_NATIVE_OVERRIDE_MATERIAL_GEOMETRY_PLACEMENT_OR_VISUAL_ACCEPTANCE',sourceScene='OWNED/'+spec['pack']+'/'+spec['scene'],sourceSceneSha256=spec['sha'],sourceSceneMetaSha256=sha(source_meta),selection=spec['selection'],instanceCount=len(instances),prefabCount=spec['prefabs'],parentTransforms=[parents[key]for key in sorted(parents)],instances=instances,sourceLocalTrsPreserved=True,nativeOverridesReconciled=False,nativeMaterialImportsValidated=False,nativeMeshBoundsMeasured=False,nativeSupportAndRoadWingBuffersValidated=False),raw,source_meta


def owned_architecture_compositions(entries,index,owned,destination,root,selected_keys=None):
    chosen=list(COMPOSITION_KEYS)if selected_keys is None else list(selected_keys)
    if not chosen or len(chosen)!=len(set(chosen))or not set(chosen)<=set(COMPOSITION_KEYS):raise RuntimeError('Exact nonempty distinct reviewed architecture composition keys required')
    seeds=set();layouts=[];scenes={}
    for key,spec in zip(COMPOSITION_KEYS,SPECS):
        if key not in chosen:continue
        selected_seeds,layout,raw,meta=extract_composition(spec,entries,index,owned,destination,root)
        layout['compositionKey']=key
        seeds.update(selected_seeds);layouts.append(layout)
        scenes[spec['sha']]=dict(source=layout['sourceScene'],raw=raw,meta=meta)
    return sorted(seeds),layouts,scenes
