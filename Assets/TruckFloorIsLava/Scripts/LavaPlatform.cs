using UnityEngine;
using DG.Tweening;
using LWS.TruckSports;
namespace LWS.TruckFloorIsLava
{
    public enum LavaPlatformKind { Wide, Ordinary, Narrow, Moving, Tilting, ShortWarning, TemporarySafe }
    // Game-specific lifecycle over real PhysX bodies and NWH's existing moving
    // ground/wheel reaction support. The finite rules object owns collapse time.
    public sealed class LavaPlatform : MonoBehaviour
    {
        public Rigidbody body;
        public Collider surface;
        public Renderer plate;
        public Transform cracks;
        public ConfigurableJoint tilt;
        public LavaPlatformKind kind;
        public Vector2 halfSize=new Vector2(22,22);
        public Color restingColor=new Color(.18f,.26f,.29f);
        [Min(1)]public float warningSeconds=10;
        [Range(0,3)]public float slideMetres=2.5f;
        SportsEventSession session;LavaRules rules;int slot;
        Vector3 home;Quaternion homeRotation;bool cached,released,retired;LavaPlatformState shown;
        JointDrive xDrive,yzDrive;MaterialPropertyBlock tint;Tween warning;Collider[] ownedColliders;
        public bool Retired=>retired;
        public void ResetPlatform(SportsEventSession owner,LavaRules authority,int index)
        {
            if(!cached){home=body.position;homeRotation=body.rotation;if(tilt!=null){xDrive=tilt.angularXDrive;yzDrive=tilt.angularYZDrive;}tint=new MaterialPropertyBlock();ownedColliders=GetComponentsInChildren<Collider>(true);cached=true;}
            session=owner;rules=authority;slot=index;warning?.Kill();warning=null;released=retired=false;shown=LavaPlatformState.Untouched;
            gameObject.SetActive(true);var interpolation=body.interpolation;body.interpolation=RigidbodyInterpolation.None;
            body.isKinematic=false;body.linearVelocity=body.angularVelocity=Vector3.zero;body.position=home;body.rotation=homeRotation;
            body.transform.SetPositionAndRotation(home,homeRotation);body.isKinematic=true;body.interpolation=interpolation;surface.enabled=true;cracks.gameObject.SetActive(false);cracks.localScale=Vector3.one;
            if(tilt!=null){tilt.xMotion=tilt.yMotion=tilt.zMotion=ConfigurableJointMotion.Locked;tilt.angularYMotion=ConfigurableJointMotion.Locked;tilt.angularXMotion=tilt.angularZMotion=ConfigurableJointMotion.Limited;tilt.angularXDrive=xDrive;tilt.angularYZDrive=yzDrive;}
            Paint(restingColor,0);
            foreach(var collider in ownedColliders)collider.enabled=true;
        }
        public bool TouchFromTruck(SportsEventTruck truck)
        {return session!=null&&session.Live&&rules.Started&&!rules.Complete&&truck!=null&&truck.session==session&&truck.slot>=0&&truck.slot<rules.Count&&rules.alive[truck.slot]&&rules.Touch(slot);}
        void OnCollisionEnter(Collision collision)
        {if(collision.rigidbody!=null)TouchFromTruck(collision.rigidbody.GetComponentInParent<SportsEventTruck>());}
        public void Tick(float lavaY)
        {
            if(rules==null||!rules.Started||retired)return;var state=rules.platforms[slot];
            if(state!=shown)
            {
                shown=state;
                if(state==LavaPlatformState.Warning)
                {
                    // Fracture decoration shakes independently; it cannot kick
                    // a truck through an unexpected collider displacement.
                    cracks.gameObject.SetActive(true);warning=cracks.DOShakeScale(Mathf.Max(.1f,rules.Remaining(slot)),.025f,12,40,false).SetEase(Ease.Linear);
                }
                else if(state==LavaPlatformState.Falling)
                {
                    warning?.Kill();warning=null;released=true;body.isKinematic=false;
                    if(tilt!=null){tilt.xMotion=tilt.yMotion=tilt.zMotion=ConfigurableJointMotion.Free;tilt.angularXMotion=tilt.angularYMotion=tilt.angularZMotion=ConfigurableJointMotion.Free;tilt.angularXDrive=tilt.angularYZDrive=new JointDrive();}
                    body.WakeUp();Paint(new Color(.42f,.12f,.04f),1);
                }
            }
            if(state==LavaPlatformState.Warning)
            {
                float urgency=1-Mathf.Clamp01(rules.Remaining(slot)/warningSeconds);float pulse=.5f+.5f*Mathf.Sin(rules.Clock*Mathf.Lerp(4,14,urgency));Paint(Color.Lerp(new Color(1,.55f,.04f),new Color(1,.06f,.015f),urgency),.3f+pulse*urgency*2);
            }
            if(!released)
            {
                if(kind==LavaPlatformKind.Tilting&&body.isKinematic)body.isKinematic=false;
                if(kind==LavaPlatformKind.Moving)
                {float ease=Mathf.SmoothStep(0,1,Mathf.Clamp01(rules.Clock/2));body.MovePosition(home+new Vector3(Mathf.Sin(rules.Clock*.35f+slot)*slideMetres*ease,0,0));}
            }
            else if(body.position.y<lavaY-30)
            {body.linearVelocity=body.angularVelocity=Vector3.zero;body.isKinematic=true;foreach(var collider in ownedColliders)collider.enabled=false;plate.enabled=false;cracks.gameObject.SetActive(false);retired=true;}
        }
        void Paint(Color color,float emission)
        {plate.enabled=true;plate.GetPropertyBlock(tint);tint.SetColor("_BaseColor",color);tint.SetColor("_Color",color);tint.SetColor("_EmissionColor",color*emission);plate.SetPropertyBlock(tint);}
        public void Freeze(){warning?.Kill();warning=null;if(body!=null)body.isKinematic=true;}
        void OnDisable(){warning?.Kill();warning=null;}
    }
}
