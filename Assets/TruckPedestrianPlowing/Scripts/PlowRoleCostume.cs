using UnityEngine;
using System.Collections.Generic;
namespace LWS.TruckPedestrianPlowing {
 // Role wardrobe is layered over the shared character roster. Authored skin materials stay untouched.
 public sealed class PlowRoleCostume:MonoBehaviour {
  public GameObject standard,bonus,penalty,pitchfork;
  public GameObject[] bonusAttachments=System.Array.Empty<GameObject>(),penaltyAttachments=System.Array.Empty<GameObject>();
  public Renderer[] clothing=System.Array.Empty<Renderer>();
  Material[][] authoredMaterials;bool registered;
  static readonly Dictionary<Material,Dictionary<Color,Material>> palette=new Dictionary<Material,Dictionary<Color,Material>>();
  static int live,materialCount;
  public static int SharedMaterialCount=>materialCount;
  void Awake(){registered=true;live++;authoredMaterials=new Material[clothing.Length][];for(int i=0;i<clothing.Length;i++)authoredMaterials[i]=clothing[i]!=null?clothing[i].sharedMaterials:System.Array.Empty<Material>();}
  static Material Fabric(Material source,Color color){
   if(source==null)return null;
   if(!palette.TryGetValue(source,out var variants)){variants=new Dictionary<Color,Material>();palette.Add(source,variants);}
   if(!variants.TryGetValue(color,out var material)){
    material=new Material(source){name=source.name+" · shared role fabric",hideFlags=HideFlags.DontSave};
    if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",color);if(material.HasProperty("_Color"))material.SetColor("_Color",color);
    variants.Add(color,material);materialCount++;
   }
   return material;
  }
  [Header("Role fabric colors — never skin")]
  public Color bonusFabric=new Color(.018f,.022f,.032f),penaltyFabric=new Color(.92f,.035f,.07f);
  public Color[] standardFabric={new Color(.08f,.7f,.9f),new Color(.57f,.31f,.95f),new Color(.19f,.82f,.4f),new Color(.98f,.52f,.07f),new Color(.96f,.7f,.16f)};
  public void Apply(PlowTargetKind role,int id){
   standard.SetActive(role==PlowTargetKind.Standard);bonus.SetActive(role==PlowTargetKind.BonusRunner);penalty.SetActive(role==PlowTargetKind.PenaltyMarshal);
   foreach(var part in bonusAttachments)part.SetActive(role==PlowTargetKind.BonusRunner);foreach(var part in penaltyAttachments)part.SetActive(role==PlowTargetKind.PenaltyMarshal);
   pitchfork.SetActive(id%7==0);
   Color color=role==PlowTargetKind.BonusRunner?bonusFabric:role==PlowTargetKind.PenaltyMarshal?penaltyFabric:standardFabric.Length>0?standardFabric[id%standardFabric.Length]:Color.cyan;
   // Shared material variants retain URP batching; per-renderer tint blocks multiplied SetPass cost.
   for(int i=0;i<clothing.Length;i++){var renderer=clothing[i];if(renderer==null)continue;var materials=new Material[authoredMaterials[i].Length];for(int j=0;j<materials.Length;j++)materials[j]=Fabric(authoredMaterials[i][j],color);renderer.sharedMaterials=materials;renderer.SetPropertyBlock(null);}
  }
  void OnDestroy(){if(!registered)return;registered=false;if(--live!=0)return;foreach(var variants in palette.Values)foreach(var material in variants.Values)if(material!=null)Destroy(material);palette.Clear();materialCount=0;}
  public bool IsDynamic(Renderer renderer){if(System.Array.IndexOf(clothing,renderer)>=0)return true;foreach(var root in new[]{standard,bonus,penalty,pitchfork})if(root!=null&&renderer.transform.IsChildOf(root.transform))return true;foreach(var root in bonusAttachments)if(renderer.transform.IsChildOf(root.transform))return true;foreach(var root in penaltyAttachments)if(renderer.transform.IsChildOf(root.transform))return true;return false;}
 }
}
