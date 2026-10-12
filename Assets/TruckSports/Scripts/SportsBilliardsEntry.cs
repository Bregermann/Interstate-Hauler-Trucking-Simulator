using System.Collections;
using UnityEngine;
using LWS.TruckBilliards;
namespace LWS.TruckSports {
public sealed class SportsBilliardsEntry:MonoBehaviour {IEnumerator Start(){yield return null;var session=GetComponent<BilliardsSession>();session.menus.Action(SportsNavigation.BilliardsPractice?"practice":"play");SportsNavigation.BilliardsPractice=false;}}
}

