using System;
namespace LWS.TruckSnowmanBuilding
{
    // Mode-owned route arithmetic only. Actual movement, contacts and braking remain NWH/PhysX.
    public static class SnowmanDeliveryRoute
    {
        public const double ArrivalRadius=1.4; // Encloses DriveTo's existing 1.3m stop radius.
        public static bool Arrived(double dx,double dz)=>dx*dx+dz*dz<=ArrivalRadius*ArrivalRadius;
        public static double InterceptX(double x,double z,double vx,double vz)
        {
            // Front contact is expected near z=-8 with the parked original tractor at z=-3.
            // Bounded prediction is refreshed while approaching; the parked tractor stays physical.
            double seconds=Math.Min(10,Math.Max(0,(-8-z)/Math.Max(.1,vz)));
            return x+vx*seconds;
        }
    }
}
