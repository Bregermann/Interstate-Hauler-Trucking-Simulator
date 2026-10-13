import io
import json
from pathlib import Path
import tarfile
import tempfile
import unittest
import InspectUnityPackage as api

ROOT='a'*32
MATERIAL='b'*32
SCRIPT='c'*32

def archive(path,script=False):
    with tarfile.open(path,'w:gz') as target:
        values={ROOT:{'pathname':b'Assets/Reviewed/Tree.prefab\n00','asset':('%YAML 1.1\nmaterial: {guid: '+MATERIAL+'}\n'+('script: {guid: '+SCRIPT+'}' if script else '')).encode()},
            MATERIAL:{'pathname':b'Assets/Reviewed/Bark.mat','asset':b'%YAML 1.1\nshader: {guid: 0000000000000000f000000000000000}'},
            SCRIPT:{'pathname':b'Assets/Reviewed/Editor/Framework.cs','asset':b'// excluded framework'}}
        for guid,parts in values.items():
            for name,data in parts.items():
                info=tarfile.TarInfo(guid+'/'+name);info.size=len(data);target.addfile(info,io.BytesIO(data))
        bad=tarfile.TarInfo(ROOT+'/../../outside.txt');bad.size=3;target.addfile(bad,io.BytesIO(b'bad'))

class PackageInspectorTests(unittest.TestCase):
    def test_unsafe_asset_path_is_excluded(self):
        with tempfile.TemporaryDirectory() as folder:
            source=Path(folder)/'source.unitypackage'
            with tarfile.open(source,'w:gz') as target:
                data=b'Assets/../outside.prefab';info=tarfile.TarInfo(ROOT+'/pathname');info.size=len(data);target.addfile(info,io.BytesIO(data))
            self.assertTrue(api.inspect(source)['entries'][0]['excluded'])
    def test_unity_asset_store_extra_gzip_header(self):
        with tempfile.TemporaryDirectory() as folder:
            source=Path(folder)/'source.unitypackage';archive(source)
            data=bytearray(source.read_bytes());data[3]|=4
            extra=b'A$'+(len(b'{"link":{"id":"166252"}}')).to_bytes(2,'little')+b'{"link":{"id":"166252"}}'
            source.write_bytes(data[:10]+len(extra).to_bytes(2,'little')+extra+data[10:])
            self.assertEqual(3,len(api.inspect(source)['entries']))
    def test_dependency_closed_subset_excludes_scripts_and_unsafe_members(self):
        with tempfile.TemporaryDirectory() as folder:
            source=Path(folder)/'source.unitypackage';archive(source)
            before=source.read_bytes();index=api.inspect(source)
            self.assertEqual(3,len(index['entries']))
            output=Path(folder)/'subset.unitypackage';api.select(index,{'paths':['Assets/Reviewed/Tree.prefab']},output)
            self.assertEqual(before,source.read_bytes())
            with tarfile.open(output,'r:gz') as result:
                names=result.getnames();self.assertTrue(all(n.startswith((ROOT,MATERIAL)) for n in names));self.assertFalse(any('..' in n for n in names))
            manifest=json.loads(output.with_suffix('.manifest.json').read_text())
            self.assertEqual('STAGED - NOT IMPORTED',manifest['status'])
            self.assertEqual(2,len(manifest['import_paths']))
    def test_art_with_framework_dependency_fails_closed(self):
        with tempfile.TemporaryDirectory() as folder:
            source=Path(folder)/'source.unitypackage';archive(source,script=True)
            with self.assertRaisesRegex(ValueError,'excluded content'):
                api.select(api.inspect(source),{'paths':['Assets/Reviewed/Tree.prefab']},Path(folder)/'subset.unitypackage')
    def test_unresolved_dependency_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            source=Path(folder)/'source.unitypackage';archive(source)
            index=api.inspect(source);index['entries']=[e for e in index['entries'] if e['guid']!=MATERIAL]
            with self.assertRaisesRegex(ValueError,'Unresolved dependencies'):
                api.select(index,{'paths':['Assets/Reviewed/Tree.prefab']},Path(folder)/'subset.unitypackage')
    def test_source_archive_cannot_be_overwritten(self):
        with tempfile.TemporaryDirectory() as folder:
            source=Path(folder)/'source.unitypackage';archive(source)
            with self.assertRaisesRegex(ValueError,'overwrite source'):
                api.select(api.inspect(source),{'paths':['Assets/Reviewed/Tree.prefab']},source)

if __name__=='__main__':unittest.main()
