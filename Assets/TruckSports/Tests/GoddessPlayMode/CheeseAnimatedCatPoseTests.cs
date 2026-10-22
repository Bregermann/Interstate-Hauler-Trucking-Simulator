using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class CheeseAnimatedCatPoseTests
{
    [UnityTest] public IEnumerator OwnedCatSkinActuallyDeformsWithoutMovingTheSceneryColliderAndResets()
    {
#if UNITY_EDITOR
        yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/TruckCheeseRolling/Scenes/TruckCheeseRolling.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
        yield return SceneManager.LoadSceneAsync("TruckCheeseRolling");
#endif
        yield return null;
        var type = Type.GetType("LWS.TruckCheeseRolling.CheeseCatPresentation, Assembly-CSharp", true); var cats = UnityEngine.Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None); Assert.That(cats.Length, Is.EqualTo(4));
        var presentation = (Component)cats[0]; var root = presentation.transform; root.gameObject.SetActive(true); var animator = (Animator)type.GetField("animator").GetValue(presentation); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        var collider = root.GetComponent<CapsuleCollider>(); Physics.SyncTransforms(); var position = root.position; var rotation = root.rotation; var bounds = collider.bounds;
        var skin = animator.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r => r.name == "Cat_Body"); var before = new Mesh(); var after = new Mesh();
        try
        {
            int scratch = Animator.StringToHash("Base Layer.Scratch"); Assert.That(animator.HasState(0, scratch), Is.True);
            animator.Play(scratch, 0, .15f); animator.Update(0); skin.BakeMesh(before); animator.Play(scratch, 0, .65f); animator.Update(0); skin.BakeMesh(after);
            var a = before.vertices; var b = after.vertices; Assert.That(a.Length, Is.EqualTo(b.Length)); int changed = a.Where((v, i) => (v - b[i]).sqrMagnitude > .000001f).Count(); Assert.That(changed, Is.GreaterThan(20), "A real vendor skeletal animation must deform the cat mesh, not merely move an outer pivot.");
            Assert.That(root.position, Is.EqualTo(position)); Assert.That(root.rotation, Is.EqualTo(rotation)); Physics.SyncTransforms(); Assert.That(collider.bounds.center, Is.EqualTo(bounds.center)); Assert.That(collider.bounds.size, Is.EqualTo(bounds.size));
            type.GetMethod("ResetPose").Invoke(presentation, new object[] { 8137 }); int idle = Animator.StringToHash("Base Layer.Idle"); Assert.That(animator.GetCurrentAnimatorStateInfo(0).fullPathHash, Is.EqualTo(idle)); float phase = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            type.GetMethod("ResetPose").Invoke(presentation, new object[] { 8137 }); Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.EqualTo(phase).Within(.0001f));
            Debug.Log("CHEESE_CAT_REAL_SKIN_DEFORMATION vertices=" + a.Length + " changed=" + changed + " root/collider unchanged; deterministic pooled idle reset.");
            Time.timeScale = 0; var frozen = skin.bones.Select(t => t.localRotation).ToArray(); yield return new WaitForSecondsRealtime(.15f); for (int i = 0; i < frozen.Length; i++) Assert.That(Quaternion.Angle(frozen[i], skin.bones[i].localRotation), Is.LessThan(.001f));
        }
        finally { Time.timeScale = 1; UnityEngine.Object.Destroy(before); UnityEngine.Object.Destroy(after); }
    }
}
