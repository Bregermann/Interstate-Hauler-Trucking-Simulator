using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace LWS.TruckSports.Tests
{
    public sealed class LavaSceneTests
    {
        SceneSetup[] previous;Component match;Array floors;
        [SetUp]public void Open()
        {
            previous=EditorSceneManager.GetSceneManagerSetup();var scene=EditorSceneManager.OpenScene("Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity");
            var type=Type.GetType("LWS.TruckFloorIsLava.LavaMatch, Assembly-CSharp",true);
            foreach(var root in scene.GetRootGameObjects()){match=root.GetComponent(type);if(match!=null)break;}
            Assert.That(match,Is.Not.Null);floors=(Array)type.GetField("platforms").GetValue(match);
        }
        [TearDown]public void Restore(){if(previous!=null&&previous.Length>0&&Array.TrueForAll(previous,p=>!string.IsNullOrEmpty(p.path)))EditorSceneManager.RestoreSceneManagerSetup(previous);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
        object Field(object instance,string field)=>instance.GetType().GetField(field).GetValue(instance);
        [Test]public void EveryFiniteSlabHasMatchingTopCollisionAndWeatheradeMetadata()
        {
            Assert.That(floors.Length,Is.EqualTo(25));var kinds=new HashSet<string>();var bodies=new HashSet<Rigidbody>();
            foreach(Component floor in floors)
            {
                var body=(Rigidbody)Field(floor,"body");Assert.That(bodies.Add(body),Is.True);Assert.That(body.isKinematic,Is.True);
                var collider=(BoxCollider)Field(floor,"surface");var renderer=(Renderer)Field(floor,"plate");
                Assert.That(renderer.gameObject,Is.SameAs(collider.gameObject),"Existing Weatherade root binding must resolve the visible slab, not a separate child.");
                Assert.That(collider.isTrigger,Is.False);Assert.That(collider.center.y+collider.size.y*.5f,Is.EqualTo(0).Within(.001f));
                Assert.That(renderer.GetComponent<MeshFilter>().sharedMesh.bounds.max.y,Is.EqualTo(0).Within(.001f));
                Assert.That(renderer.GetComponent(Type.GetType("LWS.TruckTaxi.TruckTaxiSurface, LWS.TruckTaxi.Runtime",true)),Is.Not.Null);
                kinds.Add(Field(floor,"kind").ToString());
            }
            Assert.That(kinds.Count,Is.EqualTo(7));
        }
        [Test]public void SnowAndLavaUseTheExistingHdrGradeOnEveryGameplayCamera()
        {
            var dataType=Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime",true);
            var eventCameraType=Type.GetType("LWS.TruckSports.SportsEventCamera, Assembly-CSharp",true);int count=0;
            foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))if(camera.gameObject.scene==match.gameObject.scene&&camera.GetComponent(eventCameraType)!=null)
            {
                var data=camera.GetComponent(dataType);Assert.That(data,Is.Not.Null,"The authored HDR grade cannot run without a URP camera configuration.");
                Assert.That((bool)dataType.GetProperty("renderPostProcessing").GetValue(data),Is.True);Assert.That(camera.allowHDR,Is.True);count++;
            }
            Assert.That(count,Is.EqualTo(5));Assert.That(GameObject.Find("Reused volcanic HDR grade"),Is.Not.Null);
        }
        [Test]public void AuthoredJumpEdgesJoinAdjacentReachableSlabsOnly()
        {
            var from=(int[])Field(match,"edgeFrom");var to=(int[])Field(match,"edgeTo");Assert.That(from.Length,Is.EqualTo(40));Assert.That(to.Length,Is.EqualTo(from.Length));
            for(int i=0;i<from.Length;i++)
            {
                var a=(Component)floors.GetValue(from[i]);var b=(Component)floors.GetValue(to[i]);var delta=b.transform.position-a.transform.position;
                Assert.That(delta.magnitude,Is.EqualTo(48).Within(.001f));Assert.That(Mathf.Abs(delta.y),Is.LessThan(.001f));
                var sizeA=(Vector2)Field(a,"halfSize");var sizeB=(Vector2)Field(b,"halfSize");bool x=Mathf.Abs(delta.x)>1;
                float gap=48-(x?sizeA.x+sizeB.x:sizeA.y+sizeB.y);Assert.That(gap,Is.InRange(2f,11f));
            }
        }
    }
}
