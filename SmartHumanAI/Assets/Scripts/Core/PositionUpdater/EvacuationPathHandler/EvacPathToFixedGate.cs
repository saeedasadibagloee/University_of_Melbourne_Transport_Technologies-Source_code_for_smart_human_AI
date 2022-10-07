using Core.Threat;
using System.Collections.Generic;
using System.Linq;
using Domain;

namespace Core.PositionUpdater.EvacuationPathHandler
{
    using Rooms2GatesInfo = Dictionary<List<int>, List<List<int>>>;

    internal class EvacPathToFixedGate : IPath
    {
        public List<int> GetPath(Agent pAgent, IThreatHandler tHandler)
        {
            var routePlan = pAgent.CurrentLevel.basicRouteData.RoutePlan;
            
            if (routePlan.ContainsKey(pAgent.EvacData.GateType))
            {
                if (!routePlan[pAgent.EvacData.GateType].ContainsPlanForRoom(pAgent.CurrentRoomID))
                    return null;

                var destinationRoomID = pAgent.CurrentLevel.GetRoomIDByDestinationGateID(pAgent.DestinationGateId);

                if (destinationRoomID == -1)
                {
                    destinationRoomID = pAgent.CurrentLevel.GetTrainRoomIDByGateID(pAgent.DestinationGateId);

                    if (destinationRoomID == -1)
                        return null;

                    pAgent.DestinationIsTrain = true;
                }

                var evacPathObject = routePlan[pAgent.EvacData.GateType].evacPlanMap;

                //get all available paths given the roomID 
                var evacPlanPerRoomTemp = evacPathObject[pAgent.CurrentRoomID].Rooms2gates;

                //narrow down the search by removing  room combinations that 
                //do not inculde destination room as the last element
                var requiredPlans =
                    evacPlanPerRoomTemp.Where(
                        pRoute => pRoute.RoomCombination.Last() == destinationRoomID).ToList();

                var evacPlanPerRoom = EvacuationHandler.CheckForIntermediateGates(pAgent, evacPlanPerRoomTemp);

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

                //calculate and return the appropriate path based on Logit functions
                return LogitPathIdentifier.Path(
                    pAgent.CurrentLevel.LevelId, pAgent.EvacData, tHandler, requiredPlans
                );
            }

            return null;
        }
    }
}
