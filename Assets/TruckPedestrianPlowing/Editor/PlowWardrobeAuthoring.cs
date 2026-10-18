using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using DigitalRuby.WeatherMaker;
using LWS.TruckSports;
using UnityEngine.Rendering.Universal;
namespace LWS.TruckPedestrianPlowing.Editor {
 public static class PlowWardrobeAuthoring {
  static string Root=>PlowAuthoring.Root;
  public static void Install(){
   foreach(var id in AssetDatabase.FindAssets("t:Prefab",new[]{Root+"/Prefabs"})){
    string path=AssetDatabase.GUIDToAssetPath(id);var root=PrefabUtility.LoadPrefabContents(path);
    try{var ped=root.GetComponent<PlowPedestrian>();if(ped==null)continue;Ensure(ped);FitCurrent(ped);CombineHair(ped);PlowCrowdLodAuthoring.Apply(ped);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
   }
   foreach(var path in new[]{PlowAuthoring.Scene,PlowCabAuthoring.Scene}){
    var scene=EditorSceneManager.OpenScene(path);var match=UnityEngine.Object.FindAnyObjectByType<PlowMatch>();InstallCurrent(match);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   }
   AssetDatabase.SaveAssets();Debug.Log("PLOW_WARDROBE_NIGHT_SAVED both existing scenes; no crowd roster changes");
  }
  public static void Ensure(PlowPedestrian ped){
   FitDefaultRobotRig(ped);
   if(ped.GetComponent<PlowBonusEvasion>()==null)ped.gameObject.AddComponent<PlowBonusEvasion>();
   if(ped.GetComponent<PlowRoleCostume>()!=null)return;
   var costume=ped.gameObject.AddComponent<PlowRoleCostume>();
   var original=ped.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r!=ped.vest&&r!=ped.costumeNose&&r.name!="Distance crowd mesh"&&r.GetComponent<TMPro.TMP_Text>()==null).ToArray();
   var head=ped.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Bip01 Head");
   var headModel=original.FirstOrDefault(r=>r.name.Equals("Head",StringComparison.OrdinalIgnoreCase));
   var bounds=headModel!=null?headModel.bounds:new Bounds(head.position,new Vector3(.55f,.65f,.55f));
   var center=bounds.center;var size=bounds.size;
   costume.clothing=original.Where(r=>{string n=r.name.ToLowerInvariant();return n.Contains("sleeve")||n.Contains("pants")||n.Contains("shirt")||n=="body"||n.Contains("shoe");}).Cast<Renderer>().Concat(new[]{ped.vest}).ToArray();
   costume.standard=Group("Standard festival hairstyle",head);costume.bonus=Group("Bonus fabric balaclava and gloves",head);costume.penalty=Group("Penalty red uniform and dealer ponytail",head);
   var black=Mat("Bonus charcoal fabric",new Color(.018f,.022f,.032f),.12f);var red=Mat("Penalty scarlet fabric",new Color(.92f,.035f,.07f),.16f);var gold=Mat("Bonus gold trim",new Color(1,.67f,.05f),.5f);var hair=Mat("Festival purple hair",new Color(.55f,.18f,.85f),.12f);
   // Clothing shell with actual eye openings: does not recolor the underlying head or hands.
   var mask=MeshPart("Fabric ski mask",costume.bonus.transform,Mask(),black);mask.transform.position=center;mask.transform.localScale=Vector3.Scale(size,new Vector3(1.11f,1.08f,1.11f));
   var gloves=new System.Collections.Generic.List<GameObject>();
   foreach(var hand in original.Where(r=>r.name.EndsWith(" hand",StringComparison.OrdinalIgnoreCase)||r.name.Equals("Hand",StringComparison.OrdinalIgnoreCase))){
    var glove=MeshPart("Black fabric glove",hand.transform.parent,hand.GetComponent<MeshFilter>().sharedMesh,black);glove.transform.SetLocalPositionAndRotation(hand.transform.localPosition,hand.transform.localRotation);glove.transform.localScale=hand.transform.localScale*1.045f;
    gloves.Add(glove);glove.SetActive(false);
   }
   for(int side=-1;side<=1;side+=2){var brow=Part("Angled gold mask brow",costume.bonus.transform,PrimitiveType.Cube,gold);brow.transform.position=center+new Vector3(side*size.x*.18f,size.y*.13f,size.z*.52f);brow.transform.localScale=new Vector3(size.x*.28f,.035f,.035f);brow.transform.rotation=Quaternion.Euler(0,0,side*18);}
   // Messy/festival hair gives standard targets a different silhouette without altering their face.
   for(int i=0;i<7;i++){float angle=i*Mathf.PI*2/7;var tuft=Part("Bright festival hair tuft",costume.standard.transform,PrimitiveType.Sphere,hair);tuft.transform.position=center+new Vector3(Mathf.Sin(angle)*size.x*.34f,size.y*.47f+(i%2)*.08f,Mathf.Cos(angle)*size.z*.24f);tuft.transform.localScale=new Vector3(.2f,.23f,.19f);}
   DealerHair(costume.penalty.transform,center+Vector3.up*size.y*.34f,size.y*.9f);
   costume.bonusAttachments=gloves.ToArray();var curves=new System.Collections.Generic.List<GameObject>();
   for(int side=-1;side<=1;side+=2){var curve=Part("Fully clothed red uniform curve",ped.vest.transform.parent,PrimitiveType.Sphere,red);curve.transform.position=new Vector3(side*.2f,.98f,.27f);curve.transform.localScale=new Vector3(.4f,.38f,.24f);curves.Add(curve);curve.SetActive(false);}costume.penaltyAttachments=curves.ToArray();
   costume.pitchfork=Group("Festival pitchfork prop",ped.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Bip01 R Hand"));
   var fork=MeshPart("Blunt festival pitchfork",costume.pitchfork.transform,Fork(),Mat("Pitchfork weathered steel",new Color(.32f,.37f,.42f),.42f));fork.transform.localPosition=new Vector3(.12f,0,0);fork.transform.localRotation=Quaternion.Euler(0,0,-12);
   foreach(var text in new[]{ped.frontBadge,ped.backBadge,ped.beacon}){text.text="";text.gameObject.SetActive(false);}
   if(ped.goldSymbols!=null)foreach(var renderer in ped.goldSymbols.GetComponentsInChildren<Renderer>(true)){renderer.sharedMaterial=gold;renderer.transform.localScale=Vector3.one*1.8f;}
   costume.standard.SetActive(false);costume.bonus.SetActive(false);costume.penalty.SetActive(false);costume.pitchfork.SetActive(false);
   FitCurrent(ped);CombineHair(ped);
   EditorUtility.SetDirty(ped);
  }
  static GameObject Group(string name,Transform parent){var g=new GameObject(name);g.layer=2;g.transform.SetParent(parent,false);return g;}
  static void FitDefaultRobotRig(PlowPedestrian ped){
   if(ped.name!="analytical-robot Parade")return;
   var head=ped.GetComponentsInChildren<MeshRenderer>(true).First(r=>r.name=="Head");if(head.bounds.center.y>=.8f)return;
   // The source robot's half-meter model was attached at world size to a full-size humanoid
   // skeleton. Its animated arms separated from its torso. Normalize this owned default once.
   var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LWS/TruckTaxi/Passengers/Prefabs/analytical-robot.prefab");
   var names=source.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.name).ToHashSet();float scale=1.3f/head.bounds.center.y;
   var old=ped.GetComponent<PlowRoleCostume>();if(old!=null){foreach(var part in new[]{old.standard,old.bonus,old.penalty,old.pitchfork}.Concat(old.bonusAttachments).Concat(old.penaltyAttachments).Distinct())if(part!=null)UnityEngine.Object.DestroyImmediate(part);UnityEngine.Object.DestroyImmediate(old);}
   foreach(var renderer in ped.GetComponentsInChildren<MeshRenderer>(true).Where(r=>names.Contains(r.name)))WorldPose(renderer.transform,renderer.transform.position*scale,renderer.transform.lossyScale*scale,renderer.transform.rotation);
   WorldPose(ped.vest.transform,new Vector3(0,.73f,.015f),new Vector3(.77f,.3f,.49f),Quaternion.identity);
   if(ped.costumeNose!=null)WorldPose(ped.costumeNose.transform,head.bounds.center+new Vector3(0,-.025f,head.bounds.extents.z+.18f),new Vector3(.3f,.3f,.5f),Quaternion.identity);
   Debug.Log("PLOW_ROBOT_RIG_NORMALIZED sport-owned default only, scale="+scale);
  }
  static void FitCurrent(PlowPedestrian ped){
   var costume=ped.GetComponent<PlowRoleCostume>();var gold=Mat("Bonus gold trim",new Color(1,.67f,.05f),.5f);gold.shader=Shader.Find("Universal Render Pipeline/Unlit");gold.SetColor("_BaseColor",new Color(1,.67f,.05f));EditorUtility.SetDirty(gold);
   var head=ped.GetComponentsInChildren<MeshRenderer>(true).First(r=>r.name.Equals("Head",StringComparison.OrdinalIgnoreCase));
   // Respect the proportions of any smaller compatible custom character.
   float costumeFit=Mathf.Clamp(head.bounds.center.y/1.2f,.25f,1);
   if(costumeFit<.8f)WorldPose(ped.vest.transform,new Vector3(0,.73f,.015f)*costumeFit,new Vector3(.77f,.3f,.49f)*costumeFit,Quaternion.identity);
   var mask=costume.bonus.transform.Find("Fabric ski mask");WorldPose(mask,head.bounds.center,Vector3.Scale(head.bounds.size,new Vector3(1.11f,1.08f,1.11f)),Quaternion.identity);
   foreach(var symbol in ped.goldSymbols.GetComponentsInChildren<MeshRenderer>(true)){symbol.sharedMaterial=gold;WorldPose(symbol.transform,new Vector3(0,.74f*costumeFit,Mathf.Sign(symbol.transform.position.z)*.29f*costumeFit),Vector3.one*1.2f*costumeFit,symbol.transform.rotation);}
   foreach(var symbol in ped.stopSymbols.GetComponentsInChildren<MeshRenderer>(true))WorldPose(symbol.transform,new Vector3(0,.73f*costumeFit,Mathf.Sign(symbol.transform.position.z)*.29f*costumeFit),Vector3.one*costumeFit,symbol.transform.rotation);
   if(costumeFit<.8f)foreach(var curve in costume.penaltyAttachments)WorldPose(curve.transform,new Vector3(Mathf.Sign(curve.transform.position.x)*.2f,.98f,.27f)*costumeFit,new Vector3(.4f,.38f,.24f)*costumeFit,Quaternion.identity);
   var valid=new System.Collections.Generic.List<GameObject>();foreach(var glove in costume.bonusAttachments){if(glove.GetComponent<Renderer>().bounds.size.magnitude>.7f)UnityEngine.Object.DestroyImmediate(glove);else valid.Add(glove);}costume.bonusAttachments=valid.ToArray();
   var hand=ped.GetComponentsInChildren<MeshRenderer>(true).First(r=>r.name.Equals("Right hand",StringComparison.OrdinalIgnoreCase));var fork=costume.pitchfork.transform.Find("Blunt festival pitchfork");WorldPose(fork,hand.bounds.center+new Vector3(.08f,-.45f,.05f),Vector3.one,Quaternion.Euler(0,0,-8));
  }
  static void WorldPose(Transform transform,Vector3 position,Vector3 scale,Quaternion rotation){var parent=transform.parent;transform.SetParent(null,true);transform.SetPositionAndRotation(position,rotation);transform.localScale=scale;transform.SetParent(parent,true);}
  static void CombineHair(PlowPedestrian ped){
   var root=ped.GetComponent<PlowRoleCostume>().standard.transform;var tufts=root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name=="Bright festival hair tuft").ToArray();if(tufts.Length==0)return;
   string path=Root+"/Models/"+ped.name+" festival hair.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   var generated=new Mesh{name="Combined festival hair"};generated.CombineMeshes(tufts.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());
   if(mesh==null){mesh=generated;AssetDatabase.CreateAsset(mesh,path);}else{EditorUtility.CopySerialized(generated,mesh);UnityEngine.Object.DestroyImmediate(generated);EditorUtility.SetDirty(mesh);}
   var material=tufts[0].GetComponent<Renderer>().sharedMaterial;foreach(var tuft in tufts)UnityEngine.Object.DestroyImmediate(tuft.gameObject);MeshPart("Combined festival hair",root,mesh,material);
  }
  static GameObject Part(string name,Transform parent,PrimitiveType shape,Material material){var g=GameObject.CreatePrimitive(shape);g.name=name;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.layer=2;g.transform.SetParent(parent,false);g.GetComponent<Renderer>().sharedMaterial=material;return g;}
  static GameObject MeshPart(string name,Transform parent,Mesh mesh,Material material){var g=Group(name,parent);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=material;return g;}
  static Material Mat(string name,Color color,float gloss){string path=Root+"/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat!=null)return mat;mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",gloss);mat.enableInstancing=true;AssetDatabase.CreateAsset(mat,path);return mat;}
  static Mesh Mask(){string path=Root+"/Models/Fabric balaclava with eye holes.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved!=null)return saved;
   const int rows=18,columns=32;var vertices=new Vector3[(rows+1)*(columns+1)];var triangles=new System.Collections.Generic.List<int>();
   for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++){float a=x*2*Mathf.PI/columns,b=y*Mathf.PI/rows;vertices[y*(columns+1)+x]=new Vector3(Mathf.Sin(b)*Mathf.Sin(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Cos(a))*.5f;}
   for(int y=0;y<rows;y++)for(int x=0;x<columns;x++){int a=y*(columns+1)+x,b=a+columns+1;Vector3 mid=(vertices[a]+vertices[a+1]+vertices[b]+vertices[b+1])*.25f;bool eye=mid.z>.25f&&mid.y>.035f&&mid.y<.2f&&Mathf.Abs(mid.x)>.035f&&Mathf.Abs(mid.x)<.3f;if(eye)continue;triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
   var mesh=new Mesh{name="Fabric balaclava with eye holes"};mesh.vertices=vertices;mesh.triangles=triangles.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
  }
  static Mesh Fork(){string path=Root+"/Models/Blunt festival pitchfork.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved!=null)return saved;var parts=new System.Collections.Generic.List<CombineInstance>();var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var mesh=cube.GetComponent<MeshFilter>().sharedMesh;
   parts.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(new Vector3(0,.5f,0),Quaternion.identity,new Vector3(.045f,1.5f,.045f))});parts.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(new Vector3(0,1.2f,0),Quaternion.identity,new Vector3(.42f,.065f,.055f))});
   for(int i=-1;i<=1;i++)parts.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(new Vector3(i*.18f,1.39f,0),Quaternion.Euler(0,0,-i*7),new Vector3(.04f,.4f,.04f))});
   saved=new Mesh{name="Blunt festival pitchfork"};saved.CombineMeshes(parts.ToArray());UnityEngine.Object.DestroyImmediate(cube);AssetDatabase.CreateAsset(saved,path);return saved;
  }
  static void DealerHair(Transform parent,Vector3 center,float height){
   const string path="Assets/TruckPoker/Art/DealerHair/Hair_Ponytail_01.fbx";var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(model==null)throw new Exception("Missing existing Poker dealer hair");
   string meshPath=Root+"/Models/Dealer ponytail static crowd LOD.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
   if(mesh==null){var instance=UnityEngine.Object.Instantiate(model);var skin=instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderBy(r=>r.sharedMesh.vertexCount).First();mesh=new Mesh{name="Dealer ponytail static crowd LOD"};skin.BakeMesh(mesh);AssetDatabase.CreateAsset(mesh,meshPath);UnityEngine.Object.DestroyImmediate(instance);}
   var hair=MeshPart("Existing Poker dealer ponytail",parent,mesh,Mat("Penalty dealer blonde hair",new Color(.96f,.69f,.2f),.22f));float scale=height/Mathf.Max(.01f,mesh.bounds.size.y);hair.transform.localScale=Vector3.one*scale;hair.transform.position=center-hair.transform.TransformVector(mesh.bounds.center);
  }
  public static void InstallCurrent(PlowMatch match){
   // Match the existing Taxi integration: an assigned Unity skybox suppresses Weather Maker's sky pass.
   RenderSettings.skybox=null;
   if(GameObject.Find("Decorative festival surroundings")==null){
    var foundation=GameObject.Find("Physical city foundation");
    if(foundation!=null){var surround=GameObject.CreatePrimitive(PrimitiveType.Plane);surround.name="Decorative festival surroundings";UnityEngine.Object.DestroyImmediate(surround.GetComponent<Collider>());surround.transform.position=new Vector3(0,-.08f,0);surround.transform.localScale=new Vector3(60,1,60);surround.GetComponent<Renderer>().sharedMaterial=foundation.GetComponent<Renderer>().sharedMaterial;}
   }
   match.climbSurfaces=UnityEngine.Object.FindObjectsByType<Collider>().Where(c=>c.GetComponent<PlowProp>()!=null||c.name.StartsWith("Car_",StringComparison.Ordinal)).ToArray();
   var presentation=match.menus.GetComponent<PlowMenuPresentation>();if(presentation==null)presentation=match.menus.gameObject.AddComponent<PlowMenuPresentation>();presentation.match=match;
   var weather=match.GetComponent<SportsChaosWeather>();if(weather==null)weather=LWS.TruckSports.Editor.SportsChaosAuthoring.Weather(match,"TruckPedestrianPlowing");
   weather.session=match;
   BindWeatherRenderer(match);
   foreach(var light in UnityEngine.Object.FindObjectsByType<Light>())if(light.name=="Sports sunlight")light.enabled=false;
   weather.sun=weather.vendor.GetComponentsInChildren<WeatherMakerCelestialObjectScript>(true).First(s=>s.IsSun).GetComponent<Light>();
   var night=match.GetComponent<PlowNight>();if(night==null)night=match.gameObject.AddComponent<PlowNight>();night.match=match;night.weather=weather;
   string path=Root+"/Materials/Parade full moon night.asset";var profile=AssetDatabase.LoadAssetAtPath<WeatherMakerDayNightCycleProfileScript>(path);
   if(profile==null){profile=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<WeatherMakerDayNightCycleProfileScript>("Assets/TruckBaseJumping/Materials/Fixed daylight.asset"));profile.Speed=profile.NightSpeed=0;profile.Year=2017;profile.Month=3;profile.Day=12;profile.TimeOfDay=22*3600;profile.NightAmbientColorIntensity=.38f;AssetDatabase.CreateAsset(profile,path);}
   weather.vendor.GetComponentInChildren<WeatherMakerDayNightCycleManagerScript>(true).DayNightProfile=profile;
   foreach(var moon in weather.vendor.GetComponentsInChildren<WeatherMakerCelestialObjectScript>(true))if(!moon.IsSun){
    string moonPath=Root+"/Materials/Parade moon.mat";var moonMaterial=AssetDatabase.LoadAssetAtPath<Material>(moonPath);
    if(moonMaterial==null){moonMaterial=new Material(moon.Renderer.sharedMaterial);AssetDatabase.CreateAsset(moonMaterial,moonPath);}moon.Renderer.sharedMaterial=moonMaterial;
    moon.Scale=.075f;moon.LightBaseIntensity=.6f;moon.TintColor=new Color(.75f,.83f,1);
   }
   var lights=match.transform.Find("Festival night fill lights");if(lights==null){lights=Group("Festival night fill lights",match.transform).transform;for(int i=0;i<9;i++){var light=Group("Warm festival light "+i,lights).AddComponent<Light>();light.type=LightType.Point;light.range=38;light.intensity=6;light.color=i%2==0?new Color(1,.62f,.27f):new Color(.35f,.58f,1);light.shadows=LightShadows.None;light.transform.position=new Vector3((i%3-1)*42,8,(i/3-1)*42);}}
  }
  static void BindWeatherRenderer(PlowMatch match){
   string folder=Root+"/Rendering";if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder(Root,"Rendering");
   string rendererPath=folder+"/Parade Weather Maker renderer.asset",pipelinePath=folder+"/Parade night pipeline.asset";
   var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);if(renderer==null){AssetDatabase.CopyAsset("Assets/TruckBaseJumping/Rendering/Base renderer.asset",rendererPath);renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);}
   if(!renderer.rendererFeatures.Any(f=>f is WeatherMakerURPRenderFeatureScript)){var feature=ScriptableObject.CreateInstance<WeatherMakerURPRenderFeatureScript>();feature.name="Installed Weather Maker URP night and clouds";AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);feature.Create();EditorUtility.SetDirty(renderer);EditorUtility.SetDirty(feature);}
   var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);if(pipeline==null){AssetDatabase.CopyAsset("Assets/TruckBaseJumping/Rendering/Base pipeline.asset",pipelinePath);pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);}
   var serialized=new SerializedObject(pipeline);serialized.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=renderer;serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);
   match.GetComponent<LWS.TruckBaseJumping.BaseJumpRendering>().pipeline=pipeline;
  }
 }
}
