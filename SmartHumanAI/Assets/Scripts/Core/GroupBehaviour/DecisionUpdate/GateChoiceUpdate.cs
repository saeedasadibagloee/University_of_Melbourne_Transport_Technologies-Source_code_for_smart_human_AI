using Domain.Elements;

namespace Core.GroupBehaviour.DecisionUpdate
{   
    internal class RoomGateUpdate
    {
        private int _roomID = -1;
        private RoomElement _gate = null;
        private bool _replacePrevChoice = true;

        public RoomGateUpdate(int roomID, RoomElement gate)
        {
            _roomID = roomID;
            _gate = gate;
        }

        public RoomElement Gate { get { return _gate; } }
        public int RoomID { get { return _roomID;  } }

        public bool ReplacePrevChoice { get { return _replacePrevChoice; } }
    }

    internal class GateChoiceUpdate : GroupUpdateData
    {
        private RoomGateUpdate _roomGateUpdate = null;

        public GateChoiceUpdate(int groupID, int levelID, int roomID, RoomElement gate)
            : base(groupID, levelID)
        {
            _roomGateUpdate = new RoomGateUpdate(roomID, gate);
        }

        public override
        GroupUpdateType UpdateType()
        {
            return GroupUpdateType.GateIsSet;
        }

        public override
        object UpdateInfo()
        {
            return _roomGateUpdate;
        }
    }
}

