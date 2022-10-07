using System.Collections.Generic;

namespace Core.GroupBehaviour.DecisionUpdate
{
    /// <summary>
    /// Class to store evacuation path related data
    /// used by a Leader to update their followers
    /// </summary>
    internal class EvacuationPathUpdate : GroupUpdateData
    {        
        private List<int> _evacPath = new List<int>();

        public EvacuationPathUpdate(int groupID, int levelID, List<int> path)
            : base(groupID, levelID)
        {
            _groupID = groupID;
            _levelID = levelID;
            _evacPath.AddRange(path);
        }

        public override 
        GroupUpdateType UpdateType()
        {
            return GroupUpdateType.EvacPathIsFound;
        }

        public override 
        object UpdateInfo()
        {
            return _evacPath;
        }
    }
}


