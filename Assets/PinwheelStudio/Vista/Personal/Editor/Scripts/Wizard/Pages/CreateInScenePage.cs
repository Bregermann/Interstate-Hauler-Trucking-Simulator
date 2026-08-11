#if VISTA
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.VistaEditor.UIElements;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Pinwheel.VistaEditor.Wizard
{
    /// <summary>
    /// Compact hub for creating Vista content in the current scene. A small live status row handles the
    /// scene's Vista Manager dependency; terrain and biome stay visible as the two primary destinations.
    /// Scene aware: it rebuilds on hierarchyChanged so the status and available actions remain current.
    /// </summary>
    public class CreateInScenePage : HubPage, INavLevelPage
    {
        private VisualElement m_sections;

        public override string title => "Create in Scene";

        protected override HubNavId HighlightedNav => HubNavId.CreateInScene;

        protected override void BuildContent(VisualElement content)
        {
            // Build into our own sub container, never the shared content pane. The pane also holds the page
            // title strip and breadcrumb that HubPage placed above us, and Rebuild clears m_sections wholesale
            // on every hierarchyChanged. Clearing the shared pane would wipe the title too.
            m_sections = new VisualElement();
            content.Add(m_sections);
            EditorApplication.hierarchyChanged += Rebuild;
            BuildSections();
        }

        public override bool OnPop(WizardWindow host)
        {
            EditorApplication.hierarchyChanged -= Rebuild;
            return true;
        }

        public override void OnWizardClose(WizardWindow host)
        {
            EditorApplication.hierarchyChanged -= Rebuild;
        }

        private void Rebuild()
        {
            if (m_sections == null)
                return;
            m_sections.Clear();
            BuildSections();
        }

        private void BuildSections()
        {
            List<VistaManager> managers = new List<VistaManager>(VistaManager.allInstances);
            VistaManager manager = managers.Count > 0 ? managers[0] : null;

            VisualElement row = VistaUI.Row();

            ObjectField field = new ObjectField("Vista Manager")
            {
                objectType = typeof(VistaManager),
                allowSceneObjects = true,
                value = manager,
                tooltip = "The Vista Manager hosts this scene's biomes, manages its terrain tiles, and coordinates terrain generation. You normally need one per scene.",
            };
            field.AddToClassList("create-scene-manager__field");
            row.Add(field);

            if (manager == null)
                row.Add(VistaUI.Clickable("Create", CreateManager).Chip());

            m_sections.Add(row);
            m_sections.Add(VistaUI.Spacer(8));

            bool canCreate = manager != null;

            NavCard terrain = new NavCard(
                "Terrains",
                "Add terrain tiles to the scene.",
                () => host.PushPage(new CreateTerrainPage()))
            {
                tooltip = "Open the terrain creation flow to configure and add a grid of terrain tiles.\n" +
                    "The tiles are registered with the Vista Manager so biomes can build onto them.\n" +
                    "A Vista Manager is required before you can continue.",
            };
            terrain.SetEnabled(canCreate);
            m_sections.Add(terrain);

            NavCard biome = new NavCard(
                "Biomes",
                "Create from a biome template.",
                () => host.PushPage(new CreateBiomePage()))
            {
                tooltip = "Open the biome creation flow to choose a template and create a biome under the Vista Manager.\n" +
                    "Biomes generate terrain shapes, textures, foliage, and other content across the Manager's terrain tiles.\n" +
                    "A Vista Manager is required before you can continue.",
            };
            biome.SetEnabled(canCreate);
            m_sections.Add(biome);
        }

        private void CreateManager()
        {
            VistaManager manager = VistaManager.CreateInstanceInScene();
            Selection.activeObject = manager;
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            SuccessfulActionCounter.Record(SuccessfulActionCounter.ActionKeys.VISTA_MANAGER_CREATED);
            // hierarchyChanged fires from the creation and rebuilds the section to show the object field.
        }
    }
}
#endif
