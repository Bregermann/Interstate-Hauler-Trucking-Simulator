#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom
{
    /// <summary>Explicit, project-owned asset authoring. Never runs on import or in gameplay.</summary>
    public static partial class DragonSpeciesAuthoring
    {
        public const string CatalogPath = "Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset";
        public const string DragonRoot = "Assets/TruckDragonKingdom/PrivateLicensedDragons/Monsters Ultimate Pack 02 Cute Series/";
        public const string AnimalRoot = "Assets/TruckDragonKingdom/PrivateLicensedAnimalController/Malbers Animations/";
        const string AuthoredRoot = "Assets/TruckDragonKingdom/Animations/Authored";

        [MenuItem("LWS/Truck Sports/Dragon Kingdom/Create Species Catalog")]
        static void CreateCatalogMenu() => CreateCatalog();

        public static DragonSpeciesCatalog CreateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>(CatalogPath);
            if (catalog != null)
            {
                // Selected-action upgrades and designer edits own existing data.
                // Repeating initial authoring is a read-only validation operation.
                ReportCatalogIssues(catalog);
                Debug.Log("Existing dragon species catalog preserved; selected changes require their explicit authoring command.", catalog);
                return catalog;
            }
            EnsureFolder("Assets/TruckDragonKingdom/Settings");
            EnsureFolder(AuthoredRoot);
            catalog = ScriptableObject.CreateInstance<DragonSpeciesCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            catalog.species = new[]
            {
                Build("Dragon Inferno", "dragon-inferno", DragonSpeciesRole.Ground | DragonSpeciesRole.Flying, 5, 32, DragonTemperament.Territorial),
                Build("Dragon Fire", "dragon-fire", DragonSpeciesRole.Flying, 7, 36, DragonTemperament.Protective),
                Build("Dragon Spark", "dragon-spark", DragonSpeciesRole.Ground | DragonSpeciesRole.Baby, 1, 0, DragonTemperament.Curious)
            };
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            ReportCatalogIssues(catalog);
            Debug.Log("Authored owned dragon catalog. Bone-overlay clips require actual rendered verification; no visual acceptance is implied.", catalog);
            return catalog;
        }

        static void ReportCatalogIssues(DragonSpeciesCatalog catalog)
        {
            var issues = new List<string>();
            catalog.CollectValidationIssues(issues);
            foreach (string issue in issues) Debug.LogWarning("Dragon species authoring: " + issue, catalog);
        }

        static DragonSpeciesDefinition Build(string name, string id, DragonSpeciesRole roles, float scale, float airSpeed, DragonTemperament temperament)
        {
            string folder = DragonRoot + name + " Cute Series/";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "Prefabs/" + name + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Owned dragon intake is required: " + folder);
            var entry = new DragonSpeciesDefinition
            {
                id = id, displayName = name, visualPrefab = prefab, supportedRoles = roles,
                visualScale = scale, groundSpeed = id == "dragon-spark" ? 3 : 7,
                flightSpeed = airSpeed, flightAcceleration = id == "dragon-fire" ? 11 : 8,
                turnRate = id == "dragon-fire" ? 68 : 54, bankAngle = 30,
                maximumStamina = id == "dragon-inferno" ? 130 : id == "dragon-fire" ? 80 : 30,
                element = DragonElement.Fire, temperament = temperament,
                mouthBone = "RigJaw", leftGripBone = "RigLArmPalm", rightGripBone = "RigRArmPalm", saddleBone = "RigSpine2",
                breathEffect = AssetDatabase.LoadAssetAtPath<GameObject>(AnimalRoot + "Common/Particles/Prefabs/FireBreath.prefab"),
                sounds = LoadSounds()
            };
            var sources = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);
            foreach (string guid in AssetDatabase.FindAssets(string.Empty, new[] { folder + "FBX" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string filename = System.IO.Path.GetFileNameWithoutExtension(path);
                int at = filename.IndexOf('@');
                if (at < 0) continue;
                var source = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
                if (source != null) sources[filename.Substring(at + 1)] = source;
            }
            var mappings = new List<DragonAnimationMapping>();
            AddSource(mappings, sources, DragonAnimationAction.GroundIdle, "Idle", true);
            AddSource(mappings, sources, DragonAnimationAction.Walk, "Walk Forward In Place", true);
            AddSource(mappings, sources, DragonAnimationAction.Run, "Run Forward In Place", true);
            AddSource(mappings, sources, DragonAnimationAction.TurnLeft, "Turn Left", true);
            AddSource(mappings, sources, DragonAnimationAction.TurnRight, "Turn Right", true);
            AddSource(mappings, sources, DragonAnimationAction.Fly, "Fly Forward In Place", true);
            AddSource(mappings, sources, DragonAnimationAction.FlyIdle, "Fly Idle", true);
            AddSource(mappings, sources, DragonAnimationAction.Breath, "Cast Spell", false);
            AddSource(mappings, sources, DragonAnimationAction.TailAttack, "Tail Attack", false);
            AddSource(mappings, sources, DragonAnimationAction.Damage, "Take Damage", false);
            AddSource(mappings, sources, DragonAnimationAction.Defeat, "Die", false);
            AddSource(mappings, sources, DragonAnimationAction.Retreat, (roles & DragonSpeciesRole.Flying) != 0 ? "Fly Forward In Place" : "Run Forward In Place", true);

            var instance = UnityEngine.Object.Instantiate(prefab);
            instance.hideFlags = HideFlags.HideAndDontSave;
            instance.SetActive(false);
            try
            {
                bool ground = (roles & DragonSpeciesRole.Ground) != 0;
                bool flying = (roles & DragonSpeciesRole.Flying) != 0;
                AnimationClip idle = FindSource(sources, ground ? "Idle" : "Fly Idle");
                AnimationClip flight = FindSource(sources, "Fly Forward In Place");
                AnimationClip bite = FindSource(sources, "Bite Attack");
                AnimationClip cast = FindSource(sources, "Cast Spell") ?? bite;
                AnimationClip groundPose = FindSource(sources, "Idle") ?? FindSource(sources, "Stomp Ground Attack");
                AddAuthored(mappings, instance, entry, idle, DragonAnimationAction.LookAround, 3.2f, true);
                AddAuthored(mappings, instance, entry, cast, DragonAnimationAction.Roar, 2.4f, false);
                AddAuthored(mappings, instance, entry, bite, DragonAnimationAction.Eat, 2.4f, false);
                if (ground)
                {
                    AddAuthored(mappings, instance, entry, idle, DragonAnimationAction.Sleep, 4, true);
                    AddAuthored(mappings, instance, entry, idle, DragonAnimationAction.Wake, 2.6f, false);
                    AddAuthored(mappings, instance, entry, idle, DragonAnimationAction.InspectEgg, 2.5f, false);
                    AddAuthored(mappings, instance, entry, idle, DragonAnimationAction.Nest, 3, true);
                    AddAuthored(mappings, instance, entry, idle, DragonAnimationAction.Perch, 3, true);
                }
                if (flying)
                {
                    AddAuthored(mappings, instance, entry, flight, DragonAnimationAction.Takeoff, 1.8f, false, groundPose);
                    AddAuthored(mappings, instance, entry, flight, DragonAnimationAction.Land, 1.8f, false, groundPose);
                    AddAuthored(mappings, instance, entry, flight, DragonAnimationAction.Glide, 3, true);
                    AddAuthored(mappings, instance, entry, flight, DragonAnimationAction.BankLeft, 2, true);
                    AddAuthored(mappings, instance, entry, flight, DragonAnimationAction.BankRight, 2, true);
                    AddAuthored(mappings, instance, entry, flight, DragonAnimationAction.Dive, 2, true);
                    AddAuthored(mappings, instance, entry, flight, DragonAnimationAction.Grab, 1.4f, false);
                    AddAuthored(mappings, instance, entry, flight, DragonAnimationAction.Carry, 2, true);
                    AddAuthored(mappings, instance, entry, flight, DragonAnimationAction.Drop, 1.2f, false);
                    AddAuthored(mappings, instance, entry, flight, DragonAnimationAction.ClawAttack, 1.2f, false);
                }
                if ((roles & DragonSpeciesRole.Baby) != 0)
                    AddAuthored(mappings, instance, entry, FindSource(sources, "Jump In Place") ?? idle, DragonAnimationAction.Hatch, 2.8f, false);
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
            entry.animations = mappings.ToArray();
            // Advanced roles are enabled only when all configured action clips exist.
            // Existence is authoring readiness; rendered/motor verification is separate.
            if (id != "dragon-spark" && entry.Supports(DragonSpeciesRole.Ground) && Has(entry, DragonAnimationAction.Sleep, DragonAnimationAction.Wake, DragonAnimationAction.Roar)) entry.supportedRoles |= DragonSpeciesRole.HoardGuardian;
            if (entry.Supports(DragonSpeciesRole.Flying) && Has(entry, DragonAnimationAction.Takeoff, DragonAnimationAction.Land, DragonAnimationAction.Glide, DragonAnimationAction.BankLeft, DragonAnimationAction.BankRight, DragonAnimationAction.Dive)) entry.supportedRoles |= DragonSpeciesRole.RiderMount;
            if (entry.Supports(DragonSpeciesRole.Ground | DragonSpeciesRole.Flying) && Has(entry, DragonAnimationAction.InspectEgg, DragonAnimationAction.Nest, DragonAnimationAction.Takeoff, DragonAnimationAction.Land)) entry.supportedRoles |= DragonSpeciesRole.EggGuardian;
            if (entry.Supports(DragonSpeciesRole.Flying) && Has(entry, DragonAnimationAction.Grab, DragonAnimationAction.Carry, DragonAnimationAction.Drop, DragonAnimationAction.Eat)) entry.supportedRoles |= DragonSpeciesRole.SheepCarrier;
            return entry;
        }

        static bool Has(DragonSpeciesDefinition species, params DragonAnimationAction[] actions) => actions.All(species.HasAnimation);
        static AnimationClip FindSource(Dictionary<string, AnimationClip> sources, string name) => sources.TryGetValue(name, out var source) ? source : null;

        static void AddSource(List<DragonAnimationMapping> mappings, Dictionary<string, AnimationClip> sources, DragonAnimationAction action, string name, bool loop)
        {
            if (!sources.TryGetValue(name, out var source)) return;
            mappings.Add(new DragonAnimationMapping { action = action, clip = source, loop = loop, playbackSpeed = 1, sourceNote = "OWNED ORIGINAL: " + AssetDatabase.GetAssetPath(source) + "; rendered verification pending." });
        }

        static void AddAuthored(List<DragonAnimationMapping> mappings, GameObject model, DragonSpeciesDefinition species, AnimationClip source, DragonAnimationAction action, float duration, bool loop, AnimationClip groundPose = null)
        {
            if (source == null || !RigSupports(model, action)) return;
            if ((action == DragonAnimationAction.Takeoff || action == DragonAnimationAction.Land) && groundPose == null) return;
            string folder = AuthoredRoot + "/" + species.id;
            EnsureFolder(folder);
            string path = folder + "/" + action + ".anim";
            var previous = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            var output = previous;
            // Catalog creation fills genuinely missing actions. Existing saved
            // derivatives require an explicit selected-action upgrade.
            if (output == null)
            {
                output = CreateRigAnimation(model, source, action, duration, loop, groundPose);
                output.name = species.id + " " + action;
                AssetDatabase.CreateAsset(output, path);
            }
            mappings.Add(new DragonAnimationMapping { action = action, clip = output, loop = loop, playbackSpeed = 1, sourceNote = "LWS AUTHORED RIG OVERLAY on " + AssetDatabase.GetAssetPath(source) + "; real source curves + sampled bone rotation/pose curves; rendered verification pending." });
        }

        static bool RigSupports(GameObject model, DragonAnimationAction action)
        {
            var names = new HashSet<string>(model.GetComponentsInChildren<Transform>(true).Select(t => t.name), StringComparer.Ordinal);
            bool wings = names.Contains("RigLWing01") && names.Contains("RigRWing01");
            bool grip = names.Contains("RigLArmPalm") && names.Contains("RigRArmPalm");
            switch (action)
            {
                case DragonAnimationAction.Takeoff: case DragonAnimationAction.Land: case DragonAnimationAction.Glide:
                case DragonAnimationAction.BankLeft: case DragonAnimationAction.BankRight: case DragonAnimationAction.Dive:
                    return wings && names.Contains("RigPelvis");
                case DragonAnimationAction.Grab: case DragonAnimationAction.Carry: case DragonAnimationAction.Drop: case DragonAnimationAction.ClawAttack:
                    return wings && grip;
                case DragonAnimationAction.Eat: return grip && names.Contains("RigJaw") && names.Contains("RigHead");
                default: return names.Contains("RigPelvis") && names.Contains("RigHead") && names.Contains("RigJaw");
            }
        }

        public static AnimationClip CreateRigActionClip(GameObject model, AnimationClip source, DragonAnimationAction action, float duration, bool loop)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || model == null || source == null || EditorUtility.IsPersistent(model)
                || !float.IsFinite(duration) || duration <= 0)
                throw new InvalidOperationException("Explicit stopped authoring requires a temporary actual rig instance, real source clip and positive finite duration.");
            return CreateRigAnimation(model, source, action, duration, loop, null);
        }

        static AnimationClip CreateRigAnimation(GameObject model, AnimationClip source, DragonAnimationAction action, float duration, bool loop, AnimationClip groundPose)
        {
            if ((action == DragonAnimationAction.Sleep || action == DragonAnimationAction.Wake) && SupportsInfernoRestPose(model))
                return CreateInfernoRestClip(model, source, action, duration, loop);
            var output = new AnimationClip { frameRate = 30, legacy = false, wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever };
            var bindings = AnimationUtility.GetCurveBindings(source);
            if (bindings.Length == 0) throw new InvalidOperationException("Owned source has no accessible curves: " + AssetDatabase.GetAssetPath(source));
            float timeScale = duration / Mathf.Max(source.length, 0.001f);
            foreach (var binding in bindings)
            {
                var curve = AnimationUtility.GetEditorCurve(source, binding);
                var keys = curve.keys;
                for (int i = 0; i < keys.Length; i++)
                {
                    keys[i].time *= timeScale;
                    keys[i].inTangent /= timeScale;
                    keys[i].outTangent /= timeScale;
                }
                curve.keys = keys;
                AnimationUtility.SetEditorCurve(output, binding, curve);
            }
            var bones = model.GetComponentsInChildren<Transform>(true).Where(t => IsActionBone(t.name, action)).ToArray();
            var holdPose = new Dictionary<Transform, Quaternion>();
            var groundedRotations = new Dictionary<Transform, Quaternion>();
            var groundedPositions = new Dictionary<Transform, Vector3>();
            if (groundPose != null)
            {
                // Fire's real Stomp clip supplies its neutral grounded endpoint;
                // Inferno has a real ground Idle. Neither is a fabricated pose.
                groundPose.SampleAnimation(model, groundPose.length * 0.99f);
                foreach (var bone in bones)
                {
                    groundedRotations[bone] = bone.localRotation;
                    groundedPositions[bone] = bone.localPosition;
                }
            }
            if (action == DragonAnimationAction.Glide)
            {
                var all = model.GetComponentsInChildren<Transform>(true);
                var left = all.LastOrDefault(t => t.name.StartsWith("RigLWing", StringComparison.Ordinal) && !t.name.EndsWith("Gizmo", StringComparison.Ordinal));
                var right = all.LastOrDefault(t => t.name.StartsWith("RigRWing", StringComparison.Ordinal) && !t.name.EndsWith("Gizmo", StringComparison.Ordinal));
                float widest = -1, phase = 0;
                for (int i = 0; i < 24; i++)
                {
                    source.SampleAnimation(model, source.length * i / 24f);
                    float span = left != null && right != null ? (left.position - right.position).sqrMagnitude : 0;
                    if (span > widest) { widest = span; phase = i / 24f; }
                }
                source.SampleAnimation(model, source.length * phase);
                foreach (var bone in bones.Where(t => t.name.Contains("Wing"))) holdPose[bone] = bone.localRotation;
            }
            int sampleCount = Mathf.Max(2, Mathf.CeilToInt(duration * 30));
            var rotations = bones.ToDictionary(bone => bone, bone => new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve(), new AnimationCurve() });
            var pelvis = bones.FirstOrDefault(b => b.name == "RigPelvis");
            var pelvisPosition = new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };
            float modelHeight = model.GetComponentsInChildren<Renderer>(true).Select(r => r.bounds.size.y).DefaultIfEmpty(1).Max();
            for (int sample = 0; sample <= sampleCount; sample++)
            {
                float normalized = sample / (float)sampleCount;
                source.SampleAnimation(model, normalized * source.length);
                foreach (var bone in bones)
                {
                    Quaternion baseline = holdPose.TryGetValue(bone, out var held) ? held : bone.localRotation;
                    if (groundedRotations.TryGetValue(bone, out var groundRotation))
                        baseline = action == DragonAnimationAction.Takeoff ? Quaternion.Slerp(groundRotation, baseline, Mathf.SmoothStep(0, 1, normalized)) : Quaternion.Slerp(baseline, groundRotation, Mathf.SmoothStep(0, 1, normalized));
                    Quaternion rotation = baseline * Quaternion.Euler(BoneOffset(bone.name, action, normalized));
                    var curves = rotations[bone];
                    float time = normalized * duration;
                    curves[0].AddKey(time, rotation.x); curves[1].AddKey(time, rotation.y);
                    curves[2].AddKey(time, rotation.z); curves[3].AddKey(time, rotation.w);
                }
                if (pelvis != null)
                {
                    Vector3 position = pelvis.localPosition;
                    if (groundedPositions.TryGetValue(pelvis, out var groundPosition))
                        position = action == DragonAnimationAction.Takeoff ? Vector3.Lerp(groundPosition, position, Mathf.SmoothStep(0, 1, normalized)) : Vector3.Lerp(position, groundPosition, Mathf.SmoothStep(0, 1, normalized));
                    float fold = action == DragonAnimationAction.Sleep ? 1 : action == DragonAnimationAction.Wake ? 1 - Mathf.SmoothStep(0, 1, normalized) : 0;
                    if (fold > 0 && pelvis.parent != null)
                        position += pelvis.parent.InverseTransformVector(-model.transform.up * modelHeight * 0.12f * fold);
                    pelvisPosition[0].AddKey(normalized * duration, position.x);
                    pelvisPosition[1].AddKey(normalized * duration, position.y);
                    pelvisPosition[2].AddKey(normalized * duration, position.z);
                }
            }
            if (pelvis != null)
            {
                string path = AnimationUtility.CalculateTransformPath(pelvis, model.transform);
                string[] axes = { "x", "y", "z" };
                for (int i = 0; i < axes.Length; i++) AnimationUtility.SetEditorCurve(output, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition." + axes[i]), pelvisPosition[i]);
            }
            foreach (var bone in bones)
            {
                string path = AnimationUtility.CalculateTransformPath(bone, model.transform);
                // Remove Euler tracks for the same bone before installing quaternion tracks.
                foreach (var binding in bindings.Where(b => b.path == path && b.type == typeof(Transform) && b.propertyName.IndexOf("Euler", StringComparison.OrdinalIgnoreCase) >= 0))
                    AnimationUtility.SetEditorCurve(output, binding, null);
                var curves = rotations[bone];
                string[] axes = { "x", "y", "z", "w" };
                for (int i = 0; i < axes.Length; i++) AnimationUtility.SetEditorCurve(output, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation." + axes[i]), curves[i]);
            }
            var settings = AnimationUtility.GetAnimationClipSettings(output);
            settings.loopTime = loop; settings.loopBlend = loop;
            AnimationUtility.SetAnimationClipSettings(output, settings);
            output.EnsureQuaternionContinuity();
            return output;
        }

        static bool IsActionBone(string bone, DragonAnimationAction action)
        {
            if (!bone.StartsWith("Rig", StringComparison.Ordinal) || bone.EndsWith("Gizmo", StringComparison.Ordinal)) return false;
            return bone == "RigPelvis" || bone == "RigHead" || bone == "RigJaw" || bone.StartsWith("RigNeck", StringComparison.Ordinal) ||
                bone.StartsWith("RigSpine", StringComparison.Ordinal) || bone == "RigRibcage" || bone.StartsWith("RigTail", StringComparison.Ordinal) ||
                bone.Contains("Wing") || bone.StartsWith("RigLArm", StringComparison.Ordinal) || bone.StartsWith("RigRArm", StringComparison.Ordinal) ||
                bone.StartsWith("RigLLeg", StringComparison.Ordinal) || bone.StartsWith("RigRLeg", StringComparison.Ordinal);
        }

        // Local bone offsets layer onto actual sampled vendor rotations. Motion
        // authority and attachment timing remain in the runtime motor/interaction.
        static Vector3 BoneOffset(string bone, DragonAnimationAction action, float t)
        {
            float cycle = Mathf.Sin(t * Mathf.PI * 2);
            float breath = Mathf.Sin(t * Mathf.PI * 4);
            float pulse = Mathf.Sin(t * Mathf.PI);
            float rise = Mathf.SmoothStep(0, 1, t);
            bool neck = bone.StartsWith("RigNeck", StringComparison.Ordinal);
            bool spine = bone.StartsWith("RigSpine", StringComparison.Ordinal) || bone == "RigRibcage";
            bool tail = bone.StartsWith("RigTail", StringComparison.Ordinal);
            bool wing = bone.Contains("Wing");
            bool left = bone.StartsWith("RigL", StringComparison.Ordinal);
            float side = left ? -1 : 1;
            bool upperArm = bone == "RigLArm1" || bone == "RigRArm1";
            bool elbow = bone == "RigLArm2" || bone == "RigRArm2";
            bool palm = bone.EndsWith("Palm", StringComparison.Ordinal);
            bool finger = bone.Contains("Thumb") || bone.Contains("Index") || bone.Contains("Pinky");
            bool knee = bone == "RigLLeg2" || bone == "RigRLeg2";
            Vector3 offset = Vector3.zero;
            switch (action)
            {
                case DragonAnimationAction.Sleep:
                case DragonAnimationAction.Wake:
                    float folded = action == DragonAnimationAction.Sleep ? 1 : 1 - rise;
                    if (neck) offset = new Vector3(18 * folded + breath * 0.7f, side * 2 * folded, 0);
                    if (bone == "RigHead") offset = new Vector3(16 * folded, 15 * folded, 0);
                    if (spine) offset = new Vector3(-6 * folded + breath * 0.6f, 0, 0);
                    if (knee) offset = new Vector3(28 * folded, 0, 0);
                    if (upperArm) offset = new Vector3(18 * folded, 0, side * 10 * folded);
                    if (elbow) offset = new Vector3(32 * folded, 0, 0);
                    if (tail) offset = new Vector3(0, 5 * folded + breath * 0.2f, 0);
                    if (wing) offset = new Vector3(0, 0, side * 3 * folded + breath * 0.2f);
                    if (action == DragonAnimationAction.Wake && bone == "RigJaw") offset = new Vector3(-18 * pulse, 0, 0);
                    break;
                case DragonAnimationAction.LookAround:
                    if (neck) offset = new Vector3(2 * breath, 7 * cycle, 0);
                    if (bone == "RigHead") offset = new Vector3(4 * breath, 18 * cycle, 3 * breath);
                    if (tail) offset = new Vector3(0, 2 * cycle, 0);
                    break;
                case DragonAnimationAction.Roar:
                    if (neck) offset = new Vector3(-12 * pulse, 0, 0);
                    if (bone == "RigHead") offset = new Vector3(-18 * pulse, 4 * breath * pulse, 0);
                    if (bone == "RigJaw") offset = new Vector3(-22 * pulse, 0, 0);
                    if (spine) offset = new Vector3(-5 * pulse, 0, 0);
                    if (wing) offset = new Vector3(0, 0, side * 5 * pulse);
                    break;
                case DragonAnimationAction.Takeoff:
                case DragonAnimationAction.Land:
                    float phase = action == DragonAnimationAction.Takeoff ? t : 1 - t;
                    float crouch = phase < 0.35f ? Mathf.Sin(phase / 0.35f * Mathf.PI * 0.5f) : Mathf.Cos((phase - 0.35f) / 0.65f * Mathf.PI * 0.5f);
                    if (knee) offset = new Vector3(32 * crouch, 0, 0);
                    if (bone == "RigPelvis") offset = new Vector3(-12 * crouch, 0, 0);
                    if (neck) offset = new Vector3(-6 * crouch, 0, 0);
                    if (wing) offset = new Vector3(0, 0, side * 12 * crouch);
                    if (upperArm) offset = new Vector3(12 * crouch, 0, 0);
                    break;
                case DragonAnimationAction.Glide:
                    if (wing) offset = new Vector3(cycle * 1.1f, 0, side * breath * 0.7f);
                    if (neck) offset = new Vector3(-3 + cycle * 0.6f, 0, 0);
                    if (tail) offset = new Vector3(0, cycle, 0);
                    break;
                case DragonAnimationAction.BankLeft:
                case DragonAnimationAction.BankRight:
                    float direction = action == DragonAnimationAction.BankLeft ? -1 : 1;
                    if (wing) offset = new Vector3(direction * side * 8, 0, direction * 6 + cycle * 0.5f);
                    if (neck) offset = new Vector3(0, direction * 5, 0);
                    if (tail) offset = new Vector3(0, -direction * 2, 0);
                    break;
                case DragonAnimationAction.Dive:
                    if (wing) offset = new Vector3(9, side * 5, side * 8);
                    if (neck) offset = new Vector3(-9 + cycle * 0.5f, 0, 0);
                    if (tail) offset = new Vector3(2 + cycle * 0.3f, 0, 0);
                    break;
                case DragonAnimationAction.Grab:
                case DragonAnimationAction.Carry:
                case DragonAnimationAction.Drop:
                case DragonAnimationAction.ClawAttack:
                    float grip = action == DragonAnimationAction.Grab ? rise : action == DragonAnimationAction.Drop ? 1 - rise : action == DragonAnimationAction.ClawAttack ? pulse : 1;
                    if (upperArm) offset = new Vector3(30 * grip, side * 8 * grip, side * 12 * grip);
                    if (elbow) offset = new Vector3(-28 * grip, 0, 0);
                    if (palm) offset = new Vector3(18 * grip, 0, side * 8 * grip);
                    if (finger) offset = new Vector3(30 * grip, 0, 0);
                    if (neck) offset = new Vector3(5 * grip, 0, 0);
                    break;
                case DragonAnimationAction.Eat:
                    if (upperArm) offset = new Vector3(-28 * pulse, side * -12 * pulse, side * 10 * pulse);
                    if (elbow) offset = new Vector3(55 * pulse, 0, 0);
                    if (palm) offset = new Vector3(-12 * pulse, 0, 0);
                    if (neck) offset = new Vector3(8 * pulse, 0, 0);
                    if (bone == "RigJaw") offset = new Vector3(-8 * Mathf.Abs(breath) * pulse, 0, 0);
                    break;
                case DragonAnimationAction.InspectEgg:
                case DragonAnimationAction.Nest:
                    float inspect = action == DragonAnimationAction.Nest ? 0.65f + 0.12f * cycle : pulse;
                    if (neck) offset = new Vector3(13 * inspect, 4 * cycle * inspect, 0);
                    if (bone == "RigHead") offset = new Vector3(20 * inspect, 8 * cycle * inspect, 0);
                    if (tail) offset = new Vector3(0, 3 * inspect, 0);
                    if (upperArm) offset = new Vector3(5 * inspect, 0, side * 4 * inspect);
                    break;
                case DragonAnimationAction.Hatch:
                    float curl = 1 - rise;
                    if (neck) offset = new Vector3(22 * curl + breath * 4 * curl, 0, 0);
                    if (bone == "RigHead") offset = new Vector3(24 * curl, 12 * breath * curl, 0);
                    if (spine) offset = new Vector3(12 * curl, cycle * 4 * curl, 0);
                    if (knee) offset = new Vector3(35 * curl, 0, 0);
                    if (upperArm) offset = new Vector3(28 * curl, 0, side * 15 * curl);
                    if (tail) offset = new Vector3(0, 12 * curl + breath * 2 * curl, 0);
                    if (bone == "RigJaw") offset = new Vector3(-15 * pulse, 0, 0);
                    break;
                case DragonAnimationAction.Perch:
                    if (neck) offset = new Vector3(-5 + breath * 0.6f, 5 * cycle, 0);
                    if (bone == "RigHead") offset = new Vector3(0, 10 * cycle, 0);
                    if (tail) offset = new Vector3(0, 3 + cycle, 0);
                    break;
            }
            return offset;
        }

        static AudioClip[] LoadSounds()
        {
            string fire = AnimalRoot + "Common/Audio/Fire/";
            string growl = "Assets/TruckDragonKingdom/PrivateLicensedAudioVfx/EpicSounds/";
            return new[]
            {
                AssetDatabase.LoadAssetAtPath<AudioClip>(growl + "Fantasy_Game_Creature_Longer_Low_Growl.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>(growl + "Fantasy_Game_Creature_Growl_Loud_Gritty.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>(growl + "Fantasy_Game_Creature_Growl_Loud_2.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>(fire + "Fire Throw.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>(fire + "Fire Shake.wav")
            }.Where(clip => clip != null).ToArray();
        }

        static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
