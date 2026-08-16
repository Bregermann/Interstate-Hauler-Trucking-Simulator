# Prompt 014 - Scene Streamer API Matrix

| Feature | Scene Streamer Class | API | Used By LWS | Support Level | Notes |
|---|---|---|---|---|---|
| Package root | n/a | `Assets/Plugins/Pixel Crushers/Scene Streamer` | Validator/docs | Native | Installed package version is 1.26.1. |
| Singleton component | `PixelCrushers.SceneStreamer.SceneStreamer` | MonoBehaviour | Master scene | Native | Added to `StreamingHighwayValidation.unity`. |
| Current scene | `SceneStreamer` | `SetCurrentScene(string)` | `LwsSceneStreamerAdapter.SetCurrentScene` | Native | Exposed through adapter, but not used for explicit policy loads because direct LWS loads are tracked outside Scene Streamer's private loaded list. |
| Loaded query | `SceneStreamer` | `IsSceneLoaded(string)` | `LwsSceneStreamerAdapter.IsSceneLoaded` | Native | Adapter checks Unity `SceneManager` first, then Scene Streamer. |
| Explicit load | `SceneStreamer` | `LoadScene(string)` | Not used for policy loads | Partial | Installed source has `Load(string sceneName)` call internal load with current scene name. Do not edit vendor source. |
| Explicit unload | `SceneStreamer` | `UnloadScene(string)` | Available through vendor, LWS uses Unity unload for explicit policy unloads | Partial | LWS keeps explicit load/unload state in its own service. |
| Additive load internals | `SceneStreamer` | `SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive)` | Audited/documented | Native | Confirms package is additive-scene based. |
| Additive unload internals | `SceneStreamer` | `SceneManager.UnloadSceneAsync(sceneName)` | Audited/documented | Native | Confirms package unload path. |
| Loading event | `SceneStreamer.StringAsyncEvent` | `onLoading` | Audited/documented | Native | Future adapter could subscribe directly if needed. |
| Loaded event | `SceneStreamer.StringEvent` | `onLoaded` | Audited/documented | Native | LWS currently observes Unity scene loaded/unloaded events. |
| Neighbor metadata | `NeighboringScenes` | `sceneNames` | `LwsStreamedChunkSceneRoot` | Native | Chunk roots synchronize this field reflectively. |
| Scene edge triggers | `SceneEdge` | `nextSceneName` | Not used | Native | Prompt 014 uses manifest/policy instead of trigger edges. |
| Start scene helper | `SetStartScene` | `startSceneName` | Not used | Native | Master scene configures through LWS coordinator. |

## Decision

LWS uses Scene Streamer as the installed streaming package boundary and keeps neighbor metadata compatible with it. Explicit chunk load policy is controlled by `LwsWorldStreamingService` because the installed explicit `LoadScene` path is not safe to rely on without editing vendor source.
