using System;
using UnityEngine;

namespace LWS.TruckSports
{
    // An existing render-only mesh supplies its real pose. This is not a wheel
    // controller: no forces, contact, steering or synthetic rotation are added.
    [Serializable]
    public sealed class SportsTruckStaticWheelPose
    {
        public Transform source;
        public Vector3 center;
        public float diameter;
        public bool ValidFor(Transform owner)
        {
            if(owner==null||source==null||source==owner||!source.IsChildOf(owner)||!SportsTruckAppearanceWheelMath.Finite(center)||!float.IsFinite(diameter)||diameter<=.0001f)return false;
            return source.GetComponent<MeshRenderer>()!=null&&source.TryGetComponent<MeshFilter>(out var filter)&&filter.sharedMesh!=null&&source.GetComponentsInChildren<Collider>(true).Length==0&&source.GetComponentsInChildren<Rigidbody>(true).Length==0;
        }
    }
    public static class SportsTruckAppearanceWheelMath
    {
        public static bool Finite(Vector3 value)=>float.IsFinite(value.x)&&float.IsFinite(value.y)&&float.IsFinite(value.z);
        public static bool TryUniformScale(Vector3 scale,out float value)
        {
            value=0;if(!Finite(scale)||scale.x<=.0001f||scale.y<=.0001f||scale.z<=.0001f)return false;
            if(Mathf.Abs(scale.x-scale.y)>Mathf.Max(.00001f,scale.x*.0001f)||Mathf.Abs(scale.x-scale.z)>Mathf.Max(.00001f,scale.x*.0001f))return false;value=scale.x;return true;
        }
        // Fit diameters are host-local. The canonical mesh scale and following
        // center/offset are world-space. Convert once, retaining proportions at
        // .55 passenger scale and smaller uniformly scaled display rigs.
        public static bool TryFit(float visualDiameter,float originalDiameter,float canonicalDiameter,float canonicalWorldScale,float ownerScale,out float geometryScale,out float verticalOffset)
        {
            geometryScale=verticalOffset=0;
            if(!float.IsFinite(visualDiameter)||!float.IsFinite(originalDiameter)||!float.IsFinite(canonicalDiameter)||!float.IsFinite(canonicalWorldScale)||!float.IsFinite(ownerScale)||visualDiameter<=0||originalDiameter<=0||canonicalDiameter<=0||Mathf.Abs(canonicalWorldScale)<1e-8f||ownerScale<=0)return false;
            geometryScale=visualDiameter*ownerScale/(canonicalDiameter*Mathf.Abs(canonicalWorldScale));verticalOffset=(visualDiameter-originalDiameter)*ownerScale*.5f;
            return float.IsFinite(geometryScale)&&geometryScale>0&&float.IsFinite(verticalOffset);
        }
    }
}
