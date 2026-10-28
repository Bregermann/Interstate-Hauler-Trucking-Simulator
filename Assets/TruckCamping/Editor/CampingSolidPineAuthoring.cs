using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace LWS.TruckCamping.Editor
{
    // Explicit reviewed stage only. Never run on startup/build; never reseed,
    // regenerate or save scenes/shared world/nav/vendor assets.
    public static class CampingSolidPineAuthoring
    {
        public const string SourcePrefab="Assets/JC_LP_MegaCity/Prefabs/Nature/SM_Nature_Tree_07.prefab";
        public const string FormerPrefab="Assets/TruckCamping/Prefabs/Camp Haul living Vista pine.prefab";
        public const string OwnedPrefab="Assets/TruckCamping/Prefabs/Camp Haul solid JC07 pine.prefab";
        public const string SharedWorld="Assets/TruckCamping/Prefabs/Camp Haul shared wilderness.prefab";
        public const string LatestReceipt="Logs/TruckSports/Impossible07/CampingSolidPineUpgrade/latest-upgrade-receipt.json";
        [Serializable]public sealed class Input{public string path,sha256;public Input(string p,string s){path=p;sha256=s;}}
        [Serializable]public sealed class Tile{public string path,guid,beforeSha256,afterSha256,metaSha256,preservedDigest;public int trees;}
        [Serializable]public sealed class Receipt{public string status,backup,source,previousOwned,owned,worldBefore,worldAfter;public int disabledColliders;public Vector3 originalEnvelope,ownedEnvelope;public Tile[] files;public Input[] retainedInputs;}
        static readonly Input[] ReviewedInputs={
            new Input("Assets/JC_LP_MegaCity/Materials/M_MegaCity_01.mat","63e535eb3c3379d42fe2cdc7ec51c470e7304d40aac497f63deb7890c16d564e"),
            new Input("Assets/JC_LP_MegaCity/Materials/M_MegaCity_01.mat.meta","f63b9f94216a9a54832820acbb948753cfb6f1d68176ad604e7c8a2e4b04fb99"),
            new Input("Assets/JC_LP_MegaCity/Models/Nature/SM_Nature_Tree_07.fbx","5afbe9e6213ee10c4abb8f3dc0335c9f0b460044f6ce19a2ec544c3752b564e6"),
            new Input("Assets/JC_LP_MegaCity/Models/Nature/SM_Nature_Tree_07.fbx.meta","448c21e8a4cb11208b8ec806bc3f80b938d1d720f52992190d4953be0fca19f7"),
            new Input("Assets/JC_LP_MegaCity/Prefabs/Nature/SM_Nature_Tree_07.prefab","3fd3e276ce1c39400bcb91ab9423a9021401e19481cd6bfd7f33d55aba4db20a"),
            new Input("Assets/JC_LP_MegaCity/Prefabs/Nature/SM_Nature_Tree_07.prefab.meta","1d3b7607413b6567d78a601a30a987e13a10ee8e294b3eb6a03830e5d7bf4fbc"),
            new Input("Assets/JC_LP_MegaCity/Textures/T_MegaCity_01.png","1749fe55f16b130bbeccce5d2ea07ca75d4916afb8f3d6e305a97cb76fc11a8a"),
            new Input("Assets/JC_LP_MegaCity/Textures/T_MegaCity_01.png.meta","250cf70eae2fc8f67faba9198778bc2767a7b0e2954097bff09b1394bd635620"),
            new Input("Assets/JC_LP_MegaCity/Textures/T_MegaCity_LightMap.png","9be2d85d88554ac847421c592426f26f0f9e9b9b7ad7a0cf66b3d3f300e48385"),
            new Input("Assets/JC_LP_MegaCity/Textures/T_MegaCity_LightMap.png.meta","b38a737c61e4fb66c207166677ed9843d42d5d1e2db8bc12243602c8928823be"),
            new Input("Assets/TruckCamping/Models/Camp Haul connected woodland navigation.asset","514343ce7ac8c43c483ea353743fd6dd1ce8c800f7855af82618d75e17add505"),
            new Input("Assets/TruckCamping/Models/Camp Haul connected woodland navigation.asset.meta","03cd55d7ddd0a9ae8d6e2035b9cb46bcdedc1ea31e0d786aca6d10f5cab8f718"),
            new Input("Assets/TruckCamping/Prefabs/Camp Haul living Vista pine.prefab","73699247864c4f3c417751d207f1c06ca532d6d8a817934c733e3edd71709d25"),
            new Input("Assets/TruckCamping/Prefabs/Camp Haul living Vista pine.prefab.meta","d9696dbaaf4ba31506a6e88abbc4d0c9f25f2153928c79965cee2927baccc68e"),
            new Input("Assets/TruckCamping/Prefabs/Camp Haul shared wilderness.prefab","a8448bc181521b1f01734f143c0bbf108893636e47324d44c65875b7574e0cdd"),
            new Input("Assets/TruckCamping/Prefabs/Camp Haul shared wilderness.prefab.meta","e49c815a758743e10c9aa4ac9878f61d3af1da5adf8aca016dca291ae5001ff1"),
            new Input("Assets/TruckCamping/Scenes/TruckCamping.unity","8004ea2236519ec5bbfdd96b4656d215462397a767d900a9ce1471ab4e011a43"),
            new Input("Assets/TruckCamping/Scenes/TruckCamping.unity.meta","ce7d37794a91c148b97b042075d35f4a439d0e548c634bad9061c494cce2047e"),
            new Input("Assets/TruckPetPals/Scenes/TruckPetPals.unity","b94eb9431d663f596fccf7af98895a91ae2ba61600e29b1824dd1e1f5d9c3a16"),
            new Input("Assets/TruckPetPals/Scenes/TruckPetPals.unity.meta","be17ac284cf15466db16d0f4fd0cb6834275a070236fa761d8733fcb2301b251"),
        };
        static readonly Tile[] ReviewedTiles={
            new Tile{path="Assets/TruckCamping/Models/Camp Haul Vista terrain 0 0.asset",guid="ab66ebbed2d5de042a26d66937d9ef41",beforeSha256="a9db85b879fbfa4f17169af6cab9de8aca79b3736285aaac992a00e6f184fa0f",metaSha256="c44f5b7249451d8e3ae87b3f740d10bfb051a9d9bf7278584033f959c82c4c12",preservedDigest="465048ef30781af0846b93bd1b3e08bcc600239a93c1d066169165ff29448077",trees=588},
            new Tile{path="Assets/TruckCamping/Models/Camp Haul Vista terrain 1 0.asset",guid="536d3ebb9c605dc4c8125f50b46b7dbe",beforeSha256="7fd10306fd665444321a83137bb0edad5e45d063217d9c6810b4f0af61fa2ae8",metaSha256="9ec9c530eb0a460703af7daddff0b901abe676b89b4df433dacc93ba887b050d",preservedDigest="538a603e5fddb27da0601cc4055d8bc233310e09a9b07e88fbf847495804e120",trees=582},
            new Tile{path="Assets/TruckCamping/Models/Camp Haul Vista terrain 0 1.asset",guid="41b2c3577830ed9498c578c32d6b4553",beforeSha256="9f689b4fc3d19943898ce5f8337f1dfa76b6755218820ae754e4bad6c22132dd",metaSha256="16facd489792f8660ba79d77f855abc6f3ebc2913e817b07f76ca72e56973686",preservedDigest="d8b8f75b9c7dd03e33f7c6df160764c62878b2325df8bf42415bbd1076d60c25",trees=578},
            new Tile{path="Assets/TruckCamping/Models/Camp Haul Vista terrain 1 1.asset",guid="aa4f37bee36f7cb4fb33f3c3f9d8ec8e",beforeSha256="a7a1e94ab0c3acbe7feb14f7f8a1462817e24645dcd4dead584a3a2be853e709",metaSha256="3706e0c3152f9316c31b5c562d48aa8923862907bf025844a44cfeb91460480e",preservedDigest="e69a16da1a91906eac002be3d364389a362e5b4c8b12ef3eea4e774507495178",trees=549},
        };
        public static string Digest(string path)=>CampingLivingForestAuthoring.DigestFile(path);
        public static string Semantic(TerrainData data)=>CampingLivingForestAuthoring.PreservedTerrainDigest(data);
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        public static void RequireAbsentOutput(string path){Require(!File.Exists(path)&&!File.Exists(path+".meta")&&AssetDatabase.LoadAssetAtPath<GameObject>(path)==null,"Existing/customized owned tree asset or orphan meta preserved: "+path);}
        public static Bounds RenderBounds(GameObject model){var rs=model.GetComponentsInChildren<MeshRenderer>(true);Require(rs.Length>0,"Real original tree meshes required.");var bounds=rs[0].bounds;foreach(var renderer in rs)bounds.Encapsulate(renderer.bounds);return bounds;}
        public static void ValidatePreservedTile(TerrainData data,int tile,GameObject expectedPrototype)
        {
            Require(tile>=0&&tile<ReviewedTiles.Length&&data!=null,"Exact authored tile required.");var row=ReviewedTiles[tile];
            Require(data.treeInstanceCount==row.trees&&Semantic(data)==row.preservedDigest,"Unknown/customized terrain/tree/height/paint/holes/layers/details preserved: "+row.path);
            Require(data.treePrototypes.Length==1&&data.treePrototypes[0].prefab==expectedPrototype&&data.treePrototypes[0].bendFactor==.2f,"Unknown/customized prototype/bend preserved: "+row.path);
        }
        public static void ValidateReviewedStageAndMissingOutput()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Explicit stopped author operation only.");RequireAbsentOutput(OwnedPrefab);
            foreach(var input in ReviewedInputs)Require(File.Exists(input.path)&&Digest(input.path)==input.sha256,"Unknown/customized reviewed source/world/nav/scene preserved: "+input.path);
            var former=AssetDatabase.LoadAssetAtPath<GameObject>(FormerPrefab);Require(former!=null,"Actual retained Vista variant required.");
            for(int i=0;i<ReviewedTiles.Length;i++){var row=ReviewedTiles[i];Require(File.Exists(row.path)&&Digest(row.path)==row.beforeSha256&&Digest(row.path+".meta")==row.metaSha256&&AssetDatabase.AssetPathToGUID(row.path)==row.guid,"Unknown/customized exact terrain input preserved: "+row.path);var data=AssetDatabase.LoadAssetAtPath<TerrainData>(row.path);Require(!EditorUtility.IsDirty(data),"Unsaved terrain edits preserved: "+row.path);ValidatePreservedTile(data,i,former);}
        }
        public static void ConfigureOwnedInstance(GameObject model,GameObject former)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefab);Require(PrefabUtility.GetCorrespondingObjectFromSource(model)==source,"Exact connected vendor prefab instance required.");
            Require(model.transform.localScale==Vector3.one&&model.transform.localPosition==Vector3.zero&&model.transform.localRotation==Quaternion.identity&&model.GetComponent<Renderer>()==null,"Reviewed unit root and separate original render children required.");
            Require(model.GetComponentsInChildren<Rigidbody>(true).Length==0&&model.GetComponentsInChildren<MonoBehaviour>(true).Length==0,"Owned tree must remain render-only.");
            var colliders=model.GetComponentsInChildren<Collider>(true);Require(colliders.Length==2&&colliders.All(c=>c.enabled),"Exact two original JC07 colliders required before owned overrides.");
            var rs=model.GetComponentsInChildren<Renderer>(true);var materials=rs.SelectMany(r=>r.sharedMaterials).ToArray();var meshes=model.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).ToArray();
            var group=model.GetComponent<LODGroup>();Require(group!=null&&group.GetLODs().Length==2,"Actual two original vendor LODs required.");var levels=group.GetLODs().Select(l=>l.screenRelativeTransitionHeight).ToArray();Require(levels[0]==.25f&&levels[1]==.01f,"Original vendor LOD thresholds changed.");
            var wanted=RenderBounds(former);var current=RenderBounds(model);float fit=wanted.size.y/current.size.y;
            foreach(Transform child in model.transform){child.localPosition*=fit;child.localScale*=fit;}
            current=RenderBounds(model);var offset=new Vector3(wanted.center.x-current.center.x,wanted.min.y-current.min.y,wanted.center.z-current.center.z);foreach(Transform child in model.transform)child.localPosition+=offset;
            foreach(var collider in colliders)collider.enabled=false;group.RecalculateBounds();model.name="Camp Haul solid JC07 pine";
            current=RenderBounds(model);Require(Mathf.Abs(current.size.y-wanted.size.y)<.001f&&Mathf.Abs(current.min.y-wanted.min.y)<.001f&&current.size.x<=wanted.size.x+.001f&&current.size.z<=wanted.size.z+.001f,"Reviewed natural16.6m envelope/base/footprint fit failed.");
            Require(materials.SequenceEqual(rs.SelectMany(r=>r.sharedMaterials))&&meshes.SequenceEqual(model.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh)),"Vendor mesh/material references must remain exact.");
            Require(group.GetLODs().Select(l=>l.screenRelativeTransitionHeight).SequenceEqual(levels)&&model.transform.localScale==Vector3.one&&colliders.All(c=>!c.enabled),"Unit root/two genuine LODs/zero enabled colliders required.");
        }
        public static void UpgradeExactReviewedVistaPrototypesToJc07()
        {
            ValidateReviewedStageAndMissingOutput();
            string backup=Path.GetFullPath("Logs/TruckSports/Impossible07/CampingSolidPineUpgrade/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));Require(!Directory.Exists(backup),"Fresh tree upgrade backup required.");Directory.CreateDirectory(backup);
            var former=AssetDatabase.LoadAssetAtPath<GameObject>(FormerPrefab);var data=ReviewedTiles.Select(t=>AssetDatabase.LoadAssetAtPath<TerrainData>(t.path)).ToArray();
            for(int i=0;i<data.Length;i++){File.Copy(ReviewedTiles[i].path,Path.Combine(backup,i+"-vista.asset"));File.Copy(ReviewedTiles[i].path+".meta",Path.Combine(backup,i+"-vista.asset.meta"));}
            File.Copy(SharedWorld,Path.Combine(backup,"world-before.prefab"));File.Copy(SharedWorld+".meta",Path.Combine(backup,"world-before.prefab.meta"));
            var receipt=new Receipt{status="PREPARED_EXACT_VISTA_STAGE",backup=backup,source=SourcePrefab,previousOwned=FormerPrefab,owned=OwnedPrefab,worldBefore=Digest(SharedWorld),retainedInputs=ReviewedInputs.Select(x=>new Input(x.path,x.sha256)).ToArray()};
            string journal=Path.Combine(backup,"upgrade-receipt.json");File.WriteAllText(journal,JsonUtility.ToJson(receipt,true));
            var preview=EditorSceneManager.NewPreviewScene();
            try
            {
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefab),preview);
                var original=(GameObject)PrefabUtility.InstantiatePrefab(former,preview);receipt.originalEnvelope=RenderBounds(original).size;
                ConfigureOwnedInstance(model,original);receipt.ownedEnvelope=RenderBounds(model).size;receipt.disabledColliders=model.GetComponentsInChildren<Collider>(true).Count(c=>!c.enabled);
                // Only create a NEW connected prefab variant; no replacement or
                // vendor save. Child fit keeps the root unit for Terrain trees.
                var owned=PrefabUtility.SaveAsPrefabAsset(model,OwnedPrefab);Require(owned!=null&&PrefabUtility.GetPrefabAssetType(owned)==PrefabAssetType.Variant,"Actual new prefab VARIANT required.");
                Require(owned.transform.localScale==Vector3.one&&owned.transform.localPosition==Vector3.zero&&owned.transform.localRotation==Quaternion.identity,"Saved owned root must remain identity.");
                Require(owned.GetComponentsInChildren<Collider>(true).Length==2&&owned.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),"Saved variant must have ZERO enabled physics colliders.");
                for(int i=0;i<data.Length;i++)
                {
                    var detached=UnityEngine.Object.Instantiate(data[i]);try{detached.name=data[i].name;detached.treePrototypes=new[]{new TreePrototype{prefab=owned,bendFactor=.2f}};ValidatePreservedTile(detached,i,owned);}finally{UnityEngine.Object.DestroyImmediate(detached);}
                }
                foreach(var input in ReviewedInputs)Require(Digest(input.path)==input.sha256,"Reviewed input changed while preparing new variant: "+input.path);
                receipt.status="OWNED_VARIANT_VALIDATED_BEFORE_TERRAIN_WRITES";File.WriteAllText(journal,JsonUtility.ToJson(receipt,true));
                for(int i=0;i<data.Length;i++)
                {
                    data[i].treePrototypes=new[]{new TreePrototype{prefab=owned,bendFactor=.2f}};ValidatePreservedTile(data[i],i,owned);EditorUtility.SetDirty(data[i]);AssetDatabase.SaveAssetIfDirty(data[i]);
                    Require(Digest(ReviewedTiles[i].path+".meta")==ReviewedTiles[i].metaSha256&&AssetDatabase.AssetPathToGUID(ReviewedTiles[i].path)==ReviewedTiles[i].guid,"Terrain meta/GUID changed; original backup retained.");
                }
                receipt.files=ReviewedTiles.Select((t,i)=>new Tile{path=t.path,guid=t.guid,beforeSha256=t.beforeSha256,afterSha256=Digest(t.path),metaSha256=Digest(t.path+".meta"),preservedDigest=Semantic(data[i]),trees=data[i].treeInstanceCount}).ToArray();
                foreach(var input in ReviewedInputs)Require(Digest(input.path)==input.sha256,"Unrelated source/world/nav/scene/vendor input changed: "+input.path);
                File.Copy(SharedWorld,Path.Combine(backup,"world-after.prefab"));File.Copy(SharedWorld+".meta",Path.Combine(backup,"world-after.prefab.meta"));receipt.worldAfter=Digest(SharedWorld);Require(receipt.worldBefore==receipt.worldAfter,"Shared world prefab must remain BYTE-EXACT.");
                receipt.status="ONLY_FOUR_TREE_PROTOTYPE_REFS_CHANGED_ALL_TERRAIN_PAYLOADS_GUIDS_SHARED_WORLD_NAV_VENDOR_INPUTS_RETAINED";File.WriteAllText(journal,JsonUtility.ToJson(receipt,true));Directory.CreateDirectory(Path.GetDirectoryName(LatestReceipt));File.WriteAllText(LatestReceipt,JsonUtility.ToJson(receipt,true));
                Debug.Log("CAMP_SOLID_PINE_UPGRADE exact4tiles/2297tree records/heights/paint/holes/layers/details/metas/nav/sharedworld retained; new trueJC07variant unitroot/natural16.6m/2LODs/0enabledcolliders. Backup="+backup);
            }
            catch(Exception error){receipt.status="FAILED_OR_PARTIAL_EXPLICIT_BACKUP_RETAINED: "+error.Message;File.WriteAllText(journal,JsonUtility.ToJson(receipt,true));throw;}
            finally{EditorSceneManager.ClosePreviewScene(preview);}
        }
    }
}
