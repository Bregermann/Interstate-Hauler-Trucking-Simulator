using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
namespace LWS.TruckSports.Tests {
public sealed class PresentationTests {
 [Test]public void EveryRegisteredSportHasDistinctImportedTransparentArtwork(){
  var type=Type.GetType("LWS.TruckSports.SportsNavigation, Assembly-CSharp",true);var scenes=type.GetFields(BindingFlags.Public|BindingFlags.Static).Where(f=>f.IsLiteral&&f.FieldType==typeof(string)&&f.Name!="Front").Select(f=>(string)f.GetRawConstantValue()).Where(s=>s.StartsWith("Truck")).Distinct().ToArray();Assert.That(scenes.Length,Is.GreaterThanOrEqualTo(41));var guids=scenes.Select(scene=>{string path="Assets/TruckSports/Art/Logos/"+scene+".png";var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);Assert.That(sprite,Is.Not.Null,scene);var importer=(TextureImporter)AssetImporter.GetAtPath(path);Assert.That(importer.alphaIsTransparency,Is.True,scene);Assert.That(importer.mipmapEnabled,Is.False,scene);Assert.That(importer.maxTextureSize,Is.EqualTo(1024),scene);return AssetDatabase.AssetPathToGUID(path);}).ToArray();Assert.That(guids.Distinct().Count(),Is.EqualTo(scenes.Length));
 }
 [Test]public void DockBubbleStaysInsideEachCameraViewport(){
  var type=Type.GetType("LWS.TruckDockDiving.DockSlang, Assembly-CSharp",true);var method=type.GetMethod("PositionInViewport");var canvas=new Rect(-960,-540,1920,1080);foreach(var viewport in new[]{new Rect(0,0,1,1),new Rect(0,.5f,.5f,.5f),new Rect(.5f,0,.5f,.5f)})foreach(var p in new[]{new Vector2(-2,3),new Vector2(2,-1),new Vector2(.5f,.5f)}){var result=(Vector2)method.Invoke(null,new object[]{canvas,viewport,new Vector2(250,108),p});var area=new Rect(canvas.xMin+viewport.x*canvas.width,canvas.yMin+viewport.y*canvas.height,viewport.width*canvas.width,viewport.height*canvas.height);Assert.That(area.Contains(result),Is.True);Assert.That(result.x-125,Is.GreaterThanOrEqualTo(area.xMin));Assert.That(result.x+125,Is.LessThanOrEqualTo(area.xMax));Assert.That(result.y-54,Is.GreaterThanOrEqualTo(area.yMin));Assert.That(result.y+54,Is.LessThanOrEqualTo(area.yMax));}
 }
}
}
