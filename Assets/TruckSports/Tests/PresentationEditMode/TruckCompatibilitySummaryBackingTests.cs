#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class TruckCompatibilitySummaryBackingTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo f?f.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    [Test]public void OwnedRoundedBackingAddsContrastWithoutChangingOriginalGeometryArtOrTextAndRefusesEdits()
    {
        var previous=EditorSceneManager.GetSceneManagerSetup();GameObject designer=null;Transform created=null;
        try
        {
            var scene=EditorSceneManager.OpenScene("Assets/TruckDemolitionDerby/Scenes/TruckDemolitionDerby.unity",OpenSceneMode.Single);var source=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren(Sports("SportsTruckCompatibilitySource"),true)).Cast<Component>().Single();var presenter=source.GetComponent(Sports("SportsTruckCompatibilityPresenter"));var page=(GameObject)Get(source,"setupPage");var summary=(Component)Get(presenter,"subtitle");var author=Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilitySummaryBackingAuthoring, Assembly-CSharp-Editor",true);string name=(string)author.GetField("BackingName").GetRawConstantValue();var upgrade=author.GetMethod("Upgrade");bool existed=page.transform.Find(name)!=null;var rects=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<RectTransform>(true)).Select(rect=>(rect,rect.anchoredPosition,rect.sizeDelta,rect.anchorMin,rect.anchorMax,rect.pivot,rect.localRotation,rect.localScale)).ToArray();var imageType=Type.GetType("UnityEngine.UI.Image, UnityEngine.UI",true);var images=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren(imageType,true)).Cast<Component>().Select(image=>(image,sprite:Get(image,"sprite"),color:Get(image,"color"),raycast:Get(image,"raycastTarget"))).ToArray();var textType=Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro",true);var texts=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren(textType,true)).Cast<Component>().Select(text=>(text,value:Get(text,"text"))).ToArray();
            Assert.That(upgrade.Invoke(null,new object[]{source}),Is.EqualTo(!existed));var backing=page.transform.Find(name);if(!existed)created=backing;Assert.That(backing,Is.Not.Null);var image=backing.GetComponent(imageType);Assert.That(Get(image,"sprite"),Is.SameAs(author.GetProperty("SharedSprite").GetValue(null)));Assert.That(Get(image,"type").ToString(),Is.EqualTo("Sliced"));Assert.That(Get(image,"raycastTarget"),Is.EqualTo(false));Assert.That(((RectTransform)backing).sizeDelta,Is.EqualTo(new Vector2(1064,50)));Assert.That(((RectTransform)backing).anchoredPosition,Is.EqualTo(((RectTransform)summary.transform).anchoredPosition));Assert.That(backing.GetSiblingIndex(),Is.EqualTo(summary.transform.GetSiblingIndex()-1));Assert.That(upgrade.Invoke(null,new object[]{source}),Is.EqualTo(false),"Established owned backing is idempotent.");
            var oldColor=Get(image,"color");try{image.GetType().GetProperty("color").SetValue(image,Color.magenta);Assert.That(Assert.Throws<TargetInvocationException>(()=>upgrade.Invoke(null,new object[]{source})).InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(Get(image,"color"),Is.EqualTo(Color.magenta));}finally{image.GetType().GetProperty("color").SetValue(image,oldColor);}
            designer=new GameObject("Designer summary backing",typeof(RectTransform),imageType);designer.transform.SetParent(page.transform,false);var custom=(RectTransform)designer.transform;custom.anchorMin=custom.anchorMax=custom.pivot=new Vector2(.5f,.5f);custom.sizeDelta=new Vector2(1040,42);custom.anchoredPosition=((RectTransform)summary.transform).anchoredPosition;Assert.That(Assert.Throws<TargetInvocationException>(()=>upgrade.Invoke(null,new object[]{source})).InnerException,Is.TypeOf<InvalidOperationException>(),"Never replace or cover an unknown designer backing.");UnityEngine.Object.DestroyImmediate(designer);designer=null;
            try{Set(presenter,"subtitle",null);Assert.That(Assert.Throws<TargetInvocationException>(()=>upgrade.Invoke(null,new object[]{source})).InnerException,Is.TypeOf<InvalidOperationException>());}finally{Set(presenter,"subtitle",summary);}foreach(var e in rects){Assert.That(e.rect.anchoredPosition,Is.EqualTo(e.anchoredPosition));Assert.That(e.rect.sizeDelta,Is.EqualTo(e.sizeDelta));Assert.That(e.rect.anchorMin,Is.EqualTo(e.anchorMin));Assert.That(e.rect.anchorMax,Is.EqualTo(e.anchorMax));Assert.That(e.rect.pivot,Is.EqualTo(e.pivot));Assert.That(e.rect.localRotation,Is.EqualTo(e.localRotation));Assert.That(e.rect.localScale,Is.EqualTo(e.localScale));}foreach(var e in images){Assert.That(Get(e.image,"sprite"),Is.SameAs(e.sprite));Assert.That(Get(e.image,"color"),Is.EqualTo(e.color));Assert.That(Get(e.image,"raycastTarget"),Is.EqualTo(e.raycast));}foreach(var e in texts)Assert.That(Get(e.text,"text"),Is.EqualTo(e.value));
        }
        finally{if(designer!=null)UnityEngine.Object.DestroyImmediate(designer);if(created!=null)UnityEngine.Object.DestroyImmediate(created.gameObject);Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilityAuthoring, Assembly-CSharp-Editor",true).GetMethod("RestorePreviousSceneSetup").Invoke(null,new object[]{previous});}
    }
}
#endif
