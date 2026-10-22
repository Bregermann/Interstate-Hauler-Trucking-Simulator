using System;
using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckDockDiving
{
    // Dock-only visual response to the existing recorded actual-body water events.
    // NWH, the first-water trigger and the original match own all physical results.
    [DefaultExecutionOrder(100)]
    public sealed class DockPoolPresentation : MonoBehaviour
    {
        public DockMatch match;
        public int authoredVersion=1;
        public SportsWaterInteraction interaction;
        public MeshFilter surface;
        public MeshFilter crownSheet;
        public ParticleSystem crown, droplets, mist;
        [Range(.002f,.06f)] public float ambientHeight=.018f;
        [Range(.2f,1.5f)] public float impactScale=1;
        [Min(1)] public float referenceImpactEnergy=600;
        struct Wave { public Vector3 point; public float born,strength; }
        readonly Wave[] waves=new Wave[4];
        Vector3[] rest,vertices; Mesh original,mesh; Material originalMaterial,material;
        Renderer water; int next,launch=-1,lastEntryFrame=-1; float lastUpdate,priorCursor=-1;
        Mesh sheetMesh; Renderer sheetRenderer; Vector3[] sheetVertices; Color[] sheetColors;
        Vector3 sheetPoint;float sheetBorn,sheetStrength,particleSpeed=-1;
        public int LiveImpacts {get;private set;}
        public int ReplayImpacts {get;private set;}
        public float LastIncomingNormalSpeed {get;private set;}
        public float LastImpactEnergy {get;private set;}
        public float LastStrength {get;private set;}
        public Vector3 ImpactPoint {get;private set;}
        public int ImpactLaunch {get;private set;}=-1;
        public float MaximumDisplacement {get;private set;}
        public int ActiveWaves {get;private set;}
        void OnEnable()
        {
            if(surface==null||interaction==null)return;
            original=surface.sharedMesh;mesh=Instantiate(original);mesh.name="Owned runtime calm Dock surface";mesh.MarkDynamic();surface.sharedMesh=mesh;
            rest=mesh.vertices;vertices=new Vector3[rest.Length];water=surface.GetComponent<Renderer>();originalMaterial=water.sharedMaterial;
            material=new Material(originalMaterial);water.sharedMaterial=material;
            if(crownSheet!=null)
            {
                const int segments=64,levels=4;sheetMesh=new Mesh{name="Owned runtime ballistic Dock crown"};sheetMesh.MarkDynamic();
                sheetVertices=new Vector3[(segments+1)*(levels+1)];sheetColors=new Color[sheetVertices.Length];var uv=new Vector2[sheetVertices.Length];var triangles=new int[segments*levels*6];
                for(int j=0;j<=levels;j++)for(int i=0;i<=segments;i++)uv[j*(segments+1)+i]=new Vector2(i*4f/segments,j/(float)levels);
                int at=0;for(int j=0;j<levels;j++)for(int i=0;i<segments;i++){int a=j*(segments+1)+i;triangles[at++]=a;triangles[at++]=a+segments+1;triangles[at++]=a+1;triangles[at++]=a+1;triangles[at++]=a+segments+1;triangles[at++]=a+segments+2;}
                sheetMesh.vertices=sheetVertices;sheetMesh.uv=uv;sheetMesh.triangles=triangles;crownSheet.sharedMesh=sheetMesh;sheetRenderer=crownSheet.GetComponent<Renderer>();sheetRenderer.enabled=false;
            }
            interaction.SurfaceEntry+=Entry;Clear();
        }
        void Entry(Vector3 point,Vector3 velocity,float mass,bool replay)
        {
            if(match==null||!match.Running||match.Paused)return;
            lastEntryFrame=Time.frameCount;
            float normal=Mathf.Max(0,-velocity.y);
            // Real mass/velocity control displacement and spray; capped for readability.
            LastIncomingNormalSpeed=normal;LastImpactEnergy=.5f*Mathf.Max(1,mass)*normal*normal;
            float strength=Mathf.Clamp(Mathf.Sqrt(LastImpactEnergy/Mathf.Max(1,referenceImpactEnergy))*impactScale,.55f,2.1f);LastStrength=strength;
            if(replay)ReplayImpacts++;else LiveImpacts++;
            ImpactPoint=point;ImpactLaunch=match.Launches;
            float effectsTime=replay?match.replay.Clock:Time.time;
            waves[next++%waves.Length]=new Wave{point=point,born=effectsTime,strength=strength};
            sheetPoint=point;sheetBorn=effectsTime;sheetStrength=strength;if(sheetRenderer!=null)sheetRenderer.enabled=true;
            Crown(effectsTime);
            Emit(crown,point,velocity,strength,220,false,0x6831u);
            Emit(droplets,point,velocity,strength,180,false,0x9275u);
            Emit(mist,point,velocity,strength,36,true,0x4623u);
        }
        static void Emit(ParticleSystem ps,Vector3 point,Vector3 incoming,float strength,int count,bool vapor,uint seed)
        {
            if(ps==null)return;
            ps.transform.position=point;
            int amount=Mathf.Clamp(Mathf.RoundToInt(count*strength),16,320);
            float golden=2.39996323f;
            for(int i=0;i<amount;i++)
            {
                // Ballistic crown, varied droplets and a slower thin mist. Each emitter
                // replays the same bounded distribution; gravity/collision is Unity's.
                float phase=i*golden,variation=(Mathf.Sin(i*71.17f)+1)*.5f;
                float radius=vapor?variation*.9f:1.1f+variation*.8f;
                var radial=new Vector3(Mathf.Cos(phase),0,Mathf.Sin(phase));
                float up=vapor?2+variation*2:6+Mathf.Sqrt(strength)*3+variation*3;
                float outward=vapor?1.1f:3+variation*3;
                var direction=radial*outward*Mathf.Sqrt(strength)+Vector3.up*up+Vector3.ProjectOnPlane(incoming,Vector3.up)*.1f;
                var emit=new ParticleSystem.EmitParams{position=point+radial*radius+Vector3.up*.08f,velocity=direction,
                    startLifetime=vapor?1.3f+variation*.8f:1.5f+variation*1.1f,
                    startSize=vapor?.55f+variation*.7f:ps.name.Contains("droplet")?.06f+variation*.14f:.35f+variation*.45f,
                    randomSeed=seed+(uint)i};
                ps.Emit(emit,1);
            }
        }
        void LateUpdate()
        {
            if(mesh==null||match==null||match.Paused)return;
            if(!match.Running||match.Phase==DockPhase.Approach&&launch!=match.Launches){Clear();launch=match.Launches;}
            if(match.replay.Playing){if((priorCursor<0||match.replay.Clock<priorCursor)&&lastEntryFrame!=Time.frameCount)Clear();priorCursor=match.replay.Clock;}else priorCursor=-1;
            if(Time.time-lastUpdate<1f/30)return;lastUpdate=Time.time;
            float clock=Time.time;MaximumDisplacement=0;ActiveWaves=0;
            float effectsClock=match.replay.Playing?match.replay.Clock:clock;
            float speed=match.replay.Playing?match.replay.Speed:1;
            if(Mathf.Abs(speed-particleSpeed)>.001f){particleSpeed=speed;SetSpeed(crown,speed);SetSpeed(droplets,speed);SetSpeed(mist,speed);}
            for(int k=0;k<waves.Length;k++)if(waves[k].strength>0&&effectsClock-waves[k].born<6)ActiveWaves++;
            for(int i=0;i<rest.Length;i++)
            {
                var world=surface.transform.TransformPoint(rest[i]);
                float shore=Mathf.Clamp01(Mathf.Min(11-Mathf.Abs(world.x),Mathf.Min(world.z,130-world.z))*2);
                float h=ambientHeight*(Mathf.Sin(world.x*.4f+clock*.7f)+Mathf.Sin(world.z*.22f-clock*.5f)*.6f);
                for(int k=0;k<waves.Length;k++)
                {
                    var w=waves[k];float age=effectsClock-w.born;if(w.strength<=0||age<0||age>6)continue;
                    float distance=Vector2.Distance(new Vector2(world.x,world.z),new Vector2(w.point.x,w.point.z));
                    float front=distance-age*4.2f;
                    h+=Mathf.Cos(front*2.5f)*Mathf.Exp(-front*front*.3f)*w.strength*.28f*Mathf.Exp(-age*.65f)/Mathf.Sqrt(1+distance*.12f);
                }
                h=Mathf.Clamp(h*shore,-.65f,.65f);MaximumDisplacement=Mathf.Max(MaximumDisplacement,Mathf.Abs(h));
                world.y+=h;vertices[i]=surface.transform.InverseTransformPoint(world);
            }
            mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
            // Oscillation returns around the same UV origin: no river-like current.
            material.mainTextureOffset=new Vector2(Mathf.Sin(clock*.17f)*.009f,Mathf.Cos(clock*.13f)*.009f);
            Crown(effectsClock);
        }
        void Crown(float clock)
        {
            if(sheetMesh==null||sheetRenderer==null||!sheetRenderer.enabled)return;
            float age=clock-sheetBorn;float speed=7+Mathf.Sqrt(sheetStrength)*3.5f;
            float height=speed*age-.5f*9.81f*age*age;
            if(age>2.5f||height<0){sheetRenderer.enabled=false;return;}
            float fade=Mathf.Clamp01(age*12)*Mathf.Clamp01((1.15f-age)*2);
            for(int j=0;j<=4;j++)for(int i=0;i<=64;i++)
            {
                float u=j*.25f,angle=i*Mathf.PI*2/64;float lobe=1+Mathf.Sin(angle*7+sheetStrength)*.3f+Mathf.Sin(angle*11)*.12f;
                float radius=1.2f+(2.2f+sheetStrength)*age*u+Mathf.Sin(u*Mathf.PI)*.4f;
                var point=sheetPoint+new Vector3(Mathf.Cos(angle)*radius,height*Mathf.Pow(u,.8f)*lobe,Mathf.Sin(angle)*radius);
                int k=j*65+i;sheetVertices[k]=crownSheet.transform.InverseTransformPoint(point);
                float foam=Mathf.Pow(u,4);sheetColors[k]=new Color(Mathf.Lerp(.28f,.9f,foam),Mathf.Lerp(.7f,.98f,foam),1,fade*Mathf.Lerp(.17f,.78f,foam));
            }
            sheetMesh.vertices=sheetVertices;sheetMesh.colors=sheetColors;sheetMesh.RecalculateNormals();sheetMesh.RecalculateBounds();
        }
        public void Clear()
        {
            Array.Clear(waves,0,waves.Length);ActiveWaves=0;MaximumDisplacement=0;ImpactLaunch=-1;
            if(sheetRenderer!=null)sheetRenderer.enabled=false;
            Stop(crown);Stop(droplets);Stop(mist);
        }
        static void Stop(ParticleSystem ps){if(ps!=null)ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
        static void SetSpeed(ParticleSystem ps,float speed){if(ps!=null){var main=ps.main;main.simulationSpeed=speed;}}
        void OnDisable()
        {
            if(interaction!=null)interaction.SurfaceEntry-=Entry;Clear();
            if(surface!=null&&original!=null)surface.sharedMesh=original;
            if(water!=null&&originalMaterial!=null)water.sharedMaterial=originalMaterial;
            if(mesh!=null)Destroy(mesh);if(sheetMesh!=null)Destroy(sheetMesh);if(material!=null)Destroy(material);mesh=null;sheetMesh=null;material=null;
        }
    }
}
