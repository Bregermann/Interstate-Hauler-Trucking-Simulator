using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Consumer of existing regional town semantics, never a second boundary database.
    [DisallowMultipleComponent, DefaultExecutionOrder(450)]
    public sealed class TruckTaxiMoralityTownBinding : MonoBehaviour
    {
        public TruckTaxiRegionalWorld regionalWorld;
        private TruckTaxiBootstrap host;
        private TruckTaxiSession session;
        private TruckTaxiSession attemptedSession;
        private Func<Vector3,string> resolver;
        public bool IsBound => session!=null && session.TownIdAtPosition==resolver;
        public string Status { get; private set; }="Awaiting Session and regional metadata.";

        public bool Initialize(TruckTaxiSession owner,TruckTaxiRegionalWorld regional)
        {
            if(owner==null || regional==null || regional.regions==null || regional.regions.Length==0)
            { Status="Existing authored regional metadata is required."; return false; }
            if(resolver==null) resolver=ResolveTown;
            if(owner.TownIdAtPosition!=null && owner.TownIdAtPosition!=resolver)
            { Status="Another town-position provider already owns the Session binding."; return false; }
            if(session!=owner) Release();
            regionalWorld=regional; session=owner; session.TownIdAtPosition=resolver;
            Status="Bound to existing ResolveRideTown metadata."; return true;
        }
        public string ResolveTown(Vector3 position) => ResolveAuthoredTown(regionalWorld,position);
        public static string ResolveAuthoredTown(TruckTaxiRegionalWorld regional,Vector3 position)
        {
            if(regional==null || !regional.isActiveAndEnabled || regional.regions==null ||
                !TruckTaxiMoralArrivalPolicy.Finite(position)) return string.Empty;
            // ResolveRideTown already folds outskirts/venues into their authored town IDs.
            return regional.ResolveRideTown(position);
        }
        private void OnEnable() { host=GetComponent<TruckTaxiBootstrap>(); TryBindHost(); }
        private void Update()
        {
            // Bootstrap creates its Session during startup. No state capture or bound delegate polling.
            if(host!=null && attemptedSession!=host.Session) TryBindHost();
        }
        private void TryBindHost()
        {
            if(host?.Session!=null)
            {
                attemptedSession=host.Session;
                Initialize(host.Session,regionalWorld!=null ? regionalWorld : GetComponent<TruckTaxiRegionalWorld>());
            }
        }
        private void Release()
        {
            if(session!=null && session.TownIdAtPosition==resolver) session.TownIdAtPosition=null;
            session=null;
        }
        private void OnDisable() { Release(); attemptedSession=null; }
    }
}
