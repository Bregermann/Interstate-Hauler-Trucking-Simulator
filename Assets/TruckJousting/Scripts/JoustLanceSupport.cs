using UnityEngine;
using Assets.CoreCollapse.Scripts.Structural;
namespace LWS.TruckJousting
{
    // Initialize a self-contained sporting prop without changing vendor Support.
    public sealed class JoustLanceSupport:Support
    {
        void Awake(){ClearSupports();onCollapse=new OnCollapseEvent();}
    }
}
