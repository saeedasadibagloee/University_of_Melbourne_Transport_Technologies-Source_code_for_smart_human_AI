using System.Collections.Generic;

namespace Core.Threat
{
    internal enum ThreatType { Unknown, GateBlockade, RoomBlockade, StairBlockade }
    internal enum ObjectType { Unknown, Gate, Room, Stair }    

    internal class GeneralThreatData
    {
        private ThreatType _type = ThreatType.Unknown;        

        public GeneralThreatData(ThreatType pType, int pThreatId, int pObjectId)
        {
            Duration = 0;
            StartTime = 0;
            _type = pType;
            ThreatId = pThreatId;
            ObjectId = pObjectId;
            Active = false;
        }

        public int ThreatLevelID { set; get; }

        public ThreatType Type { get { return this._type; } }

        public int StartTime { set; get; }

        public int Duration { set; get; }

        public int ThreatId { set; get; }

        public int ObjectId { set; get; }

        public bool Active { set; get; }
    }

    internal interface ILevelThreat
    {
        int ObjectID { get; }
        ThreatType Type { get; }
    }

    internal class LevelThreat : ILevelThreat
    {
        private int _threatID = -1;
        private int _objectID = -1;
        private ThreatType _type = ThreatType.Unknown;

        public LevelThreat(int pThreatID, int pObjectID, ThreatType pType)
        {
            this._threatID = pThreatID;
            this._objectID = pObjectID;
            this._type = pType;
        }

        public int ObjectID { get { return this._objectID; } }
        public ThreatType Type { get { return this._type; } }
    }

    internal class LevelThreats
    {
        public List<ILevelThreat> _threats = new List<ILevelThreat>();

        public LevelThreats(GeneralThreatData newThreat)
        {
            AddNewThreatDetails(newThreat);
        }

        public void AddNewThreatDetails(GeneralThreatData newThreat)
        {
            if (!_threats.Exists(pThreat => pThreat.ObjectID == newThreat.ObjectId))
            {
                _threats.Add(
                    new LevelThreat(
                        newThreat.ThreatId, newThreat.ObjectId, newThreat.Type)
                );
            }
        }
    }
}
