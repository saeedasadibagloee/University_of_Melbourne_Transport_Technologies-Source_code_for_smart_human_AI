using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.DataFormats;
using Assets.Scripts.PathfindingExtensions;
using Core;
using Core.Logger;
using DataFormats;
using Domain.Distribution;
using Domain.ElementFinder;
using Domain.Elements;
using Domain.Room;
using Pathfinding;
using UnityEngine;
using Gate = Domain.Elements.Gate;

namespace Domain.Level
{
    internal class SimpleLevel
    {
        private int _defaultRegionSize = 2;
        private int _defaultNeighbors = 50;
        private int _nRooms = -1;

        private Dictionary<int, uint> _roomGridAreaMap;
        public Dictionary<GatePair, float> GateDistancesDict = new Dictionary<GatePair, float>(new GatePairComparer());

        public List<List<int>> Regions = new List<List<int>>();
        public List<Train> Trains = new List<Train>();
        public List<RoomElement> GeneralElements = new List<RoomElement>();
        public List<RoomElement> destinationGates = new List<RoomElement>();

        public List<SimpleRoom> TempRooms = new List<SimpleRoom>();
        public Dictionary<int, SimpleRoom> _Rooms = new Dictionary<int, SimpleRoom>();

        public List<RoomConnection> ConnectedRooms = new List<RoomConnection>();

        public BasicRouteData basicRouteData = new BasicRouteData();
        public List<IDistribution> Distributions = new List<IDistribution>();

        public List<WaitPoint> WaitPoints = new List<WaitPoint>();
        public Dictionary<int, List<WaitPoint>> roomIdToWaitPoints = new Dictionary<int, List<WaitPoint>>();
        public Dictionary<int, List<StochasticDetourData>> roomIdToSDD = new Dictionary<int, List<StochasticDetourData>>();

        public int LevelId = -1;
        public int[,,] Grid;
        private int numPathsDone = 0;

        public SimpleLevel(int pLevelId = -1)
        {
            LevelId = pLevelId;
        }

        public void SetLevelSize(int pWidth, int pHeight)
        {
            Grid = new int[pWidth, pHeight, _defaultNeighbors];
        }

        public uint GetGridAreaId(int roomId)
        {
            return _roomGridAreaMap.ContainsKey(roomId) ? _roomGridAreaMap[roomId] : 1;
        }

        public void SetRoomGridAreaMap(Dictionary<int, uint> pRoomGridAreaMap)
        {
            _roomGridAreaMap = pRoomGridAreaMap;
        }

        public List<RoomElement> GetWallsAndBarricades(int roomID)
        {
            return _Rooms.ContainsKey(roomID) ? _Rooms[roomID].WallsAndBarricades : null;
        }

        public List<RoomElement> GetBarricades(int roomID)
        {
            return _Rooms.ContainsKey(roomID) ? _Rooms[roomID].Barricades : null;
        }

        public List<RoomElement> DestinationGates
        {
            get { return destinationGates; }
        }

        public List<RoomElement> GetPoles(int roomID)
        {
            return _Rooms.ContainsKey(roomID) ? _Rooms[roomID].Poles : null;
        }

        public bool HasDestinationGates()
        {
            foreach (var roomElement in destinationGates)
            {
                var gate = roomElement as Gate;
                if (gate != null && !gate.DesignatedOnly)
                    return true;
            }

            return false;
        }

        public bool HasDestinationOrTrainGates()
        {
            foreach (var roomElement in destinationGates)
            {
                var gate = roomElement as Gate;
                if (gate != null)
                    return true;
            }

            return false;
        }

        public List<RoomElement> GetGatesByRoomID(int roomID)
        {
            return _Rooms.ContainsKey(roomID) ? _Rooms[roomID].Gates : null;
        }

        public RoomElement FindGate(int gateID)
        {
            return GeneralElements.FirstOrDefault(element => element.ElementId == gateID);
        }

        public Train FindTrain(int destinationGateID)
        {
            return Trains.FirstOrDefault(train => train.destinationGateID == destinationGateID);
        }

        public List<RoomElement> GetAllGates()
        {
            return GeneralElements.Where(element => element.Type() == (int)ElementType.Gate).ToList();
        }

        public List<RoomElement> GetDestinationGates(int roomID)
        {
            return _Rooms.ContainsKey(roomID) ? _Rooms[roomID].DestinationGates : null;
        }

        public List<RoomElement> GetSpecificRoomGatesByRoomID(int roomID, InnerType type)
        {
            return _Rooms.ContainsKey(roomID) ? _Rooms[roomID].GetGatesByInnerType(type) : null;
        }

        public int GetRoomIDByDestinationGateID(int destinationGateID)
        {
            foreach (var room in _Rooms.Values)
            {
                if (!room.HasDestinationGates())
                    continue;

                if (room.HasGateID(destinationGateID))
                    return room.RoomId;
            }

            return -1;
        }

        public int GetRoomIDByGateID(int gateID)
        {
            foreach (var room in _Rooms.Values)
            {
                if (room.HasGateID(gateID))
                    return room.RoomId;
            }

            return -1;
        }

        public int GetTrainRoomIDByGateID(int destinationGateID)
        {
            foreach (var room in _Rooms.Values)
            {
                if (!room.IsTrainCarriage())
                    continue;

                if (room.HasGateID(destinationGateID))
                    return room.RoomId;
            }

            return -1;
        }

        public void CreateLevelRooms()
        {
            var roomId = 0;

            foreach (var region in Regions)
            {
                var regionSize = region.Count;
                //we must have at least 3 vertices to create a room
                if (regionSize < _defaultRegionSize)
                    continue;

                var intialVertixId = region[0];
                var vertexStartId = intialVertixId;

                SimpleRoom newRoom = new SimpleRoom(roomId++);
                newRoom.AssignCoordinates(Finder.FindAllCoordinates(GeneralElements, region));

                for (var index = 1; index < regionSize; ++index)
                {
                    var vertexEndId = region[index];
                    AddRoomElement(newRoom, vertexStartId, vertexEndId);

                    if (index == regionSize - 1) //last vertex
                    {
                        if (region[index] != intialVertixId)
                            AddRoomElement(newRoom, vertexEndId, intialVertixId);
                    }

                    vertexStartId = vertexEndId;
                }

                TempRooms.Add(newRoom);
            }

            this._nRooms = TempRooms.Count;
        }

        public void CreateLevelInfo()
        {
            CreateConnectionMap();
            CreateDestinationGates();

            CreateBarricades();
            CreatePoles();
            CreateCommonStorage();
            LogRoomDetails();
        }

        public void SaveDistribution(DistributionData dData, List<int> groupData)
        {
            Distributions.Add(DistributionFactory.GenerateDistribution(this, dData, groupData));
        }

        public int GetRoomIndex(float x, float y)
        {
            var roomIndex = -1;

            if (_nRooms == 0)
            {
                LogWriter.Instance.WriteToLog("There are no rooms, so I could not find the room index.");
                return -1;
            }

            for (var tRoomIndex = 0; tRoomIndex < _nRooms; ++tRoomIndex)
            {
                if (!Utils.PointIsInside(x, y, TempRooms[tRoomIndex].Coordinates)) continue;
                roomIndex = tRoomIndex;
                break;
            }

            return roomIndex;
        }

        public int GetRoomID(float x, float y)
        {
            var roomID = -1;

            if (_nRooms == 0)
            {
                LogWriter.Instance.WriteToLog("There are no rooms, so I could not find the room index.");
                return -1;
            }

            foreach (var room in _Rooms.Values)
            {
                if (Utils.PointIsInside(x, y, room.Coordinates))
                    return room.RoomId;
            }

            return roomID;
        }

        public void CreateRoomRouteInfo(InnerType gateType = InnerType.EvacuationGate)
        {
            var evacPlanPerLevel = new Dictionary<int, EvacPlanPerRoom>();
            var evacPlan = new EvacPlan();

            foreach (var room in TempRooms)
            {
                var pathsViaRooms = new List<List<int>>();
                var evacPlanPerRoom = new EvacPlanPerRoom();

                if (room.HasSpecificGateType(gateType))
                {
                    List<int> singlePath = new List<int> { room.RoomId };
                    pathsViaRooms.Add(singlePath);
                }

                var neighborRoomIDs = GetNeighborRooms(room.RoomId);

                foreach (var id in neighborRoomIDs)
                {
                    List<int> path = new List<int> { room.RoomId, id };
                    CreateDestinationPaths(path, pathsViaRooms, id, gateType);
                }

                foreach (var path in pathsViaRooms)
                {
                    var newRoomCombinationData = new EvacRoomCombination();

                    List<List<int>> pathsViaGates = new List<List<int>>();

                    if (path.Count == 1)
                    {
                        //var gates = this.GetDestinationGatesByRoomID(path[0]);
                        var gates = GetSpecificGatesByRoomId(path[0], gateType);

                        if (gates != null)
                        {
                            foreach (var gate in gates)
                            {
                                List<int> singleGatePath = new List<int> { gate.ElementId };
                                pathsViaGates.Add(singleGatePath);
                            }
                        }
                    }
                    else
                    {
                        var index = 1;
                        CreateDestinationPaths(index, path, pathsViaGates, gateType);
                    }

                    List<GatesData> gatesData = new List<GatesData>();

                    foreach (var gatesPath in pathsViaGates)
                    {
                        var distance = 0f;
                        var gatesPathSize = gatesPath.Count;

                        for (var i = 1; i != gatesPathSize; ++i)
                        {
                            var key = new GatePair(gatesPath[i - 1], gatesPath[i]);

                            if (GateDistancesDict.ContainsKey(key))
                                distance += GateDistancesDict[key];
                        }

                        var newGatesData = new GatesData(gatesPath, distance);
                        gatesData.Add(newGatesData);
                    }

                    //add new data into EvacRoomCombination object
                    newRoomCombinationData.AddNewPlan(path, gatesData);
                    evacPlanPerRoom.Rooms2gates.Add(newRoomCombinationData);
                }

                evacPlan.evacPlanMap.Add(room.RoomId, evacPlanPerRoom);
            }

            basicRouteData.AddNewEvacPlanEntry(gateType, evacPlan);
        }

        public void CreateRealRooms()
        {
            foreach (var room in TempRooms)
                _Rooms.Add(room.RoomId, room);

            TempRooms.Clear();
        }

        public List<int> GetCommonGateIDs(int roomIdL, int roomIdR)
        {
            var cRooms = ConnectedRooms.Find(
                rooms => (rooms.FirstRoomId == roomIdL || rooms.SecondRoomId == roomIdL) &&
                            (rooms.FirstRoomId == roomIdR || rooms.SecondRoomId == roomIdR)
            );
            return cRooms != null ? cRooms.CommonGatesIDs : null;
        }

        public List<RoomElement> GetCommonGates(int roomIdL, int roomIdR)
        {
            var cRoom = ConnectedRooms.Find(
                rooms => rooms.FirstRoomId == roomIdL && rooms.SecondRoomId == roomIdR ||
                         rooms.FirstRoomId == roomIdR && rooms.SecondRoomId == roomIdL
            );
            return cRoom != null ? cRoom.CommonGates : null;
        }

        public List<RoomElement> GetDecisionUpdateGates(Agent agent)//int roomID, EvacuationPath evacData)
        {
            List<RoomElement> roomGates = GetEvacGates(agent);

            var roomGatesFinal = new List<RoomElement>();

            List<int> gatesToRoom = new List<int>();

            foreach (var visitedRoom in agent.VisitedRooms)
            {
                var commonGateIDs = agent.CurrentLevel.GetCommonGateIDs(agent.CurrentRoomID, visitedRoom.Key);

                if (commonGateIDs != null)
                    gatesToRoom.AddRange(commonGateIDs);
            }


            foreach (var roomGate in roomGates)
            {
                if (gatesToRoom.Contains(roomGate.ElementId))
                    continue;

                roomGatesFinal.Add(roomGate);
            }

            return roomGatesFinal;
        }

        public List<RoomElement> GetEvacGates(Agent agent)
        {
            var roomGates = new List<RoomElement>();
            var gatesByRoomId = GetGatesByRoomID(agent.CurrentRoomID);
            if (gatesByRoomId == null)
                return roomGates;
            roomGates.AddRange(gatesByRoomId);

            if (basicRouteData.RoutePlan.ContainsKey(agent.EvacData.GateType))
            {
                if (basicRouteData.RoutePlan[agent.EvacData.GateType].ContainsPlanForRoom(agent.CurrentRoomID))
                {
                    var evacPathObject = basicRouteData.RoutePlan[agent.EvacData.GateType].evacPlanMap;

                    //Get all available paths given the roomID and number of blocked rooms 
                    var evacPlanPerRoom = evacPathObject[agent.CurrentRoomID].Rooms2gates;

                    roomGates.RemoveAll(
                        pGate => !GateLeadsToEvacuation(pGate, evacPlanPerRoom)
                    );
                }
            }

            return roomGates;
        }

        private bool GateLeadsToEvacuation(RoomElement gate, List<EvacRoomCombination> roomCombinations)
        {
            return roomCombinations.Any(
                rCombination => rCombination.GateCombinations.Any(
                    rGateCombition => rGateCombition.Gates.Contains(gate.ElementId)));
        }

        private void CreateDestinationPaths(int currentIndex,
                                            List<int> roomPath,
                                            List<List<int>> pathsViaGates,
                                            InnerType type = InnerType.EvacuationGate,
                                            List<int> gatesPath = null)
        {
            if (currentIndex < roomPath.Count)
            {
                var gates = GetCommonGateIDs(roomPath[currentIndex - 1], roomPath[currentIndex]);

                if (gates == null) return;
                foreach (var gateId in gates)
                {
                    List<int> intialPath = new List<int>();
                    if (gatesPath != null)
                        intialPath.AddRange(gatesPath);

                    intialPath.Add(gateId);
                    CreateDestinationPaths(currentIndex + 1, roomPath, pathsViaGates, type, intialPath);
                }
            }
            else
            {
                //var destinationGates = this.GetDestinationGatesByRoomID(roomPath[currentIndex - 1]);//last room
                var destinationGates = GetSpecificGatesByRoomId(roomPath[currentIndex - 1], type);

                foreach (var gate in destinationGates)
                {
                    List<int> path = new List<int>();
                    path.AddRange(gatesPath); path.Add(gate.ElementId);
                    pathsViaGates.Add(path);
                }
            }
        }

        private List<RoomElement> GetSpecificGatesByRoomId(int roomId, InnerType type)
        {
            var roomWithId = TempRooms.Find(room => room.RoomId == roomId);

            return roomWithId != null ? roomWithId.GetGatesByInnerType(type) : null;
        }

        private List<int> GetNeighborRooms(int roomId)
        {
            List<int> neightborRooms = new List<int>();

            foreach (var room in ConnectedRooms)
            {
                if (room.FirstRoomId == roomId)
                    neightborRooms.Add(room.SecondRoomId);
                else if (room.SecondRoomId == roomId)
                    neightborRooms.Add(room.FirstRoomId);
            }

            return neightborRooms;
        }

        private void CreateDestinationPaths(List<int> path,
                                            List<List<int>> allDestinationPaths,
                                            int neightborRoomId,
                                            InnerType type = InnerType.EvacuationGate)
        {
            var index = -1;
            for (var i = 0; i < _nRooms; ++i)
            {
                if (TempRooms[i].RoomId != neightborRoomId) continue;
                index = i; break;
            }

            //if (rooms[index].HasDestinationGates())
            if (TempRooms[index].HasSpecificGateType(type))
            {
                allDestinationPaths.Add(path);
                //return;
            }

            var neighborRooms = GetNeighborRooms(neightborRoomId);

            if (neighborRooms.Count != 0)
            {
                foreach (var id in neighborRooms)
                {
                    if (path.Contains(id)) continue;
                    List<int> newPath = new List<int>();
                    newPath.AddRange(path);
                    newPath.Add(id);
                    CreateDestinationPaths(newPath, allDestinationPaths, id, type);
                }
            }
            else
                return;
        }

        private void CreateConnectionMap()
        {
            var nRooms = TempRooms.Count;

            if (nRooms == 1)
                return;

            for (var index = 0; index < nRooms; ++index)
            {
                for (var j = 0; j < nRooms; ++j)
                {
                    var currentRoomId = TempRooms[index].RoomId;
                    var neightborRoomId = TempRooms[j].RoomId;

                    if (currentRoomId == neightborRoomId)
                        continue;

                    var connectedRoomsObject =
                        ConnectedRooms.FirstOrDefault(
                         element =>
                            (element.FirstRoomId == currentRoomId || element.SecondRoomId == currentRoomId) &&
                            (element.FirstRoomId == neightborRoomId || element.SecondRoomId == neightborRoomId)
                    );

                    if (connectedRoomsObject != null)
                        continue;

                    var roomGates = TempRooms[index].GateIDs();
                    //find common gates ids
                    var commonGatesIds = Finder.CommonGatesIDs(roomGates, TempRooms[j].GateIDs());
                    //create list of gates objects based on their gate ids
                    var commonGatesObjects = Finder.CommonGates(GeneralElements, commonGatesIds);

                    if (commonGatesObjects.Count == 0)
                        continue;

                    foreach (var cgate in commonGatesObjects)
                        cgate.SetInnerType(InnerType.ConnectingGate);

                    RoomConnection newRoomConnection =
                        new RoomConnection(currentRoomId,
                            neightborRoomId, commonGatesIds, commonGatesObjects
                    );
                    ConnectedRooms.Add(newRoomConnection);
                }
            }

            //LogConnectionMap();
        }

        private void LogConnectionMap()
        {
            LogWriter.Instance.WriteToLog("+++++++ Connection Map +++++++");
            foreach (var cRoom in ConnectedRooms)
            {
                LogWriter.Instance.WriteToLog("");
                LogWriter.Instance.WriteToLog("L Room ID: " + cRoom.FirstRoomId);
                LogWriter.Instance.WriteToLog("R Room ID: " + cRoom.SecondRoomId);
                LogWriter.Instance.WriteToLog("----------------------------------");

                foreach (var id in cRoom.CommonGatesIDs)
                    LogWriter.Instance.WriteToLog("Common gate ID: " + id);
            }
        }

        private void CreatePoles()
        {
            List<RoomElement> poles = GeneralElements.FindAll(
                element => element.Type() == (int)ElementType.CircularObstacle).ToList();

            foreach (var pole in poles)
            {
                var indexVs = GetRoomIndex(pole.VMiddle.X, pole.VMiddle.Y);

                if (indexVs != -1)
                {
                    TempRooms[indexVs].AddPole(pole);
                }
                else
                {
                    LogWriter.Instance.WriteToLog(
                        "Error locating Pole with [ +" +
                        pole.VMiddle.X + "," + pole.VMiddle.Y + "]");
                }
            }
        }

        private void CreateBarricades()
        {
            List<RoomElement> barricades = GeneralElements.FindAll(
                element => element.Type() == (int)ElementType.Barricade).ToList();

            foreach (var barricade in barricades)
            {
                var indexVstart = GetRoomIndex(barricade.VStart.X, barricade.VStart.Y);
                var indexVend = GetRoomIndex(barricade.VEnd.X, barricade.VEnd.Y);

                if (indexVstart == indexVend)
                {
                    AddElementToRoom(barricade, indexVstart);
                }
                else
                {
                    AddElementToRoom(barricade, indexVstart);
                    AddElementToRoom(barricade, indexVend);
                }
            }
        }

        private void AddElementToRoom(RoomElement barricade, int roomIndex)
        {
            if (roomIndex != -1)
                TempRooms[roomIndex].AddElement(barricade);
        }

        private void CreateCommonStorage()
        {
            foreach (var room in TempRooms)
                room.CreateCommonStorage();
        }

        private void LogRoomDetails()
        {
            foreach (var room in TempRooms)
            {
                LogWriter.Instance.WriteToLog("Room ID: " + room.RoomId);

                string gateIdStr = room.Gates.Aggregate("", (current, gate) => current + ("[" + gate.ElementId) + (gate.IsDestination ? "(destination)" : "") + "]");

                LogWriter.Instance.WriteToLog("Gate IDs: " + gateIdStr);
                LogWriter.Instance.WriteToLog("Number of barricades: " + room.Barricades.Count);
            }
        }

        private void CreateDestinationGates()
        {
            foreach (var room in TempRooms)
            {
                //get room gates
                var gates = room.Gates;

                foreach (var gate in gates)
                {
                    var gateIsCommon = GateIsCommon(gate);
                    var innerType = gate.GetInnerType();

                    if (gateIsCommon || innerType == InnerType.StairwayGateDown || innerType == InnerType.StairwayGateUp)
                        continue;

                    room.AddDestinationGate(gate);
                    destinationGates.Add(gate);
                }
            }
        }

        private bool GateIsCommon(RoomElement gate)
        {
            return ConnectedRooms.Any(connectedPair => connectedPair.GateIsCommon(gate.ElementId));
        }

        private void AddRoomElement(SimpleRoom room, int vStartId, int vEndId)
        {
            var roomElement = Finder.Find(GeneralElements, vStartId, vEndId);

            if (roomElement != null)
                room.AddElement(roomElement);
        }

        public void FillGateDistancesTable()
        {
            Dictionary<GatePair, Path> pathsDict = new Dictionary<GatePair, Path>();
            numPathsDone = 0;
            int pathsRequested = 0;
            float levelHeight = Utils.FindLevelHeight(LevelId);

            foreach (var room in TempRooms)
            {
                foreach (var gate1 in room.Gates)
                {
                    foreach (var gate2 in room.Gates)
                    {
                        if (gate1.ElementId == gate2.ElementId)
                            continue;

                        Path p = ABPath.Construct(
                            new Vector3(gate1.VMiddle.X, levelHeight, gate1.VMiddle.Y),
                            new Vector3(gate2.VMiddle.X, levelHeight, gate2.VMiddle.Y),
                            OnPathComplete);

                        pathsRequested++;

                        p.nnConstraint = new NNConstraint();
                        p.nnConstraint.constrainArea = true;
                        p.nnConstraint.area = (int)GetGridAreaId(room.RoomId);

                        GatePair pair = new GatePair(gate1.ElementId, gate2.ElementId);

                        p.GatePair1 = pair.Gate1;
                        p.GatePair2 = pair.Gate2;

                        pathsDict[pair] = p;

                        AstarPath.StartPath(pathsDict[pair]);
                    }
                }
            }

            // Wait until all paths are calcuated and recorded (see OnPathComplete delegate)
            while (pathsRequested > numPathsDone)
            {
                foreach (var kvp in pathsDict)
                {
                    AstarPath.BlockUntilCalculated(kvp.Value);
                }
            }
        }

        public void PrintGateDistanceTable()
        {
            LogWriter.Instance.WriteToLog("Real Distances Between Gates (LEVEL " + LevelId + "):");

            foreach (var kvp in GateDistancesDict)
                LogWriter.Instance.WriteToLog(kvp.Key + " " + kvp.Value);
        }

        public class GatePair
        {
            public GatePair(int gate1, int gate2)
            {
                Gate1 = Math.Min(gate1, gate2);
                Gate2 = Math.Max(gate1, gate2);
            }

            public int Gate1 = -1;
            public int Gate2 = -1;

            public override string ToString()
            {
                return "(" + Gate1 + " <-> " + Gate2 + ")";
            }
        }

        private class GatePairComparer : IEqualityComparer<GatePair>
        {
            public bool Equals(GatePair gp1, GatePair gp2)
            {
                return (gp1.Gate1 == gp2.Gate1) & (gp1.Gate2 == gp2.Gate2);
            }

            public int GetHashCode(GatePair obj)
            {
                string combined = obj.Gate1 + "|" + obj.Gate2;
                return combined.GetHashCode();
            }
        }

        public void OnPathComplete(Path p)
        {
            numPathsDone++;
            FunnelModifierSingleton.Instance.Apply(p);
            var gatePair = new GatePair(p.GatePair1, p.GatePair2);
            GateDistancesDict[gatePair] = p.GetTotalLength();
        }

        public void RemoveRoomAt(int i)
        {
            TempRooms.RemoveAt(i);
            _nRooms = TempRooms.Count;
        }

        public void AssignWaitPointsToRooms()
        {
            foreach (var waitPoint in WaitPoints)
            {
                int roomId = GetRoomID(waitPoint.x, waitPoint.y);
                if (!roomIdToWaitPoints.ContainsKey(roomId))
                    roomIdToWaitPoints.Add(roomId, new List<WaitPoint>());
                roomIdToWaitPoints[roomId].Add(waitPoint);
            }
        }
    }
}
