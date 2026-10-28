using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LWS.TruckCamping.Editor
{
    // Reuse the already reviewed CC0 archives retained for Base Jumping birds.
    // No network, executable or unrelated package members are imported.
    public static class WildernessAssetIntake
    {
        public const string Folder = "Assets/TruckCamping/ThirdParty/QuaterniusWilderness/";
        sealed class Model
        {
            public string archive, member, filename, sha, walk, idle;
        }
        static readonly Model[] Models =
        {
            new Model { archive = "animated-vol2.zip", member = "Animal Pack Vol.2 by @Quaternius/FBX/Dog.fbx", filename = "Dog.fbx", sha = "a7250b3c68857120e6da0fd9ea2cdb4a4493823b6afed371b7aa9208407af039", walk = "Walking", idle = "Idle" },
            new Model { archive = "animals-vol1.zip", member = "Animals Pack by Quaternius/FBX/Red Fox.fbx", filename = "Red Fox.fbx", sha = "a76f570c6c63e04d0b43d62aaa79b949c74e0f4affa4649a62fdf06ae55106bd", walk = "ArmatureAction", idle = "ArmatureAction" },
            new Model { archive = "animals-vol1.zip", member = "Animals Pack by Quaternius/FBX/Fish.fbx", filename = "Fish.fbx", sha = "bae04f35d3fa216018073e017fc532de94e62b658573b183705f6bd8dc0c69ec", walk = "ArmatureAction", idle = "ArmatureAction" }
        };
        static string Digest(byte[] data) { using (var hash = SHA256.Create()) return string.Concat(hash.ComputeHash(data).Select(b => b.ToString("x2"))); }
        public static void BindOwnedURPPalette(GameObject instance,string label)
        {
            foreach(var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var source=materials[i];if(source==null)throw new InvalidOperationException("The original CC0 animal surface material is missing.");
                    var color=source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):source.HasProperty("_Color")?source.color:Color.white;
                    string name="CC0 "+label+" original palette "+source.name.Replace('/','_').Replace('\\','_');bool fresh=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckCamping/Materials/"+name+".mat")==null;
                    var owned=LWS.TruckMechanicalBull.Editor.ImpossibleBalanceArt.Material("TruckCamping",name,color,.2f);
                    if(fresh&&source.mainTexture!=null)owned.SetTexture("_BaseMap",source.mainTexture);materials[i]=owned;
                }
                renderer.sharedMaterials=materials;
            }
        }
        static string LocateArchive(string filename)
        {
            var directory = new DirectoryInfo(Directory.GetParent(Application.dataPath).FullName);
            while (directory != null)
            {
                string path = Path.Combine(directory.FullName, ".sports-validation", "AssetIntake", "Quaternius", filename);
                if (File.Exists(path)) return path; directory = directory.Parent;
            }
            throw new FileNotFoundException("The retained, previously reviewed Quaternius CC0 archive is required: " + filename);
        }
        [MenuItem("Truck Sports/Impossible Games/Import Reviewed CC0 Wilderness Dog Fox And Fish")]
        public static void ImportReviewedModels()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped, compiled Editor required.");
            var bytes = new byte[Models.Length][];
            for (int i = 0; i < Models.Length; i++)
            {
                var model = Models[i]; string target = Folder + model.filename;
                if (File.Exists(target))
                {
                    if (Digest(File.ReadAllBytes(target)) != model.sha) throw new InvalidOperationException("Preserve customized wilderness source: " + target);
                    var existing = AssetImporter.GetAtPath(target) as ModelImporter;
                    if (existing == null || existing.animationType != ModelImporterAnimationType.Generic || existing.avatarSetup!=ModelImporterAvatarSetup.CreateFromThisModel || !existing.importAnimation || existing.importCameras || existing.importLights)
                        throw new InvalidOperationException("Existing different wilderness importer preserved: " + target);
                    continue;
                }
                string path = LocateArchive(model.archive);
                string expected = model.archive == "animated-vol2.zip" ? "6d8f6789d65dbdb352ebad98a4209f26b41b384e87d81bc05e9ca76139db1248" : "b3268015beed5d1e1d745e004a37ca2ec215e6ac1bf37541f91c1908c58a2e39";
                if (Digest(File.ReadAllBytes(path)) != expected) throw new InvalidDataException("Retained CC0 archive hash differs from its reviewed source record.");
                using (var archive = ZipFile.OpenRead(path))
                {
                    var entry = archive.GetEntry(model.member); if (entry == null) throw new InvalidDataException("Missing reviewed animal member: " + model.member);
                    using (var stream = entry.Open()) using (var output = new MemoryStream()) { stream.CopyTo(output); bytes[i] = output.ToArray(); }
                }
                if (Digest(bytes[i]) != model.sha) throw new InvalidDataException("CC0 animal source digest mismatch: " + model.filename);
            }
            Directory.CreateDirectory(Folder);
            for (int i = 0; i < Models.Length; i++) if (bytes[i] != null) File.WriteAllBytes(Folder + Models[i].filename, bytes[i]);
            string license = Folder + "SOURCE_AND_LICENSE.md";
            if (!File.Exists(license)) File.WriteAllText(license,
                "# Quaternius wilderness animals\n\nArtist: Quaternius. CC0-1.0. Unchanged original FBX bytes from the already reviewed source archives.\n\n" +
                "https://opengameart.org/content/animated-animales-low-poly\nhttps://opengameart.org/content/5-low-poly-animals\nhttps://creativecommons.org/publicdomain/zero/1.0/\n\n" +
                string.Join("\n", Models.Select(m => "- " + m.filename + " — " + m.sha + " — " + m.archive + " / " + m.member)) +
                "\n\nExisting Eagle/bird rigs remain referenced from TruckBaseJumping/ThirdParty/QuaterniusBirds; they are not copied. New Generic importer/controller/URP configuration is LWS-owned. Contextual pet tricks are original bone routines, not claimed vendor clips. Native animation/visual acceptance remains separate.\n");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            for(int modelIndex=0;modelIndex<Models.Length;modelIndex++)
            {
                var model=Models[modelIndex];
                var importer = AssetImporter.GetAtPath(Folder + model.filename) as ModelImporter;
                if (importer == null) throw new InvalidOperationException("Actual animal model importer unavailable.");
                // Only these new owned importers are configured. Differing
                // existing nonempty controllers are retained below.
                if (bytes[modelIndex]!=null)
                {
                    importer.animationType = ModelImporterAnimationType.Generic; importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; importer.importAnimation = true; importer.importCameras = false; importer.importLights = false;
                    var clips = importer.defaultClipAnimations;
                    foreach (var clip in clips) { clip.loopTime = true; clip.lockRootPositionXZ = true; clip.lockRootHeightY = true; clip.lockRootRotation = true; }
                    importer.clipAnimations = clips; importer.SaveAndReimport();
                }
                string controllerPath = Folder + Path.GetFileNameWithoutExtension(model.filename) + " locomotion.controller";
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                if (controller != null) { if (controller.layers.Length == 0) throw new InvalidOperationException("Existing incomplete/custom controller retained: " + controllerPath); continue; }
                var imported = AssetDatabase.LoadAllAssetsAtPath(Folder + model.filename).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__", StringComparison.Ordinal)).ToArray();
                var walk = imported.SingleOrDefault(c => c.name.IndexOf(model.walk, StringComparison.OrdinalIgnoreCase) >= 0);
                var idle = imported.SingleOrDefault(c => c.name.IndexOf(model.idle, StringComparison.OrdinalIgnoreCase) >= 0);
                if (walk == null || idle == null || walk.empty || walk.length < .1f) throw new InvalidOperationException("Genuine compatible locomotion takes missing: " + model.filename + " clips=" + string.Join(",", imported.Select(c => c.name)));
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath); var machine = controller.layers[0].stateMachine;
                var a = machine.AddState("Idle"); a.motion = idle; var b = machine.AddState("Walk"); b.motion = walk; machine.defaultState = a;
                EditorUtility.SetDirty(a); EditorUtility.SetDirty(b); EditorUtility.SetDirty(machine); EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets();
            }
            Debug.Log("CAMPING_CC0_ANIMALS_IMPORTED three exact original animal rigs and compatible locomotion clips. Same shared wilderness for Camping/PetPals; no vendor source or old importer modified.");
        }
    }
}
