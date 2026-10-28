#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
public sealed class OrchestraVenueTitleGuardTests
{
 static object[] Style(TextMesh t)=>new object[]{t.text,t.font,t.characterSize,t.fontSize,t.fontStyle,t.anchor,t.alignment,t.lineSpacing,t.tabSize,t.richText,t.color,t.offsetZ};
 [Test]public void OnlyExactOriginalWorldTitlePoseAndTypographyPassThenOnlyPositionChanges()
 {
  var type=Type.GetType("LWS.TruckOrchestra.Editor.OrchestraVenueTitleAuthoring, Assembly-CSharp-Editor",true);var check=type.GetMethod("ValidateKnownPose",BindingFlags.Static|BindingFlags.NonPublic);var apply=type.GetMethod("ApplyNewTitlePosition",BindingFlags.Static|BindingFlags.NonPublic);var g=new GameObject("Concert venue original enamel title");var title=g.AddComponent<TextMesh>();
  try
  {
   title.text="THE SIX-TON SYMPHONY";title.characterSize=.26f;title.fontSize=64;title.anchor=TextAnchor.MiddleCenter;title.alignment=TextAlignment.Center;title.fontStyle=FontStyle.Normal;title.richText=true;title.lineSpacing=1;title.color=new Color(1,.84f,.45f);g.transform.position=new Vector3(0,10.5f,25);Action validate=()=>check.Invoke(null,new object[]{title});Assert.DoesNotThrow(()=>validate());
   g.transform.position+=Vector3.up*.001f;Assert.That(Assert.Throws<TargetInvocationException>(()=>validate()).InnerException,Is.TypeOf<InvalidOperationException>());g.transform.position=new Vector3(0,10.5f,25);
   title.text="CUSTOM CONCERT";Assert.That(Assert.Throws<TargetInvocationException>(()=>validate()).InnerException,Is.TypeOf<InvalidOperationException>());title.text="THE SIX-TON SYMPHONY";
   title.characterSize=.27f;Assert.That(Assert.Throws<TargetInvocationException>(()=>validate()).InnerException,Is.TypeOf<InvalidOperationException>());title.characterSize=.26f;
   g.transform.rotation=Quaternion.Euler(0,1,0);Assert.That(Assert.Throws<TargetInvocationException>(()=>validate()).InnerException,Is.TypeOf<InvalidOperationException>());g.transform.rotation=Quaternion.identity;
   var style=Style(title);var font=title.font;var material=title.GetComponent<MeshRenderer>().sharedMaterial;apply.Invoke(null,new object[]{title});Assert.That(g.transform.position,Is.EqualTo(new Vector3(0,5.4f,25)));Assert.That(g.transform.localScale,Is.EqualTo(Vector3.one));Assert.That(g.transform.rotation,Is.EqualTo(Quaternion.identity));CollectionAssert.AreEqual(style,Style(title));Assert.That(title.font,Is.EqualTo(font));Assert.That(title.GetComponent<MeshRenderer>().sharedMaterial,Is.EqualTo(material));Assert.That(Assert.Throws<TargetInvocationException>(()=>validate()).InnerException,Is.TypeOf<InvalidOperationException>());
  }
  finally{UnityEngine.Object.DestroyImmediate(g);}
 }
}
#endif
