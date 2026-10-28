#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class ImpossibleVenueLogoTests
{
    static Type Author => Type.GetType("LWS.TruckSports.Editor.SportsImpossibleVenueLogoAuthoring, Assembly-CSharp-Editor", true);
    static bool Fits(Vector3 center, Vector2 size) => (bool)Author.GetMethod("GuessProposalFitsOriginalHeader").Invoke(null,new object[]{center,size});
    static string Hash(string path) { using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))); }
    [Test]
    public void OriginalFixedBoardCameraAndActualAnswerRailRefuseTallCenteredArtButPermitSmallRightHeaderEmblem()
    {
        Assert.That(Fits(new Vector3(0,20.3f,.98f),new Vector2(6,3)),Is.False,"Original proposed top crosses actual fixed-camera frustum.");
        Assert.That(Fits(new Vector3(0,19.3f,.98f),new Vector2(4.8f,2.4f)),Is.False,"Smaller centered art still overlaps the original live answer row/rail.");
        Assert.That(Fits(new Vector3(9.8f,17.2f,.98f),new Vector2(2.4f,1.2f)),Is.True);
        Assert.That(Fits(new Vector3(12,17.2f,.98f),new Vector2(2.4f,1.2f)),Is.False);
        Assert.That(Fits(new Vector3(9.5f,16.2f,.98f),new Vector2(2.4f,1.2f)),Is.False);
    }
    [Test]
    public void ActualV24SavedGlyphMeasurementsLeaveClearReviewedBackdropAndBoardHeaderGaps()
    {
        // Exact stopped-Editor V24 measurements, retained with the failed V22
        // placement. This fixture does not claim a new rendered/native pass.
        var bakeGlyphs=new Rect(-36.334999f,7.3525f,72.67f,9.295f);
        var oldBake=new Rect(-37,7.5f,14,7);var newBake=new Rect(-52.5f,7.5f,14,7);
        Assert.That(oldBake.Overlaps(bakeGlyphs),Is.True);Assert.That(newBake.Overlaps(bakeGlyphs),Is.False);Assert.That(bakeGlyphs.xMin-newBake.xMax,Is.GreaterThan(2));
        var originalBackdrop=new Rect(-90,0,180,16);Assert.That(originalBackdrop.Contains(newBake.min)&&originalBackdrop.Contains(newBake.max),Is.True);
        var guessGlyphs=new Rect(-8.424f,16.3705f,16.848f,1.859f);
        var oldGuess=new Rect(8.3f,16.6f,2.4f,1.2f);var newGuess=new Rect(8.6f,16.6f,2.4f,1.2f);
        Assert.That(oldGuess.Overlaps(guessGlyphs),Is.True);Assert.That(newGuess.Overlaps(guessGlyphs),Is.False);Assert.That(newGuess.xMin-guessGlyphs.xMax,Is.GreaterThan(.15f));Assert.That(newGuess.xMax,Is.LessThan(12.5f));
        Assert.That(Fits(new Vector3(9.8f,17.2f,.98f),new Vector2(2.4f,1.2f)),Is.True,"Actual fixed-camera/answer/header envelope remains unchanged.");
    }
    [TestCase("TruckBakeOff",1)]
    [TestCase("TruckGuessThat",4)]
    public void ExplicitlySavedAdditionsAreOriginalTransparentRenderOnlyAndPreserveTitleObjects(string name,int expected)
    {
        string path="Assets/"+name+"/Scenes/"+name+".unity";Assert.That(File.Exists(path),Is.True);
        var previous=EditorSceneManager.GetSceneManagerSetup();string before=Hash(path);
        try
        {
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var all=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Transform>(true)).ToArray();
            var logos=all.Where(item=>item.name=="Owned Impossible original venue logo").ToArray();Assert.That(logos,Has.Length.EqualTo(expected),"Run only the explicit two-scene author first.");
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TruckSports/Art/Logos/"+name+".png");Assert.That(sprite,Is.Not.Null);
            foreach(var logo in logos)
            {
                Assert.That(logo.GetComponents<Component>(),Has.Length.EqualTo(3));Assert.That(logo.childCount,Is.Zero);Assert.That(logo.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(logo.GetComponentsInChildren<Rigidbody>(true),Is.Empty);
                Assert.That(logo.gameObject.layer,Is.EqualTo(logo.parent.gameObject.layer));var renderer=logo.GetComponent<MeshRenderer>();Assert.That(renderer.shadowCastingMode,Is.EqualTo(ShadowCastingMode.Off));Assert.That(renderer.receiveShadows,Is.False);Assert.That(renderer.sharedMaterial.mainTexture,Is.SameAs(sprite.texture));Assert.That(renderer.sharedMaterial.shader.name,Is.EqualTo("Universal Render Pipeline/Unlit"));Assert.That(renderer.sharedMaterial.GetFloat("_ZWrite"),Is.Zero);Assert.That(renderer.sharedMaterial.renderQueue,Is.EqualTo(3000));
                if(name=="TruckGuessThat"){Assert.That(logo.parent.name,Does.StartWith("Owner ").And.EndWith(" private mechanical board"));Assert.That(Vector3.Distance(logo.localPosition,new Vector3(9.8f,17.2f,.98f)),Is.LessThan(.0001f));Assert.That(Vector3.Distance(logo.lossyScale,new Vector3(2.4f,1.2f,1)),Is.LessThan(.0001f));}
                else {Assert.That(logo.parent.name,Is.EqualTo("Giant television bake-off title backdrop"));Assert.That(Vector3.Distance(logo.position,new Vector3(-45.5f,11,94.98f)),Is.LessThan(.0001f));Assert.That(Vector3.Distance(logo.lossyScale,new Vector3(14,7,1)),Is.LessThan(.0001f));}
            }
            string titleName=name=="TruckBakeOff"?"Original television show title":"Mechanical board title";var titles=all.Where(item=>item.name==titleName).Select(item=>item.GetComponent<TextMesh>()).ToArray();Assert.That(titles,Has.Length.EqualTo(expected));
            foreach(var title in titles){Assert.That(title.text,Is.EqualTo(name=="TruckBakeOff"?"TRUCK BAKE-OFF":"GUESS THAT TRUCK"));Assert.That(title.GetComponent<MeshRenderer>().enabled,Is.True);Assert.That(title.gameObject.activeSelf,Is.True);Assert.That(title.transform.childCount,Is.Zero);}
            Assert.That(scene.isDirty,Is.False);Assert.That(Hash(path),Is.EqualTo(before));
        }
        finally {if(previous.Any(item=>item.isLoaded&&item.isActive&&!string.IsNullOrEmpty(item.path)))EditorSceneManager.RestoreSceneManagerSetup(previous);}
    }
}
#endif
