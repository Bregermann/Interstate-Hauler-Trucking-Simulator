using UnityEngine;
namespace LWS.TruckArchaeology
{
    public sealed partial class ArchaeologyMatch
    {
        readonly ArchaeologyAiWell[] aiWells=new ArchaeologyAiWell[4];
        void ResetAiWells(){for(int i=0;i<aiWells.Length;i++)aiWells[i]=new ArchaeologyAiWell();}
        bool ValidAiDigPoint(int owner,ArchaeologyArtifact item,Vector3 point)
        {
            if(!float.IsFinite(point.x)||!float.IsFinite(point.y)||!float.IsFinite(point.z))return false;
            Vector3 plot=Plot(item.index/3);return Mathf.Abs(point.x-plot.x)<=13&&Mathf.Abs(point.z-plot.z)<=13&&point.y>=-.5f&&point.y<=12&&Vector3.Distance(trucks[owner].body.position,point)<=28;
        }
        float KnownDigClearance(int owner,Vector3 point)
        {
            float clearance=float.PositiveInfinity;Vector3 head=equipment[owner].head.position;
            foreach(var find in artifacts)
            {
                if(!find.gameObject.activeInHierarchy||!find.shape.enabled||Rules.Stages[find.index]<ArtifactStage.Scanned)continue;
                clearance=Mathf.Min(clearance,Mathf.Min(Vector3.Distance(point,find.shape.ClosestPoint(point)),Vector3.Distance(head,find.shape.ClosestPoint(head))));
            }
            return clearance;
        }
        Vector3 AiDigPoint(int owner,ArchaeologyArtifact item)
        {
            var well=aiWells[owner];well.ChangeArtifact(item.index);
            if(item.Clearance){well.Invalidate();return item.WorkPoint;}
            if(well.Active&&!ValidAiDigPoint(owner,item,well.Point))well.Invalidate();
            if(!well.Due(Clock))return well.Active?well.Point:item.WorkPoint;
            well.BeginSample(Clock);Vector3 sample=well.Active?well.SamplePosition:item.WorkPoint;
            // No synthetic floor or exposure from an absent ray. Reacquire the
            // authoritative cached work point; original terrain gates still veto.
            if(!ValidAiDigPoint(owner,item,sample)||!earth.Surface(sample,out RaycastHit hit)||!ValidAiDigPoint(owner,item,hit.point-Vector3.up*.2f)||hit.point.y<item.shape.bounds.min.y)
            {well.Invalidate();return item.WorkPoint;}
            if(well.Active)
            {
                if(well.Observe(hit.point,item.shape.bounds.min.y))return well.Point;
                return item.WorkPoint;
            }
            Vector3 point=hit.point-Vector3.up*.2f;var tool=ArchaeologyAiDigPolicy.Choose(KnownDigClearance(owner,point));float radius=tool==ArchaeologyTool.Excavator?2.4f:tool==ArchaeologyTool.Drill?1.25f:tool==ArchaeologyTool.Blower?1.05f:.6f;
            well.Acquire(hit.point,radius,item.shape.bounds.min.y);return well.Point;
        }
        public string AiWellTelemetry(int owner)
        {
            var well=aiWells[owner];return "AI_WELL owner="+owner+" active="+well.Active+" artifact="+well.Artifact+" xz="+well.SamplePosition.ToString("F6")+" currentActualPoint="+well.Point.ToString("F6")+" targetSurfaceY="+well.TargetSurfaceY.ToString("R")+" queries="+well.Queries+" acquisitions="+well.Acquisitions+" completed="+well.CompletedSteps+" invalidations="+well.Invalidations;
        }
    }
}
