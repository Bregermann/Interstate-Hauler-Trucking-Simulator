using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckDockDiving.Editor
{
    [CustomEditor(typeof(DockAppearanceSplitSettings))]
    public sealed class DockAppearanceSplitSettingsInspector : UnityEditor.Editor
    {
        PreviewRenderUtility preview; Mesh plane; Material line; float separation, yaw = 25;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Private Dock rendering only. Plane Y is in the original vehicle frame. Preview separation uses the original local split amount times the saved legacy model Y scale. Changes never regenerate recipes automatically; existing recipes require explicit artist revision.", MessageType.Info);
            separation = EditorGUILayout.Slider("Original local split amount", separation, 0, .6f); yaw = EditorGUILayout.Slider("Preview yaw", yaw, -180, 180);
            Rect rect = GUILayoutUtility.GetRect(200, 280, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint) return;
            var settings = (DockAppearanceSplitSettings)target; var catalog = Resources.Load<SportsTruckAppearanceCatalog>("SportsTruckAppearanceCatalog");
            if (catalog == null || settings.planes == null) return;
            var body = catalog.FindBody(settings.previewBodyId); if (body == null) return; var cut = settings.planes.FirstOrDefault(item => item.bodyId == body.id); if (cut == null) return;
            if (preview == null)
            {
                preview = new PreviewRenderUtility(); preview.camera.fieldOfView = 35; preview.camera.nearClipPlane = .01f; preview.camera.farClipPlane = 100;
                line = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave }; line.SetInt("_Cull", 0); line.SetInt("_ZWrite", 0); line.color = Color.cyan;
                plane = new Mesh { hideFlags = HideFlags.HideAndDontSave }; plane.vertices = new[] { new Vector3(-3,0,-7), new Vector3(3,0,-7), new Vector3(3,0,7), new Vector3(-3,0,7) }; plane.SetIndices(new[] { 0,1,1,2,2,3,3,0 }, MeshTopology.Lines, 0);
            }
            preview.BeginPreview(rect, GUIStyle.none); var center = new Vector3(0, 1, 0); preview.camera.transform.position = center + Quaternion.Euler(17, yaw, 0) * new Vector3(0, 0, -16); preview.camera.transform.LookAt(center); preview.lights[0].intensity = 1.1f; preview.lights[1].intensity = .8f;
            var recipe = AssetDatabase.LoadAssetAtPath<DockAppearanceSplitRecipe>(LWS.TruckSports.Editor.SportsTruckAppearanceHostAuthoring.DockRecipeFolder + "/" + body.id + ".asset");
            if (recipe != null && recipe.ValidFor(body))
            {
                var renderers = body.visualPrefab.GetComponentsInChildren<Renderer>(true);
                foreach (var part in recipe.parts)
                {
                    var source = renderers[part.rendererIndex]; var matrix = Matrix4x4.Translate(part.upper ? Vector3.up * separation * settings.legacyVisualYScale : Vector3.zero) * cut.canonicalToVehicle * body.visualPrefab.transform.worldToLocalMatrix * source.localToWorldMatrix;
                    for (int sub = 0; sub < part.mesh.subMeshCount; sub++) preview.DrawMesh(part.mesh, matrix, sub == 0 ? source.sharedMaterials[part.materialIndex] : recipe.capMaterial, sub);
                }
            }
            else foreach (var source in body.visualPrefab.GetComponentsInChildren<MeshRenderer>(true))
            {
                var filter = source.GetComponent<MeshFilter>(); if (filter == null || filter.sharedMesh == null || !source.enabled) continue;
                for (int sub = 0; sub < filter.sharedMesh.subMeshCount; sub++) preview.DrawMesh(filter.sharedMesh, cut.canonicalToVehicle * body.visualPrefab.transform.worldToLocalMatrix * source.localToWorldMatrix, source.sharedMaterials[sub], sub);
            }
            preview.DrawMesh(plane, Matrix4x4.Translate(Vector3.up * cut.vehiclePlaneY), line, 0); preview.camera.Render(); GUI.DrawTexture(rect, preview.EndPreview(), ScaleMode.StretchToFill, false);
        }
        void OnDisable() { if (preview != null) preview.Cleanup(); if (plane != null) DestroyImmediate(plane); if (line != null) DestroyImmediate(line); }
    }
}
