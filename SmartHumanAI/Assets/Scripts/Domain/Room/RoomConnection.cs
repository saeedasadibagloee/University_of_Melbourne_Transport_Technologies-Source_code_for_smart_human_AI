using System.Collections.Generic;
using Domain.Elements;

namespace Domain.Room
{
    internal class RoomConnection
    {
        private readonly List<int> _commonGatesIDs = new List<int>();
        private readonly List<RoomElement> _commonGates = new List<RoomElement>();

        private readonly int _firstRoomId  = -1;
        private readonly int _secondRoomId = -1;

        public RoomConnection(int fRoomId, int sRoomId, List<int> pCommonGatesIDs, List<RoomElement> pCommonGates)     
        {
            _firstRoomId  = fRoomId;
            _secondRoomId = sRoomId;

            _commonGatesIDs.AddRange(pCommonGatesIDs);
            _commonGates.AddRange(pCommonGates);            
        }
        
        public void SetGatesInfo(List<RoomElement> pCommonGates, List<int> pCommongGatesIDs)
        {
            _commonGates.AddRange(pCommonGates);
            _commonGatesIDs.AddRange(pCommongGatesIDs);
        }

        public bool GateIsCommon(int gateId)
        {
            return _commonGatesIDs.Contains(gateId);
        }        

        public List<int> CommonGatesIDs { get { return _commonGatesIDs; } }
        public List<RoomElement> CommonGates { get { return _commonGates; } }

        public int FirstRoomId {get { return _firstRoomId; }}
        public int SecondRoomId { get { return _secondRoomId; } }
    }
}
