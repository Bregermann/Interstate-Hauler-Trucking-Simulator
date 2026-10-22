using System;
using System.IO;
using System.Linq;
using System.Text;
using LWS.TruckSports;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

namespace LWS.TruckDockDiving.Editor
{
    public static class DockPoolLifeAuthoring
    {
        const string Folder="Assets/TruckDockDiving/Presentation";
        static readonly string[] States={"Idle","Look","Talk A","Talk B","Listen","Dance","Clap","Cheer","Photo","Laugh","Walk","Disappointed"};
        static readonly string[] Clips={"idle1","lookaround","talk1","talk2","listen","dance","claphands","cheer","drivecar","talk2","walk","idle2"};
        static AnimationClip Clip(string directory,string word)
        {
            string path=AssetDatabase.FindAssets("t:AnimationClip",new[]{directory}).Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p=>Path.GetFileNameWithoutExtension(p).Equals(word,StringComparison.OrdinalIgnoreCase)||Path.GetFileNameWithoutExtension(p).Equals(word+"_f",StringComparison.OrdinalIgnoreCase)||word=="dance"&&(Path.GetFileNameWithoutExtension(p).Equals("Dance_man",StringComparison.OrdinalIgnoreCase)||Path.GetFileNameWithoutExtension(p).Equals("Dance_woman",StringComparison.OrdinalIgnoreCase)));
            if(path==null)throw new InvalidOperationException("Compatible owned UTS clip required: "+directory+"/"+word);
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
        }
        static void FitModel(Transform parent,string name,string path,Vector3 point,Vector3 size,float yaw=0)
        {
            var g=new GameObject(name).transform;g.SetParent(parent,false);g.SetPositionAndRotation(point,Quaternion.Euler(0,yaw,0));
            LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(g,path,size,0,true,"TruckDockDiving");
        }
        static GameObject Box(Transform parent,string name,Vector3 point,Vector3 size,Material material)
        {
            var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.name=name;box.transform.SetParent(parent,false);box.transform.position=point;box.transform.localScale=size;box.GetComponent<Renderer>().sharedMaterial=material;return box;
        }
        static ParticleSystem Spray(Transform parent,string name,Material material,bool vapor,Transform surface)
        {
            var ps=new GameObject(name).AddComponent<ParticleSystem>();ps.transform.SetParent(parent,false);ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=false;main.playOnAwake=false;main.duration=3;main.startLifetime=2;main.startSpeed=0;main.startSize=.3f;
            main.startColor=vapor?new Color(.86f,.97f,1,.16f):new Color(.8f,.94f,1,.78f);main.gravityModifier=vapor?.12f:1;main.maxParticles=400;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,1,1,.15f));
            var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.08f),new GradientAlphaKey(.65f,.6f),new GradientAlphaKey(0,1)});color.color=gradient;
            if(!vapor){var collision=ps.collision;collision.enabled=true;collision.type=ParticleSystemCollisionType.Planes;collision.SetPlane(0,surface);collision.bounce=0;collision.lifetimeLoss=1;collision.dampen=1;}
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            return ps;
        }
        static Transform Phone(Animator actor)
        {
            const string modelPath="Assets/TruckDockDiving/PrivateLicensedProps/MobilePhone.FBX",materialPath=Folder+"/Spectator phone graphite.mat";
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);if(model==null)throw new InvalidOperationException("Run Tools/TruckSports/ImportDockPhoneAsset.py first; preserve licensed phone source privately.");
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(.025f,.031f,.04f));material.SetFloat("_Smoothness",.7f);AssetDatabase.CreateAsset(material,materialPath);}
            var root=new GameObject("Owned HQ spectator camera phone").transform;root.SetParent(actor.transform,false);var phone=UnityEngine.Object.Instantiate(model,root,false);
            var renderers=phone.GetComponentsInChildren<Renderer>(true);var bounds=renderers[0].bounds;foreach(var r in renderers){bounds.Encapsulate(r.bounds);r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;}
            phone.transform.localScale=Vector3.Scale(phone.transform.localScale,new Vector3(.075f/bounds.size.x,.15f/bounds.size.y,.012f/bounds.size.z));
            bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);phone.transform.position+=root.position-bounds.center;
            foreach(var collider in phone.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
            root.gameObject.SetActive(false);return root;
        }
        static void Refine(DockPoolPresentation presentation)
        {
            if(presentation.authoredVersion>=4)return;
            if(presentation.authoredVersion<2)
            {
            var platform=GameObject.Find("Solid approach service apron");
            if(platform!=null&&platform.GetComponent<MeshFilter>().sharedMesh.name=="Cube")platform.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckDockDiving/Materials/Pool wall without duplicate coping face.asset");
            presentation.surface.GetComponent<Renderer>().sharedMaterial.mainTextureScale=new Vector2(3,16);
            EditorUtility.SetDirty(presentation.surface.GetComponent<Renderer>().sharedMaterial);
            const string path=Folder+"/Impact water sheet.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null)
            {
                material=new Material(Shader.Find("Universal Render Pipeline/Particles/Lit"));material.SetColor("_BaseColor",Color.white);material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);material.SetFloat("_Cull",0);material.SetFloat("_Smoothness",.9f);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.SetOverrideTag("RenderType","Transparent");material.renderQueue=(int)RenderQueue.Transparent;AssetDatabase.CreateAsset(material,path);
            }
            var sheet=new GameObject("Gravity-driven displaced water crown");sheet.transform.SetParent(presentation.match.transform,false);presentation.crownSheet=sheet.AddComponent<MeshFilter>();var renderer=sheet.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;
            }
            if(presentation.authoredVersion<3)
            {
            var sheetMaterial=presentation.crownSheet.GetComponent<Renderer>().sharedMaterial;
            sheetMaterial.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RiverModeler/Textures/Splash_AlbedoAlpha.png"));sheetMaterial.SetFloat("_Smoothness",.4f);sheetMaterial.SetFloat("_BlendModePreserveSpecular",0);sheetMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");sheetMaterial.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);EditorUtility.SetDirty(sheetMaterial);
            // Keep the original simulation/lifecycle, but remove its round
            // billboard orb from the new layered impact presentation.
            presentation.match.splash.GetComponent<ParticleSystemRenderer>().enabled=false;
            var font=presentation.match.menus.header.font;const string letteringPath=Folder+"/Depth tested pool distance lettering.mat";
            var lettering=AssetDatabase.LoadAssetAtPath<Material>(letteringPath);
            if(lettering==null){lettering=new Material(font.material);lettering.shader=Shader.Find("TextMeshPro/Distance Field");lettering.SetFloat("unity_GUIZTestMode",4);lettering.renderQueue=3000;AssetDatabase.CreateAsset(lettering,letteringPath);}
            foreach(var label in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Distance ")))
            {
                label.GetComponent<Renderer>().enabled=false;
                var g=new GameObject("Depth-tested pool distance");g.transform.SetParent(label.transform,false);var text=g.AddComponent<TextMeshPro>();text.font=font;text.fontSharedMaterial=lettering;text.text=label.text;text.fontSize=7;text.color=new Color(.025f,.065f,.1f);text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(4,1);text.overflowMode=TextOverflowModes.Overflow;text.isOverlay=false;text.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
            }
            foreach(var ps in presentation.interaction.spray)ps.GetComponent<ParticleSystemRenderer>().enabled=false;
            var droplets=presentation.droplets.GetComponent<ParticleSystemRenderer>();droplets.renderMode=ParticleSystemRenderMode.Stretch;droplets.velocityScale=.07f;droplets.lengthScale=1.5f;
            presentation.authoredVersion=4;EditorUtility.SetDirty(presentation);
        }
        [MenuItem("Truck Sports/Dock Diving/Upgrade calm pool and lively spectators")]
        public static void Upgrade()
        {
            var scene=EditorSceneManager.OpenScene(DockAuthoring.Scene);var match=UnityEngine.Object.FindAnyObjectByType<DockMatch>();
            var existing=match.GetComponent<DockPoolPresentation>();
            if(existing!=null)
            {
                if(existing.authoredVersion<4){Refine(existing);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
                Debug.Log("DOCK_POOL_LIFE_PRESERVED saved owner customization.");return;
            }
            var presence=match.GetComponent<DockAudiencePresentation>();if(presence==null||presence.authoredVersion!=4)throw new InvalidOperationException("Preserve and extend the accepted owned Dock venue.");
            var water=GameObject.Find("NWH water plane · URP pool surface");
            var legacy=water.GetComponent<LWS.TruckFishing.FishingWaterFlow>();if(legacy!=null)UnityEngine.Object.DestroyImmediate(legacy);
            var presentation=match.gameObject.AddComponent<DockPoolPresentation>();presentation.match=match;presentation.interaction=match.GetComponent<SportsWaterInteraction>();presentation.surface=water.GetComponent<MeshFilter>();
            string meshPath=Folder+"/Calm pool response grid.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(mesh==null)
            {
                const int columns=32,rows=192;var vertices=new Vector3[(columns+1)*(rows+1)];var uv=new Vector2[vertices.Length];var triangles=new int[columns*rows*6];
                for(int z=0;z<=rows;z++)for(int x=0;x<=columns;x++){int i=z*(columns+1)+x;vertices[i]=new Vector3(x*10f/columns-5,0,z*10f/rows-5);uv[i]=new Vector2((float)x/columns,(float)z/rows);}
                int at=0;for(int z=0;z<rows;z++)for(int x=0;x<columns;x++){int i=z*(columns+1)+x;triangles[at++]=i;triangles[at++]=i+columns+1;triangles[at++]=i+1;triangles[at++]=i+1;triangles[at++]=i+columns+1;triangles[at++]=i+columns+2;}
                mesh=new Mesh{name="Owned bounded pool wave grid"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,meshPath);
            }
            presentation.surface.sharedMesh=mesh;
            string materialPath=Folder+"/Calm pool water.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(water.GetComponent<Renderer>().sharedMaterial);material.name="Calm pool water";material.SetFloat("_BumpScale",.1f);material.SetTextureScale("_BumpMap",new Vector2(3,16));material.SetColor("_BaseColor",new Color(.045f,.39f,.54f));AssetDatabase.CreateAsset(material,materialPath);}
            water.GetComponent<Renderer>().sharedMaterial=material;
            var fxroot=new GameObject("Pooled cannonball splash · actual recorded water entry").transform;fxroot.SetParent(match.transform,false);
            var spray=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckSports/Rendering/River splash alpha.mat");
            presentation.crown=Spray(fxroot,"Impact crown sheets",spray,false,water.transform);presentation.droplets=Spray(fxroot,"Gravity-driven droplets",spray,false,water.transform);presentation.mist=Spray(fxroot,"Thin impact mist",spray,true,water.transform);
            var platform=new GameObject("Finished right-hand approach platform").transform;platform.SetParent(match.transform,false);
            var navy=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckDockDiving/Materials/Stadium navy.mat");var ivory=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckDockDiving/Materials/Distance ivory.mat");var ceramic=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckDockDiving/Materials/Pool ceramic.mat");
            Box(platform,"Solid approach service apron",new Vector3(9.85f,1.1f,-15),new Vector3(12.3f,2.2f,30),navy);
            Box(platform,"Closed near pool ceramic wall",new Vector3(0,-1.6f,-.3f),new Vector3(22.8f,3.2f,.6f),ceramic);
            const string wood="Assets/ConstructionPropsBundle/URP/Prefabs/SM_Planks01 Variant.prefab",fence="Assets/JC_LP_MegaCity/Prefabs/FloorProps/SM_FloorProps_Fence_01.prefab";
            for(int x=0;x<2;x++)for(int z=0;z<13;z++)FitModel(platform,"Owned timber service-platform decking",wood,new Vector3(6.78f+x*6.15f,2.075f,-28.8f+z*2.3f),new Vector3(6.12f,.12f,2.32f));
            for(int z=0;z<7;z++)FitModel(platform,"Owned outer platform safety fence",fence,new Vector3(16.08f,2.2f,-27+z*4.25f),new Vector3(4.2f,1,.16f),90);
            Box(platform,"Near-edge pool coping cap",new Vector3(9.85f,2.205f,-.1f),new Vector3(12.3f,.03f,.18f),ivory);
            var audit=new StringBuilder("Owned UTS Full Pack2.0 compatible generic Bip01 spectator clips; no vendor source changed. Cached owned humanoid/locomotion packs inspected; incompatible rigs are not blindly retargeted.\n");
            for(int i=0;i<presence.variants.Length;i++)
            {
                var variant=presence.variants[i];var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UTS_FullPack/Models/People/Prefabs/"+DockVenueAssetAudit.Characters[i]+".prefab");string source=AssetDatabase.GetAssetPath(prefab.GetComponentInChildren<Animator>(true).runtimeAnimatorController);
                string directory="Assets/UTS_FullPack/Models/People/Animations/"+(source.Contains("Girl with Heel")?"Girl with Heel":source.Contains("Girl no Heel")?"Girl no Heel":"man");
                var clips=Clips.Select(c=>Clip(directory,c)).ToArray();variant.ambientClips=new[]{clips[0],clips[1],clips[2],clips[3],clips[4],clips[5],clips[6],clips[7]};
                string path=Folder+"/Lively audience "+i+".controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                if(controller==null){controller=AnimatorController.CreateAnimatorControllerAtPath(path);var machine=controller.layers[0].stateMachine;for(int j=0;j<States.Length;j++){var state=machine.AddState(States[j]);state.motion=clips[j];if(j==0)machine.defaultState=state;}}
                foreach(var actor in presence.crowd.nearFans.Where(a=>a.runtimeAnimatorController==variant.animator.runtimeAnimatorController))
                {
                    actor.runtimeAnimatorController=controller;var life=actor.gameObject.AddComponent<DockSpectatorLife>();life.match=match;life.crowd=presence.crowd;life.actor=actor;life.anchor=actor.transform;life.variety=Array.IndexOf(presence.crowd.nearFans,actor);life.phone=Phone(actor);
                }
                audit.AppendLine(variant.sourceName+" · "+directory+" · "+string.Join(", ",clips.Select(c=>c.name)));
            }
            Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair");File.WriteAllText("Logs/TruckSports/PlaytestRepair/dock-pool-life-owned-assets.txt",audit.ToString());
            Refine(presentation);EditorUtility.SetDirty(presence);EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
    }
}
