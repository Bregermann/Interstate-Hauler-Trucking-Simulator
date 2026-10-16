using UnityEngine;using TMPro;using Michsky.UI.Heat;
namespace LWS.TruckTugOfWar {
// Read-only broadcast adapter. Heat owns meter rendering; no scoring or force authority here.
public sealed class TugHUD:MonoBehaviour {
 public TugMatch match;public GameObject root;public ProgressBar[] effort,danger;public TMP_Text readout;public TMP_Text[] teamLabels;float next;readonly float[] power=new float[2],edge=new float[2];readonly int[] count=new int[2];
 void Update(){if(root==null)return;bool show=match.Running&&!match.Finished&&(match.Stage==TugStage.Pull||match.Stage==TugStage.Countdown);root.SetActive(show);if(!show||Time.unscaledTime<next)return;next=Time.unscaledTime+.1f;power[0]=power[1]=0;edge[0]=edge[1]=20;count[0]=count[1]=0;for(int i=0;i<match.setup.count;i++){int team=match.Team(i);count[team]++;power[team]+=Mathf.Abs(match.trucks[i].command.y)*match.trucks[i].motorTorque/13;edge[team]=Mathf.Min(edge[team],match.trucks[i].body.worldCenterOfMass.x*match.Side(team)-match.edge);}for(int t=0;t<2;t++){effort[t].SetValue(100*Mathf.Clamp01(power[t]/Mathf.Max(1,count[t])));danger[t].SetValue(100*Mathf.Clamp01(edge[t]/9));teamLabels[t].text="TEAM "+(t+1)+" · EDGE "+LWS.TruckSports.SportsMeasure.Feet(Mathf.Max(0,edge[t]))+" · "+(Mathf.RoundToInt(effort[t].currentValue))+"% EFFORT";}readout.text="ROPE STRAIN "+Mathf.RoundToInt(Mathf.Clamp01(match.Tension/(125*Mathf.Max(2,match.setup.count)))*100)+"% · GROUND ADVANTAGE "+LWS.TruckSports.SportsMeasure.Feet(Mathf.Abs(match.Advantage))+" TEAM "+(match.Advantage>=0?2:1);}
}
}
