using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsRenderingEditModeTests
    {
        private const string RenderingSettingsPath = "Assets/LWS/InterstateHauler/Rendering/Data/IH_RenderingSettings.asset";
        private const string LwsVolumePath = "Assets/LWS/InterstateHauler/Rendering/Volume/IH_DefaultVolumeProfile.asset";
        private const string RenderValidationScenePath = "Assets/LWS/InterstateHauler/Rendering/Validation/RenderValidation.unity";

        [Test]
        public void GraphicsSettingsAssignsPcRenderPipelineAsset()
        {
            string graphicsSettings = File.ReadAllText("ProjectSettings/GraphicsSettings.asset");

            StringAssert.Contains(
                "m_CustomRenderPipeline: {fileID: 11400000, guid: 4b83569d67af61e458304325a23e5dfd, type: 2}",
                graphicsSettings);
        }

        [Test]
        public void ProjectColorSpaceIsLinear()
        {
            string projectSettings = File.ReadAllText("ProjectSettings/ProjectSettings.asset");

            StringAssert.Contains("m_ActiveColorSpace: 1", projectSettings);
        }

        [Test]
        public void QualitySettingsContainExpectedRenderingTiers()
        {
            string qualitySettings = File.ReadAllText("ProjectSettings/QualitySettings.asset");

            StringAssert.Contains("name: Ultra", qualitySettings);
            StringAssert.Contains("name: High", qualitySettings);
            StringAssert.Contains("name: Medium", qualitySettings);
            StringAssert.Contains("name: Low", qualitySettings);
            StringAssert.Contains("name: Steam Deck", qualitySettings);
            Assert.AreEqual(5, CountOccurrences(qualitySettings, "customRenderPipeline: {fileID: 11400000"));
        }

        [Test]
        public void LwsVolumeProfileContainsNoMissingScriptReferences()
        {
            string volume = File.ReadAllText(LwsVolumePath);

            Assert.Zero(CountOccurrences(volume, "m_Script: {fileID: 0}"));
        }

        [Test]
        public void RenderingSettingsAssetValidates()
        {
            LwsRenderingSettings settings = AssetDatabase.LoadAssetAtPath<LwsRenderingSettings>(RenderingSettingsPath);

            Assert.IsNotNull(settings);
            Assert.IsTrue(settings.Validate(out string message), message);
            Assert.IsTrue(settings.TryGetProfile(LwsRenderQualityTier.SteamDeck, out LwsRenderingQualityProfile steamDeck));
            Assert.IsNotNull(steamDeck.renderPipelineAsset);
        }

        [Test]
        public void RenderValidationSceneExistsButIsNotABuildScene()
        {
            Assert.IsTrue(File.Exists(RenderValidationScenePath));

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                Assert.AreNotEqual(RenderValidationScenePath, scene.path);
            }
        }

        private static int CountOccurrences(string text, string needle)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(needle, index, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += needle.Length;
            }

            return count;
        }
    }
}
