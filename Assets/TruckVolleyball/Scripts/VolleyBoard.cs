using UnityEngine;using TMPro;
namespace LWS.TruckVolleyball
{
 public sealed class VolleyBoard:MonoBehaviour
 {
  public VolleyMatch match;public TMP_Text text;float at;
  void Update(){if(Time.unscaledTime<at)return;at=Time.unscaledTime+.1f;text.text="BEACH TRUCK VOLLEYBALL\n<color=#45CAED>BLUE "+match.rules.points[0]+"</color>  :  <color=#FF8F44>"+match.rules.points[1]+" ORANGE</color>\n"+match.Status;}
 }
}
