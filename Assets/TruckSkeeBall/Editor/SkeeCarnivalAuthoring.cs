using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckSkeeBall.Editor
{
 // Explicit additive authoring. Caller owns opening/saving the scene. The same
 // Install path runs in native tests without any AssetDatabase writes.
 public static class SkeeCarnivalAuthoring
 {
  const string Folder="Assets/TruckSkeeBall/Carnival";
  static readonly List<UnityEngine.Object> temporary=new List<UnityEngine.Object>();
  static bool persist;
  [MenuItem("Truck Sports/Skee Ball/Install worn arcade and physical zero return in open scene")]
  public static void InstallOpenScene()
  {
   var match=UnityEngine.Object.FindAnyObjectByType<SkeeMatch>();
   if(match==null||match.gameObject.scene.path!="Assets/TruckSkeeBall/Scenes/TruckSkeeBall.unity")throw new InvalidOperationException("Open the existing TruckSkeeBall scene first.");
   Install(match,true);
  }
  static T Owned<T>(T value,string name) where T:UnityEngine.Object
  {
   value.name=name;if(!persist){temporary.Add(value);return value;}
   string path=Folder+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old!=null){UnityEngine.Object.DestroyImmediate(value);return old;}
   AssetDatabase.CreateAsset(value,path);return value;
  }
  static Material Material(string source,string name,Color tint,float smoothness)
  {
   var original=AssetDatabase.LoadAssetAtPath<Material>(source);if(original==null)throw new InvalidOperationException("Installed venue material required: "+source);
   var copy=new Material(original);copy.SetColor("_BaseColor",tint);copy.SetFloat("_Smoothness",smoothness);copy.enableInstancing=true;return Owned(copy,name);
  }
  static GameObject Box(Transform parent,string name,Vector3 position,Vector3 size,Material material,PhysicsMaterial contact,bool solid=true)
  {
   var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;var shape=go.GetComponent<BoxCollider>();if(solid)shape.sharedMaterial=contact;else UnityEngine.Object.DestroyImmediate(shape);return go;
  }
  static void Quad(List<Vector3> vertices,List<int> triangles,List<Vector2> uv,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
  {
   int at=vertices.Count;vertices.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});triangles.AddRange(new[]{at,at+1,at+2,at,at+2,at+3,at+2,at+1,at,at+3,at+2,at});
  }
  static Mesh Net(float width,float height,string name)
  {
   var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();const float half=.035f;
   for(float x=0;x<=width+.001f;x+=1.4f)Quad(vertices,triangles,uv,new Vector3(x-half,0,0),new Vector3(x+half,0,0),new Vector3(x+half,height,0),new Vector3(x-half,height,0));
   for(float y=0;y<=height+.001f;y+=1.4f)Quad(vertices,triangles,uv,new Vector3(0,y-half,0),new Vector3(width,y-half,0),new Vector3(width,y+half,0),new Vector3(0,y+half,0));
   var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return Owned(mesh,name);
  }
  static void NetPanel(Transform parent,List<BoxCollider> nets,string name,Vector3 center,Vector2 size,Quaternion rotation,Mesh mesh,Material material,PhysicsMaterial contact)
  {
   var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(BoxCollider));go.transform.SetParent(parent,false);go.transform.localPosition=center;go.transform.localRotation=rotation;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
   // A continuous thin physical backing catches the truck; the visible woven
   // mesh has real gaps and cannot let a wheel slip through a decorative grid.
   var shape=go.GetComponent<BoxCollider>();shape.center=new Vector3(size.x*.5f,size.y*.5f,0);shape.size=new Vector3(size.x,size.y,.16f);shape.sharedMaterial=contact;nets.Add(shape);
  }
  static TextMeshPro Label(Transform parent,string name,string text,Vector3 position,Vector2 size,float fontSize,TMP_FontAsset font,Color color)
  {
   var label=new GameObject(name,typeof(TextMeshPro)).GetComponent<TextMeshPro>();label.transform.SetParent(parent,false);label.transform.localPosition=position;label.rectTransform.sizeDelta=size;label.font=font;label.fontSize=fontSize;label.alignment=TextAlignmentOptions.Center;label.text=text;label.color=color;return label;
  }
  static Mesh Bulbs()
  {
   var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
   for(int row=0;row<2;row++)for(int i=0;i<25;i++)
   {
    var center=new Vector3(i-12,35.65f+row*2.1f,-.67f);float r=.13f;
    Quad(vertices,triangles,uv,center+new Vector3(-r,-r,0),center+new Vector3(r,-r,0),center+new Vector3(r,r,0),center+new Vector3(-r,r,0));
   }
   var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return Owned(mesh,"Bounded arcade marquee bulbs");
  }
  public static SkeeCarnivalVenue Install(SkeeMatch match,bool undo)
  {
   if(match==null||match.courses==null||match.courses.Length!=4)throw new ArgumentException("Four original physical Skee courses required.");
   var existing=match.GetComponentInChildren<SkeeCarnivalVenue>(true);if(existing!=null)return existing;
   persist=!Application.isPlaying;temporary.Clear();if(persist){Directory.CreateDirectory(Folder);AssetDatabase.Refresh();}
   var wood=Material("Assets/RoadPropsPack/URP/Materials/M_WoodBox.mat","Worn coin-op timber",new Color(.72f,.51f,.31f),.12f);
   var metal=Material("Assets/ConstructionPropsBundle/URP/Materials/M_ConstructionProps.mat","Scratched coin-op metal",new Color(.28f,.32f,.31f),.17f);
   var paint=Material("Assets/TruckSkeeBall/Materials/Arcade navy painted metal.mat","Faded arcade navy paint",new Color(.085f,.12f,.14f),.12f);
   var net=Material("Assets/TruckVolleyball/Materials/Dark readable woven net.mat","Skee retaining woven net",new Color(.24f,.19f,.13f),.08f);net.SetInt("_Cull",0);
   var warm=Material("Assets/TruckSkeeBall/Materials/Warm scoring ring lights.mat","Worn arcade warm bulbs",new Color(1,.69f,.3f),.18f);warm.EnableKeyword("_EMISSION");warm.SetColor("_EmissionColor",new Color(1,.46f,.12f));
   Mesh sideMesh=Net(61,49.2f,"Skee side retaining net mesh"),backMesh=Net(32.8f,49.2f,"Skee back retaining net mesh"),roofMesh=Net(32.8f,61,"Skee roof retaining net mesh"),bulbMesh=Bulbs();
   var root=new GameObject("Carnival worn 1988 arcade - physical zero return and retaining nets");root.transform.SetParent(match.transform,false);var venue=root.AddComponent<SkeeCarnivalVenue>();venue.match=match;venue.suppliedReferenceAvailable=false;var nets=new List<BoxCollider>();var returns=new List<BoxCollider>();var bulbs=new List<Renderer>();
   foreach(var runner in match.courses)
   {
    var arena=match.arenas[runner.lane];var font=runner.target.GetComponentsInChildren<TextMeshPro>(true).First().font;var contact=runner.target.GetComponentInChildren<MeshCollider>().sharedMaterial;
    foreach(var renderer in arena.GetComponentsInChildren<MeshRenderer>(true))
    {
     var original=renderer.sharedMaterial;if(original==null)continue;Material replacement=original.name=="Arcade polished maple"?wood:original.name=="Arcade navy painted metal"?paint:null;
     if(replacement!=null){if(undo)Undo.RecordObject(renderer,"Age original Skee cabinet");renderer.sharedMaterial=replacement;}
    }
    var physical=new GameObject("Lane "+runner.lane+" zero-point physical return enclosure").transform;physical.SetParent(arena,false);physical.localPosition=new Vector3(runner.Center,0,0);physical.gameObject.AddComponent<SkeeBoard>();
    var tray=Box(physical,"ZERO RETURN - settle for zero points",new Vector3(0,-1.2f,75.5f),new Vector3(32.8f,.5f,61),metal,contact);returns.Add(tray.GetComponent<BoxCollider>());
    NetPanel(physical,nets,"Physical left woven retaining net",new Vector3(-16.4f,-1.2f,45),new Vector2(61,49.2f),Quaternion.Euler(0,-90,0),sideMesh,net,contact);
    NetPanel(physical,nets,"Physical right woven retaining net",new Vector3(16.4f,-1.2f,106),new Vector2(61,49.2f),Quaternion.Euler(0,90,0),sideMesh,net,contact);
    NetPanel(physical,nets,"Physical rear woven retaining net",new Vector3(-16.4f,-1.2f,106),new Vector2(32.8f,49.2f),Quaternion.identity,backMesh,net,contact);
    NetPanel(physical,nets,"Physical roof woven retaining net",new Vector3(-16.4f,48,45),new Vector2(32.8f,61),Quaternion.Euler(90,0,0),roofMesh,net,contact);
    foreach(int sign in new[]{-1,1}){Box(physical,"Scuffed timber zero-return surround",new Vector3(sign*16.25f,-.6f,75.5f),new Vector3(.3f,1.2f,61),wood,contact);var label=Label(physical,"Actual zero point return label","0  -  MISS RETURN",new Vector3(sign*9,1.1f,47),new Vector2(11,1.6f),7,font,new Color(.92f,.77f,.5f));label.transform.localRotation=Quaternion.Euler(10,0,0);}
    foreach(var ring in runner.rings)
    {
     var label=ring.GetComponentInChildren<TextMeshPro>(true);if(undo)Undo.RecordObjects(new UnityEngine.Object[]{label,label.transform},"Read actual Skee point labels");label.transform.localPosition=new Vector3(0,-ring.radius-.52f,-.3f);label.transform.localScale=Vector3.one;label.rectTransform.sizeDelta=new Vector2(3.3f,1);label.fontSize=6;label.text=ring.points+" PTS";label.color=new Color(.98f,.81f,.48f);
     // Existing recessed collector and rolled rim retain their original meshes,
     // contact surfaces, radius and location. Their dark depth stays visible.
    }
    var oldTitle=runner.target.GetComponentsInChildren<TextMeshPro>(true).First(label=>label.name=="SKEE BALL arcade marquee lettering");if(undo)Undo.RecordObject(oldTitle,"Fade original arcade lettering");oldTitle.text="TRUCK-O-MATIC  /  SKEE BALL";oldTitle.color=new Color(.83f,.46f,.29f);
    Label(runner.target,"Faded 1988 coin-op maker plate","EST. 1988  /  NINE TRUCKS ONE QUARTER",new Vector3(0,38.5f,-.65f),new Vector2(25,1.2f),6,font,new Color(.72f,.65f,.47f));
    var lamps=new GameObject("Fifty bounded warm arcade bulbs",typeof(MeshFilter),typeof(MeshRenderer));lamps.transform.SetParent(runner.target,false);lamps.GetComponent<MeshFilter>().sharedMesh=bulbMesh;lamps.GetComponent<MeshRenderer>().sharedMaterial=warm;bulbs.Add(lamps.GetComponent<MeshRenderer>());
    // Separate small exposed scuffs on the cabinet frame, all outside openings.
    var random=new System.Random(1988+runner.lane);for(int i=0;i<24;i++){float side=i%2==0?-1:1;var scuff=Box(runner.target,"Exposed scratched cabinet metal",new Vector3(side*12.65f,1+(float)random.NextDouble()*33,-.29f),new Vector3(.18f,.07f+(float)random.NextDouble()*.4f,.02f),metal,null,false);scuff.transform.localRotation=Quaternion.Euler(0,0,(float)random.NextDouble()*40-20);}
   }
   venue.zeroReturns=returns.ToArray();venue.retainingNets=nets.ToArray();venue.arcadeBulbs=bulbs.ToArray();if(!persist)venue.transientAssets=temporary.ToArray();
   var pixels=match.gameObject.AddComponent<SkeeArcadePixels>();pixels.match=match;pixels.arcadeShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/TruckSkeeBall/Rendering/SkeeArcadePixels.shader");if(pixels.arcadeShader==null)throw new InvalidOperationException("Owned Skee arcade shader required.");
   if(undo){Undo.RegisterCreatedObjectUndo(root,"Install worn Skee arcade");Undo.RegisterCreatedObjectUndo(pixels,"Install Skee-only pixel presentation");foreach(var runner in match.courses){var enclosure=match.arenas[runner.lane].Find("Lane "+runner.lane+" zero-point physical return enclosure");Undo.RegisterCreatedObjectUndo(enclosure.gameObject,"Install physical Skee catch");}}
   if(persist){UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(match.gameObject.scene);Debug.Log("SKEE_CARNIVAL_AUTHORED additive - caller saves scene. Vintage reference unavailable; installed wood/metal/net foundations reused.");}
   return venue;
  }
 }
}
