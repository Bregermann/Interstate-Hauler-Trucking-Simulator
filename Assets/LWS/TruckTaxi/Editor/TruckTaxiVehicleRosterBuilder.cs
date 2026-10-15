using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.InterstateHauler;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiVehicleRosterBuilder
    {
        public const string Root="Assets/LWS/TruckTaxi/Art/Trucks";
        private const string Starter="Assets/LWS/TruckTaxi/Prefabs/TruckTaxi_InterstateTractor.prefab";
        private const string BaseDefinition="Assets/LWS/TruckTaxi/Edenborough/CanonicalTaxiTruck.asset";
        [Serializable] private sealed class Geometry { public string model; public float cabRearZ,rearAxleOffset,fifthWheelOffset; public bool sleeper; }
        private sealed class Spec
        {
            public string name,id,title,engine,handling,description;
            public long price;
            public float mass,power,city,highway;
            public Spec(string n,string title,string desc,long price,float mass,float power,float city,float highway,string engine,string handling)
            { name=n; this.title=title; description=desc; this.price=price; this.mass=mass; this.power=power; this.city=city; this.highway=highway;
                this.engine=engine; this.handling=handling; id="taxi.truck."+n.ToLowerInvariant(); }
        }
        private static readonly Spec[] Specs={
            new Spec("HighwayKing","Highway King","Classic long-nose sleeper. Chrome, tall stacks and a real furnished berth.",60000,8400,3.5f,36,7.5f,"Heavy highway torque","Long-wheelbase comfort"),
            new Spec("Nightrunner","Nightrunner","Aerodynamic high-roof sleeper with a curved raised roof and integrated lighting.",90000,7600,3.6f,40,8,"Highway cruiser","Balanced high-speed touring"),
            new Spec("Outlaw","Outlaw","Custom daycab. Tall stacks, rally lamps, dark trim and a protective front bar.",125000,7400,4.1f,43,8.5f,"Custom response","Responsive street handling"),
            new Spec("Thunderhaul","Thunderhaul","Sport daycab. Lower roof, tapered hood, heat vents, aero lip and splitter.",170000,6900,4.5f,45,9,"Performance launch","Sport-oriented, speed-limited steering"),
            new Spec("RoadPalace","Road Palace","Premium high-roof sleeper with extra storage, refrigerator, berth and warm interior lighting.",240000,9300,3.4f,34,7,"Heavy luxury cruiser","Longest wheelbase; stable deliberate turns")
        };
        [MenuItem("Truck Taxi/Vehicle Factory/Author Six Truck Roster")]
        public static void Build()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring the roster.");
            foreach(string folder in new[]{"Materials","Prefabs","Definitions"}) Directory.CreateDirectory(Root+"/"+folder);
            AssetDatabase.Refresh();
            var baseDefinition=AssetDatabase.LoadAssetAtPath<LwsTruckDefinition>(BaseDefinition);
            var baseline=AssetDatabase.LoadAssetAtPath<GameObject>(Starter);
            if(baseDefinition==null || baseline==null) throw new InvalidOperationException("Canonical Taxi baseline is unavailable.");
            var sourceMeshes=baseline.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null &&
                AssetDatabase.GetAssetPath(f.sharedMesh).EndsWith("010416TRUK_HD.fbx",StringComparison.OrdinalIgnoreCase) && !f.name.StartsWith("Whl HD "))
                .Select(f=>f.sharedMesh).Distinct().ToArray();
            var config=AssetDatabase.LoadAssetAtPath<TruckTaxiConfiguration>("Assets/LWS/TruckTaxi/ScriptableObjects/TruckTaxi_DemoConfiguration.asset");
            var starterContents=PrefabUtility.LoadPrefabContents(Starter);
            try
            {
                var data=starterContents.GetComponent<TruckTaxiTruckModel>();
                if(data==null) data=starterContents.AddComponent<TruckTaxiTruckModel>();
                data.retainsOriginalBody=true; data.primaryPaintMaterials=config.truckBodyMaterials;
                data.engineDescription="Balanced NWH starter"; data.handlingDescription="Short-wheelbase daycab";
                PrefabUtility.SaveAsPrefabAsset(starterContents,Starter);
            }
            finally { PrefabUtility.UnloadPrefabContents(starterContents); }
            var entries=new List<TruckTaxiCareerTruckDefinition>{new TruckTaxiCareerTruckDefinition {
                stableId=baseDefinition.StableId,displayName="Interstate Starter",truck=baseDefinition,priceCents=0,
                description="Your original, already-owned NWH daycab. Original appearance and proven controls retained.",availableForPurchase=false,
                icon=Preview("Starter") }};
            foreach(var spec in Specs)
            {
                var geometry=JsonUtility.FromJson<Geometry>(File.ReadAllText("ArtSource/TruckTaxi/Models/"+spec.name+".json"));
                var body=ImportModel(spec.name,spec.name);
                var lod1=ImportModel(spec.name,spec.name+"_LOD1");
                var lod2=ImportModel(spec.name,spec.name+"_LOD2");
                string path=Root+"/Prefabs/TT_"+spec.name+"_NWH.prefab";
                var contents=PrefabUtility.LoadPrefabContents(Starter);
                try
                {
                    contents.name="TT_"+spec.name+"_NWH";
                    var model=contents.GetComponent<TruckTaxiTruckModel>();
                    model.retainsOriginalBody=false; model.bodyVisualPrefab=body;
                    model.distanceLodPrefabs=new[]{lod1,lod2}; model.replacedSourceMeshes=sourceMeshes;
                    model.primaryPaintMaterials=body.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials)
                        .Where(m=>m!=null && m.name=="TT_Paint_Primary").Distinct().ToArray();
                    model.hasSleeper=geometry.sleeper; model.rearAxleOffset=geometry.rearAxleOffset; model.fifthWheelOffset=geometry.fifthWheelOffset;
                    model.driverEye=new Vector3(-.56f,2.20f,.62f);model.gpsMount=new Vector3(-.08f,2.18f,1.35f);
                    model.sleeperDriver=new Vector3(-.65f,1.76f,geometry.cabRearZ+.37f);
                    model.companionSeat=new Vector3(.65f,1.76f,geometry.cabRearZ+.37f);
                    float roof=spec.name=="RoadPalace"?4.12f:spec.name=="Nightrunner"?3.96f:spec.name=="HighwayKing"?3.89f:spec.name=="Thunderhaul"?3.15f:3.37f;
                    model.sleeperLamp=new Vector3(0,roof-.27f,geometry.cabRearZ+.40f);
                    model.cabinCollision=new Bounds(new Vector3(0,(1.16f+roof)*.5f,(1.5f+geometry.cabRearZ)*.5f),new Vector3(2.30f,roof-1.16f,1.5f-geometry.cabRearZ));
                    model.hoodCollision=new Bounds(new Vector3(0,1.40f,2.28f),new Vector3(1.76f,1.20f,1.55f));
                    model.tractorMassKg=spec.mass; model.launchPowerAtRest=spec.power; model.launchPowerAt15Mph=Mathf.Min(2.5f,spec.power*.65f);
                    model.citySteeringLock=spec.city; model.highwaySteeringLock=spec.highway;
                    model.engineDescription=spec.engine; model.handlingDescription=spec.handling;
                    model.previewAngles=new[]{Preview(spec.name)};
                    model.headlampOffset=new Vector3(.99f,spec.name=="Nightrunner" || spec.name=="Thunderhaul" ? .94f : 1.05f,3.24f);
                    var dashboard=contents.GetComponent<LwsTruckDashboardController>();
                    if(dashboard==null) dashboard=contents.AddComponent<LwsTruckDashboardController>();
                    dashboard.Configure(baseDefinition.DashboardDefinition);
                    var mirrors=contents.GetComponent<LwsTruckMirrorController>();
                    if(mirrors==null) mirrors=contents.AddComponent<LwsTruckMirrorController>();
                    var mirrorSettings=new SerializedObject(mirrors);
                    mirrorSettings.FindProperty("leftMirrorCamera").objectReferenceValue=TruckTaxiTruckRig.Find(contents.transform,"RenderTextureMirrorCameraL").GetComponent<Camera>();
                    mirrorSettings.FindProperty("rightMirrorCamera").objectReferenceValue=TruckTaxiTruckRig.Find(contents.transform,"RenderTextureMirrorCameraR").GetComponent<Camera>();
                    mirrorSettings.ApplyModifiedPropertiesWithoutUndo();
                    var recovery=contents.GetComponent<LwsTruckCabInteriorRecovery>();
                    if(recovery==null) recovery=contents.AddComponent<LwsTruckCabInteriorRecovery>();
                    var recoverySettings=new SerializedObject(recovery); recoverySettings.FindProperty("createSleeperPlaceholders").boolValue=false;
                    recoverySettings.ApplyModifiedPropertiesWithoutUndo();
                    TruckTaxiTruckRig.Apply(contents,model);
                    PrefabUtility.SaveAsPrefabAsset(contents,path);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
                string definitionPath=Root+"/Definitions/TT_"+spec.name+".asset";
                var definition=AssetDatabase.LoadAssetAtPath<LwsTruckDefinition>(definitionPath);
                if(definition==null){definition=UnityEngine.Object.Instantiate(baseDefinition);AssetDatabase.CreateAsset(definition,definitionPath);}
                var so=new SerializedObject(definition);
                so.FindProperty("stableId").stringValue=spec.id; so.FindProperty("displayName").stringValue=spec.title;
                so.FindProperty("manufacturerFamily").stringValue="Truck Taxi Original";
                so.FindProperty("playerPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var dashPath=Root+"/Definitions/TT_"+spec.name+"_Dashboard.asset";
                var dash=AssetDatabase.LoadAssetAtPath<LwsTruckDashboardDefinition>(dashPath);
                if(dash==null){dash=UnityEngine.Object.Instantiate(baseDefinition.DashboardDefinition);AssetDatabase.CreateAsset(dash,dashPath);}
                var ds=new SerializedObject(dash); ds.FindProperty("truckDefinitionId").stringValue=spec.id;
                foreach(string gauge in new[]{"speedometer","tachometer"})
                {ds.FindProperty(gauge+".startAngle").floatValue=0;ds.FindProperty(gauge+".endAngle").floatValue=-220;}
                ds.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(dash);
                so.FindProperty("dashboardDefinition").objectReferenceValue=dash;
                so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(definition);
                var finished=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var player=finished.GetComponent<LwsPlayerTruck>();
                    if(player==null) player=finished.AddComponent<LwsPlayerTruck>();
                    player.Configure(definition,"player.truck.starter");
                    finished.GetComponent<LwsTruckDashboardController>().Configure(dash);
                    PrefabUtility.SaveAsPrefabAsset(finished,path);
                }
                finally { PrefabUtility.UnloadPrefabContents(finished); }
                if(!TruckTaxiTruckModel.ValidatePlayerPrefab(definition,out string error)) throw new InvalidOperationException(spec.name+": "+error);
                entries.Add(new TruckTaxiCareerTruckDefinition{stableId=spec.id,displayName=spec.title,truck=definition,
                    priceCents=spec.price,description=spec.description,availableForPurchase=true,icon=Preview(spec.name)});
            }
            foreach(string catalogPath in new[]{"Assets/LWS/TruckTaxi/Edenborough/CareerCatalog.asset","Assets/LWS/TruckTaxi/Resources/TruckTaxi/Career/Catalog.asset"})
            {
                var catalog=AssetDatabase.LoadAssetAtPath<TruckTaxiCareerCatalog>(catalogPath);
                if(catalog==null) throw new InvalidOperationException("Missing existing Career catalog "+catalogPath);
                catalog.trucks=entries.ToArray();
                var ids=entries.Select(e=>e.StableId).ToArray();
                foreach(var upgrade in catalog.upgrades) if(upgrade.adapterId==TruckTaxiCareerUpgradeTuning.AdapterId) upgrade.compatibleTruckIds=ids;
                foreach(var cosmetic in catalog.cosmetics) if(cosmetic.category==TruckTaxiCosmeticCategory.Paint) cosmetic.compatibleTruckIds=ids;
                if(!catalog.Validate(out string error)) throw new InvalidOperationException(error);
                EditorUtility.SetDirty(catalog);
            }
            BuildDealershipFacility();
            AssetDatabase.SaveAssets();
            Debug.Log("TRUCK_TAXI_SIX_NWH_PREFABS_AUTHORED: "+string.Join(", ",entries.Select(e=>e.StableId)));
        }
        private static GameObject ImportModel(string model,string file)
        {
            string path=Root+"/Models/"+file+".fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            if(importer.GetExternalObjectMap().Count>=10) return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            importer.globalScale=1; importer.importCameras=false; importer.importLights=false; importer.importAnimation=false;
            importer.isReadable=false; importer.SaveAndReimport();
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Directory.CreateDirectory(Root+"/Materials/"+model); AssetDatabase.Refresh();
            foreach(var material in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
            {
                string name=material.name.Split('.')[0]; string output=Root+"/Materials/"+model+"/"+name+".mat";
                var owned=AssetDatabase.LoadAssetAtPath<Material>(output);
                if(owned==null){owned=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(owned,output);}
                owned.name=name; owned.SetColor("_BaseColor",material.color);
                float metallic=name.Contains("Chrome") ? .94f : name.Contains("MetalTrim") ? .65f : name.Contains("Paint_Primary") ? .55f : 0;
                owned.SetFloat("_Metallic",metallic); owned.SetFloat("_Smoothness",name.Contains("Chrome") ? .8f : name.Contains("Paint") ? .70f : .35f);
                if(name=="TT_Glass")
                {
                    owned.SetFloat("_Surface",1);owned.SetFloat("_ZWrite",0);owned.SetFloat("_SrcBlend",(int)BlendMode.SrcAlpha);
                    owned.SetFloat("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);owned.SetFloat("_Cull",(int)CullMode.Off);
                    owned.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");owned.renderQueue=(int)RenderQueue.Transparent;
                }
                if(name.Contains("Lamp")){owned.EnableKeyword("_EMISSION");owned.SetColor("_EmissionColor",material.color*.3f);}
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),material.name),owned);
                EditorUtility.SetDirty(owned);
            }
            importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        private static Sprite Preview(string model)
        {
            string path=Root+"/Previews/"+model+"_FrontThreeQuarter.png";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null) return null;
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.maxTextureSize=1024; importer.mipmapEnabled=false; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static void BuildDealershipFacility()
        {
            const string folder="Assets/LWS/TruckTaxi/Resources/TruckTaxi/Trucks";
            if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/LWS/TruckTaxi/Resources/TruckTaxi","Trucks");
            var root=PrefabUtility.LoadPrefabContents("Assets/LWS/TruckTaxi/Edenborough/Facilities/Edenborough_Garage.prefab");
            try
            {
                root.name="TruckTaxi Dealership Facility";
                var garage=root.GetComponent<TruckTaxiGarage>();if(garage==null) garage=root.AddComponent<TruckTaxiGarage>();
                var zone=new GameObject("Drive-in dealership and customization bay").AddComponent<BoxCollider>();
                zone.transform.SetParent(root.transform,false); zone.isTrigger=true;
                zone.center=new Vector3(0,2.9f,0);zone.size=new Vector3(7.8f,5.8f,23.8f);
                var binding=root.AddComponent<TruckTaxiDealershipFacility>();binding.garage=garage;binding.interactionVolume=zone;
                var text=new GameObject("Truck dealership sign",typeof(RectTransform),typeof(TMPro.TextMeshPro)).GetComponent<TMPro.TextMeshPro>();
                text.transform.SetParent(root.transform,false);text.transform.localPosition=new Vector3(0,4.8f,-12.1f);
                text.rectTransform.sizeDelta=new Vector2(15,1.8f);text.font=TMPro.TMP_Settings.defaultFontAsset;
                text.fontSize=3.8f;text.alignment=TMPro.TextAlignmentOptions.Center;text.color=Color.white;
                text.text="TRUCK DEALERSHIP\nCUSTOMIZATION";
                PrefabUtility.SaveAsPrefabAsset(root,folder+"/DealershipFacility.prefab");
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
