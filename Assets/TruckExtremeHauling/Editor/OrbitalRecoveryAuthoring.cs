#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckExtremeHauling.Editor
{
    public static class OrbitalRecoveryAuthoring
    {
        [MenuItem("Truck Sports/Alpha 0.10/Install active orbital recovery field")]
        public static void UpgradeActiveScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before authoring orbital hazards.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/TruckExtremeHauling/Scenes/ZeroGHitchUp.unity")throw new InvalidOperationException("Open the saved Zero-G Hitch-Up venue.");
            var match=UnityEngine.Object.FindAnyObjectByType<ZeroGHitchMatch>();
            if(match==null)throw new InvalidOperationException("The original orbital match is required.");
            if(match.recoveryField!=null&&match.recoveryField.asteroids.Length>0)return;
            const string source="Assets/TruckFishing/Prefabs/River boulder 0.prefab";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(source)==null)throw new InvalidOperationException("Existing owned rock art is required; no placeholder replacement.");
            var field=match.GetComponent<OrbitalRecoveryField>();if(field==null)field=match.gameObject.AddComponent<OrbitalRecoveryField>();
            field.asteroids=new Rigidbody[16];
            for(int lane=0;lane<4;lane++)for(int n=0;n<4;n++)
            {
                int index=lane*4+n;float radius=1.5f+n*.35f;
                var root=new GameObject("Physical orbital recovery asteroid "+index).transform;root.SetParent(match.transform,false);
                root.SetPositionAndRotation(new Vector3(lane*80+(n%2==0?-13:13),34+n*3,-15+n*35),Quaternion.Euler(n*17,index*29,n*11));
                LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(root,source,Vector3.one*radius*2,-radius,true,"TruckExtremeHauling");
                var collider=root.gameObject.AddComponent<SphereCollider>();collider.radius=radius*.9f;
                var body=root.gameObject.AddComponent<Rigidbody>();body.mass=12000+n*4000;body.useGravity=false;body.isKinematic=true;
                body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
                field.asteroids[index]=body;root.gameObject.SetActive(false);
            }
            match.recoveryField=field;EditorUtility.SetDirty(field);EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Authored physical orbital recovery debris; save explicitly. Ordinary docking remains unchanged; recovery/relay acceptance is separate.");
        }
    }
}
#endif
