using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckSkyTow.Editor
{
    public static class SkyTowRepairAuthoring
    {
        const string Art="Assets/TruckSkyTow/Art/";
        const string Snow="Assets/NOT_Lonely/Weatherade SRS/Samples/SharedContent/Particles/SnowHitFX.prefab";
        [MenuItem("Truck Sports/Sky Tow/Apply playtest gate repair once")]
        public static void Upgrade()
        {
            EditorSceneManager.OpenScene(SkyTowAuthoring.Scene);var match=UnityEngine.Object.FindAnyObjectByType<SkyTowMatch>();
            if(match==null)throw new InvalidOperationException("Saved Sky Tow match missing.");
            if(match.course.playtestRepairVersion>=3)return;
            if(match.course.playtestRepairVersion>=1){Frame("Arctic standard gate",8);Frame("Arctic precision gold gate",4.8f);if(match.course.playtestRepairVersion<2)BoundBursts(match);Save(match);return;}
            Material coral=Material("Expedition safety coral"),navy=Material("Arctic marine navy"),white=Material("Expedition warm white"),gold=Material("Gold bonus rings");
            var normal=Frame("Arctic standard gate",8);var precision=Frame("Arctic precision gold gate",4.8f);
            foreach(var ring in match.course.rings)
            {
                match.course.Frame(ring.at,ring.slot,out var boatPoint,out var boatRotation);
                var oldNominal=boatPoint+boatRotation*new Vector3(0,18,-29);
                var authoredOffset=Quaternion.Inverse(boatRotation)*(ring.transform.position-oldNominal);
                match.course.NominalFrame(ring.at,ring.slot,out var delayedPoint,out var delayedRotation);
                ring.transform.SetPositionAndRotation(delayedPoint+delayedRotation*authoredOffset,delayedRotation);
                ring.radius=ring.bonus?4.8f:8;ring.rimThickness=.28f;
                ring.GetComponent<MeshFilter>().sharedMesh=ring.bonus?precision:normal;
                ring.art.sharedMaterials=new[]{ring.bonus?gold:coral,navy,white,ring.bonus?gold:white};
                ring.name=(ring.bonus?"Precision GOLD":"Broad Arctic")+" gate "+(ring.index+1)+" lane "+(ring.slot+1);
                EditorUtility.SetDirty(ring);
            }
            match.course.followDelayedTowPath=true;
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Snow);if(source==null)throw new InvalidOperationException("Audited owned Weatherade snow burst missing.");
            var feedback=new GameObject("Pooled Arctic gate snow bursts").AddComponent<SkyTowGateFeedback>();feedback.transform.SetParent(match.transform,false);feedback.bursts=new ParticleSystem[4];
            for(int i=0;i<4;i++)
            {
                var burst=(GameObject)PrefabUtility.InstantiatePrefab(source,match.gameObject.scene);burst.name="Reusable gate snow burst "+(i+1);burst.transform.SetParent(feedback.transform,false);
                // Unpack the owned instance before configuring it; never modify
                // the original vendor prefab or allow its Destroy stop action.
                PrefabUtility.UnpackPrefabInstance(burst,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                foreach(var audio in burst.GetComponentsInChildren<AudioSource>(true)){audio.Stop();audio.enabled=false;audio.playOnAwake=false;}
                foreach(var particle in burst.GetComponentsInChildren<ParticleSystem>(true)){var main=particle.main;main.stopAction=ParticleSystemStopAction.None;main.playOnAwake=false;main.loop=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=Mathf.Min(main.maxParticles,160);particle.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
                feedback.bursts[i]=burst.GetComponentInChildren<ParticleSystem>(true);
            }
            match.gateFeedback=feedback;BoundBursts(match);Save(match);
            Debug.Log("SKYTOW_REPAIR: preserved120-second lanes/physics; normal8m/gold4.8m gates; delayed tow-path alignment; segmented authored gate models; four pooled owned snow bursts.");
        }
        static void Save(SkyTowMatch match)
        {
            match.course.playtestRepairVersion=3;
            EditorUtility.SetDirty(match);EditorUtility.SetDirty(match.course);EditorSceneManager.MarkSceneDirty(match.gameObject.scene);EditorSceneManager.SaveScene(match.gameObject.scene);AssetDatabase.SaveAssets();
        }
        static void BoundBursts(SkyTowMatch match)
        {
            string path="Assets/TruckSkyTow/Materials/Arctic gate snow chips.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")){name="Arctic gate snow chips"};material.SetColor("_BaseColor",new Color(.6f,.78f,.85f,.7f));material.SetFloat("_Cull",0);AssetDatabase.CreateAsset(material,path);}
            foreach(var particle in match.gateFeedback.GetComponentsInChildren<ParticleSystem>(true))
            {
                particle.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=particle.main;main.startLifetime=new ParticleSystem.MinMaxCurve(.4f,.8f);main.startSpeed=new ParticleSystem.MinMaxCurve(.8f,2.5f);main.startSize3D=false;main.startSize=new ParticleSystem.MinMaxCurve(.04f,.12f);main.maxParticles=48;main.gravityModifier=.25f;main.startColor=new Color(.7f,.85f,1,.7f);
                var trails=particle.trails;trails.enabled=false;var lights=particle.lights;lights.enabled=false;var emission=particle.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,18)});
                var shape=particle.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.45f;
                var color=particle.colorOverLifetime;color.enabled=true;var fade=new Gradient();fade.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.7f,0),new GradientAlphaKey(0,1)});color.color=fade;
                var renderer=particle.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterials=new[]{material};renderer.trailMaterial=null;renderer.maxParticleSize=.025f;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;EditorUtility.SetDirty(particle);EditorUtility.SetDirty(renderer);
            }
            Debug.Log("SKYTOW_BOUNDED_SNOW: short non-emissive chips; no sample ground trails, coverage/sparkle shader, light or oversized screen particles.");
        }
        static Material Material(string name){var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckSkyTow/Materials/"+name+".mat");if(material==null)throw new InvalidOperationException("Existing Sky Tow material missing: "+name);return material;}
        static Mesh Frame(string name,float radius)
        {
            string path=Art+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved!=null&&saved.normals.Length==saved.vertexCount&&saved.normals.All(n=>float.IsFinite(n.x)&&float.IsFinite(n.y)&&float.IsFinite(n.z)&&n.sqrMagnitude>.5f))return saved;
            var body=AssetDatabase.LoadAssetAtPath<Mesh>(Art+name+" body.asset")??SkyTowAuthoring.Torus(name+" body",radius,.28f,96,12);
            var trim=AssetDatabase.LoadAssetAtPath<Mesh>(Art+name+" inset.asset")??SkyTowAuthoring.Torus(name+" inset",radius+.5f,.12f,96,8);
            var vertices=new List<Vector3>(body.vertices);var uv=new List<Vector2>(body.uv);var groups=new[]{new List<int>(),new List<int>(),new List<int>(),new List<int>()};
            var triangles=body.triangles;for(int face=0;face<triangles.Length;face+=3){int angle=triangles[face]/12;groups[(angle/8)%3].AddRange(new[]{triangles[face],triangles[face+1],triangles[face+2]});}
            int offset=vertices.Count;vertices.AddRange(trim.vertices);uv.AddRange(trim.uv);foreach(int index in trim.triangles)groups[3].Add(offset+index);
            // Twelve embossed, double-sided inward chevrons are baked into the
            // shared mesh: no144x12extra renderers, objects or colliders.
            for(int arrow=0;arrow<12;arrow++)
            {
                float angle=arrow*Mathf.PI/6;var outward=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);var tangent=new Vector3(-outward.y,outward.x,0);var center=outward*(radius+.64f);
                var outline=new[]{new Vector2(-.48f,.38f),new Vector2(0,-.22f),new Vector2(.48f,.38f),new Vector2(.28f,.55f),new Vector2(0,.2f),new Vector2(-.28f,.55f)};
                // Front/back/side faces have separate vertices. Reversing
                // triangles over the SAME vertices cancels their normals,
                // causing invalid HDR lighting and enormous bloom artifacts.
                int[] faces={0,1,4,0,4,5,1,2,3,1,3,4};
                for(int side=0;side<2;side++){offset=vertices.Count;foreach(var point in outline){vertices.Add(center+tangent*point.x+outward*point.y+Vector3.forward*(side==0?.18f:.1f));uv.Add(point);}if(side==0)foreach(int index in faces)groups[3].Add(offset+index);else for(int i=faces.Length-1;i>=0;i--)groups[3].Add(offset+faces[i]);}
                for(int edge=0;edge<outline.Length;edge++){offset=vertices.Count;var a=center+tangent*outline[edge].x+outward*outline[edge].y;var b=center+tangent*outline[(edge+1)%outline.Length].x+outward*outline[(edge+1)%outline.Length].y;vertices.AddRange(new[]{a+Vector3.forward*.18f,b+Vector3.forward*.18f,a+Vector3.forward*.1f,b+Vector3.forward*.1f});uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one});foreach(int index in new[]{0,2,1,1,2,3})groups[3].Add(offset+index);}
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.subMeshCount=groups.Length;for(int i=0;i<groups.Length;i++)mesh.SetTriangles(groups[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();if(saved!=null){EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);UnityEngine.Object.DestroyImmediate(mesh);return saved;}AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
    }
}
