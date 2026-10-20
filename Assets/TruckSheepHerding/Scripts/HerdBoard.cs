using UnityEngine;using TMPro;
namespace LWS.TruckSheepHerding
{
 public sealed class HerdBoard:MonoBehaviour
 {
  public HerdMatch match;public TMP_Text label;float next;
  void Update(){if(match==null||label==null||Time.unscaledTime<next)return;next=Time.unscaledTime+.2f;label.text=match.Header+"\n"+match.Standings;}
 }
}
