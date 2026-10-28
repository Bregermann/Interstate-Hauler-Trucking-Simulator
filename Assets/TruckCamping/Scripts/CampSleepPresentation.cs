using UnityEngine;
namespace LWS.TruckCamping
{
    // Cartoon sleep presentation only. Real NWH bodies, hammock springs and
    // wheel contacts remain the sole source of truck motion.
    public sealed class CampSleepPresentation:MonoBehaviour
    {
        public CampExplorer explorer;
        public TextMesh[] letters;
        float phase;
        void LateUpdate()
        {
            bool sleeping=explorer.match.Live&&explorer.Sleeping;
            if(sleeping)phase+=Time.deltaTime;
            for(int i=0;i<letters.Length;i++)
            {
                letters[i].gameObject.SetActive(sleeping);if(!sleeping)continue;
                float t=Mathf.Repeat(phase*.32f+i*.33f,1);letters[i].transform.position=explorer.truck.body.worldCenterOfMass+new Vector3(Mathf.Sin(t*5+i)*.4f,3+t*2.5f,0);letters[i].color=new Color(.81f,.94f,1,Mathf.Sin(t*Mathf.PI));letters[i].characterSize=.1f+t*.07f;
            }
        }
    }
}
