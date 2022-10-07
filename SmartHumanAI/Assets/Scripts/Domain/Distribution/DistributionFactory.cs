using DataFormats;
using Domain.Level;
using System.Collections.Generic;

namespace Domain.Distribution
{
    internal static class DistributionFactory
    {
        public static IDistribution GenerateDistribution(SimpleLevel level, 
                                                         DistributionData dData, 
                                                         List<int> groupIDs)
        {
            var index = level.GetRoomIndex(dData.xPosition, dData.yPosition);

            if (index == -1)
                return null;
            
            IDistribution rDist = null;

            switch (dData.placement)
            {
                case (int)Def.AgentPlacement.Circle:
                    rDist = new RoundDistribution(dData.xPosition, dData.yPosition, dData.radius);
                    break;
                case (int)Def.AgentPlacement.Rectangle:
                    rDist = new RectangularDistribution(dData.RectangleVertices);
                    break;
                case (int)Def.AgentPlacement.Room:
                    rDist = new RoomDistribution(level.TempRooms[index].Coordinates);
                    break;
            }

            if (rDist == null) return null;

            rDist.SetPopulation(dData.population);
            rDist.SetWalls(level.TempRooms[index].WallsAndBarricades);
            rDist.SetColor(dData.color.ToColor());

            if (dData.dynamicDistributionData != null)
            {
                rDist.SetDynamicDistributionData(dData.dynamicDistributionData);
            }

            if (dData.dGatesData != null)
            {
                rDist.SetDesignatedGatesData(dData.dGatesData);
            }

            if (groupIDs != null)
            {
                foreach (var groupID in groupIDs)
                    rDist.SetGroupID(groupID);
            }

            return rDist;
        }
    }   
}

