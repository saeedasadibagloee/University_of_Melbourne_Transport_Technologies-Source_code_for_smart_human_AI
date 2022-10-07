using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.Threat
{
    /// <summary>
    /// Threat handling strategy interface
    /// </summary>
    internal interface IThreatHandlingStrategy
    {
        bool IsObjectBlocked(int objectID, List<ILevelThreat> levelThreats);
        bool LevelHasBlockedObjects(List<ILevelThreat> levelThreats);
        List<ILevelThreat> BlockedObjects(List<ILevelThreat> levelThreats);
    }

    /// <summary>
    /// Implementation of Threat handling strategy interface for Room object
    /// </summary>
    internal class RoomThreatHandlingStrategy : IThreatHandlingStrategy
    {
        public bool IsObjectBlocked(int objectID, List<ILevelThreat> levelThreats)
        {
            return levelThreats.Any(
                pThreat => pThreat.Type == ThreatType.RoomBlockade 
                    && pThreat.ObjectID == objectID);
        }

        public bool LevelHasBlockedObjects(List<ILevelThreat> levelThreats)
        {
            return levelThreats.Any(pThreat => pThreat.Type == ThreatType.RoomBlockade);
        }

        public List<ILevelThreat> BlockedObjects(List<ILevelThreat> levelThreats)
        {
            return levelThreats.Where(pThreat => pThreat.Type == ThreatType.RoomBlockade).ToList();
        }
    }

    /// <summary>
    /// Implementation of Threat handling strategy interface for Gate object
    /// </summary>
    internal class GateThreatHandlingStrategy : IThreatHandlingStrategy
    {
        public bool IsObjectBlocked(int objectID, List<ILevelThreat> levelThreats)
        {
            return levelThreats.Any(
                pThreat => pThreat.Type == ThreatType.GateBlockade
                    && pThreat.ObjectID == objectID);
        }

        public bool LevelHasBlockedObjects(List<ILevelThreat> levelThreats)
        {
            return levelThreats.Any(pThreat => pThreat.Type == ThreatType.GateBlockade);
        }

        public List<ILevelThreat> BlockedObjects(List<ILevelThreat> levelThreats)
        {
            return levelThreats.Where(pThreat => pThreat.Type == ThreatType.GateBlockade).ToList();
        }
    }
}
