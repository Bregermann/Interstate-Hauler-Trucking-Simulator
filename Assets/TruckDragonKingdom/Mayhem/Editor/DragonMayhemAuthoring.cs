#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckKeepaway;
using LWS.TruckBalloonBattle;
using LWS.TruckCornhole;
using LWS.TruckCheeseRolling;
using MalbersAnimations.Controller;
using Michsky.UI.Heat;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.TruckDragonKingdom.Mayhem.Editor
{
    public static class DragonMayhemAuthoring
    {
        public const string Root="Assets/TruckDragonKingdom/Mayhem/";
        public const string GoldPath=Root+"Prefabs/Physical Dragon Gold.prefab";
        public const string SheepPath=Root+"Prefabs/Temporary Sheep.prefab";
        // Explicit selected configuration upgrade only. Neither mode playback
        // nor a build invokes this measurement or changes existing Drop data.
        public static int UpgradeActualGroundGrabContactGeometry()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Explicit GroundGrab geometry migration requires stopped authoring.");
            var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>(DragonSpeciesAuthoring.CatalogPath);
            if(catalog==null||!catalog.TryGet("dragon-inferno",out var species)||!species.TryGetAnimation(DragonAnimationAction.GroundGrab,out var mapping))throw new InvalidOperationException("Accepted actual Inferno GroundGrab action required before contact calibration.");
            string actorPath="Assets/TruckDragonKingdom/Prefabs/Dragons/"+species.id+"/"+species.id+" Dragon Actor.prefab";
            var contents=PrefabUtility.LoadPrefabContents(actorPath);
            try
            {
                var actor=contents.GetComponent<DragonCharacter>();var motor=actor.motor;
                var binding=motor.actionBindings.Single(value=>value.action==DragonAnimationAction.GroundGrab);
                motor.animator.enabled=false;mapping.clip.SampleAnimation(motor.gameObject,mapping.clip.length*binding.contactNormalizedTime);
                Vector3 offset=motor.transform.InverseTransformPoint(actor.leftGrip.position);
                if(!float.IsFinite(offset.x)||!float.IsFinite(offset.y)||!float.IsFinite(offset.z)||offset.sqrMagnitude<.01f||binding.contactNormalizedTime<=0||binding.contactNormalizedTime>=1)
                    throw new InvalidOperationException("Actual configured GroundGrab contact grip/timing is invalid.");
                int changed=0;
                foreach(DragonMayhemSport sport in Enum.GetValues(typeof(DragonMayhemSport)))
                {
                    string path=Root+"Configuration/"+sport+" Mayhem.asset";var config=AssetDatabase.LoadAssetAtPath<DragonMayhemConfiguration>(path);
                    if(config==null)throw new InvalidOperationException("Existing owned Mayhem configuration required: "+path);
                    if(config.groundGrabSpeciesId==species.id&&config.groundGrabContactClip==mapping.clip&&Mathf.Approximately(config.groundGrabContactNormalizedTime,binding.contactNormalizedTime)&&Vector3.Distance(config.groundGrabGripLocalOffset,offset)<.000001f)continue;
                    config.groundGrabSpeciesId=species.id;config.groundGrabContactClip=mapping.clip;config.groundGrabContactNormalizedTime=binding.contactNormalizedTime;config.groundGrabGripLocalOffset=offset;
                    EditorUtility.SetDirty(config);AssetDatabase.SaveAssetIfDirty(config);changed++;
                    Debug.Log("MAYHEM_AUTHORED_ACTUAL_GROUND_GRAB_GEOMETRY config="+path+" species="+species.id+" actualClip="+AssetDatabase.GetAssetPath(mapping.clip)+" normalized="+binding.contactNormalizedTime+" motorLocalGrip="+offset.ToString("F6"));
                }
                return changed;
            }
            finally{PrefabUtility.UnloadPrefabContents(contents);}
        }
        public static int UpgradeActualDropContactGeometry()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Explicit Drop geometry migration requires stopped Play Mode.");
            var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>(DragonSpeciesAuthoring.CatalogPath);
            if(catalog==null)throw new InvalidOperationException("Existing actual species catalog required.");
            var species=catalog.species.First(s=>s!=null&&s.Supports(DragonSpeciesRole.Flying|DragonSpeciesRole.SheepCarrier)&&s.HasAnimation(DragonAnimationAction.Drop));
            var actor=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckDragonKingdom/Prefabs/Dragons/"+species.id+"/"+species.id+" Dragon Actor.prefab");
            int changed=0;
            foreach(DragonMayhemSport sport in Enum.GetValues(typeof(DragonMayhemSport)))
            {
                string path=Root+"Configuration/"+sport+" Mayhem.asset";var config=AssetDatabase.LoadAssetAtPath<DragonMayhemConfiguration>(path);
                if(config==null)throw new InvalidOperationException("Existing owned Mayhem configuration required: "+path);
                if(CalibrateDropGeometry(config,species,actor)){AssetDatabase.SaveAssetIfDirty(config);changed++;}
            }
            return changed;
        }
        static bool CalibrateDropGeometry(DragonMayhemConfiguration config,DragonSpeciesDefinition species,GameObject actualActor)
        {
            if(actualActor==null||!species.TryGetAnimation(DragonAnimationAction.Drop,out var mapping)||mapping.clip==null)throw new InvalidOperationException("Actual configured actor and real Drop clip required.");
            var contents=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(actualActor));
            try
            {
                var actor=contents.GetComponent<DragonCharacter>();var motor=actor.motor;var binding=motor.actionBindings.Single(value=>value.action==DragonAnimationAction.Drop);
                motor.animator.enabled=false;
                mapping.clip.SampleAnimation(motor.gameObject,mapping.clip.length*binding.contactNormalizedTime);
                Vector3 offset=motor.transform.InverseTransformPoint(actor.mouth.position);
                if(!float.IsFinite(offset.x)||!float.IsFinite(offset.y)||!float.IsFinite(offset.z)||offset.sqrMagnitude<.01f)throw new InvalidOperationException("Measured real calibrated mouth offset is invalid.");
                if(config.dropSpeciesId==species.id&&config.dropContactClip==mapping.clip&&Mathf.Approximately(config.dropContactNormalizedTime,binding.contactNormalizedTime)&&Vector3.Distance(config.dropMouthLocalOffset,offset)<.000001f)return false;
                config.dropSpeciesId=species.id;config.dropContactClip=mapping.clip;config.dropContactNormalizedTime=binding.contactNormalizedTime;config.dropMouthLocalOffset=offset;EditorUtility.SetDirty(config);
                Debug.Log("MAYHEM_AUTHORED_ACTUAL_DROP_GEOMETRY config="+AssetDatabase.GetAssetPath(config)+" species="+species.id+" actualClip="+AssetDatabase.GetAssetPath(mapping.clip)+" normalized="+binding.contactNormalizedTime+" motorLocalMouth="+offset.ToString("F6"));
                return true;
            }
            finally{PrefabUtility.UnloadPrefabContents(contents);}
        }
        // Explicit additive migration only. The caller owns reviewed original
        // scene save/backup; this method never opens or overwrites a scene.
        public static DragonMayhemDirector Install(Component original)
        {
            if(original==null || !original.gameObject.scene.IsValid())throw new ArgumentException("An original loaded sport controller is required.");
            var prior=original.GetComponentInChildren<DragonMayhemDirector>(true);
            if(prior!=null)throw new InvalidOperationException("Mayhem is already installed; review that configuration instead of duplicating it.");
            var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>(DragonSpeciesAuthoring.CatalogPath);
            if(catalog==null)throw new InvalidOperationException("Native-configured owned dragon catalog is required.");
            var species=catalog.species.FirstOrDefault(s=>s!=null&&s.Supports(DragonSpeciesRole.Flying|DragonSpeciesRole.SheepCarrier)&&s.HasAnimation(DragonAnimationAction.Drop)&&s.HasAnimation(DragonAnimationAction.Breath)&&s.HasAnimation(DragonAnimationAction.Eat));
            if(species==null)throw new InvalidOperationException("Mayhem needs a real configured flying dragon with Grab/Carry/Drop/Eat/Breath clips.");
            var actor=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckDragonKingdom/Prefabs/Dragons/"+species.id+"/"+species.id+" Dragon Actor.prefab");
            if(actor==null)throw new InvalidOperationException("Configured real dragon actor is missing.");
            var root=new GameObject("Dragon Mayhem · optional additive variant");root.transform.SetParent(original.transform,false);
            var adapter=root.AddComponent<DragonMayhemSportAdapter>();
            SportsMenuView menu;
            if(original is KeepawayMatch k){adapter.sport=DragonMayhemSport.Keepaway;adapter.keepaway=k;menu=k.menus;}
            else if(original is BalloonMatch b){adapter.sport=DragonMayhemSport.BalloonBattle;adapter.balloons=b;menu=b.menus;}
            else if(original is CornholeMatch c){adapter.sport=DragonMayhemSport.Cornhole;adapter.cornhole=c;menu=c.menus;}
            else if(original is CheeseMatch h){adapter.sport=DragonMayhemSport.CheeseRolling;adapter.cheese=h;menu=h.menus;}
            else {UnityEngine.Object.DestroyImmediate(root);throw new ArgumentException("Only Keepaway, Balloon Battle, Cornhole, and Cheese Rolling support Mayhem.");}
            adapter.Validate();
            var director=root.AddComponent<DragonMayhemDirector>();director.adapter=adapter;director.catalog=catalog;director.configuration=CreateConfiguration(adapter.sport);director.actorPrefab=actor;director.speciesId=species.id;director.goldPrefab=CreateGold();director.sheepPrefab=CreateSheep();
            CalibrateDropGeometry(director.configuration,species,actor);
            var presentation=root.AddComponent<DragonMayhemPresentation>();presentation.director=director;
            var toggle=SportsUIAuthoring.Button(menu.pages[0].transform,"DRAGON MAYHEM: OFF","DragonMayhemToggle",new Vector2(480,425),new Vector2(460,36));
            UnityEngine.Object.DestroyImmediate(toggle.GetComponent<SportsMenuAction>());
            presentation.toggle=toggle;presentation.caption=toggle.normalTextObj;
            presentation.helpButton=SportsUIAuthoring.Button(menu.pages[0].transform,"MAYHEM RULES","MayhemRules",new Vector2(-510,425),new Vector2(350,36));UnityEngine.Object.DestroyImmediate(presentation.helpButton.GetComponent<SportsMenuAction>());
            presentation.helpPage=SportsUIAuthoring.Page("Optional Dragon Mayhem Rules",menu.transform,"DRAGON MAYHEM · OPTIONAL VARIANT");
            SportsUIAuthoring.Text("Actual variant rules",presentation.helpPage.transform,DragonMayhemDirector.Help,new Vector2(1250,460),new Vector2(0,0),29);
            presentation.closeHelp=SportsUIAuthoring.Button(presentation.helpPage.transform,"BACK TO SETUP","CloseMayhemRules",new Vector2(0,-350));UnityEngine.Object.DestroyImmediate(presentation.closeHelp.GetComponent<SportsMenuAction>());presentation.helpPage.SetActive(false);
            var overlay=SportsUIAuthoring.Rect("Dragon Mayhem overlay",menu.transform,new Vector2(450,145),new Vector2(705,420));
            presentation.overlay=overlay;
            presentation.warning=SportsUIAuthoring.Text("Visible pass warning",overlay.transform,"",new Vector2(430,42),new Vector2(0,45),20);
            presentation.bonus=SportsUIAuthoring.Text("Physical side gold",overlay.transform,"",new Vector2(410,95),new Vector2(0,-22),17);
            presentation.resultPanel=SportsUIAuthoring.Rect("Dragon Mayhem side tally",menu.pages[6].transform,new Vector2(1320,58),new Vector2(0,-165));
            presentation.result=SportsUIAuthoring.Text("Variant side tokens",presentation.resultPanel.transform,"",new Vector2(1320,58),Vector2.zero,20);
            string badge="Assets/TruckDragonKingdom/Art/Dragon Mayhem badge.png";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(badge);
            if(texture!=null)
            {
                var image=SportsUIAuthoring.Rect("Optional Mayhem badge",overlay.transform,new Vector2(96,64),new Vector2(-175,98)).AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;
            }
            overlay.SetActive(false);presentation.resultPanel.SetActive(false);
            UpgradePresentation(director);
            EditorUtility.SetDirty(original);EditorUtility.SetDirty(director);return director;
        }
        // Narrow idempotent migration for already-authored scenes. This never
        // reinstalls a director, actor, configuration, gold, sheep, or rules.
        public static DragonMayhemPresentation UpgradePresentation(DragonMayhemDirector existingDirector)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop play mode before explicit presentation migration.");
            if(existingDirector==null||!existingDirector.gameObject.scene.IsValid())throw new ArgumentException("An existing loaded Mayhem director is required.");
            var presentation=existingDirector.GetComponent<DragonMayhemPresentation>();
            if(presentation==null||presentation.helpPage==null||presentation.helpButton==null||presentation.closeHelp==null||presentation.overlay==null)throw new InvalidOperationException("Preserve and inspect the existing Mayhem presentation; required owned UI bindings are missing.");
            var adapter=existingDirector.adapter;adapter.Validate();
            SportsMenuView menu=adapter.keepaway!=null?adapter.keepaway.menus:adapter.balloons!=null?adapter.balloons.menus:adapter.cornhole!=null?(SportsMenuView)adapter.cornhole.menus:adapter.cheese.menus;
            presentation.menu=menu;
            var group=menu.pages[0].GetComponent<CanvasGroup>();if(group==null)group=menu.pages[0].AddComponent<CanvasGroup>();presentation.setupInteraction=group;
            if(presentation.helpWindow==null)
            {
                const string heat="Assets/Heat - Complete Modern UI/Prefabs/UI Elements/Modal Window/Modal Window (Custom Content).prefab";
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(heat);if(prefab==null)throw new InvalidOperationException("Installed Heat custom-content modal is required.");
                var ownedContent=presentation.helpPage.transform.Cast<Transform>().ToArray();
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,presentation.helpPage.transform);instance.name="Mayhem help · installed Heat modal";instance.SetActive(true);
                var window=instance.GetComponent<ModalWindowManager>();if(window==null)throw new InvalidOperationException("Installed Heat modal component schema changed.");
                var content=instance.transform.Find("Content");if(content==null)throw new InvalidOperationException("Installed Heat custom-content transform changed.");
                foreach(var layout in content.GetComponents<UnityEngine.UI.LayoutGroup>())layout.enabled=false;
                foreach(Transform child in content)child.gameObject.SetActive(false);
                foreach(var hotkey in instance.GetComponentsInChildren<HotkeyEvent>(true))hotkey.enabled=false;
                foreach(var child in ownedContent)child.SetParent(content,false);
                instance.GetComponent<Animator>().updateMode=AnimatorUpdateMode.UnscaledTime;
                presentation.helpWindow=window;
            }
            var modal=presentation.helpWindow;modal.useCustomContent=true;modal.useLocalization=false;modal.showCancelButton=modal.showConfirmButton=false;modal.closeOnConfirm=modal.closeOnCancel=false;
            modal.startBehaviour=ModalWindowManager.StartBehaviour.Enable;modal.closeBehaviour=ModalWindowManager.CloseBehaviour.Disable;modal.inputType=ModalWindowManager.InputType.Free;modal.isOn=false;
            presentation.helpPage.SetActive(false);
            var overlay=(RectTransform)presentation.overlay.transform;overlay.anchoredPosition=new Vector2(705,385);overlay.sizeDelta=new Vector2(450,225);
            var badge=overlay.Find("Optional Mayhem badge") as RectTransform;if(badge!=null){badge.anchoredPosition=new Vector2(-175,70);badge.sizeDelta=new Vector2(96,64);}
            EditorUtility.SetDirty(presentation);EditorUtility.SetDirty(modal);EditorUtility.SetDirty(overlay);return presentation;
        }
        // Explicit saved-UI upgrade. The former footer occupied the original
        // replay button. Only the known factory placement/type sizes qualify;
        // Play Mode/builds never overwrite a designer's layout.
        public static bool UpgradeResultTallyLayout(DragonMayhemDirector existingDirector)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stop playback/compilation before the explicit result tally upgrade.");
            var view=existingDirector!=null?existingDirector.GetComponent<DragonMayhemPresentation>():null;
            if(view==null||view.resultPanel==null||view.result==null)throw new InvalidOperationException("Existing Mayhem result bindings are required.");
            var rect=(RectTransform)view.resultPanel.transform;var text=view.result;
            bool original=rect.anchoredPosition==new Vector2(0,-240)&&text.fontSize==17&&text.fontSizeMin==13.6f&&text.fontSizeMax==17;
            bool current=rect.anchoredPosition==new Vector2(0,-165)&&text.fontSize==20&&text.fontSizeMin==16&&text.fontSizeMax==20;
            if(rect.sizeDelta!=new Vector2(1320,58)||text.rectTransform.sizeDelta!=new Vector2(1320,58)||text.rectTransform.anchoredPosition!=Vector2.zero||!original&&!current)throw new InvalidOperationException("Custom Mayhem results layout detected; preserve it for deliberate designer review.");
            if(current)return false;
            Undo.RecordObjects(new UnityEngine.Object[]{rect,text},"Separate Mayhem results tally from replay controls");
            rect.anchoredPosition=new Vector2(0,-165);text.fontSize=text.fontSizeMax=20;text.fontSizeMin=16;
            EditorUtility.SetDirty(rect);EditorUtility.SetDirty(text);return true;
        }
        public static void PersistOnlyResultTallyFields(DragonMayhemDirector existingDirector)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped explicit authoring required.");
            var view=existingDirector.GetComponent<DragonMayhemPresentation>();var rect=(RectTransform)view.resultPanel.transform;var text=view.result;
            if(rect.anchoredPosition!=new Vector2(0,-165)||text.fontSize!=20||text.fontSizeMin!=16||text.fontSizeMax!=20)throw new InvalidOperationException("Validate the exact selected UI upgrade before persisting it.");
            string path=existingDirector.gameObject.scene.path;
            if(string.IsNullOrEmpty(path)||!File.Exists(path))throw new InvalidOperationException("An existing saved scene is required.");
            // Whole-scene SaveScene also serializes newly introduced fields
            // on unrelated components. Patch only these two existing owned
            // UI documents; preserve every other original byte and identity.
            string original=System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(path)),updated=original;
            string ReplaceField(string value,string id,string field,string desired)
            {
                string pattern=@"(?ms)^--- !u!\d+ &"+System.Text.RegularExpressions.Regex.Escape(id)+@"[^\r\n]*\r?\n.*?(?=^--- !u!|\z)";
                var documents=System.Text.RegularExpressions.Regex.Matches(value,pattern);if(documents.Count!=1)throw new InvalidOperationException("Exact existing UI identity required: "+id);
                var document=documents[0];string line=@"(?m)^(  "+System.Text.RegularExpressions.Regex.Escape(field)+@": )[^\r\n]*";
                if(System.Text.RegularExpressions.Regex.Matches(document.Value,line).Count!=1)throw new InvalidOperationException("Exact serialized UI field required: "+field);
                string actual=System.Text.RegularExpressions.Regex.Match(document.Value,line).Value.Substring(("  "+field+": ").Length);
                string factory=field=="m_AnchoredPosition"?"{x: 0, y: -240}":field=="m_fontSizeMin"?"13.6":"17";
                if(actual!=factory&&actual!=desired)throw new InvalidOperationException("Preserve a customized on-disk UI field: "+field);
                string replaced=System.Text.RegularExpressions.Regex.Replace(document.Value,line,match=>match.Groups[1].Value+desired);
                return value.Substring(0,document.Index)+replaced+value.Substring(document.Index+document.Length);
            }
            string rectId=GlobalObjectId.GetGlobalObjectIdSlow(rect).targetObjectId.ToString(),textId=GlobalObjectId.GetGlobalObjectIdSlow(text).targetObjectId.ToString();
            updated=ReplaceField(updated,rectId,"m_AnchoredPosition","{x: 0, y: -165}");
            foreach(string field in new[]{"m_fontSize","m_fontSizeBase","m_fontSizeMax"})updated=ReplaceField(updated,textId,field,"20");
            updated=ReplaceField(updated,textId,"m_fontSizeMin","16");
            if(updated!=original){File.WriteAllBytes(path,System.Text.Encoding.UTF8.GetBytes(updated));AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);}
        }
        static DragonMayhemConfiguration CreateConfiguration(DragonMayhemSport sport)
        {
            string path=Root+"Configuration/"+sport+" Mayhem.asset";
            var config=AssetDatabase.LoadAssetAtPath<DragonMayhemConfiguration>(path);if(config!=null){if(config.sport!=sport)throw new InvalidOperationException("Mayhem configuration sport mismatch.");return config;}
            Directory.CreateDirectory(Root+"Configuration");config=ScriptableObject.CreateInstance<DragonMayhemConfiguration>();config.sport=sport;
            switch(sport)
            {
                case DragonMayhemSport.Keepaway:config.variantName="DRAGON RAID";config.sideBonusLabel="RAID GOLD";config.maximumSideGold=6;break;
                case DragonMayhemSport.BalloonBattle:config.variantName="DRAGONSTORM";config.sideBonusLabel="STORM GOLD";config.maximumSideGold=4;config.gustVelocityChange=.9f;break;
                case DragonMayhemSport.Cornhole:config.variantName="DRAGON GOLD RUSH";config.sideBonusLabel="APPROACH GOLD";config.maximumSideGold=4;config.gustVelocityChange=.35f;config.approachHeight=18;break;
                case DragonMayhemSport.CheeseRolling:config.variantName="DRAGON PURSUIT";config.sideBonusLabel="PURSUIT GOLD";config.maximumSideGold=5;config.gustVelocityChange=1.1f;config.approachHeight=18;break;
            }
            AssetDatabase.CreateAsset(config,path);return config;
        }
        static GameObject CreateGold()
        {
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(GoldPath);if(existing!=null)return existing;
            Directory.CreateDirectory(Root+"Prefabs");Directory.CreateDirectory(Root+"Art");
            // A small functional collectible, not a replacement environment kit.
            // Distinct milled edges and two faces are authored in metre units.
            var vertices=new Vector3[98];
            vertices[96]=new Vector3(0,.07f,0);vertices[97]=new Vector3(0,-.07f,0);
            for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;float r=i%2==0?.34f:.325f;vertices[i]=new Vector3(Mathf.Cos(a)*r,.07f,Mathf.Sin(a)*r);vertices[48+i]=new Vector3(Mathf.Cos(a)*r,-.07f,Mathf.Sin(a)*r);}
            var faces=new System.Collections.Generic.List<int>();
            for(int i=0;i<48;i++){int j=(i+1)%48;faces.AddRange(new[]{96,j,i,97,48+i,48+j,i,j,48+i,j,48+j,48+i});}
            var mesh=new Mesh{name="Milled physical dragon gold"};mesh.vertices=vertices;mesh.triangles=faces.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Root+"Art/Milled dragon gold.asset");
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Dragon gold"};material.SetColor("_BaseColor",new Color(1,.63f,.06f));material.SetFloat("_Metallic",.85f);material.SetFloat("_Smoothness",.62f);AssetDatabase.CreateAsset(material,Root+"Art/Dragon gold.mat");
            var physics=new PhysicsMaterial("Gold bounce"){bounciness=.42f,dynamicFriction=.5f,staticFriction=.55f};AssetDatabase.CreateAsset(physics,Root+"Art/Gold bounce.physicMaterial");
            var coin=new GameObject("Physical Dragon Gold");coin.SetActive(false);coin.AddComponent<MeshFilter>().sharedMesh=mesh;coin.AddComponent<MeshRenderer>().sharedMaterial=material;
            var solid=coin.AddComponent<SphereCollider>();solid.radius=.32f;solid.sharedMaterial=physics;
            var trigger=coin.AddComponent<SphereCollider>();trigger.radius=.82f;trigger.isTrigger=true;
            var body=coin.AddComponent<Rigidbody>();body.mass=.08f;body.isKinematic=true;body.collisionDetectionMode=CollisionDetectionMode.Discrete;body.interpolation=RigidbodyInterpolation.Interpolate;
            var gold=coin.AddComponent<DragonMayhemGold>();gold.body=body;gold.solid=solid;
            var result=PrefabUtility.SaveAsPrefabAsset(coin,GoldPath);UnityEngine.Object.DestroyImmediate(coin);return result;
        }
        static GameObject CreateSheep()
        {
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(SheepPath);if(existing!=null)return existing;
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckCheeseRolling/Prefabs/ExplosiveSheep.prefab");
            if(source==null)throw new InvalidOperationException("Original genuine SheepV5 prefab is required.");
            var animal=(GameObject)PrefabUtility.InstantiatePrefab(source);animal.SetActive(false);animal.name="Temporary Sheep · nonexplosive";
            var original=animal.GetComponent<CheeseSheep>();if(original!=null){if(original.warning!=null)UnityEngine.Object.DestroyImmediate(original.warning);UnityEngine.Object.DestroyImmediate(original);}
            var body=animal.GetComponent<Rigidbody>();if(body==null)body=animal.AddComponent<Rigidbody>();body.isKinematic=true;body.mass=45;body.constraints=RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
            var item=animal.AddComponent<Pickable>();item.ByAnimation=true;item.AutoPick=false;item.Collectable=false;
            var sheep=animal.AddComponent<DragonMayhemSheep>();sheep.item=item;sheep.temporary=true;
            SheepMotionAuthoring.Install(animal,animal.transform);
            var result=PrefabUtility.SaveAsPrefabAsset(animal,SheepPath);UnityEngine.Object.DestroyImmediate(animal);return result;
        }
    }
}
#endif
