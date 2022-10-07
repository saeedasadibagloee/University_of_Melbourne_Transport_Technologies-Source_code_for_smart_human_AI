using Core.GroupBehaviour.DecisionUpdate;
using Core.GroupBehaviour.SimpleGroup;
using Domain.Elements;
using System.Collections.Generic;

namespace Core.GroupBehaviour
{
    /// <summary>
    /// General interface to implement group API
    /// </summary>
    internal interface IGroupGeneral
    {   
        void AddNewGroup(GroupData gData, bool dynamicGroupGenerationCase = false);
        GroupData GetGroupData(int groupID);
        bool GroupsShouldBeHandled();
       
        List<int> FollowersByGroupID(int groupID); 
        Dictionary<int, GroupData> GroupDataMap();

        void Clear();
    }

    /// <summary>
    /// Interface to handle evacuation level related params
    /// </summary>
    internal interface IGroupEvacLevel

    /// <summary>
    /// Main interface to handle groups
    /// </summary>
    internal interface IGroupHandler : IGroupGeneral, IGroupEvacLevel
    {
        int NewGroupID();

        bool EvacuationPathIsReady(int groupID, int levelID);

        void SaveNewGroupUpdate(GroupUpdateData newGroupUpdate);

        bool NewUpdatesExist();
        List<GroupUpdateData> GetUpdatedEntries();

        bool NewGroupsWereCreated();
        List<GroupData> RecentGroups();
        
        void UpdateEvacPath(int groupID, int levelID, List<int> path);
        void UpdateGateInfo(int groupID, int levelID, int roomID, RoomElement gate);
    }
}
   


    

