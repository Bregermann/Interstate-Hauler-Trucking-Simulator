#if VISTA
using System;
using Pinwheel.Vista;
using Pinwheel.Vista.Graph;
using Pinwheel.VistaEditor.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pinwheel.VistaEditor.Wizard
{
    /// <summary>
    /// "Create things in the project" destination: Vista assets saved to disk (graphs, content templates,
    /// data providers), as opposed to scene objects. The asset types are laid out as a grid of tiles grouped
    /// by purpose. Clicking a tile creates that asset in the current Project folder via the native flow
    /// (inline rename + ping), so the page stays a thin, data driven launcher over <see cref="ProjectWindowUtil"/>.
    ///
    /// The Real-World Data group is Pro only. Its tiles stay visible but disabled below Pro so the feature is
    /// still advertised, and the types are resolved by name (not referenced directly) because the Pro module
    /// is stripped from lower editions and must not be a compile dependency here.
    /// </summary>
    public class CreateAssetPage : HubPage, INavLevelPage
    {
        // One creatable asset type: the tile label and tooltip, the icon path (resolved through the same
        // prefix scheme quick action tiles use), the asset type to instantiate, the default file name used
        // before the user renames, and the asset file extension (".asset" for ScriptableObjects; native asset
        // types such as TerrainLayer use their own). type is null when the type is not present in this install
        // (e.g. a Pro provider stripped from a Personal build), which disables its tile.
        private sealed class Entry
        {
            public readonly string label;
            public readonly string tooltip;
            public readonly string iconPath;
            public readonly Type type;
            public readonly string defaultFileName;
            public readonly string fileExtension;

            public Entry(string label, string tooltip, string iconPath, Type type, string defaultFileName, string fileExtension = "asset")
            {
                this.label = label;
                this.tooltip = tooltip;
                this.iconPath = iconPath;
                this.type = type;
                this.defaultFileName = defaultFileName;
                this.fileExtension = fileExtension;
            }
        }

        public override string title => "Create Asset";

        protected override HubNavId HighlightedNav => HubNavId.CreateAsset;

        protected override void BuildContent(VisualElement content)
        {
            content.Add(VistaUI.Label("Create Vista assets in your project. Each one is saved into the selected Project folder, ready to rename.").P1());
            //content.Add(VistaUI.Spacer(8));

            // Graphs: the authored procedural graphs. Biome Mask Graph has no dedicated icon, so it reuses the
            // Terrain Graph icon (it is a Terrain Graph subtype), matching how the Create Assets quick actions
            // point at these same Resources textures.
            BuildGroupHeader(content, "Graphs");
            BuildGroupTiles(content, enabled: true, new[]
            {
                new Entry("Terrain Graph",
                    "The core procedural graph. Author terrain shapes, textures, foliage, and other output that biomes generate across the terrain.",
                    "res:Vista/Textures/TerrainGraphIcon", typeof(TerrainGraph), "Terrain Graph"),
                new Entry("Biome Mask Graph",
                    "Post-processes a Local Procedural Biome's mask. Runs after the base mask is rendered to shape where the biome applies.",
                    "res:Vista/Textures/BiomeMaskGraphIcon", typeof(BiomeMaskGraph), "Biome Mask Graph"),
            });

            // Content templates: reusable definitions the graph uses to paint and scatter across the terrain.
            // The Vista templates have no bespoke icon, so they fall back to the default ScriptableObject icon
            // (same as their quick actions); Terrain Layer uses its own Unity type icon.
            BuildGroupHeader(content, "Content Templates");
            BuildGroupTiles(content, enabled: true, new[]
            {
                new Entry("Tree Template",
                    "A tree species: the prefab plus the rendering and placement settings a graph uses to scatter it.",
                    "default:ScriptableObject", typeof(TreeTemplate), "Tree Template"),
                new Entry("Detail Template",
                    "A grass or detail definition used to render dense ground cover across the terrain.",
                    "default:ScriptableObject", typeof(DetailTemplate), "Detail Template"),
                new Entry("Object Template",
                    "A scattered game object definition for props such as rocks and debris, instanced across the terrain.",
                    "default:ScriptableObject", typeof(ObjectTemplate), "Object Template"),
                new Entry("Terrain Layer",
                    "A Unity terrain texture layer: the albedo, normal, and tiling used to paint the terrain surface.",
                    "default:TerrainLayer", typeof(TerrainLayer), "Terrain Layer", "terrainlayer"),
            });

            // Real-World Data: Pro only. Tiles stay visible but disabled below Pro; types resolved by name so
            // this page never hard-references the Pro module. The provider icons live in Personal's Resources
            // (not the Pro module), so they resolve for every edition and the disabled tiles still read as a
            // proper upgrade teaser. Below Pro the header carries a short note plus an Upgrade link to the store,
            // mirroring the Home page's upgrade CTA.
            bool isPro = EditorCommon.IsProEdition();
            SectionHeader rwdHeader = BuildGroupHeader(content, "Real World Data");
            if (!isPro)
            {
                rwdHeader.Actions.Add(VistaUI.Label("Available in Vista Pro").Faded());
                rwdHeader.Actions.Add(VistaUI.Clickable("Upgrade", () =>
                {
                    NetUtils.TrackClick("upgrade-cta", UILocation.Wizard_CreateAsset);
                    Application.OpenURL(Links.VISTA_PRO);
                }).Link());
            }
            BuildGroupTiles(content, enabled: isPro, new[]
            {
                new Entry("USGS",
                    "Download real-world elevation data from the USGS (United States) into a container the graph can load.",
                    "res:Vista/Textures/USGSLogo", ResolveType("Pinwheel.Vista.RealWorldData.USGS.USGSDataProviderAsset"), "USGS Data Provider"),
                new Entry("Open Topography",
                    "Download real-world elevation data from Open Topography (global) into a container the graph can load.",
                    "res:Vista/Textures/OpenTopographyIcon", ResolveType("Pinwheel.Vista.RealWorldData.OpenTopographyDataProviderAsset"), "Open Topography Data Provider"),
                new Entry("Custom Tile-Based",
                    "Provide real-world data from your own tile-based source into a container the graph can load.",
                    "res:Vista/Textures/CustomTileIcon", ResolveType("Pinwheel.Vista.RealWorldData.CustomTiledDataProviderAsset"), "Custom Data Provider"),
            });
        }

        // Add a group's section header, returning it so the caller can drop extra controls into its Actions
        // slot (an edition note today, an upgrade CTA later). Kept separate from the tiles so a section's header
        // can carry state the tiles below it do not.
        private SectionHeader BuildGroupHeader(VisualElement content, string heading)
        {
            SectionHeader header = new SectionHeader(heading);
            content.Add(header);
            return header;
        }

        // Add a group's tiles as a wrapping grid (the same grid layout the Home quick actions use). When
        // disabled the whole grid greys out, so a Pro-only group reads as present but unavailable.
        private void BuildGroupTiles(VisualElement content, bool enabled, Entry[] entries)
        {
            VisualElement grid = new VisualElement();
            grid.AddToClassList("quick-action-grid");
            foreach (Entry entry in entries)
                grid.Add(MakeTile(entry));
            grid.SetEnabled(enabled);
            content.Add(grid);
        }

        // A commit-an-action tile (icon + label, description on the tooltip), mirroring the Home quick actions.
        // The icon resolves through the same path quick action tiles use, from the entry's prefixed icon path.
        private ActionTile MakeTile(Entry entry)
        {
            Entry captured = entry;
            ActionTile tile = new ActionTile(entry.label, EditorCommon.LoadIcon(entry.iconPath), () =>
            {
                if (CreateAsset(captured))
                    SuccessfulActionCounter.Record(SuccessfulActionCounter.ActionKeys.ASSET_CREATED);
            });
            tile.tooltip = entry.tooltip;
            return tile;
        }

        // Create the asset in the current Project folder using Unity's native flow: it drops in with an inline
        // rename and is pinged, exactly like Assets > Create > Vista. Guards a missing type (disabled tile).
        // ScriptableObject assets are built with CreateInstance; native asset objects (e.g. TerrainLayer) with
        // their public constructor.
        private static bool CreateAsset(Entry entry)
        {
            if (entry.type == null)
                return false;

            UnityEngine.Object instance = typeof(ScriptableObject).IsAssignableFrom(entry.type)
                ? ScriptableObject.CreateInstance(entry.type)
                : Activator.CreateInstance(entry.type) as UnityEngine.Object;
            if (instance == null)
                return false;

            ProjectWindowUtil.CreateAsset(instance, "New " + entry.defaultFileName + "." + entry.fileExtension);
            return true;
        }

        // Resolve an optional ScriptableObject type by full name, without a compile-time reference. Returns
        // null when the type is absent (e.g. the Pro Real-World Data module is not installed), which leaves
        // its tile disabled rather than breaking the build.
        private static Type ResolveType(string fullName)
        {
            foreach (Type type in TypeCache.GetTypesDerivedFrom<ScriptableObject>())
            {
                if (type.FullName == fullName)
                    return type;
            }
            return null;
        }
    }
}
#endif
