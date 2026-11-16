"""Selective, dependency-closed private intake of owned Dragon Kingdom art.

Dry run by default. --apply copies exact bytes and metadata; never overwrites
different files. Does not launch Unity, modify vendor bytes, or extract tar paths.
"""
from __future__ import annotations

import argparse
import gzip
import hashlib
import io
import json
import math
import struct
import re
import subprocess
import tarfile
import uuid
from collections import deque
from pathlib import Path, PurePosixPath

ROOT = Path(__file__).resolve().parents[2]
OWNED = Path('F:/Unity Projects/UnityAssetPackages/Unity Asset Packages/Assets/Plugins')
CACHE = Path('C:/Users/Jeremy/AppData/Roaming/Unity/Asset Store-5.x/Tidal Flask Studios/3D ModelsEnvironments/FANTASTIC - Ancient Forest.unitypackage')
DEST = ROOT / 'Assets/TruckDragonKingdom/PrivateLicensedEnvironment'
REPORT = ROOT / 'Documentation/TruckSports/DragonKingdom/KingdomAssetAudit.json'
GUID = re.compile(rb'guid:\s*([0-9a-fA-F]{32})')
TEXT = {'.prefab', '.mat', '.asset', '.controller', '.anim', '.shader', '.shadergraph', '.shadersubgraph', '.hlsl', '.cginc', '.cs', '.compute', '.vfx', '.terrainlayer'}
MARKET_LAYOUT = DEST / 'DerivedVillageLayout/Artist Market Front v3.json'
PACKS = ['Synty Studios/PolygonKnights', 'Tidal Flask Studios/Fantastic Village Pack',
         'Tarbo Studio/TARBO-TowerDefensePack', 'Tarbo Studio/TARBO-FantasyVillage',
         'Ventuar3D/MedievalCastlePack', 'Polyart Studio/HandpaintedEnvironmentVol1']


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def clean_package_path(data: bytes) -> str:
    # Unity cache pathname records can carry a literal newline + 00 suffix.
    value = data.decode('utf-8-sig').splitlines()[0].split('\0')[0].strip().replace('\\', '/')
    path = PurePosixPath(value)
    if path.is_absolute() or '..' in path.parts or not value.startswith('Assets/'):
        raise ValueError(f'Unsafe package pathname: {value!r}')
    return str(path)


def owned_market_layout(entries: dict, index: dict) -> tuple[list[str], dict]:
    """Read the real artist's scene; retain private poses, never import its weather.

    This bounded selection was reviewed against the owned vendor day demo and
    documentation. Its one identity parent and exact source bytes are required;
    an updated or hand-edited vendor scene needs a new explicit review.
    """
    scene = OWNED / 'Tidal Flask Studios/Fantastic Village Pack/scenes/demoscene_village_day.unity'
    raw = scene.read_bytes()
    expected = 'd65f8a0eb8f553e68e00913fa59170471a2dabeace8cbdf0f0d456de972a49bd'
    if digest(raw) != expected:
        raise RuntimeError('Preserve and review changed original vendor market composition; do not overwrite private layout.')
    text = raw.decode('utf-8-sig')
    blocks = re.split(r'(?m)^--- !u!', text)[1:]
    transforms = {}
    for block in blocks:
        if not block.startswith('4 ') or ' stripped\n' in block[:60]:
            continue
        ident = re.match(r'4 &(-?\d+)', block)
        father = re.search(r'm_Father: \{fileID: (-?\d+)\}', block)
        if ident and father:
            values = {}
            for kind in ['Position', 'Rotation', 'Scale']:
                value = re.search(r'm_Local' + kind + r': \{([^}]+)\}', block)
                if value:
                    values[kind] = {a: float(v) for a, v in re.findall(r'([xyzw]): ([^, }]+)', value[1])}
            transforms[ident[1]] = (father[1], values)
    parent = '2075655012'
    seen = set()
    while parent != '0':
        if parent in seen or parent not in transforms:
            raise RuntimeError('Artist market parent must resolve through explicit scene transforms.')
        seen.add(parent)
        parent, values = transforms[parent]
        if values.get('Position') != dict(x=0.0, y=0.0, z=0.0) or values.get('Scale') != dict(x=1.0, y=1.0, z=1.0) or values.get('Rotation') != dict(x=0.0, y=0.0, z=0.0, w=1.0):
            raise RuntimeError('Preserve changed artist market parent transforms; no guessed world/local conversion.')
    center = dict(x=199.45, y=14.997253, z=194.77)
    rows = []
    sources = set()
    for block in blocks:
        if not block.startswith('1001 ') or not re.search(r'm_TransformParent: \{fileID: 2075655012\}', block):
            continue
        source = re.search(r'm_SourcePrefab: \{fileID: \d+, guid: ([a-f0-9]{32})', block)
        if source is None or source[1] not in index:
            continue
        path = index[source[1]]
        if not path.startswith('Assets/Plugins/Tidal Flask Studios/Fantastic Village Pack/prefabs/props/'):
            continue
        supplier = entries[path][0]
        data = supplier if isinstance(supplier, bytes) else supplier.read_bytes()
        roots = [b for b in re.split(r'(?m)^--- !u!', data.decode('utf-8-sig'))[1:]
                 if b.startswith('4 ') and re.search(r'm_Father: \{fileID: 0\}', b)]
        if len(roots) != 1:
            raise RuntimeError('Exact single original prefab root required: ' + path)
        root = roots[0]
        root_id = re.match(r'4 &(-?\d+)', root)[1]
        values = {}
        for kind in ['Position', 'Rotation', 'Scale']:
            value = re.search(r'm_Local' + kind + r': \{([^}]+)\}', root)
            if value:
                values.update({'m_Local' + kind + '.' + a: float(v) for a, v in re.findall(r'([xyzw]): ([^, }]+)', value[1])})
        overrides = []
        unreviewed = []
        pattern = r'- target: \{fileID: (-?\d+), guid: ([a-f0-9]{32}),\s+type: 3\}\s+propertyPath: ([^\r\n]+)\s+value: ([^\r\n]*)'
        for target, guid, prop, value in re.findall(pattern, block):
            if target == root_id and guid == source[1] and prop.startswith(('m_LocalPosition.', 'm_LocalRotation.', 'm_LocalScale.')):
                values[prop] = float(value)
            elif prop.startswith('m_BoundingSphereOverride.'):
                number = float(value)
                overrides.append(dict(targetId=target,targetGuid=guid,property=prop,sourceValue=value))
            elif prop not in ['m_Name', 'm_RootOrder', 'm_StaticEditorFlags', 'm_Layer'] and not prop.startswith('m_LocalEulerAnglesHint.'):
                unreviewed.append(prop)
        position = {a: values.get('m_LocalPosition.' + a, 0) for a in 'xyz'}
        if ((position['x'] - center['x']) ** 2 + (position['z'] - center['z']) ** 2) ** .5 > 10 or abs(position['y'] - center['y']) >= 3:
            continue
        classified = []
        for raw_override in overrides:
            target,guid,prop,value=raw_override['targetId'],raw_override['targetGuid'],raw_override['property'],raw_override['sourceValue']
            number=float(value)
            inherited = []
            for nested in re.split(r'(?m)^--- !u!', data.decode('utf-8-sig'))[1:]:
                if not nested.startswith('1001 '): continue
                nested_guid = re.search(r'm_SourcePrefab: \{fileID: \d+, guid: ([a-f0-9]{32})', nested)
                nested_id = re.match(r'1001 &(-?\d+)', nested)[1]
                if nested_guid is None or nested_guid[1] not in index: continue
                nested_path = index[nested_guid[1]]; supplier = entries[nested_path][0]
                nested_data = supplier if isinstance(supplier, bytes) else supplier.read_bytes()
                for component in re.split(r'(?m)^--- !u!', nested_data.decode('utf-8-sig'))[1:]:
                    if not component.startswith('108 '): continue
                    light_id = re.match(r'108 &(-?\d+)', component)[1]
                    if int(nested_id) ^ int(light_id) != int(target): continue
                    if not re.search(r'(?m)^  m_UseBoundingSphereOverride: 0\r?$', component): raise RuntimeError('Active light bound override cannot be ignored')
                    inherited.append(dict(sourceComponentGuid=nested_guid[1], sourceComponentId=light_id, nestedSourceInstanceId=nested_id, sourceComponentType='UnityEngine.Light'))
            if len(inherited) != 1: raise RuntimeError('Exact disabled inherited source Light proof required for old demo bound metadata')
            classified.append(dict(targetId=target, targetGuid=guid, property=prop, value=0.0, sourceValue=value, sourceValueFloatBits=struct.pack('<f', number).hex(), state='INACTIVE_SOURCE_LIGHT_BOUND_OVERRIDE', applied=False, **inherited[0]))
        overrides=classified
        if unreviewed: raise RuntimeError('Unreviewed artist override in selected market grouping: ' + ', '.join(sorted(set(unreviewed))))
        rows.append(dict(sceneInstanceId=re.match(r'1001 &(-?\d+)', block)[1], prefabGuid=source[1],
                         prefab=(DEST / path.removeprefix('Assets/')).relative_to(ROOT).as_posix(),
                         sourceSha256=digest(data), sourceMetaSha256=digest(entries[path][1]),
                         position={a: position[a] - center[a] for a in 'xyz'},
                         rotation={a: values.get('m_LocalRotation.' + a, 1 if a == 'w' else 0) for a in 'xyzw'},
                         scale={a: values.get('m_LocalScale.' + a, 1) for a in 'xyz'}, overrides=overrides))
        sources.add(path)
    if len(rows) != 61 or len(sources) != 28 or len({r['sceneInstanceId'] for r in rows}) != 61:
        raise RuntimeError('The exact reviewed61-instance/28-prefab artist group must remain intact.')
    return sorted(sources), dict(schemaVersion=3, scope='PRIVATE_DERIVED_REAL_VENDOR_MARKET_LAYOUT_NOT_GAMEPLAY_OR_VISUAL_ACCEPTANCE',
                                 sourceScene='OWNED/Tidal Flask Studios/Fantastic Village Pack/scenes/demoscene_village_day.unity',
                                 sourceSceneSha256=expected, sourceSceneMetaSha256=digest(Path(str(scene) + '.meta').read_bytes()),
                                 identityParent='2075655012', center=center, radius=10, instances=rows)


def owned_finish_assemblies(entries: dict, index: dict) -> tuple[list[str], list[dict]]:
    """Exact authored courts and rock/vegetation assemblies, never demo managers.

    Only raw source intake is authorized here. Inherited component overrides,
    public shader properties and native support still require Editor preflight.
    """
    specs = [
        dict(name='Village Garden Court v1', pack='Tidal Flask Studios/Fantastic Village Pack',
             scene='scenes/demoscene_village_day.unity', sha='d65f8a0eb8f553e68e00913fa59170471a2dabeace8cbdf0f0d456de972a49bd',
             center=dict(x=187., y=15., z=232.), radius=14., vertical=8., instances=89, prefabs=44),
        dict(name='Autumn Roadside Assembly v1', pack='Polyart Studio/HandpaintedEnvironmentVol1',
             scene='Scenes/Autumn_Forest_Demo.unity', sha='2b93b8788de7303cba83bd5b116e204de9d8e4b4b6b99f6113ad90a53c26d917',
             center=dict(x=-29.000854, y=-6.139099, z=22.084839), radius=14., vertical=8., instances=34, prefabs=14)
    ]
    sources = set(); layouts = []
    for spec in specs:
        scene = OWNED / spec['pack'] / spec['scene']; raw = scene.read_bytes()
        if digest(raw) != spec['sha']: raise RuntimeError('Changed original authored scene requires explicit review: ' + str(scene))
        blocks = re.split(r'(?m)^--- !u!', raw.decode('utf-8-sig'))[1:]
        transforms = {}
        def trs(block):
            result = {}
            for kind, axes, fallback in [('Position','xyz',0), ('Rotation','xyzw',0), ('Scale','xyz',1)]:
                m = re.search(r'm_Local' + kind + r': \{([^}]+)\}', block)
                if not m: raise RuntimeError('Complete original transform required')
                result[kind] = {a: float(v) for a,v in re.findall(r'([xyzw]): ([^, }]+)', m[1])}
            return result
        for block in blocks:
            m = re.match(r'4 &(-?\d+)', block)
            if m and ' stripped\n' not in block[:60]: transforms[m[1]] = (re.search(r'm_Father: \{fileID: (-?\d+)\}', block)[1], trs(block))
        rows = []; parents = {}
        for block in blocks:
            if not block.startswith('1001 '): continue
            m = re.search(r'm_SourcePrefab: \{fileID: \d+, guid: ([a-f0-9]{32})', block)
            parent = re.search(r'm_TransformParent: \{fileID: (-?\d+)\}', block)
            if not m or not parent or m[1] not in index: continue
            path = index[m[1]]
            if not path.startswith('Assets/Plugins/' + spec['pack'] + '/') or not path.endswith('.prefab'): continue
            if spec['name'].startswith('Autumn') and parent[1] != '1712594225': continue
            supplier=entries[path][0];data=supplier if isinstance(supplier,bytes) else supplier.read_bytes()
            roots=[b for b in re.split(r'(?m)^--- !u!', data.decode('utf-8-sig'))[1:] if b.startswith('4 ') and re.search(r'm_Father: \{fileID: 0\}', b)]
            if len(roots)!=1: raise RuntimeError('Exactly one original prefab root required: '+path)
            rootid=re.match(r'4 &(-?\d+)',roots[0])[1];values=trs(roots[0]);overrides=[]
            pattern=r'- target: \{fileID: (-?\d+), guid: ([a-f0-9]{32}),\s+type: 3\}\s+propertyPath: ([^\r\n]+)\s+value: ([^\r\n]*)\r?\n\s+objectReference: \{([^}]+)\}'
            for target,guid,prop,value,reference in re.findall(pattern,block):
                if target==rootid and guid==m[1] and prop.startswith(('m_LocalPosition.','m_LocalRotation.','m_LocalScale.')):
                    kind=prop.split('.')[0].removeprefix('m_Local');values[kind][prop[-1]]=float(value)
                elif prop not in ['m_Name','m_RootOrder','m_StaticEditorFlags','m_Layer'] and not prop.startswith('m_LocalEulerAnglesHint.'):
                    overrides.append(dict(targetId=target,targetGuid=guid,property=prop,sourceValue=value,objectReference=reference))
            pos=values['Position'];center=spec['center']
            if math.hypot(pos['x']-center['x'],pos['z']-center['z'])>spec['radius'] or abs(pos['y']-center['y'])>=spec['vertical']:continue
            chain=[];ancestor=parent[1];seen=set()
            while ancestor!='0':
                if ancestor in seen or ancestor not in transforms: raise RuntimeError('Incomplete/cyclic authored assembly parent chain')
                seen.add(ancestor);father,pose=transforms[ancestor]
                if pose['Rotation']!=dict(x=0.,y=0.,z=0.,w=1.) or pose['Scale']!=dict(x=1.,y=1.,z=1.):raise RuntimeError('Rotated/scaled source parent requires separate coherent transform review')
                chain.append(dict(id=ancestor,position=pose['Position'],rotation=pose['Rotation'],scale=pose['Scale']));ancestor=father
            parents[parent[1]]=chain
            if any(not math.isfinite(v) for value in values.values() for v in value.values()):raise RuntimeError('Nonfinite active authored TRS')
            rows.append(dict(sceneInstanceId=re.match(r'1001 &(-?\d+)',block)[1],prefabGuid=m[1],prefab=(DEST/path.removeprefix('Assets/')).relative_to(ROOT).as_posix(),sourceSha256=digest(data),sourceMetaSha256=digest(entries[path][1]),sourceParent=parent[1],position={a:pos[a]-center[a] for a in 'xyz'},rotation=values['Rotation'],scale=values['Scale'],overrides=overrides));sources.add(path)
            for change in overrides:
                for ref in GUID.findall(change['objectReference'].encode()):
                    if ref.decode() not in index:raise RuntimeError('Unknown active authored override asset GUID: '+ref.decode())
                    sources.add(index[ref.decode()])
        if len(rows)!=spec['instances'] or len({r['prefab'] for r in rows})!=spec['prefabs']:raise RuntimeError('Exact reviewed authored assembly differs: '+spec['name']+' '+str((len(rows),len({r['prefab'] for r in rows}))))
        layouts.append(dict(schemaVersion=1,name=spec['name'],scope='PRIVATE_EXACT_AUTHOR_COMPOSITION_PENDING_NATIVE_COMPONENT_MATERIAL_AND_SUPPORT_PROOF',sourceScene='OWNED/'+spec['pack']+'/'+spec['scene'],sourceSceneSha256=spec['sha'],sourceSceneMetaSha256=digest(Path(str(scene)+'.meta').read_bytes()),center=spec['center'],radius=spec['radius'],sourceParents=[dict(parent=key,ancestors=value) for key,value in sorted(parents.items())],instances=rows))
    return sorted(sources), layouts


def record_finish_material_provenance(apply: bool) -> None:
    """Pin the actual V228 material stage; hierarchy failure is kept explicit."""
    directory=ROOT/'Logs/TruckSports/DragonKingdom/V228'
    archive=directory/'private-native-material-observations.zip'
    archive_sha='067fbccad3f7d3af56944298a07fb630e641d0af109185655e3d5b5f1006d894'
    if digest(archive.read_bytes())!=archive_sha:raise RuntimeError('Exact preserved V228 native material bytes required')
    import zipfile
    observation=json.loads((directory/'rendered-evidence/original-authored-assemblies-preflight.json').read_text())
    if not observation['nativeSourcesPreserved'] or not observation['missingLegacyTextureIsInactive'] or len(observation['materialObservations'])!=51:raise RuntimeError('Actual complete51 material-stage observations required, independently of failed hierarchy stage')
    intake=json.loads(REPORT.with_name('FinishAuthoredAssembliesIntake.json').read_text());snapshots={r['source']:r for r in intake['originalMaterialSnapshots']}
    old=json.loads((DEST/'DerivedVillageLayout/Material Import Provenance v1.json').read_text())
    def property_text(text,kind,name):
        pattern=(r'(?m)^    - '+re.escape(name)+r':\r?\n((?:        .*\r?\n)+)') if kind=='Texture' else (r'(?m)^'+re.escape(('  ' if kind=='Shader' else '    - ')+name)+r': ([^\r\n]+)')
        m=re.search(pattern,text)
        if not m:raise RuntimeError('Actual serialized material property missing: '+kind+'/'+name)
        return m[1].replace('\r\n','\n').strip()
    def properties(text):
        rows=[dict(kind='Shader',name='m_Shader')]
        for section,kind in [('m_TexEnvs','Texture'),('m_Ints','Int'),('m_Floats','Float'),('m_Colors','Color')]:
            m=re.search(r'(?ms)^    '+section+r':(?:\r?\n| \[\]\r?\n)(.*?)(?=^    m_|^  m_|\Z)',text)
            if m:rows += [dict(kind=kind,name=n) for n in re.findall(r'(?m)^    - ([^:\r\n]+):',m[1])]
        for row in rows:row['sourceValue']=property_text(text,row['kind'],row['name'])
        return rows
    def metadata(text):
        material=next(b for b in re.split(r'(?m)^--- !u!',text)[1:] if b.startswith('21 '));matches=list(re.finditer(r'(?m)^  ([A-Za-z_][A-Za-z_0-9]*):',material));result={}
        for i,m in enumerate(matches):
            if m[1]=='m_SavedProperties':continue
            end=matches[i+1].start() if i+1<len(matches) else len(material)
            result[m[1]]=material[m.end():end].strip().replace('\r\n','\n')
        result['URP.AssetVersion.version']=re.search(r'(?m)^  version: (\d+)',text)[1] if re.search(r'(?m)^  version: (\d+)',text) else '<absent>'
        return result
    aliases={'M_CLR_blue.mat','M_CLR_yellow_E.mat','M_FX_steam.mat','M_PROP_paper.mat'};emissions={'M_CLR_blue.mat','M_CLR_yellow_E.mat'};bindings=[]
    allowed_metadata={'serializedVersion','m_ShaderKeywords','m_Parent','m_ModifiedSerializedProperties','m_ValidKeywords','m_InvalidKeywords','m_CustomRenderQueue','disabledShaderPasses','m_LockedProperties','m_BuildTextureStacks','m_AllowLocking','URP.AssetVersion.version'}
    public_shaders=[];package=next(p for p in (ROOT/'Library/PackageCache').iterdir() if p.name.startswith('com.unity.render-pipelines.universal@'))
    for relative in ['Shaders/Lit.shader','Shaders/Particles/ParticlesUnlit.shader']:
        file=package/relative;meta=Path(str(file)+'.meta').read_bytes();text=file.read_text();
        if not re.search(r'\[MainColor\] _BaseColor',text):raise RuntimeError('Actual public shader must declare authored BaseColor as MainColor')
        public_shaders.append(dict(package=old['upgraderPackage'],relativePath=relative,sha256=digest(file.read_bytes()),guid=GUID.search(meta)[1].decode(),mainColor='_BaseColor'))
    with zipfile.ZipFile(archive) as z:
        for row in observation['materialObservations']:
            path=row['source'];raw=snapshots[path];original_bytes=(ROOT/raw['path']).read_bytes();native_bytes=z.read(path);meta=z.read(path+'.meta')
            if digest(original_bytes)!=raw['sha256'] or digest(native_bytes)!=row['nativeSha256'] or digest(meta)!=row['metaSha256']:raise RuntimeError('Actual original/native/metadata byte proof changed')
            original=original_bytes.decode('utf-8-sig');native=native_bytes.decode('utf-8-sig');unchanged=[];converted=[]
            for item in row['unchangedProperties']:
                if property_text(original,item['kind'],item['name'])!=item['sourceValue'] or property_text(native,item['kind'],item['name'])!=item['sourceValue']:raise RuntimeError('Protected authored material value changed')
                unchanged.append(dict(kind=item['kind'],name=item['name'],sourceValue=item['sourceValue'],importedValue=item['sourceValue'],reason='EXACT_AUTHORED_VALUE_PRESERVED'))
            for item in row['changedProperties']:
                name=Path(path).name;kind=item['kind'];prop=item['name']
                if kind!='Color' or not ((prop=='_Color' and name in aliases) or (prop=='_EmissionColor' and name in emissions)):raise RuntimeError('Unreviewed material conversion')
                if property_text(original,kind,prop)!=item['sourceValue'] or property_text(native,kind,prop)!=item['importedValue']:raise RuntimeError('Exact reviewed conversion bytes differ')
                color=lambda value:[float(re.search(axis+r': ([^,} ]+)',value)[1]) for axis in 'rgba']
                before=color(item['sourceValue']);after=color(item['importedValue'])
                if prop=='_Color':expected=color(property_text(original,'Color','_BaseColor'));reason='PUBLIC_URP_MAINCOLOR_BASECOLOR_TO_LEGACY_COLOR_ALIAS_NO_AUTHORED_BASECOLOR_CHANGE'
                else:expected=[v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 if v<=1 else v**2.2 for v in before[:3]]+[before[3]];reason='PUBLIC_URP_NATIVE_V1_TO_V10_HDR_EMISSION_COLOR_LINEAR_COMPATIBILITY'
                if max(abs(a-b) for a,b in zip(after,expected))>2e-5:raise RuntimeError('Actual public BaseColor/Color.linear conversion differs')
                converted.append(dict(kind=kind,name=prop,sourceValue=item['sourceValue'],importedValue=item['importedValue'],reason=reason))
            source_props={(p['kind'],p['name']) for p in properties(original)};added=[dict(kind=p['kind'],name=p['name'],sourceValue='<absent>',importedValue=p['sourceValue'],reason='EXACT_NATIVE_PUBLIC_URP_DEFAULT_FIELD_ADDITION') for p in properties(native) if (p['kind'],p['name']) not in source_props]
            before_meta=metadata(original);after_meta=metadata(native);changes=[]
            for name in sorted(before_meta.keys()|after_meta.keys()):
                before=before_meta.get(name,'<absent>');after=after_meta.get(name,'<absent>')
                if before==after:continue
                if name not in allowed_metadata:raise RuntimeError('Unreviewed native material metadata change: '+path+'/'+name)
                changes.append(dict(name=name,sourceValue=before,importedValue=after,reason='EXACT_PUBLIC_URP_NATIVE_SERIALIZATION_QUEUE_KEYWORD_PASS_VERSION_MIGRATION'))
            queue=lambda text:int(re.search(r'(?m)^  m_CustomRenderQueue: (-?\d+)',text)[1])
            bindings.append(dict(path=path,guid=raw['sourceGuid'],originalSha256=raw['sha256'],importedSha256=row['nativeSha256'],metaSha256=row['metaSha256'],snapshot=raw['path'],snapshotSha256=raw['sha256'],snapshotMetaSha256=raw['metaSha256'],snapshotGuid=raw['guid'],originalRenderQueue=queue(original),importedRenderQueue=queue(native),protectedProperties=unchanged,convertedProperties=converted,addedProperties=added,metadataChanges=changes))
    if sum(len(r['protectedProperties']) for r in bindings)!=2544 or sum(len(r['convertedProperties']) for r in bindings)!=6 or sum(r['originalSha256']!=r['importedSha256'] for r in bindings)!=30:raise RuntimeError('Exact actual51 import accounting differs')
    result={k:v for k,v in old.items() if k!='bindings'};result.update(scope='EXACT_V228_51_MATERIAL_NATIVE_STAGE_PROVENANCE_NOT_GENERIC_WAIVER_OR_HIERARCHY_VISUAL_ACCEPTANCE',sourceNativeRun='V228',sourceNativeArchiveSha256=archive_sha,sourceNativeCaseStatus=observation['status'],materialStageOnly=True,publicShaderSources=public_shaders,sourceLayouts=intake['authoredAssemblies'],bindings=bindings)
    payload=(json.dumps(result,indent=2,allow_nan=False)+'\n').encode();path=DEST/'DerivedFinishAssemblies/Material Import Provenance v1.json';guid=uuid.uuid5(uuid.NAMESPACE_URL,'interstate-hauler/private-finish-material-provenance/v1').hex;meta=('fileFormatVersion: 2\nguid: '+guid+'\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n').encode()
    for file,data in [(path,payload),(Path(str(path)+'.meta'),meta)]:
        if file.exists() and file.read_bytes()!=data:raise RuntimeError('Preserve different existing exact material import provenance')
    if apply:
        path.parent.mkdir(parents=True,exist_ok=True)
        for file,data in [(path,payload),(Path(str(path)+'.meta'),meta)]:
            if not file.exists():file.write_bytes(data)
            if file.read_bytes()!=data:raise RuntimeError('Native material provenance write verification failed')
    print(json.dumps(dict(path=path.relative_to(ROOT).as_posix(),sha256=digest(payload),metaSha256=digest(meta),guid=guid,bindings=len(bindings),protectedProperties=2544,convertedProperties=6,addedProperties=sum(len(r['addedProperties']) for r in bindings),metadataChanges=sum(len(r['metadataChanges']) for r in bindings),applied=apply),indent=2))


def intake_road_shader(apply: bool) -> None:
    """Preserve packaged URP shader; derive only a unique shader identity.

    The package targets the SAME GUID/path as installed legacy EasyRoads.
    Importing it over that path would modify vendor source, so this bounded
    branch never extracts package paths and never copies its source metadata.
    """
    package = ROOT / 'Assets/EasyRoads3D/SRP Support Packages/URP_7_3_1.unityPackage'
    expected = 'Assets/EasyRoads3D/shaders/ER Road.shader'
    with tarfile.open(package, 'r:gz') as archive:
        rows = [m for m in archive.getmembers() if m.name.endswith('/pathname')
                and clean_package_path(archive.extractfile(m).read()) == expected]
        if len(rows) != 1:
            raise RuntimeError('Expected one actual vendor URP road shader')
        prefix = rows[0].name.rsplit('/', 1)[0]
        raw = archive.extractfile(prefix + '/asset').read()
        original_meta = archive.extractfile(prefix + '/asset.meta').read()
    old = b'Shader "EasyRoads3D/ER Road"'
    new = b'Shader "LWS/Owned EasyRoads/URP Road Review"'
    if raw.count(old) != 1 or b'"UniversalPipeline"' not in raw or b'_OffsetFactor' not in raw:
        raise RuntimeError('Actual retained vendor shader family changed')
    derived = raw.replace(old, new, 1)
    folder = DEST / 'DerivedEasyRoads/URPRoadReview'
    target = folder / 'Owned ER Road URP.shader'
    shader_guid = uuid.uuid5(uuid.NAMESPACE_URL, target.relative_to(ROOT).as_posix()).hex
    meta = ('fileFormatVersion: 2\nguid: ' + shader_guid + '\nShaderImporter:\n'
            '  externalObjects: {}\n  defaultTextures: []\n  nonModifiableTextures: []\n'
            '  userData: \n  assetBundleName: \n  assetBundleVariant: \n').encode()
    outputs = {target: derived, Path(str(target) + '.meta'): meta,
               folder / 'Original ER Road URP.shader.bytes': raw,
               folder / 'Original ER Road URP.meta.bytes': original_meta}
    includes = []
    for value in sorted(set(re.findall(rb'#include\s+"([^"\r\n]+)"', raw))):
        name = value.decode()
        parts = PurePosixPath(name).parts
        if len(parts) < 3 or parts[0] != 'Packages' or '..' in parts:
            raise RuntimeError('Review unexpected shader include: ' + name)
        direct = ROOT / name
        candidates = [direct] + [p.joinpath(*parts[2:]) for p in (ROOT / 'Library/PackageCache').glob(parts[1] + '@*')]
        actual = next((p for p in candidates if p.is_file()), None)
        if actual is None:
            raise RuntimeError('Installed shader include missing: ' + name)
        includes.append({'include': name, 'installed': actual.relative_to(ROOT).as_posix(), 'sha256': digest(actual.read_bytes())})
    for path, payload in outputs.items():
        if path.exists() and path.read_bytes() != payload:
            raise RuntimeError('Preserve differing prior review payload: ' + path.relative_to(ROOT).as_posix())
    if apply:
        folder.mkdir(parents=True, exist_ok=True)
        for path, payload in outputs.items():
            if not path.exists():
                path.write_bytes(payload)
            if path.read_bytes() != payload:
                raise RuntimeError('Shader intake byte verification failed')
    report = {'scope': 'PRIVATE_VENDOR_URP_ROAD_SHADER_REVIEW_NOT_NATIVE_COMPILED_OR_ADOPTED',
              'applied': apply, 'package': package.relative_to(ROOT).as_posix(), 'packageSha256': digest(package.read_bytes()),
              'originalPackagePath': expected, 'rawSha256': digest(raw), 'rawMetaSha256': digest(original_meta),
              'derivedSha256': digest(derived), 'derivedPath': target.relative_to(ROOT).as_posix(), 'derivedGuid': shader_guid,
              'derivation': 'Only Shader declaration name changed; unique owned metadata prevents vendor GUID/path collision.',
              'nativeShaderCompileAndRenderRequired': True, 'vendorFilesModified': False, 'includes': includes}
    REPORT.with_name('RoadShaderIntake.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(report, indent=2))


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apply', action='store_true')
    parser.add_argument('--record-finish-material-provenance', action='store_true', help='Pin the exact private V228 native51 material stage and public URP conversions; no generic import waiver.')
    parser.add_argument('--mountain-art-only', action='store_true', help='Intake the two audited actual Polyart mountain meshes, preserving the original environment receipt.')
    parser.add_argument('--bridge-support-only', action='store_true', help='Intake actual modular pillar/beam models for measured bridge bearing foundations, avoiding orphan prefab collision scripts.')
    parser.add_argument('--terrain-palette-only', action='store_true', help='Exact authored FantasticVillage grass2m and Polyart11m ground TerrainLayers with original texture dependencies; no terrain painting or source edits.')
    parser.add_argument('--finish-assemblies-only', action='store_true', help='Exact original89-instance garden court and34-instance natural assembly; private raw source only, native proof still required.')
    parser.add_argument('--architecture-compositions-only', action='store_true', help='Separate substantial original castle/town/warehouse compositions with complete parent TRS/raw modifications; source intake only, never changes existing Finish layouts.')
    parser.add_argument('--architecture-composition', action='append', choices=['fortified-quarter','town-market','lumber-warehouse','walled-town','fishing-warehouse'], help='Explicit independent architecture subsets; omit for all five. A broken original source in one subset never becomes an optional missing mesh.')
    parser.add_argument('--market-front-only', action='store_true', help='Exact61-instance authored vendor market grouping and its dependency-closed source assets; private derived poses, never imports the vendor scene/weather.')
    parser.add_argument('--settlement-art-only', action='store_true', help='Dependency-closed real market, farm, harbor and forest underbrush art for a separate candidate finish pass.')
    parser.add_argument('--road-shader-only', action='store_true', help='Prepare a separate private vendor URP road shader review; never replace installed legacy shader.')
    args = parser.parse_args()
    if args.architecture_composition and not args.architecture_compositions_only:parser.error('--architecture-composition requires the explicit opt-in architecture scope')
    if args.record_finish_material_provenance:
        record_finish_material_provenance(args.apply)
        return
    if not (ROOT / 'ProjectSettings/ProjectVersion.txt').is_file():
        raise RuntimeError('Expected Unity workspace root')
    if sum([args.road_shader_only, args.mountain_art_only, args.settlement_art_only, args.bridge_support_only, args.market_front_only, args.finish_assemblies_only, args.architecture_compositions_only, args.terrain_palette_only]) > 1:
        raise RuntimeError('Choose one isolated intake scope')
    if args.road_shader_only:
        intake_road_shader(args.apply)
        return
    if not OWNED.is_dir() or (not (args.market_front_only or args.finish_assemblies_only or args.architecture_compositions_only or args.terrain_palette_only) and not CACHE.is_file()):
        raise RuntimeError('Required authorized owned sources are unavailable')

    # entries map original Unity asset paths to byte suppliers and original meta.
    entries: dict[str, tuple[object, bytes, str]] = {}
    index: dict[str, str] = {}
    for pack in PACKS:
        for meta in sorted((OWNED / pack).rglob('*.meta')):
            asset = Path(str(meta)[:-5])
            if not asset.is_file():
                continue
            key = 'Assets/Plugins/' + asset.relative_to(OWNED).as_posix()
            entries[key] = (asset, meta.read_bytes(), str(asset))

    # Read nested URP distribution in memory. Tar entry names are never used as
    # filesystem destinations; only validated original Unity pathname records.
    nested_hash = None
    if not (args.market_front_only or args.finish_assemblies_only or args.architecture_compositions_only or args.terrain_palette_only):
        with tarfile.open(CACHE, 'r:gz') as outer:
            members = {m.name: m for m in outer.getmembers()}
            nested = None
            for name, member in members.items():
                if name.endswith('/pathname'):
                    original = clean_package_path(outer.extractfile(member).read())
                    if '/URP_FANTASTIC_Ancient_Forest_U6-1.unitypackage' in original:
                        nested = outer.extractfile(members[name.rsplit('/', 1)[0] + '/asset']).read()
                        break
        if nested is None:
            raise RuntimeError('Owned Ancient Forest Unity 6 URP distribution missing')
        nested_hash = digest(nested)
        # Decompress once: random extractfile reads from a compressed tar otherwise
        # repeatedly inflate this large asset distribution from the beginning.
        with tarfile.open(fileobj=io.BytesIO(gzip.decompress(nested)), mode='r:') as archive:
            members = {m.name: m for m in archive.getmembers()}
            for name, member in members.items():
                if not name.endswith('/pathname'):
                    continue
                original = clean_package_path(archive.extractfile(member).read())
                folder = name.rsplit('/', 1)[0]
                if folder + '/asset' not in members or folder + '/asset.meta' not in members:
                    continue
                data = archive.extractfile(members[folder + '/asset']).read()
                meta = archive.extractfile(members[folder + '/asset.meta']).read()
                entries[original] = (data, meta, str(CACHE) + '::URP::' + original)
        del nested


    for path, (_, meta, _) in entries.items():
        match = GUID.search(meta)
        if match:
            guid = match.group(1).decode().lower()
            if guid in index and index[guid] != path:
                raise RuntimeError(f'Ambiguous owned GUID {guid}: {index[guid]}, {path}')
            index[guid] = path

    seeds = []
    def select(prefix: str, names: list[str]) -> None:
        for name in names:
            path = prefix + '/' + name
            if path not in entries:
                raise RuntimeError(f'Expected owned asset missing: {path}')
            seeds.append(path)

    # Supplied prefabs reference generated convex data whose authoring script is
    # absent from the owned source. Use the actual complete models/materials and
    # let project-owned authoring add static mesh collision without that orphan.
    select('Assets/Plugins/Synty Studios/PolygonKnights/Models', [
        'SM_Bld_Castle_Tower_01.fbx', 'SM_Bld_Castle_Tower_02.fbx',
        'SM_Bld_Castle_Tower_03.fbx', 'SM_Bld_Castle_Tower_04.fbx',
        'SM_Bld_Castle_Tower_Round_01.fbx', 'SM_Bld_Castle_Wall_01.fbx',
        'SM_Bld_Castle_Wall_Gate_01.fbx', 'SM_Bld_Castle_Iron_Gate_01.fbx',
        'SM_Bld_Rockwall_Archway_01.fbx', 'SM_Bld_Rockwall_Straight_01.fbx'])
    select('Assets/Plugins/Synty Studios/PolygonKnights/Materials', ['PolyKnights_Mat_01.mat'])
    select('Assets/Plugins/Tidal Flask Studios/Fantastic Village Pack/prefabs/buildings',
           [f'P_BLD_house_{i:02d}.prefab' for i in [1, 2, 3, 5, 7, 10, 12, 14]])
    select('Assets/Plugins/Tidal Flask Studios/Fantastic Village Pack/prefabs/props', [
        'P_PROP_bridge_wood_01.prefab', 'P_PROP_bridge_wood_04.prefab',
        'P_PROP_bridge_stone_01.prefab', 'P_PROP_crate_01.prefab',
        'P_PROP_barrel_01.prefab', 'P_PROP_hay_01.prefab', 'P_PROP_cart_01.prefab'])
    select('Assets/Plugins/Tidal Flask Studios/Fantastic Village Pack/prefabs/buildings_modules',
           ['P_BLD_windmill_sail.prefab', 'P_BLD_waterwheel.prefab'])
    select('Assets/Plugins/Tidal Flask Studios/Fantastic Village Pack/animation',
           ['CTRL_BLD_windmill_sail.controller', 'CTRL_BLD_waterwheel.controller'])
    select('Assets/Plugins/Polyart Studio/HandpaintedEnvironmentVol1/Meshes/Prefab/Cliffs',
           [f'RockFormation_{i:02d}.prefab' for i in range(1, 5)])
    select('Assets/Plugins/Polyart Studio/HandpaintedEnvironmentVol1/Meshes/Prefab/Props', ['Docks_01.prefab'])
    select('Assets/Plugins/Tarbo Studio/TARBO-TowerDefensePack/Prefabs/Crystals',
           [f'TB_Str_Crystal_{i}.prefab' for i in ['A', 'C', 'F', 'H']])
    select('Assets/Plugins/Ventuar3D/MedievalCastlePack/Prefabs/Structures/Dock Pieces',
           ['DockNoRailingStart.prefab', 'DockNoRailingConnector.prefab', 'DockEnd.prefab', 'DockSingleRailing.prefab'])
    select('Assets/Plugins/Tarbo Studio/TARBO-FantasyVillage/Prefabs/Props/Tools', ['TB_Prop_Ship.prefab'])
    forest_prefix = 'Assets/Fantastic Ancient Forest/Prefabs/'
    for path in entries:
        if path.startswith(forest_prefix) and path.endswith('.prefab') and any(
            token in path for token in ['/Trees/', '/Stones/', '/Misc/P_ENV_birdnest',
                                        '/Props/P_PROP_column', '/Mushrooms/']):
            seeds.append(path)

    if args.mountain_art_only:
        seeds = []
        select('Assets/Plugins/Polyart Studio/HandpaintedEnvironmentVol1/Meshes/Prefab/Mountains',
               ['Terrain_01.prefab', 'Terrain_02.prefab'])

    if args.settlement_art_only:
        seeds = []
        select('Assets/Plugins/Tidal Flask Studios/Fantastic Village Pack/prefabs/buildings',
               [f'P_BLD_house_{i:02d}.prefab' for i in [4, 6, 8, 9, 11, 13]])
        select('Assets/Plugins/Tidal Flask Studios/Fantastic Village Pack/prefabs/props', [
            'P_PROP_market_v01_01.prefab', 'P_PROP_market_v02_01.prefab',
            'P_PROP_market_v03_01.prefab', 'P_PROP_market_v04_01.prefab',
            'P_PROP_market_shelf_02_deco.prefab', 'P_PROP_market_shelf_03_deco.prefab',
            'P_PROP_well.prefab', 'P_PROP_fence_v01_01.prefab',
            'P_PROP_fence_v01_03.prefab', 'P_PROP_fence_door_gate.prefab',
            'P_PROP_trough_01.prefab', 'P_PROP_hay_02.prefab',
            'P_PROP_pitchfork.prefab', 'P_PROP_sack_01.prefab',
            'P_PROP_cart_02.prefab', 'P_PROP_rowboat.prefab',
            'P_PROP_streetlamp_v01_01.prefab', 'P_PROP_signpost_01.prefab',
            'P_PROP_table_01.prefab', 'P_PROP_food_pumpkin_01.prefab',
            'P_PROP_food_carrot_01.prefab', 'P_PROP_food_corn.prefab'])
        select('Assets/Fantastic Ancient Forest/Prefabs/Plants', [
            'P_ENV_bush_ancient_01.prefab', 'P_ENV_bush_ancient_02.prefab',
            'P_ENV_bush_ancient_03.prefab', 'P_ENV_grass_ancient_01.prefab',
            'P_ENV_grass_ancient_02.prefab', 'P_ENV_plant_ancient_03_01.prefab',
            'P_ENV_plant_ancient_06.prefab', 'P_ENV_plant_ancient_08.prefab',
            'P_ENV_plant_ancient_10.prefab'])
        select('Assets/Fantastic Ancient Forest/Prefabs/Flowers', [
            'P_ENV_flower_ancient_02.prefab', 'P_ENV_flower_ancient_03.prefab',
            'P_ENV_flower_ancient_05.prefab'])

    if args.bridge_support_only:
        seeds = []
        select('Assets/Plugins/Synty Studios/PolygonKnights/Models', ['SM_Bld_Castle_Pillar_01.fbx', 'SM_Prop_Beam_01.fbx'])
        select('Assets/Plugins/Synty Studios/PolygonKnights/Materials', ['PolyKnights_Mat_01.mat'])

    market_layout = None
    if args.market_front_only:
        seeds, market_layout = owned_market_layout(entries, index)

    finish_layouts = []
    if args.finish_assemblies_only:
        seeds, finish_layouts = owned_finish_assemblies(entries, index)

    architecture_layouts=[];architecture_scenes={}
    if args.architecture_compositions_only:
        from DragonKingdomAuthoredArchitecture import owned_architecture_compositions
        seeds,architecture_layouts,architecture_scenes=owned_architecture_compositions(entries,index,OWNED,DEST,ROOT,args.architecture_composition)

    if args.terrain_palette_only:
        seeds=[]
        select('Assets/Plugins/Tidal Flask Studios/Fantastic Village Pack/2d/textures/terrain_layers',['Layer_grass_01.terrainlayer'])
        select('Assets/Plugins/Polyart Studio/HandpaintedEnvironmentVol1/Terrain',['GrassGreen.terrainlayer','GrassYellow.terrainlayer','Dirt.terrainlayer','Rock.terrainlayer'])

    queue = deque(sorted(set(seeds)))
    chosen: dict[str, bytes] = {}
    missing: dict[str, list[str]] = {}
    includes_missing = []
    def source_bytes(path: str) -> bytes:
        source = entries[path][0]
        return source if isinstance(source, bytes) else source.read_bytes()

    while queue:
        path = queue.popleft()
        if path in chosen:
            continue
        data = source_bytes(path)
        chosen[path] = data
        meta = entries[path][1]
        scan = meta + (data if Path(path).suffix.lower() in TEXT else b'')
        for guid_bytes in GUID.findall(scan):
            guid = guid_bytes.decode().lower()
            if guid.startswith('0000000000000000'):
                continue
            dependency = index.get(guid)
            if dependency:
                queue.append(dependency)
            else:
                missing.setdefault(guid, []).append(path)
        if Path(path).suffix.lower() in {'.shader', '.hlsl', '.cginc', '.compute'}:
            for include in re.findall(rb'#include\s+"([^"\r\n]+)"', data):
                name = include.decode()
                relative = str(PurePosixPath(path).parent / name)
                if relative in entries:
                    queue.append(relative)
                elif not (name.startswith('Packages/') or '/' not in name):
                    includes_missing.append({'source': path, 'include': name})

    # Resolve package/installed references and forbid importing a second asset
    # with a GUID already owned by a different existing ROOT path.
    installed: dict[str, str] = {}
    lookup_roots = [ROOT / 'Assets', ROOT / 'Packages']
    package_cache = ROOT / 'Library/PackageCache'
    if package_cache.is_dir():
        lookup_roots += [p for p in package_cache.iterdir() if p.is_dir() and p.name.startswith('com.unity.')]
    wanted = set(missing) | {GUID.search(entries[p][1]).group(1).decode().lower() for p in chosen if GUID.search(entries[p][1])}
    # Native bounded search avoids tens of thousands of individual Python/NTFS
    # metadata opens. Read only exact GUID hits after rg has scanned the roots.
    query = '\n'.join('guid: ' + g for g in sorted(wanted)) + '\n'
    found = subprocess.run(['rg', '--hidden', '--no-ignore', '-l', '-F',
                            '--glob', '*.meta', '-f', '-', *map(str, lookup_roots)],
                           input=query, text=True, capture_output=True, check=False)
    if found.returncode not in (0, 1):
        raise RuntimeError('Installed GUID search failed: ' + found.stderr)
    for name in found.stdout.splitlines():
        meta = Path(name)
        if DEST in meta.parents:
            continue
        match = GUID.search(meta.read_bytes())
        if match and match.group(1).decode().lower() in wanted:
            installed[match.group(1).decode().lower()] = str(meta)[:-5]
    # These three original Village references are absent even in the complete
    # accessible owned source. They are exclusively _MetallicGlossMap entries;
    # albedo/normal/geometry dependencies must still resolve. Project-owned URP
    # material variants omit the absent optional metallic maps. Source is exact.
    optional = {}
    optional_metallic = {'7ffcbccc9791c2846a615463921c860e',
                         '61aebd76e3ea7454a85a07f19dd05669',
                         '760ad9a8e52ad6d499609265edcf624c'}
    for guid in optional_metallic:
        paths = missing.get(guid, [])
        for path in paths:
            if not path.endswith('.mat') or not re.search(
                rb'_MetallicGlossMap:\s*\r?\n\s*m_Texture:\s*\{[^}]*guid:\s*' + guid.encode(), source_bytes(path)):
                raise RuntimeError('Optional missing GUID used outside audited metallic field: ' + guid)
        if paths:
            optional[guid] = {'paths': sorted(set(paths)), 'reason': 'Original optional metallic texture absent; project-owned URP variant omits missing map'}
    if args.finish_assemblies_only:
        old_guid='b938426691c97aa4bb34c670a5f14805'
        paths=sorted(set(missing.get(old_guid, [])))
        expected='Assets/Plugins/Polyart Studio/HandpaintedEnvironmentVol1/Materials/Old/M_Bush.mat'
        if paths!=[expected] or not re.search(rb'_MainTex:\s*\r?\n\s*m_Texture:\s*\{[^}]*guid:\s*'+old_guid.encode(),source_bytes(expected)):raise RuntimeError('Exact original legacy Bush saved field required')
        optional[old_guid]={'paths':paths,'property':'_MainTex','reason':'DOCUMENTARY_SOURCE_ONLY_REQUIRES_NATIVE_SHADER_HASPROPERTY_FALSE_AND_ACTUAL_ALBEDO_TEXTURE_PROOF','nativeProofRequired':True}
    unresolved = {g: sorted(set(p)) for g, p in missing.items() if g not in installed and g not in optional}
    records = []
    conflicts = []
    for path, data in sorted(chosen.items()):
        relative = path.removeprefix('Assets/')
        destination = DEST / relative
        if DEST not in destination.resolve().parents:
            raise RuntimeError('Destination escaped private intake root')
        meta = entries[path][1]
        match = GUID.search(meta)
        guid = match.group(1).decode().lower() if match else None
        existing = installed.get(guid)
        if existing:
            conflicts.append({'guid': guid, 'source': path, 'installed': existing})
        for out, payload in [(destination, data), (Path(str(destination) + '.meta'), meta)]:
            if out.exists() and out.read_bytes() != payload:
                conflicts.append({'destination': str(out), 'reason': 'different existing bytes'})
        records.append({'source': ('OWNED/' + path.removeprefix('Assets/Plugins/')) if (args.mountain_art_only or args.settlement_art_only or args.bridge_support_only or args.market_front_only or args.finish_assemblies_only or args.architecture_compositions_only or args.terrain_palette_only) else entries[path][2], 'originalPath': path,
                        'destination': destination.relative_to(ROOT).as_posix(), 'guid': guid,
                        'bytes': len(data), 'sha256': digest(data), 'metaSha256': digest(meta)})
    report = {'schemaVersion': 1, 'mode': 'apply' if args.apply else 'dry-run',
              'nestedUrpPackageSha256': nested_hash, 'seedCount': len(set(seeds)),
              'assetCount': len(records), 'totalAssetBytes': sum(r['bytes'] for r in records),
              'unresolvedGuids': unresolved, 'unresolvedLocalIncludes': includes_missing,
              'knownOptionalSourceReferences': optional,
              'installedDependencies': {g: p for g, p in installed.items() if g in missing},
              'conflicts': conflicts, 'assets': records, 'applied': False}
    layout_payload = None
    layout_meta = None
    if market_layout is not None:
        market_layout['dependencies'] = [dict(path=r['destination'], guid=r['guid'], sha256=r['sha256'], metaSha256=r['metaSha256']) for r in records]
        provenance_path = DEST / 'DerivedVillageLayout/Material Import Provenance v1.json'
        if not provenance_path.exists() or digest(provenance_path.read_bytes()) != '4c0f984d7487a2c7728c1c05559632c3bd90eaf3467ef3a2d1a85ce92a637415':
            raise RuntimeError('Exact reviewed V201 nineteen-material native import provenance required; never infer arbitrary imported bytes.')
        provenance = json.loads(provenance_path.read_text(encoding='utf-8'))
        if len(provenance['bindings']) != 19: raise RuntimeError('Exact nineteen material import records required')
        bindings = {r['path']: r for r in provenance['bindings']}
        for dependency in market_layout['dependencies']:
            if dependency['path'] not in bindings: continue
            binding = bindings[dependency['path']]
            if dependency['sha256'] != binding['originalSha256'] or dependency['guid'] != binding['guid'] or dependency['metaSha256'] != binding['metaSha256']:
                raise RuntimeError('Original source material differs from its exact native import provenance')
            for path, expected in [(binding['snapshot'], binding['snapshotSha256']), (binding['snapshot'] + '.meta', binding['snapshotMetaSha256'])]:
                if digest((ROOT / path).read_bytes()) != expected: raise RuntimeError('Immutable original material snapshot differs')
        market_layout['materialImportProvenance'] = provenance_path.relative_to(ROOT).as_posix()
        market_layout['materialImportProvenanceSha256'] = digest(provenance_path.read_bytes())

        layout_payload = (json.dumps(market_layout, indent=2) + '\n').encode('utf-8')
        if MARKET_LAYOUT.exists() and MARKET_LAYOUT.read_bytes() != layout_payload:
            conflicts.append({'destination': MARKET_LAYOUT.relative_to(ROOT).as_posix(), 'reason': 'preserve different existing private artist layout'})
        layout_guid = uuid.uuid5(uuid.NAMESPACE_URL, 'interstate-hauler/private-artist-layout/' + MARKET_LAYOUT.relative_to(ROOT).as_posix()).hex
        layout_meta = ('fileFormatVersion: 2\nguid: ' + layout_guid + '\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n').encode('utf-8')
        layout_meta_path = Path(str(MARKET_LAYOUT) + '.meta')
        if layout_meta_path.exists() and layout_meta_path.read_bytes() != layout_meta:
            conflicts.append({'destination': layout_meta_path.relative_to(ROOT).as_posix(), 'reason': 'preserve different existing private artist metadata'})
        report['marketLayout'] = dict(path=MARKET_LAYOUT.relative_to(ROOT).as_posix(), sha256=digest(layout_payload), guid=layout_guid, metaSha256=digest(layout_meta), instances=61, prefabs=28, sourceSceneSha256=market_layout['sourceSceneSha256'])
    finish_payloads=[]
    if finish_layouts:
        for record in records:
            if not record['destination'].endswith('.mat'):continue
            path=DEST/'DerivedFinishAssemblies/OriginalMaterialSources'/(record['guid']+'.bytes')
            payload=source_bytes(record['originalPath']);guid=uuid.uuid5(uuid.NAMESPACE_URL,'interstate-hauler/private-finish-raw-material/'+record['guid']).hex
            meta=('fileFormatVersion: 2\nguid: '+guid+'\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n').encode()
            for out,data in [(path,payload),(Path(str(path)+'.meta'),meta)]:
                if out.exists() and out.read_bytes()!=data:conflicts.append({'destination':str(out.relative_to(ROOT)),'reason':'Preserve different immutable original material snapshot'})
            finish_payloads.append((path,payload,meta));report.setdefault('originalMaterialSnapshots',[]).append(dict(path=path.relative_to(ROOT).as_posix(),source=record['destination'],sourceGuid=record['guid'],sha256=digest(payload),metaSha256=digest(meta),guid=guid))
    for layout in finish_layouts:
        layout['dependencies']=[dict(path=r['destination'],guid=r['guid'],sha256=r['sha256'],metaSha256=r['metaSha256']) for r in records]
        layout['originalMaterials']=report.get('originalMaterialSnapshots',[])
        path=DEST/'DerivedFinishAssemblies'/(layout['name']+'.json');payload=(json.dumps(layout,indent=2,allow_nan=False)+'\n').encode('utf-8');guid=uuid.uuid5(uuid.NAMESPACE_URL,'interstate-hauler/private-author-assembly/'+path.relative_to(ROOT).as_posix()).hex
        meta=('fileFormatVersion: 2\nguid: '+guid+'\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n').encode()
        for out,data in [(path,payload),(Path(str(path)+'.meta'),meta)]:
            if out.exists() and out.read_bytes()!=data:conflicts.append({'destination':str(out.relative_to(ROOT)),'reason':'Preserve different original authored assembly selection'})
        finish_payloads.append((path,payload,meta));report.setdefault('authoredAssemblies',[]).append(dict(path=path.relative_to(ROOT).as_posix(),sha256=digest(payload),metaSha256=digest(meta),guid=guid,instances=len(layout['instances']),sourceSceneSha256=layout['sourceSceneSha256']))
    architecture_payloads=[];architecture_folder_metadata=[]
    if architecture_layouts:
        report['architectureSelectedCompositions']=[layout['compositionKey']for layout in architecture_layouts]
        architecture_folder=DEST/'DerivedArchitectureLayoutsV1'
        for folder in (architecture_folder,architecture_folder/'OriginalSceneSources',architecture_folder/'OriginalMaterialSources'):
            guid=uuid.uuid5(uuid.NAMESPACE_URL,'interstate-hauler/private-architecture-v1-folder/'+folder.relative_to(ROOT).as_posix()).hex
            meta=('fileFormatVersion: 2\nguid: '+guid+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n').encode();path=Path(str(folder)+'.meta')
            if path.exists()and path.read_bytes()!=meta:conflicts.append(dict(destination=path.relative_to(ROOT).as_posix(),reason='Preserve different architecture folder metadata'))
            architecture_folder_metadata.append((path,meta));report.setdefault('architectureFolderMetadata',[]).append(dict(path=path.relative_to(ROOT).as_posix(),guid=guid,sha256=digest(meta)))
        def architecture_payload(path,payload,kind):
            guid=uuid.uuid5(uuid.NAMESPACE_URL,'interstate-hauler/private-architecture-v1/'+path.relative_to(ROOT).as_posix()).hex
            meta=('fileFormatVersion: 2\nguid: '+guid+'\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n').encode()
            for out,data in ((path,payload),(Path(str(path)+'.meta'),meta)):
                if out.exists()and out.read_bytes()!=data:conflicts.append(dict(destination=out.relative_to(ROOT).as_posix(),reason='Preserve different opt-in architecture source/customization'))
            architecture_payloads.append((path,payload,meta))
            return dict(path=path.relative_to(ROOT).as_posix(),sha256=digest(payload),guid=guid,metaSha256=digest(meta),kind=kind)
        scene_snapshots={}
        for source_sha,scene in sorted(architecture_scenes.items()):
            raw=architecture_payload(architecture_folder/'OriginalSceneSources'/(source_sha+'.bytes'),scene['raw'],'IMMUTABLE_ORIGINAL_SCENE_BYTES_NOT_IMPORTED_AS_SCENE')
            raw['source']=scene['source'];raw['originalMetadata']=architecture_payload(architecture_folder/'OriginalSceneSources'/(source_sha+'.source-meta.bytes'),scene['meta'],'IMMUTABLE_ORIGINAL_SCENE_METADATA_BYTES')
            scene_snapshots[source_sha]=raw
        materials=[]
        for record in records:
            if not record['destination'].endswith('.mat'):continue
            snapshot=architecture_payload(architecture_folder/'OriginalMaterialSources'/(record['guid']+'.bytes'),source_bytes(record['originalPath']),'IMMUTABLE_RAW_MATERIAL_PENDING_NATIVE_IMPORT_PROOF')
            snapshot.update(source=record['destination'],sourceGuid=record['guid'],sourceMetaSha256=record['metaSha256']);materials.append(snapshot)
        for layout in architecture_layouts:
            layout['sourceSceneSnapshot']=scene_snapshots[layout['sourceSceneSha256']]
            layout['dependencies']=[dict(path=r['destination'],guid=r['guid'],sha256=r['sha256'],metaSha256=r['metaSha256'])for r in records]
            layout['originalMaterials']=materials
            payload=(json.dumps(layout,indent=2,allow_nan=False)+'\n').encode('utf-8')
            descriptor=architecture_payload(architecture_folder/(layout['name']+'.json'),payload,'SOURCE_COMPOSITION_NOT_NATIVE_OR_VISUAL_ACCEPTANCE')
            descriptor.update(instances=layout['instanceCount'],prefabs=layout['prefabCount'],sourceSceneSha256=layout['sourceSceneSha256'])
            report.setdefault('architectureCompositions',[]).append(descriptor)
        report['architectureOriginalScenes']=list(scene_snapshots.values());report['architectureOriginalMaterials']=materials
    if args.apply and not (unresolved or includes_missing or conflicts):
        for path, data in sorted(chosen.items()):
            destination = DEST / path.removeprefix('Assets/')
            destination.parent.mkdir(parents=True, exist_ok=True)
            for out, payload in [(destination, data), (Path(str(destination) + '.meta'), entries[path][1])]:
                if not out.exists():
                    out.write_bytes(payload)
                if out.read_bytes() != payload:
                    raise RuntimeError(f'Post-copy hash verification failed: {out}')
        if layout_payload is not None:
            MARKET_LAYOUT.parent.mkdir(parents=True, exist_ok=True)
            if not MARKET_LAYOUT.exists(): MARKET_LAYOUT.write_bytes(layout_payload)
            if MARKET_LAYOUT.read_bytes() != layout_payload: raise RuntimeError('Private source layout bytes changed while applying intake')
            layout_meta_path = Path(str(MARKET_LAYOUT) + '.meta')
            if not layout_meta_path.exists(): layout_meta_path.write_bytes(layout_meta)
            if layout_meta_path.read_bytes() != layout_meta: raise RuntimeError('Private artist metadata changed while applying intake')
        for path,payload,meta in finish_payloads:
            path.parent.mkdir(parents=True,exist_ok=True)
            for out,data in [(path,payload),(Path(str(path)+'.meta'),meta)]:
                if not out.exists():out.write_bytes(data)
                if out.read_bytes()!=data:raise RuntimeError('Exact authored source preservation failed')
        for path,payload,meta in architecture_payloads:
            path.parent.mkdir(parents=True,exist_ok=True)
            for out,data in ((path,payload),(Path(str(path)+'.meta'),meta)):
                if not out.exists():out.write_bytes(data)
                if out.read_bytes()!=data:raise RuntimeError('Exact opt-in architecture preservation failed')
        for path,payload in architecture_folder_metadata:
            path.parent.mkdir(parents=True,exist_ok=True)
            if not path.exists():path.write_bytes(payload)
            if path.read_bytes()!=payload:raise RuntimeError('Exact owned architecture folder metadata preservation failed')
        report['applied'] = True
    architecture_report='ArchitectureAuthoredCompositionsIntake'+('-'+'-'.join(report['architectureSelectedCompositions'])if args.architecture_composition else'')+'.json'if args.architecture_compositions_only else None
    report_path = REPORT.with_name(architecture_report) if args.architecture_compositions_only else REPORT.with_name('HandpaintedTerrainPaletteIntake.json') if args.terrain_palette_only else REPORT.with_name('FinishAuthoredAssembliesIntake.json') if args.finish_assemblies_only else REPORT.with_name('MarketFrontArtIntake.json') if args.market_front_only else REPORT.with_name('BridgeSupportArtIntake.json') if args.bridge_support_only else REPORT.with_name('SettlementForestArtIntake.json') if args.settlement_art_only else REPORT.with_name('MountainArtIntake.json') if args.mountain_art_only else REPORT
    if args.mountain_art_only or args.settlement_art_only or args.bridge_support_only or args.market_front_only or args.finish_assemblies_only or args.architecture_compositions_only or args.terrain_palette_only:
        report['scope'] = 'DEPENDENCY_CLOSED_REAL_MOUNTAIN_CANDIDATES_NOT_PLACED_OR_VISUALLY_ACCEPTED'
        report['installedDependencies'] = {g: str(Path(p).relative_to(ROOT)).replace('\\', '/') if Path(p).is_relative_to(ROOT) else 'INSTALLED_PACKAGE/' + Path(p).name for g, p in report['installedDependencies'].items()}
    if args.terrain_palette_only:
        report['scope']='EXACT_SOURCE_AUTHOR_TERRAIN_PALETTE_ONLY_NO_WORLD_SURFACE_ADOPTION'
    if args.architecture_compositions_only:
        report['scope']='EXACT_SUBSTANTIAL_CASTLE_TOWN_WAREHOUSE_COMPOSITION_SOURCE_INTAKE_PENDING_NATIVE_PROOF_NO_EXISTING_LAYOUT_REGENERATION'
    if args.finish_assemblies_only:
        report['scope']='EXACT123_ARTIST_COURT_AND_NATURAL_SOURCE_INSTANCES_RAW_PRIVATE_INTAKE_NOT_NATIVE_OR_VISUAL_ACCEPTANCE'
    if args.settlement_art_only:
        report['scope'] = 'DEPENDENCY_CLOSED_REAL_SETTLEMENT_AND_UNDERBRUSH_ART_NOT_PLACED_OR_VISUALLY_ACCEPTED'
    if args.market_front_only:
        report['scope'] = 'EXACT_ARTIST_MARKET_LAYOUT_AND_DEPENDENCIES_PRIVATE_NOT_PLACED_OR_RENDERED'
    if args.bridge_support_only:
        report['scope'] = 'DEPENDENCY_CLOSED_ACTUAL_PILLAR_AND_BEAM_MODELS_NOT_STRUCTURAL_OR_VISUAL_ACCEPTANCE'
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({k: v for k, v in report.items() if k not in {'assets', 'installedDependencies','architectureOriginalMaterials','architectureOriginalScenes'}}, indent=2))
    if unresolved or includes_missing or conflicts:
        raise SystemExit(2)


if __name__ == '__main__':
    main()
