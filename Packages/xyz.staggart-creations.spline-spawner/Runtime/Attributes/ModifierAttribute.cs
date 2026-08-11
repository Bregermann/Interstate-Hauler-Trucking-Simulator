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

using System;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace sc.splines.spawner.runtime
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ModifierAttribute : Attribute
    {
        public readonly string displayName;
        public readonly string description;
        public readonly DistributionSettings.DistributionMode[] incompatibleDistributionModes;
        
        public ModifierAttribute(string displayName, string description, DistributionSettings.DistributionMode[] disallowedDistributionModes = null)
        {
            this.displayName = displayName;
            this.description = description;
            this.incompatibleDistributionModes = disallowedDistributionModes;
        }

        public bool IsIncompatibleDistributionMode(DistributionSettings.DistributionMode mode)
        {
            if (incompatibleDistributionModes == null) return false;
            
            for (int i = 0; i < incompatibleDistributionModes.Length; i++)
            {
                if(incompatibleDistributionModes[i] == mode) return true;
            }

            return false;
        }

        #if UNITY_EDITOR
        public static ModifierAttribute GetFor(SerializedProperty modifierProperty)
        {
            Modifier modifier = modifierProperty.managedReferenceValue as Modifier;
            return GetFor(modifier.GetType());
        }
        #endif
        
        public static ModifierAttribute GetFor(Type type)
        {
            return (ModifierAttribute)type.GetCustomAttribute(typeof(ModifierAttribute));
        }
    }
}