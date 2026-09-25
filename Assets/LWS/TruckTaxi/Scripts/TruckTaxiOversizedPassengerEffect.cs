using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiOversizedPassengerEffect : MonoBehaviour
    {
        private Rigidbody body;
        private float addedMass;
        public bool Applied => addedMass>0;
        public void Apply(Rigidbody target,TruckTaxiSeatProfile seat)
        {
            Restore(); if(target==null || seat==null) return;
            body=target; addedMass=Mathf.Clamp(seat.additionalMassKg,0,800); body.mass+=addedMass;
            if(!body.isKinematic && seat.boardingImpulse>0)
                body.AddForceAtPosition(Vector3.down*Mathf.Clamp(seat.boardingImpulse,0,2000),body.transform.TransformPoint(seat.boardingImpulseOffset),ForceMode.Impulse);
        }
        public void Restore() { if(body!=null) body.mass=Mathf.Max(1,body.mass-addedMass); addedMass=0; body=null; }
        private void OnDisable() => Restore();
    }
}
