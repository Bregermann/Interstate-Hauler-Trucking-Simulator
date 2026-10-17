using UnityEngine;using UnityEditor;
namespace LWS.TruckWallball.Editor {public static class WallballProductionAuthoring {public static void Install(WallballMatch m){m.contactHalfExtents=new Vector3(1.15f,1.3f,1.6f);m.contactCenter=new Vector3(0,.55f,.1f);m.ball.maxSpeed=12;EditorUtility.SetDirty(m);}}}
