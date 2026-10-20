using System.Linq;
using UnityEditor;
using UnityEngine;
using TMPro;
using Cinemachine;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
namespace LWS.TruckRedLightGreenLight.Editor
{
    public static class RedLightPolishAuthoring
    {
        public static void Install(RedLightMatch match)
        {
            if (match.GetComponent<RedLightPresentation>() != null) return;
            var presentation = match.gameObject.AddComponent<RedLightPresentation>(); presentation.match = match;
            var root = new GameObject("Permanent digital stadium boards and loudspeaker towers").transform;
            var metal = SportsEventAuthoring.Mat(RedLightAuthoring.Folder, "PA tower navy steel", new Color(.025f,.04f,.065f), .25f);
            var cone = SportsEventAuthoring.Mat(RedLightAuthoring.Folder, "PA cone graphite", new Color(.10f,.12f,.14f), .1f);
            presentation.boards = new TMP_Text[2];
            for (int side = 0; side < 2; side++)
            {
                float x = side == 0 ? -64 : 64;
                SportsEventAuthoring.Box("Digital standings physical display", new Vector3(x,15,280),new Vector3(22,21,1),metal,null,root);
                foreach (int leg in new[]{-1,1}) SportsEventAuthoring.Box("Scoreboard support",new Vector3(x+leg*8,5,280),new Vector3(.8f,10,.8f),metal,null,root);
                var label = new GameObject("Live six-driver digital standings").AddComponent<TextMeshPro>(); label.transform.SetParent(root); label.transform.position = new Vector3(x,15,279.4f); label.rectTransform.sizeDelta = new Vector2(20,19); label.font = Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme").headingFont; label.fontSize = 5; label.enableAutoSizing = true; label.fontSizeMin = 2; label.fontSizeMax = 5; label.alignment = TextAlignmentOptions.TopLeft; label.text = "LIVE STADIUM STANDINGS"; presentation.boards[side] = label;
                for (int bank = 0; bank < 3; bank++)
                {
                    var tower = SportsEventAuthoring.Box("Large original stadium PA speaker enclosure",new Vector3(x,6+bank*4,235),new Vector3(4,3.7f,2.4f),metal,null,root);
                    foreach (float height in new[]{-.8f,.8f}) { var driver = GameObject.CreatePrimitive(PrimitiveType.Cylinder); driver.name = "Visible recessed stadium loudspeaker cone"; Object.DestroyImmediate(driver.GetComponent<Collider>()); driver.transform.SetParent(tower.transform,false); driver.transform.localPosition = new Vector3(0,height/3.7f,-.53f); driver.transform.localRotation = Quaternion.Euler(90,0,0); driver.transform.localScale = new Vector3(.65f,.06f,.65f); driver.GetComponent<Renderer>().sharedMaterial = cone; }
                }
            }
            var smokeMaterial = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckDemolitionDerby/Materials/Soft alpha smoke.mat")); AssetDatabase.CreateAsset(smokeMaterial,"Assets/TruckRedLightGreenLight/Materials/Pooled violation smoke.mat");
            var exhaust = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NWH/Vehicle Physics 2/Effects/Prefabs/ExhaustSmoke.prefab");
            foreach (var fireball in match.explosions)
            {
                var smoke = Object.Instantiate(exhaust,fireball.transform).GetComponent<ParticleSystem>(); smoke.name = "Pooled NWH-derived heavy violation smoke"; smoke.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); var main=smoke.main; main.loop=false; main.playOnAwake=false; main.startLifetime=new ParticleSystem.MinMaxCurve(2,4); main.startSpeed=new ParticleSystem.MinMaxCurve(2,7); main.startSize=new ParticleSystem.MinMaxCurve(5,11); main.startColor=new Color(.15f,.15f,.16f,.7f); main.maxParticles=40; main.simulationSpace=ParticleSystemSimulationSpace.World; var emit=smoke.emission; emit.rateOverTime=0; emit.SetBursts(new[]{new ParticleSystem.Burst(0,30)}); smoke.GetComponent<ParticleSystemRenderer>().sharedMaterial=smokeMaterial;
                var sparks = new GameObject("Pooled actual violation sparks").AddComponent<ParticleSystem>(); sparks.transform.SetParent(fireball.transform,false); SportsChaosAuthoring.FixBlast(sparks,SportsChaosAuthoring.Flare(RedLightAuthoring.Folder)); var spark=sparks.main; spark.startSize=new ParticleSystem.MinMaxCurve(.08f,.2f); spark.startSpeed=new ParticleSystem.MinMaxCurve(15,32); spark.startLifetime=new ParticleSystem.MinMaxCurve(.5f,1.1f); spark.gravityModifier=1.2f; spark.maxParticles=64; spark.simulationSpace=ParticleSystemSimulationSpace.World;
            }
            presentation.impulse = match.gameObject.AddComponent<CinemachineImpulseSource>(); presentation.impulse.m_ImpulseDefinition.m_ImpulseChannel = 8; presentation.impulse.m_ImpulseDefinition.m_ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform; presentation.impulse.m_ImpulseDefinition.m_ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Explosion; presentation.impulse.m_ImpulseDefinition.m_ImpulseDuration = .45f;
            presentation.listeners = new[]{match.broadcast.view}.Concat(match.drivingViews.rally.views.Select(c=>c.view)).Select(camera=> {var listener=camera.gameObject.AddComponent<CinemachineIndependentImpulseListener>(); listener.m_ChannelMask=8; listener.m_Gain=.7f; listener.m_UseLocalSpace=true; listener.m_ReactionSettings=new CinemachineImpulseListener.ImpulseReaction{m_AmplitudeGain=.3f,m_FrequencyGain=1,m_Duration=.5f}; return listener;}).ToArray();
            EditorUtility.SetDirty(match);
        }
    }
}
