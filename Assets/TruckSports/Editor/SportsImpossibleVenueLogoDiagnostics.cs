using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports.Editor
{
    // Read-only saved glyph measurement. Never calls authoring, text setters,
    // camera pose APIs, import, material creation or SaveScene/SaveAssets.
    public static class SportsImpossibleVenueLogoDiagnostics
    {
        [Serializable] sealed class Measurement
        {
            public string scene, sceneSha256, name, hierarchy, text, font;
            public int fontSize;
            public float characterSize;
            public Vector3 position, scale;
            public Bounds actualGlyphBounds, actualBackdropBounds;
            public Rect proposedLogoRect, actualGlyphRect, suggestedLeftRect;
            public bool currentOverlapsGlyphs, suggestedLeftOverlapsGlyphs, suggestedLeftFitsBackdrop;
            public float suggestedLeftCenterX, originalFov;
            public Vector3 originalCameraPosition, originalCameraEuler;
        }
        [Serializable] sealed class Report
        {
            public string scope = "Stopped Editor actual saved renderer bounds only. No rendered/native gameplay/Windows/human acceptance.";
            public Measurement[] measurements;
        }
        static string Hash(string path) {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        static string Hierarchy(Transform transform) => transform.parent == null ? transform.name : Hierarchy(transform.parent)+"/"+transform.name;
        static T[] All<T>(Scene scene) where T:Component => scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<T>(true)).ToArray();
        public static void InspectSavedTitleBounds()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required for read-only glyph measurement.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve pending scene edits before measurement.");
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-tsImpossibleVenueBoundsOutput");
            string output=index>=0&&index+1<args.Length?args[index+1]:"Logs/TruckSports/PlaytestRepair/ImpossibleVenueLogoBounds/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".json";
            output=Path.GetFullPath(output);if(File.Exists(output))throw new IOException("Preserve earlier measurements; choose a new output file.");
            var previous=EditorSceneManager.GetSceneManagerSetup();var rows=new List<Measurement>();
            try
            {
                foreach(string name in new[]{"TruckBakeOff","TruckGuessThat"})
                {
                    string path="Assets/"+name+"/Scenes/"+name+".unity",before=Hash(path);var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                    string titleName=name=="TruckBakeOff"?"Original television show title":"Mechanical board title";
                    var titles=All<TextMesh>(scene).Where(title=>title.name==titleName).ToArray();
                    if(titles.Length!=(name=="TruckBakeOff"?1:4))throw new InvalidOperationException("Exact original saved titles required.");
                    foreach(var title in titles)
                    {
                        var glyph=title.GetComponent<MeshRenderer>().bounds;var rectangle=Rect.MinMaxRect(glyph.min.x,glyph.min.y,glyph.max.x,glyph.max.y);
                        var center=name=="TruckBakeOff"?new Vector3(-30,11,94.98f):title.transform.parent.TransformPoint(new Vector3(9.5f,17.2f,.98f));var size=name=="TruckBakeOff"?new Vector2(14,7):new Vector2(2.4f,1.2f);
                        var proposed=new Rect(center.x-size.x/2,center.y-size.y/2,size.x,size.y);
                        var row=new Measurement{scene=name,sceneSha256=before,name=title.name,hierarchy=Hierarchy(title.transform),text=title.text,font=title.font!=null?title.font.name:"NONE",fontSize=title.fontSize,characterSize=title.characterSize,position=title.transform.position,scale=title.transform.lossyScale,actualGlyphBounds=glyph,actualGlyphRect=rectangle,proposedLogoRect=proposed,currentOverlapsGlyphs=rectangle.Overlaps(proposed)};
                        if(name=="TruckBakeOff")
                        {
                            var backdrop=All<Transform>(scene).Single(item=>item.name=="Giant television bake-off title backdrop").GetComponent<MeshRenderer>().bounds;row.actualBackdropBounds=backdrop;
                            row.suggestedLeftCenterX=Mathf.Min(-30,glyph.min.x-7-2);
                            row.suggestedLeftRect=new Rect(row.suggestedLeftCenterX-7,7.5f,14,7);
                            row.suggestedLeftOverlapsGlyphs=rectangle.Overlaps(row.suggestedLeftRect);
                            row.suggestedLeftFitsBackdrop=row.suggestedLeftRect.xMin>=backdrop.min.x+2&&row.suggestedLeftRect.xMax<=backdrop.max.x-2&&row.suggestedLeftRect.yMin>=backdrop.min.y+1&&row.suggestedLeftRect.yMax<=backdrop.max.y-1;
                        }
                        else
                        {
                            var camera=All<LWS.TruckGuessThat.GuessMatch>(scene).Single().boardCamera.view;row.originalFov=camera.fieldOfView;row.originalCameraPosition=camera.transform.position;row.originalCameraEuler=camera.transform.eulerAngles;
                        }
                        rows.Add(row);Debug.Log("IMPOSSIBLE_SAVED_GLYPH "+name+" "+row.hierarchy+" glyph="+glyph.ToString("F6")+" proposed="+proposed.ToString("F6")+" overlap="+row.currentOverlapsGlyphs+" suggestedLeftX="+row.suggestedLeftCenterX.ToString("R")+" fitsExistingBackdrop="+row.suggestedLeftFitsBackdrop);
                    }
                    if(Hash(path)!=before)throw new InvalidOperationException("Read-only measurement changed saved scene bytes unexpectedly.");
                }
                Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonUtility.ToJson(new Report{measurements=rows.ToArray()},true));Debug.Log("IMPOSSIBLE_SAVED_GLYPH_REPORT="+output);
            }
            finally {if(previous.Any(item=>item.isLoaded&&item.isActive&&!string.IsNullOrEmpty(item.path)))EditorSceneManager.RestoreSceneManagerSetup(previous);}
        }
    }
}
