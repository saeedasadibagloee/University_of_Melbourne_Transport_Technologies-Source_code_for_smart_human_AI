using Core.GroupBehaviour.DecisionUpdate;
using Core.GroupBehaviour.SimpleGroup;
using Domain.Elements;
using System.Collections.Generic;
using System.Linq;

namespace Core.GroupBehaviour
{
    /// <summary>
    /// Main handler of GroupData
    /// </summary>
    internal class GroupHandler : IGroupHandler
    {
 
        //used in dymanic group generation
        private List<GroupData> _recentGroups = null;

        public GroupHandler() { }
        public GroupHandler(GroupHandler pHandler)//Dictionary<int, GroupData> pGroupDataMap)
        {
            foreach (var key in pHandler.groupDataMap.Keys)
                groupDataMap.Add(key, pHandler.groupDataMap[key]);
        }

        public int NewGroupID()
        {
            return ++_groupID;
        }

        public void AddNewGroup(GroupData gData, bool dynamicGroupGenerationCase = false)
        {
            if (!groupDataMap.ContainsKey(gData.GroupID))                       
                groupDataMap.Add(gData.GroupID, gData);

            if (dynamicGroupGenerationCase)
            {
                if (_recentGroups == null)
                    _recentGroups = new List<GroupData>();

                if (!_recentGroups.Any(group => group.GroupID == gData.GroupID))
                    _recentGroups.Add(gData);
            } 
        }

        public Dictionary<int, GroupData> GroupDataMap()
        {
            return groupDataMap;
        }

        public GroupData GetGroupData(int groupID)
        {
            if (groupDataMap.ContainsKey(groupID))
                return groupDataMap[groupID];

            return null;
        }      

            return null;
        }

        public bool GroupsShouldBeHandled()
        {
            return groupDataMap.Count != 0;
        }

        public bool EvacuationPathIsReady(int groupID, int levelID)
        {
            if (groupDataMap.ContainsKey(groupID))
                return groupDataMap[groupID].EvacPathIsEvailable(levelID);

            return false;
        }

        public List<int> EvacPath(int groupID, int levelID)
        {
            if (groupDataMap.ContainsKey(groupID))
                return groupDataMap[groupID].EvacPath(levelID);

            return null;
        }

        public void SaveNewGroupUpdate(GroupUpdateData newGroupUpdate)
        {
            _recentGroupDataUpdates.Add(newGroupUpdate);
        }

        public bool NewUpdatesExist()
        {
            return _recentGroupDataUpdates.Count != 0;
        }

        public bool NewGroupsWereCreated()
        {
            if (_recentGroups == null)
                return false;

            return _recentGroups.Count != 0;
        }

        public List<GroupUpdateData> GetUpdatedEntries()
        {
            List<GroupUpdateData> updates = new List<GroupUpdateData>();
            updates.AddRange(_recentGroupDataUpdates);
            _recentGroupDataUpdates.Clear();

            return updates;
        }

        public List<GroupData> RecentGroups()
        {
            if (_recentGroups != null && _recentGroups.Count != 0)
            {
                var recentGroupsCopy = new List<GroupData>();
                recentGroupsCopy.AddRange(_recentGroups);
                _recentGroups.Clear();

                return recentGroupsCopy;
            }
            else
                return null;
        }

        public void UpdateEvacPath(int groupID, int levelID, List<int> path)
        {
            if (groupDataMap.ContainsKey(groupID))
            {
                var leaderDataPerLevel = groupDataMap[groupID].LeaderData(levelID);

                if (leaderDataPerLevel != null)
                {
                    leaderDataPerLevel.SetLeaderEvacuationPath(path);
                }
                else
                {
                    groupDataMap[groupID].AddNewGroupDetails(levelID, new LeaderLevelData(path));
                }
            }
        }

        public void UpdateGateInfo(int groupID, int levelID, int roomID, RoomElement gate)
        {
            if (groupDataMap.ContainsKey(groupID))
            {
                var leaderDataPerLevel = groupDataMap[groupID].LeaderData(levelID);

                if (leaderDataPerLevel != null)
                    leaderDataPerLevel.UpdateRoomEntry(roomID, new GateData(gate));
            }
        }

        public RoomElement Gate(int groupID, int levelID, int roomID)
        {
            if (groupDataMap.ContainsKey(groupID))
                return groupDataMap[groupID].Gate(levelID, roomID);

            return null;
        }

        public bool IsGateSet(int groupID, int levelID, int roomID)
        {
            if (groupDataMap.ContainsKey(groupID))
                return groupDataMap[groupID].IsGateSet(levelID, roomID);

            return false;
        }

        public void SetEvacuationLevelID(int groupID, int levelID)
        {
            if (groupDataMap.ContainsKey(groupID))
                groupDataMap[groupID].EvacuationLevelID = levelID;
        }

        public int EvacuationLevelID(int groupID)
        {
            if (groupDataMap.ContainsKey(groupID))
                return groupDataMap[groupID].EvacuationLevelID;

            return -1;
        }

        public void Clear()
        {
            groupDataMap.Clear();
            _groupID = 0;
        }        
    }
}
