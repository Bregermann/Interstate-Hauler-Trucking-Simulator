using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckDockDiving.Editor {
public static class DockPoolEdgeAuthoring {
 public static void Upgrade(){EditorSceneManager.OpenScene(DockAuthoring.Scene);Install();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();}
 public static void Install(){const string path="Assets/TruckDockDiving/Materials/Pool wall without duplicate coping face.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);mesh=UnityEngine.Object.Instantiate(primitive.GetComponent<MeshFilter>().sharedMesh);UnityEngine.Object.DestroyImmediate(primitive);mesh.name="Pool wall — sides and base, coping owns top";var source=mesh.triangles;var normals=mesh.normals;var triangles=new List<int>();for(int i=0;i<source.Length;i+=3)if(normals[source[i]].y<.9f){triangles.Add(source[i]);triangles.Add(source[i+1]);triangles.Add(source[i+2]);}mesh.SetTriangles(triangles,0);AssetDatabase.CreateAsset(mesh,path);}
 int count=0;foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include))if(filter.name=="Pool side wall"){filter.sharedMesh=mesh;EditorUtility.SetDirty(filter);count++;var collider=filter.GetComponent<BoxCollider>();if(collider==null||Mathf.Abs(collider.bounds.max.y)>.001f)throw new Exception("Pool physical wall changed");}if(count!=2)throw new Exception("Expected both original pool walls");foreach(int index in mesh.triangles)if(mesh.normals[index].y>.9f)throw new Exception("Duplicate coping top remains");Debug.Log("DOCK_EDGE_VALIDATED two coplanar wall tops removed from rendering; original BoxCollider, water and scoring untouched");
 }
}
}
