using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckFareFight {
 // Optional match-end recorded highlight; never interrupts live competition.
 public sealed class FareFightReview:MonoBehaviour {
  public FareFightMatch match;public SportsMotionReplay record;public bool Playing{get;private set;}
  Renderer[][] renderers;bool[][] visible;int focus;
  public void Ensure(){if(record!=null)return;record=gameObject.AddComponent<SportsMotionReplay>();record.capacity=240;record.interval=.1f;record.rolling=true;record.maximumReplayAge=25;record.sources=new Transform[4];record.ghosts=new Transform[4];renderers=new Renderer[4][];visible=new bool[4][];
   for(int i=0;i<4;i++){record.sources[i]=match.trucks[i].transform;var ghost=SportsReplayVisuals.Copy(record.sources[i]);ghost.name="Recorded Fare Fight tractor "+i;record.ghosts[i]=ghost.transform;ghost.SetActive(false);renderers[i]=match.trucks[i].GetComponentsInChildren<Renderer>(true);visible[i]=new bool[renderers[i].Length];}
  }
  public void ResetRecord(){End(false);Ensure();record.ResetRecord();}
  public void Capture(float time){if(record!=null&&!Playing)record.Record(time,(1<<match.setup.count)-1);}
  public void Begin(){if(!match.Finished||record==null||record.Duration<1)return;focus=0;for(int i=0;i<match.setup.count;i++)if(match.rules.winners[i]){focus=i;break;}for(int i=0;i<4;i++)foreach(var view in match.drivingViews.rally.views)view.view.enabled=false;for(int i=0;i<4;i++)for(int j=0;j<renderers[i].Length;j++){visible[i][j]=renderers[i][j].forceRenderingOff;renderers[i][j].forceRenderingOff=true;}match.broadcast.view.enabled=true;match.broadcast.view.rect=new Rect(0,0,1,1);match.cameras=new[]{match.broadcast.view};match.menus.Show(SportsEventScreen.Play);record.Begin(Mathf.Max(0,record.Duration-18));Playing=true;Time.timeScale=0;match.atmosphere.Pause(true);}
  public void Tick(){if(!Playing)return;if(record.Step(Time.unscaledDeltaTime)){End();return;}var position=record.Position(focus);Vector3 offset=record.Angle==0?new Vector3(16,10,-20):record.Angle==1?new Vector3(-15,5,8):new Vector3(0,42,-5);match.broadcast.Pose(position,offset,65);}
  public void Action(string key){if(key=="watchfare"){Begin();return;}if(!Playing)return;if(key=="replayskip")End();else if(key=="replayangle")record.Angle=(record.Angle+1)%3;else if(key=="replayspeed")record.Speed=record.Speed==1?.35f:record.Speed<1?2:1;else if(key=="replayrewind")record.Seek(Mathf.Max(0,record.Duration-18));}
  public void End(bool result=true){if(!Playing)return;record.End();for(int i=0;i<4;i++)for(int j=0;j<renderers[i].Length;j++)if(renderers[i][j]!=null)renderers[i][j].forceRenderingOff=visible[i][j];Playing=false;Time.timeScale=1;match.atmosphere.Pause(false);match.drivingViews.Layout();if(result&&match.Finished)match.menus.Show(SportsEventScreen.Result);}
  void OnDisable(){End(false);}void OnDestroy(){if(record!=null&&record.ghosts!=null)foreach(var ghost in record.ghosts)if(ghost!=null)Destroy(ghost.gameObject);}
 }
}
