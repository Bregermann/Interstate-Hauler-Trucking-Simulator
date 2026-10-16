using UnityEngine;
namespace LWS.TruckSports {
public static class SportsMeasure {
 public static string Feet(float metres)=>(metres*3.2808399f).ToString("F1")+" ft";
 public static string Miles(float metres)=>(metres/1609.344f).ToString("F2")+" mi";
 public static string Speed(float metresPerSecond)=>Mathf.RoundToInt(metresPerSecond*2.2369363f)+" MPH";
}
}
