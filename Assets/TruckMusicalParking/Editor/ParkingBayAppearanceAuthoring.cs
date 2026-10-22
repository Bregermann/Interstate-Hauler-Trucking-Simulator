using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using LWS.TruckSports;
using LWS.TruckSports.Editor;

namespace LWS.TruckMusicalParking.Editor
{
    public static class ParkingBayAppearanceAuthoring
    {
        static SportsBroadcastTheme theme;
        static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 at)=>SportsUIAuthoring.Rect(name,parent,size,at).GetComponent<RectTransform>();
        static Image Image(string name,Transform parent,Vector2 size,Vector2 at,Color color)
        {
            var rect=Rect(name,parent,size,at);var image=rect.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;
        }
        static TMP_Text Text(string name,Transform parent,string value,Vector2 size,Vector2 at,int font,bool bold)
        {
            var text=SportsUIAuthoring.Text(name,parent,value,size,at,font);text.font=bold?theme.headingFont:theme.bodyFont;text.color=SportsBroadcastCanvas.Cream;text.fontSizeMin=font-3;text.overflowMode=TextOverflowModes.Ellipsis;text.raycastTarget=false;return text;
        }
        public static void Upgrade()
        {
            const string path="Assets/TruckMusicalParking/Scenes/TruckMusicalParking.unity";var scene=EditorSceneManager.OpenScene(path);var match=UnityEngine.Object.FindAnyObjectByType<ParkingMatch>();
            if(match==null||match.bays?.Length!=8)throw new InvalidOperationException("Saved eight-bay Parking match required");
            if(match.bays.Any(b=>b.GetComponent<ParkingBayAppearance>()!=null))
            {
                if(match.bays.All(b=>b.GetComponent<ParkingBayAppearance>()!=null))
                {
                    bool changed=false;var ownedPaint=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckMusicalParking/Materials/Reflective bay paint.mat");
                    foreach(var bay in match.bays)
                    {
                        var p=bay.GetComponent<ParkingBayAppearance>();var text=p.numberText;
                        if(Mathf.Approximately(text.fontSizeMax,68)&&text.rectTransform.sizeDelta==new Vector2(96,70))
                        {
                            text.fontSize=text.fontSizeMax=60;text.fontSizeMin=54;text.rectTransform.sizeDelta=new Vector2(96,78);EditorUtility.SetDirty(text);changed=true;
                        }
                        if(ownedPaint!=null)foreach(var marking in bay.markings)if(marking.sharedMaterial!=null&&marking.sharedMaterial.name=="Bay marking enamel"){marking.sharedMaterial=ownedPaint;EditorUtility.SetDirty(marking);changed=true;}
                    }
                    if(changed){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
                    Debug.Log("PARKING_BAY_APPEARANCE preserved authored graphics; stock fitting/paint migration="+changed);return;
                }
                throw new InvalidOperationException("Partial bay graphics already exist; inspect them instead of rebuilding customized markers.");
            }
            theme=AssetDatabase.LoadAssetAtPath<SportsBroadcastTheme>("Assets/TruckSports/Resources/SportsBroadcastTheme.asset");if(theme==null)throw new InvalidOperationException("Existing Heat/TMP broadcast theme required");
            // One shared WorldSpace canvas batches the eight noninteractive decals.
            var canvasRoot=Rect("Parking world bay graphics",match.transform,new Vector2(1920,1080),Vector2.zero);var canvas=canvasRoot.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
            canvasRoot.localPosition=Vector3.zero;canvasRoot.localRotation=Quaternion.identity;canvasRoot.localScale=Vector3.one;
            var portrait=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TruckSports/Resources/Broadcast truck portrait.png");
            string materialPath="Assets/TruckMusicalParking/Materials/Reflective bay paint.mat";
            var paint=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(paint==null){paint=new Material(match.bays[0].markings[0].sharedMaterial);paint.name="Reflective bay paint";if(paint.HasProperty("_Smoothness"))paint.SetFloat("_Smoothness",.3f);AssetDatabase.CreateAsset(paint,materialPath);}
            for(int i=0;i<8;i++)
            {
                var bay=match.bays[i];var p=bay.gameObject.AddComponent<ParkingBayAppearance>();p.match=match;p.bay=bay;p.number=i+1;
                var plate=Image("Bay "+(i+1)+" identity",canvasRoot,new Vector2(320,112),Vector2.zero,SportsBroadcastCanvas.Ink);plate.gameObject.AddComponent<SportsBroadcastPlate>().cut=12;p.plate=plate.rectTransform;p.plate.localScale=Vector3.one*.0125f;p.plate.position=bay.transform.TransformPoint(new Vector3(0,.085f,-bay.size.y*.5f-.7f));p.plate.rotation=Quaternion.Euler(90,0,0);
                p.stripe=Image("Bay state stripe",p.plate,new Vector2(6,88),new Vector2(-154,0),p.closed);
                Text("Parking symbol",p.plate,"P",new Vector2(52,26),new Vector2(-100,39),25,true);
                p.numberText=Text("Bay number",p.plate,(i+1).ToString("00"),new Vector2(96,78),new Vector2(-100,-15),60,true);p.numberText.fontSizeMin=54;
                p.stateText=Text("Bay state",p.plate,"CLOSED",new Vector2(180,40),new Vector2(48,17),34,true);
                p.ownerText=Text("Bay ownership",p.plate,"WAIT FOR HORN",new Vector2(150,29),new Vector2(25,-22),24,false);
                p.portrait=Image("Secured truck",p.plate,new Vector2(30,28),new Vector2(132,-33),Color.white);p.portrait.sprite=portrait;p.portrait.preserveAspect=true;p.portrait.gameObject.SetActive(false);
                p.closedMark=Rect("Closed cross",p.plate,new Vector2(26,26),new Vector2(134,39)).gameObject;
                foreach(float angle in new[]{45f,-45f}){var line=Image("Cross stroke",p.closedMark.transform,new Vector2(24,4),Vector2.zero,p.closed);line.rectTransform.localRotation=Quaternion.Euler(0,0,angle);}
                p.openMark=Rect("Open chevron",p.plate,new Vector2(26,26),new Vector2(134,39)).gameObject;
                foreach(int side in new[]{-1,1}){var line=Image("Chevron stroke",p.openMark.transform,new Vector2(16,4),new Vector2(side*5,-1),p.available);line.rectTransform.localRotation=Quaternion.Euler(0,0,side*45);}
                p.openMark.SetActive(false);
                p.plate.gameObject.SetActive(bay.gameObject.activeInHierarchy);
                // Only retire the known stock text renderer. Its reference and
                // semantic text updates remain intact for existing integrations.
                if(bay.caption!=null&&bay.caption.name=="Painted bay phase")bay.caption.GetComponent<Renderer>().enabled=false;
                foreach(var marking in bay.markings)
                {
                    if(marking.GetComponent<Collider>()!=null)throw new InvalidOperationException("Painted markings must remain noncolliding");
                    if(marking.sharedMaterial!=null&&marking.sharedMaterial.name=="Bay marking enamel")marking.sharedMaterial=paint;
                    var scale=marking.transform.localScale;if(Mathf.Approximately(scale.x,.12f))scale.x=.18f;if(Mathf.Approximately(scale.z,.12f))scale.z=.18f;marking.transform.localScale=scale;
                }
                EditorUtility.SetDirty(p);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair");File.WriteAllText("Logs/TruckSports/PlaytestRepair/parking-bay-graphics.txt","PASS eight owned numbered bay decals,shared world canvas,existing Heat/TMP/portrait/plate,original colliders and ParkingBay rule authority preserved.\n");
        }
    }
}
