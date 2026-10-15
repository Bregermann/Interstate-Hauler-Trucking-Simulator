using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckTaxi.Tests
{
    // This namespace uses TruckTaxiPlayModeSaveIsolation's existing isolated vendor save root.
    public sealed class TruckTaxiSnowGpuPlayModeTests
    {
        [UnityTest, Timeout(180000)]
        public IEnumerator DemoHeldWheelContactWritesWeatheradeGpuTraceMask() => ProbeWorld(TruckTaxiWorldCatalog.Demo);

        [UnityTest, Timeout(180000)]
        public IEnumerator EdenboroughElevatedContactWritesWeatheradeGpuTraceMask() => ProbeWorld(TruckTaxiWorldCatalog.Edenborough);

        private IEnumerator ProbeWorld(string scene)
        {
            Assert.AreNotEqual(GraphicsDeviceType.Null, SystemInfo.graphicsDeviceType, "Requires an actual graphics device, not -nographics.");
            Assert.IsTrue(SystemInfo.supportsAsyncGPUReadback, "Requires actual GPU readback.");
            yield return SceneManager.LoadSceneAsync(scene);
            float deadline = Time.realtimeSinceStartup + 60;
            while ((TruckTaxiBootstrap.Instance == null || !TruckTaxiBootstrap.Instance.Ready) && Time.realtimeSinceStartup < deadline)
                yield return null;
            var host = TruckTaxiBootstrap.Instance;
            Assert.IsNotNull(host); Assert.IsTrue(host.Ready);
            yield return TruckTaxiPlayModeSaveIsolation.CompleteNewRunIntroduction(host);
            if (scene == TruckTaxiWorldCatalog.Edenborough)
            {
                // Use the same authored elevated connector as Windows; the town spawn is surrounded by buildings.
                var exit = UnityEngine.Object.FindFirstObjectByType<TruckTaxiWorldExit>();
                Assert.IsNotNull(exit); Assert.IsNotNull(exit.arrival);
                yield return host.GetComponent<TruckTaxiRegionalWorld>().PrepareInitialWorld(exit.arrival.position);
                var body = host.Player.GetComponent<Rigidbody>();
                Assert.IsTrue(TruckTaxiRecoverySpace.TryGroundedPose(body, exit.arrival.position, exit.arrival.rotation,
                    .35f, body, null, out var grounded), "Existing elevated connector needs supported terrain.");
                TruckTaxiRecoverySpace.Move(body, grounded, exit.arrival.rotation);
                body.linearVelocity = body.angularVelocity = Vector3.zero;
                host.Session.DiscardTeleportDistance(); Physics.SyncTransforms();
            }
            Assert.IsTrue(host.Environment.weatherAdapter.WeatherMakerRuntimeExists, "Use the real world weather runtime.");
            var surface = host.GetComponent<TruckTaxiSnowSurface>();
            Assert.IsNotNull(surface);
            bool frozen = host.Environment.Frozen;
            TruckTaxiSnowSnapshot original = host.Environment.Snow.Capture();
            Assert.IsNotNull(original);
            try
            {
                host.SetPaused(false);
                host.Environment.SetFrozen(true);
                Assert.IsTrue(host.Environment.ForceWeather(LwsWeatherPresetCatalog.HeavySnowId));
                var snow = host.Environment.Snow.Capture();
                snow.backgroundDepth = snow.maximumDepth * .8f;
                for (int i = 0; i < snow.depths.Length; i++) snow.depths[i] = snow.backgroundDepth;
                Assert.IsTrue(host.Environment.Snow.Restore(snow));
                const float refreshInterval = .25f;
                float nextRefresh = Time.time + refreshInterval;
                deadline = Time.realtimeSinceStartup + 30;
                while ((!surface.EnvironmentBindingsReady || !surface.TraceMaskReady) && Time.realtimeSinceStartup < deadline)
                {
                    // Match the real regional loading screen's bounded 64-renderer preparation batches.
                    surface.Refresh(); nextRefresh = Time.time + refreshInterval;
                    yield return null;
                }
                Assert.IsTrue(surface.TraceMaskReady, surface.Diagnostic);
                Assert.IsTrue(surface.EnvironmentBindingsReady, surface.Diagnostic);

                Vector3 contact = Vector3.zero;
                float width = 0;
                bool grounded = false;
                deadline = Time.realtimeSinceStartup + 5;
                while (!grounded && Time.realtimeSinceStartup < deadline)
                {
                    foreach (var wheel in host.Player.NwhAdapter.VehicleController.powertrain.wheels)
                    {
                        var api = wheel.wheelUAPI;
                        if (api == null || !api.IsGrounded || api.HitCollider == null || api.HitNormal.y < .25f) continue;
                        contact = api.HitPoint; width = Mathf.Max(.15f, api.Width); grounded = true;
                        Debug.Log("SNOW GPU real wheel contact=" + contact.ToString("F3") + " collider=" + api.HitCollider.name + " width=" + width);
                        break;
                    }
                    if (!grounded) yield return null;
                }
                Assert.IsTrue(grounded, scene + " needs a real grounded NWH contact for this renderer-only probe.");
                Vector3 end = contact + Vector3.ProjectOnPlane(host.Player.transform.forward, Vector3.up).normalized;
                Component generator = Field<Component>(surface, "generator");
                Assert.IsNotNull(generator);
                Camera objects = Field<Camera>(generator, "traceObjsCam");
                Camera surfaces = Field<Camera>(generator, "traceSurfCam");
                Material source = Field<Material>(surface, "traceMaterial");
                Debug.Log("SNOW GPU device=" + SystemInfo.graphicsDeviceName + " pipeline=" + GraphicsSettings.currentRenderPipeline +
                    " configured=" + host.Environment.settings.weatherRenderPipeline + " SRS index=" + surface.WeatheradeDepthRendererIndex);
                PrintSourcePasses(source);
                PrintRendererFilter(surface.WeatheradeDepthRendererIndex);

                // Reproduce a player-like vendor reset after the adapter has cached both cameras.
                Component coverage = Field<Component>(surface, "coverage");
                if (scene == TruckTaxiWorldCatalog.Edenborough)
                {
                    // Reproduce the lost native follower at elevated terrain, without moving the truck or changing clip planes.
                    coverage.GetType().GetMethod("UpdateDepth", BindingFlags.Instance | BindingFlags.Public).Invoke(coverage, null);
                    Vector3 stale = coverage.transform.position; stale.y = 0;
                    coverage.transform.position = stale;
                    generator.GetType().GetMethod("SimOneStep", BindingFlags.Instance | BindingFlags.Public).Invoke(generator, null);
                    PrintContactVolume("elevated stale anchor", coverage, objects, surfaces, contact);
                    surface.Refresh();
                    nextRefresh = Time.time + refreshInterval;
                    yield return null;
                    PrintContactVolume("elevated after Refresh", coverage, objects, surfaces, contact);
                    foreach (Camera camera in new[] { objects, surfaces })
                    {
                        float distance = camera.WorldToViewportPoint(contact).z;
                        Assert.That(distance, Is.InRange(camera.nearClipPlane, camera.farClipPlane),
                            "Actual elevated contact must be inside both vendor trace camera depth ranges after Refresh.");
                    }
                }
                FieldInfo rendererId = coverage.GetType().GetField("srsRendererId", BindingFlags.Instance | BindingFlags.Public);
                Assert.IsNotNull(rendererId);
                rendererId.SetValue(coverage, 0);
                generator.GetType().GetMethod("Init", BindingFlags.Instance | BindingFlags.Public).Invoke(generator, null);
                Debug.Log("SNOW GPU simulated player Init nativeId=" + rendererId.GetValue(coverage) +
                    " objects renderer=" + RendererIndex(objects) + " surface renderer=" + RendererIndex(surfaces));
                Assert.AreEqual(0, RendererIndex(objects), "Fault injection must actually reset the vendor object camera.");
                Assert.AreEqual(0, RendererIndex(surfaces), "Fault injection must actually reset the vendor surface camera.");
                surface.Refresh();
                Debug.Log("SNOW GPU after adapter Refresh nativeId=" + rendererId.GetValue(coverage) +
                    " objects renderer=" + RendererIndex(objects) + " surface renderer=" + RendererIndex(surfaces));
                Assert.AreEqual(surface.WeatheradeDepthRendererIndex, RendererIndex(objects));
                Assert.AreEqual(surface.WeatheradeDepthRendererIndex, RendererIndex(surfaces));
                Assert.AreEqual(surface.WeatheradeDepthRendererIndex, (int)rendererId.GetValue(coverage));

                // Freeze the clock, not rendering. No synthetic driving or writes to traceTex.
                deadline = Time.realtimeSinceStartup + 1.5f;
                while (Time.realtimeSinceStartup < deadline)
                {
                    if (Time.time >= nextRefresh) { surface.Refresh(); nextRefresh = Time.time + refreshInterval; }
                    surface.TraceWheel(0, contact, end, width, snow.backgroundDepth, true);
                    yield return null;
                }
                PrintCamera("objects", objects);
                PrintCamera("surface", surfaces);
                PrintContactVolume("held contact", coverage, objects, surfaces, contact);
                var objectRange = new DepthRange();
                var surfaceRange = new DepthRange();
                yield return ReadDepth(objects.targetTexture, objectRange);
                yield return ReadDepth(surfaces.targetTexture, surfaceRange);
                Debug.Log("SNOW GPU input objects=" + objectRange + " surface=" + surfaceRange);
                surface.RequestTracePixelDiagnostic();
                deadline = Time.realtimeSinceStartup + 10;
                while (surface.TracePixelDiagnostic.Contains("pending") && Time.realtimeSinceStartup < deadline) yield return null;
                Debug.Log("SNOW GPU output " + surface.TracePixelDiagnostic);
                Assert.AreEqual(1 << 29, objects.cullingMask, "Object camera must see only the trace volumes.");
                Assert.AreEqual(1 << 30, surfaces.cullingMask, "Surface camera must see only the depth proxies.");
                Assert.Greater(surface.TraceActivePixelCount, 0, "Actual GPU B>8 must contain marks. objects=" + objectRange +
                    " surface=" + surfaceRange + "; " + surface.TracePixelDiagnostic);

                Vector3 forward = (end - contact).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                var offroadPath = new RaycastHit[51];
                bool offroadFound = false;
                float depthMargin = width * .5f + surfaces.orthographicSize * 4 / surfaces.targetTexture.width;
                var roads = Field<Dictionary<MeshRenderer, MeshRenderer>>(surface, "roadDepthProxies");
                foreach (float offset in new[] { 14f, -14f, 25f, -25f })
                {
                    bool valid = true;
                    for (int i = 0; i < offroadPath.Length; i++)
                    {
                        float t = i / 50f;
                        Vector3 candidate = contact + forward * (3 * t) + right * (offset + .25f * Mathf.Sin(t * Mathf.PI));
                        if (!Physics.Raycast(candidate + Vector3.up * 4, Vector3.down, out RaycastHit hit, 12,
                                ~((1 << 29) | (1 << 30)), QueryTriggerInteraction.Ignore) || hit.normal.y < .25f ||
                            hit.collider.attachedRigidbody != null || !surface.IsSnowCoveredOffRoad(hit.collider, hit.point, host.Player.transform))
                        { valid = false; break; }
                        // Reject even conservative XZ bounds overlap: no full-road depth source may support the sampled footprint.
                        foreach (MeshRenderer roadProxy in roads.Values)
                        {
                            if (roadProxy == null) continue;
                            Bounds bounds = roadProxy.bounds;
                            bounds.Expand(new Vector3(depthMargin * 2, 0, depthMargin * 2));
                            if (hit.point.x >= bounds.min.x && hit.point.x <= bounds.max.x &&
                                hit.point.z >= bounds.min.z && hit.point.z <= bounds.max.z)
                            { valid = false; break; }
                        }
                        if (!valid) break;
                        offroadPath[i] = hit;
                    }
                    Debug.Log("SNOW GPU nearby offroad offset=" + offset + " full 3m path eligible and outside road depth bounds=" + valid);
                    if (!valid) continue;
                    offroadFound = true;
                    Debug.Log("SNOW GPU renderer-only OFFROAD path collider=" + offroadPath[0].collider.name +
                        " start=" + offroadPath[0].point.ToString("F3") + " end=" + offroadPath[50].point.ToString("F3") +
                        " road-depth exclusion margin=" + depthMargin + " (no physical NWH driving claim)");
                    PrintContactVolume("offroad contact", coverage, objects, surfaces, offroadPath[0].point);
                    break;
                }
                Assert.IsTrue(offroadFound, "Need a nearby exposed nonroad path outside full-road depth-proxy footprints; do not substitute road contacts.");

                surface.StopWheelTrace(0);
                typeof(TruckTaxiSnowSurface).GetMethod("ClearTraceHistory", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(surface, null);
                surface.RequestTracePixelDiagnostic();
                deadline = Time.realtimeSinceStartup + 10;
                while (surface.TracePixelDiagnostic.Contains("pending") && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.Zero(surface.TraceActivePixelCount, "Moving probe must start with no held-case history.");

                int objectRenders = 0, surfaceRenders = 0;
                Action<ScriptableRenderContext, Camera> rendered = (context, camera) =>
                { if (camera == objects) objectRenders++; if (camera == surfaces) surfaceRenders++; };
                RenderPipelineManager.endCameraRendering += rendered;
                try
                {
                    // Actual nearby offroad raycast contacts; only the reusable wheel contact proxy supplies local surface depth.
                    Vector3 previousContact = offroadPath[0].point;
                    float shortest = float.PositiveInfinity, longest = 0;
                    int frame = Time.frameCount;
                    for (int i = 1; i <= 50; i++)
                    {
                        if (Time.time >= nextRefresh) { surface.Refresh(); nextRefresh = Time.time + refreshInterval; }
                        RaycastHit hit = offroadPath[i];
                        Assert.IsTrue(surface.IsSnowCoveredOffRoad(hit.collider, hit.point, host.Player.transform),
                            "Each replayed short segment must remain eligible actual offroad ground.");
                        float length = Vector3.Distance(previousContact, hit.point);
                        shortest = Mathf.Min(shortest, length); longest = Mathf.Max(longest, length);
                        surface.TraceWheel(0, previousContact, hit.point, width, snow.backgroundDepth, true);
                        previousContact = hit.point;
                        yield return null;
                    }
                    var tracer = Field<Dictionary<int, MeshRenderer>>(surface, "wheelTracers")[0];
                    var proxy = Field<Dictionary<int, MeshRenderer>>(surface, "wheelDepthProxies")[0];
                    Debug.Log("SNOW GPU moving frames=" + (Time.frameCount - frame) + " renders objects/surface=" + objectRenders + "/" + surfaceRenders +
                        " length min/max=" + shortest + "/" + longest + " width=" + width +
                        " metersPerPixel objects/surface=" + (objects.orthographicSize * 2 / objects.targetTexture.width) + "/" +
                        (surfaces.orthographicSize * 2 / surfaces.targetTexture.width) + " tracer enabled=" + tracer.enabled +
                        " scale=" + tracer.transform.lossyScale.ToString("F4") + " proxy enabled=" + proxy.enabled +
                        " scale=" + proxy.transform.lossyScale.ToString("F4"));

                    var movingObjects = new DepthRange();
                    var movingSurface = new DepthRange();
                    IEnumerator readObjects = ReadDepth(objects.targetTexture, movingObjects);
                    IEnumerator readSurface = ReadDepth(surfaces.targetTexture, movingSurface);
                    int movingPixels;
                    string movingDiagnostic;
                    try
                    {
                        // Queue both input snapshots and mask readback before disabling; do not hold the final short stamp during the readbacks.
                        bool objectsPending = readObjects.MoveNext();
                        bool surfacePending = readSurface.MoveNext();
                        surface.RequestTracePixelDiagnostic();
                        surface.StopWheelTrace(0);
                        if (objectsPending) yield return readObjects;
                        if (surfacePending) yield return readSurface;
                        deadline = Time.realtimeSinceStartup + 10;
                        while (surface.TracePixelDiagnostic.Contains("pending") && Time.realtimeSinceStartup < deadline) yield return null;
                        movingPixels = surface.TraceActivePixelCount;
                        movingDiagnostic = surface.TracePixelDiagnostic;
                        Debug.Log("SNOW GPU moving immediate objects=" + movingObjects + " surface=" + movingSurface + " " + movingDiagnostic);
                    }
                    finally { (readObjects as IDisposable)?.Dispose(); (readSurface as IDisposable)?.Dispose(); }

                    Assert.Greater(movingPixels, 0, "One-frame short segments must produce actual GPU marks: " + movingDiagnostic);
                    var availabilityChanged = typeof(TruckTaxiBootstrap).GetMethod("OnRegionalAvailabilityChanged",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    Assert.IsNotNull(availabilityChanged);
                    availabilityChanged.Invoke(host, null);
                    surface.Refresh();
                    nextRefresh = Time.time + refreshInterval;
                    surface.RequestTracePixelDiagnostic();
                    deadline = Time.realtimeSinceStartup + 10;
                    while (surface.TracePixelDiagnostic.Contains("pending") && Time.realtimeSinceStartup < deadline) yield return null;
                    Debug.Log("SNOW GPU actual regional availability callback before B>8=" + movingPixels +
                        " after=" + surface.TracePixelDiagnostic + " refresh interval=" + refreshInterval);
                    Assert.Greater(surface.TraceActivePixelCount, 0,
                        "Actual OnRegionalAvailabilityChanged + Refresh must preserve moving marks: " + surface.TracePixelDiagnostic);

                    deadline = Time.realtimeSinceStartup + 8;
                    while (Time.realtimeSinceStartup < deadline)
                    {
                        if (Time.time >= nextRefresh) { surface.Refresh(); nextRefresh = Time.time + refreshInterval; }
                        yield return null;
                    }
                    surface.RequestTracePixelDiagnostic();
                    deadline = Time.realtimeSinceStartup + 10;
                    while (surface.TracePixelDiagnostic.Contains("pending") && Time.realtimeSinceStartup < deadline) yield return null;
                    Debug.Log("SNOW GPU moving stopped 8s objects/surface renders=" + objectRenders + "/" + surfaceRenders +
                        " renderer=" + RendererIndex(objects) + "/" + RendererIndex(surfaces) + " " + surface.TracePixelDiagnostic);
                    Assert.Greater(surface.TraceActivePixelCount, 0,
                        "Moving marks must survive regional availability and the braking-style delay: " + surface.TracePixelDiagnostic);
                }
                finally { RenderPipelineManager.endCameraRendering -= rendered; }
            }
            finally
            {
                if (surface != null) surface.StopWheelTrace(0);
                if (host != null)
                {
                    host.Environment.Snow.Restore(original);
                    host.Environment.SetFrozen(frozen);
                    host.SetPaused(false);
                }
                Time.timeScale = 1;
            }
        }

        private static void PrintSourcePasses(Material material)
        {
            Assert.IsNotNull(material);
            string passes = "";
            var findTag = typeof(Shader).GetMethod("FindPassTagValue", new[] { typeof(int), typeof(ShaderTagId) });
            for (int i = 0; i < material.passCount; i++)
            {
                string name = material.GetPassName(i);
                object value = findTag?.Invoke(material.shader, new object[] { i, new ShaderTagId("LightMode") });
                passes += " [" + name + " LightMode=" + (value is ShaderTagId tag ? tag.name : "unavailable") +
                    " enabled=" + material.GetShaderPassEnabled(name) + "]";
            }
            Debug.Log("SNOW GPU source=" + material.shader.name + " queue=" + material.renderQueue +
                " RenderType=" + material.GetTag("RenderType", false) + " _Render=" + material.GetFloat("_Render") + passes);
        }

        private static void PrintRendererFilter(int index)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            var data = Field<Array>(pipeline, "m_RendererDataList");
            Assert.That(index, Is.InRange(0, data.Length - 1));
            object renderer = data.GetValue(index);
            Debug.Log("SNOW GPU renderer data=" + renderer);
            var features = (IEnumerable)renderer.GetType().GetProperty("rendererFeatures").GetValue(renderer);
            foreach (object feature in features)
            {
                if (feature.GetType().Name != "SRS_RenderDepthWithReplacement") continue;
                object settings = Field<object>(feature, "settings");
                object pass = Field<object>(feature, "m_ScriptablePass");
                FilteringSettings filter = Field<FilteringSettings>(pass, "filteringSettings");
                var tags = Field<List<ShaderTagId>>(pass, "shaderTagsList");
                Shader replacement = Field<Shader>(settings, "replacementShader");
                Debug.Log("SNOW GPU feature active=" + feature.GetType().GetProperty("isActive").GetValue(feature) +
                    " settings layers=" + Field<LayerMask>(settings, "layerMask").value + " cached filter layers=" + filter.layerMask +
                    " includes29=" + ((filter.layerMask & (1 << 29)) != 0) + " includes30=" + ((filter.layerMask & (1 << 30)) != 0) +
                    " queue=" + filter.renderQueueRange.lowerBound + ".." + filter.renderQueueRange.upperBound +
                    " tags=" + string.Join(",", tags.ConvertAll(tag => tag.name)) + " replacement=" + replacement);
                return;
            }
            Assert.Fail("Actual active renderer has no SRS_RenderDepthWithReplacement feature.");
        }

        private static void PrintCamera(string label, Camera camera)
        {
            Assert.IsNotNull(camera);
            Type feature = FindType("SRS_RenderDepthWithReplacement");
            var targets = (IDictionary)feature.GetField("rtHandles", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            object handle = targets != null && targets.Contains(camera) ? targets[camera] : null;
            var bound = handle?.GetType().GetProperty("rt").GetValue(handle) as RenderTexture;
            var data = camera.GetComponent(FindType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData"));
            Debug.Log("SNOW GPU " + label + " camera=" + camera.name + "#" + camera.GetInstanceID() +
                " mask=" + camera.cullingMask + " renderer=" + Field<int>(data, "m_RendererIndex") +
                " enabled=" + camera.enabled + " position=" + camera.transform.position.ToString("F3") +
                " rotation=" + camera.transform.eulerAngles.ToString("F2") + " size=" + camera.orthographicSize +
                " near/far=" + camera.nearClipPlane + "/" + camera.farClipPlane + " target=" + Target(camera.targetTexture) +
                " registered=" + Target(bound) + " same=" + (camera.targetTexture == bound));
        }

        private static int RendererIndex(Camera camera) => Field<int>(
            camera.GetComponent(FindType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData")), "m_RendererIndex");

        private static void PrintContactVolume(string label, Component coverage, Camera objects, Camera surfaces, Vector3 contact)
        {
            Transform target = Field<Transform>(coverage, "followTarget");
            Debug.Log("SNOW GPU " + label + " contact=" + contact.ToString("F3") + " coverage=" + coverage.transform.position.ToString("F3") +
                " follow=" + (target != null ? target.position.ToString("F3") : "null") +
                " offset=" + Field<Vector3>(coverage, "targetPosOffset").ToString("F3") +
                " object viewport=" + objects.WorldToViewportPoint(contact).ToString("F5") +
                " surface viewport=" + surfaces.WorldToViewportPoint(contact).ToString("F5") +
                " cameraY object/surface=" + objects.transform.position.y + "/" + surfaces.transform.position.y +
                " clips=" + objects.nearClipPlane + ".." + objects.farClipPlane + "/" + surfaces.nearClipPlane + ".." + surfaces.farClipPlane);
        }

        private sealed class DepthRange
        {
            public float min = float.PositiveInfinity, max = float.NegativeInfinity;
            public int nonClear;
            public override string ToString() => "[" + min.ToString("G9") + "," + max.ToString("G9") + "] nonClear=" + nonClear;
        }

        private static IEnumerator ReadDepth(RenderTexture depth, DepthRange range)
        {
            Assert.IsNotNull(depth);
            RenderTexture copy = RenderTexture.GetTemporary(depth.width, depth.height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            RenderTexture previous = RenderTexture.active;
            try
            {
                // Convert actual hardware depth to a readback-supported color format; never touch trace inputs/output.
                Graphics.Blit(depth, copy);
                RenderTexture.active = previous;
                var request = AsyncGPUReadback.Request(copy, 0, TextureFormat.RFloat);
                float deadline = Time.realtimeSinceStartup + 10;
                while (!request.done && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(request.done, "Depth GPU readback timed out.");
                Assert.IsFalse(request.hasError, "Depth GPU readback failed.");
                var values = request.GetData<float>();
                float clear = SystemInfo.usesReversedZBuffer ? 0 : 1;
                for (int i = 0; i < values.Length; i++)
                {
                    float value = values[i];
                    range.min = Mathf.Min(range.min, value); range.max = Mathf.Max(range.max, value);
                    if (Mathf.Abs(value - clear) > .00001f) range.nonClear++;
                }
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(copy); }
        }

        private static string Target(RenderTexture texture) => texture == null ? "null" : texture.name + "#" + texture.GetInstanceID() +
            " " + texture.width + "x" + texture.height + " " + texture.format + " created=" + texture.IsCreated();

        private static T Field<T>(object owner, string name)
        {
            Assert.IsNotNull(owner, "Missing owner for " + name);
            for (Type type = owner.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return (T)field.GetValue(owner);
            }
            throw new MissingFieldException(owner.GetType().FullName, name);
        }

        private static Type FindType(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            { Type type = assembly.GetType(name, false); if (type != null) return type; }
            throw new TypeLoadException(name);
        }
    }
}
