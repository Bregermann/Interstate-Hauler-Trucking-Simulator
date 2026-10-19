using System;using UnityEngine;using UnityEditor;
namespace LWS.TruckSports.Editor {
public static class SportsMatchAudioAuthoring {
 public static void Install(SportsEventSession match,SportsEventTruck[] trucks){var anchor=match.GetComponent<SportsMatchAudioAnchor>()??match.gameObject.AddComponent<SportsMatchAudioAnchor>();anchor.match=match;anchor.trucks=trucks;if(anchor.listener==null){var listeners=UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);if(listeners.Length!=1)throw new InvalidOperationException("Preserve exactly one Sports AudioListener.");var old=listeners[0];if(old.transform==match.transform){bool enabled=old.enabled;UnityEngine.Object.DestroyImmediate(old);var root=new GameObject("Single viewport audio focus");root.transform.SetParent(match.transform,false);anchor.listener=root.AddComponent<AudioListener>();anchor.listener.enabled=enabled;}else anchor.listener=old;}EditorUtility.SetDirty(anchor);}
}
}
