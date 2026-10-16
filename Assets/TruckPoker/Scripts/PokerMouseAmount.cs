using UnityEngine;using UnityEngine.UI;
namespace LWS.TruckPoker {
public sealed class PokerMouseAmount:MonoBehaviour {
 public PokerMatch match;public int view;void Start(){GetComponent<Slider>().onValueChanged.AddListener(v=>match.MouseRaise(view,Mathf.RoundToInt(v)));}
}
}
