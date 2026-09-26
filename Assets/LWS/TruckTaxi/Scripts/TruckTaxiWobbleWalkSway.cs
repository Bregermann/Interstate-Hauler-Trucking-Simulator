using UnityEngine;

namespace LWS.TruckTaxi
{
    // Oscillates only the visual child; UTS moves the pedestrian root.
    public sealed class TruckTaxiWobbleWalkSway : MonoBehaviour
    {
        private Vector3 lastPosition;
        private float phase;

        private void OnEnable()
        {
            lastPosition=transform.parent != null ? transform.parent.position : transform.position;
        }

        private void LateUpdate()
        {
            if(transform.parent==null) return;
            Vector3 position=transform.parent.position;
            float speed=Vector3.Distance(position,lastPosition)/Mathf.Max(Time.deltaTime,.001f);
            lastPosition=position;
            float amount=Mathf.Clamp01(speed);
            phase+=Time.deltaTime*(4f+speed*3f);
            transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(phase)*7f*amount);
            transform.localPosition=new Vector3(0,Mathf.Abs(Mathf.Sin(phase))*.035f*amount,0);
        }
    }
}
