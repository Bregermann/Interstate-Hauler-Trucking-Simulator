"""Read owned Unity packages and stage dependency-closed art subsets for Unity import."""
import argparse
import gzip
import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import struct
import subprocess
import tarfile
import tempfile

GUID=re.compile(rb'guid:\s*([0-9a-f]{32})',re.I)
FORBIDDEN={'.cs','.dll','.asmdef','.asmref','.unity','.inputactions','.rsp','.exe','.bat','.ps1'}
ART={'.prefab','.fbx','.obj','.blend','.mat','.png','.jpg','.jpeg','.tga','.tif','.tiff','.psd','.exr','.hdr','.asset','.wav','.mp3','.ogg','.aif','.aiff','.anim','.controller','.shader','.shadergraph','.shadersubgraph','.compute','.ttf','.otf',''}

def inspect(package):
    records={}
    # gzip handles Unity Asset Store's FEXTRA metadata header correctly; tarfile's
    # streaming gzip reader misinterprets that header on the bundled Python version.
    with gzip.open(package,'rb') as compressed,tarfile.open(fileobj=compressed,mode='r|') as archive:
        for item in archive:
            if not item.isfile():continue
            parts=PurePosixPath(item.name).parts
            if len(parts)!=2 or not re.fullmatch('[0-9a-f]{32}',parts[0],re.I):continue
            ident,kind=parts;ident=ident.lower()
            entry=records.setdefault(ident,{'guid':ident,'path':None,'bytes':0,'references':[]})
            if kind=='pathname':
                # Current Unity packages append a second-line pathname flag (for example 00).
                entry['path']=archive.extractfile(item).read().decode('utf-8-sig').splitlines()[0].rstrip('\0')
            elif kind=='asset':
                entry['bytes']=item.size
                if item.size<=8*1024*1024:
                    data=archive.extractfile(item).read()
                    data=data.removeprefix(b'\xef\xbb\xbf')
                    if data.startswith((b'%YAML',b'---')):entry['references']+=list({m.decode().lower() for m in GUID.findall(data)})
                    elif data.lstrip().startswith(b'{'):
                        # Shader Graph stores asset references inside serialized JSON values.
                        def collect(value):
                            if isinstance(value,dict):
                                for key,child in value.items():
                                    if key=='guid' and isinstance(child,str) and re.fullmatch('[0-9a-f]{32}',child,re.I):
                                        entry['references'].append(child.lower())
                                    else:collect(child)
                            elif isinstance(value,list):
                                for child in value:collect(child)
                            elif isinstance(value,str) and value.lstrip().startswith('{'):
                                try:collect(json.loads(value))
                                except (ValueError,TypeError):pass
                        decoder=json.JSONDecoder();text=data.decode('utf-8-sig');offset=0
                        while offset<len(text):
                            offset+=len(text[offset:])-len(text[offset:].lstrip())
                            try:value,end=decoder.raw_decode(text,offset)
                            except ValueError:break
                            collect(value);offset=end
                    elif len(data)>=20 and 9<=struct.unpack('>I',data[8:12])[0]<22 and struct.unpack('>I',data[4:8])[0]==len(data) and 0<struct.unpack('>I',data[:4])[0]<len(data):
                        reader=Path(r'C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Data\Tools\binary2text.exe')
                        if not reader.exists():raise ValueError('Unity binary2text is required to inspect binary serialized art dependencies.')
                        with tempfile.TemporaryDirectory(prefix='edenborough-art-') as directory:
                            source=Path(directory)/'asset';dump=Path(directory)/'asset.txt';source.write_bytes(data)
                            subprocess.run([str(reader),str(source),str(dump),'-largebinaryhashonly'],check=True,capture_output=True)
                            references=dump.read_text(encoding='utf-8-sig').split('\nID:',1)[0]
                            entry['references']+=re.findall(r'GUID:\s*([0-9a-f]{32})',references,re.I)
            elif kind=='asset.meta':
                entry['references']+=list({m.decode().lower() for m in GUID.findall(archive.extractfile(item).read()) if m.decode().lower()!=ident})
    entries=[]
    for record in records.values():
        path=record['path']
        if not path:continue
        record['references']=sorted(set(record['references']))
        record['kind']=PurePosixPath(path).suffix.lower()
        record['excluded']=record['kind'] in FORBIDDEN or not path.startswith('Assets/') or '\\' in path or ':' in path or any(p in ('..','.','Editor','ProjectSettings','Packages') for p in path.split('/'))
        entries.append(record)
    return {'source':str(package.resolve()),'source_bytes':package.stat().st_size,'source_mtime_ns':package.stat().st_mtime_ns,'entries':sorted(entries,key=lambda e:e['path'])}

def select(index,selection,output):
    entries={e['guid']:e for e in index['entries']};bypath={e['path']:e for e in entries.values()}
    external={g.lower() for g in selection.get('existing_project_guids',[])}
    chosen=set();pending=[bypath[p]['guid'] for p in selection['paths']];missing=set()
    while pending:
        ident=pending.pop()
        if ident in chosen or ident in external or ident.startswith('0000000000000000'):continue
        entry=entries.get(ident)
        if entry is None:missing.add(ident);continue
        if entry['excluded'] or entry['kind'] not in ART:raise ValueError('Selected art depends on excluded content: '+entry['path'])
        chosen.add(ident);pending.extend(entry['references'])
    if missing:raise ValueError('Unresolved dependencies: '+', '.join(sorted(missing)))
    parents={str(parent) for ident in chosen for parent in PurePosixPath(entries[ident]['path']).parents}
    chosen.update(e['guid'] for e in entries.values() if e['kind']=='' and e['path'] in parents)
    package=Path(index['source'])
    if package.stat().st_size!=index['source_bytes'] or package.stat().st_mtime_ns!=index['source_mtime_ns']:raise ValueError('Source archive changed; inspect again.')
    if output.resolve()==package.resolve():raise ValueError('Cannot overwrite source archive.')
    output.parent.mkdir(parents=True,exist_ok=True)
    with gzip.open(package,'rb') as compressed,tarfile.open(fileobj=compressed,mode='r|') as source,tarfile.open(output,'w:gz') as target:
        for member in source:
            parts=PurePosixPath(member.name).parts
            if len(parts)==1 and parts[0].lower() in chosen and member.isdir():
                target.addfile(member)
            elif len(parts)==2 and parts[0].lower() in chosen and parts[1] in ('pathname','asset','asset.meta') and member.isfile():
                target.addfile(member,source.extractfile(member))
    with output.open('rb') as stream:digest=hashlib.file_digest(stream,'sha256').hexdigest()
    manifest={'source_package':str(package),'staging_package':str(output.resolve()),'selected_roots':selection['paths'],'import_paths':[entries[g]['path'] for g in sorted(chosen)],'bytes':output.stat().st_size,'sha256':digest,'status':'STAGED - NOT IMPORTED'}
    output.with_suffix('.manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    print(json.dumps({'selected_entries':len(chosen),'staged_bytes':manifest['bytes'],'status':manifest['status']}))

def main():
    parser=argparse.ArgumentParser();sub=parser.add_subparsers(dest='command',required=True)
    scan=sub.add_parser('inspect');scan.add_argument('package',type=Path);scan.add_argument('output',type=Path)
    stage=sub.add_parser('select');stage.add_argument('index',type=Path);stage.add_argument('selection',type=Path);stage.add_argument('output',type=Path)
    args=parser.parse_args()
    if args.command=='inspect':
        result=inspect(args.package);args.output.parent.mkdir(parents=True,exist_ok=True)
        args.output.write_text(json.dumps(result,indent=2),encoding='utf-8')
        print(json.dumps({'entries':len(result['entries']),'prefabs':sum(e['kind']=='.prefab' for e in result['entries']),'bytes':result['source_bytes']}))
    else:select(json.loads(args.index.read_text(encoding='utf-8')),json.loads(args.selection.read_text(encoding='utf-8')),args.output)

if __name__=='__main__':main()
