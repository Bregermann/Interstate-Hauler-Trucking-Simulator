#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonRiderViewportHudAnchorTests
{
    [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)]
    public void EveryActualSplitViewportKeepsExistingCardInsideItsOwnCornerAwayFromTheWindscreenCenter(int count)
    {
        var matchType=Type.GetType("LWS.TruckDragonRiders.DragonRidersMatch, Assembly-CSharp",true);var layout=Type.GetType("LWS.TruckCornhole.CornholeMatch, Assembly-CSharp",true).GetMethod("ViewRect",BindingFlags.Public|BindingFlags.Static);var go=new GameObject("Mode-owned existing viewport card anchor control");go.SetActive(false);
        try
        {
            var match=go.AddComponent(matchType);
            foreach(var screen in new[]{new Vector2(1280,720),new Vector2(1920,1080),new Vector2(3440,1440)})
            {
                // Existing Sports Canvas is1920x1080, MatchWidthOrHeight .5.
                float scale=Mathf.Sqrt(screen.x/1920f*screen.y/1080f);var canvas=screen/scale;
                for(int view=0;view<count;view++)
                {
                    var viewport=(Rect)layout.Invoke(null,new object[]{count,view});var anchor=(Vector2)matchType.GetMethod("ViewportCardAnchor").Invoke(match,new object[]{view,viewport});Assert.That(anchor,Is.EqualTo(Vector2.zero));
                    var panel=new Rect(Vector2.Scale(viewport.min,canvas),Vector2.Scale(viewport.size,canvas));var card=new Rect(panel.min+Vector2.one*10,new Vector2(340,140));
                    Assert.That(panel.Contains(card.min)&&panel.Contains(card.max),Is.True,"Keep the existing tallest telemetry card screen-safe at "+screen+"/"+count+"/"+view);
                    Assert.That(card.Contains(panel.center),Is.False,"A card must not sit on its own central windscreen ray.");
                    Assert.That(card.xMin,Is.EqualTo(panel.xMin+10));Assert.That(card.yMin,Is.EqualTo(panel.yMin+10));
                }
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
}
#endif
