using System.Collections.Generic;
using System.Linq;
using DataFormats;
using Domain.Elements;
using Gate = Domain.Elements.Gate;

namespace Domain.Room
{
    internal class SimpleRoom
    {
        private readonly int _roomId = -1;
        private readonly List<RoomElement> _walls = new List<RoomElement>();
        private readonly List<RoomElement> _gates = new List<RoomElement>();
        private readonly List<RoomElement> _barricades = new List<RoomElement>();
        private readonly List<RoomElement> _poles = new List<RoomElement>();

        private readonly List<Vertex> _coordinates = new List<Vertex>();
        private readonly List<RoomElement> _destinationGates = new List<RoomElement>();

        private readonly List<RoomElement> _commonStorage = new List<RoomElement>();

        public SimpleRoom(int roomId)
        {
            _roomId = roomId;
        }

        public void AddElement(RoomElement rElement)
        {
            switch (rElement.Type())
            {
                case (int)ElementType.Wall:

                    if (!_walls.Any(pWall => pWall.ElementId == rElement.ElementId))
                        _walls.Add(rElement);

                    break;

                case (int)ElementType.Gate:

                    if (!_gates.Any(pGate => pGate.ElementId == rElement.ElementId))
                        _gates.Add(rElement);
                    
                    break;

                case (int)ElementType.Barricade:

                    if (!_barricades.Any(pBar => pBar.ElementId == rElement.ElementId))
                        _barricades.Add(rElement);
                    
                    break;
            }
        }

        public void AddPole(RoomElement pole)
        {
            _poles.Add(pole);
        }

        public void AssignCoordinates(List<Vertex> pCoordinates)
        {
            _coordinates.AddRange(pCoordinates);
        }

        public void AddDestinationGate(RoomElement destGate)
        {
            destGate.SetAsDestination();
            _destinationGates.Add(destGate);
        }

        public bool HasDestinationGates()
        {
            return _destinationGates.Count != 0;
        }

        public List<int> GateIDs()
        {
            List<int> ids = new List<int>();
            foreach (var gate in _gates)
                ids.Add(gate.ElementId);

            return ids;
        }

        public List<int> DestinationGateIds()
        {
            return (from gate in _gates where gate.IsDestination select gate.ElementId).ToList();
        }

        public List<Vertex> Coordinates { get { return _coordinates; } }

        public List<RoomElement> GetGatesByInnerType(InnerType iType)
        {
            return _gates.FindAll(pGate => pGate.GetInnerType() == iType);
        }

        public RoomElement GetGateByInnerTypeAndId(InnerType iType, int gateId)
        {
            return _gates.Find(pGate => pGate.GetInnerType() == iType && pGate.ElementId == gateId);
        }

        public bool HasSpecificGateType(InnerType iType)
        {
            return _gates.FindAll(pGate => pGate.GetInnerType() == iType).Count != 0;
        }

        public bool HasGateID(int gateID)
        {
            return (_gates.FirstOrDefault(pGate => pGate.ElementId == gateID) != null);
        }

        public int RoomId { get { return _roomId; } }

        public List<RoomElement> Gates { get { return _gates; } }
        public List<RoomElement> Barricades { get { return _barricades; } }
        public List<RoomElement> Poles { get { return _poles; } }
        public List<RoomElement> RoomWalls { get { return _walls; } }
        public List<RoomElement> DestinationGates { get { return _destinationGates; } }
        public List<RoomElement> WallsAndBarricades { get { return _commonStorage; } }

        public void CreateCommonStorage()
        {
            _commonStorage.AddRange(_walls);
            _commonStorage.AddRange(_barricades);
        }

        public bool IsTrainCarriage()
        {
            foreach (var roomElement in _gates)
            {
                var gate = roomElement as Gate;

                if (gate.TrainData == null)
                    return false;
            }

            return _gates.Count > 0;
        }
    }
}
