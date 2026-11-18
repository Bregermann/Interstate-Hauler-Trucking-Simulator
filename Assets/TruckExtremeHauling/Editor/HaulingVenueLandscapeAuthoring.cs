#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckExtremeHauling.Editor
{
    /// <summary>
    /// Explicit saved-scene scenery intake, using installed authored terrain and production models.
    /// No runtime authority, weather replacement, course rebuild, or changes to starts/objectives.
    /// Vista's whole-tile regeneration was not used: the saved sporting geometry must remain fixed.
    /// Skyhook alone replaces its broad collision floor with visible equal-height receiving pads.
    /// </summary>
    public static class HaulingVenueLandscapeAuthoring
    {
        const string RootName = "Owned hauling venue landscape";
        const string Folder = "Assets/TruckExtremeHauling/Landscapes";
        const string TerrainSource = "Assets/RiverModeler/Demo/River Modeler Demo Terrain.asset";
        const string LayerRoot = "Assets/3D Haven/Path Painter II/Editor/Demo/Terrains/DocsTerrain/TerrainLayers/";
        const string City = "Assets/JC_LP_MegaCity/Prefabs/Buildings/SM_Buildings_";
        const string Garage = "Assets/LeartesStudios/Garage Warehouse Environment/Art/Prefabs/";
        const string Container = "Assets/ShipContainerPack/URP/Prefabs/ShipContainer";
        const string Rock = "Assets/TruckFishing/Prefabs/River boulder ";
        const string Tree = "Assets/JC_LP_MegaCity/Prefabs/Nature/SM_Nature_Tree_07.prefab";
        const string StandLibrary = "Assets/TruckBeerPong/Carnival/Tailgate stadium and render library.prefab";
        const int Resolution = 257;
        static readonly string[] Venues = { "CargoCrisis", "DominoDerby", "TrailerPlinko", "JackknifeOnIce", "RoadTrainWreck", "HitchRingToss", "ZeroGHitchUp", "SkyhookSprint", "IceRoadReckoning" };

        [MenuItem("Truck Sports/Alpha 0.10/Upgrade active advanced hauling surroundings")]
        public static void UpgradeActiveVenue() { InstallActiveVenue(); }

        public static void RepairSavedCargoCrisisApronGap()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before repairing the saved apron.");
            var scene = EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/CargoCrisis.unity", OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var floor = roots.Single(g => g.name == "Continuous venue support").GetComponent<BoxCollider>();
            const string path = Folder + "/CargoCrisis/Surrounding authored terrain.asset";
            var asset = Need<TerrainData>(path);
            var terrain = roots.SelectMany(g => g.GetComponentsInChildren<Terrain>(true)).Single(t => t.terrainData == asset);
            if (roots.SelectMany(g => g.GetComponentsInChildren<TerrainCollider>(true)).Any(c => c.terrainData == asset))
                throw new InvalidOperationException("The Cargo Crisis scenery asset must remain independent of all physical surfaces.");
            if (floor == null || floor.isTrigger || !floor.enabled)
                throw new InvalidOperationException("The existing physical freight apron is required.");

            Bounds apron = floor.bounds;
            int resolution = asset.holesResolution;
            bool[,] visible = asset.GetHoles(0, 0, resolution, resolution);
            Vector3 origin = terrain.transform.position;
            int repaired = 0;
            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                if (visible[z, x]) continue;
                float worldX = origin.x + (x + .5f) * asset.size.x / resolution;
                float worldZ = origin.z + (z + .5f) * asset.size.z / resolution;
                // Keep the inner terrain cutout for the alternate flooded
                // loading bays. Bridge only the gap around the original apron.
                float insideX = Mathf.Min(worldX - apron.min.x, apron.max.x - worldX);
                float insideZ = Mathf.Min(worldZ - apron.min.z, apron.max.z - worldZ);
                if (Mathf.Min(insideX, insideZ) > 12) continue;
                visible[z, x] = true;
                repaired++;
            }
            if (repaired > 0)
            {
                asset.SetHoles(0, 0, visible);
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
            }
            Debug.Log("HH10_CARGO_VISIBLE_APRON_GAP_REPAIRED cells=" + repaired + " saved visual terrain perimeter only.");
        }

        // Saving is deliberately left to the integrator, after inspection of the active scene.
        public static GameObject InstallActiveVenue()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before adding hauling surroundings.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!Venues.Contains(scene.name) || scene.path != "Assets/TruckExtremeHauling/Scenes/" + scene.name + ".unity")
                throw new InvalidOperationException("Open one of the nine saved advanced hauling venues. Other scenes are not supported.");
            ExtremeHaulingMatch match = null;
            foreach (var existing in scene.GetRootGameObjects())
            {
                if (existing.name == RootName) { Debug.Log("HH10_LANDSCAPE_PRESERVED " + scene.name); return existing; }
                var candidate = existing.GetComponentInChildren<ExtremeHaulingMatch>(true);
                if (candidate != null) match = candidate;
            }
            if (match == null || match.trucks == null || match.objectives == null)
                throw new InvalidOperationException("The saved scene must own its original hauling match, vehicles and objectives.");
            bool orbit = scene.name == "ZeroGHitchUp", winter = scene.name == "JackknifeOnIce" || scene.name == "IceRoadReckoning";
            bool cable = scene.name == "SkyhookSprint";
            var source = orbit ? null : Need<TerrainData>(TerrainSource);
            var grass = orbit ? null : Need<TerrainLayer>(winter ? "Assets/TruckSkyTow/Art/Arctic terrain layer.terrainlayer" : LayerRoot + "stdGrassHill.terrainlayer");
            var cliff = orbit ? null : Need<TerrainLayer>(winter ? "Assets/TruckSkyTow/Art/Arctic ice crust.terrainlayer" : LayerRoot + "stdCliff.terrainlayer");
            foreach (string path in new[] { City + "Warehouse_01.prefab", City + "Factory_01.prefab", City + "House_04_V1.prefab", City + "Commercial_08.prefab", Garage + "SM_Floor_4x6m_01.prefab", Garage + "SM_Wall_4x3m_01.prefab", Garage + "SM_PK04_WoodenCrate_01.prefab", Container + "Blue_URP.prefab", Container + "Red_URP.prefab", Container + "Gray_URP.prefab", Tree, Rock + "0.prefab", Rock + "1.prefab", Rock + "2.prefab" }) Need<GameObject>(path);
            if (!orbit && !winter && !cable && scene.name != "RoadTrainWreck" && scene.name != "CargoCrisis")
                Need<GameObject>(StandLibrary);
            var originalTruckPositions = match.trucks.Select(t => t.transform.position).ToArray();
            var originalObjectives = match.objectives.Select(t => t.position).ToArray();
            string output = Folder + "/" + scene.name;
            EnsureFolder(output + "/Materials");
            var root = new GameObject(RootName); root.SetActive(false);
            if (orbit) SpaceStation(root.transform, output);
            else
            {
                var floor = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Continuous venue support");
                Bounds safe = floor != null && floor.GetComponent<Collider>() != null ? floor.GetComponent<Collider>().bounds : new Bounds(new Vector3(120,0,100), new Vector3(550,2,600));
                if (scene.name == "IceRoadReckoning") safe = new Bounds(new Vector3(140,0,87.5f), new Vector3(420,1,455));
                Terrain terrain = Surroundings(root.transform, source, grass, cliff, output, safe, winter, cable);
                if (cable) CableRavine(match, root.transform, output, floor);
                else if (winter) NorthernShore(root.transform, terrain, safe, output, scene.name == "IceRoadReckoning");
                else if (scene.name == "RoadTrainWreck") RoadTrainYard(root.transform, terrain, safe, output);
                else if (scene.name == "CargoCrisis") RescueDepot(root.transform, terrain, safe, output);
                else ArcadeVenue(root.transform, terrain, safe, output, scene.name);
                // Texture-only replacement: retain the exact original floor mesh, dimensions and contact.
                if (floor != null && !winter && !cable)
                {
                    var renderer = floor.GetComponent<Renderer>();
                    if (renderer != null && renderer.sharedMaterial != null)
                    {
                        var material = new Material(renderer.sharedMaterial) { name = scene.name + " worn freight yard surface" };
                        var dirt = Need<TerrainLayer>(LayerRoot + "stdMudRocky.terrainlayer");
                        material.mainTexture = dirt.diffuseTexture; material.mainTextureScale = new Vector2(safe.size.x / 16, safe.size.z / 16);
                        material.color = scene.name == "CargoCrisis" ? new Color(.55f,.53f,.48f) : new Color(.68f,.66f,.59f);
                        AssetDatabase.CreateAsset(material, output + "/Materials/Freight yard surface.mat"); renderer.sharedMaterial = material;
                    }
                }
            }
            for (int i = 0; i < match.trucks.Length; i++) if (match.trucks[i].transform.position != originalTruckPositions[i]) throw new InvalidOperationException("A truck start changed during additive scenery authoring.");
            for (int i = 0; i < match.objectives.Length; i++) if (match.objectives[i].position != originalObjectives[i]) throw new InvalidOperationException("An objective moved during additive scenery authoring.");
            root.SetActive(true); Undo.RegisterCreatedObjectUndo(root,"Add owned hauling surroundings"); EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("HH10_LANDSCAPE_ADDED " + scene.name + (cable ? "; PHYSICS CHANGE: broad floor replaced by visible top=0 launch/receiving pads; focused cable retest required." : "; existing gameplay collision, starts, hitches and objectives preserved.") + " Caller must save scene/assets.");
            return root;
        }

        // Shared explicit authoring helpers; caller owns a new saved venue and
        // its bounds. These create no runtime manager or gameplay collision.
        public static Terrain CreatePeripheralTerrain(Transform parent,string output,Bounds safe,bool ravine=false)
        {
            EnsureFolder(output+"/Materials");
            return Surroundings(parent,Need<TerrainData>(TerrainSource),Need<TerrainLayer>(LayerRoot+"stdGrassHill.terrainlayer"),Need<TerrainLayer>(LayerRoot+"stdCliff.terrainlayer"),output,safe,false,ravine);
        }
        public static void PlacePeripheralModel(Transform parent,string path,Vector3 position,Vector3 size,float yaw,string output)
            => Model(parent,path,position,size,yaw,output);
        public static void PlacePeripheralModel(Transform parent,GameObject source,Vector3 position,Vector3 size,float yaw,string output)
            => Model(parent,source,position,size,yaw,output);

        static Terrain Surroundings(Transform parent, TerrainData source, TerrainLayer surface, TerrainLayer cliff, string output, Bounds safe, bool winter, bool ravine)
        {
            float width = Mathf.Max(1500, safe.size.x + 1000), depth = Mathf.Max(1450, safe.size.z + 1000);
            Vector3 origin = new Vector3(safe.center.x-width/2,-120,safe.center.z-depth/2);
            var data = UnityEngine.Object.Instantiate(source); data.name="Owned production relief around preserved sport";
            data.treeInstances=Array.Empty<TreeInstance>(); data.treePrototypes=Array.Empty<TreePrototype>(); data.detailPrototypes=Array.Empty<DetailPrototype>();
            data.heightmapResolution=Resolution; data.size=new Vector3(width,400,depth); data.alphamapResolution=128;
            data.terrainLayers=new[]{surface,cliff};
            var heights=new float[Resolution,Resolution]; var solid=new bool[Resolution-1,Resolution-1];
            for(int z=0;z<Resolution;z++) for(int x=0;x<Resolution;x++)
            {
                float u=x/(float)(Resolution-1),v=z/(float)(Resolution-1);
                float wx=origin.x+u*width,wz=origin.z+v*depth;
                float dx=Mathf.Max(safe.min.x-wx,0,wx-safe.max.x),dz=Mathf.Max(safe.min.z-wz,0,wz-safe.max.z),distance=Mathf.Sqrt(dx*dx+dz*dz);
                float relief=source.GetInterpolatedHeight(u,v)/Mathf.Max(1,source.size.y);
                float rise=Mathf.SmoothStep(0,1,Mathf.Clamp01(distance/210));
                float y=-.3f+rise*(25+relief*160);
                if(ravine)
                {
                    float canyon = Mathf.SmoothStep(0,1,Mathf.Clamp01((wz+35)/30))*Mathf.SmoothStep(0,1,Mathf.Clamp01((185-wz)/30));
                    y-=canyon*(85+relief*24)*Mathf.Clamp01((distance+120)/120);
                }
                heights[z,x]=Mathf.Clamp01((y-origin.y)/data.size.y);
                if(z<Resolution-1&&x<Resolution-1) solid[z,x]=ravine||distance>20;
            }
            data.SetHeights(0,0,heights);data.SetHoles(0,0,solid);
            var paint=new float[128,128,2];
            for(int z=0;z<128;z++)for(int x=0;x<128;x++) {float slope=data.GetSteepness(x/127f,z/127f);float rock=Mathf.Clamp01((slope-18)/30);paint[z,x,0]=1-rock;paint[z,x,1]=rock;}
            data.SetAlphamaps(0,0,paint);AssetDatabase.CreateAsset(data,output+"/Surrounding authored terrain.asset");
            var go=Terrain.CreateTerrainGameObject(data);go.name=winter?"Continuous northern mountain shore":"Continuous surrounding owned valley relief";go.transform.SetParent(parent,false);go.transform.position=origin;
            // Peripheral visual relief cannot create new invisible barriers or change the measured sport.
            UnityEngine.Object.DestroyImmediate(go.GetComponent<TerrainCollider>());
            var terrain=go.GetComponent<Terrain>();terrain.drawInstanced=true;terrain.heightmapPixelError=8;terrain.basemapDistance=900;terrain.shadowCastingMode=ShadowCastingMode.On;
            // Keep the established clear material and its published terrain properties. The scene's
            // existing TruckTaxiSnowSurface owns the isolated Weatherade terrain swap at runtime;
            // assigning a Weatherade shader here would skip its URP-baseline discovery and restore.
            var clearSource=Need<Material>(winter?"Assets/TruckSkyTow/Materials/Arctic Vista terrain.mat":"Assets/TruckFishing/Materials/Continuous grassy bank terrain.mat");
            if(clearSource.shader==null||clearSource.shader.name!="Universal Render Pipeline/Terrain/Lit")throw new InvalidOperationException("Inspect the existing clear terrain material before landscape intake.");
            var material=new Material(clearSource){name="Owned hauling landscape terrain",enableInstancing=true};material.EnableKeyword("_ALPHATEST_ON");
            AssetDatabase.CreateAsset(material,output+"/Materials/Surrounding terrain.mat");terrain.materialTemplate=material;
            var random=new System.Random(winter?22122:22118);
            for(int i=0;i<150;i++)
            {
                float wx=safe.min.x-400+(float)random.NextDouble()*(safe.size.x+800),wz=safe.min.z-400+(float)random.NextDouble()*(safe.size.z+800);
                if(wx>safe.min.x-35&&wx<safe.max.x+35&&wz>safe.min.z-35&&wz<safe.max.z+35)continue;
                float y=terrain.SampleHeight(new Vector3(wx,0,wz))+origin.y;
                if(ravine&&wz>-45&&wz<195)continue;
                bool tree=i%3!=0;
                Model(parent,tree?Tree:Rock+(i%3)+".prefab",new Vector3(wx,y,wz),tree?new Vector3(18,28,18):new Vector3(23,16,24),i*137.5f,output);
            }
            return terrain;
        }
        static void RescueDepot(Transform root,Terrain t,Bounds safe,string output)
        {
            foreach(int side in new[]{-1,1})for(int i=0;i<4;i++)
            {
                float x=side<0?safe.min.x-42:safe.max.x+42,z=-110+i*125;
                Model(root,City+(i%2==0?"Factory_01.prefab":"Warehouse_01.prefab"),new Vector3(x,Ground(t,x,z),z),new Vector3(75,38,85),side*90,output);
                Model(root,Container+(i%2==0?"Red_URP.prefab":"Blue_URP.prefab"),new Vector3(side<0?safe.min.x-15:safe.max.x+15,0,z),new Vector3(10,8,24),i*90,output);
            }
            DepotDetails(root,output,-105,-140);DepotDetails(root,output,345,245);
        }
        static void RoadTrainYard(Transform root,Terrain t,Bounds safe,string output)
        {
            for(int i=0;i<8;i++)
            {
                float x=safe.min.x-30+i*(safe.size.x+60)/7,z=safe.max.z+45;
                Model(root,City+"Warehouse_01.prefab",new Vector3(x,Ground(t,x,z),z),new Vector3(65,32,70),180,output);
                Model(root,Container+(i%2==0?"Blue_URP.prefab":"Red_URP.prefab"),new Vector3(x,0,safe.min.z-32),new Vector3(12,9,30),90,output);
            }
            DepotDetails(root,output,safe.min.x-18,120);DepotDetails(root,output,safe.max.x+18,120);
        }
        static void NorthernShore(Transform root,Terrain t,Bounds safe,string output,bool thin)
        {
            for(int i=0;i<7;i++)
            {
                float x=safe.min.x-40+i*(safe.size.x+80)/6,z=safe.max.z+55+(i%2)*25;
                Model(root,City+"House_04_V1.prefab",new Vector3(x,Ground(t,x,z),z),new Vector3(32,24,38),180,output);
            }
            foreach(int side in new[]{-1,1})
            {
                float x=side<0?safe.min.x-48:safe.max.x+48,z=120;
                Model(root,City+"Warehouse_01.prefab",new Vector3(x,Ground(t,x,z),z),new Vector3(58,28,65),side*90,output);
                Model(root,Container+"Red_URP.prefab",new Vector3(x,Ground(t,x,-95),-95),new Vector3(12,8,28),side*90,output);
            }
            // Shore structures and woodland remain outside all fragile ice sheet bounds.
        }
        static void ArcadeVenue(Transform root,Terrain t,Bounds safe,string output,string venue)
        {
            var library=Need<GameObject>(StandLibrary);
            var tier=library.GetComponentsInChildren<Transform>(true).FirstOrDefault(c=>c.name=="Union Field Grandstand tier");
            if(tier==null)throw new InvalidOperationException("The existing Tailgate grandstand render template is required.");
            foreach(int side in new[]{-1,1})for(int i=0;i<5;i++)
            {
                float x=side<0?safe.min.x-30:safe.max.x+30,z=safe.min.z+25+i*95;
                Model(root,tier.gameObject,new Vector3(x,Ground(t,x,z),z),new Vector3(28,15,80),side<0?90:-90,output);
                Model(root,Container+(i%2==0?"Red_URP.prefab":"Blue_URP.prefab"),new Vector3(x,Ground(t,x,z)+16,z),new Vector3(8,6,18),90,output);
            }
            // Full-size Plinko boards retain their 160m drop and all physical pegs/bins.
            float backdrop=venue=="TrailerPlinko"?safe.max.z+70:safe.max.z+40;
            for(int i=0;i<5;i++) {float x=safe.min.x+55+i*(safe.size.x-110)/4;Model(root,City+"Commercial_08.prefab",new Vector3(x,Ground(t,x,backdrop),backdrop),new Vector3(65,45,55),180,output);}
        }
        static void CableRavine(ExtremeHaulingMatch match,Transform root,string output,GameObject oldFloor)
        {
            var contact=Need<PhysicsMaterial>("Assets/TruckExtremeHauling/Materials/Freight concrete.asset");
            for(int slot=0;slot<match.trucks.Length;slot++)
            {
                float x=match.trucks[slot].transform.position.x;
                Pad(root,new Vector3(x,-1,-77),new Vector3(54,2,83),output,contact,"Visible cable launch station");
                Pad(root,new Vector3(match.objectives[slot].position.x,-1,match.objectives[slot].position.z+5),new Vector3(54,2,78),output,contact,"Visible cable receiving station");
                foreach(float z in new[]{-118f,260f}) Model(root,City+"Warehouse_01.prefab",new Vector3(x,0,z),new Vector3(40,23,30),z<0?0:180,output);
            }
            // Replace only the named authored broad support, never any cable/gantry/slalom geometry.
            if(oldFloor!=null)Undo.DestroyObjectImmediate(oldFloor);
        }
        static void Pad(Transform root,Vector3 center,Vector3 size,string output,PhysicsMaterial contact,string name)
        {
            var pad=new GameObject(name);pad.transform.SetParent(root,false);pad.transform.position=center;
            var support=pad.AddComponent<BoxCollider>();support.size=size;support.sharedMaterial=contact;
            // A visible production steel/concrete floor occupies the exact collider footprint.
            Model(pad.transform,Garage+"SM_Floor_4x6m_01.prefab",center-Vector3.up, size,0,output,true);
        }
        static void SpaceStation(Transform root,string output)
        {
            // All structures are render-only and lie beyond the original 6-DoF docking corridors.
            foreach(int side in new[]{-1,1})for(int i=0;i<7;i++)
            {
                float x=side<0?-75:325,z=-100+i*55;
                Model(root,Container+"Gray_URP.prefab",new Vector3(x,15,z),new Vector3(25,15,38),i*90,output);
                Model(root,Garage+"SM_Wall_4x3m_01.prefab",new Vector3(x,35,z),new Vector3(8,35,45),side*90,output,true);
                Model(root,Container+(i%2==0?"Blue_URP.prefab":"Red_URP.prefab"),new Vector3(x,78,z),new Vector3(16,10,29),90,output);
            }
            for(int i=0;i<5;i++)
            {
                float x=-30+i*75;
                Model(root,Garage+"SM_Floor_4x6m_01.prefab",new Vector3(x,82,185),new Vector3(72,3,55),0,output,true);
                Model(root,Container+"Gray_URP.prefab",new Vector3(x,110,195),new Vector3(22,18,35),90,output);
            }
            // Existing Earth/star material and sky remain the sole orbital sky authority.
        }
        static void DepotDetails(Transform root,string output,float x,float z)
        {
            for(int i=0;i<8;i++)Model(root,Garage+"SM_PK04_WoodenCrate_01.prefab",new Vector3(x+(i%2)*5,0,z+(i/2)*6),new Vector3(4,4+(i%3),4),i*23,output);
        }
        static float Ground(Terrain terrain,float x,float z)=>terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;
        static GameObject Model(Transform root,string path,Vector3 position,Vector3 size,float yaw,string output,bool stretch=false)=>Model(root,Need<GameObject>(path),position,size,yaw,output,stretch);
        static GameObject Model(Transform root,GameObject source,Vector3 position,Vector3 size,float yaw,string output,bool stretch=false)
        {
            var holder=new GameObject("Owned "+source.name).transform;holder.SetParent(root,false);holder.position=position;holder.rotation=Quaternion.Euler(0,yaw,0);
            var model=UnityEngine.Object.Instantiate(source);model.SetActive(true);model.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            foreach(var joint in model.GetComponentsInChildren<Joint>(true))UnityEngine.Object.DestroyImmediate(joint);
            foreach(var behaviour in model.GetComponentsInChildren<MonoBehaviour>(true).Reverse())UnityEngine.Object.DestroyImmediate(behaviour);
            foreach(var shape in model.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(shape);
            foreach(var body in model.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(body);
            var renderers=model.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)throw new InvalidOperationException("Production model has no render geometry: "+source.name);
            Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            Vector3 ratios=new Vector3(size.x/Mathf.Max(.001f,bounds.size.x),size.y/Mathf.Max(.001f,bounds.size.y),size.z/Mathf.Max(.001f,bounds.size.z));
            Vector3 scale=stretch?ratios:Vector3.one*Mathf.Min(ratios.x,ratios.y,ratios.z);
            model.transform.localScale=Vector3.Scale(model.transform.localScale,scale);model.transform.position=-Vector3.Scale(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z),scale);
            model.transform.SetParent(holder,false);
            foreach(var renderer in renderers)
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var original=materials[i];if(original==null||original.shader.name.Contains("Universal")||original.shader.name.Contains("URP"))continue;
                    string path=output+"/Materials/Imported "+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original))+".mat";
                    var owned=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(owned==null){owned=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=original.name+" owned URP",mainTexture=original.mainTexture};if(original.HasProperty("_Color"))owned.color=original.color;AssetDatabase.CreateAsset(owned,path);}materials[i]=owned;
                }
                renderer.sharedMaterials=materials;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
            }
            var groups=model.GetComponentsInChildren<LODGroup>(true);if(groups.Length==0){var group=holder.gameObject.AddComponent<LODGroup>();group.SetLODs(new[]{new LOD(.018f,renderers)});group.RecalculateBounds();}
            foreach(var child in holder.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(child.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic);
            return holder.gameObject;
        }
        static T Need<T>(string path)where T:UnityEngine.Object {var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset==null)throw new InvalidOperationException("Installed hauling scenery asset is missing: "+path);return asset;}
        static void EnsureFolder(string path){string parent="Assets";foreach(var part in path.Split('/').Skip(1)){string next=parent+"/"+part;if(!AssetDatabase.IsValidFolder(next))AssetDatabase.CreateFolder(parent,part);parent=next;}}
    }
}
#endif
