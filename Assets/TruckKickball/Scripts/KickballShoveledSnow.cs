using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckKickball
{
    // Visual lifecycle only. Weatherade owns snow; the original baseball
    // colliders, bases, driving and match rules are never changed here.
    [DefaultExecutionOrder(260)]
    public sealed class KickballShoveledSnow : MonoBehaviour
    {
        public SportsSnowSurface snow;
        public GameObject clearedPresentation;
        public Renderer[] clearedBases;
        public Renderer[] clearedPaths;
        public Renderer[] snowbanks;
        public bool Visible => clearedPresentation != null && clearedPresentation.activeSelf;
        void LateUpdate()
        {
            if (clearedPresentation != null)
                clearedPresentation.SetActive(snow != null && snow.Coverage > .04f);
        }
    }
}
