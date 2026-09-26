using UnityEngine;

namespace LWS.TruckTaxi
{
    // Inspector-visible semantic identity shared by Compass POIs and the offer-map projection.
    // May also be authored on future collectible/event targets; it never owns gameplay completion.
    public sealed class TruckTaxiMapMarker : MonoBehaviour
    {
        public string stableId, label;
        public TruckTaxiMapMarkerType markerType;
        public TruckTaxiMapMarkerState state=TruckTaxiMapMarkerState.Optional;
        public Transform source;
        public Vector3 sourceLocalOffset;
        [SerializeField] private Sprite assignedIcon;
        [SerializeField] private Component compassPoi;
        [SerializeField] private bool presented;
        internal TruckTaxiMapMarkers Owner { get; set; }
        public Sprite AssignedIcon => assignedIcon;
        public Component CompassPoi => compassPoi;
        public bool Presented => presented;
        public Vector3 Position => source!=null ? source.TransformPoint(sourceLocalOffset) : transform.position;
        public void BindPresentation(Sprite icon,Component poi,bool visible)
        { assignedIcon=icon; compassPoi=poi; presented=visible; }
        private void OnDestroy() { if(Owner!=null) Owner.Unregister(this); }
    }
}
