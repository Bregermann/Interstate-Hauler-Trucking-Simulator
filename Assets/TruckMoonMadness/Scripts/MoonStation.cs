using System;
using UnityEngine;
namespace LWS.TruckMoonMadness
{
    public sealed class MoonStation : MonoBehaviour
    {
        public MoonMatch match; public int owner; public Collider volume;
        readonly float[] touched = new float[8];
        public bool Contains(int flag) => touched[flag] > 0 && match.Clock - touched[flag] < .12f;
        public void ResetStation() => Array.Clear(touched, 0, touched.Length);
        void OnTriggerStay(Collider other)
        {
            var flag = other.GetComponentInParent<MoonFlag>();
            if (flag != null && flag.match == match && flag.id >= 0 && flag.id < touched.Length) touched[flag.id] = match.Clock;
        }
    }
}
