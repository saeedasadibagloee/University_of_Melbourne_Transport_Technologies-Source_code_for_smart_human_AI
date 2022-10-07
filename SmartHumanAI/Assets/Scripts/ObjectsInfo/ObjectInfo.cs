using System;
using System.Collections.Generic;
using System.Linq;
using DataFormats;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Info
{
    internal class ObjectInfo
    {
        public static ObjectInfo Instance
        {
            get { return _objectInfo ?? (_objectInfo = new ObjectInfo()); }
        }

        internal int ArtifactId = 0;
        private static ObjectInfo _objectInfo;
        private int _vertexId = 0;
        private readonly Dictionary<Vertex, int> _vertices = new Dictionary<Vertex, int>();
        private readonly List<Wall> _stairWalls = new List<Wall>();

        internal void ResetIdCounter()
        {
            ArtifactId = 0;
            _vertexId = 0;
            _vertices.Clear();
            _stairWalls.Clear();
        }
        internal DistributionData ObjectToAgents(GameObject g)
        {
            DistributionData dData = new DistributionData
            {
                xPosition = g.transform.position.x,
                yPosition = g.transform.position.z,
                radius = g.transform.localScale.x
            };

            AgentDistInfo agentDistInfo = g.GetComponent<AgentDistInfo>();

            dData.id = agentDistInfo.ID;
            dData.population = agentDistInfo.NumberOfAgents;
            dData.placement = (int)agentDistInfo.AgentPlacement;
            dData.distributionType = (int)agentDistInfo.AgentType;
            dData.dGatesData = agentDistInfo.DGatesData;
            dData.color = Color3.Convert(agentDistInfo.color);
            dData.GroupNumbers = agentDistInfo.GroupNumbers;

            if (dData.placement == (int)Def.AgentPlacement.Rectangle)
                dData.RectangleVertices = agentDistInfo.GetRectangleVertices();

            // If dynamic
            if (dData.distributionType == 1)
            {
                  }
            }

            return dData;
        }
        internal CircularObstacle ObjectToPoleObstacle(GameObject g)
        {
            CircularObstacle obstacle = new CircularObstacle
            {
                XPosition = g.transform.position.x,
                YPosition = g.transform.position.z,
                Radius = g.transform.localScale.x / 2f
            };

            DangerArea avC = g.GetComponent<DangerArea>();
            if (avC != null)
                obstacle.Weight = avC.WeightModifier;

            return obstacle;
        }
        internal Gate ObjectToGate(GameObject g)
        {
            Gate gate = new Gate();
            gate.counter = g.GetComponent<GateInfo>().IsCounter;

            Vector2 vertex1 = new Vector2(
                Mathf.RoundToInt(g.transform.GetChild(0).position.x / Consts.GridResolution) * Consts.GridResolution,
                Mathf.RoundToInt(g.transform.GetChild(0).position.z / Consts.GridResolution) * Consts.GridResolution);
            Vector2 vertex2 = new Vector2(
                Mathf.RoundToInt(g.transform.GetChild(1).position.x / Consts.GridResolution) * Consts.GridResolution,
                Mathf.RoundToInt(g.transform.GetChild(1).position.z / Consts.GridResolution) * Consts.GridResolution);

            Vertex v1 = new Vertex(vertex1.x, vertex1.y);
            Vertex v2 = new Vertex(vertex2.x, vertex2.y);

            gate.id = ArtifactId++;
            gate.angle = g.transform.localRotation.eulerAngles.y;

            if (!_vertices.ContainsKey(v1))
            {
                _vertices.Add(v1, _vertexId);
                v1.id = _vertexId;
                _vertexId++;
            }
            else
            {
                v1.id = _vertices[v1];
            }

            if (!_vertices.ContainsKey(v2))
            {
                _vertices.Add(v2, _vertexId);
                v2.id = _vertexId;
                _vertexId++;
            }
            else
            {
                v2.id = _vertices[v2];
            }

            gate.vertices.Add(v1);
            gate.vertices.Add(v2);
            gate.length = Mathf.Sqrt(Mathf.Pow(v2.X - v1.X, 2) + Mathf.Pow(v2.Y - v1.Y, 2));

            return gate;
        }
        internal Wall ObjectToWall(GameObject g)
        {
            Wall wall = new Wall();

            Vector2 vertex1 = new Vector2(
                Mathf.RoundToInt(g.transform.GetChild(0).position.x / Consts.GridResolution) * Consts.GridResolution,
                Mathf.RoundToInt(g.transform.GetChild(0).position.z / Consts.GridResolution) * Consts.GridResolution);
            Vector2 vertex2 = new Vector2(
                Mathf.RoundToInt(g.transform.GetChild(1).position.x / Consts.GridResolution) * Consts.GridResolution,
                Mathf.RoundToInt(g.transform.GetChild(1).position.z / Consts.GridResolution) * Consts.GridResolution);

            Vertex v1 = new Vertex(vertex1.x, vertex1.y);
            Vertex v2 = new Vertex(vertex2.x, vertex2.y);

            wall.id = ArtifactId++;
            wall.angle = g.transform.localRotation.eulerAngles.y;
            wall.iWlWG = g.GetComponent<WallInfo>().iWLWG;
            wall.isLow = g.GetComponent<WallInfo>().IsLow;
            wall.isTransparent = g.GetComponent<WallInfo>().IsTransparent;

            if (!_vertices.ContainsKey(v1))
            {
                _vertices.Add(v1, _vertexId);
                v1.id = _vertexId;
                _vertexId++;
            }
            else
            {
                v1.id = _vertices[v1];
            }

            if (!_vertices.ContainsKey(v2))
            {
                _vertices.Add(v2, _vertexId);
                v2.id = _vertexId;
                _vertexId++;
            }
            else
            {
                v2.id = _vertices[v2];
            }

            wall.vertices.Add(v1);
            wall.vertices.Add(v2);
            wall.length = Mathf.Sqrt(Mathf.Pow(v2.X - v1.X, 2) + Mathf.Pow(v2.Y - v1.Y, 2));

            return wall;
        }
        internal Stair ObjectToStair(GameObject g, int level)
        {
            StairInfo si = g.GetComponent<StairInfo>();

            Stair stair = new Stair
            {
                id = si.StairId,
                x = g.transform.position.x,
                y = g.transform.position.z,
                length = si.Length,
                width = si.Width,
                widthLanding = si.WidthLanding,
                rotation = (int)g.transform.eulerAngles.y,
                direction = (int)si.stairDirection,
                speed = si.speed,
                spanFloors = si.spanFloors
            };

            GateInfo upperGateInfo = si.upperGate.gameObject.GetComponent<GateInfo>();
            GateInfo lowerGateInfo = si.lowerGate.gameObject.GetComponent<GateInfo>();
            stair.upper.gate = upperGateInfo.Get;
            stair.lower.gate = lowerGateInfo.Get;
            stair.upper.level = upperGateInfo.LevelId;
            stair.lower.level = lowerGateInfo.LevelId;

            if (upperGateInfo.LevelId - lowerGateInfo.LevelId != si.spanFloors)
                Debug.Log("Stair span floors inconsistency.");

            // Transfer references of attached seperators and walls to a list of walls.
            foreach (Transform t in si.wallList)
            {
                if (t == null)
                {
                    Debug.Log("Wall references on the stair not set correctly.");
                    continue;
                }

                Wall w = t.gameObject.GetComponent<WallInfo>().Get;

                foreach (Wall otherWall in _stairWalls)
                {
                    if (AreTheSame(w, otherWall))
                    {
                        w = otherWall;
                        break;
                    }
                }
                stair.walls.Add(w);
                _stairWalls.Add(w);
            }
            foreach (Transform t in si.seperatorList)
                stair.walls.Add(t.gameObject.GetComponent<WallInfo>().Get);

            switch (si.stairType)
            {
                case Def.StairType.Straight:
                    stair.type = (int)Def.StairType.Straight;
                    break;
                case Def.StairType.HalfLanding:
                    stair.type = (int)Def.StairType.HalfLanding;
                    break;
                case Def.StairType.Escalator:
                    stair.type = (int)Def.StairType.Escalator;
                    break;
                case Def.StairType.DoubleLanding:
                    break;
                case Def.StairType.Winder:
                    break;
                case Def.StairType.HalfWinder:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return stair;
        }
        internal static bool AreTheSame(Gate gate1, Gate gate2)
        {
            if (gate1.vertices.Count < 2 || gate2.vertices.Count < 2)
                Debug.LogError("Can't compare gates with less than 2 verticies each!");

            bool vStartMatch = false;
            bool vEndMatch = false;

            if (gate1.vertices[0].X == gate2.vertices[0].X &&
                gate1.vertices[0].Y == gate2.vertices[0].Y)
                vStartMatch = true;
            if (gate1.vertices[1].X == gate2.vertices[1].X &&
                gate1.vertices[1].Y == gate2.vertices[1].Y)
                vEndMatch = true;

            if (vStartMatch == vEndMatch && vStartMatch)
                return true;

            vStartMatch = vEndMatch = false;

            if (gate1.vertices[0].X == gate2.vertices[1].X &&
                gate1.vertices[0].Y == gate2.vertices[1].Y)
                vStartMatch = true;
            if (gate1.vertices[1].X == gate2.vertices[0].X &&
                gate1.vertices[1].Y == gate2.vertices[0].Y)
                vEndMatch = true;

            return vStartMatch == vEndMatch && vStartMatch;
        }
        internal static bool AreTheSame(Wall wall1, Wall wall2)
        {
            if (wall1.vertices.Count < 2 || wall2.vertices.Count < 2)
                Debug.LogError("Can't compare gates with less than 2 verticies each!");

            bool vStartMatch = false;
            bool vEndMatch = false;

            if (wall1.vertices[0].X == wall2.vertices[0].X &&
                wall1.vertices[0].Y == wall2.vertices[0].Y)
                vStartMatch = true;
            if (wall1.vertices[1].X == wall2.vertices[1].X &&
                wall1.vertices[1].Y == wall2.vertices[1].Y)
                vEndMatch = true;

            if (vStartMatch == vEndMatch && vStartMatch)
                return true;
            vStartMatch = vEndMatch = false;

            if (wall1.vertices[0].X == wall2.vertices[1].X &&
                wall1.vertices[0].Y == wall2.vertices[1].Y)
                vStartMatch = true;
            if (wall1.vertices[1].X == wall2.vertices[0].X &&
                wall1.vertices[1].Y == wall2.vertices[0].Y)
                vEndMatch = true;

            return vStartMatch == vEndMatch && vStartMatch;
        }

        public void ProcessNewWall(Wall wall)
        {
            UpdateId(wall.id);
            ProcessVertices(wall.vertices);
        }
        public void ProcessNewGate(Gate gate)
        {
            UpdateId(gate.id);
            ProcessVertices(gate.vertices);
        }

        private void UpdateId(int id)
        {
            if (ArtifactId <= id)
                ArtifactId = id + 1;
        }
        private void UpdateVId(int id)
        {
            if (_vertexId <= id)
                _vertexId = id + 1;
        }
        private void ProcessVertices(List<Vertex> vertices)
        {
            foreach (Vertex v in vertices)
            {
                // It's fine if it already contains this position, 
                // vertices should be all good provided the previous model didn't have errors.
                if (_vertices.ContainsKey(v) || v.id < 0)
                    continue;
                _vertices.Add(v, v.id);
                UpdateVId(v.id);
            }
        }

        internal GateInfo FindGate(int gateID)
        {
            foreach (var gi in Object.FindObjectsOfType<GateInfo>())
            {
                if (gi.Id == gateID)
                    return gi;
            }

            return null;
        }

        internal GateInfo[] AllGates()
        {
            return Object.FindObjectsOfType<GateInfo>();
        }

        internal int AssignOrGetVID(Vertex vertex)
        {
            if (!_vertices.ContainsKey(vertex))
            {
                _vertices.Add(vertex, _vertexId);
                _vertexId++;
            }

            return _vertices[vertex];
        }
    }
}
