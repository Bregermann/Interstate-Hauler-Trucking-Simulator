using UnityEngine;using TMPro;using System.Text;
namespace LWS.TruckHungryHaulers
{
 public sealed class HaulerBoard:MonoBehaviour
 {
  public HaulerMatch match;public TMP_Text label;float next;readonly StringBuilder text=new StringBuilder(300);
  void Update(){if(Time.unscaledTime<next)return;next=Time.unscaledTime+.1f;text.Clear();text.Append("HUNGRY HUNGRY HAULERS\n");if(!match.Running){text.Append("LOAD UP YOUR CREW\nSCOOP · HAUL · BANK");}else{for(int i=0;i<match.setup.count;i++){text.Append(match.setup.players[i].name).Append(' ').Append(match.rules.points[i]).Append("P").Append(i%2==0&&i+1<match.setup.count?"   :   ":"\n");}text.Append(Mathf.Max(0,match.rules.Duration-match.rules.Clock).ToString("F0")).Append("s · NORMAL1 / GOLD5 / AVOID FUSES");}label.text=text.ToString();}
 }
}
