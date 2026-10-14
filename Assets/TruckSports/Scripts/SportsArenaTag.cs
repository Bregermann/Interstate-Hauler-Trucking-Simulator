using UnityEngine;
namespace LWS.TruckSports {
[DefaultExecutionOrder(100)]public sealed class SportsArenaTag:MonoBehaviour {
 public TextMesh text;public string marker;
 void LateUpdate(){if(text!=null&&text.text!=marker)text.text=marker;}
}
}
