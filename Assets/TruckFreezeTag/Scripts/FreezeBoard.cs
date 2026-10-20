using UnityEngine;using TMPro;
namespace LWS.TruckFreezeTag
{
 public sealed class FreezeBoard:MonoBehaviour
 {
  public FreezeMatch match;public TMP_Text text;float at;
  void Update(){if(Time.unscaledTime<at)return;at=Time.unscaledTime+.1f;if(!match.Running){text.text=match.Title+"\nWINTER FREEZE FESTIVAL\nCONFIGURE YOUR CREW";return;}int runners=0;for(int i=0;i<match.setup.count;i++)if(match.rules.Team(i)!=match.rules.Freezers&&!match.rules.frozen[i])runners++;text.text=match.Title+"\nBLUE "+match.rules.wins[0]+" : "+match.rules.wins[1]+" ORANGE · ROUND "+(match.rules.Round+1)+" / "+match.rules.Rounds+"\n"+(match.rules.Freezers==0?"BLUE":"ORANGE")+" CANNONS · "+Mathf.Max(0,match.roundSeconds-match.rules.Clock).ToString("F0")+"s · "+runners+" RUNNERS ACTIVE";}
 }
}
