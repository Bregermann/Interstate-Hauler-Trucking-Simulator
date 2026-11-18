using System;
using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckExtremeHauling
{
    /// <summary>Collapse-only lane highlights using the collection's existing pose recorder.</summary>
    public sealed class DominoCollapseReplay : MonoBehaviour
    {
        public SportsMotionReplay[] lanes;
        public float captureSeconds = 12;
        Renderer[][] liveRenderers;
        bool[][] wasEnabled;
        bool[] started;
        float[] firstPush;
        int participants;
        bool skip;
        public bool Playing { get; private set; }
        public int LaneCount => lanes == null ? 0 : lanes.Length;
        public float Cursor => LaneCount == 0 ? 0 : lanes[0].Cursor;
        public float Speed => LaneCount == 0 ? 1 : lanes[0].Speed;
        public int Angle => LaneCount == 0 ? 0 : lanes[0].Angle;
        public int VisibleGhosts
        {
            get { int count=0; foreach(var lane in lanes) foreach(var ghost in lane.ghosts) if(ghost.gameObject.activeInHierarchy)count++; return count; }
        }
        public void ResetCapture(int count)
        {
            End();
            if(lanes==null || lanes.Length!=4)throw new InvalidOperationException("Domino requires four authored lane recorders.");
            participants=count; started=new bool[lanes.Length]; firstPush=new float[lanes.Length];
            liveRenderers=new Renderer[lanes.Length][]; wasEnabled=new bool[lanes.Length][];
            for(int lane=0;lane<lanes.Length;lane++)
            {
                var record=lanes[lane];
                if(record.sources.Length>12 || record.sources.Length!=record.ghosts.Length)throw new InvalidOperationException("Each Domino replay lane needs at most twelve matching original sources/ghosts.");
                record.End();
                // Native wheel/appearance transforms can settle after scene initialization.
                // Refresh metadata without introducing gameplay components into ghosts.
                var indices=new int[record.sources.Length];for(int i=0;i<indices.Length;i++)indices[i]=i;
                if(!record.RefreshAppearanceSources(indices,false) && !record.RefreshAppearanceSources(indices,true))throw new InvalidOperationException("Domino replay hierarchy changed; reauthor its render-only lane ghosts.");
                record.ResetRecord();
                var renderers=new System.Collections.Generic.List<Renderer>();
                foreach(var source in record.sources)renderers.AddRange(source.GetComponentsInChildren<Renderer>(true));
                liveRenderers[lane]=renderers.ToArray();wasEnabled[lane]=new bool[renderers.Count];
            }
            skip=false;
        }
        public void RecordLane(int lane,float matchAge,bool hasTriggered)
        {
            if(Playing || lane<0 || lane>=participants || !hasTriggered)return;
            if(!started[lane]){started[lane]=true;firstPush[lane]=matchAge;}
            float elapsed=matchAge-firstPush[lane];
            if(elapsed<=captureSeconds)lanes[lane].Record(elapsed,(1<<lanes[lane].sources.Length)-1);
        }
        public bool BeginPlayback()
        {
            if(liveRenderers==null)return false;
            bool any=false;
            for(int lane=0;lane<participants;lane++)any|=started[lane] && lanes[lane].Duration>.05f;
            if(!any)return false;
            for(int lane=0;lane<lanes.Length;lane++)
            {
                if(lane<participants)
                {
                    // An untriggered participant remains visibly upright; this is a static
                    // record of the real final pose, never a synthesized chain reaction.
                    if(!started[lane]){lanes[lane].Record(0,(1<<lanes[lane].sources.Length)-1);lanes[lane].Record(.11f,(1<<lanes[lane].sources.Length)-1);}
                    lanes[lane].Begin(0);
                }
                for(int i=0;i<liveRenderers[lane].Length;i++)
                {var renderer=liveRenderers[lane][i];wasEnabled[lane][i]=renderer.enabled;renderer.enabled=false;}
            }
            Playing=true;skip=false;return true;
        }
        public bool Step(float dt)
        {
            if(!Playing)return true;
            bool complete=true;
            for(int lane=0;lane<participants;lane++)complete &= lanes[lane].Step(dt);
            return skip || complete;
        }
        public void Action(string key)
        {
            if(!Playing)return;
            for(int lane=0;lane<participants;lane++)
            {
                var record=lanes[lane];
                if(key=="replayangle")record.Angle=(record.Angle+1)%3;
                else if(key=="replayspeed")record.Speed=record.Speed==1?.5f:record.Speed==.5f?2:1;
                else if(key=="replayrewind")record.Seek(Mathf.Max(0,record.Cursor-3));
            }
            if(key=="replayskip")skip=true;
        }
        public void Present(SportsRallyViews views,SportsEventCamera broadcast)
        {
            if(!Playing || broadcast==null)return;
            Bounds bounds=default;bool first=true;
            int focusLane=0;
            if(views!=null && views.Humans>0)focusLane=Mathf.Clamp(views.owners[0],0,participants-1);
            for(int lane=0;lane<participants;lane++)
            {
                if(Angle!=0 && lane!=focusLane)continue;
                foreach(var ghost in lanes[lane].ghosts)
                {if(first){bounds=new Bounds(ghost.position,Vector3.zero);first=false;}else bounds.Encapsulate(ghost.position);}
            }
            bounds.Expand(new Vector3(30,25,35));
            float aspect=Mathf.Max(.6f,broadcast.view.aspect);
            float distance=Mathf.Max(42,Mathf.Max(bounds.size.z,bounds.size.x/aspect)*1.12f);
            Vector3 offset=Angle==2?new Vector3(distance*.5f,distance*.8f,-distance*.65f):new Vector3(0,distance,-distance*.22f);
            if(views!=null)views.ReplayView(bounds.center,Angle,offset);else broadcast.Pose(bounds.center,offset,62);
        }
        public void End()
        {
            bool restore=Playing;Playing=false;
            if(lanes!=null)foreach(var lane in lanes)if(lane!=null)lane.End();
            if(restore && liveRenderers!=null)
                for(int lane=0;lane<liveRenderers.Length;lane++)for(int i=0;i<liveRenderers[lane].Length;i++)if(liveRenderers[lane][i]!=null)liveRenderers[lane][i].enabled=wasEnabled[lane][i];
        }
        void OnDisable()=>End();
    }
}
