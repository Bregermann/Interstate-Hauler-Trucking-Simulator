using UnityEngine;
using TMPro;
namespace LWS.TruckBowling {
// Shares one cached state string between Join and Disconnect; no per-frame enumeration.
public sealed class BowlingJoinText:MonoBehaviour {public BowlingMenus menus;public TMP_Text text;int revision=-1;void Update(){if(revision==menus.Revision)return;revision=menus.Revision;text.text=menus.joinInfo.text;}}
}