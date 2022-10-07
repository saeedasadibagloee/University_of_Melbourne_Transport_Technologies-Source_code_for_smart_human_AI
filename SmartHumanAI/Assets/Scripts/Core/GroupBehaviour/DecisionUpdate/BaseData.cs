using System.Collections.Generic;

namespace Core.GroupBehaviour.DecisionUpdate
{
    enum GroupUpdateType { None, EvacPathIsFound, TargetLevelID, GateIsSet };

    /// <summary>
    /// Base class to hold group update related data
    /// </summary>
    internal abstract class GroupUpdateData
    {
        protected int _groupID = -1;
        protected int _levelID = -1;

        public GroupUpdateData(int groupID, int levelID)
        {
            _groupID = groupID;
            _levelID = levelID;            
        }

        public int GetGroupID { get { return _groupID; } }
        public int LevelID { get { return _levelID; } }

        public virtual GroupUpdateType UpdateType() { return GroupUpdateType.None; }
        public virtual object UpdateInfo() { return null; }
    }

    
}
    