using UnityEngine;using UnityEngine.UI;
namespace LWS.TruckPoker {
public sealed class PokerMouseButton:MonoBehaviour {
 public PokerMatch match;public int view,action;void Start(){GetComponent<Button>().onClick.AddListener(()=>match.MouseAction(view,action));}
}
}
