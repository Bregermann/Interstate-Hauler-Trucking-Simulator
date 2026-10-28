using UnityEngine;
namespace LWS.TruckArchaeology
{
    // Managed AI work-plan state only. All surface samples come from the real
    // Digger collider; this cannot expose, move or score an artifact.
    public sealed class ArchaeologyAiWell
    {
        public bool Active{get;private set;}public int Artifact{get;private set;}=-1;
        public Vector3 SamplePosition{get;private set;}public Vector3 Point{get;private set;}
        public float TargetSurfaceY{get;private set;}public float NextSample{get;private set;}
        public int Queries{get;private set;}public int Acquisitions{get;private set;}public int CompletedSteps{get;private set;}public int Invalidations{get;private set;}
        public bool Due(float clock)=>clock>=NextSample;
        public bool BeginSample(float clock){if(!float.IsFinite(clock)||!Due(clock))return false;NextSample=clock+.25f;Queries++;return true;}
        public void ChangeArtifact(int id){if(Artifact==id)return;Active=false;Artifact=id;}
        public bool Acquire(Vector3 actualSurface,float existingRadius,float actualBottom)
        {
            if(!Finite(actualSurface)||!float.IsFinite(existingRadius)||existingRadius<=0||!float.IsFinite(actualBottom)||actualSurface.y<actualBottom){Invalidate();return false;}
            SamplePosition=actualSurface;Point=actualSurface-Vector3.up*.2f;
            TargetSurfaceY=Mathf.Max(actualBottom,actualSurface.y-existingRadius);Active=true;Acquisitions++;return true;
        }
        public bool Observe(Vector3 actualSurface,float actualBottom)
        {
            if(!Finite(actualSurface)||!float.IsFinite(actualBottom)||Vector2.Distance(new Vector2(actualSurface.x,actualSurface.z),new Vector2(SamplePosition.x,SamplePosition.z))>.01f){Invalidate();return false;}
            if(actualSurface.y<=TargetSurfaceY||actualSurface.y<=actualBottom){Active=false;CompletedSteps++;return false;}
            Point=actualSurface-Vector3.up*.2f;return true;
        }
        static bool Finite(Vector3 point)=>float.IsFinite(point.x)&&float.IsFinite(point.y)&&float.IsFinite(point.z);
        public void Invalidate(){if(Active)Invalidations++;Active=false;}
    }
}
