#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Ambience
{
    public static partial class DragonKingdomAmbienceAuthoring
    {
        [Serializable]public sealed class RegroundEvidence
        {
            public string scope="EXPLICIT_EXISTING_SCENE_HABITAT_REGROUND_AND_NATIVE_NAVMESH_REBAKE_NOT_NEW_ACTORS_OR_PLAY_ACCEPTANCE";
            public int walkers,birds,sounds;public bool allExistingObjectsAndRouteReferencesPreserved;public string[] navigation;public RegroundHabitat[] habitats;
        }
        [Serializable]public sealed class RegroundHabitat{public string id,region;public Vector3 oldStart,newStart;public string navmesh;public int routePoints;}
        public static RegroundEvidence RegroundExistingForChangedWorld(DragonKingdomWorld world)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||world==null||EditorUtility.IsPersistent(world))throw new InvalidOperationException("Stopped actual existing scene required for explicit habitat migration.");
            var owner=world.GetComponentInChildren<DragonKingdomAmbience>(true);if(owner==null)throw new InvalidOperationException("Existing authored ambience required. This API never reinstalls it.");owner.ValidateBindings();world.ValidateReferences();
            if(owner.walkers.Length!=8||owner.birds.Length!=7||owner.sounds.Length!=9)throw new InvalidOperationException("Preserve all existing8 ground actors,7 birds and9 regional emitters.");
            var oldObjects=new HashSet<UnityEngine.Object>(owner.GetComponentsInChildren<Transform>(true).SelectMany(t=>t.GetComponents<Component>().Cast<UnityEngine.Object>().Append(t.gameObject)));
            var oldRoutes=world.regions.SelectMany(r=>r.ambientRoutes).Concat(owner.walkers.SelectMany(w=>w.route)).Concat(owner.birds.SelectMany(b=>b.route)).ToArray();
            Physics.SyncTransforms();var habitats=new List<Habitat>();
            foreach(var life in owner.walkers)
            {
                string prefix="Offroad habitat ",name=life.transform.parent.name;if(!name.StartsWith(prefix,StringComparison.Ordinal))throw new InvalidOperationException("Unrecognized actual habitat identity: "+name);
                string id=name.Substring(prefix.Length);int phase=life.villager?int.Parse(id.Substring("villager-".Length)):id.Contains("deer")?6:id.Contains("rabbit")?10:14;
                var points=FindGroundLoop(world,world.Region(life.regionId),phase,habitats);if(points.Length!=life.route.Length)throw new InvalidOperationException("Preserve existing waypoint identities/count: "+id);
                habitats.Add(new Habitat{id=id,region=life.regionId,points=points});
            }
            // Prepare every real supported habitat and navigation output before
            // changing any existing actor/waypoint. No original rig or collider
            // is replaced; Unity Navigation remains the only path authority.
            foreach(var habitat in habitats)habitat.data=BakeHabitat(world,habitat);
            var air=new Dictionary<DragonAmbientBird,Vector3[]>();
            foreach(var bird in owner.birds)
            {
                var region=world.Region(bird.regionId);var hint=(region.ambientRoutes[0].position+region.ambientRoutes[1].position)*.5f;Vector3[] chosen=null;Exception last=null;
                foreach(float rise in new[]{0f,20f,40f,70f,100f,140f,180f})
                {try{chosen=FindAirLoop(world,region,hint+Vector3.up*rise);break;}catch(InvalidOperationException e){last=e;}}
                if(chosen==null)throw new InvalidOperationException("No actual swept clear replacement bird loop: "+bird.name,last);
                if(chosen.Length!=bird.route.Length)throw new InvalidOperationException("Preserve existing bird waypoint identities/count.");air.Add(bird,chosen);
            }
            var evidence=new RegroundEvidence{walkers=owner.walkers.Length,birds=owner.birds.Length,sounds=owner.sounds.Length};var rows=new List<RegroundHabitat>();
            for(int i=0;i<owner.walkers.Length;i++)
            {
                var walker=owner.walkers[i];var habitat=habitats[i];rows.Add(new RegroundHabitat{id=habitat.id,region=habitat.region,oldStart=walker.authoredPosition,newStart=habitat.points[0],routePoints=habitat.points.Length,navmesh=AssetDatabase.GetAssetPath(habitat.data)});
                for(int p=0;p<walker.route.Length;p++){walker.route[p].position=habitat.points[p];EditorUtility.SetDirty(walker.route[p]);}
                walker.authoredPosition=habitat.points[0];walker.transform.SetPositionAndRotation(walker.authoredPosition,walker.authoredRotation);EditorUtility.SetDirty(walker);EditorUtility.SetDirty(walker.transform);
            }
            foreach(var pair in air)
            {
                var bird=pair.Key;for(int p=0;p<bird.route.Length;p++){bird.route[p].position=pair.Value[p];EditorUtility.SetDirty(bird.route[p]);}
                bird.authoredPosition=pair.Value[0];bird.transform.SetPositionAndRotation(bird.authoredPosition,bird.authoredRotation);EditorUtility.SetDirty(bird);EditorUtility.SetDirty(bird.transform);
            }
            foreach(var sound in owner.sounds){sound.source.transform.position=SoundPosition(world,world.Region(sound.regionId));EditorUtility.SetDirty(sound.source.transform);}
            owner.navigation=habitats.Select(h=>h.data).ToArray();EditorUtility.SetDirty(owner);
            var afterObjects=new HashSet<UnityEngine.Object>(owner.GetComponentsInChildren<Transform>(true).SelectMany(t=>t.GetComponents<Component>().Cast<UnityEngine.Object>().Append(t.gameObject)));
            var afterRoutes=world.regions.SelectMany(r=>r.ambientRoutes).Concat(owner.walkers.SelectMany(w=>w.route)).Concat(owner.birds.SelectMany(b=>b.route)).ToArray();
            evidence.allExistingObjectsAndRouteReferencesPreserved=oldObjects.SetEquals(afterObjects)&&oldRoutes.SequenceEqual(afterRoutes);
            if(!evidence.allExistingObjectsAndRouteReferencesPreserved)throw new InvalidOperationException("Habitat migration replaced an existing object or route reference.");
            owner.ValidateBindings();evidence.navigation=owner.navigation.Select(AssetDatabase.GetAssetPath).ToArray();evidence.habitats=rows.ToArray();return evidence;
        }
    }
}
#endif
