using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using LWS.TruckSports;
namespace LWS.TruckDockDiving.Editor
{
    // Explicit diagnostic only. Uses an ordinary URP Game/Base camera targeting
    // an RT, never PreviewRenderUtility's URP backbuffer/CameraType.Preview path.
    public static class DockSplitBatchPreview
    {
        [MenuItem("Truck Sports/Customizer/Dock/Capture Seven Source Bodies And Cyan Planes")]
        public static void CaptureOpenDockSourcePlanes()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
            var originalScene = SceneManager.GetActiveScene();
            if (originalScene.path != "Assets/TruckDockDiving/Scenes/TruckDockDiving.unity") throw new InvalidOperationException("Explicitly open the original Dock scene first. This method never opens/saves/rebuilds it.");
            LWS.TruckSports.Editor.SportsTruckAppearanceHostAuthoring.CreateDockSplitPlaneSettings();
            var settings = AssetDatabase.LoadAssetAtPath<DockAppearanceSplitSettings>(LWS.TruckSports.Editor.SportsTruckAppearanceHostAuthoring.DockPlanePath);
            var catalog = Resources.Load<SportsTruckAppearanceCatalog>("SportsTruckAppearanceCatalog"); var theme = Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme");
            if (settings == null || catalog == null || !catalog.Validate(out _) || catalog.bodies.Length != 7 || theme == null || theme.headingFont == null) throw new InvalidOperationException("Existing seven source bodies, explicit fit settings and installed broadcast font required.");
            const int width = 640, height = 480; Scene scene = default; GameObject root = null; RenderTexture target = null; Texture2D sheet = null; Material cyan = null;
            var priorTarget = RenderTexture.active;
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, "..")); string output = Path.GetFullPath(Path.Combine(project, "Logs/TruckSports/PlaytestRepair/DockSplitPlanePreview", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
            string guard = Path.GetFullPath(Path.Combine(project, "Logs/TruckSports/PlaytestRepair/DockSplitPlanePreview")) + Path.DirectorySeparatorChar;
            if (!output.StartsWith(guard, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Dock diagnostic output must stay within its canonical log folder.");
            try
            {
                scene = EditorSceneManager.NewPreviewScene(); root = new GameObject("Temporary Dock source-plane diagnostic only"); SceneManager.MoveGameObjectToScene(root, scene);
                var cameraObject = new GameObject("Ordinary Dock source Game camera"); cameraObject.transform.SetParent(root.transform, false); var camera = cameraObject.AddComponent<Camera>();
                camera.cameraType = CameraType.Game; camera.scene = scene; camera.enabled = false; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.055f,.07f,.095f); camera.fieldOfView = 38; camera.nearClipPlane = .02f; camera.farClipPlane = 150; camera.cullingMask = 1 << 31; camera.allowHDR = false; camera.allowMSAA = false;
                var data = camera.GetUniversalAdditionalCameraData(); data.renderType = CameraRenderType.Base; data.renderPostProcessing = false; data.allowXRRendering = false;
                for (int index = 0; index < 2; index++) { var lightObject = new GameObject("Temporary source key light " + index); lightObject.transform.SetParent(root.transform, false); lightObject.transform.rotation = Quaternion.Euler(index == 0 ? 45 : 25, index == 0 ? -30 : 145, 0); var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = index == 0 ? 1.6f : .9f; light.cullingMask = 1 << 31; light.shadows = LightShadows.None; }
                var shader = Shader.Find("Universal Render Pipeline/Unlit"); if (shader == null) throw new InvalidOperationException("Installed URP Unlit shader required for explicit cyan diagnostic plane."); cyan = new Material(shader) { hideFlags = HideFlags.HideAndDontSave }; cyan.SetColor("_BaseColor", Color.cyan);
                target = new RenderTexture(width,height,24,RenderTextureFormat.ARGB32) { name = "Owned temporary Dock source target", hideFlags = HideFlags.HideAndDontSave }; target.Create(); camera.targetTexture = target; camera.aspect = (float)width/height;
                sheet = new Texture2D(width*4,height*2,TextureFormat.RGB24,false); var background = Enumerable.Repeat(new Color(.055f,.07f,.095f),sheet.width*sheet.height).ToArray(); sheet.SetPixels(background);
                var report = new System.Text.StringBuilder("DOCK_SOURCE_PLANES_ONLY\nActual uncut source bodies fitted to the original vehicle frame. Cyan line grid is the explicit vehicle-local Y plane. No recipes generated; no scene saved.\n");
                for (int index = 0; index < catalog.bodies.Length; index++)
                {
                    var body = catalog.bodies[index]; var plane = settings.planes.Single(item => item.bodyId == body.id); var bodyRoot = new GameObject("Temporary actual source " + body.id); bodyRoot.transform.SetParent(root.transform, false);
                    try
                    {
                        Bounds bounds = default; bool found = false; int surfaces = 0;
                        foreach (var source in body.visualPrefab.GetComponentsInChildren<MeshRenderer>(true))
                        {
                            if (!source.enabled || !source.gameObject.activeSelf) continue; var filter = source.GetComponent<MeshFilter>(); if (filter == null || filter.sharedMesh == null) throw new InvalidOperationException("Original source mesh missing: " + body.id);
                            var item = new GameObject(source.name, typeof(MeshFilter),typeof(MeshRenderer)); item.layer = 31; item.transform.SetParent(bodyRoot.transform, false);
                            DockSelectedSplitPresentation.SetLocal(item.transform, plane.canonicalToVehicle * body.visualPrefab.transform.worldToLocalMatrix * source.localToWorldMatrix); item.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh; var renderer = item.GetComponent<MeshRenderer>(); renderer.sharedMaterials = source.sharedMaterials; renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                            if (!found) { bounds = renderer.bounds; found = true; } else bounds.Encapsulate(renderer.bounds); surfaces++;
                        }
                        if (!found) throw new InvalidOperationException("No visible actual source surfaces: " + body.id);
                        float x0 = bounds.min.x-.3f, x1 = bounds.max.x+.3f, z0 = bounds.min.z-.3f, z1 = bounds.max.z+.3f;
                        void Line(string name,Vector3 a,Vector3 b) { var go = new GameObject(name); go.layer = 31; go.transform.SetParent(bodyRoot.transform,false); var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = cyan; line.useWorldSpace = false; line.positionCount = 2; line.SetPosition(0,a); line.SetPosition(1,b); line.widthMultiplier = .018f; line.numCapVertices = 0; }
                        float y = plane.vehiclePlaneY; Line("Cyan plane left",new Vector3(x0,y,z0),new Vector3(x0,y,z1)); Line("Cyan plane right",new Vector3(x1,y,z0),new Vector3(x1,y,z1)); Line("Cyan plane near",new Vector3(x0,y,z0),new Vector3(x1,y,z0)); Line("Cyan plane far",new Vector3(x0,y,z1),new Vector3(x1,y,z1));
                        for (int grid = 1; grid < 4; grid++) { float x = Mathf.Lerp(x0,x1,grid/4f), z = Mathf.Lerp(z0,z1,grid/4f); Line("Cyan plane cross x",new Vector3(x,y,z0),new Vector3(x,y,z1)); Line("Cyan plane cross z",new Vector3(x0,y,z),new Vector3(x1,y,z)); }
                        Vector3 center = bounds.center; float distance = Mathf.Max(3,bounds.extents.magnitude/Mathf.Sin(camera.fieldOfView*Mathf.Deg2Rad*.5f)*1.16f); camera.transform.position = center + new Vector3(-.65f,.4f,-1).normalized*distance; camera.transform.LookAt(center);
                        var canvasObject = new GameObject("Temporary source caption",typeof(RectTransform),typeof(Canvas)); canvasObject.layer = 31; canvasObject.transform.SetParent(bodyRoot.transform,false); var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .1f; canvas.sortingOrder = 1000;
                        var labelObject = new GameObject("Actual body and plane label",typeof(RectTransform)); labelObject.layer = 31; labelObject.transform.SetParent(canvasObject.transform,false); var label = labelObject.AddComponent<TextMeshProUGUI>(); label.font = theme.headingFont; label.text = body.label + "\n" + body.id + "   |   CYAN Y=" + y.ToString("F3",System.Globalization.CultureInfo.InvariantCulture) + "\nSOURCE ONLY - BEFORE CUT GENERATION"; label.fontSize = 18; label.color = Color.white; label.alignment = TextAlignmentOptions.TopLeft; label.enableWordWrapping = false; label.raycastTarget = false; var rect = label.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(14,8); rect.offsetMax = new Vector2(-8,-8); Canvas.ForceUpdateCanvases(); label.ForceMeshUpdate();
                        camera.Render(); RenderTexture.active = target; var tile = new Texture2D(width,height,TextureFormat.RGB24,false);
                        try { tile.ReadPixels(new Rect(0,0,width,height),0,0); tile.Apply(); sheet.SetPixels((index%4)*width,(1-index/4)*height,width,height,tile.GetPixels()); }
                        finally { UnityEngine.Object.DestroyImmediate(tile); }
                        report.Append(body.id).Append(" sourceRenderers=").Append(surfaces).Append(" planeY=").Append(y.ToString("R",System.Globalization.CultureInfo.InvariantCulture)).Append(" fittedBounds=").Append(bounds).Append(" sourceHash=").Append(AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(body.visualPrefab))).AppendLine();
                    }
                    finally { UnityEngine.Object.DestroyImmediate(bodyRoot); }
                }
                sheet.Apply(); Directory.CreateDirectory(output); string image = Path.Combine(output,"seven-source-bodies-cyan-planes.png"); File.WriteAllBytes(image,sheet.EncodeToPNG()); File.WriteAllText(Path.Combine(output,"source-plane-preview.txt"),report.ToString());
                Debug.Log("DOCK_SEVEN_SOURCE_PLANES_CAPTURED " + image + "; ordinary Game/Base camera, source bodies only, no generation/scene save. Artist review required before explicit recipe generation.");
            }
            finally
            {
                RenderTexture.active = priorTarget; if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); } if (sheet != null) UnityEngine.Object.DestroyImmediate(sheet); if (cyan != null) UnityEngine.Object.DestroyImmediate(cyan); if (root != null) UnityEngine.Object.DestroyImmediate(root); if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
