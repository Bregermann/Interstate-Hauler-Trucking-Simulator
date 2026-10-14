using UnityEngine;
namespace LWS.TruckWallball {
public sealed class WallballPresentation:MonoBehaviour {
 public WallballMatch match;public Camera cameraTarget;public Transform shadow;public LineRenderer landing;public TextMesh[] labels;public Transform[] markers;
 Material ballPaint;Vector3[] circle=new Vector3[40];float next;int colorOwner=-99;
 void Awake(){ballPaint=new Material(match.ball.visual.sharedMaterial);match.ball.visual.sharedMaterial=ballPaint;}
 void LateUpdate(){var p=match.ball.body.position;bool live=match.InMatch&&match.menus.Screen==WallballScreen.Play;shadow.gameObject.SetActive(live);shadow.position=new Vector3(p.x,.025f,p.z);shadow.localScale=new Vector3(1+Mathf.Min(2,p.y)*.08f,.01f,1+Mathf.Min(2,p.y)*.08f);
  int owner=match.rules.Eligible>=0?match.rules.Eligible:match.rules.Hitter;var color=owner>=0?match.players[owner].color:new Color(.95f,.95f,.4f);if(owner!=colorOwner){colorOwner=owner;ballPaint.color=color;match.ball.trail.startColor=color;match.ball.trail.endColor=new Color(color.r,color.g,color.b,0);}
  for(int i=0;i<2;i++){labels[i].text=match.players[i].name;labels[i].color=match.players[i].color;labels[i].transform.position=match.trucks[i].body.position+Vector3.up*1.05f;labels[i].transform.rotation=cameraTarget.transform.rotation;labels[i].gameObject.SetActive(live);markers[i].gameObject.SetActive(live&&match.rules.Eligible==i);markers[i].position=new Vector3(match.trucks[i].body.position.x,.03f,match.trucks[i].body.position.z);}
  if(Time.unscaledTime<next)return;next=Time.unscaledTime+.08f;bool valid=live&&match.rules.Active&&WallballFlight.Landing(p,match.ball.body.linearVelocity,match,out var target)&&Mathf.Abs(target.x)<=match.halfWidth&&target.z>=-match.courtLength;landing.gameObject.SetActive(valid);if(valid){WallballFlight.Landing(p,match.ball.body.linearVelocity,match,out target);for(int i=0;i<circle.Length;i++){float a=i*Mathf.PI*2/(circle.Length-1);circle[i]=target+new Vector3(Mathf.Cos(a)*.65f,0,Mathf.Sin(a)*.65f);}landing.SetPositions(circle);landing.startColor=landing.endColor=color;}
 }
 void OnDestroy(){if(ballPaint!=null)Destroy(ballPaint);}
}
}
