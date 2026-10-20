"""Original nonverbal dealer cues using the existing local WorkHere engine.

No reference voice, voice cloning, package installation or model download.
Existing model weights/default conditioning and operation lock are reused.
"""
from pathlib import Path
import os,sys,json,hashlib
os.environ['HF_HUB_OFFLINE']='1'
os.environ['HF_HUB_DISABLE_TELEMETRY']='1'
sys.dont_write_bytecode=True
work=Path('F:/Codexprojects/MyVoiceForRecording/WorkHere')
sys.path.insert(0,str(work))
from app.engine import Engine
from app.config import parameters
from app.audio import atomic_wav
from app.storage import operation_lock
import numpy as np

root=Path(__file__).resolve().parents[2]
out=root/'Assets/TruckPoker/Audio';out.mkdir(parents=True,exist_ok=True)
engine=Engine();records=[]
with operation_lock():
 for index,seed in enumerate([107,207,307]):
  target=out/('Dealer giggle '+str(index+1)+'.wav')
  if target.exists():continue
  audio,rate,provenance=engine.generate('[chuckle]',{'model':'turbo','device':'auto','parameters':parameters('turbo','Lecture Neutral')},seed,builtin=True)
  audio=np.asarray(audio,dtype=np.float32);active=np.flatnonzero(np.abs(audio)>.003)
  if not len(active):raise RuntimeError('Silent nonverbal generation')
  pad=round(rate*.06);audio=audio[max(0,int(active[0])-pad):min(len(audio),int(active[-1])+pad)]
  if len(audio)/rate>3.5:raise RuntimeError('Reaction is too long: inspect before using')
  audio*=.72/max(.001,float(np.max(np.abs(audio))))
  atomic_wav(target,audio,rate)
  records.append({'asset':target.relative_to(root).as_posix(),'prompt':'[chuckle]','seed':seed,'referenceVoice':None,'conditioning':'installed model default; no cloned voice','seconds':len(audio)/rate,'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'engine':provenance})
  print(target.name,round(len(audio)/rate,2),'seconds',flush=True)
engine.unload()
if records:(out/'Dealer giggle provenance.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
