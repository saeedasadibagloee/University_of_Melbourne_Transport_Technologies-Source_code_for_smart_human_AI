using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DataFormats;
using Domain.Elements;

namespace Domain.Stairway
{
    internal enum StarwayType { Unknown, Straight, HalfLanding, DoubleLanding, Winder, DoubleWinder, Escalator};
    internal enum PortType { Unknown, UpperPort, LowerPort }

    internal class Port
    {
        private int _levelId = -1;
        private RoomElement _data = null;
        private PortType _type;

        public Port(int pLevelId, PortType pType, RoomElement pElement)
        {
            _levelId = pLevelId;
            _data = pElement;
            _type = pType;
        }

        public PortType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public int LevelId
        {
            get { return _levelId; }
            set { _levelId = value; }
        }

        public RoomElement Data
        {
            get { return _data; }
            set { _data = value; }
        }
    }  

    internal abstract class AStairway
    {
        protected List<Vertex> coordinates = new List<Vertex>();
        protected StairBoundaries StairBoundarys = null;
        protected int StairtwayId = -1;
        public float Speed = -1;
        public int SpanFloors = 1;

        public Def.StairDirection Direction = Def.StairDirection.Bidirectional;

        protected List<Port> ports = new List<Port>();
        protected List<RoomElement> walls = new List<RoomElement>();

            }

            Port towards = null;
            Port away = null;

            foreach (var port in ports)
            {
                if (Direction == Def.StairDirection.UpOnly)
                {
                    if (port.Type == PortType.LowerPort)
                        away = port;
                    if (port.Type == PortType.UpperPort)
                        towards = port;
                } else if (Direction == Def.StairDirection.DownOnly)
                {
                    if (port.Type == PortType.LowerPort)
                        towards = port;
                    if (port.Type == PortType.UpperPort)
                        away = port;
                }
            }

            if (towards == null) return dir;

            dir.X = towards.Data.VMiddle.X - away.Data.VMiddle.X;
            dir.Y = towards.Data.VMiddle.Y - away.Data.VMiddle.Y;

            return dir;
        }

        public int StairwayID { get { return StairtwayId; } }

        public RoomElement GateByLevelId(int levelId)
        {
            var port = ports.Find(pPort => pPort.LevelId == levelId);

            return port != null ? port.Data : null;
        }

        public void SetPort(Port pPort)
        {
            ports.Add(pPort);

            AddCoordinates(
                pPort.Data.VStart.X, pPort.Data.VStart.Y,
                pPort.Data.VEnd.X, pPort.Data.VEnd.Y
            );
        }

        public bool WallsAreIdentical(List<RoomElement> pWalls)
        {
            return pWalls.All(pWall => walls.Find(wall => wall.ElementId == pWall.ElementId) != null);
        }

        public bool PortsAreIdentical(List<RoomElement> pGates)
        {
            return ports.All(port => pGates.Find(pPort => pPort.ElementId == port.Data.ElementId) != null);
        }


        public bool PortIdExists(int pId)
        {
            return ports.Find(tPort => tPort.Data.ElementId == pId) != null;
        }

        public List<RoomElement> Walls { get { return walls; } }
        public List<Port> Ports { get { return ports; } }

        public RoomElement GetCorrespondingPort(PortType type)
        {
            var port = ports.Find(pPort => pPort.Type == type);
            return port != null ? port.Data : null;
        }

        public int GetPortEntryID()
        {
            switch (Direction)
            {
                case Def.StairDirection.UpOnly:
                    return GetCorrespondingPort(PortType.LowerPort).ElementId;
                case Def.StairDirection.DownOnly:
                    return GetCorrespondingPort(PortType.UpperPort).ElementId;
            }

            return -1;
        }

        public bool Connect2Levels(int upLevelId, int downLevelId)
        {
            var upperLevelIsFound = ports[0].LevelId == upLevelId   || ports[0].LevelId == downLevelId;
            var downLevelIsFound  = ports[1].LevelId == downLevelId || ports[1].LevelId == upLevelId;

            return upperLevelIsFound && downLevelIsFound;
        }

        public List<Vertex> Coordinates { get { return coordinates; } }

        public int TopLevelId()
        {
            var port = ports.Find(pPort => pPort.Type == PortType.UpperPort);

            if (port != null)
                return port.LevelId;

            return -1;
        }

        public RoomElement TopLevelGate()
        {
            var port = ports.Find(pPort => pPort.Type == PortType.UpperPort);

            return port != null ? port.Data : null;
        }

        public int BottomLevelId()
        {
            var port = ports.Find(pPort => pPort.Type == PortType.LowerPort);

            if (port != null)
                return port.LevelId;

            return -1;
        }

        public RoomElement BottomLevelGate()
        {
            var port = ports.Find(pPort => pPort.Type == PortType.LowerPort);

            return port != null ? port.Data : null;
        }

        public bool PointIsInside(float x, float y)
        {
            if (StairBoundarys == null)
            {
                StairBoundarys = GenerateStairBoundaries();
            }

            if (!(x < StairBoundarys.XMax) || !(x > StairBoundarys.XMin)) return false;
            return y < StairBoundarys.YMax && y > StairBoundarys.YMin;
        }        

        private StairBoundaries GenerateStairBoundaries()
        {
            StairBoundaries sB = new StairBoundaries
            {
                XMin = coordinates.Min(pCoord => pCoord.X),
                YMin = coordinates.Min(pCoord => pCoord.Y),
                XMax = coordinates.Max(pCoord => pCoord.X),
                YMax = coordinates.Max(pCoord => pCoord.Y)
            };
            return sB;
        }

        private void AddCoordinates(float startX, float startY, float endX, float endY)
        {
            if (coordinates.Find(pEntry => pEntry.X == startX && pEntry.Y == startY) == null)
                coordinates.Add(new Vertex(startX, startY));

            if (coordinates.Find(pEntry => pEntry.X == endX && pEntry.Y == endY) == null)
                coordinates.Add(new Vertex(endX, endY));
        }

        internal class StairBoundaries
        {
            public float XMin, YMin = float.MaxValue;
            public float XMax, YMax = float.MinValue;
        }
    }     
}
