using System.Collections.Generic;
using Core;
using Core.Logger;
using Helper;
using System.Diagnostics;

namespace Domain.Forces
{
    internal static class NeighborForce
    {
        public static void CalculateForce(Agent agent, float gateX, float gateY, List<Neighbor> neighbors, ref float x, ref float y)
        {
            foreach (var neighbor in neighbors)
            {
                float peerCenterDistance = Utils.DistanceBetween(neighbor.X, neighbor.Y, agent.X, agent.Y);

                float peerOuterDistance = peerCenterDistance - agent.Radius - neighbor.Radius;
                float peerXDirection = (neighbor.X - agent.X) / peerCenterDistance;
                float peerYDirection = (neighbor.Y - agent.Y) / peerCenterDistance;

                agent.DensityTmp += (float)(Params.Current.DensityFactor - peerOuterDistance);

                if (peerOuterDistance <= 0)
                {
                {
                    float angle = ((gateX - agent.X) * (neighbor.X - agent.X) + (gateY - agent.Y) * (neighbor.Y - agent.Y)) /
                        (Utils.DistanceBetween(agent.X, agent.Y, gateX, gateY) * peerCenterDistance);

                    float toSquare = (1 - angle) / 2;
                    float inf = 1 - toSquare * toSquare;

                    var podDrd = peerOuterDistance - Params.Current.DefaultRepultionDistance;
                    var denominator = podDrd * podDrd + Params.Current.DefaultAttractiveForce * Params.Current.DefaultAttractiveForce;

                    if (peerOuterDistance <= Params.Current.DefaultRepultionDistance)
                    {
                        x += inf * Params.Current.DefaultRepulsiveForce * podDrd * peerXDirection / denominator;
                        y += inf * Params.Current.DefaultRepulsiveForce * podDrd * peerYDirection / denominator;
                    }
                    else
                    {
                        x += inf * Params.Current.DefaultAttractiveForce * podDrd * peerXDirection / denominator;
                        y += inf * Params.Current.DefaultAttractiveForce * podDrd * peerYDirection / denominator;
                    }
                }
            }
        }
    }
}
