using System;using System.Linq;using UnityEngine;using UnityEditor;using LWS.TruckSports;using LWS.TruckSports.Editor;
namespace LWS.TruckKeepaway.Editor {
public static class KeepawayRepairAuthoring {
 public static void Install(KeepawayMatch m){m.cabVariant=m.gameObject.scene.name=="TruckKeepawayFirstPerson";foreach(var sh in m.chaosField.sheep){SheepMotionAuthoring.Install(sh.gameObject,sh.transform);var motion=sh.GetComponent<SportsSheepMotion>();var model=motion.animator.gameObject;var clip=motion.animator.runtimeAnimatorController.animationClips.First(c=>c.name.Contains("locomotion"));clip.SampleAnimation(model,0);var skin=sh.GetComponentInChildren<SkinnedMeshRenderer>(true);var baked=new Mesh();skin.BakeMesh(baked,true);float low=float.PositiveInfinity;foreach(var v in baked.vertices)low=Mathf.Min(low,sh.transform.InverseTransformPoint(skin.transform.TransformPoint(v)).y);UnityEngine.Object.DestroyImmediate(baked);model.transform.localPosition-=Vector3.up*(low+.6f);sh.rootToFeet=.6f;var shape=sh.GetComponent<SphereCollider>();shape.radius=.95f;shape.center=new Vector3(0,.35f,0);EditorUtility.SetDirty(sh);}m.broadcast.view.rect=new Rect(0,0,1,1);foreach(var view in m.cabViews)view.view.enabled=false;m.broadcast.view.enabled=true;KeepawayHudAuthoring.Install(m);EditorUtility.SetDirty(m);}
}
}

