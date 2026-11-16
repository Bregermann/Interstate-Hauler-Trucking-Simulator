#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using sc.modeling.river.runtime;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class CoastalBand
        {
            public string status="IN_PROGRESS",tag,sourcePrefab,sourceReceipt,sourceReceiptSha256,seaMesh,seaMaterial;
            public int outerTerrains,totalTerrains,seaVertices,seaRows,seaColumns,invertedSeaTriangles,invalidSeaVertices;public float seaLevel=-2,maximumSeaRowDrift,minimumSeaTriangleArea;public bool exactStraightSeaGeometry;public Bounds actualSeaBounds;
            public bool coreHeightmapsExact,coreColliderGeometryExact,sourceBytesPreserved,duplicateLakeRenderingReplaced;
            public DragonKingdomOuterVistaAuthoring.SeamReceipt seams;
            public DragonKingdomOuterVistaAuthoring.CurrentCoreCoordination currentCoreCoordination;
        }
        static string OuterFolder()
        {
            string tag=Argument("-tsDragonOuterBand","");if(string.IsNullOrEmpty(tag))return null;
            if(tag.Length>40||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new ArgumentException("Exact bounded passed outer Vista tag required.");
            return Root+"ReviewCandidates/"+tag;
        }
        static IEnumerable<string> OuterSourceFiles()
        {
            string folder=OuterFolder();if(folder==null)yield break;
            if(!Directory.Exists(Io(folder)))throw new InvalidOperationException("Retain the actual passed outer Vista candidate first: "+folder);
            foreach(string path in Directory.GetFiles(folder,"*",SearchOption.AllDirectories))yield return path;
            if(File.Exists(Io(folder+".meta")))yield return folder+".meta";
            foreach(string path in UnityEditor.AssetDatabase.GetDependencies("Assets/RiverModeler/ShaderGraph/Default River.mat",true).SelectMany(p=>new[]{p,p+".meta"}))if(File.Exists(Io(path)))yield return path;
        }
        static CoastalBand AttachMeasuredCoast(DragonKingdomWorld world,string folder,bool prepareCurrentCoreCopies=false)
        {
            string outerFolder=OuterFolder();if(outerFolder==null)return null;
            string receiptPath=outerFolder+"/Outer Vista Receipt.json";var proof=JsonUtility.FromJson<DragonKingdomOuterVistaAuthoring.Receipt>(File.ReadAllText(Io(receiptPath)));
            if(proof.status!="MEASURED_SCENERY_CANDIDATE_REQUIRES_RENDER_AND_WORLD_ATTACHMENT_REVIEW"||!proof.sourceBytesPreserved||!proof.temporaryObjectsRemoved||!proof.originalInnerNotGenerated||proof.heightCallbacks!=12||proof.uniqueHeightTiles!=12||proof.validationIssues==null||proof.validationIssues.Length!=0)throw new InvalidOperationException("Actual twelve-tile vendor generation/seam/source proof required before attaching coast scenery.");
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(outerFolder+"/Outer Vista Band.prefab");if(source==null)throw new InvalidOperationException("Actual saved outer Vista prefab missing.");
            var evidence=new CoastalBand{tag=Path.GetFileName(outerFolder),sourcePrefab=AssetDatabase.GetAssetPath(source),sourceReceipt=receiptPath,sourceReceiptSha256=Hash(File.ReadAllBytes(Io(receiptPath))).Replace("-","").ToLowerInvariant()};
            evidence.seams=DragonKingdomOuterVistaAuthoring.ValidateAgainstInner(world.gameObject,source);
            if(evidence.seams.issues.Length!=0&&!prepareCurrentCoreCopies)throw new InvalidOperationException("Actual fresh inner terrain differs from measured outer seams: "+string.Join(" | ",evidence.seams.issues));
            var core=world.GetComponentsInChildren<Terrain>(true);if(core.Length!=4)throw new InvalidOperationException("Attach only once to the original four actual inner tiles.");
            var heights=core.ToDictionary(t=>t,t=>HashHeights(t.terrainData));var oldCollision=world.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>ColliderPose(c));var sourceHashes=Directory.GetFiles(outerFolder,"*",SearchOption.AllDirectories).ToDictionary(p=>p,p=>Hash(File.ReadAllBytes(Io(p))));
            var root=(GameObject)PrefabUtility.InstantiatePrefab(source,world.transform);PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);root.name="Owned Vista coastal outskirts";
            var added=root.GetComponentsInChildren<Terrain>(true);if(added.Length!=12||root.GetComponentsInChildren<MonoBehaviour>(true).Length!=0)throw new InvalidOperationException("Only twelve genuine static Vista scenery terrains may be attached; no generation authority.");
            Directory.CreateDirectory(Io(folder+"/Materials/Coast"));AssetDatabase.Refresh();var materials=new Dictionary<string,Material>();
            foreach(var terrain in added)
            {
                string dimensions=terrain.terrainData.size.x+"x"+terrain.terrainData.size.z;
                if(!materials.TryGetValue(dimensions,out var material))
                {material=new Material(core[0].materialTemplate){name="Actual Digger coastal terrain "+dimensions};material.SetFloat("_TerrainWidthInv",1/terrain.terrainData.size.x);material.SetFloat("_TerrainHeightInv",1/terrain.terrainData.size.z);AssetDatabase.CreateAsset(material,folder+"/Materials/Coast/Terrain "+dimensions+".mat");materials.Add(dimensions,material);}
                terrain.materialTemplate=material;terrain.allowAutoConnect=false;EditorUtility.SetDirty(terrain);
            }
            var all=world.GetComponentsInChildren<Terrain>(true);var neighborhood=root.AddComponent<DragonTerrainNeighborhood>();neighborhood.terrains=all;neighborhood.Connect();LinkActualTerrainNeighbors(all);evidence.outerTerrains=added.Length;evidence.totalTerrains=all.Length;
            var sea=new GameObject("Actual RiverModeler coastal sea");sea.transform.SetParent(root.transform,false);var spline=sea.AddComponent<SplineContainer>();spline.Spline=new Spline();
            // AddComponent invokes the vendor Reset(), which immediately
            // rebuilds using its default2m density. Keep a valid tiny spline
            // during that lifecycle; configure the real coarse sea density
            // BEFORE expanding the actual12.4km span. No logged-error waiver
            // or vendor/index-format change is required.
            foreach(float x in new[]{0f,2f})spline.Spline.Add(new BezierKnot(new float3(x,evidence.seaLevel,800)),TangentMode.Linear);
            var filter=sea.AddComponent<MeshFilter>();var renderer=sea.AddComponent<MeshRenderer>();var sourceMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/RiverModeler/ShaderGraph/Default River.mat");if(sourceMaterial==null)throw new InvalidOperationException("Actual installed RiverModeler water material required.");
            var seaMaterial=new Material(sourceMaterial){name="Actual vendor coastal water"};evidence.seaMaterial=folder+"/Materials/Coast/Actual vendor coastal water.mat";AssetDatabase.CreateAsset(seaMaterial,evidence.seaMaterial);renderer.sharedMaterial=seaMaterial;
            var river=sea.AddComponent<RiverModeler>();river.rebuildTriggers=RiverModeler.RebuildTriggers.None;river.meshFilter=filter;river.splineContainer=spline;river.settings.shape.width=12400;river.settings.triangulation.vertexDistance=160;river.settings.triangulation.vertexDistanceWidth=160;
            var seaSpline=UniformStraightSeaSpline(evidence.seaLevel);spline.Spline=seaSpline;river.Rebuild();
            if(filter.sharedMesh==null||filter.sharedMesh.vertexCount==0)throw new InvalidOperationException("Vendor RiverModeler produced no real sea mesh.");Directory.CreateDirectory(Io(folder+"/Models"));AssetDatabase.Refresh();evidence.seaMesh=folder+"/Models/Actual RiverModeler coastal sea.asset";AssetDatabase.CreateAsset(filter.sharedMesh,evidence.seaMesh);evidence.seaVertices=filter.sharedMesh.vertexCount;ValidateStraightSeaGeometry(sea.transform,filter.sharedMesh,spline.Spline.Count,evidence);
            // One coherent water surface replaces the old coplanar lake strip.
            // Keep its original object/component/mesh identities and semantics;
            // avoid drawing two transparent water layers over the same bay.
            var lake=world.transform.Find("Lanternwater Lake")?.GetComponent<MeshRenderer>();if(lake==null)throw new InvalidOperationException("Original lake renderer identity required.");lake.enabled=false;EditorUtility.SetDirty(lake);evidence.duplicateLakeRenderingReplaced=true;
            evidence.coreHeightmapsExact=heights.All(p=>HashHeights(p.Key.terrainData)==p.Value);evidence.coreColliderGeometryExact=oldCollision.All(p=>p.Key!=null&&ColliderPose(p.Key)==p.Value);evidence.sourceBytesPreserved=sourceHashes.All(p=>File.Exists(Io(p.Key))&&Hash(File.ReadAllBytes(Io(p.Key)))==p.Value);
            if(!evidence.coreHeightmapsExact||!evidence.coreColliderGeometryExact||!evidence.sourceBytesPreserved||all.Length!=16)throw new InvalidOperationException("Outer attachment changed accepted core height/collider/source geometry or omitted real tiles.");
            evidence.status=prepareCurrentCoreCopies?"ATTACHED_OUTER_SOURCE_REQUIRES_OWNED_CURRENT_CORE_COLLAR":"ATTACHED_REAL_VISTA_COAST_REQUIRES_FULL_WORLD_RENDER_AND_FLIGHT_REVIEW";return evidence;
        }
        static void CompleteCurrentCoreCoastCopies(DragonKingdomWorld world,string folder,CoastalBand evidence)
        {
            if(evidence==null||evidence.status!="ATTACHED_OUTER_SOURCE_REQUIRES_OWNED_CURRENT_CORE_COLLAR")throw new InvalidOperationException("Explicit current-core copied coast stage required.");
            var original=JsonUtility.FromJson<DragonKingdomOuterVistaAuthoring.Receipt>(File.ReadAllText(Io(evidence.sourceReceipt)));var outer=world.transform.Find("Owned Vista coastal outskirts");if(outer==null)throw new InvalidOperationException("Actual attached outer band missing.");
            try{evidence.currentCoreCoordination=DragonKingdomOuterVistaAuthoring.CoordinateOwnedOuterCopiesToCurrentCore(world.gameObject,outer.gameObject,original.source,evidence.sourcePrefab,folder);}finally{evidence.currentCoreCoordination=DragonKingdomOuterVistaAuthoring.LastCurrentCoreCoordination;}evidence.seams=evidence.currentCoreCoordination.after;
            if(!evidence.currentCoreCoordination.passed||evidence.seams.issues.Length!=0)throw new InvalidOperationException("Owned current-core Vista collar did not satisfy original seam/source contract.");evidence.status="ATTACHED_REAL_VISTA_COAST_REQUIRES_FULL_WORLD_RENDER_AND_FLIGHT_REVIEW";
        }
        [Serializable]public sealed class CurrentCoastCopyProbe
        {public DragonKingdomOuterVistaAuthoring.CurrentCoreCoordination coordination;public int nativeHeightContacts;public float maximumNativeHeightError;public bool nativeHeightContactsPassed;}
        static CurrentCoastCopyProbe CaptureActualOwnedCurrentCoast(DragonKingdomWorld world)
        {
            string tag=Argument("-tsDragonCoastCoordCandidate","");if(string.IsNullOrEmpty(tag)||tag.Length>40||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new InvalidOperationException("Fresh bounded current-coast diagnostic tag required.");string folder=Root+"VisualCandidates/"+tag;if(Directory.Exists(Io(folder))||File.Exists(Io(folder+".meta")))throw new InvalidOperationException("Preserve existing coast diagnostics; select a fresh candidate.");
            string originalFolder=OuterFolder();if(originalFolder==null)throw new InvalidOperationException("Actual measured Vista outer source required.");var sourceProof=JsonUtility.FromJson<DragonKingdomOuterVistaAuthoring.Receipt>(File.ReadAllText(Io(originalFolder+"/Outer Vista Receipt.json")));var source=AssetDatabase.LoadAssetAtPath<GameObject>(originalFolder+"/Outer Vista Band.prefab");if(source==null||sourceProof.validationIssues?.Length!=0||!sourceProof.sourceBytesPreserved)throw new InvalidOperationException("Passed original vendor outer source required.");
            Directory.CreateDirectory(Io(folder+"/Terrain/Palette"));AssetDatabase.Refresh();var clone=(GameObject)PrefabUtility.InstantiatePrefab(source,world.transform);clone.name="Temporary owned current-core coast measurement";var terrains=clone.GetComponentsInChildren<Terrain>(true);if(terrains.Length!=12)throw new InvalidOperationException("Actual twelve-source outer tiles required.");
            for(int i=0;i<terrains.Length;i++){var terrain=terrains[i];var data=UnityEngine.Object.Instantiate(terrain.terrainData);AssetDatabase.CreateAsset(data,folder+"/Terrain/Palette/Outer "+i+".asset");terrain.terrainData=data;terrain.GetComponent<TerrainCollider>().terrainData=data;}
            var proof=new CurrentCoastCopyProbe();try{proof.coordination=DragonKingdomOuterVistaAuthoring.CoordinateOwnedOuterCopiesToCurrentCore(world.gameObject,clone,sourceProof.source,AssetDatabase.GetAssetPath(source),folder);}finally{proof.coordination=DragonKingdomOuterVistaAuthoring.LastCurrentCoreCoordination;}Physics.SyncTransforms();bool contacts=true;
            foreach(var terrain in terrains)foreach(int x in new[]{128,256,384})
            {
                var data=terrain.terrainData;int z=256;var position=terrain.transform.position;var point=new Vector3(position.x+data.size.x*x/(data.heightmapResolution-1),position.y+data.GetHeight(x,z),position.z+data.size.z*z/(data.heightmapResolution-1));var collider=terrain.GetComponent<TerrainCollider>();bool hit=collider.Raycast(new Ray(point+Vector3.up*50,Vector3.down),out var actual,100);proof.nativeHeightContacts++;if(!hit){contacts=false;continue;}float error=Mathf.Abs(actual.point.y-point.y);proof.maximumNativeHeightError=Mathf.Max(proof.maximumNativeHeightError,error);contacts&=error<=.03f;
            }
            proof.nativeHeightContactsPassed=contacts&&proof.nativeHeightContacts==36;if(!proof.nativeHeightContactsPassed)throw new InvalidOperationException("Actual owned coast collider does not match its coordinated saved heightfield: "+JsonUtility.ToJson(proof));return proof;
        }
        public static void ValidateStraightSeaGeometry(Transform sea,Mesh mesh,int splineKnots,CoastalBand evidence)
        {
            // A redundant middle LINEAR knot has zero evaluated tangent at
            // t=.5. RiverModeler correctly falls back to a safe rotation, but
            // that rotates the entire79-vertex middle row90degrees and creates
            // huge overlapping triangles. Two endpoints describe the same
            // straight12.4km water surface without that singular interior knot.
            var vertices=mesh.vertices.Select(sea.TransformPoint).ToArray();var topology=mesh.triangles;
            if(topology.Length<6||topology.Length%3!=0||topology.Any(i=>i<0||i>=vertices.Length))throw new InvalidOperationException("Actual native triangle indices required.");
            // RiverModeler uses CEIL(width/density) across and ROUND(actual
            // spline length/density) along. A valid straight surface therefore
            // need not be square; derive its real row stride from adjacent
            // triangle topology rather than assuming sqrt(vertexCount).
            int columns=0;for(int i=0;i<topology.Length;i+=3)for(int edge=0;edge<3;edge++)columns=Math.Max(columns,Math.Abs(topology[i+edge]-topology[i+(edge+1)%3]));
            if(columns<3||vertices.Length%columns!=0||vertices.Length/columns<3||splineKnots!=2||topology.Length!=(vertices.Length/columns-1)*(columns-1)*6)throw new InvalidOperationException("Complete native rectangular RiverModeler grid and two straight endpoints required.");
            var cells=new Dictionary<long,int>();
            for(int i=0;i<topology.Length;i+=3)
            {
                int r0=Math.Min(topology[i]/columns,Math.Min(topology[i+1]/columns,topology[i+2]/columns)),r1=Math.Max(topology[i]/columns,Math.Max(topology[i+1]/columns,topology[i+2]/columns));int c0=Math.Min(topology[i]%columns,Math.Min(topology[i+1]%columns,topology[i+2]%columns)),c1=Math.Max(topology[i]%columns,Math.Max(topology[i+1]%columns,topology[i+2]%columns));
                if(r1-r0!=1||c1-c0!=1||topology[i]==topology[i+1]||topology[i]==topology[i+2]||topology[i+1]==topology[i+2])throw new InvalidOperationException("Sea triangle does not cover three distinct adjacent row/column corners.");
                int triangle=0;for(int j=0;j<3;j++)triangle|=1<<((topology[i+j]/columns-r0)*2+topology[i+j]%columns-c0);int code=triangle==7?1:triangle==14?2:triangle==11?4:triangle==13?8:0;long key=Key(r0,c0);cells.TryGetValue(key,out int present);if(code==0||(present&code)!=0)throw new InvalidOperationException("Duplicate or malformed native sea cell triangle.");cells[key]=present|code;
            }
            if(cells.Count!=(vertices.Length/columns-1)*(columns-1)||cells.Values.Any(mask=>mask!=3&&mask!=12))throw new InvalidOperationException("Native sea grid has missing/overlapping cell triangles.");
            evidence.seaRows=vertices.Length/columns;evidence.seaColumns=columns;evidence.minimumSeaTriangleArea=float.PositiveInfinity;var bounds=new Bounds(vertices[0],Vector3.zero);
            for(int row=0;row<evidence.seaRows;row++)
            {
                var first=vertices[row*columns];var last=vertices[row*columns+columns-1];float previousZ=float.PositiveInfinity;
                for(int column=0;column<columns;column++)
                {
                    var p=vertices[row*columns+column];bounds.Encapsulate(p);bool finite=!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.y)&&!float.IsInfinity(p.z);if(!finite||Mathf.Abs(p.y-evidence.seaLevel)>.001f||p.z>=previousZ)evidence.invalidSeaVertices++;previousZ=p.z;evidence.maximumSeaRowDrift=Mathf.Max(evidence.maximumSeaRowDrift,Mathf.Abs(p.x-first.x));
                }
                if(Mathf.Abs(first.z-7000)>.02f||Mathf.Abs(last.z+5400)>.02f)evidence.invalidSeaVertices++;
                if(row>0&&first.x<=vertices[(row-1)*columns].x)evidence.invalidSeaVertices++;
            }
            var triangles=mesh.triangles;for(int i=0;i<triangles.Length;i+=3){var cross=Vector3.Cross(vertices[triangles[i+1]]-vertices[triangles[i]],vertices[triangles[i+2]]-vertices[triangles[i]]);if(cross.y<=0||float.IsNaN(cross.sqrMagnitude)||float.IsInfinity(cross.sqrMagnitude))evidence.invertedSeaTriangles++;evidence.minimumSeaTriangleArea=Mathf.Min(evidence.minimumSeaTriangleArea,cross.magnitude*.5f);}
            evidence.actualSeaBounds=bounds;evidence.exactStraightSeaGeometry=evidence.invalidSeaVertices==0&&evidence.invertedSeaTriangles==0&&evidence.maximumSeaRowDrift<.15f&&evidence.minimumSeaTriangleArea>.01f&&Mathf.Abs(bounds.min.x+6200)<1&&Mathf.Abs(bounds.max.x-6200)<1&&Mathf.Abs(bounds.min.z+5400)<.02f&&Mathf.Abs(bounds.max.z-7000)<.02f;
            if(!evidence.exactStraightSeaGeometry)throw new InvalidOperationException("Native sea row/triangle orientation, finite water level or original fullspan failed: "+JsonUtility.ToJson(evidence));
        }
        public static void LinkActualTerrainNeighbors(Terrain[] all)
        {
            DragonTerrainNeighborhood.ConnectOwnedTerrains(all,all.Length==16);foreach(var terrain in all)EditorUtility.SetDirty(terrain);
        }
        static string ColliderPose(Collider collider)=>EditorJsonUtility.ToJson(collider)+collider.transform.localToWorldMatrix.ToString("R");
    }
}
#endif
