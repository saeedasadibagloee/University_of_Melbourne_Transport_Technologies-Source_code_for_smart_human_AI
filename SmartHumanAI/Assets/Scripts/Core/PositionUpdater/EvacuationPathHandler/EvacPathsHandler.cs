using System;
using System.Collections.Generic;
using System.Linq;
using Core.Logger;
using Core.Threat;
using Domain.Room;

namespace Core.PositionUpdater.EvacuationPathHandler
{
    using Domain;

    internal class Distance2GateIndexMap
    {
        private readonly int _index = -1;
        private readonly float _distance = 0f;
        private readonly List<int> _roomConnection = null;

        /// <summary>
        /// Class stores information about corresponding room connection 
        /// and shortest path via these rooms
        /// </summary>
        /// <param name="pIndex"> index of the gate list </param>
        /// <param name="pDistance"> float value of the evacuation path </param>
        /// <param name="pRoomConnection"> reference to the room connection object </param>
        public Distance2GateIndexMap(int pIndex, float pDistance, List<int> pRoomConnection)
        {
            _index = pIndex;
            _distance = pDistance;
            _roomConnection = pRoomConnection;
        }

        public int Index { get { return _index; } }
        public float Distance { get { return _distance; } }
        public List<int> RoomConnection { get { return _roomConnection; } }
    }

    internal class InitialGatesHandler
    {
        public static void CompletePhase(Agent pAgent, uint areaId, IThreatHandler tHandler)
        {
            pAgent.EvacData.Clear();

            if (!pAgent.CurrentLevel._Rooms.ContainsKey(pAgent.CurrentRoomID))
            {
                LogWriter.Instance.WriteToLog("Shortest path: can't figure room from which to build the evacuation path");
                pAgent.Active = false;
            }

            var roomGates = pAgent.CurrentLevel._Rooms[pAgent.CurrentRoomID].Gates;

            foreach (var gate in roomGates)
            {
                if (!tHandler.IsObjectBlocked(new GateThreatHandlingStrategy(), gate.ElementId, pAgent.CurrentLevel.LevelId))
                    pAgent.EvacData.TestGates.Add(gate);
            }

            if (pAgent.EvacData.TestGates.Count == 0)
            {
                LogWriter.Instance.WriteToLog("Shortest path: can't figure out first gates to choose");
                pAgent.Active = false;
            }
            else
            {
                //get the last gate and set it's id
                var lastGate = pAgent.EvacData.TestGates.Last();
                pAgent.EvacData.LastGateId = lastGate.ElementId;

                pAgent.CheckGatePath(areaId, lastGate);

                pAgent.EvacData.TestGates.Remove(lastGate);
                pAgent.EvacData.LookingForGates = false;
            }
        }
    }

    internal class TestGatesHandler
    {
        public static void CompletePhase(Agent pAgent, uint areaId)
        {
            var lastGate = pAgent.EvacData.TestGates.Last();
            pAgent.EvacData.LastGateId = lastGate.ElementId;

            pAgent.CheckGatePath(areaId, lastGate);
            pAgent.EvacData.TestGates.Remove(lastGate);
        }
    }

    internal class EvacuationHandler
    {
        public static bool GetPath<T>(Agent pAgent, uint areaId, IThreatHandler tHandler) where T : IPath
        {
            if (pAgent.EvacData.LookingForGates)
            {
                InitialGatesHandler.CompletePhase(pAgent, areaId, tHandler);
            }
            else if (pAgent.PathIsUpdated())
            {
                if (pAgent.EvacData.TestGates.Count != 0)
                {
                    TestGatesHandler.CompletePhase(pAgent, areaId);
                }
                else //all initial distances are calculated ===> get the evacuation path based on the Logit function
                {
                    T pathObject = Activator.CreateInstance<T>();

                    var evacuationPath = pathObject.GetPath(pAgent, tHandler);

                    if (evacuationPath != null)
                    {
                        if (pAgent.VisitedRooms.Count != 0)
                            pAgent.VisitedRooms.Clear();

                        pAgent.EvacData.UpdatePath(evacuationPath);
                    }
                    else
                    {
                        pAgent.HasToWait = true;
                    }

                    pAgent.EvacData.UpdatingPaths = false;

                    return true;
                }
            }

            return false;
        }

        public static List<EvacRoomCombination> CheckForIntermediateGates(Agent pAgent, List<EvacRoomCombination> evacPlanPerRoomTemp)
        {
            var evacPlanPerRoom = new List<EvacRoomCombination>();

            if (pAgent.IntermediateGates != null && pAgent.IntermediateGates.Count > 0)
            {
                int counter = 0;

                while (evacPlanPerRoom.Count < 1)
                {
                    foreach (var evacRoomCombination in evacPlanPerRoomTemp)
                    {
                        bool gatesListHasAll = false;

                        foreach (var gatesList in evacRoomCombination.GateCombinations)
                        {
                            gatesListHasAll = true;

                            foreach (var intermediateGateId in pAgent.IntermediateGates)
                            {
                                if (!gatesList.Gates.Contains(intermediateGateId))
                                    gatesListHasAll = false;
                            }

                            if (!gatesListHasAll) continue;

                            evacPlanPerRoom.Add(evacRoomCombination);
                            break;
                        }

                        if (gatesListHasAll) break;
                    }

                    pAgent.GenerateIntermediateGates();
                    if (counter++ > 10000) break;
                }
            }
            else
            {
                return evacPlanPerRoomTemp;
            }

            return evacPlanPerRoom;
        }
    }
}
