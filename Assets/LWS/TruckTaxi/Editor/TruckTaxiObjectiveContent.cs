using System;
using System.Linq;
using LWS.InterstateHauler;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiObjectiveContent
    {
        [MenuItem("Truck Taxi/Write Objective Content Inventory")]
        public static void WriteInventory()
        {
            var definitions=AssetDatabase.FindAssets("t:PassengerRequestDefinition",new[]{TruckTaxiDemoBuilder.Root})
                .Select(g=>AssetDatabase.LoadAssetAtPath<PassengerRequestDefinition>(AssetDatabase.GUIDToAssetPath(g))).OrderBy(d=>(int)d.requestType);
            var profiles=AssetDatabase.FindAssets("t:PassengerProfile",new[]{TruckTaxiDemoBuilder.Root})
                .Select(g=>AssetDatabase.LoadAssetAtPath<PassengerProfile>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            var text=new System.Text.StringBuilder("# Authored Objective Content Inventory\n\nGenerated from current assets by Truck Taxi / Write Objective Content Inventory. Runtime capabilities still gate these authored references. See TruckTaxi_ObjectiveAudit.md for evaluators, failure conditions, conflicts and validation.\n\n");
            foreach(var definition in definitions)
            {
                text.Append("## ").Append(definition.StableId).Append('\n');
                text.Append("Asset: `").Append(AssetDatabase.GetAssetPath(definition)).Append("`\n\n");
                text.Append("Authored description: ").Append(definition.description).Append(". Runtime text uses authoritative Target.\n\n");
                text.Append("Enabled: ").Append(definition.enabledForSelection).Append("; deadline: ").Append(definition.timer).Append("s; base target: ").Append(definition.target)
                    .Append("; requirements: ").Append(definition.RequiredCapabilities).Append("; requires/forbids: ").Append(definition.RequiredBehavior).Append(" / ").Append(definition.ForbiddenBehavior).Append(".\n\n");
                text.Append("Authored eligible passengers: ");
                text.Append(string.Join(", ",profiles.Where(p=>Array.Exists(p.possibleRequests??Array.Empty<PassengerRequestDefinition>(),d=>d==definition))
                    .OrderBy(p=>p.passengerId).Select(p=>p.passengerName+" (`"+p.passengerId+"`)")));
                text.Append("\n\n");
            }
            System.IO.File.WriteAllText(TruckTaxiDemoBuilder.Root+"/Documentation/TruckTaxi_ObjectiveContentInventory.md",text.ToString());
            AssetDatabase.Refresh();
        }
        [MenuItem("Truck Taxi/Update Optional Stops And Objectives")]
        public static void UpdateContent()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene=EditorSceneManager.OpenScene(TruckTaxiDemoBuilder.ScenePath);
            var host=Object.FindFirstObjectByType<TruckTaxiBootstrap>();
            var root=GameObject.Find("Taxi optional stop points") ?? new GameObject("Taxi optional stop points");
            string[] scenic={"West skyline bay","Western park vista","Northwest road vista","North skyline bay","North park horizon","Northeast cityscape"};
            string[] illicit={"Warehouse side bay","East service layby","Industrial exchange bay","South loading layby","Quiet package stop","Backstreet meeting bay"};
            var asphalt=AssetDatabase.LoadAssetAtPath<Material>(TruckTaxiDemoBuilder.Root+"/Materials/Asphalt.mat");
            for(int i=0;i<12;i++)
            {
                bool view=i<6; int j=i%6;
                string id="taxi.stop."+(view ? "scenic." : "illicit.")+j;
                Transform existing=root.transform.Find(id);
                var go=existing!=null ? existing.gameObject : new GameObject(id);
                go.transform.SetParent(root.transform,false);
                var point=go.GetComponent<TruckTaxiStopObjectivePoint>() ?? go.AddComponent<TruckTaxiStopObjectivePoint>();
                if(existing==null)
                {
                    go.transform.position=j<3 ? new Vector3(view ? -339 : 339,.15f,-240+j*240) :
                        new Vector3(-240+(j-3)*240,.15f,view ? 339 : -339);
                    point.stableId=id; point.displayName=(view ? scenic : illicit)[j]; point.radius=8;
                    point.durationSeconds=view ? 12 : 8; point.category=view ? TruckTaxiStopCategory.Scenic : TruckTaxiStopCategory.IllicitPickup;
                    point.viewDirection=-go.transform.position.normalized;
                    point.district=view ? "City edge" : "Industrial";
                    point.arrivalDialogue=view ? "Now that's a view. Let me enjoy it a moment." : "Quick handoff. It's probably just an aggressively wrapped sandwich.";
                    point.completionDialogue=view ? "Worth the detour. Back to the ride!" : "Package acquired. Absolutely no follow-up questions.";
                    point.chaosReward=view ? 0 : 50;
                    var pad=GameObject.CreatePrimitive(PrimitiveType.Cube); pad.name="Optional stop paved bay"; pad.transform.SetParent(go.transform,false);
                    pad.transform.localPosition=Vector3.down*.08f; pad.transform.localScale=new Vector3(22,.16f,26);
                    if(asphalt!=null) pad.GetComponent<Renderer>().sharedMaterial=asphalt;
                    pad.AddComponent<TruckTaxiSurface>().isRoad=true;
                }
            }
            foreach(var collider in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                string name=collider.name;
                bool road=name=="Taxi bay" || name=="Parking court" || name=="Central Park Plaza";
                for(Transform t=collider.transform;t!=null;t=t.parent) if(t.name.Contains("EasyRoads Baked")) road=true;
                if(road || name=="Ground")
                { var surface=collider.GetComponent<TruckTaxiSurface>() ?? collider.gameObject.AddComponent<TruckTaxiSurface>(); surface.isRoad=road; }
            }
            // Superseded drive-through scenic triggers must not award the new timed-stop objective.
            foreach(var trigger in Object.FindObjectsByType<ShortcutTrigger>(FindObjectsSortMode.None)) if(trigger.scenicPoint) trigger.enabled=false;
            var definitions=AssetDatabase.FindAssets("t:PassengerRequestDefinition",new[]{TruckTaxiDemoBuilder.Root})
                .Select(g=>AssetDatabase.LoadAssetAtPath<PassengerRequestDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToList();
            var sketchy=definitions.Find(d=>d.requestType==TaxiRequestType.IllicitStop);
            if(sketchy==null)
            {
                sketchy=ScriptableObject.CreateInstance<PassengerRequestDefinition>(); sketchy.requestType=TaxiRequestType.IllicitStop;
                sketchy.description="Make a sketchy pickup"; sketchy.dialogue="I gotta make a quick stop. Don't ask.";
                sketchy.timer=180; sketchy.bonusMoneyCents=650; sketchy.ratingModifier=.3f;
                AssetDatabase.CreateAsset(sketchy,TruckTaxiDemoBuilder.Root+"/ScriptableObjects/Requests/IllicitStop.asset"); definitions.Add(sketchy);
            }
            foreach(var definition in definitions)
            { definition.objectiveId="taxi.objective."+definition.requestType; definition.enabledForSelection=true; EditorUtility.SetDirty(definition); }
            host.configuration.requests=definitions.OrderBy(d=>(int)d.requestType).ToArray(); EditorUtility.SetDirty(host.configuration);
            var scenicDef=definitions.Find(d=>d.requestType==TaxiRequestType.ScenicRoute);
            scenicDef.description="Scenic stop"; scenicDef.dialogue="Can we pull over somewhere with a view?";
            foreach(var profile in host.configuration.passengerDatabase.passengers)
            {
                if(profile.chaosAffinity>0)
                    profile.possibleRequests=(profile.possibleRequests??Array.Empty<PassengerRequestDefinition>()).Concat(new[]{sketchy}).Distinct().ToArray();
                if(profile.passengerName=="Bree Brightside")
                {
                    profile.explicitlyAdult=true; profile.minimumAdultAge=21; profile.adultFemalePresentation=true;
                    profile.flirtatiousPresentation=true; profile.specialAppreciationEligible=true;
                    profile.possibleRequests=(profile.possibleRequests??Array.Empty<PassengerRequestDefinition>()).Concat(new[]{scenicDef}).Distinct().ToArray();
                }
                EditorUtility.SetDirty(profile);
            }
            Physics.SyncTransforms();
            AddStopAccessLinks(host,root.GetComponentsInChildren<TruckTaxiStopObjectivePoint>());
            foreach(var p in root.GetComponentsInChildren<TruckTaxiStopObjectivePoint>())
            {
                var route=new TruckTaxiRouteDistanceService(host.roadGraph.Graph).Measure(Vector3.zero,p.Position);
                if(!route.Navigable) throw new InvalidOperationException("Stop not routable: "+p.stableId);
                if(!Physics.Raycast(p.Position+Vector3.up*3,Vector3.down,out _,6,~0,QueryTriggerInteraction.Ignore))
                    throw new InvalidOperationException("Stop has no ground: "+p.stableId);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("TAXI OBJECTIVE CONTENT: 6 scenic / 6 sketchy paved bays; route and ground checks passed; existing city preserved.");
        }
        private static void AddStopAccessLinks(TruckTaxiBootstrap host,TruckTaxiStopObjectivePoint[] points)
        {
            var graph=host.roadGraph.Graph;
            foreach(var point in points)
            {
                // Author an access leg on the existing street graph, using the same bay
                // sampling helper as passenger stops. No alternate runtime route solver.
                graph.edges.RemoveAll(e=>e.fromNodeId==point.stableId || e.toNodeId==point.stableId);
                graph.nodes.RemoveAll(n=>n.nodeId==point.stableId);
                graph.nodes.Add(new LwsRoadNode { nodeId=point.stableId,position=point.Position,regionId="taxi.demo",sceneChunkId="taxi.city" });
                bool vertical=Mathf.Abs(point.Position.x)>Mathf.Abs(point.Position.z);
                float street=vertical ? Mathf.Sign(point.Position.x)*320 : Mathf.Sign(point.Position.z)*320;
                var approach=vertical ? new Vector3(street,point.Position.y,point.Position.z) : new Vector3(point.Position.x,point.Position.y,street);
                var junctions=graph.nodes.Where(n=>n.nodeId.StartsWith("taxi.n",StringComparison.Ordinal) &&
                    Mathf.Abs((vertical ? n.position.x : n.position.z)-street)<.1f)
                    .OrderBy(n=>(n.position-approach).sqrMagnitude).Take(2).ToArray();
                foreach(var node in junctions)
                {
                    var access=(node.position-approach).sqrMagnitude<.01f ? new[]{node.position,point.Position} : new[]{node.position,approach,point.Position};
                    TruckTaxiDemoBuilder.BayEdge(graph,node.nodeId,point.stableId,access);
                    TruckTaxiDemoBuilder.BayEdge(graph,point.stableId,node.nodeId,access.Reverse().ToArray());
                }
            }
            host.roadGraph.SetGraph(graph,false);
            if(!graph.Validate().IsValid) throw new InvalidOperationException(graph.Validate().Summary);
            EditorUtility.SetDirty(host.roadGraph);
        }
    }
}
