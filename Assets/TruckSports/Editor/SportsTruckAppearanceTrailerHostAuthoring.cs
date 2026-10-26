using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckTrailerPlug;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports.Editor
{
    public static partial class SportsTruckAppearanceHostAuthoring
    {
        [MenuItem("Truck Sports/Customizer/Bind Missing Trailer Plug Turn Host")]
        public static void BindMissingTrailerPlugHost()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            var catalog=AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(LWS.TruckTrailerPlug.Editor.TrailerPlugRepairAuthoring.TractorSource);
            if(catalog==null||!catalog.Validate(out _)||source==null)throw new InvalidOperationException("Existing validated catalog and original matched NWH tractor required.");
            const string path="Assets/TruckTrailerPlug/Scenes/TruckTrailerPlug.unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(scene.isLoaded&&scene.isDirty)throw new InvalidOperationException("Save existing scene edits first.");if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var roots=scene.GetRootGameObjects();var match=roots.SelectMany(root=>root.GetComponentsInChildren<TrailerPlugMatch>(true)).Single();var rig=match.rig;var truck=rig!=null?rig.truck:null;
                if(truck==null||truck.session!=match||truck.visuals!=truck.transform||truck.transform.localScale!=Vector3.one||truck.tractorMotor==null||truck.wheels.Length!=4||truck.wheels.Any(wheel=>wheel==null||wheel.wheel.rotatingContainer==null)||rig.tractorHitch==null||rig.trailerModule==null||rig.trailer==null||rig.spring==null||rig.cable==null||rig.anchor==null||truck.GetComponent<SportsFragmentRig>()!=null)throw new InvalidOperationException("Original matched suspended NWH tractor/trailer assembly required.");
                if(truck.appearanceHost!=null||truck.GetComponent<SportsTruckAppearanceHost>()!=null){Debug.Log("SPORTS_TRAILER_TURN_HOST_PRESERVED existing custom host retained.");return;}
                if(match.replay==null||match.replay.sources.Length!=2||match.replay.ghosts.Length!=2||match.replay.sources[0]!=truck.transform||match.replay.sources[1]!=rig.trailer.transform||match.replay.ghosts.Any(ghost=>ghost==null)||match.replay.ghosts.Distinct().Count()!=2)throw new InvalidOperationException("Preserve original tractor/trailer replay roots.");
                var body=MatchedTrailerRenderers(truck.transform,source.transform,FacadeBodyPaths);var wheels=MatchedTrailerRenderers(truck.transform,source.transform,FacadeWheelPaths);
                var smoke=truck.transform.Find("Exhaust/ExhaustSmokeSemi")?.GetComponent<ParticleSystemRenderer>();if(smoke==null)throw new InvalidOperationException("Original NWH smoke required.");
                var known=new HashSet<Renderer>(body.Concat(wheels).Concat(new Renderer[]{smoke}));if(truck.GetComponentsInChildren<Renderer>(true).Any(renderer=>!known.Contains(renderer)))throw new InvalidOperationException("Unknown matched-tractor rendering retained for inspection.");
                var protectedRoots=new[]{rig.trailer.transform,rig.anchor,rig.cable.transform,rig.tractorHitch.module.attachmentPoint,rig.trailerModule.module.attachmentPoint,truck.transform.Find("Cab/DashInstruments"),truck.transform.Find("Cab/RenderTextureMirrorCameraL"),truck.transform.Find("Cab/RenderTextureMirrorCameraR"),truck.transform.Find("Cameras")};
                if(protectedRoots.Any(part=>part==null||body.Concat(wheels).Any(renderer=>renderer.transform==part||renderer.transform.IsChildOf(part))))throw new InvalidOperationException("Original hitch/camera/instrument equipment overlaps rendering or is absent.");
                var physical=TrailerHostPhysics(truck,rig);var host=truck.gameObject.AddComponent<SportsTruckAppearanceHost>();host.catalog=catalog;host.participantSlot=-1;host.presentationParent=truck.transform;host.originalBodyRenderers=body;host.originalWheelRenderers=wheels;host.auxiliaryExteriorRenderers=new Renderer[]{smoke};host.protectedRoots=protectedRoots;host.wheels=truck.wheels;
                var bounds=LocalBounds(truck.transform,body.Concat(wheels));host.compatibleBodies=catalog.bodies.Select(item=>Fit(item,bounds,truck.transform,truck.wheels)).ToArray();foreach(var fit in host.compatibleBodies)fit.notice+=" Trailer Plug retains its matched trailer, bungee and original physical hitch; this shared tractor follows the current participant's saved appearance.";
                truck.appearanceHost=host;
                // This one physical tractor changes participant each attempt.
                // Never put a slot -1 host in the fixed-owner session array.
                if((match.appearanceHosts??Array.Empty<SportsTruckAppearanceHost>()).Contains(host))throw new InvalidOperationException("Shared turn host must not be a fixed session participant.");
                if(match.appearanceFraming==null||match.appearanceFraming.Length==0)match.appearanceFraming=roots.SelectMany(root=>root.GetComponentsInChildren<SportsBroadcastFraming>(true)).Where(framing=>framing.session==match).ToArray();
                if(TrailerHostPhysics(truck,rig)!=physical)throw new InvalidOperationException("Appearance authoring changed original physical components.");
                EditorUtility.SetDirty(host);EditorUtility.SetDirty(truck);EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Debug.Log("SPORTS_TRAILER_TURN_HOST_BOUND exact matched29+4 scaled leaves, shared owner -1; original two replay roots/hitch/trailer/bungee/cameras/physics retained. Native validation pending.");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        static Renderer[] MatchedTrailerRenderers(Transform root,Transform source,IEnumerable<string> paths)
        {
            return paths.Select(path=>
            {
                var original=source.Find(path);var expected=original!=null?original.GetComponent<MeshFilter>():null;var leaf=root.Find(path+"/"+path.Substring(path.LastIndexOf('/')+1));var filter=leaf!=null?leaf.GetComponent<MeshFilter>():null;var renderer=leaf!=null?leaf.GetComponent<MeshRenderer>():null;
                if(expected==null||expected.sharedMesh==null||filter==null||filter.sharedMesh!=expected.sharedMesh||renderer==null||leaf.GetComponents<Component>().Any(component=>!(component is Transform)&&!(component is MeshFilter)&&!(component is MeshRenderer))||leaf.localScale.x<=0||Mathf.Abs(leaf.localScale.x-leaf.localScale.y)>.0001f||Mathf.Abs(leaf.localScale.x-leaf.localScale.z)>.0001f)throw new InvalidOperationException("Exact original matched source mesh and render-only uniformly scaled leaf required: "+path);
                return (Renderer)renderer;
            }).ToArray();
        }
        static string TrailerHostPhysics(SportsEventTruck truck,TrailerPlugRig rig)
        {
            var objects=new[]{truck.gameObject,rig.trailer.gameObject};var text=new System.Text.StringBuilder();foreach(var root in objects){foreach(var body in root.GetComponentsInChildren<Rigidbody>(true))text.Append(body.GetInstanceID()).Append(':').Append(body.mass).Append(':').Append(body.centerOfMass.ToString("R"));foreach(var collider in root.GetComponentsInChildren<Collider>(true))text.Append(collider.GetInstanceID()).Append(':').Append(EditorJsonUtility.ToJson(collider));foreach(var joint in root.GetComponentsInChildren<Joint>(true))text.Append(joint.GetInstanceID()).Append(':').Append(EditorJsonUtility.ToJson(joint));}return text.ToString();
        }
    }
}
