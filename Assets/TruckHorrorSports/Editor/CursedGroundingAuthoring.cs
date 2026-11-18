#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
namespace LWS.TruckHorrorSports.Editor
{
    public static class CursedGroundingAuthoring
    {
        const string RootName="Exorcism physical grounding posts and cables";
        const string Folder="TruckHorrorSports/CursedCargo";
        const string Sign="Assets/RoadPropsPack/URP/Prefabs/RoadSign_URP.prefab";
        [MenuItem("Truck Sports/Alpha 0.10/Upgrade active Cursed exorcism grounding")]
        public static void UpgradeActiveVenue()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before this explicit saved-scene upgrade.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=HorrorAuthoring.Scene(HorrorSport.CursedCargo))throw new InvalidOperationException("Open the saved Cursed Cargo venue.");
            var match=UnityEngine.Object.FindObjectsByType<HorrorMatch>(FindObjectsSortMode.None).Single(m=>m.gameObject.scene==scene);
            var serialized=new SerializedObject(match);var binding=serialized.FindProperty("groundingRitual");if(binding==null)throw new InvalidOperationException("Integrate the documented groundingRitual hooks first.");
            var existing=scene.GetRootGameObjects().FirstOrDefault(g=>g.name==RootName);
            if(existing!=null){if(binding.objectReferenceValue!=existing.GetComponent<CursedGroundingRitual>())throw new InvalidOperationException("Preserve and repair the existing edited grounding binding.");Debug.Log("CURSED_GROUNDING_PRESERVED");return;}
            Need<GameObject>(Sign);var theme=Need<SportsBroadcastTheme>("Assets/TruckSports/Resources/SportsBroadcastTheme.asset");
            var connect=Need<AudioClip>("Assets/NWH/Vehicle Physics 2/Sound/Suspension/SuspensionThump.wav");var release=Need<AudioClip>("Assets/WeatherMaker/Prefab/Sound/Ambient/WeatherMakerAmbientSound_EnergyHum.ogg");
            if(match.objectives.Length!=3||match.cargo.Length<4||match.cargo.Any(c=>c==null||c.trailer.Kingpin==null))throw new InvalidOperationException("Reuse the original three wards and four original native trailer kingpins.");
            var root=new GameObject(RootName);var helper=root.AddComponent<CursedGroundingRitual>();helper.match=match;helper.geometry=Child(root.transform,"Option2 physical grounding equipment",Vector3.zero).gameObject;
            helper.posts=new Transform[3];helper.sockets=new AudioSource[3];helper.leads=new LineRenderer[4];helper.connectSound=connect;helper.releaseSound=release;
            for(int ward=0;ward<3;ward++)
            {
                Vector3 goal=match.objectives[ward].transform.position;Vector3 p=goal+Vector3.right*(goal.x<0?5:-5);p.y=0;
                var fixture=Child(helper.geometry.transform,"Ward "+(ward+1)+" owned grounding post",p);
                LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(fixture,Sign,new Vector3(1.2f,3,1),0,false,Folder);
                // The post remains beside the original cab interaction zone; no road,
                // warehouse, collider, start, hitch or scoring target is moved.
                helper.posts[ward]=Child(fixture,"Physical cable world anchor",p+Vector3.up*1.7f);
                var label=Child(fixture,"Grounding instructions",p+Vector3.up*3.3f).gameObject.AddComponent<TextMeshPro>();label.font=theme.headingFont;label.fontSize=1.8f;label.text="WARD "+(ward+1)+"\nSTOP · HOLD ACTION\nGROUND THE ORIGINAL LOAD";label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(12,4);label.richText=false;label.color=new Color(.9f,.78f,.35f);
                var socket=fixture.gameObject.AddComponent<AudioSource>();socket.playOnAwake=false;socket.spatialBlend=1;socket.minDistance=2;socket.maxDistance=35;socket.rolloffMode=AudioRolloffMode.Linear;helper.sockets[ward]=socket;
                var light=Child(fixture,"Grounding warning lamp",p+Vector3.up*2.6f).gameObject.AddComponent<Light>();light.color=new Color(.85f,.6f,.15f);light.range=8;light.intensity=1.5f;
            }
            var cableMaterial=SportsEventAuthoring.Mat(Folder,"Exorcism grounding cable jacket",new Color(.85f,.58f,.12f),.35f);
            for(int slot=0;slot<4;slot++)
            {var line=Child(helper.geometry.transform,"Participant "+(slot+1)+" actual ground lead",Vector3.zero).gameObject.AddComponent<LineRenderer>();line.sharedMaterial=cableMaterial;line.useWorldSpace=true;line.positionCount=9;line.startWidth=line.endWidth=.08f;line.numCornerVertices=2;line.numCapVertices=2;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;line.enabled=false;helper.leads[slot]=line;}
            helper.geometry.SetActive(false);binding.objectReferenceValue=helper;serialized.ApplyModifiedPropertiesWithoutUndo();Undo.RegisterCreatedObjectUndo(root,"Add exorcism grounding equipment");EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("CURSED_GROUNDING_AUTHORED option2 equipment only; original goals/colliders/rigs untouched. Caller saves and performs focused natural acceptance.");
        }
        static Transform Child(Transform parent,string name,Vector3 world){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=world;return go.transform;}
        static T Need<T>(string path)where T:UnityEngine.Object{var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset==null)throw new InvalidOperationException("Existing owned grounding asset missing: "+path);return asset;}
    }
}
#endif
