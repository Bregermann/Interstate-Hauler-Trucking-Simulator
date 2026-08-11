// Spline Spawner © Staggart Creations (http://staggart.xyz)
// COPYRIGHT PROTECTED UNDER THE UNITY ASSET STORE EULA (https://unity.com/legal/as-terms)
//
// ⚠️ WARNING: UNAUTHORIZED USE OR DISTRIBUTION IS STRICTLY PROHIBITED
// • Copying, referencing, or reverse-engineering this source code for the creation of new Asset Store or derivative products,
//   or any other publicly distributed content is strictly forbidden and will result in legal action.
// • Studying this file for the purpose of reproducing its functionality in your own assets or tools is not permitted.
// • If you are viewing this file as a reference, please close it immediately to avoid unintentional design influence or potential EULA violations.
// • Uploading this file or any derivative of it to a public GitHub or similar repository will trigger an automated DMCA takedown request.
// • Studying to understand for personal, educational or integration purposes is allowed, studying to reproduce is not.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace sc.splines.spawner.runtime
{
    public partial class SplineSpawner : MonoBehaviour
    {
        private void ValidateContainers()
        {
            int removed = containers.RemoveAll(item => !item || item.transform.parent != root);

            if (removed > 0)
            {
                //Debug.LogWarning($"[Spline Spawner] Removed {removed} missing instances containers from {this.name}. Avoid deleting Container objects manually.");
            }

            int adopted = 0;
            //If the root is empty, containers will not be children of the spawner.
            //Duplicating a spawner would mean the containers aren't duplicated
            for (int i = 0; i < containers.Count; i++)
            {
                //Belongs to the original, manually duplicate and adopt it
                if (containers[i].owner != this)
                {
                    SplineInstanceContainer newContainer = GameObject.Instantiate(containers[i], root);
                    newContainer.owner = this;
                    
                    containers[i] = newContainer;

                    adopted++;
                }
            }
            
            //Scan only top level child objects
            int childObjectCount = this.transform.childCount;
            for (int i = 0; i < childObjectCount; i++)
            {
                Transform child = transform.GetChild(i);
                SplineInstanceContainer container = child.GetComponent<SplineInstanceContainer>();
                
                if(!container) continue;
                
                if(container.owner == null || container.owner != this)
                {
                    container.owner = this;
                    adopted++;
                }
            }

            if (adopted > 0)
            {
                Debug.Log($"[Spline Spawner] {adopted} orphaned instances containers were found under {this.name}. So they have been adopted. This may happen when duplicating a Spline Spawner or when using a Root transform that another spawner also uses.");
            }
            
            if (splineCount != containers.Count)
            {
                //Debug.LogWarning($"Mismatching number of object containers ({containers.Count}) relative to the number of splines ({splineCount}). Synchronizing them now. This may happen if containers are manually deleted, or the spline container was changed.");

                //Remove excess containers
                for (int i = containers.Count - 1; i >= splineCount; i--)
                {
                    containers[i].Destroy();
                    containers.RemoveAt(i);
                }

                //Add missing containers
                for (int i = containers.Count; i < splineCount; i++)
                {
                    containers.Add(SplineInstanceContainer.Create(this, i));
                }
            }
        }
        
        public bool HasMissingPrefabs()
        {
            for (int i = 0; i < inputObjects.Count; i++)
            {
                if (!inputObjects[i].prefab) return true;
            }

            return false;
        }
        
        /// <summary>
        /// Editor only. Checks if the instance is a prefab in the project window. Respawning may involve deleting objects or changing transform hierarchies.
        /// Which is only possible for scene instances or inside a prefab editing context.
        /// </summary>
        /// <returns></returns>
        public bool IsAllowedToSpawn(bool suppressError = true)
        {
#if UNITY_EDITOR
            //Prefab selected in the Project window, with a Spline Mesher component on its root
            bool inspectingPrefab = UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this.gameObject) && this.gameObject.scene.name != string.Empty;
            
            if (inspectingPrefab && !suppressError)
            {
                Debug.LogError("[Spline Mesher] Unable to rebuild, since the instance is a prefab in the project, not a scene instance. Unity disallows modifying prefab files directly.", this);
                return false;
            }
            
            return inspectingPrefab == false;
#else
            return true;
#endif
        }
    }
}