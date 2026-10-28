using Assets.CoreCollapse.Scripts.Structural;
namespace LWS.TruckTeaParty {
 public sealed class ServingSupport:Support {void Awake(){ClearSupports();if(onCollapse==null)onCollapse=new OnCollapseEvent();}}
}
