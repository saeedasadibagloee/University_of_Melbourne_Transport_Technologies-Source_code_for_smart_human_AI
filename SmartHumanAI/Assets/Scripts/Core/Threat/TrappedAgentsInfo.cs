using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.Threat
{
    internal class TrappedPerRoom
    {
        private int  _roomID = -1;
        private int _nAgents = 0;

        public TrappedPerRoom(int roomID)
        {
            this._roomID = roomID;
            ++this._nAgents;
        }

        public int RoomID { get { return this._roomID; } }
        public int NAgents { get { return this._nAgents; } }
        public void Increment() { ++this._nAgents; }
    }

    internal class TrappedAgentsInfo
    {
        private Dictionary<int, List<TrappedPerRoom>> info =
            new Dictionary<int, List<TrappedPerRoom>>();

        public TrappedAgentsInfo() { }

        public void UpdateData(int pLevelID, int roomID)
        {
            if (info.ContainsKey(pLevelID))
            {
                var trmData = info[pLevelID].FirstOrDefault(pData => pData.RoomID == roomID);

                if (trmData != null)                
                    trmData.Increment();                                 
                else               
                    info[pLevelID].Add(new TrappedPerRoom(roomID));               
            }
            else
            {
                List<TrappedPerRoom> tpr = new List<TrappedPerRoom>();
                tpr.Add(new TrappedPerRoom(roomID));
                info.Add(pLevelID, tpr);
            }
        }

        public Dictionary<int, List<TrappedPerRoom>> AgentsInfo
        {
            get { return this.info; }
        }
    }
}
