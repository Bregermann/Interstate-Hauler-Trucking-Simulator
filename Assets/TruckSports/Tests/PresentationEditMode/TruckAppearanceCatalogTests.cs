#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class TruckAppearanceCatalogTests
{
    static Type CatalogType => Type.GetType("LWS.TruckSports.SportsTruckAppearanceCatalog, Assembly-CSharp", true);
    static object Field(object owner, string name) => owner.GetType().GetField(name).GetValue(owner);
    static object Catalog()
    {
        var value = Resources.Load("SportsTruckAppearanceCatalog", CatalogType);
        Assert.That(value, Is.Not.Null, "Explicit owned content authoring must create the real saved catalog before acceptance."); return value;
    }
    [Test]
    public void SavedCatalogHasSevenDistinctActualBodiesAndEveryRequestedCosmeticCategory()
    {
        var value = Catalog(); object[] error = { null };
        Assert.That(CatalogType.GetMethod("Validate").Invoke(value, error), Is.True, error[0] as string);
        var bodies = ((IEnumerable)Field(value, "bodies")).Cast<object>().ToArray();
        CollectionAssert.AreEquivalent(new[] { "semi.standard", "truck.monster", "semi.lowrider", "truck.lifted", "truck.fire", "novelty.hotdog", "novelty.icecream" }, bodies.Select(x => (string)Field(x, "id")));
        Assert.That(bodies.Select(x => AssetDatabase.GetAssetPath((GameObject)Field(x,"visualPrefab"))).Distinct().Count(), Is.EqualTo(7));
        var items = ((IEnumerable)Field(value,"cosmetics")).Cast<object>().ToArray(); Assert.That(items.Length, Is.GreaterThanOrEqualTo(27));
        foreach(string slot in new[]{"hat","face.eyes","face.brows","face.mouth","face.mustache","face.lashes","mirrors","wheels","rear.ornament","dashboard"})
            Assert.That(items.Any(x => (string)Field(x,"slotId")==slot), Is.True, slot+" must contain actual equipable content.");
        Assert.That(items.Count(x=>(string)Field(x,"slotId")=="hat"), Is.GreaterThanOrEqualTo(10));
        foreach(var entry in bodies.Concat(items))
        {
            var prefab=(GameObject)Field(entry,"visualPrefab");var sprite=(Sprite)Field(entry,"thumbnail");
            Assert.That(prefab,Is.Not.Null);Assert.That(sprite,Is.Not.Null);Assert.That(sprite.texture.width,Is.EqualTo(384));Assert.That(sprite.texture.height,Is.EqualTo(256));
            foreach(var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            { Assert.That(renderer.sharedMaterials.All(m=>m!=null&&m.shader!=null),Is.True,renderer.name+" needs actual imported materials."); }
            Assert.That(prefab.GetComponentsInChildren<Rigidbody>(true),Is.Empty);Assert.That(prefab.GetComponentsInChildren<Collider>(true),Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<Camera>(true),Is.Empty);Assert.That(prefab.GetComponentsInChildren<MonoBehaviour>(true),Is.Empty);
            foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                Assert.That(filter.sharedMesh,Is.Not.Null);
                Assert.That(filter.sharedMesh.vertices.All(p=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z)),Is.True,"No invalid procedural cap vertices: "+filter.name);
                var bounds=filter.sharedMesh.bounds;
                Assert.That(float.IsFinite(bounds.size.sqrMagnitude)&&float.IsFinite(bounds.center.sqrMagnitude),Is.True,"No invalid saved bounds: "+filter.name);
            }
            var image=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                Assert.That(image.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite))),Is.True);
                var pixels=image.GetPixels32();var background=pixels[0];
                Assert.That(pixels.Count(pixel=>Math.Abs(pixel.r-background.r)+Math.Abs(pixel.g-background.g)+Math.Abs(pixel.b-background.b)>20),Is.GreaterThan(32),"Actual shop art must contain visible geometry: "+Field(entry,"id"));
            }
            finally{UnityEngine.Object.DestroyImmediate(image);}
        }
    }
    [Test]
    public void UnavailableContentUsesASeparateFallbackAndNeverRewritesCatalogOrRequestedIds()
    {
        var value=Catalog();var original=JsonUtility.ToJson((UnityEngine.Object)value);string requested="future.content.owner-choice";
        var resolved=CatalogType.GetMethod("ResolveBody").Invoke(value,new object[]{requested});Assert.That(Field(resolved,"id"),Is.EqualTo("semi.standard"));
        Assert.That(CatalogType.GetMethod("FindCosmetic").Invoke(value,new object[]{requested,"hat","semi.standard"}),Is.Null);
        Assert.That(JsonUtility.ToJson((UnityEngine.Object)value),Is.EqualTo(original));Assert.That(requested,Is.EqualTo("future.content.owner-choice"));
    }
    [Test]
    public void PhysicsOrScriptsCannotBeSmuggledIntoAPresentationPrefab()
    {
        var clone=UnityEngine.Object.Instantiate((UnityEngine.Object)Catalog());GameObject root=null;
        try
        {
            var first=((IEnumerable)Field(clone,"bodies")).Cast<object>().First();root=UnityEngine.Object.Instantiate((GameObject)Field(first,"visualPrefab"));first.GetType().GetField("visualPrefab").SetValue(first,root);
            root.AddComponent<Rigidbody>();object[] error={null};Assert.That(CatalogType.GetMethod("Validate").Invoke(clone,error),Is.False);StringAssert.Contains("Rigidbody",(string)error[0]);
        }
        finally{if(root!=null)UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(clone);}
    }
}
#endif
