#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class DerbyDamageHudTests
{
    static object Field(object o,string name)=>o.GetType().GetField(name).GetValue(o);
    static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name).Invoke(o,args);
    static string Text(Transform node)=>node.GetComponent(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro",true)).GetType().GetProperty("text").GetValue(node.GetComponent(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro",true))).ToString();
    [UnityTest]
    public IEnumerator HealthReadsActualDamageAndRematchRestoresSharedCards()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"DerbyHealthCards"}))
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckDemolitionDerby/Scenes/TruckDemolitionDerby.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            var match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckDerby.DerbyMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return null;yield return null;
            var menus=(Component)Field(match,"menus");var parent=((GameObject)Field(menus,"hud")).transform.Find("Broadcast participant cards");Assert.That(parent,Is.Not.Null);
            for(int i=0;i<8;i++){var row=parent.Find("Participant card "+i+"/Derby actual engine and body health");Assert.That(row,Is.Not.Null);Assert.That(Text(row.Find("Engine remaining percent")),Is.EqualTo("100%"));Assert.That(Text(row.Find("Body remaining percent")),Is.EqualTo("100%"));}
            var fighters=(Array)Field(match,"fighters");var damage=Field(fighters.GetValue(0),"damage");var zone=Type.GetType("LWS.TruckDerby.DerbyZone, Assembly-CSharp",true);Call(damage,"Apply",Enum.Parse(zone,"Engine"),.26f,-1);yield return null;yield return null;
            var engine=parent.Find("Participant card 0/Derby actual engine and body health/Engine remaining percent");Assert.That(Text(engine),Is.EqualTo("74%"));
            Call(match,"SetupScreen");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return null;yield return null;Assert.That(Text(engine),Is.EqualTo("100%"));Call(match,"SetupScreen");
        }
    }
}
#endif
