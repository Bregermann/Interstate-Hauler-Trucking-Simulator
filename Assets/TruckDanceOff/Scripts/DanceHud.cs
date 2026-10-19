using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using LWS.TruckSports;
namespace LWS.TruckDanceOff {
public sealed class DanceHud:MonoBehaviour {
 [System.Serializable]public sealed class Strip{public RectTransform root;public Image[] receptors;public RectTransform[] notes,tails;public Image[] faces,tailFaces;public TMP_Text title,judgment,combo,routine,controls;public CanvasGroup group;}
 public Sprite[] arrows;readonly float[] gradeAt=new float[4];public Strip[] strips;public TMP_Text song,time;public RectTransform progress;public GameObject surface;
 static readonly Color[] laneColors={new Color(1,.25f,.65f),new Color(.15f,.8f,1),new Color(.5f,1,.3f),new Color(1,.7f,.13f)};
 public void Rebuild(DanceMatch m){surface.SetActive(true);for(int i=0;i<strips.Length;i++){var s=strips[i];bool active=i<m.setup.count;s.root.gameObject.SetActive(active);if(!active)continue;s.root.anchoredPosition=new Vector2((i-(m.setup.count-1)*.5f)*370,-310);s.title.text=m.setup.players[i].name;s.title.color=m.setup.players[i].color;s.controls.text=m.ControlCaption(i);for(int lane=0;lane<4;lane++)s.receptors[lane].color=laneColors[lane];foreach(var n in s.notes)n.gameObject.SetActive(false);foreach(var n in s.tails)n.gameObject.SetActive(false);}song.text=m.Song.title.ToUpperInvariant()+" <size=60%> / "+m.Song.genre.ToUpperInvariant()+" · "+m.Song.bpm+" BPM</size>";}
 public void Draw(DanceMatch m){if(m.Rules==null)return;double now=m.SongTime;time.text=m.Status;var scale=progress.localScale;scale.x=Mathf.Clamp01((float)(now/m.Song.duration));progress.localScale=scale;
  for(int i=0;i<m.setup.count;i++){var s=strips[i];var rule=m.Rules[i];s.judgment.text=now>=m.Song.freestyleStart&&now<m.Song.freestyleEnd?"FREESTYLE":rule.LastGrade==DanceGrade.None?"FEEL THE BEAT":rule.LastGrade.ToString().ToUpperInvariant();s.judgment.color=rule.LastGrade==DanceGrade.Miss?new Color(1,.35f,.4f):Color.white;s.combo.text=(m.total[i]+(m.Phase==DancePhase.RoundResult||m.Phase==DancePhase.Complete?0:rule.Score)).ToString("N0")+"  <size=65%> / COMBO "+rule.Combo+"</size>";s.routine.text=m.dancers[i].Routine;var color=s.judgment.color;color.a=now>=m.Song.freestyleStart&&now<m.Song.freestyleEnd?1:Mathf.Clamp01(1-(Time.unscaledTime-gradeAt[i])/.9f);s.judgment.color=color;
   float pulse=1+Mathf.Max(0,Mathf.Cos((float)(now/m.Song.Beat)*Mathf.PI*2))*.06f;foreach(var r in s.receptors)r.transform.localScale=Vector3.one*pulse;
   int used=0;for(int n=0;n<rule.Count&&used<s.notes.Length;n++){var note=rule.Note(n);if(!rule.Pending(n)||note.time+note.duration<now-.18||note.time>now+2.45)continue;for(int lane=0;lane<4&&used<s.notes.Length;lane++){if((note.mask&(1<<lane))==0)continue;var rect=s.notes[used];rect.gameObject.SetActive(true);float y=-80+Mathf.Clamp((float)(note.time-now),-.14f,2.45f)*60;rect.anchoredPosition=new Vector2((lane-1.5f)*68,y);s.faces[used].color=laneColors[lane];s.faces[used].sprite=arrows[lane];var tail=s.tails[used];bool hold=note.duration>0;tail.gameObject.SetActive(hold);if(hold){float end=-80+(float)(note.time+note.duration-now)*60;float lower=Mathf.Max(-80,y);tail.anchoredPosition=new Vector2((lane-1.5f)*68,(lower+end)*.5f);tail.sizeDelta=new Vector2(12,Mathf.Max(0,end-lower));s.tailFaces[used].color=laneColors[lane]*new Color(1,1,1,.45f);}used++;}}
   for(int n=used;n<s.notes.Length;n++){s.notes[n].gameObject.SetActive(false);s.tails[n].gameObject.SetActive(false);}
  }
 }
 public void Judged(int slot,DanceGrade grade){gradeAt[slot]=Time.unscaledTime;var t=strips[slot].judgment.transform;t.DOKill();t.localScale=Vector3.one;t.DOPunchScale(Vector3.one*.14f,.25f,2,.4f).SetUpdate(true).SetLink(t.gameObject);}
 public void Hide(){if(surface!=null)surface.SetActive(false);}
}
}
