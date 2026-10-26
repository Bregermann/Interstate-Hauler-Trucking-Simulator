#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class BilliardsRackVarianceTests
{
 static Vector3[] Rack(float spacing)
 {
  var positions=new Vector3[16];positions[0]=new Vector3(0,.5f,-6);int index=1;
  for(int row=0;row<5;row++)for(int col=0;col<=row;col++)positions[index++]=new Vector3((col-row*.5f)*spacing,.5f,4.5f+row*spacing*Mathf.Sqrt(3)/2);
  return positions;
 }
 static Vector3[] Generate(Vector3[] positions,int seed,float amount)=>(Vector3[])Type.GetType("LWS.TruckBilliards.BilliardsRackLayout, Assembly-CSharp",true).GetMethod("Generate").Invoke(null,new object[]{positions,Enumerable.Repeat(.5f,positions.Length).ToArray(),seed,amount});
 static void Safe(Vector3[] original,Vector3[] actual,float limit)
 {
  Assert.That(actual[0],Is.EqualTo(original[0]));
  for(int i=1;i<actual.Length;i++){Assert.That(actual[i].y,Is.EqualTo(original[i].y));Assert.That(Vector3.Distance(actual[i],original[i]),Is.LessThanOrEqualTo(limit+.00001f));for(int j=i+1;j<actual.Length;j++)Assert.That(Vector3.Distance(actual[i],actual[j]),Is.GreaterThanOrEqualTo(.999999f),"No physical sphere penetration for pair "+i+"/"+j);}
 }
 [Test]
 public void FixedSeedReproducesSubtleObjectOnlyPositionsWithoutTouchingGlobalRandomOrAuthorReferences()
 {
  var original=Rack(1.025f);var retained=(Vector3[])original.Clone();var state=UnityEngine.Random.state;var first=Generate(original,101,.006f);var repeat=Generate(original,101,.006f);var other=Generate(original,202,.006f);
  CollectionAssert.AreEqual(first,repeat);CollectionAssert.AreEqual(retained,original);Assert.That(UnityEngine.Random.state,Is.EqualTo(state));Assert.That(first.Zip(other,(a,b)=>Vector3.Distance(a,b)>.00001f).Any(value=>value),Is.True);Assert.That(first.Zip(original,(a,b)=>Vector3.Distance(a,b)>.00001f).Count(value=>value),Is.GreaterThan(10));Safe(original,first,.006f);
 }
 [Test]
 public void TouchingRackOpensTinyClearanceAndStillHasActualSeededVariation()
 {
  var touching=Rack(1);var first=Generate(touching,101,.006f);var other=Generate(touching,202,.006f);Safe(touching,first,.006f);Safe(touching,other,.006f);
  Assert.That(first.Zip(touching,(a,b)=>Vector3.Distance(a,b)>.00001f).Any(value=>value),Is.True);Assert.That(first.Zip(other,(a,b)=>Vector3.Distance(a,b)>.00001f).Any(value=>value),Is.True,"Zero authored clearance cannot silently remove all seed variation.");
 }
 [Test]
 public void ZeroDisablesImperfectionsAndLargeConfigurationRemainsTiny()
 {
  var original=Rack(1.025f);CollectionAssert.AreEqual(original,Generate(original,101,0));Safe(original,Generate(original,101,100),.008f);
 }
}
#endif
