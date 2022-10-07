using Core.Threat;
using System.Collections.Generic;
using System.Linq;
using Domain.Elements;
using Domain.Room;

namespace Core.PositionUpdater.EvacuationPathHandler
{
    using Rooms2GatesInfo = Dictionary<List<int>, List<List<int>>>;

    internal class GeneralPath : IPath
    {
        public List<int> GetPath(Domain.Agent pAgent, IThreatHandler tHandler)
        {
            //var blockedRooms = tHandler.BlockedRooms(pAgent.CurrentLevel.LevelId);
            var routePlan = pAgent.CurrentLevel.basicRouteData.RoutePlan;

            if (routePlan.ContainsKey(pAgent.EvacData.GateType))
            {
                if (!routePlan[pAgent.EvacData.GateType].ContainsPlanForRoom(pAgent.CurrentRoomID))
                    return null;

                var evacPathObject = routePlan[pAgent.EvacData.GateType].evacPlanMap;

                //get all available paths given the roomID and number of blocked rooms 
                List<EvacRoomCombination> evacPlanPerRoomAll = evacPathObject[pAgent.CurrentRoomID].Rooms2gates;

                //narrow down the search by removing room combinations that 
                //have designated only destinations
                var evacPlanPerRoomTemp = evacPlanPerRoomAll.Where(
                        pRoute => !((Gate)pAgent.CurrentLevel.FindGate(pRoute.GateCombinations.Last().Gates.Last())).DesignatedOnly
                        ).ToList();

                var evacPlanPerRoom = EvacuationHandler.CheckForIntermediateGates(pAgent, evacPlanPerRoomTemp);

                //if (blockedRooms != null)
                if (tHandler.LevelHasBlockedObjects(pAgent.CurrentLevel.LevelId, new RoomThreatHandlingStrategy()))
                {
                    var blockedRooms = tHandler.BlockedObjects(new RoomThreatHandlingStrategy(), pAgent.CurrentLevel.LevelId);

                    //identify all dangerous paths
                    var dangerousRoutes =
                        evacPlanPerRoom.Where(
                            pRoute => pRoute.RoomCombination.Any(pRoom => blockedRooms.Exists(pR => pR.ObjectID == pRoom))).ToList();

                    //exclude all dangerous paths
                    var remainingEvacChoices = evacPlanPerRoom.Except(dangerousRoutes).ToList();

                    //calculate and return the appropriate path based on Logit functions
                    return LogitPathIdentifier.Path(
                        pAgent.CurrentLevel.LevelId, pAgent.EvacData, tHandler, remainingEvacChoices
                    );

                }

                if (!tHandler.LevelHasBlockedObjects(pAgent.CurrentLevel.LevelId)
                    && (evacPlanPerRoom.Count > 1)
                    && (pAgent.VisitedRooms.Count != 0))
                    evacPlanPerRoom = ExcludeFamiliarPaths(pAgent, evacPlanPerRoom);

                return LogitPathIdentifier.Path(
                    pAgent.CurrentLevel.LevelId, pAgent.EvacData, tHandler, evacPlanPerRoom
                );
            }

            return null;
        }

        private List<EvacRoomCombination>
            ExcludeFamiliarPaths(Domain.Agent pAgent, List<EvacRoomCombination> evacPlan)
        {
            var visitedRooms = new List<int>();
            visitedRooms.AddRange(pAgent.VisitedRooms.Keys.ToList());

            var familiarPaths =
                    evacPlan.Where(
                        pRoute => pRoute.RoomCombination.Any(pC => visitedRooms.Contains(pC))
                ).ToList();

            return evacPlan.Except(familiarPaths).ToList();
            /*
            var lastVisitedRoom = pAgent.VisitedRooms.Last().Key;
            var lastVisitedRoomIndex = pAgent.EvacData.Path.IndexOf(lastVisitedRoom);

            if (lastVisitedRoomIndex != -1)
            {
                var evacPathFromCurrentRoom = new List<int>();
                evacPathFromCurrentRoom.Add(pAgent.CurrentRoomID);
                evacPathFromCurrentRoom.AddRange(pAgent.EvacData.Path.GetRange(lastVisitedRoomIndex, pAgent.EvacData.Path.Count - lastVisitedRoomIndex));

                var evacPathFromCurrentRoomSize = evacPathFromCurrentRoom.Count;

                var familiarPaths =
                    evacPlan.Where(
                        pRoute => pRoute.RoomCombination.Count == evacPathFromCurrentRoomSize 
                        && ContainsSequence(pRoute.RoomCombination, evacPathFromCurrentRoom)
                ).ToList();

                return evacPlan.Except(familiarPaths).ToList();
            }

            return null;
            */


            /*
            //identify all dangerous paths
            var familiarPaths =
                evacPlan.Where(
                    pRoute => pRoute.RoomCombination.Any(pRoom => pAgent.VisitedRooms.ContainsKey(pRoom))
            ).ToList();
            */
        }

        private bool ContainsSequence<T>(IEnumerable<T> source,
                                         IEnumerable<T> other)
        {
            int count = other.Count();

            while (source.Any())
            {
                if (source.Take(count).SequenceEqual(other))
                    return true;
                source = source.Skip(1);
            }
            return false;
        }
    }
}
