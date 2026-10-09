using System;
using LWS.InterstateHauler;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiMoralityAuthoring
    {
        [MenuItem("Truck Taxi/Morality/Add Event To Selected Existing Trailer")]
        public static void AddToSelectedTrailer()
        {
            if(Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Author only while the Editor is idle and outside Play Mode.");
            var selected=Selection.activeGameObject;
            var identity=selected!=null ? selected.GetComponent<LwsTrailerGameplayIdentity>() : null;
            if(identity==null || !identity.Validate(out _) || !selected.scene.IsValid() || EditorUtility.IsPersistent(selected))
                throw new InvalidOperationException("Select an existing scene trailer with its configured stable gameplay identity.");
            var authored=selected.GetComponent<TruckTaxiMoralTrailerEvent>();
            if(authored==null)
            {
                authored=Undo.AddComponent<TruckTaxiMoralTrailerEvent>(selected);
                Undo.RecordObject(authored,"Reference existing moral-event trailer"); authored.trailer=identity;
                EditorSceneManager.MarkSceneDirty(selected.scene);
            }
            Selection.activeObject=authored;
        }
    }

    [CustomEditor(typeof(TruckTaxiMoralTrailerEvent))]
    public sealed class TruckTaxiMoralTrailerEventEditor : UnityEditor.Editor
    {
        private string feedback;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var authored=(TruckTaxiMoralTrailerEvent)target;
            EditorGUILayout.HelpBox("Use existing trailer and destination objects. This helper does not spawn vehicles, save trailer poses, create voices or regenerate a scene.",MessageType.Info);
            using(new EditorGUI.DisabledScope(Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorUtility.IsPersistent(authored)))
            {
                if(GUILayout.Button("Fill Empty Authored Choice Options")) FillOptions(authored);
                if(GUILayout.Button("Fill Empty Destination Bindings")) FillDestinations(authored);
                if(GUILayout.Button("Validate Existing Physical Content"))
                    feedback=authored.ValidatePhysical(out var reason) ? "Authored references are valid. Live coupling and road routing still require Play Mode validation." : reason;
            }
            if(!string.IsNullOrEmpty(feedback)) EditorGUILayout.HelpBox(feedback,MessageType.Info);
        }
        private void FillOptions(TruckTaxiMoralTrailerEvent authored)
        {
            var definition=authored.definition;
            if(definition==null || string.IsNullOrWhiteSpace(definition.eventId) || definition.eventId.Length>110 ||
                definition.options!=null && definition.options.Length!=0)
            { feedback="Assign a definition with a stable event ID and an empty options array. Existing options are never overwritten."; return; }
            Undo.RecordObject(definition,"Author abstract trailer choices");
            definition.options=new[] {
                new TruckTaxiMoralChoiceOption { choiceId=TruckTaxiMoralChoiceDefinition.TurnIn,label="Turn them in",
                    completionObjectiveId=definition.eventId+".authority",effects=new TruckTaxiMoralChoiceEffects {
                        safety=5,crime=-5,reputation=5,authorityStanding=5,fareMultiplier=.01f } },
                new TruckTaxiMoralChoiceOption { choiceId=TruckTaxiMoralChoiceDefinition.Deliver,label="Deliver them to destination",
                    completionObjectiveId=definition.eventId+".contact",effects=new TruckTaxiMoralChoiceEffects {
                        safety=-5,crime=5,trafficPressure=5,shadyStanding=5,fareMultiplier=-.01f } }
            };
            EditorUtility.SetDirty(definition);
            feedback="Abstract choices added with zero payouts. Review authored effects, town, trailer ID and completion destinations before enabling.";
        }
        private void FillDestinations(TruckTaxiMoralTrailerEvent authored)
        {
            if(authored.destinations!=null && authored.destinations.Length!=0 || authored.definition==null ||
                !authored.definition.Validate(out _))
            { feedback="Use a valid choice definition and an empty destination array. Existing bindings are never overwritten."; return; }
            Undo.RecordObject(authored,"Author trailer destination bindings");
            authored.destinations=Array.ConvertAll(authored.definition.options,option=>new TruckTaxiMoralDestination {
                objectiveId=option.completionObjectiveId,displayName=option.label
            });
            EditorUtility.SetDirty(authored); EditorSceneManager.MarkSceneDirty(authored.gameObject.scene);
            feedback="IDs copied. Assign existing destination transforms and review the bounded arrival radii. No positions were invented.";
        }
    }
}
