using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiOfferMapTests
    {
        [TestCase(200,200)]
        [TestCase(0,0)]
        [TestCase(400,400)]
        public void CoincidentOfferLabelsStaySeparateAndInsideMap(float x,float y)
        {
            var placed=new Rect[3];
            for(int i=0;i<3;i++)
            {
                placed[i]=TruckTaxiOfferMap.PlaceLabel(new Vector2(x,y),new Vector2(i==2 ? 225 : 150,38),new Vector2(400,400),placed,i);
                Assert.GreaterOrEqual(placed[i].xMin,0); Assert.GreaterOrEqual(placed[i].yMin,0);
                Assert.LessOrEqual(placed[i].xMax,400); Assert.LessOrEqual(placed[i].yMax,400);
                for(int j=0;j<i;j++) Assert.IsFalse(placed[i].Overlaps(placed[j]));
            }
        }
    }
}
