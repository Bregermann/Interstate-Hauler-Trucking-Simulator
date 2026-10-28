using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckMechanicalBull.Editor;

namespace LWS.TruckOrchestra.Editor
{
    // Explicit render-only repair, never opens/saves/rebuilds a scene or asset.
    public static class OrchestraPresentationAuthoring
    {
        const string Marker="Owned connected instrument supports V2";
        const string IdlePath="Assets/UTS_FullPack/Models/People/Animations/Girl with Heel/idle1_f.FBX";
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException("Orchestra presentation preflight: "+message);}
        static bool Near(Vector3 a,Vector3 b)=>Vector3.Distance(a,b)<.0001f;
        static Transform One(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
        static void Original(Transform t,Vector3 p,Vector3 size,Quaternion rotation=default)
        {
            // Quaternion operator== compares normalized rotations by dot product;
            // zero is a sentinel, not a valid rotation (even zero==zero is false).
            Quaternion expected=rotation.Equals(default(Quaternion))?Quaternion.identity:rotation;
            Require(Near(t.localPosition,p)&&Near(t.localScale,size)&&Quaternion.Angle(t.localRotation,expected)<.001f,"customized original part refused: "+t.name);
        }
        static GameObject Part(Transform parent,string name,Mesh mesh,Material material,Vector3 p,Vector3 size,Quaternion rotation=default)=>ImpossibleBalanceArt.Part(parent,name,mesh,material,p,size,rotation);
        static void Tube(Transform root,string name,Mesh round,Material material,Vector3 a,Vector3 b,float diameter=.12f)
        {Part(root,name,round,material,a,new Vector3(diameter,Vector3.Distance(a,b),diameter),Quaternion.FromToRotation(Vector3.up,b-a));}
        public static void Install(OrchestraMatch match)
        {
            Require(!EditorApplication.isPlaying&&!EditorApplication.isCompiling,"stopped Editor required");
            Require(match!=null&&(match.gameObject.scene.path==OrchestraAuthoring.ScenePath||string.IsNullOrEmpty(match.gameObject.scene.path)),"only original Orchestra/new unsaved author scene");
            Require(match.instruments.Length==4&&match.conductor!=null,"original four stations/conductor required");
            var c=match.conductor;Require(c.actorSpace!=null&&Near(c.actorSpace.position,new Vector3(0,1.05f,-10))&&Near(c.actorSpace.localScale,Vector3.one*2),"original fixed conductor anchor/scale required");
            var animator=c.actorSpace.GetComponent<Animator>();Require(animator!=null&&!animator.enabled&&animator.runtimeAnimatorController==null&&animator.avatar!=null&&!animator.avatar.isHuman&&AssetDatabase.GetAssetPath(animator.avatar)=="Assets/UTS_FullPack/Models/People/Models/Girl/Girl_44.FBX","original disabled Generic rig required");
            Require(c.GetComponentsInChildren<Collider>(true).Length==0&&c.GetComponentsInChildren<Rigidbody>(true).Length==0,"render-only conductor required; no interactive body may move");
            Require(c.idleFoundation==null&&c.leftForearm==null&&c.rightForearm==null,"refuses existing/partial/custom presentation repair");
            var lf=One(c.actorSpace,"Bip01 L Forearm");var rf=One(c.actorSpace,"Bip01 R Forearm");var lh=One(c.actorSpace,"Bip01 L Hand");var rh=One(c.actorSpace,"Bip01 R Hand");
            Require(lf.IsChildOf(c.leftArm)&&rf.IsChildOf(c.rightArm)&&lh.IsChildOf(lf)&&rh.IsChildOf(rf),"actual original skinned arm chains required");
            var clip=AssetDatabase.LoadAllAssetsAtPath(IdlePath).OfType<AnimationClip>().Single(a=>!a.name.StartsWith("__preview")&&a.name=="idle1_f");
            Require(!clip.humanMotion&&clip.length>0,"compatible original Generic idle, not Humanoid retargeting");
            var bindings=AnimationUtility.GetCurveBindings(clip).Where(b=>b.type==typeof(Transform)).ToArray();
            Require(bindings.Length>20&&bindings.All(b=>string.IsNullOrEmpty(b.path)||c.actorSpace.Find(b.path)!=null),"every imported idle transform track must resolve on this exact rig");
            var sceneRoots=match.gameObject.scene.GetRootGameObjects();var all=sceneRoots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            var title=all.Single(t=>t.name=="Concert venue original enamel title").GetComponent<TextMesh>();var podium=all.Single(t=>t.name=="Ornate conductor podium");
            Require(title!=null&&title.text=="THE SIX-TON SYMPHONY"&&Near(title.transform.position,new Vector3(0,16,25))&&Mathf.Abs(title.characterSize-.42f)<.0001f&&title.GetComponent<SportsPerCameraBillboard>()==null&&title.fontSize==64&&Near(title.transform.localScale,Vector3.one)&&Quaternion.Angle(title.transform.rotation,Quaternion.identity)<.001f,"original world enamel title required; shared HUD untouched");
            Require(Near(podium.localPosition,new Vector3(0,.5f,-10))&&Near(podium.localScale,new Vector3(5,1,4)),"original render-only podium required");Require(podium.GetComponents<Collider>().Length==0&&podium.GetComponent<Rigidbody>()==null,"podium must remain presentation only");
            int[] counts={54,13,18,14};
            for(int i=0;i<4;i++)
            {
                var instrument=match.instruments[i];Require(instrument!=null&&instrument.part==i&&Near(instrument.transform.position,new Vector3((i-1.5f)*11,0,7))&&Near(instrument.transform.lossyScale,Vector3.one),"original station owner/position/scale "+i);
                Require(instrument.transform.Find(Marker)==null&&instrument.GetComponentsInChildren<MeshRenderer>(true).Length==counts[i]&&instrument.GetComponentsInChildren<Collider>(true).Length==0&&instrument.GetComponentsInChildren<Rigidbody>(true).Length==0,"original visual-only station inventory "+i);
                var legs=instrument.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Instrument carved leg").OrderBy(t=>t.localPosition.x).ToArray();Require(legs.Length==2,"two original supported legs");for(int side=0;side<2;side++)Original(legs[side],new Vector3(side==0?-3:3,1,1),new Vector3(.35f,2.1f,.5f));
                for(int lane=0;lane<4;lane++)
                {
                    var pivot=instrument.movingParts[lane];Require(pivot!=null&&pivot.parent==instrument.transform&&Near(pivot.localPosition,new Vector3((lane-1.5f)*1.6f,2.7f,0))&&Quaternion.Angle(pivot.localRotation,Quaternion.identity)<.001f,"original lane pivot "+i+"/"+lane);
                    Require(instrument.voices[lane]!=null&&instrument.voices[lane].transform.parent==pivot,"original independent note source binding "+i+"/"+lane);
                    string[] required=i==1?(lane<3?new[]{"Tuned drum shell","Ivory taut drum skin","Animated wooden percussion mallet"}:new[]{"Actually wobbling brass crash cymbal","Animated wooden percussion mallet"}):i==2?new[]{"Flared sounding brass instrument"}:i==3?new[]{"Resonant concert marimba bar","Hanging tuned orchestral bell","Striking felt-tipped mallet"}:Array.Empty<string>();
                    foreach(string name in required)One(pivot,name);
                    if(i==1){Original(One(pivot,lane<3?"Tuned drum shell":"Actually wobbling brass crash cymbal"),Vector3.zero,new Vector3(2.2f,lane<3?1.2f:.13f,2.2f));if(lane<3)Original(One(pivot,"Ivory taut drum skin"),new Vector3(0,1.22f,0),new Vector3(2.1f,.05f,2.1f));Original(One(pivot,"Animated wooden percussion mallet"),new Vector3(.5f,1.7f,-.3f),new Vector3(.13f,.13f,2.2f),Quaternion.Euler(-18,12,0));}
                    if(i==2){Original(One(pivot,"Flared sounding brass instrument"),Vector3.zero,new Vector3(2.3f,3.7f,2.3f),Quaternion.Euler(-70,0,0));var valves=pivot.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Polished trumpet valve and cap").OrderBy(t=>t.localPosition.x).ToArray();Require(valves.Length==3,"original three valves");for(int v=0;v<3;v++)Original(valves[v],new Vector3((v-1)*.32f,.3f,.4f),new Vector3(.22f,.65f,.22f));}
                    if(i==3){Original(One(pivot,"Resonant concert marimba bar"),Vector3.zero,new Vector3(1.3f,.22f,2.6f-lane*.25f));Original(One(pivot,"Hanging tuned orchestral bell"),new Vector3(0,2.8f,1),new Vector3(.8f,1.2f,.8f),Quaternion.Euler(180,0,0));Original(One(pivot,"Striking felt-tipped mallet"),new Vector3(.2f,.6f,-.3f),new Vector3(.15f,.15f,1.7f));}

                    Require(instrument.strikeTips.All(t=>t==null)&&instrument.strikeTargets.All(t=>t==null)&&instrument.resonators.All(t=>t==null),"no custom contact/resonator bindings");
                }
            }
            var piano=match.instruments[0].transform;var key=One(piano,"Giant grand piano curved lacquer cabinet");var lid=One(piano,"Open grand piano angled lid");
            Require(Near(lid.localPosition,new Vector3(0,5,3.5f))&&Quaternion.Angle(lid.localRotation,Quaternion.Euler(-25,0,0))<.001f,"original lid geometry required");
            Original(key,new Vector3(0,2,2.2f),new Vector3(8,1.4f,5));
            var bevel=key.GetComponent<MeshFilter>().sharedMesh;var black=key.GetComponent<Renderer>().sharedMaterial;
            var drum=One(match.instruments[1].movingParts[0],"Tuned drum shell");var round=drum.GetComponent<MeshFilter>().sharedMesh;
            var ivory=match.instruments[0].movingParts[0].GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Individually painted ivory piano key").Select(t=>t.GetComponent<Renderer>().sharedMaterial).Distinct().Single();
            var gold=One(match.instruments[2].movingParts[0],"Flared sounding brass instrument").GetComponent<Renderer>().sharedMaterial;
            var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckBowling/Materials/Maple plank.mat");Require(bevel!=null&&round!=null&&black!=null&&ivory!=null&&gold!=null&&wood!=null,"original owned instrument meshes/materials required");
            // All dependency, inventory and authored-pose checks above precede mutation.
            c.idleFoundation=clip;c.leftForearm=lf;c.rightForearm=rf;c.leftHand=lh;c.rightHand=rh;OrchestraPodiumClearanceAuthoring.ApplyNewPresentation(c.actorSpace,podium);
            OrchestraVenueTitleAuthoring.ApplyNewTitlePosition(title);title.characterSize=.26f;var billboard=title.gameObject.AddComponent<SportsPerCameraBillboard>();billboard.maximumPixelWidth=600;billboard.maximumPixelHeight=36;
            var groups=new Transform[4];for(int i=0;i<4;i++){groups[i]=new GameObject(Marker).transform;groups[i].SetParent(match.instruments[i].transform,false);foreach(var voice in match.instruments[i].voices)voice.transform.SetParent(match.instruments[i].transform,true);}
            // A real rear hinge, raised lid and prop, rather than a floating plate.
            Part(groups[0],"Connected piano keyboard bed",bevel,black,new Vector3(0,2.45f,-.65f),new Vector3(8,.3f,1.2f));
            lid.localRotation=Quaternion.Euler(25,0,0);lid.localPosition=new Vector3(0,2.75f,4.7f)+lid.localRotation*new Vector3(0,0,-2.5f);
            Tube(groups[0],"Visible lid hinge",round,gold,new Vector3(-3.8f,2.75f,4.7f),new Vector3(3.8f,2.75f,4.7f),.1f);
            Tube(groups[0],"Actual lid prop against soundboard",round,wood,new Vector3(-2,2.7f,.5f),new Vector3(-2,2.75f+4.2f*Mathf.Tan(25*Mathf.Deg2Rad),.5f),.12f);
            for(int part=1;part<4;part++)
            {
                var instrument=match.instruments[part];var root=instrument.transform;var group=groups[part];
                Part(group,"Continuous supported instrument deck",bevel,part==1?black:wood,new Vector3(0,2.375f,.2f),new Vector3(8,.65f,3.6f));
                for(int lane=0;lane<4;lane++)
                {
                    var moving=instrument.movingParts[lane];float x=(lane-1.5f)*1.6f;
                    if(part==1)
                    {
                        if(lane<3){One(moving,"Tuned drum shell").SetParent(group,true);One(moving,"Ivory taut drum skin").SetParent(group,true);}
                        else {var cymbal=One(moving,"Actually wobbling brass crash cymbal");cymbal.SetParent(group,true);instrument.resonators[lane]=cymbal;Tube(group,"Cymbal articulated support stem",round,gold,new Vector3(x,2.7f,0),new Vector3(x,2.82f,0));}
                        float pivotY=lane==3?3.11f:4.25f;var mallet=One(moving,"Animated wooden percussion mallet");moving.localPosition=new Vector3(x,pivotY,-1.4f);mallet.localPosition=new Vector3(0,0,1.1f);mallet.localRotation=Quaternion.Euler(-8,0,0);
                        Tube(group,"Percussion actuator post",round,gold,new Vector3(x,2.7f,-1.4f),new Vector3(x,pivotY,-1.4f),.18f);Part(group,"Visible percussion pivot bearing",bevel,gold,new Vector3(x,pivotY,-1.4f),new Vector3(.45f,.35f,.35f));
                        instrument.strikeTips[lane]=new GameObject("Actual drumstick striking tip").transform;instrument.strikeTips[lane].SetParent(mallet,false);instrument.strikeTips[lane].localPosition=Vector3.forward*.5f;
                        instrument.strikeTargets[lane]=new GameObject("Actual fixed drum skin strike point").transform;instrument.strikeTargets[lane].SetParent(group,false);instrument.strikeTargets[lane].localPosition=new Vector3(x,lane==3?2.83f:3.97f,.74f);
                    }
                    else if(part==2)
                    {
                        One(moving,"Flared sounding brass instrument").SetParent(group,true);
                        Tube(group,"Supported horn intake pipe",round,gold,new Vector3(x,2.7f,0),new Vector3(x,2.7f,.8f),.16f);
                        Tube(group,"Horn valve manifold",round,gold,new Vector3(x-.42f,2.9f,.4f),new Vector3(x+.42f,2.9f,.4f),.16f);
                        Tube(group,"Connected horn return bend",round,gold,new Vector3(x,2.7f,.8f),new Vector3(x,2.9f,.4f),.16f);
                        for(int valve=0;valve<3;valve++)Tube(group,"Fixed trumpet valve casing",round,gold,new Vector3(x+(valve-1)*.32f,2.85f,.4f),new Vector3(x+(valve-1)*.32f,3.16f,.4f),.25f);
                    }
                    else
                    {
                        One(moving,"Resonant concert marimba bar").SetParent(group,true);var hanging=One(moving,"Hanging tuned orchestral bell");hanging.SetParent(group,true);
                        var mallet=One(moving,"Striking felt-tipped mallet");moving.localPosition=new Vector3(x,3.15f,-1.55f);mallet.localPosition=new Vector3(0,0,.85f);mallet.localRotation=Quaternion.Euler(-8,0,0);
                        Tube(group,"Marimba actuator post",round,gold,new Vector3(x,2.7f,-1.55f),new Vector3(x,3.15f,-1.55f),.16f);Part(group,"Visible marimba pivot bearing",bevel,gold,new Vector3(x,3.15f,-1.55f),new Vector3(.35f,.25f,.25f));
                        Tube(group,"Actual suspended bell hanger",round,gold,new Vector3(x,5.5f,1),new Vector3(x,6.5f,1),.07f);
                        instrument.strikeTips[lane]=new GameObject("Actual marimba mallet striking tip").transform;instrument.strikeTips[lane].SetParent(mallet,false);instrument.strikeTips[lane].localPosition=Vector3.forward*.5f;
                        instrument.strikeTargets[lane]=new GameObject("Actual fixed marimba bar strike point").transform;instrument.strikeTargets[lane].SetParent(group,false);instrument.strikeTargets[lane].localPosition=new Vector3(x,2.81f,.1f);
                    }
                }
                if(part==3){foreach(float x in new[]{-3f,3f})Tube(group,"Connected bell frame upright",round,gold,new Vector3(x,2.05f,1),new Vector3(x,6.5f,1),.16f);Tube(group,"Connected bell frame crossbar",round,gold,new Vector3(-3.2f,6.5f,1),new Vector3(3.2f,6.5f,1),.18f);}
                EditorUtility.SetDirty(instrument);
            }
            EditorUtility.SetDirty(c);EditorUtility.SetDirty(match);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(match.gameObject.scene);
            Debug.Log("ORCHESTRA_PRESENTATION_V2 original Generic idle and bent-elbow conductor, visible front podium, bounded world enamel title, connected supported instrument/actual relative strikers; audio/rules/input/vehicle bodies unchanged. Caller owns save.");
        }
        [MenuItem("Truck Sports/Impossible Games/Repair OPEN Orchestra/Connected concert presentation")]
        public static void RepairOpenOriginalScene()
        {
            var matches=UnityEngine.Object.FindObjectsByType<OrchestraMatch>(FindObjectsInactive.Include,FindObjectsSortMode.None);Require(matches.Length==1&&matches[0].gameObject.scene.path==OrchestraAuthoring.ScenePath,"open exact original Orchestra scene required");Install(matches[0]);
        }
    }
}
