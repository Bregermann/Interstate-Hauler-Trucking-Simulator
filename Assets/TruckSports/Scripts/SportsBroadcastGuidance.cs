using CompassNavigatorPro;
using UnityEngine;
using LWS.TruckBowling;

namespace LWS.TruckSports
{
    [DefaultExecutionOrder(900)]
    public sealed class SportsBroadcastGuidance:MonoBehaviour
    {
        public SportsBroadcastFraming framing;
        public SportsBroadcastCompass compass;
        public CompassProPOI[] targets;
        public int ActiveCues{get;private set;}
        void LateUpdate()
        {
            bool active=framing!=null&&framing.OwnsShot&&framing.Configuration.offscreenObjective;
            if(compass.gameObject.activeSelf!=active)compass.gameObject.SetActive(active);
            ActiveCues=0;
            if(active)
            {
                compass.cameraMain=framing.cameraRig.view;compass.follow=compass.cameraMain.transform;compass.viewportRect=compass.cameraMain.rect;
                bool objective=framing.action.hasObjective;
                Set(0,objective,framing.action.objective.center,"OBJECTIVE",new Color(1,.82f,.15f));
                int cue=1;
                for(int slot=0;slot<framing.SubjectCount&&cue<targets.Length;slot++)
                {
                    if(!framing.Valid(slot)||!framing.IsHuman(slot)||slot==framing.Primary)continue;
                    Set(cue++,true,framing.Body(slot).position,framing.ParticipantName(slot),framing.ParticipantColor(slot));
                }
                while(cue<targets.Length)Set(cue++,false,Vector3.zero,"",Color.white);
            }
            else foreach(var target in targets)if(target.gameObject.activeSelf)target.gameObject.SetActive(false);
        }
        void Set(int index,bool active,Vector3 position,string title,Color color)
        {
            var target=targets[index];target.transform.position=position;target.title=title;target.tintColor=color;
            if(target.gameObject.activeSelf!=active)target.gameObject.SetActive(active);
            if(active){var viewport=framing.cameraRig.view.WorldToViewportPoint(position);if(viewport.z<=0||viewport.x<0||viewport.x>1||viewport.y<0||viewport.y>1)ActiveCues++;}
        }
    }
}
