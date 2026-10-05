using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Editor
{
    public sealed class TruckTaxiCustomContentWindow : EditorWindow
    {
        private ObjectField selected, configuration, phases;
        private IntegerField variant;
        private VisualElement inspector;
        private TextField report;
        [MenuItem("Truck Taxi/Custom Content")]
        [MenuItem("Truck Taxi/Private Event Content")]
        public static void Open() => GetWindow<TruckTaxiCustomContentWindow>("Truck Taxi Content");
        public void CreateGUI()
        {
            var scroll = new ScrollView(); rootVisualElement.Add(scroll);
            void Command(string text, Action action) => scroll.Add(new Button(action) { text = text });
            Command("CREATE CUSTOM PRESENTATION PROFILE", () => Create<TruckTaxiCustomPresentationProfile>());
            Command("CREATE CUSTOM AUDIO PROFILE", () => Create<TruckTaxiCustomReactionAudioProfile>());
            Command("CREATE CUSTOM ITEM INTERACTION", () => Create<TruckTaxiCustomItemInteractionDefinition>());
            Command("CREATE INTERACTION PHASE PROFILE", () => Create<TruckTaxiCustomInteractionProfile>());
            selected = new ObjectField("Content asset") { objectType = typeof(ScriptableObject), allowSceneObjects = false };
            selected.RegisterValueChangedCallback(_ => ShowInspector()); scroll.Add(selected);
            inspector = new VisualElement(); scroll.Add(inspector);
            Command("PREVIEW AUDIO / PRESENTATION (PLAY MODE)", Preview);
            Command("STOP PREVIEW", () => TruckTaxiBootstrap.Instance?.CustomContent?.Cancel());
            Command("REMOVE MISSING AUDIO REFERENCES", RemoveMissing);
            scroll.Add(new Label("Companion-event assignments"));
            phases = new ObjectField("Interaction phase profile") { objectType = typeof(TruckTaxiCustomInteractionProfile), allowSceneObjects = false }; scroll.Add(phases);
            configuration = new ObjectField("Global Truck Taxi configuration") { objectType = typeof(TruckTaxiConfiguration), allowSceneObjects = false }; scroll.Add(configuration);
            variant = new IntegerField("Companion variant (-1 = global)") { value = -1 }; scroll.Add(variant);
            Command("ASSIGN PROFILE TO PRIVATE EVENT", Assign);
            Command("VALIDATE REFERENCES", Validate);
            Command("SCAN CUSTOM CONTENT", Scan);
            report = new TextField("Content status") { multiline = true, isReadOnly = true }; report.style.minHeight = 150; scroll.Add(report);
        }
        private void Create<T>() where T : ScriptableObject
        {
            string path = EditorUtility.SaveFilePanelInProject("Create custom content", typeof(T).Name, "asset", "Choose a project-owned asset location.", "Assets/LWS/TruckTaxi");
            if (string.IsNullOrEmpty(path)) return;
            var asset = CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); AssetDatabase.SaveAssetIfDirty(asset);
            selected.value = asset; Selection.activeObject = asset;
        }
        private void ShowInspector()
        {
            inspector.Clear();
            if (selected.value != null) inspector.Add(new InspectorElement(selected.value));
        }
        private void Assign()
        {
            if (phases.value == null) { report.value = "Select an interaction phase profile first. Empty content inside it is valid."; return; }
            Object target = configuration.value;
            if (target == null) { report.value = "Select the Truck Taxi configuration used by your scene."; return; }
            if (variant.value < -1 || variant.value >= TruckTaxiCompanionPopulationPolicy.VariantCount)
            { report.value = "Variant must be -1 (global) or 0 to " + (TruckTaxiCompanionPopulationPolicy.VariantCount - 1) + "."; return; }
            var serialized = new SerializedObject(target);
            var field = serialized.FindProperty("customCompanionInteraction");
            if (variant.value >= 0)
            {
                var entries = serialized.FindProperty("customCompanionVariantInteractions");
                int oldSize = entries.arraySize;
                entries.arraySize = Mathf.Max(oldSize, variant.value + 1);
                for (int i = oldSize; i < entries.arraySize; i++) entries.GetArrayElementAtIndex(i).objectReferenceValue = null;
                field = entries.GetArrayElementAtIndex(variant.value);
            }
            field.objectReferenceValue = phases.value; serialized.ApplyModifiedProperties(); EditorUtility.SetDirty(target); AssetDatabase.SaveAssetIfDirty(target);
            report.value = "Assigned " + phases.value.name + " to " + target.name + ". The existing companion event invokes these phases.";
        }
        private void Preview()
        {
            var runner = TruckTaxiBootstrap.Instance?.CustomContent;
            if (!Application.isPlaying || runner == null) { report.value = "Start Truck Taxi Play Mode to preview through the actual runtime playback path."; return; }
            if (selected.value is TruckTaxiCustomItemInteractionDefinition item)
            { report.value = runner.TryExecuteItem(item, out var reason) ? "Interaction accepted." : reason; return; }
            if (selected.value is TruckTaxiCustomInteractionProfile interaction)
            {
                var host = TruckTaxiBootstrap.Instance;
                runner.Begin(interaction, host.Player != null ? host.Player.GetComponent<TruckTaxiDriverPresentation>()?.Occupant : null,
                    host.Companions?.ActiveCompanionVisual ?? host.Passengers?.PrimaryActor?.transform);
            }
            else runner.Preview(selected.value as TruckTaxiCustomPresentationProfile, selected.value as TruckTaxiCustomReactionAudioProfile);
            report.value = "Runtime preview started. STOP PREVIEW restores visibility and releases its pause.";
        }
        private void RemoveMissing()
        {
            if (!(selected.value is TruckTaxiCustomReactionAudioProfile audio)) return;
            Undo.RecordObject(audio, "Remove missing custom audio");
            audio.startClips = Clean(audio.startClips); audio.loopClips = Clean(audio.loopClips);
            audio.accentClips = Clean(audio.accentClips); audio.completionClips = Clean(audio.completionClips);
            EditorUtility.SetDirty(audio); AssetDatabase.SaveAssetIfDirty(audio); ShowInspector();
        }
        private static AudioClip[] Clean(AudioClip[] clips) => (clips ?? Array.Empty<AudioClip>()).Where(c => c != null).Distinct().ToArray();
        private void Validate()
        {
            var lines = new List<string>(); var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var guid in AssetDatabase.FindAssets("t:TruckTaxiCustomPresentationProfile", new[] { "Assets/LWS/TruckTaxi" }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<TruckTaxiCustomPresentationProfile>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var problem in asset.Validate()) lines.Add(asset.name + ": " + problem);
                if (!ids.Add(asset.stableId ?? "")) lines.Add(asset.name + ": Duplicate stable ID.");
                if (asset.driverAnimation == null && asset.passengerAnimation == null && asset.timeline == null && asset.presentationPrefab == null)
                    lines.Add(asset.name + ": Empty presentation; existing event fallback retained.");
            }
            foreach (var type in new[] { typeof(TruckTaxiCustomReactionAudioProfile), typeof(TruckTaxiCustomInteractionProfile), typeof(TruckTaxiCustomItemInteractionDefinition) })
            {
                ids.Clear();
                foreach (var guid in AssetDatabase.FindAssets("t:" + type.Name, new[] { "Assets/LWS/TruckTaxi" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    var serialized = new SerializedObject(asset);
                    string id = serialized.FindProperty("stableId").stringValue;
                    if (string.IsNullOrWhiteSpace(id)) lines.Add(path + ": Missing stable ID.");
                    else if (!ids.Add(id)) lines.Add(path + ": Duplicate stable ID " + id + ".");
                    if (asset is TruckTaxiCustomItemInteractionDefinition item)
                    {
                        if (TruckTaxiNeedsItems.FindByStableId(item.requiredItemStableId)?.PlayerOwnable != true) lines.Add(path + ": Unknown or non-ownable inventory stable ID.");
                        if (item.consumeQuantity < 0 || item.rewardCashCents < 0 || item.minimumAge < 0 || !float.IsFinite(item.satisfactionDelta)) lines.Add(path + ": Invalid item action values.");
                    }
                    if (asset is TruckTaxiCustomReactionAudioProfile audio &&
                        (!float.IsFinite(audio.volume) || !float.IsFinite(audio.minimumInterval) || !float.IsFinite(audio.maximumInterval) ||
                         audio.minimumInterval < 0 || audio.maximumInterval < audio.minimumInterval || audio.pitchRange.x > audio.pitchRange.y))
                        lines.Add(path + ": Check audio volume, interval and pitch range.");
                    var property = serialized.GetIterator();
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                            lines.Add(path + ": Missing asset at " + property.propertyPath + ".");
                }
            }
            lines.Add("Null audio, animation and timeline references are optional. Scene-specific anchor and Animator availability is checked at playback.");
            report.value = string.Join("\n", lines);
        }
        private void Scan()
        {
            const string root = "Assets/LWS/TruckTaxi/CustomContent";
            if (!AssetDatabase.IsValidFolder(root)) { report.value = "No CustomContent folder yet. Import authorized content there; scanning never auto-assigns it."; return; }
            var lines = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var query in new[] { "t:AudioClip", "t:AnimationClip", "t:TimelineAsset", "t:Prefab" })
                foreach (var guid in AssetDatabase.FindAssets(query, new[] { root })) lines.Add(AssetDatabase.GUIDToAssetPath(guid));
            report.value = lines.Count == 0 ? "No imported compatible content." : string.Join("\n", lines);
        }
    }
}
