using Domain.Elements;
using System.Collections.Generic;

namespace Core.GroupBehaviour.SimpleGroup
{
    /// <summary>
    /// This class is in charge to handling
    /// leader's data related to evacuation path,
    /// gate choice in a particular environment, etc.
    /// </summary>
    internal class LeaderEvacPathData
    {
        private List<int> evacPath = new List<int>();

        public List<int> EvacPath { get { return evacPath; } }
        public bool IsSet { get; set; }
    }

    internal class GateData
    {
        private RoomElement _gate = null;

        public GateData(RoomElement gate)
        {
            _gate = gate;
            GateIsSet = true;
        }
        
        public RoomElement Gate { get { return _gate; } }
        public bool GateIsSet { get; set; }
    }

    internal class LeaderLevelData 
    {
        private Dictionary<int, GateData> leaderRoomPropogation =
            new Dictionary<int, GateData>();

        private LeaderEvacPathData leaderEvacPath = new LeaderEvacPathData();
        private bool LeaderRoomDataIsUpdated { get; set; }

        public LeaderLevelData() { }
        public LeaderLevelData(List<int> evacPath)
        {
            SetLeaderEvacuationPath(evacPath);
        }

        public void UpdateRoomEntry(int roomID, GateData gData)
        {
            if (!leaderRoomPropogation.ContainsKey(roomID))
            {
                leaderRoomPropogation.Add(roomID, gData);
            }
            else
            {
                leaderRoomPropogation[roomID] = gData;
            }
        }

        public void SetLeaderEvacuationPath(List<int> path)
        {
            leaderEvacPath.EvacPath.AddRange(path);
            leaderEvacPath.IsSet = true;
        }

        public bool LeaderPathIsSet()
        {
            return leaderEvacPath.IsSet;
        }

        public List<int> EvacPath()
        {
            return leaderEvacPath.EvacPath;
        }

        public bool IsGateSet(int roomID)
        {
            if (leaderRoomPropogation.ContainsKey(roomID))
                return leaderRoomPropogation[roomID].GateIsSet;

            return false;
        }

        public RoomElement Gate(int roomID)
        {
            if (leaderRoomPropogation.ContainsKey(roomID))
                return leaderRoomPropogation[roomID].Gate;

            return null;
        }
    }
}


