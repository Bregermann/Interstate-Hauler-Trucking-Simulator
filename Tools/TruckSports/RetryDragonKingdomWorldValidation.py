"""Preserve a failed new world trial, then run the original physical gates.

Only generated output absent from both the previous frozen input and ROOT is
moved. Existing meshes, designer assets and accepted worlds are never removed.
"""
import argparse,hashlib,json,shutil,subprocess,sys,xml.etree.ElementTree as ET
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/TruckSports'))
from WorkspaceProcessSafety import require_idle_workspace

def main():
    parser=argparse.ArgumentParser();parser.add_argument('prior');parser.add_argument('tag');args=parser.parse_args()
    for value in [args.prior,args.tag]:assert value and all(c.isalnum() or c in '-_' for c in value)
    print(require_idle_workspace(ROOT),flush=True)
    clean=ROOT/'.sports-validation/TruckSportsPublicValidation';base=clean/'Assets/TruckDragonKingdom'
    prior=ROOT/'Logs/TruckSports/DragonKingdom'/args.prior
    assert int(ET.parse(prior/'tests.xml').getroot().attrib['failed'])>0,'Only a genuinely failed world trial can be moved aside.'
    frozen={item['path'] for item in json.loads((prior/'input-manifest.json').read_text(encoding='utf-8'))}
    for relative in ['Prefabs/Shared Dragon Kingdom.prefab','Scenes/DragonKingdomPreview.unity']:
        assert not (base/relative).exists() and not (ROOT/'Assets/TruckDragonKingdom'/relative).exists(),'Preserve an accepted existing world.'
    backup=ROOT/'Logs/TruckSports/DragonKingdom'/('FailedTrialBefore-'+args.tag)
    assert not backup.exists(),'Preserve previous recovery receipts; use a new tag.'
    assert not (ROOT/'Logs/TruckSports/DragonKingdom'/args.tag).exists(),'Preserve earlier native attempts.'
    pending=[]
    for folder in ['Terrain','Models']:
        pending+=list((base/folder).glob('*.asset'))+list((base/folder).glob('*.asset.meta'))
    for relative in ['Scenes/DragonKingdomFailedNativePreview.unity','Scenes/DragonKingdomFailedNativePreview.unity.meta','Configuration/Dragon Kingdom.asset','Configuration/Dragon Kingdom.asset.meta']:
        if (base/relative).exists():pending.append(base/relative)
    reviewed=[]
    for source in pending:
        assert source.is_file() and not source.is_symlink() and source.resolve().is_relative_to(base.resolve())
        relative=source.relative_to(clean)
        assert relative.as_posix() not in frozen and not (ROOT/relative).exists(),'Preserve authored original: '+str(relative)
        reviewed.append((source,relative,hashlib.sha256(source.read_bytes()).hexdigest()))
    backup.mkdir();facts=[]
    for source,relative,digest in reviewed:
        assert hashlib.sha256(source.read_bytes()).hexdigest()==digest,'Trial output changed after review.'
        target=backup/relative;target.parent.mkdir(parents=True,exist_ok=True);shutil.move(str(source),str(target))
        assert hashlib.sha256(target.read_bytes()).hexdigest()==digest
        facts.append(dict(path=relative.as_posix(),sha256=digest))
    (backup/'manifest.json').write_text(json.dumps(dict(scope='Only failed new generated trial output, absent from frozen input and ROOT. No accepted/designer asset removed.',prior=args.prior,files=facts),indent=2),encoding='utf-8')
    print('Preserved failed generated trial files',len(facts),flush=True)
    command=[sys.executable,'-B','-X','utf8',str(ROOT/'Tools/TruckSports/RunDragonKingdomValidation.py'),args.tag,'--tests','DragonKingdomWorldAuthoringTests.FullOwnedWorldBuildSavesOnlyAfterOriginalPhysicalGatesPass','--platform','EditMode','--timeout','900']
    for mode in ['TruckDragonEscape','TruckHoardHeist','TruckDragonEggDelivery','TruckDragonRiders']:command+=['--include-mode',mode]
    code=subprocess.run(command,cwd=ROOT).returncode
    if code==0:subprocess.run([sys.executable,'-B','-X','utf8',str(ROOT/'Tools/TruckSports/PreserveDragonKingdomAuthoring.py'),args.tag,'--scope','all'],cwd=ROOT,check=True)
    raise SystemExit(code)

if __name__=='__main__':main()
