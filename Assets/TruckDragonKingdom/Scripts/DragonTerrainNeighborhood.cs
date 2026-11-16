using System;
using System.Linq;
using UnityEngine;

namespace LWS.TruckDragonKingdom
{
    // The coastal band has real800/1600m rectangular tiles. Unity's automatic
    // grid inference assumes one common tile size. Keep Unity's native terrain
    // and LOD authority, supplying only this explicit authored adjacency.
    [DisallowMultipleComponent,DefaultExecutionOrder(10000)]
    public sealed class DragonTerrainNeighborhood : MonoBehaviour
    {
        public Terrain[] terrains=Array.Empty<Terrain>();
        void OnEnable(){if(terrains!=null&&terrains.Length!=0)Connect();}
        public void Connect()
        {
            if(terrains==null||terrains.Length!=16||terrains.Any(t=>t==null||t.terrainData==null)||terrains.Distinct().Count()!=16)throw new InvalidOperationException("The authored coastal neighborhood requires all sixteen distinct actual terrain tiles.");
            ConnectOwnedTerrains(terrains,true);
        }
        public static void ConnectOwnedTerrains(Terrain[] all,bool manual)
        {
            foreach(var terrain in all)
            {
                if(terrain==null||terrain.terrainData==null)throw new InvalidOperationException("Missing actual authored terrain neighbor.");
                var p=terrain.transform.position;var size=terrain.terrainData.size;
                Terrain Find(Func<Terrain,bool> test)=>all.SingleOrDefault(t=>t!=terrain&&test(t));
                var left=Find(t=>Mathf.Abs(t.transform.position.x+t.terrainData.size.x-p.x)<.01f&&Mathf.Abs(t.transform.position.z-p.z)<.01f&&Mathf.Abs(t.terrainData.size.z-size.z)<.01f);
                var right=Find(t=>Mathf.Abs(t.transform.position.x-p.x-size.x)<.01f&&Mathf.Abs(t.transform.position.z-p.z)<.01f&&Mathf.Abs(t.terrainData.size.z-size.z)<.01f);
                var bottom=Find(t=>Mathf.Abs(t.transform.position.z+t.terrainData.size.z-p.z)<.01f&&Mathf.Abs(t.transform.position.x-p.x)<.01f&&Mathf.Abs(t.terrainData.size.x-size.x)<.01f);
                var top=Find(t=>Mathf.Abs(t.transform.position.z-p.z-size.z)<.01f&&Mathf.Abs(t.transform.position.x-p.x)<.01f&&Mathf.Abs(t.terrainData.size.x-size.x)<.01f);
                // AutoConnect clears connections on the first render frame
                // only for tiles whose allowAutoConnect remains enabled.
                terrain.allowAutoConnect=!manual;terrain.SetNeighbors(left,top,right,bottom);
            }
        }
    }
}
