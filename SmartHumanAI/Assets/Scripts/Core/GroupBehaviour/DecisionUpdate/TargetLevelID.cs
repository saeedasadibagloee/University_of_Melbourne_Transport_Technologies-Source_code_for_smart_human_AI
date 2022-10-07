namespace Core.GroupBehaviour.DecisionUpdate
{
    /// <summary>
    /// Class to store evacuation level ID
    /// This data is broadcasted by a leader to their
    /// followers in the Multi-Level model
    /// </summary>
    internal class TargetLevelID : GroupUpdateData
    {
        public TargetLevelID(int groupID, int levelID)
            : base(groupID, levelID)
        { }

        public override
        GroupUpdateType UpdateType()
        {
            return GroupUpdateType.TargetLevelID;
        }
    }
}

    
