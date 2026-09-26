using System;
using System.Reflection;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // UTS ships in Assembly-CSharp, so the Taxi asmdef reads its public MovePath
    // contract through cached reflection. This adapter never mutates UTS path state.
    internal sealed class TruckTaxiVehicleObjectivesUtsRoute
    {
        private Type movePathType;
        private FieldInfo pathField, laneField, indexField, totalField, forwardField;
        private MethodInfo nextPoint;
        private bool bound;

        public bool TryUpcomingGoal(Component ai, Vector3 playerPosition,
            TruckTaxiRouteDistanceService routes, Vector3? requiredGoal, out Vector3 goal)
        {
            goal = default;
            if (ai == null || routes == null) return false;
            Component movePath = ai.GetComponent("MovePath");
            if (movePath == null || !Bind(movePath.GetType())) return false;
            try
            {
                var walkPath = pathField.GetValue(movePath) as Component;
                if (walkPath == null) return false;
                int lane = (int)laneField.GetValue(movePath);
                int current = (int)indexField.GetValue(movePath);
                int total = (int)totalField.GetValue(movePath);
                bool forward = (bool)forwardField.GetValue(movePath);
                if (total < 5 || lane < 0) return false;
                for (int steps = 3; steps <= 8; steps++)
                {
                    int candidate = current + (forward ? steps : -steps);
                    if (candidate < 1 || candidate >= total) continue;
                    Vector3 waypoint = (Vector3)nextPoint.Invoke(walkPath,
                        new object[] { lane, candidate });
                    float targetDistance = Vector3.ProjectOnPlane(
                        waypoint - ai.transform.position, Vector3.up).magnitude;
                    float playerDistance = Vector3.ProjectOnPlane(
                        waypoint - playerPosition, Vector3.up).magnitude;
                    if (targetDistance < 35 || targetDistance > 220 || playerDistance < 25)
                        continue;
                    Vector3 candidateGoal = requiredGoal ?? waypoint;
                    if (requiredGoal.HasValue && Vector3.ProjectOnPlane(
                        candidateGoal - waypoint, Vector3.up).sqrMagnitude > 64) continue;
                    if (!routes.Measure(playerPosition, candidateGoal).Navigable) continue;
                    goal = candidateGoal;
                    return true;
                }
            }
            catch (Exception) { return false; }
            return false;
        }

        private bool Bind(Type type)
        {
            if (type == movePathType) return bound;
            movePathType = type;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            pathField = type.GetField("walkPath", flags);
            laneField = type.GetField("w", flags);
            indexField = type.GetField("targetPoint", flags);
            totalField = type.GetField("targetPointsTotal", flags);
            forwardField = type.GetField("forward", flags);
            var pathType = pathField?.FieldType;
            nextPoint = pathType?.GetMethod("getNextPoint", flags, null,
                new[] { typeof(int), typeof(int) }, null);
            bound = pathField != null && laneField != null && indexField != null &&
                totalField != null && forwardField != null && nextPoint != null &&
                nextPoint.ReturnType == typeof(Vector3);
            return bound;
        }
    }
}
