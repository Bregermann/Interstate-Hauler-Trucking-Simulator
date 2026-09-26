using TMPro;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // The session evaluates stops; this adapter only routes and presents the active point.
    public sealed class TruckTaxiOptionalStops : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private TaxiRequestProgress current;
        private LineRenderer ring;
        private TextMeshPro label;
        private Material material;
        private Transform head;
        private Quaternion headRest;
        public bool MarkerVisible => ring!=null && ring.enabled;
        public bool showAllPoints;
        public void Initialize(TruckTaxiBootstrap value)
        {
            host=value;
            material=new Material(Shader.Find("Sprites/Default"));
            ring=new GameObject("Optional stop ground marker",typeof(LineRenderer)).GetComponent<LineRenderer>();
            ring.transform.SetParent(transform,false); ring.sharedMaterial=material; ring.positionCount=65; ring.widthMultiplier=.22f;
            label=new GameObject("Optional stop label",typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            label.transform.SetParent(transform,false); label.font=host.hud.font; label.fontSize=7; label.alignment=TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta=new Vector2(30,7);
            host.Session.RequestCreated+=Refresh; host.Session.RequestResolved+=Refresh;
            host.Session.DestinationChanged+=RefreshDestination; host.Session.Changed+=RefreshDestination;
            host.Session.StopStarted+=OnStopStarted;
            RefreshDestination();
        }
        private void Refresh(TaxiRequestProgress request)
        {
            if(request.StopPoint!=null && request.State==TaxiRequestState.Succeeded)
                host.Passengers.Dialogue.Speak(host.Session.Passenger,TruckTaxiDialogueCategory.UniqueMechanicReaction,host.Session,
                    request.StopPoint.completionDialogue,100);
            RefreshDestination();
        }
        public void RefreshDestination()
        {
            var next=host.Session.State==TruckTaxiState.DrivingToDestination ? host.Session.ActiveStop : null;
            if(next==current) return;
            RestoreHead(); current=next;
            ring.enabled=label.enabled=current!=null;
            if(current==null)
            {
                if(host.Session.State==TruckTaxiState.DrivingToDestination) host.GPS.SetRideDestination(host.Session.Destination);
                return;
            }
            var point=current.StopPoint;
            host.GPS.SetStopDestination(point);
            Color color=point.category==TruckTaxiStopCategory.Scenic ? Color.cyan : new Color(1,.4f,.65f);
            ring.startColor=ring.endColor=label.color=color;
            for(int i=0;i<65;i++)
            {
                float angle=i*Mathf.PI*2/64;
                ring.SetPosition(i,point.Position+new Vector3(Mathf.Cos(angle)*point.radius,.15f,Mathf.Sin(angle)*point.radius));
            }
            label.transform.position=point.Position+Vector3.up*5;
        }
        private void OnStopStarted(TaxiRequestProgress request)
        {
            host.Passengers.Dialogue.Speak(host.Session.Passenger,TruckTaxiDialogueCategory.UniqueMechanicReaction,host.Session,
                request.StopPoint.arrivalDialogue,95);
            var actor=host.Passengers.PrimaryActor;
            var animator=actor!=null ? actor.GetComponentInChildren<Animator>() : null;
            if(animator!=null && animator.isHuman)
            { RestoreHead(); head=animator.GetBoneTransform(HumanBodyBones.Head); if(head!=null) headRest=head.localRotation; }
        }
        private void LateUpdate()
        {
            if(current==null) return;
            var p=current.StopPoint;
            label.text=(p.category==TruckTaxiStopCategory.Scenic ? "SCENIC STOP" : "SKETCHY PICKUP")+"\n"+p.displayName+"\n"+
                (current.Progress>0 ? current.ProgressText : "STOP IN THE MARKED BAY");
            if(Camera.main!=null) label.transform.rotation=Quaternion.LookRotation(label.transform.position-Camera.main.transform.position);
            // Small additive look gesture; never move the passenger root out of its authored seat.
            if(head!=null && current.Progress>0)
            {
                Vector3 local=host.Player.transform.InverseTransformDirection(p.viewDirection);
                head.localRotation=headRest*Quaternion.AngleAxis(Mathf.Clamp(Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg,-35,35),Vector3.up);
            }
            else if(head!=null) head.localRotation=headRest;
        }
        private void RestoreHead() { if(head!=null) head.localRotation=headRest; head=null; }
        private void OnDrawGizmos()
        {
            if(!showAllPoints || host?.Session==null) return;
            foreach(var point in host.Session.Capabilities.Stops) if(point!=null)
            { Gizmos.color=point.category==TruckTaxiStopCategory.Scenic ? Color.cyan : Color.magenta; Gizmos.DrawWireSphere(point.Position,point.radius); }
        }
        private void OnDestroy()
        {
            RestoreHead();
            if(host?.Session!=null) { host.Session.RequestCreated-=Refresh; host.Session.RequestResolved-=Refresh;
                host.Session.Changed-=RefreshDestination; host.Session.DestinationChanged-=RefreshDestination; host.Session.StopStarted-=OnStopStarted; }
            if(material!=null) Destroy(material);
        }
    }
}
