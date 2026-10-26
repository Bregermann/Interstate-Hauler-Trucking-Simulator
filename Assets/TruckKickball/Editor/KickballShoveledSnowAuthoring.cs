using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.InterstateHauler;
using LWS.TruckSports;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckKickball.Editor
{
    public static class KickballShoveledSnowAuthoring
    {
        const string Folder = "Assets/TruckKickball/SnowVisibility";
        const string BankMaterialPath=Folder+"/Shoveled packed snow.mat";
        const string OriginalBankMaterialHash="29D5A07D919F40DDFAEFAC7E8F85D4287EEA1A75F8ADA051369251F6F92EECC3";
        sealed class BankPlan { public Mesh saved, replacement; public string name, path, guid; }
        [MenuItem("Truck Sports/Carnival/Refine owned Kickball packed snow detail")]
        public static void RefineOwnedSnowBankDetailMaterial()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before explicit packed-snow authoring.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save open scenes first.");
            var scene=EditorSceneManager.OpenScene(KickballAuthoring.Scene);var owner=UnityEngine.Object.FindAnyObjectByType<KickballShoveledSnow>();var saved=AssetDatabase.LoadAssetAtPath<Material>(BankMaterialPath);
            if(owner==null||owner.gameObject.scene!=scene||owner.snowbanks.Length!=12||saved==null||owner.snowbanks.Any(renderer=>renderer==null||renderer.sharedMaterials.Length!=1||renderer.sharedMaterial!=saved))throw new InvalidOperationException("Preserve custom bank material bindings; exact twelve owned renderers required.");
            var expected=PackedBankMaterial();
            try
            {
                if(MatchesPackedBankMaterial(saved,expected)){Debug.Log("KICKBALL_PACKED_SNOW_DETAIL_PRESERVED");return;}
                string hash;using(var sha=System.Security.Cryptography.SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(BankMaterialPath))).Replace("-","");
                if(hash!=OriginalBankMaterialHash)throw new InvalidOperationException("Preserve custom packed snow material: exact original serialized signature differs.");
                string guid=AssetDatabase.AssetPathToGUID(BankMaterialPath);var clearPaths=owner.clearedBases.Concat(owner.clearedPaths).SelectMany(renderer=>renderer.sharedMaterials).Select(AssetDatabase.GetAssetPath).Distinct().ToArray();var clearBytes=clearPaths.Select(File.ReadAllBytes).ToArray();
                EditorUtility.CopySerialized(expected,saved);EditorUtility.SetDirty(saved);AssetDatabase.SaveAssetIfDirty(saved);
                if(AssetDatabase.AssetPathToGUID(BankMaterialPath)!=guid||!MatchesPackedBankMaterial(saved,expected))throw new InvalidOperationException("Owned packed-snow material identity/configuration changed.");
                for(int i=0;i<clearPaths.Length;i++)if(!clearBytes[i].SequenceEqual(File.ReadAllBytes(clearPaths[i])))throw new InvalidOperationException("Protected maintained material changed.");
                Debug.Log("KICKBALL_PACKED_SNOW_DETAIL_REFINED; existing material/GUID preserved; installed non-tessellated Weatherade packed snow/detail, fixed bank coverage; displacement/sparkle/SSS/traces disabled; no scene/global weather/clear material changes.");
            }
            finally{UnityEngine.Object.DestroyImmediate(expected);}
        }
        static Material PackedBankMaterial()
        {
            if(!LwsWeatheradeMaterialFactory.TryCreateWeatheradeRoadSurfaceMaterial("Shoveled packed snow",new Color(.83f,.90f,.95f),LwsWeatheradeSurfaceMaterialMode.Snow,out var material))throw new InvalidOperationException("Installed non-tessellated Weatherade snow material required.");
            try
            {
                var snow=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/NOT_Lonely/Weatherade SRS/Textures/Snow_01_n_h_sm.tif");var detail=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/NOT_Lonely/Weatherade SRS/Textures/SnowDetail.tif");if(snow==null||detail==null)throw new InvalidOperationException("Existing licensed Weatherade packed snow/detail textures required.");
                void Value(string property,float value){material.SetFloat(property,value);material.SetFloat(property+"Override",1);}
                Value("_Coverage",1);Value("_CoverageAmount",1);Value("_CoverageTiling",.3f);Value("_CoverageNormalScale0",.3f);Value("_UseCoverageDetail",1);Value("_DetailTiling",1.5f);Value("_DetailNormalScale",.2f);Value("_Displacement",0);Value("_Tessellation",0);Value("_Sparkle",0);Value("_Sss",0);Value("_Traces",0);Value("_ThreeTexMode",0);
                material.SetTexture("_CoverageTex0",snow);material.SetFloat("_CoverageTex0Override",1);material.SetTexture("_CoverageDetailTex",detail);material.SetFloat("_CoverageDetailTexOverride",1);material.SetColor("_CoverageColor",new Color(.88f,.92f,.95f));material.SetFloat("_CoverageColorOverride",1);material.SetFloat("_Smoothness",.12f);
                material.EnableKeyword("_COVERAGE_ON");material.EnableKeyword("_USE_COVERAGE_DETAIL");foreach(string keyword in new[]{"_DISPLACEMENT_ON","_SPARKLE_ON","_SSS_ON","_TRACES_ON","_THREE_TEX_MODE"})material.DisableKeyword(keyword);return material;
            }
            catch{UnityEngine.Object.DestroyImmediate(material);throw;}
        }
        public static bool MatchesPackedBankMaterial(Material actual,Material expected)=>actual!=null&&expected!=null&&EditorJsonUtility.ToJson(actual)==EditorJsonUtility.ToJson(expected);
        [MenuItem("Truck Sports/Carnival/Refine owned Kickball snow banks")]
        public static void RefineOwnedSnowBanks()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before explicit snow-bank refinement.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save open scenes before explicit snow-bank refinement.");
            var scene=EditorSceneManager.OpenScene(KickballAuthoring.Scene);var owner=UnityEngine.Object.FindAnyObjectByType<KickballShoveledSnow>();
            if(owner==null||owner.gameObject.scene!=scene||owner.name!="Maintained shoveled bases and running paths"||owner.snowbanks==null||owner.snowbanks.Length!=12)throw new InvalidOperationException("The original twelve owned Kickball snow banks are required.");
            if(owner.clearedBases==null||owner.clearedBases.Length!=4||owner.clearedPaths==null||owner.clearedPaths.Length!=8||owner.clearedBases.Any(item=>item==null)||owner.clearedPaths.Any(item=>item==null))throw new InvalidOperationException("Preserve the four physical bases and eight original maintained clear footprints.");
            if(owner.GetComponentsInChildren<Collider>(true).Length!=0)throw new InvalidOperationException("Owned snow presentation must remain collider-free.");
            var protectedRenderers=owner.clearedBases.Concat(owner.clearedPaths).ToArray();var originalComponents=protectedRenderers.Select(item=>EditorJsonUtility.ToJson(item)).ToArray();
            var originalColliders=owner.clearedBases.Select(item=>EditorJsonUtility.ToJson(item.GetComponent<Collider>())).ToArray();
            var protectedPaths=protectedRenderers.Select(item=>item.GetComponent<MeshFilter>()).Where(item=>item!=null).Select(item=>AssetDatabase.GetAssetPath(item.sharedMesh)).Concat(protectedRenderers.Concat(owner.snowbanks).SelectMany(item=>item.sharedMaterials).Select(AssetDatabase.GetAssetPath)).Where(path=>!string.IsNullOrEmpty(path)&&File.Exists(path)).Distinct().ToArray();
            var protectedBytes=protectedPaths.Select(File.ReadAllBytes).ToArray();var temporary=new List<Mesh>();var plans=new List<BankPlan>();int already=0;
            try
            {
                // Validate ALL targets before changing any persistent asset. An
                // exact new mesh is idempotent; any other edit is designer-owned.
                for(int i=0;i<4;i++)
                {
                    var center=KickballMatch.Bases[i];var to=KickballMatch.Bases[(i+1)%4];
                    CheckClearing("Base "+i+" exposed raked border",LegacyDisc(center,5.8f,false,i));
                    CheckClearing("Cleared running path "+i,LegacyRibbon(center,to,2.2f,false,i));
                    Preflight("Base "+i+" irregular shoveled rim",LegacyDisc(center,6.2f,true,i),MoundDiscV1(center,6.2f,i),MoundDisc(center,6.2f,i));
                    for(int side=-1;side<=1;side+=2)Preflight("Broken snow edge "+i+" "+side,LegacyRibbon(center,to,side*2.75f,true,i+side*11),MoundRibbonV1(center,to,side,i),MoundRibbon(center,to,side,i));
                }
                foreach(var plan in plans)
                {
                    string savedName=plan.saved.name;EditorUtility.CopySerialized(plan.replacement,plan.saved);plan.saved.name=savedName;EditorUtility.SetDirty(plan.saved);AssetDatabase.SaveAssetIfDirty(plan.saved);
                    if(AssetDatabase.AssetPathToGUID(plan.path)!=plan.guid)throw new InvalidOperationException("Snow-bank asset GUID changed: "+plan.path);
                }
                for(int i=0;i<protectedRenderers.Length;i++)if(EditorJsonUtility.ToJson(protectedRenderers[i])!=originalComponents[i])throw new InvalidOperationException("Protected cleared renderer changed.");
                for(int i=0;i<owner.clearedBases.Length;i++)if(EditorJsonUtility.ToJson(owner.clearedBases[i].GetComponent<Collider>())!=originalColliders[i])throw new InvalidOperationException("Original physical base changed.");
                for(int i=0;i<protectedPaths.Length;i++)if(!protectedBytes[i].SequenceEqual(File.ReadAllBytes(protectedPaths[i])))throw new InvalidOperationException("Protected cleared geometry/material changed: "+protectedPaths[i]);
                Debug.Log("KICKBALL_SNOW_BANKS_REFINED changed="+plans.Count+" preservedRefined="+already+"; exact legacy/V1 geometry guards; V2 rounded two-dimensional heaps; existing Mesh objects/GUIDs retained; broad low outward lobes, open paths, no collider/material/scene/runtime changes.");
            }
            finally{foreach(var mesh in temporary)if(mesh!=null)UnityEngine.Object.DestroyImmediate(mesh);}
            void CheckClearing(string name,Mesh expected)
            {
                temporary.Add(expected);var matches=owner.clearedPaths.Where(item=>item!=null&&item.name==name).ToArray();string path=Folder+"/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(saved==null||matches.Length!=1||matches[0].GetComponent<MeshFilter>()==null||matches[0].GetComponent<MeshFilter>().sharedMesh!=saved||!MatchesOwnedGeometry(saved,expected))throw new InvalidOperationException("Preserve custom cleared footprint; review its bank boundary explicitly: "+path);
            }
            void Preflight(string name,Mesh legacy,Mesh prior,Mesh refined)
            {
                temporary.Add(legacy);temporary.Add(prior);temporary.Add(refined);string path=Folder+"/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);var matches=owner.snowbanks.Where(item=>item!=null&&item.name==name).ToArray();
                if(saved==null||matches.Length!=1||matches[0].GetComponent<MeshFilter>()==null||matches[0].GetComponent<MeshFilter>().sharedMesh!=saved||saved.name!=name)throw new InvalidOperationException("Preserve custom/unassigned snow-bank ownership: "+name);
                var transform=matches[0].transform;if(!transform.localPosition.Equals(Vector3.zero)||!transform.localRotation.Equals(Quaternion.identity)||!transform.localScale.Equals(Vector3.one))throw new InvalidOperationException("Preserve custom snow-bank transform: "+name);
                if(MatchesOwnedGeometry(saved,refined)){already++;return;}
                if(!MatchesOwnedGeometry(saved,legacy)&&!MatchesOwnedGeometry(saved,prior))throw new InvalidOperationException("Preserve custom snow-bank mesh; exact original vertex/triangle/normal signature differs: "+path);
                plans.Add(new BankPlan{saved=saved,replacement=refined,name=name,path=path,guid=AssetDatabase.AssetPathToGUID(path)});
            }
        }
        public static bool MatchesOwnedGeometry(Mesh actual,Mesh expected)
        {
            if(actual==null||expected==null||!actual.isReadable||actual.vertexCount!=expected.vertexCount||actual.subMeshCount!=1||actual.GetTopology(0)!=MeshTopology.Triangles||actual.indexFormat!=expected.indexFormat||actual.hideFlags!=expected.hideFlags||actual.blendShapeCount!=0||actual.bindposes.Length!=0||actual.colors.Length!=0)return false;
            if(actual.vertexAttributeCount!=expected.vertexAttributeCount||actual.uv.Length!=0||actual.uv2.Length!=0||actual.uv3.Length!=0||actual.uv4.Length!=0)return false;
            return actual.GetVertexAttributes().SequenceEqual(expected.GetVertexAttributes())&&Exact(actual.vertices,expected.vertices)&&Exact(actual.normals,expected.normals)&&actual.triangles.SequenceEqual(expected.triangles)&&Exact(actual.bounds.center,expected.bounds.center)&&Exact(actual.bounds.extents,expected.bounds.extents);
        }
        static bool Exact(Vector3[] a,Vector3[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(!Exact(a[i],b[i]))return false;return true;}
        static bool Exact(Vector3 a,Vector3 b)=>BitConverter.SingleToInt32Bits(a.x)==BitConverter.SingleToInt32Bits(b.x)&&BitConverter.SingleToInt32Bits(a.y)==BitConverter.SingleToInt32Bits(b.y)&&BitConverter.SingleToInt32Bits(a.z)==BitConverter.SingleToInt32Bits(b.z);
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            var scene = EditorSceneManager.OpenScene(KickballAuthoring.Scene);
            if (UnityEngine.Object.FindAnyObjectByType<KickballShoveledSnow>() != null)
            { Debug.Log("KICKBALL_SHOVELED_SNOW_PRESERVED existing authored clear paths retained."); return; }
            var match = UnityEngine.Object.FindAnyObjectByType<KickballMatch>();
            var snow = match.GetComponent<SportsSnowSurface>();
            if (snow == null) throw new InvalidOperationException("Preserve the existing Weatherade snow bridge.");
            var bases = Enumerable.Range(0, 4).Select(i => GameObject.Find(i == 0 ? "HOME PLATE" : "BASE " + i)?.GetComponent<MeshRenderer>()).ToArray();
            if (bases.Any(item => item == null)) throw new InvalidOperationException("All four original physical bases are required.");
            var original = bases.Select(item => EditorJsonUtility.ToJson(item.GetComponent<Collider>())).ToArray();
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var dirtSource = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckKickball/Materials/Raked kickball infield clay.mat");
            if (dirtSource == null) throw new InvalidOperationException("Existing authored clay material required.");
            var dirt = ClearMaterial(dirtSource, "Maintained exposed clay");
            var ivory = ClearMaterial(bases[0].sharedMaterial, "Maintained ivory bases and chalk");
            foreach (var item in bases) { item.sharedMaterial = ivory; EditorUtility.SetDirty(item); }
            foreach (var item in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include).Where(item => item.name == "Inlaid chalk baseline"))
            { item.sharedMaterial = ivory; EditorUtility.SetDirty(item); }
            var bankMaterial = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Shoveled packed snow.mat");
            if (bankMaterial == null)
            {
                bankMaterial = PackedBankMaterial();AssetDatabase.CreateAsset(bankMaterial,Folder+"/Shoveled packed snow.mat");
            }
            var owner = new GameObject("Maintained shoveled bases and running paths").AddComponent<KickballShoveledSnow>(); owner.snow = snow;
            owner.clearedPresentation = new GameObject("Snow-only exposed clay and natural shoveled banks"); owner.clearedPresentation.transform.SetParent(owner.transform, false);
            var paths = new List<Renderer>(); var banks = new List<Renderer>();
            for (int i = 0; i < 4; i++)
            {
                var center = KickballMatch.Bases[i];
                paths.Add(Part(owner.transform, "Base " + i + " exposed raked border", Disc(center, 5.8f, false, i), dirt));
                banks.Add(Part(owner.transform, "Base " + i + " irregular shoveled rim", Disc(center, 6.2f, true, i), bankMaterial));
                var to = KickballMatch.Bases[(i + 1) % 4];
                paths.Add(Part(owner.transform, "Cleared running path " + i, Ribbon(center, to, 2.2f, false, i), dirt));
                for (int side = -1; side <= 1; side += 2)
                    banks.Add(Part(owner.transform, "Broken snow edge " + i + " " + side, Ribbon(center, to, side * 2.75f, true, i + side * 11), bankMaterial));
            }
            owner.clearedBases = bases; owner.clearedPaths = paths.ToArray(); owner.snowbanks = banks.ToArray();
            foreach (Transform child in owner.transform.Cast<Transform>().ToArray()) if (child != owner.clearedPresentation.transform) child.SetParent(owner.clearedPresentation.transform, true);
            owner.clearedPresentation.SetActive(false);
            for (int i = 0; i < 4; i++) if (original[i] != EditorJsonUtility.ToJson(bases[i].GetComponent<Collider>())) throw new InvalidOperationException("Original base collision must remain byte-equivalent.");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("KICKBALL_SHOVELED_SNOW_SAVED four original bases, four clear clay paths and natural collider-free banks; Weatherade coverage overrides prevent reburial. No gameplay changes.");
        }
        static Material ClearMaterial(Material source, string name)
        {
            string path = Folder + "/" + name + ".mat"; var existing = AssetDatabase.LoadAssetAtPath<Material>(path); if (existing != null) return existing;
            if (!LwsWeatheradeMaterialFactory.TryCreateWeatheradeRoadSurfaceMaterial(name, source, source.color, LwsWeatheradeSurfaceMaterialMode.Snow, out var material))
                throw new InvalidOperationException("Installed Weatherade snow shader is required.");
            material.SetFloat("_Coverage", 0); material.SetFloat("_CoverageOverride", 1); material.SetFloat("_CoverageAmount", 0); material.SetFloat("_CoverageAmountOverride", 1); material.DisableKeyword("_COVERAGE_ON");
            AssetDatabase.CreateAsset(material, path); return material;
        }
        static Renderer Part(Transform parent, string name, Mesh mesh, Material material)
        {
            string path = Folder + "/" + name + ".asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (saved == null) { AssetDatabase.CreateAsset(mesh, path); saved = mesh; } else UnityEngine.Object.DestroyImmediate(mesh);
            var root = new GameObject(name); root.transform.SetParent(parent, false); root.AddComponent<MeshFilter>().sharedMesh = saved; return root.AddComponent<MeshRenderer>().WithMaterial(material);
        }
        static Mesh Disc(Vector3 center,float radius,bool bank,int seed)=>bank?MoundDisc(center,radius,seed):LegacyDisc(center,radius,false,seed);
        static Mesh Ribbon(Vector3 from,Vector3 to,float width,bool bank,int seed)=>bank?MoundRibbon(from,to,width<0?-1:1,seed-(width<0?-1:1)*11):LegacyRibbon(from,to,width,false,seed);
        // Retain the exact old generators solely as a strict migration signature
        // and for unchanged clear clay. Normal install never regenerates assets.
        static Mesh LegacyDisc(Vector3 center, float radius, bool bank, int seed)
        {
            const int count = 72; var vertices = new List<Vector3>(); var triangles = new List<int>();
            if (!bank)
            {
                vertices.Add(center + Vector3.up * .024f);
                for (int i = 0; i <= count; i++) { float a = i * Mathf.PI * 2 / count; float r = radius + .18f * Mathf.Sin(a * 7 + seed); vertices.Add(center + new Vector3(Mathf.Cos(a) * r, .024f, Mathf.Sin(a) * r)); }
                for (int i = 1; i <= count; i++) triangles.AddRange(new[] { 0, i + 1, i });
            }
            else
            {
                for (int i = 0; i <= count; i++)
                {
                    float a = i * Mathf.PI * 2 / count, r = radius + .2f * Mathf.Sin(a * 7 + seed), h = .18f + .15f * (1 + Mathf.Sin(a * 9 + seed));
                    for (int row = 0; row < 3; row++) { float d = r + (row - 1) * .45f; vertices.Add(center + new Vector3(Mathf.Cos(a) * d, row == 1 ? h : .025f, Mathf.Sin(a) * d)); }
                    if (i < count) for (int row = 0; row < 2; row++) { int k = i * 3 + row; triangles.AddRange(new[] { k, k + 3, k + 1, k + 1, k + 3, k + 4 }); }
                }
            }
            return Mesh(vertices, triangles);
        }
        static Mesh LegacyRibbon(Vector3 from, Vector3 to, float width, bool bank, int seed)
        {
            var forward = (to - from).normalized; var side = Vector3.Cross(Vector3.up, forward); var vertices = new List<Vector3>(); var triangles = new List<int>(); const int steps = 48;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps; var center = Vector3.Lerp(from, to, t); float irregular = .13f * Mathf.Sin(t * 37 + seed) + .06f * Mathf.Sin(t * 83);
                for (int row = 0; row < 3; row++)
                {
                    float lateral = bank ? width + (row - 1) * .4f + irregular : (row - 1) * (width + irregular);
                    float height = bank && row == 1 ? .15f + .2f * Mathf.Pow(Mathf.Sin(t * 31 + seed), 2) : .024f;
                    // Leave the base clearing itself open; the connecting bank
                    // tapers before the maintained circular base border.
                    if (bank) height *= Mathf.Clamp01(Mathf.Min(t, 1 - t) * (Vector3.Distance(from, to) / 7));
                    vertices.Add(center + side * lateral + Vector3.up * height);
                }
                if (i < steps) for (int row = 0; row < 2; row++) { int k = i * 3 + row; triangles.AddRange(new[] { k, k + 3, k + 1, k + 1, k + 3, k + 4 }); }
            }
            return Mesh(vertices, triangles);
        }
        const int MoundRows=9;
        static float Hash(int index,int seed){unchecked{uint value=(uint)(index*374761393+seed*668265263);value=(value^(value>>13))*1274126177u;return ((value^(value>>16))&65535)/65535f;}}
        static float Lobe(float coordinate,int seed)
        {
            int cell=Mathf.FloorToInt(coordinate);float phase=coordinate-cell,start=.04f+Hash(cell,seed)*.08f,end=.76f+Hash(cell,seed+11)*.12f;
            return Mathf.SmoothStep(0,1,Mathf.InverseLerp(start,start+.16f,phase))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(end-.16f,end,phase)));
        }
        static float ClearWidth(float t,int seed)=>2.2f+.13f*Mathf.Sin(t*37+seed)+.06f*Mathf.Sin(t*83);
        public static float ClearedFootprintDistance(Vector3 point)
        {
            float distance=float.PositiveInfinity;
            for(int i=0;i<4;i++)
            {
                var from=KickballMatch.Bases[i];var delta=point-from;float angle=Mathf.Atan2(delta.z,delta.x);distance=Mathf.Min(distance,new Vector2(delta.x,delta.z).magnitude-(5.8f+.18f*Mathf.Sin(angle*7+i)));
                var to=KickballMatch.Bases[(i+1)%4];var direction=to-from;direction.y=0;float length=direction.magnitude;direction/=length;float along=Vector3.Dot(delta,direction)/length;
                if(along>=0&&along<=1){var side=Vector3.Cross(Vector3.up,direction);distance=Mathf.Min(distance,Mathf.Abs(Vector3.Dot(delta,side))-ClearWidth(along,i));}
            }
            return distance;
        }
        static float Profile(float across,float shift)
        {
            float position=across<shift?across/shift:(1-across)/(1-shift);return Mathf.Pow(Mathf.Sin(Mathf.Clamp01(position)*Mathf.PI*.5f),1.6f);
        }
        // V2 uses separate unequal heaps. Both the footprint and the crest taper
        // to one rounded tip; there is no full-width white quad at a lobe end.
        static Mesh MoundDisc(Vector3 center,float radius,int seed)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();float length=2*Mathf.PI*radius;var spans=HeapSpans(length,6,seed+131);
            for(int heap=0;heap<spans.Length;heap++)
            {
                int index=heap;var span=spans[heap];AddHeap(vertices,triangles,span.x,span.y,seed+heap*31+7,(along,across,width)=>
                {
                    float a=along/radius;float inner=5.8f+.18f*Mathf.Sin(a*7+seed)+.18f;float wander=.055f*Mathf.Sin(a*4+seed+index*.71f);float r=inner+width*.5f+wander+across;
                    return center+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);
                });
            }
            return OrganicMesh(vertices,triangles);
        }
        static Mesh MoundRibbon(Vector3 from,Vector3 to,int sign,int seed)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();var forward=(to-from).normalized;var lateral=Vector3.Cross(Vector3.up,forward);float length=Vector3.Distance(from,to),start=6.3f,end=length-6.3f;var spans=HeapSpans(end-start,Mathf.Max(3,Mathf.RoundToInt((end-start)/6.7f)),seed+sign*197);
            for(int heap=0;heap<spans.Length;heap++)
            {
                int index=heap;var span=spans[heap];AddHeap(vertices,triangles,start+span.x,start+span.y,seed+sign*47+heap*31,(along,across,width)=>
                {
                    float t=along/length;float inner=ClearWidth(t,seed)+.18f,wander=.055f*Mathf.Sin(along*.71f+seed+index*.83f+sign);return from+forward*along+lateral*(sign*(inner+width*.5f+wander+across));
                });
            }
            return OrganicMesh(vertices,triangles);
        }
        static Vector2[] HeapSpans(float length,int count,int seed)
        {
            var result=new Vector2[count];var weights=new float[count];var gaps=new float[count+1];float total=0,gapTotal=0;
            for(int i=0;i<count;i++){weights[i]=.7f+Hash(i,seed)*1.1f;total+=weights[i];}
            for(int i=0;i<=count;i++){gaps[i]=.25f+.65f*Hash(i,seed+19);gapTotal+=gaps[i];}
            float cursor=gaps[0],usable=Mathf.Max(.1f,length-gapTotal);for(int i=0;i<count;i++){float next=cursor+usable*weights[i]/total;result[i]=new Vector2(cursor,next);cursor=next+gaps[i+1];}return result;
        }
        static Mesh OrganicMesh(List<Vector3> vertices,List<int> indices)
        {
            // Footprint clipping can leave a tiny sliver beside an open route.
            // Remove only isolated sub-.12m² fragments, never reconnect a gap.
            var parent=Enumerable.Range(0,vertices.Count).ToArray();int Root(int at){while(parent[at]!=at){parent[at]=parent[parent[at]];at=parent[at];}return at;}
            for(int i=0;i<indices.Count;i+=3){parent[Root(indices[i+1])]=Root(indices[i]);parent[Root(indices[i+2])]=Root(indices[i]);}
            var area=new Dictionary<int,float>();for(int i=0;i<indices.Count;i+=3){int root=Root(indices[i]);float value=Mathf.Abs(Vector3.Cross(vertices[indices[i+1]]-vertices[indices[i]],vertices[indices[i+2]]-vertices[indices[i]]).y)*.5f;area[root]=area.TryGetValue(root,out float total)?total+value:value;}
            var retained=new List<int>();for(int i=0;i<indices.Count;i+=3)if(area[Root(indices[i])]>=.12f)retained.AddRange(new[]{indices[i],indices[i+1],indices[i+2]});return CompactMesh(vertices,retained);
        }
        static void AddHeap(List<Vector3> vertices,List<int> triangles,float start,float end,int seed,Func<float,float,float,Vector3> location)
        {
            int steps=Mathf.Max(16,Mathf.CeilToInt((end-start)/.24f));int[] previous=null;
            for(int step=0;step<=steps;step++)
            {
                float u=step/(float)steps,along=Mathf.Lerp(start,end,u),tip=Mathf.Clamp01(Mathf.Min(u,1-u)/.23f);float taper=Mathf.Sqrt(Mathf.Max(0,1-(1-tip)*(1-tip)));float bodyWidth=Mathf.Clamp(1.52f+.46f*Hash(0,seed)+.02f*Mathf.Sin(along*.8f+seed),1.5f,2f);float crest=.11f+.12f*Hash(1,seed);var ring=new int[step==0||step==steps?1:MoundRows];
                for(int row=0;row<ring.Length;row++)
                {
                    float across=ring.Length==1?.5f:row/(float)(MoundRows-1);var point=location(along,(across-.5f)*bodyWidth*taper,bodyWidth);float clearance=ClearedFootprintDistance(point);point.y+=.024f+Profile(across,.46f+.06f*Hash(2,seed))*(crest-.024f)*taper*(.93f+.07f*Mathf.Sin(u*Mathf.PI))*Mathf.SmoothStep(0,1,Mathf.Clamp01((clearance-.08f)/.65f));ring[row]=vertices.Count;vertices.Add(point);
                }
                if(previous!=null)
                {
                    if(previous.Length==1)for(int row=0;row<MoundRows-1;row++)Face(previous[0],ring[row],ring[row+1]);
                    else if(ring.Length==1)for(int row=0;row<MoundRows-1;row++)Face(previous[row],ring[0],previous[row+1]);
                    else for(int row=0;row<MoundRows-1;row++){Face(previous[row],ring[row],previous[row+1]);Face(previous[row+1],ring[row],ring[row+1]);}
                }
                previous=ring;
            }
            void Face(int a,int b,int c)
            {
                var first=vertices[a];var second=vertices[b];var third=vertices[c];var normal=Vector3.Cross(second-first,third-first);if(Mathf.Abs(normal.y)<.0000001f)return;
                foreach(var point in new[]{first,second,third,(first+second)*.5f,(second+third)*.5f,(third+first)*.5f,(first+second+third)/3})if(ClearedFootprintDistance(point)<.08f)return;
                if(normal.y>0)triangles.AddRange(new[]{a,b,c});else triangles.AddRange(new[]{a,c,b});
            }
        }
        // The V1 generators and stitcher remain byte-identical solely to
        // recognize our previously authored geometry; never migrate custom art.
        static Mesh MoundDiscV1(Vector3 center,float radius,int seed)
        {
            const int steps=144;var vertices=new List<Vector3>();var triangles=new List<int>();var envelope=new float[steps+1];
            for(int i=0;i<=steps;i++)
            {
                float a=i*Mathf.PI*2/steps,noise=.5f+.5f*Mathf.Sin(a*3+seed)*Mathf.Cos(a*5+seed*.37f);float inner=radius-.4f+.18f*Mathf.Sin(a*7+seed)+.12f;float width=1.5f+.5f*noise;float crest=.10f+.13f*(.5f+.5f*Mathf.Sin(a*5+seed*.83f));envelope[i]=Lobe(i/(float)steps*(12+seed%3),seed+37);
                for(int row=0;row<MoundRows;row++){float across=row/(float)(MoundRows-1),r=inner+width*across;var point=center+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);float clearance=ClearedFootprintDistance(point);point.y+=.024f+Profile(across,.45f+.08f*noise)*(crest-.024f)*envelope[i]*Mathf.SmoothStep(0,1,Mathf.Clamp01((clearance-.08f)/.65f));vertices.Add(point);}
            }
            StitchMounds(vertices,triangles,envelope,steps);return CompactMesh(vertices,triangles);
        }
        static Mesh MoundRibbonV1(Vector3 from,Vector3 to,int sign,int seed)
        {
            const int steps=96;var forward=(to-from).normalized;var side=Vector3.Cross(Vector3.up,forward);float cells=Mathf.Max(5,Mathf.CeilToInt(Vector3.Distance(from,to)/3.4f));var vertices=new List<Vector3>();var triangles=new List<int>();var envelope=new float[steps+1];
            for(int i=0;i<=steps;i++)
            {
                float t=i/(float)steps,noise=.5f+.5f*Mathf.Sin(t*23+seed+sign*3)*Mathf.Cos(t*41+seed*.37f);var center=Vector3.Lerp(from,to,t);float inner=ClearWidth(t,seed)+.12f,width=1.5f+.5f*noise,crest=.10f+.13f*(.5f+.5f*Mathf.Sin(t*31+seed+sign));envelope[i]=Lobe(t*cells,seed+sign*47);
                for(int row=0;row<MoundRows;row++){float across=row/(float)(MoundRows-1);var point=center+side*(sign*(inner+width*across));float clearance=ClearedFootprintDistance(point);point.y+=.024f+Profile(across,.45f+.08f*noise)*(crest-.024f)*envelope[i]*Mathf.SmoothStep(0,1,Mathf.Clamp01((clearance-.08f)/.65f));vertices.Add(point);}
            }
            StitchMounds(vertices,triangles,envelope,steps);return CompactMesh(vertices,triangles);
        }
        static void StitchMounds(List<Vector3> vertices,List<int> triangles,float[] envelope,int steps)
        {
            for(int i=0;i<steps;i++)
            {
                if(envelope[i]<.015f||envelope[i+1]<.015f)continue;
                for(int row=0;row<MoundRows-1;row++)
                {
                    int a=i*MoundRows+row,b=a+MoundRows,c=a+1,d=b+1;
                    if(ClearedFootprintDistance(vertices[a])<.08f||ClearedFootprintDistance(vertices[b])<.08f||ClearedFootprintDistance(vertices[c])<.08f||ClearedFootprintDistance(vertices[d])<.08f)continue;
                    // Upward-facing winding for either side and circular banks.
                    if(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).y>0)triangles.AddRange(new[]{a,b,c,c,b,d});else triangles.AddRange(new[]{a,c,b,c,d,b});
                }
            }
        }
        static Mesh CompactMesh(List<Vector3> original,List<int> indices)
        {
            var used=new Dictionary<int,int>();var vertices=new List<Vector3>();var triangles=new List<int>();foreach(int old in indices){if(!used.TryGetValue(old,out int next)){next=vertices.Count;used.Add(old,next);vertices.Add(original[old]);}triangles.Add(next);}return Mesh(vertices,triangles);
        }
        static Mesh Mesh(List<Vector3> vertices, List<int> triangles)
        { var mesh = new Mesh { vertices = vertices.ToArray(), triangles = triangles.ToArray() }; mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh; }
        static MeshRenderer WithMaterial(this MeshRenderer renderer, Material material) { renderer.sharedMaterial = material; return renderer; }
    }
}
