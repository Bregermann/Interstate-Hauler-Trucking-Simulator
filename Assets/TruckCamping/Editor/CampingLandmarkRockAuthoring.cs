using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckCamping.Editor
{
    public static class CampingLandmarkRockAuthoring
    {
        public const string WorldSha="64f490c84f3076ccac500d9679a66066de97580865d6c16a792ddd3687dc11fe";
        public const string Source="Assets/Toby Fredson/Rocky Hills Environment - Whitebark Pine/RHEWP_Demo/Prefabs/Prefabs_Static/RockBigRHEWP-S.prefab";
        public const string SourceMeshName="RockBigRHE_LOD3";
        public const string MaterialPath="Assets/TruckCamping/Materials/Camp Haul natural closed rock URP.mat";
        public const string VariantPath="Assets/TruckCamping/Prefabs/Camp Haul natural closed rock render-only.prefab";
        public const string ProfilePath="Assets/TruckCamping/Resources/TruckCamping/Camp Haul landmark rock presentation.asset";
        [Serializable] sealed class FileFact { public string path,sha256;public long bytes; }
        [Serializable] sealed class Receipt { public string status="NEW_RENDER_ONLY_ASSETS_NO_ORIGINAL_WORLD_OR_VENDOR_WRITES";public FileFact[] outputs,preservedInputs; }
        static FileFact Fact(string path)=>new FileFact {path=path,sha256=Digest(path),bytes=new FileInfo(path).Length};
        static void Require(bool value,string reason){if(!value)throw new InvalidOperationException("Known four-cluster rock presentation: "+reason+"; custom content preserved.");}
        public static string Digest(string p){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant();}
        public static MeshFilter SourceFilter()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Source);Require(prefab!=null,"installed closed Toby boulder");
            var filter=prefab.GetComponentsInChildren<MeshFilter>(true).SingleOrDefault(f=>f.name==SourceMeshName);Require(filter!=null&&filter.sharedMesh!=null,"actual authored LOD3 renderer");return filter;
        }
        static Matrix4x4 SourceMatrix()=>SourceFilter().transform.localToWorldMatrix;
        static Bounds BoundsOf(Vector3[] vertices){Require(vertices.Length>0,"real mesh vertices");var b=new Bounds(vertices[0],Vector3.zero);foreach(var p in vertices)b.Encapsulate(p);return b;}
        public static Material CreateOwnedMaterial()
        {
            var renderer=SourceFilter().GetComponent<MeshRenderer>();Require(renderer!=null&&renderer.sharedMaterials.Length==1,"original single source material");var original=renderer.sharedMaterial;
            var texture=original.HasProperty("_BaseMap")?original.GetTexture("_BaseMap"):original.mainTexture;Require(texture!=null,"original real rock albedo");
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Camp Haul closed natural rock opaque URP",enableInstancing=true};
            material.SetTexture("_BaseMap",texture);material.SetTextureScale("_BaseMap",original.GetTextureScale(original.HasProperty("_BaseMap")?"_BaseMap":"_MainTex"));material.SetTextureOffset("_BaseMap",original.GetTextureOffset(original.HasProperty("_BaseMap")?"_BaseMap":"_MainTex"));material.SetColor("_BaseColor",Color.white);material.SetFloat("_Smoothness",.12f);material.SetFloat("_Metallic",0);
            if(original.HasProperty("_BumpMap")&&original.GetTexture("_BumpMap")!=null){material.SetTexture("_BumpMap",original.GetTexture("_BumpMap"));material.SetFloat("_BumpScale",original.HasProperty("_BumpScale")?original.GetFloat("_BumpScale"):1);material.EnableKeyword("_NORMALMAP");}return material;
        }
        public static GameObject CreateIsolatedSourceRender()
        {
            // A clean renderer hierarchy with the FULL authored source matrix,
            // never instantiate the vendor's LODGroup/collider/scripts.
            var filter=SourceFilter();var matrix=SourceMatrix();var root=new GameObject("Original closed boulder authored basis render only");var child=new GameObject("Original LOD3 source renderer");child.transform.SetParent(root.transform,false);child.transform.localPosition=matrix.GetColumn(3);child.transform.localRotation=matrix.rotation;child.transform.localScale=matrix.lossyScale;child.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;child.AddComponent<MeshRenderer>().sharedMaterials=filter.GetComponent<MeshRenderer>().sharedMaterials;return root;
        }
        static GameObject Variant(Material material)
        {
            var root=CreateIsolatedSourceRender();root.name="Camp Haul closed natural rock render only";var child=root.transform.GetChild(0);var points=ReadActualVertices(child.GetComponent<MeshFilter>().sharedMesh).Select(child.TransformPoint).ToArray();child.localPosition-=BoundsOf(points).center;child.GetComponent<MeshRenderer>().sharedMaterial=material;return root;
        }
        public static Vector3[] BoulderVertices(CampLandmarkRockProfile profile)
        {var child=profile.boulderPrefab.GetComponentInChildren<MeshFilter>(true);var matrix=Matrix4x4.TRS(child.transform.localPosition,child.transform.localRotation,child.transform.localScale);return ReadActualVertices(child.sharedMesh).Select(matrix.MultiplyPoint3x4).ToArray();}
        public static string VerifyActualSourceNormalization(CampLandmarkRockProfile profile)
        {
            var raw=ReadActualVertices(SourceFilter().sharedMesh);var matrix=SourceMatrix();var before=raw.Select(matrix.MultiplyPoint3x4).ToArray();var center=BoundsOf(before).center;var after=BoulderVertices(profile);Require(raw.Length==after.Length,"same genuine source vertex count");float error=0;for(int i=0;i<raw.Length;i++)error=Mathf.Max(error,(before[i]-center-after[i]).magnitude);Require(error<.00001f,"actual authored-matrix vertex identity after translation only");
            var bounds=BoundsOf(before);var normalized=BoundsOf(after);Require((bounds.size-normalized.size).magnitude<.00001f,"actual renderer dimensions preserved");Require(ClosedBoundaryCount(profile.originalRockAMesh)==0,"closed genuine mesh; no open shell or generated cap");
            return "ACTUAL_SOURCE_NORMALIZATION vertexCount="+raw.Length+" actualSourceBounds="+bounds.ToString("R")+" normalized="+normalized.ToString("R")+" maxVertexError="+error.ToString("R")+" authoredMatrix="+matrix+" weldedBoundaryEdges=0";
        }
        static int[] PathOf(Transform world,Transform child)
        {var parts=new List<int>();while(child!=world){Require(child!=null,"target under original world");parts.Add(child.GetSiblingIndex());child=child.parent;}parts.Reverse();return parts.ToArray();}
        static float Ground(CampingWilderness world,Vector3 local)
        {
            Vector3 p=world.transform.TransformPoint(local);
            foreach(var terrain in world.terrains){var q=p-terrain.transform.position;var size=terrain.terrainData.size;if(q.x>=0&&q.z>=0&&q.x<=size.x&&q.z<=size.z)return terrain.terrainData.GetInterpolatedHeight(q.x/size.x,q.z/size.z)+terrain.transform.position.y-world.transform.position.y;}
            throw new InvalidOperationException("No actual terrain under boulder footing.");
        }
        static Bounds LocalBounds(CampingWilderness world,Bounds bounds)=>new Bounds(world.transform.InverseTransformPoint(bounds.center),bounds.size);
        public static Vector3[] ReadActualVertices(Mesh mesh)
        {
            // Editor read-only data access; never toggle the vendor importer.
            using(var data=MeshUtility.AcquireReadOnlyMeshData(mesh))
            using(var vertices=new Unity.Collections.NativeArray<Vector3>(data[0].vertexCount,Unity.Collections.Allocator.Temp))
            {data[0].GetVertices(vertices);return vertices.ToArray();}
        }
        public static int[] ReadActualTriangles(Mesh mesh)
        {
            using(var data=MeshUtility.AcquireReadOnlyMeshData(mesh))
            {var list=new List<int>();for(int sub=0;sub<data[0].subMeshCount;sub++){var desc=data[0].GetSubMesh(sub);Require(desc.topology==MeshTopology.Triangles,"actual triangular source surface");using(var indices=new Unity.Collections.NativeArray<int>(desc.indexCount,Unity.Collections.Allocator.Temp)){data[0].GetIndices(indices,sub,true);list.AddRange(indices.ToArray());}}return list.ToArray();}
        }
        public static int ClosedBoundaryCount(Mesh mesh)
        {
            var vertices=ReadActualVertices(mesh);var unique=new Dictionary<Vector3Int,int>();int[] ids=new int[vertices.Length];
            for(int i=0;i<vertices.Length;i++){var v=vertices[i];var key=new Vector3Int(Mathf.RoundToInt(v.x*100000),Mathf.RoundToInt(v.y*100000),Mathf.RoundToInt(v.z*100000));if(!unique.TryGetValue(key,out int id)){id=unique.Count;unique.Add(key,id);}ids[i]=id;}
            var edges=new Dictionary<long,int>();var tri=ReadActualTriangles(mesh);for(int i=0;i<tri.Length;i+=3)for(int n=0;n<3;n++){int a=ids[tri[i+n]],b=ids[tri[i+(n+1)%3]];long key=((long)Mathf.Min(a,b)<<32)|(uint)Mathf.Max(a,b);if(edges.ContainsKey(key))edges[key]++;else edges.Add(key,1);}return edges.Count(e=>e.Value!=2);
        }
        static float[] SurfaceCrossings(Vector3[] vertices,int[] triangles,float x,float z)
        {
            var hits=new List<float>();
            for(int i=0;i<triangles.Length;i+=3)
            {
                var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];float determinant=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);if(Mathf.Abs(determinant)<.0000001f)continue;
                float u=((b.z-c.z)*(x-c.x)+(c.x-b.x)*(z-c.z))/determinant,v=((c.z-a.z)*(x-c.x)+(a.x-c.x)*(z-c.z))/determinant,w=1-u-v;if(u<-.00001f||v<-.00001f||w<-.00001f)continue;float y=u*a.y+v*b.y+w*c.y;if(!hits.Any(h=>Mathf.Abs(h-y)<.0001f))hits.Add(y);
            }
            hits.Sort();return hits.ToArray();
        }
        public static Vector2 VerticalSurfaceInterval(Vector3[] vertices,int[] triangles,float x,float z)
        {var hits=SurfaceCrossings(vertices,triangles,x,z);Require(hits.Length>0,"real triangle surface missing at XZ=("+x.ToString("R")+","+z.ToString("R")+") actualVertexBounds="+BoundsOf(vertices).ToString("R")+" triangleCount="+triangles.Length/3);return new Vector2(hits[0],hits[hits.Length-1]);}
        static Vector2 NamedSurface(Vector3[] vertices,int[] triangles,float x,float z,string context)
        {try{return VerticalSurfaceInterval(vertices,triangles,x,z);}catch(InvalidOperationException e){throw new InvalidOperationException(context+": "+e.Message,e);}}
        static List<Vector3> ClipSupportPolygon(List<Vector3> points,int axis,float plane,bool keepAbove)
        {
            var result=new List<Vector3>();if(points.Count==0)return result;var previous=points[points.Count-1];bool wasInside=keepAbove?previous[axis]>=plane:previous[axis]<=plane;
            foreach(var current in points){bool inside=keepAbove?current[axis]>=plane:current[axis]<=plane;if(inside!=wasInside){float t=(plane-previous[axis])/(current[axis]-previous[axis]);result.Add(Vector3.LerpUnclamped(previous,current,t));}if(inside)result.Add(current);previous=current;wasInside=inside;}return result;
        }
        public static Vector3 ChooseActualRoofSupport(Vector3[] roof,int[] triangles,Bounds footprint,string context)
        {
            // A bounding-box centre can be empty between irregular roof pieces.
            // Clip REAL source triangles to the inner60% of this unchanged
            // pillar footprint; choose the nearest interior point, never a
            // synthetic height or extrapolated surface outside a triangle.
            Vector3 center=footprint.center;float xmin=center.x-footprint.size.x*.3f,xmax=center.x+footprint.size.x*.3f,zmin=center.z-footprint.size.z*.3f,zmax=center.z+footprint.size.z*.3f;Vector3 chosen=default;float best=float.PositiveInfinity;int feasible=0;
            for(int i=0;i<triangles.Length;i+=3)
            {
                var polygon=new List<Vector3>{roof[triangles[i]],roof[triangles[i+1]],roof[triangles[i+2]]};polygon=ClipSupportPolygon(polygon,0,xmin,true);polygon=ClipSupportPolygon(polygon,0,xmax,false);polygon=ClipSupportPolygon(polygon,2,zmin,true);polygon=ClipSupportPolygon(polygon,2,zmax,false);if(polygon.Count<3)continue;
                float area=0;Vector3 centroid=Vector3.zero;for(int v=0;v<polygon.Count;v++){var a=polygon[v];var b=polygon[(v+1)%polygon.Count];area+=a.x*b.z-b.x*a.z;centroid+=a;}if(Mathf.Abs(area)<.00001f)continue;centroid/=polygon.Count;
                Vector3 nearest=centroid;float distance=float.PositiveInfinity;bool inside=true;float sign=0;
                for(int v=0;v<polygon.Count;v++)
                {
                    var a=polygon[v];var b=polygon[(v+1)%polygon.Count];var edge=new Vector2(b.x-a.x,b.z-a.z);var delta=new Vector2(center.x-a.x,center.z-a.z);float cross=edge.x*delta.y-edge.y*delta.x;if(Mathf.Abs(cross)>.00001f){if(sign==0)sign=Mathf.Sign(cross);else if(Mathf.Sign(cross)!=sign)inside=false;}
                    float t=edge.sqrMagnitude<.0000001f?0:Mathf.Clamp01(Vector2.Dot(delta,edge)/edge.sqrMagnitude);var point=Vector3.LerpUnclamped(a,b,t);float d=new Vector2(point.x-center.x,point.z-center.z).sqrMagnitude;if(d<distance){nearest=point;distance=d;}
                }
                if(inside)nearest=new Vector3(center.x,centroid.y,center.z);var candidate=Vector3.LerpUnclamped(nearest,centroid,.02f);var hits=SurfaceCrossings(roof,triangles,candidate.x,candidate.z);if(hits.Length==0)continue;candidate.y=hits[0];feasible++;float score=new Vector2(candidate.x-center.x,candidate.z-center.z).sqrMagnitude;
                if(score<best-.000001f||Mathf.Abs(score-best)<.000001f&&candidate.y<chosen.y){best=score;chosen=candidate;}
            }
            Require(feasible>0,context+" has no REAL roof triangle in inner footprint; originalCentre="+center.ToString("R")+" footprint="+footprint.ToString("R")+" roofVertices="+BoundsOf(roof).ToString("R")+" triangles="+triangles.Length/3);
            Debug.Log("CAMPING_REAL_ROOF_SUPPORT "+context+" originalCentre="+center.ToString("R")+" originalCentreHits="+SurfaceCrossings(roof,triangles,center.x,center.z).Length+" selectedActualTrianglePoint="+chosen.ToString("R")+" candidateTriangles="+feasible+" XZshift="+Mathf.Sqrt(best).ToString("R"));return chosen;
        }
        static bool InsideRealClosedSurface(Vector3[] vertices,int[] triangles,float x,float y,float z)
        {var hits=SurfaceCrossings(vertices,triangles,x,z);return hits.Count(h=>h>y)%2==1;}
        static MeshFilter CaveRoof(CampingWilderness world)=>world.landmarks[6].GetComponentsInChildren<MeshFilter>(true).Single(f=>f.name=="Rock_Cluster_1A"&&Mathf.Abs(f.GetComponent<BoxCollider>().bounds.size.y-8)<.02f);
        public static CampLandmarkRockProfile CreateIsolatedProfile()
        {
            var material=CreateOwnedMaterial();var variant=Variant(material);variant.SetActive(false);var profile=ScriptableObject.CreateInstance<CampLandmarkRockProfile>();profile.boulderPrefab=variant;profile.originalRockAMesh=variant.GetComponentInChildren<MeshFilter>(true).sharedMesh;profile.ownedMaterial=material;VerifyActualSourceNormalization(profile);return profile;
        }
        static Terrain TerrainAt(CampingWilderness world,Vector3 worldPoint)
        {
            foreach(var terrain in world.terrains){var p=terrain.transform.InverseTransformPoint(worldPoint);var size=terrain.terrainData.size;if(p.x>=0&&p.z>=0&&p.x<=size.x&&p.z<=size.z)return terrain;}throw new InvalidOperationException("No actual terrain cell at "+worldPoint.ToString("R"));
        }
        public static float ActualTerrainTriangleHeight(CampingWilderness world,Vector3 local)
        {
            Vector3 point=world.transform.TransformPoint(local);var terrain=TerrainAt(world,point);var p=terrain.transform.InverseTransformPoint(point);var data=terrain.terrainData;int n=data.heightmapResolution-1;float gx=p.x/data.size.x*n,gz=p.z/data.size.z*n;int ix=Mathf.Clamp(Mathf.FloorToInt(gx),0,n-1),iz=Mathf.Clamp(Mathf.FloorToInt(gz),0,n-1);float u=Mathf.Clamp01(gx-ix),v=Mathf.Clamp01(gz-iz);var heights=data.GetHeights(ix,iz,2,2);
            // Actual heightfield cell triangles, not a scalar-only bilinear
            // height. Both diagonals coincide on the authored flat clearings.
            float height=u+v<=1?heights[0,0]+u*(heights[0,1]-heights[0,0])+v*(heights[1,0]-heights[0,0]):heights[1,1]+(1-u)*(heights[1,0]-heights[1,1])+(1-v)*(heights[0,1]-heights[1,1]);
            return terrain.transform.TransformPoint(new Vector3(p.x,height*data.size.y,p.z)).y-world.transform.position.y;
        }
        static List<Vector3> LowerSurfaceSamples(Vector3[] vertices,int[] triangles,int resolution=11)
        {
            var bounds=BoundsOf(vertices);var points=new List<Vector3>();for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++){float px=Mathf.Lerp(bounds.min.x,bounds.max.x,(x+.5f)/resolution),pz=Mathf.Lerp(bounds.min.z,bounds.max.z,(z+.5f)/resolution);var hits=SurfaceCrossings(vertices,triangles,px,pz);if(hits.Length>=2)points.Add(new Vector3(px,hits[0],pz));}Require(points.Count>=20,"real closed lower surface sampling");return points;
        }
        static float EmbedBase(CampingWilderness world,Vector3[] vertices,int[] triangles)
        {
            var gaps=LowerSurfaceSamples(vertices,triangles).Select(v=>ActualTerrainTriangleHeight(world,v)-v.y).OrderBy(v=>v).ToArray();
            // Put a broad lower-surface patch inside the real terrain. A single
            // .08m vertex touch left the visible mass apparently suspended.
            return gaps[Mathf.Clamp(Mathf.FloorToInt(gaps.Length*.72f),0,gaps.Length-1)]-.5f;
        }
        static CampLandmarkRockProfile.Target Fingerprint(CampingWilderness world,BoxCollider box,int index,bool cap=false)
        {return new CampLandmarkRockProfile.Target {landmark=index,overheadCap=cap,siblingPath=PathOf(world.transform,box.transform),sourcePosition=box.transform.localPosition,sourceScale=box.transform.localScale,sourceRotation=box.transform.localRotation,colliderCenter=box.center,colliderSize=box.size,sourceMesh=box.GetComponent<MeshFilter>().sharedMesh,sourceMaterials=box.GetComponent<MeshRenderer>().sharedMaterials,sourceBounds=LocalBounds(world,box.bounds)};}
        static void SetEnvelope(CampLandmarkRockProfile profile,CampLandmarkRockProfile.Target target)
        {
            var bounds=target.sourceBounds;foreach(var pose in target.boulders)bounds.Encapsulate(CampLandmarkRockPresentation.TransformBounds(CampLandmarkRockPresentation.UnitBounds(profile),pose.rotation,pose.uniformScale,pose.position));target.allowedEnvelope=bounds;
        }
        static CampLandmarkRockProfile.Pose FitConnectedHeadwater(CampLandmarkRockProfile profile,CampLandmarkRockProfile.Target bank,Vector3 desired,Quaternion rotation,float scale,Bounds region,float realWaterTop)
        {
            var vertices=BoulderVertices(profile);var triangles=ReadActualTriangles(profile.originalRockAMesh);var supports=bank.boulders.Take(3).Select(p=>vertices.Select(v=>p.position+p.rotation*v*p.uniformScale).ToArray()).ToArray();
            for(int step=0;step<=64;step++)
            {
                var point=desired-Vector3.up*(step*.1f);var conservative=CampLandmarkRockPresentation.TransformBounds(CampLandmarkRockPresentation.UnitBounds(profile),rotation,scale,point);if(conservative.min.x<region.min.x-.001f||conservative.max.x>region.max.x+.001f||conservative.min.z<region.min.z-.001f||conservative.max.z>region.max.z+.001f||conservative.min.y<region.min.y||conservative.max.y>region.max.y)continue;
                var actual=vertices.Select(v=>point+rotation*v*scale).ToArray();if(BoundsOf(actual).max.y<realWaterTop+.05f)continue;
                for(int support=0;support<supports.Length;support++)if(SolidWitness(actual,supports[support],triangles,out var witness))
                {Debug.Log("CAMPING_REAL_HEADWATER_CONNECTION bank="+string.Join("/",bank.siblingPath)+" supportBoulder="+support+" downwardSteps="+step+" pose="+point.ToString("R")+" actualInteriorWitness="+witness.ToString("R")+" realRockBounds="+BoundsOf(actual).ToString("R")+" waterTop="+realWaterTop.ToString("R"));return new CampLandmarkRockProfile.Pose {position=point,rotation=rotation,uniformScale=scale};}
            }
            throw new InvalidOperationException("No real closed-headwater/bank connection within original region and above actual sheet top: bank="+string.Join("/",bank.siblingPath)+" desired="+desired.ToString("R")+" region="+region.ToString("R")+" supportBounds="+string.Join(";",supports.Select(v=>BoundsOf(v).ToString("R"))));
        }
        public static CampLandmarkRockProfile CreatePreviewProfile(CampingWilderness world)
        {
            Require(world!=null&&world.landmarks.Length==12&&world.terrains.Length==4,"original shared wilderness");var profile=CreateIsolatedProfile();var vertices=BoulderVertices(profile);var triangles=ReadActualTriangles(profile.originalRockAMesh);var targets=new List<CampLandmarkRockProfile.Target>();
            foreach(int index in new[]{2,6})
            {
                var chosen=world.landmarks[index].GetComponentsInChildren<BoxCollider>(true).Where(c=>c.transform.parent==world.landmarks[index].transform&&c.name=="Rock_Cluster_1A"&&c.GetComponent<MeshRenderer>()!=null&&c.GetComponent<MeshFilter>()!=null).Where(c=>index==2?Mathf.Abs(c.bounds.size.y-13)<.02f||Mathf.Abs(c.bounds.size.y-15)<.02f:Mathf.Abs(c.bounds.size.y-12)<.02f).OrderBy(c=>c.bounds.center.x).ToArray();Require(chosen.Length==2,"two exact original side masses per landmark");
                foreach(var box in chosen)
                {
                    var original=LocalBounds(world,box.GetComponent<Renderer>().bounds);float x=original.center.x,z=original.center.z;var region=CampLandmarkRockPresentation.LandmarkRenderEnvelope(world,index);float ground=ActualTerrainTriangleHeight(world,new Vector3(x,0,z));float targetTop=index==6?22.5f:original.max.y;
                    Quaternion[] rotations={Quaternion.Euler(-5,12,-3),Quaternion.Euler(6,-17,4),Quaternion.Euler(-4,27,-4)};float[] factors={1,.91f,.82f};Vector3[] shifts={Vector3.zero,new Vector3(.28f,0,-.25f),new Vector3(-.25f,0,.28f)};
                    var rotated=rotations.Select((q,row)=>vertices.Select(v=>q*v*factors[row]+shifts[row]).ToArray()).ToArray();var spans=rotated.Select((v,row)=>NamedSurface(v,triangles,0,0,"broad upright mass landmark="+index+" sibling="+box.transform.GetSiblingIndex()+" row="+row)).ToArray();float[] y={0,0,0};
                    for(int row=1;row<3;row++)y[row]=y[row-1]+spans[row-1].y-spans[row].x-Mathf.Min(spans[row-1].y-spans[row-1].x,spans[row].y-spans[row].x)*.20f;
                    float layout=rotated.Select((v,row)=>v.Max(p=>p.y)+y[row]).Max()-rotated[0].Min(v=>v.y);float scale=(targetTop-ground+.7f)/layout;
                    for(int row=0;row<3;row++){var b=CampLandmarkRockPresentation.TransformBounds(CampLandmarkRockPresentation.UnitBounds(profile),rotations[row],factors[row],shifts[row]);scale=Mathf.Min(scale,original.size.x*.94f/b.size.x,original.size.z*.94f/b.size.z);}
                    float offset=0;for(int iteration=0;iteration<4;iteration++)
                    {
                        var baseVertices=rotated[0].Select(v=>new Vector3(x+v.x*scale,v.y*scale,z+v.z*scale)).ToArray();offset=EmbedBase(world,baseVertices,triangles);
                        float top=rotations.Select((q,row)=>CampLandmarkRockPresentation.TransformBounds(CampLandmarkRockPresentation.UnitBounds(profile),q,scale*factors[row],new Vector3(x+shifts[row].x*scale,offset+y[row]*scale,z+shifts[row].z*scale)).max.y).Max();if(top<=region.max.y-.01f)break;scale*=Mathf.Max(.1f,(region.max.y-.02f-offset)/(top-offset));
                    }
                    var target=Fingerprint(world,box,index);var poses=new List<CampLandmarkRockProfile.Pose>();for(int row=0;row<3;row++)poses.Add(new CampLandmarkRockProfile.Pose {position=new Vector3(x+shifts[row].x*scale,offset+y[row]*scale,z+shifts[row].z*scale),rotation=rotations[row],uniformScale=scale*factors[row]});target.boulders=poses.ToArray();SetEnvelope(profile,target);targets.Add(target);
                }
            }
            // Two headwater boulders mask the unchanged rectangular sheet's
            // top edge. They touch both broad banks; no water reference writes.
            var waterfall=world.landmarks[2].GetComponentsInChildren<Renderer>(true).Single(r=>r.name=="River Modeler flowing silver cascade");var water=LocalBounds(world,waterfall.bounds);var head=new List<CampLandmarkRockProfile.Pose>(targets[0].boulders);
            var waterfallRegion=CampLandmarkRockPresentation.LandmarkRenderEnvelope(world,2);
            head.Add(FitConnectedHeadwater(profile,targets[0],new Vector3(water.center.x-2.1f,water.max.y-1.1f,water.center.z+3.4f),Quaternion.Euler(45,18,2),.76f,waterfallRegion,water.max.y));
            head.Add(FitConnectedHeadwater(profile,targets[1],new Vector3(water.center.x+4.4f,water.max.y-1.05f,water.center.z+4.0f),Quaternion.Euler(55,-8,-3),.88f,waterfallRegion,water.max.y));targets[0].boulders=head.ToArray();SetEnvelope(profile,targets[0]);
            // Replace ONLY the original cap renderer; its transform/collider
            // remains active and exact. Three overlapping horizontal closed
            // masses bridge the original opening instead of black blade art.
            var roof=CaveRoof(world);var cap=Fingerprint(world,roof.GetComponent<BoxCollider>(),6,true);var oldRoof=LocalBounds(world,roof.GetComponent<Renderer>().bounds);var caps=new List<CampLandmarkRockProfile.Pose>();
            for(int i=0;i<3;i++)caps.Add(new CampLandmarkRockProfile.Pose {position=new Vector3(oldRoof.center.x+(i-1)*9.0f,22.1f+(i==1?.35f:0),oldRoof.center.z),rotation=Quaternion.Euler(i==1?-3:3,i==0?9:i==1?-8:14,i==0?3:-3),uniformScale=i==1?1.24f:1.20f});cap.boulders=caps.ToArray();SetEnvelope(profile,cap);targets.Add(cap);profile.targets=targets.ToArray();Require(profile.targets.Sum(t=>t.boulders.Length)==17,"bounded17 genuine closed rocks");
            foreach(var t in profile.targets)Debug.Log("CAMPING_BROAD_ROCK_RECIPE landmark="+t.landmark+" cap="+t.overheadCap+" source="+string.Join("/",t.siblingPath)+" envelope="+t.allowedEnvelope.ToString("R")+" poses="+string.Join(";",t.boulders.Select(p=>p.position.ToString("R")+" scale="+p.uniformScale.ToString("R"))));return profile;
        }
        static bool SolidWitness(Vector3[] a,Vector3[] b,int[] triangles,out Vector3 witness)
        {
            witness=default;var ba=BoundsOf(a);var bb=BoundsOf(b);if(!ba.Intersects(bb))return false;var min=Vector3.Max(ba.min,bb.min);var max=Vector3.Min(ba.max,bb.max);if(max.y-min.y<.15f)return false;
            for(int z=0;z<5;z++)for(int x=0;x<5;x++){float px=Mathf.Lerp(min.x,max.x,(x+.5f)/5),pz=Mathf.Lerp(min.z,max.z,(z+.5f)/5);var ah=SurfaceCrossings(a,triangles,px,pz);var bh=SurfaceCrossings(b,triangles,px,pz);if(ah.Length<2||bh.Length<2)continue;float low=Mathf.Max(ah[0],bh[0]),high=Mathf.Min(ah[ah.Length-1],bh[bh.Length-1]);if(high-low<.15f)continue;float py=(low+high)*.5f;if(InsideRealClosedSurface(a,triangles,px,py,pz)&&InsideRealClosedSurface(b,triangles,px,py,pz)){witness=new Vector3(px,py,pz);return true;}}return false;
        }
        public static string VerifyActualRenderedPlacement(CampingWilderness world,CampLandmarkRockProfile profile,Transform visualRoot)
        {
            var raw=ReadActualVertices(profile.originalRockAMesh);var tri=ReadActualTriangles(profile.originalRockAMesh);int count=visualRoot.childCount;Require(count==17,"17 bounded render-only masses");var actual=new Vector3[count][];for(int i=0;i<count;i++){var frame=visualRoot.GetChild(i).GetComponentInChildren<MeshFilter>().transform;actual[i]=raw.Select(frame.TransformPoint).ToArray();Require(BoundsOf(actual[i]).size.y>2,"real closed mass height");}
            var report=new List<string>();var grounded=new HashSet<int>();int start=0;
            foreach(var target in profile.targets)
            {
                if(!target.overheadCap)
                {
                    var points=LowerSurfaceSamples(actual[start],tri);int embedded=0;float minGap=float.PositiveInfinity,maxDifference=0;var b=BoundsOf(actual[start]);float areaPerCell=b.size.x*b.size.z/121;
                    foreach(var lower in points)
                    {
                        var local=world.transform.InverseTransformPoint(lower);float ground=ActualTerrainTriangleHeight(world,local)+world.transform.position.y;var intervals=SurfaceCrossings(actual[start],tri,lower.x,lower.z);float gap=lower.y-ground;minGap=Mathf.Min(minGap,gap);if(gap<-.25f&&intervals[intervals.Length-1]>ground+.3f)embedded++;
                        if(Application.isPlaying){var terrain=TerrainAt(world,lower);var collider=terrain.GetComponent<TerrainCollider>();RaycastHit hit=default;Require(collider!=null&&collider.Raycast(new Ray(new Vector3(lower.x,ground+300,lower.z),Vector3.down),out hit,600),"actual terrain collider under visible base "+lower.ToString("R"));maxDifference=Mathf.Max(maxDifference,Mathf.Abs(hit.point.y-ground));}
                    }
                    float fraction=embedded/(float)points.Count,patch=embedded*areaPerCell;Debug.Log("CAMPING_ACTUAL_TERRAIN_TRIANGLE_CONTACT landmark="+target.landmark+" source="+string.Join("/",target.siblingPath)+" actualBaseBounds="+b.ToString("R")+" lowerSamples="+points.Count+" embedded="+embedded+" patchArea="+patch.ToString("R")+" fraction="+fraction.ToString("R")+" minGap="+minGap.ToString("R")+" actualColliderRaysExecuted="+Application.isPlaying+" actualColliderHeightDelta="+maxDifference.ToString("R")+" verticesAndActualHeightfieldTriangles=true");Require(fraction>=.18f&&patch>2,"broad actual lower-surface/terrain TRIANGLE intersection patch, not a single vertex; landmark="+target.landmark+" samples="+points.Count+" embedded="+embedded+" patch="+patch+" minGap="+minGap);Require(maxDifference<.12f,"actual TerrainCollider ray agrees with sampled rendered heightfield triangles; delta="+maxDifference);grounded.Add(start);report.Add("terrainTriangleContact target="+target.landmark+" source="+string.Join("/",target.siblingPath)+" samples="+points.Count+" embedded="+embedded+" patchArea="+patch.ToString("R")+" fraction="+fraction.ToString("R")+" minGap="+minGap.ToString("R")+" actualColliderRaysExecuted="+Application.isPlaying+" actualColliderHeightDelta="+maxDifference.ToString("R"));
                }
                start+=target.boulders.Length;
            }
            var links=new bool[count,count];for(int i=0;i<count;i++)for(int j=i+1;j<count;j++)if(SolidWitness(actual[i],actual[j],tri,out var witness)){links[i,j]=links[j,i]=true;report.Add("jointInsideWitness="+i+"/"+j+" "+witness.ToString("R"));}
            Debug.Log("CAMPING_ACTUAL_CLOSED_SOLID_LINKS "+string.Join(";",report.Where(r=>r.StartsWith("jointInsideWitness="))));
            var reached=new HashSet<int>(grounded);bool changed=true;while(changed){changed=false;for(int i=0;i<count;i++)if(!reached.Contains(i)&&Enumerable.Range(0,count).Any(j=>reached.Contains(j)&&links[i,j])){reached.Add(i);changed=true;}}Require(reached.Count==count,"every visible mass/cap must have REAL closed-solid connection to an actual terrain-contact base; disconnected="+string.Join(",",Enumerable.Range(0,count).Where(i=>!reached.Contains(i))));
            int caveLeft=profile.targets[0].boulders.Length+profile.targets[1].boulders.Length,caveRight=caveLeft+profile.targets[2].boulders.Length;var bridge=new HashSet<int>{caveLeft};changed=true;while(changed){changed=false;for(int i=caveLeft;i<count;i++)if(!bridge.Contains(i)&&bridge.Any(j=>links[i,j])){bridge.Add(i);changed=true;}}Require(bridge.Contains(caveRight),"actual solid cap bridges BOTH original side masses; no floating roof blades");report.Add("realSolidRoofBridge=true all17Connected=true");return string.Join(";",report);
        }
        public static void DestroyPreview(CampLandmarkRockProfile profile)
        {if(profile==null)return;if(profile.boulderPrefab!=null&&!AssetDatabase.Contains(profile.boulderPrefab))UnityEngine.Object.DestroyImmediate(profile.boulderPrefab);if(profile.ownedMaterial!=null&&!AssetDatabase.Contains(profile.ownedMaterial))UnityEngine.Object.DestroyImmediate(profile.ownedMaterial);UnityEngine.Object.DestroyImmediate(profile);}
        [MenuItem("Truck Sports/Impossible Games/Create NEW Camping Landmark Rock Presentation Assets")]
        public static void CreateNewProfileAndVariant()
        {
            Require(!EditorApplication.isPlaying&&!EditorApplication.isCompiling,"stopped explicit author");Require(Digest(CampingWilderness.SharedPrefab)==WorldSha,"exact current shared world remains untouched");
            foreach(string path in new[]{MaterialPath,VariantPath,ProfilePath})Require(!File.Exists(path)&&!File.Exists(path+".meta"),"new outputs only; never overwrite designer assets "+path);
            var files=AssetDatabase.GetDependencies(Source,true).Where(File.Exists).SelectMany(p=>File.Exists(p+".meta")?new[]{p,p+".meta"}:new[]{p}).Concat(new[]{CampingWilderness.SharedPrefab,CampingWilderness.SharedPrefab+".meta"}).Distinct().ToArray();var hashes=files.Select(Digest).ToArray();
            var detached=PrefabUtility.LoadPrefabContents(CampingWilderness.SharedPrefab);CampLandmarkRockProfile profile=null;
            try
            {
                var world=detached.GetComponent<CampingWilderness>();profile=CreatePreviewProfile(world);
                var probe=detached.AddComponent<CampLandmarkRockPresentation>();Require(probe.Validate(world,profile,out string reason),reason);
                foreach(string path in new[]{MaterialPath,VariantPath,ProfilePath}){string dir=Path.GetDirectoryName(path);if(!Directory.Exists(dir))Directory.CreateDirectory(dir);}AssetDatabase.Refresh();
                AssetDatabase.CreateAsset(profile.ownedMaterial,MaterialPath);AssetDatabase.SaveAssetIfDirty(profile.ownedMaterial);
                profile.boulderPrefab.SetActive(true);var prefab=PrefabUtility.SaveAsPrefabAsset(profile.boulderPrefab,VariantPath);UnityEngine.Object.DestroyImmediate(profile.boulderPrefab);profile.boulderPrefab=prefab;
                AssetDatabase.CreateAsset(profile,ProfilePath);AssetDatabase.SaveAssetIfDirty(profile);
                for(int i=0;i<files.Length;i++)Require(Digest(files[i])==hashes[i],"unchanged original world/vendor dependency "+files[i]);
                string receipt=Path.GetFullPath("Logs/TruckSports/Impossible07/CampingLandmarkRockAuthor/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N")+"/receipt.json");Directory.CreateDirectory(Path.GetDirectoryName(receipt));
                File.WriteAllText(receipt,JsonUtility.ToJson(new Receipt { outputs=new[]{MaterialPath,VariantPath,ProfilePath}.SelectMany(p=>new[]{p,p+".meta"}).Select(Fact).ToArray(),preservedInputs=files.Select(Fact).ToArray() },true),new System.Text.UTF8Encoding(false));
                Debug.Log("CAMPING_LANDMARK_ROCK_RECEIPT "+receipt);
                Debug.Log("CAMPING_LANDMARK_ROCK_ASSETS new5target/17uniformClosedRock visuals; originalworld="+Digest(CampingWilderness.SharedPrefab)+" preserved. No scene/shared-prefab edits, physics/collider changes or vendor writes. Profile designer-editable; actual visual comparison still required.");
            }
            finally {if(profile!=null&&!AssetDatabase.Contains(profile))DestroyPreview(profile);PrefabUtility.UnloadPrefabContents(detached);}
        }
    }
}
