using System;
using System.Collections.Generic;
using System.Linq;
using LWS.InterstateHauler;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiMoralityValidationAuthoring
    {
        private const string ScenePath="Assets/LWS/TruckTaxi/Scenes/TruckTaxi_DemoCity.unity";
        private const string AssetRoot="Assets/LWS/TruckTaxi/Resources/TruckTaxi/MoralityValidation";
        // Batch entry point. Never invoke regional/demo generators from here.
        [MenuItem("Truck Taxi/Morality/Author Optional Existing-Content Validation")]
        public static void Execute()
        {
            if(Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring.");
            for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save existing scene edits first; no dirty scene is replaced.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
                var host=Components<TruckTaxiBootstrap>(scene).Single();
                var regional=host.GetComponent<TruckTaxiRegionalWorld>();
                if(regional==null || regional.regions==null || regional.regions.Length==0)
                    throw new InvalidOperationException("Persistent Taxi scene has no authored regional metadata.");
                var stops=Components<TruckTaxiRideLocation>(scene).Where(s=>s.isActiveAndEnabled && s.dropoffAllowed &&
                    !string.IsNullOrWhiteSpace(s.locationId)).OrderBy(s=>s.locationId,StringComparer.Ordinal).ToArray();
                var persistentTrailers=Components<LwsTrailerGameplayIdentity>(scene).ToArray();
                int regionalTrailerCount=0;
                var regionPaths=AssetDatabase.FindAssets("t:Scene",new[] { "Assets/LWS/TruckTaxi/Regional/Scenes" })
                    .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
                foreach(string path in regionPaths)
                {
                    var chunk=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                    var trailers=Components<LwsTrailerGameplayIdentity>(chunk).ToArray(); regionalTrailerCount+=trailers.Length;
                    Debug.Log("MORALITY AUDIT scene="+path+" trailer-identities="+trailers.Length);
                    EditorSceneManager.CloseScene(chunk,true);
                }
                var graph=host.roadGraph!=null ? host.roadGraph.Graph : null;
                var routes=new TruckTaxiRouteDistanceService(graph);
                ReportExistingStopPair(stops,regional,routes,graph!=null);

                // Independent useful integration even when the optional trailer event is blocked.
                var binding=host.GetComponent<TruckTaxiMoralityTownBinding>();
                if(binding==null) binding=Undo.AddComponent<TruckTaxiMoralityTownBinding>(host.gameObject);
                if(binding.regionalWorld!=null && binding.regionalWorld!=regional)
                    throw new InvalidOperationException("Existing town binding references another regional owner; refusing to overwrite it.");
                Undo.RecordObject(binding,"Bind existing Taxi town metadata"); binding.regionalWorld=regional;
                EditorUtility.SetDirty(binding);

                if(persistentTrailers.Length==0)
                {
                    var spawner=host.spawner!=null ? new SerializedObject(host.spawner) : null;
                    bool hasPrefab=spawner?.FindProperty("trailerPrefab")?.objectReferenceValue!=null;
                    bool hasPoint=spawner?.FindProperty("trailerSpawnPoint")?.objectReferenceValue!=null;
                    Debug.LogWarning("MORALITY VALIDATION BLOCKED: persistent trailer identities=0; regional trailer identities="+
                        regionalTrailerCount+"; spawner trailerPrefab="+hasPrefab+"; trailerSpawnPoint="+hasPoint+
                        ". No trailer/event/choice assets created. A prefab in the project is not an existing physical scene trailer.");
                }
                else TryAuthorEvent(host,regional,persistentTrailers,stops,routes,graph!=null);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                Debug.Log("MORALITY TOWN BINDING: persistent scene now consumes TruckTaxiRegionalWorld.ResolveRideTown. Existing town boundaries and stops unchanged.");
            }
            finally
            {
                if(setup.Length>0 && setup.All(s=>!string.IsNullOrEmpty(s.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            }
        }
        private static IEnumerable<T> Components<T>(Scene scene) where T:Component =>
            scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<T>(true));
        private static void ReportExistingStopPair(TruckTaxiRideLocation[] stops,TruckTaxiRegionalWorld regional,TruckTaxiRouteDistanceService routes,bool hasGraph)
        {
            if(hasGraph) for(int i=0;i<stops.Length;i++) for(int j=i+1;j<stops.Length;j++)
            {
                string town=regional.ResolveRideTown(stops[i].StopPosition);
                if(string.IsNullOrEmpty(town) || town!=regional.ResolveRideTown(stops[j].StopPosition) ||
                    Vector3.Distance(stops[i].StopPosition,stops[j].StopPosition)<40) continue;
                var forward=routes.BetweenStops(stops[i],stops[j]); var backward=routes.BetweenStops(stops[j],stops[i]);
                if(!forward.Navigable || !backward.Navigable) continue;
                Debug.Log("MORALITY EXISTING STOPS: town="+town+" A="+stops[i].locationId+" ("+stops[i].locationName+
                    ") B="+stops[j].locationId+" ("+stops[j].locationName+") route-meters="+forward.Meters.ToString("F1")+
                    ". Graph reachability only; trailer clearance still requires live validation."); return;
            }
            Debug.LogWarning("MORALITY EXISTING STOPS: no graph-validated same-town pair is available; no destinations will be invented.");
        }
        private static void TryAuthorEvent(TruckTaxiBootstrap host,TruckTaxiRegionalWorld regional,LwsTrailerGameplayIdentity[] trailers,
            TruckTaxiRideLocation[] stops,TruckTaxiRouteDistanceService routes,bool hasGraph)
        {
            var candidates=trailers.Where(t=>t.isActiveAndEnabled && t.Validate(out _) && t.GetComponent<Rigidbody>()!=null &&
                !t.GetComponent<Rigidbody>().isKinematic && t.GetComponent<Rigidbody>().detectCollisions).ToArray();
            if(candidates.Length!=1 || !hasGraph)
            { Debug.LogWarning("MORALITY VALIDATION BLOCKED: exactly one existing usable persistent trailer and a road graph are required; candidates="+candidates.Length); return; }
            var trailer=candidates[0];
            // Runtime validation owns the NWH dependency; the editor must not fabricate physical components.
            if(!trailer.GetComponents<Component>().Any(c=>c!=null && c.GetType().FullName=="NWH.VehiclePhysics2.VehicleController"))
            { Debug.LogWarning("MORALITY VALIDATION BLOCKED: existing trailer lacks NWH VehicleController."); return; }
            string townId=regional.ResolveRideTown(trailer.transform.position);
            var suitable=stops.Where(s=>regional.ResolveRideTown(s.StopPosition)==townId &&
                s.detectionRadius>=3 && s.detectionRadius<=50 && routes.Measure(trailer.transform.position,s.StopPosition).Navigable).ToArray();
            var pair=(from a in suitable from b in suitable where string.CompareOrdinal(a.locationId,b.locationId)<0 &&
                Vector3.Distance(a.StopPosition,b.StopPosition)>=40 && routes.BetweenStops(a,b).Navigable && routes.BetweenStops(b,a).Navigable
                select new[] { a,b }).FirstOrDefault();
            if(string.IsNullOrEmpty(townId) || pair==null)
            { Debug.LogWarning("MORALITY VALIDATION BLOCKED: existing trailer has no two matching route-validated authored stops."); return; }
            if(trailer.GetComponent<TruckTaxiMoralTrailerEvent>()!=null || AssetDatabase.IsValidFolder(AssetRoot))
            { Debug.LogWarning("MORALITY VALIDATION: existing event/content retained; use explicit Inspector review rather than overwrite."); return; }
            string eventId="taxi.morality.validation."+trailer.StableTrailerId;
            if(eventId.Length>110) { Debug.LogWarning("MORALITY VALIDATION BLOCKED: trailer identity too long for derived objective IDs."); return; }
            var town=ScriptableObject.CreateInstance<TruckTaxiTownDefinition>(); town.initialState.townId=townId;
            var choice=ScriptableObject.CreateInstance<TruckTaxiMoralChoiceDefinition>();
            choice.eventId=eventId; choice.town=town; choice.trailerId=trailer.StableTrailerId;
            choice.description="People are inside this trailer. Choose the authority destination or the contact destination.";
            choice.options=new[] {
                new TruckTaxiMoralChoiceOption { choiceId=TruckTaxiMoralChoiceDefinition.TurnIn,label="Turn them in",completionObjectiveId=eventId+".authority",
                    effects=new TruckTaxiMoralChoiceEffects { payoutCents=1000,safety=5,crime=-5,reputation=5,authorityStanding=5,fareMultiplier=.01f } },
                new TruckTaxiMoralChoiceOption { choiceId=TruckTaxiMoralChoiceDefinition.Deliver,label="Deliver to destination",completionObjectiveId=eventId+".contact",
                    effects=new TruckTaxiMoralChoiceEffects { payoutCents=1500,safety=-5,crime=5,trafficPressure=5,shadyStanding=5,fareMultiplier=-.01f } }
            };
            if(!choice.Validate(out var reason)) { Object.DestroyImmediate(choice); Object.DestroyImmediate(town); throw new InvalidOperationException(reason); }
            AssetDatabase.CreateFolder("Assets/LWS/TruckTaxi/Resources/TruckTaxi","MoralityValidation");
            AssetDatabase.CreateAsset(town,AssetRoot+"/ValidationTown.asset"); AssetDatabase.CreateAsset(choice,AssetRoot+"/ValidationEvent.asset");
            var authored=Undo.AddComponent<TruckTaxiMoralTrailerEvent>(trailer.gameObject); authored.trailer=trailer; authored.definition=choice;
            authored.destinations=Enumerable.Range(0,2).Select(i=>new TruckTaxiMoralDestination {
                objectiveId=choice.options[i].completionObjectiveId,displayName=pair[i].locationName,
                center=pair[i].truckStopPoint!=null ? pair[i].truckStopPoint : pair[i].transform,radius=pair[i].detectionRadius
            }).ToArray();
            if(!authored.ValidatePhysical(out reason)) throw new InvalidOperationException(reason);
            var controller=host.GetComponent<TruckTaxiMoralityWorldController>() ?? Undo.AddComponent<TruckTaxiMoralityWorldController>(host.gameObject);
            Undo.RecordObject(controller,"Add optional explicit morality event");
            controller.authoredEvents=(controller.authoredEvents ?? Array.Empty<TruckTaxiMoralTrailerEvent>()).Concat(new[] { authored }).ToArray();
            EditorUtility.SetDirty(authored); EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets();
            Debug.Log("MORALITY VALIDATION AUTHORED: "+eventId+" uses existing trailer and stops "+pair[0].locationId+", "+pair[1].locationId+". No vehicle or coordinates created.");
        }
    }
}
