using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LWS.TruckCamping;
using LWS.TruckCamping.Editor;
using LWS.TruckMechanicalBull.Editor;
using LWS.TruckSports.Editor;
namespace LWS.TruckPetPals.Editor
{
    public static class PetVisualAuthoring
    {
        public static GameObject Template(PetKind kind)
        {
            Directory.CreateDirectory("Assets/TruckPetPals/Prefabs");AssetDatabase.Refresh();string path="Assets/TruckPetPals/Prefabs/Animated "+kind+" companion art.prefab";var prior=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prior!=null)return prior;
            var root=new GameObject("Expressive animated "+kind+" companion art");
            try
            {
                Transform collar;
                if(kind==PetKind.MiniatureTruck)
                {
                    var art=root.AddComponent<PetTruckArt>();art.assembly=new GameObject("Render-only pet truck assembly").transform;art.assembly.SetParent(root.transform,false);
                    art.rearRibbon=new GameObject("Wagging little rear ribbon").transform;art.rearRibbon.SetParent(art.assembly,false);art.rearRibbon.localPosition=new Vector3(0,.65f,-1.3f);
                    for(int i=0;i<3;i++)CampingWildernessAuthoring.Part(art.rearRibbon,"Tail ribbon segment",new Vector3(0,.12f*i,-i*.18f),new Vector3(.14f,.1f,.28f),CampingWildernessAuthoring.Cloth,Quaternion.Euler(i*12,0,0));
                    art.cuteHorn=root.AddComponent<AudioSource>();art.cuteHorn.playOnAwake=false;art.cuteHorn.spatialBlend=.8f;art.cuteHorn.maxDistance=40;art.cuteHorn.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/NWH/Vehicle Physics 2/Sound/Horn/Horn - Truck1.wav");
                    if(art.cuteHorn.clip==null){string source=AssetDatabase.FindAssets("t:AudioClip Horn",new[]{"Assets/NWH"}).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();if(source==null)throw new InvalidOperationException("Existing actual truck horn required.");art.cuteHorn.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(source);}
                    collar=CampingWildernessAuthoring.Point("Oversized safe pet truck cosmetic collar",art.assembly,new Vector3(0,.75f,.75f));
                }
                else
                {
                    WildernessAssetIntake.ImportReviewedModels();bool human=kind==PetKind.ClothedHuman;
                    GameObject model;Animator animator;
                    if(human)
                    {
                        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UTS_FullPack/Models/People/RagDoll Prefabs/Girl_44.prefab");if(prefab==null)throw new InvalidOperationException("Installed fully clothed adult UTS rig required.");
                        animator=SportsAudienceAuthoring.Spawn(prefab,root.transform,Vector3.zero,Vector3.forward,null,1.7f);model=animator.gameObject;animator.runtimeAnimatorController=null;animator.enabled=false;
                    }
                    else
                    {
                        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(WildernessAssetIntake.Folder+"Dog.fbx");model=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root.transform);animator=model.GetComponentInChildren<Animator>(true);if(animator==null||animator.avatar==null||!animator.avatar.isValid)throw new InvalidOperationException("Actual compatible Generic dog rig/avatar required.");
                        var all=model.GetComponentsInChildren<Transform>(true);var front=(all.Single(t=>t.name=="Bone.014").position+all.Single(t=>t.name=="Bone.017").position)*.5f;var rear=(all.Single(t=>t.name=="Bone.008").position+all.Single(t=>t.name=="Bone.011").position)*.5f;
                        var heading=Vector3.ProjectOnPlane(front-rear,Vector3.up);if(heading.sqrMagnitude<.0001f)throw new InvalidOperationException("Dog anatomical forward axis unavailable.");model.transform.rotation=Quaternion.FromToRotation(heading.normalized,Vector3.forward)*model.transform.rotation;CampingWildernessAuthoring.Fit(model,1.6f,true);
                        animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(WildernessAssetIntake.Folder+"Dog locomotion.controller");ConvertCC0Materials(model);
                    }
                    animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                    var living=root.AddComponent<CampLivingArt>();living.animator=animator;living.model=model.transform;living.biped=human;living.groundSize=human?1.7f:.8f;
                    string[] names=human?new[]{"Bip01 Pelvis","Bip01 Spine","Bip01 Head","Bip01 L UpperArm","Bip01 L Forearm","Bip01 R UpperArm","Bip01 R Forearm","Bip01 L Thigh","Bip01 L Calf","Bip01 R Thigh","Bip01 R Calf"}:new[]{"Bone","Bone.001","Bone.003","Bone.014","Bone.015","Bone.017","Bone.018","Bone.008","Bone.009","Bone.011","Bone.012","Bone.004"};
                    var roles=new[]{CampBone.Pelvis,CampBone.Spine,CampBone.Head,CampBone.FrontLeft,CampBone.FrontLeftLower,CampBone.FrontRight,CampBone.FrontRightLower,CampBone.RearLeft,CampBone.RearLeftLower,CampBone.RearRight,CampBone.RearRightLower,CampBone.Tail};var transforms=model.GetComponentsInChildren<Transform>(true);living.bones=names.Select(n=>transforms.Single(t=>t.name==n)).ToArray();living.roles=roles.Take(names.Length).ToArray();
                    collar=CampingWildernessAuthoring.Point("Fitted animated companion collar",living.bones[2],living.bones[2].position+Vector3.down*(human?.15f:.08f));collar.rotation=root.transform.rotation;
                }
                Accessories(root,collar,kind);CampingWildernessAuthoring.Point("Actual non-graphic leg effect mount",root.transform,new Vector3(-.2f,.35f,-.25f));CampingWildernessAuthoring.Point("Actual grass-kick effect mount",root.transform,new Vector3(0,.08f,-.6f));
                var result=PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();return result;
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        static void ConvertCC0Materials(GameObject model)
        {
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var source=materials[i];if(source==null)throw new InvalidOperationException("CC0 pet surface material missing.");Color color=source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):source.HasProperty("_Color")?source.color:new Color(.55f,.34f,.15f);
                    var owned=ImpossibleBalanceArt.Material("TruckPetPals","Dog original source palette "+source.name,color,.2f);if(source.mainTexture!=null)owned.SetTexture("_BaseMap",source.mainTexture);materials[i]=owned;
                }
                renderer.sharedMaterials=materials;
            }
        }
        static void Accessories(GameObject root,Transform anchor,PetKind kind)
        {
            var accessory=root.AddComponent<PetAccessory>();accessory.ids=new[]{"collar.teal","collar.sun","bandana.rose"};accessory.models=new GameObject[3];float width=kind==PetKind.MiniatureTruck?.8f:kind==PetKind.ClothedHuman?.22f:.28f;
            for(int i=0;i<3;i++)
            {
                var child=new GameObject(accessory.ids[i]).transform;child.SetParent(anchor,false);accessory.models[i]=child.gameObject;
                var color=i==0?new Color(.07f,.61f,.55f):i==1?new Color(.95f,.66f,.13f):new Color(.7f,.18f,.31f);var material=ImpossibleBalanceArt.Material("TruckPetPals","Pet accessory "+accessory.ids[i],color,.3f);
                var mesh=ImpossibleBalanceArt.Mesh("TruckPetPals","Rounded stitched pet collar",()=>ImpossibleBalanceArt.Lathe(new[]{new Vector2(.46f,-.12f),new Vector2(.53f,-.1f),new Vector2(.53f,.1f),new Vector2(.46f,.12f)}));
                ImpossibleBalanceArt.Part(child,"Soft rounded woven collar",mesh,material,Vector3.zero,new Vector3(width*2,.9f*width,width*2));
                var tag=ImpossibleBalanceArt.Part(child,"Engraved little companion tag",CampingWildernessAuthoring.Round,CampingWildernessAuthoring.Stone,new Vector3(0,-width*.22f,width*.53f),new Vector3(width*.3f,width*.3f,width*.07f));
                if(i==2){var band=ImpossibleBalanceArt.Mesh("TruckPetPals","Bound triangular pet bandana",()=>{var m=new Mesh();m.vertices=new[]{new Vector3(-.5f,0,0),new Vector3(.5f,0,0),new Vector3(0,-.65f,.08f),new Vector3(-.5f,0,.025f),new Vector3(.5f,0,.025f),new Vector3(0,-.65f,.105f)};m.uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.zero,Vector2.right,Vector2.up};m.triangles=new[]{0,1,2,3,5,4,0,3,1,1,3,4,1,4,2,2,4,5,2,5,0,0,5,3};return m;});ImpossibleBalanceArt.Part(child,"Tied rose bandana with bound edges",band,material,new Vector3(0,-.03f,width*.5f),Vector3.one*width*1.7f);}
                child.gameObject.SetActive(i==0);
            }
        }
    }
}
