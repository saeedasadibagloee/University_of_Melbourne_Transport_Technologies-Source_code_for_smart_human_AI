using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DataFormats;
using Domain;
using Domain.Elements;
using Domain.Room;
using Domain.Stairway;
using Gate = Domain.Elements.Gate;

namespace Helper
{
    internal static class MultiLevelPathfind
    {
        private static Core.Core _core;
        private static object multithreadingLock = new object();
        private static int _destinationRoomId = -1;
        private static int _destinationLevel = -1;
        private static int _destinationGateId = -1;
        private static Stack<LevelRoomGate> pathStack = new Stack<LevelRoomGate>();
        private static List<LevelRoomGate> finalPath = null;

        internal static List<LevelRoomGate> Process(Agent pAgent, Core.Core core)
        {
            lock (multithreadingLock)
            {
                int level = pAgent.LevelId;
                int currentRoomId = pAgent.CurrentRoomID;

                _core = core;
                _destinationGateId = pAgent.DestinationGateId;
                _destinationRoomId = -1;
                _destinationLevel = -1;

                foreach (var coreLevel in _core.GetLevels())
                {
                    _destinationRoomId = coreLevel.GetRoomIDByGateID(_destinationGateId);
                    if (_destinationRoomId >= 0)
                    {
                        _destinationLevel = coreLevel.LevelId;
                        break;
                    }
                }

                if (_destinationRoomId < 0 || _destinationLevel < 0)
                {
                    UnityEngine.Debug.Log("Can't destination gate " + _destinationGateId + " anywhere.");
                }

                pathStack = new Stack<LevelRoomGate>();
                finalPath = null;

                SearchRoom(level, currentRoomId);

                return finalPath;
            }
        }

        private static void SearchRoom(int level, int currentRoomId)
        {
            if (finalPath != null)
                return;

            if (level == _destinationLevel)
            {
                Dictionary<InnerType, EvacPlan> routePlan = _core.GetLevels()[level].basicRouteData.RoutePlan;
                List<EvacRoomCombination> evacPlanPerRoom = routePlan[InnerType.EvacuationGate].evacPlanMap[currentRoomId].Rooms2gates;
                var requiredPlans = evacPlanPerRoom.Where(pRoute => pRoute.RoomCombination.Last() == _destinationRoomId).ToList();

                if (requiredPlans.Count > 0)
                {
                    pathStack.Push(new LevelRoomGate(level, _destinationRoomId, _destinationGateId, InnerType.EvacuationGate));

                    // Found a path!
                    finalPath = pathStack.ToList();
                    finalPath.Reverse();

                    return;
                }
            }

            var reachableRooms = GetReachableStairRooms(level, currentRoomId);

            foreach (var levelRoom in pathStack)
                if (levelRoom.level == level && reachableRooms.Contains(levelRoom.roomId))
                    reachableRooms.Remove(levelRoom.roomId);

            foreach (var roomId in reachableRooms)
            {
                foreach (var roomElement in _core.GetLevels()[level]._Rooms[roomId].Gates)
                {
                    var gate = (Gate)roomElement;

                    if (gate.GetInnerType() == InnerType.StairwayGateUp)
                    {
                        var newLevel = level + 1;
                        var stairFromGate = _core.GetStairFromGate(gate.ElementId);
                        var nextLevelGate = stairFromGate.GetCorrespondingPort(PortType.UpperPort);
                        var nextRoomId = _core.GetLevels()[newLevel].GetRoomIDByGateID(nextLevelGate.ElementId);

                        if (!AlreadyVisited(newLevel, nextRoomId) &&
                            (stairFromGate.Direction == Def.StairDirection.Bidirectional ||
                             gate.ElementId == stairFromGate.GetPortEntryID()))
                        {
                            pathStack.Push(new LevelRoomGate(level, roomId, gate.ElementId, gate.GetInnerType()));
                            SearchRoom(newLevel, _core.GetLevels()[newLevel].GetRoomIDByGateID(nextLevelGate.ElementId));
                            pathStack.Pop();
                        }
                    }
                    else if (gate.GetInnerType() == InnerType.StairwayGateDown)
                    {
                        var newLevel = level - 1;
                        var stairFromGate = _core.GetStairFromGate(gate.ElementId);
                        var nextLevelGate = stairFromGate.GetCorrespondingPort(PortType.LowerPort);
                        var nextRoomId = _core.GetLevels()[newLevel].GetRoomIDByGateID(nextLevelGate.ElementId);

                        if (!AlreadyVisited(newLevel, nextRoomId) &&
                            (stairFromGate.Direction == Def.StairDirection.Bidirectional ||
                            gate.ElementId == stairFromGate.GetPortEntryID()))
                        {
                            pathStack.Push(new LevelRoomGate(level, roomId, gate.ElementId, gate.GetInnerType()));
                            SearchRoom(newLevel, _core.GetLevels()[newLevel].GetRoomIDByGateID(nextLevelGate.ElementId));
                            pathStack.Pop();
                        }
                    }
                }
            }
        }

        private static bool AlreadyVisited(int level, int nextRoomId)
        {
            foreach (var levelRoom in pathStack)
                if (levelRoom.level == level && levelRoom.roomId == nextRoomId)
                    return true;
            return false;
        }

        internal static List<int> GetReachableStairRooms(int level, int startRoomId)
        {
            List<int> reachableRooms = new List<int>();

            Dictionary<InnerType, EvacPlan> routePlan = _core.GetLevels()[level].basicRouteData.RoutePlan;

            List<EvacRoomCombination> evacPlanPerRoom = new List<EvacRoomCombination>();
            evacPlanPerRoom.AddRange(routePlan[InnerType.StairwayGateUp].evacPlanMap[startRoomId].Rooms2gates);
            evacPlanPerRoom.AddRange(routePlan[InnerType.StairwayGateDown].evacPlanMap[startRoomId].Rooms2gates);

            foreach (var evanCombo in evacPlanPerRoom)
            {
                int lastRoom = evanCombo.RoomCombination.Last();

                if (!reachableRooms.Contains(lastRoom))
                    reachableRooms.Add(lastRoom);
            }

            return reachableRooms;
        }
    }

    internal class LevelRoomGate
    {
        internal int level = -1;
        internal int roomId = -1;
        internal int gateId = -1;
        internal InnerType innerType = InnerType.Unknown;

        internal LevelRoomGate(int level, int roomId, int gateId, InnerType innerType)
        {
            this.level = level;
            this.roomId = roomId;
            this.gateId = gateId;
            this.innerType = innerType;
        }

        public override string ToString()
        {
            return level + ", " + roomId + ", " + gateId;
        }
    }
}
