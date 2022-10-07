
using Domain.Elements;
using System.Collections.Generic;

namespace Core.GroupBehaviour.SimpleGroup
{
    /// <summary>
    /// Class to handle basic groupd data
    /// </summary>
    internal class GroupData
    {
        private Dictionary<int,  LeaderLevelData> _leaderLevelData =
            new Dictionary<int, LeaderLevelData>();

        private List<int> _followersID = new List<int>();

        public GroupData()
        {
            EvacuationLevelID = -1;
        }

        public GroupData(int nAgents, int groupID)
        {
            NAgents = nAgents;
            GroupID = groupID;
            EvacuationLevelID = -1;
        }

        public void SaveFollowerID(int id)
        {
            if (!_followersID.Contains(id))
                _followersID.Add(id);
        }

        public int NAgents { get; set; }
        public int LeaderID { get; set; }
        public int GroupID { get; set; }
        public int EvacuationLevelID { get; set; }

        public bool LevelDataExists(int levelID)
        {
            return _leaderLevelData.ContainsKey(levelID);
        }

        public List<int> EvacPath(int levelID)
        {
            if (_leaderLevelData.ContainsKey(levelID))
                return _leaderLevelData[levelID].EvacPath();

            return null;
        }

        public List<int> Followers { get { return _followersID; } }

        public LeaderLevelData LeaderData(int levelID)
        {
            if (_leaderLevelData.ContainsKey(levelID))
                return _leaderLevelData[levelID];

            return null;
        }

        public void AddNewGroupDetails(int levelID, LeaderLevelData lData)
        {
            _leaderLevelData.Add(levelID, lData);
        }

        public bool EvacPathIsEvailable(int levelID)
        {
            if (_leaderLevelData.ContainsKey(levelID))
                return _leaderLevelData[levelID].LeaderPathIsSet();

            return false;
        }

        public bool IsGateSet(int levelID, int roomID)
        {
            if (_leaderLevelData.ContainsKey(levelID))
                return _leaderLevelData[levelID].IsGateSet(roomID);

            return false;
        }

        public RoomElement Gate(int levelID, int roomID)
        {
            if (_leaderLevelData.ContainsKey(levelID))
                return _leaderLevelData[levelID].Gate(roomID);

            return null;
        }
    }
}



