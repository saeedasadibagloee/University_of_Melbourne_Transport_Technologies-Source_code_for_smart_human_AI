using System.Collections.Generic;
using System.Linq;

namespace Domain.Room
{
    using Elements;

    //using RoutePlan = Dictionary<Elements.InnerType, Dictionary<int, EvacPlanPerRoom>>;
    using RoutePlan = Dictionary<Elements.InnerType, EvacPlan>;

    internal class GatesData
    {
        public List<int> Gates = new List<int>();
        public float DistanceViaGates = 0f;

        public GatesData(List<int> pGates, float pDistance)
        {
            Gates.AddRange(pGates);
            DistanceViaGates = pDistance;
        }
    }

    internal class EvacRoomCombination
    {
        public List<int> RoomCombination = new List<int>();
        public List<GatesData> GateCombinations = new List<GatesData>();

        public EvacRoomCombination() { }

        public void AddNewPlan(List<int> pRoomCombination, List<GatesData> pGateCombination)
        {
            RoomCombination.AddRange(pRoomCombination);
            GateCombinations.AddRange(pGateCombination);
        }
    }

    internal class EvacPlanPerRoom
    {
        public List<EvacRoomCombination> Rooms2gates = new List<EvacRoomCombination>();

        public EvacPlanPerRoom() { }
    }

    internal class EvacPlan
    {
        //map between room ID and evacuation paths that can be constructed from this room
        public Dictionary<int, EvacPlanPerRoom> evacPlanMap = 
            new Dictionary<int, EvacPlanPerRoom>();

        public bool ContainsPlanForRoom(int roomID)
        {
            return evacPlanMap.ContainsKey(roomID);
        }

        public EvacPlan() { }
    }

    internal class BasicRouteData
    {
        public RoutePlan RoutePlan = new RoutePlan();

        public BasicRouteData() { }

        public void AddNewEvacPlanEntry(InnerType gateType, EvacPlan pEntry)
        {
            RoutePlan.Add(gateType, pEntry);
        }

        //public RoutePlan GetRoutePlan { get { return this.routePlan; } }

        public bool AlternativeRoutesAreAvailable(int currentRoomID, InnerType gateType, List<Core.Threat.ILevelThreat> blockedRooms)
        {
            if (RoutePlan.ContainsKey(gateType))
            {
                if (!RoutePlan[gateType].ContainsPlanForRoom(currentRoomID))
                    return false;

                var evacPlanObject = RoutePlan[gateType].evacPlanMap;
                var evacPlanPerRoom = evacPlanObject[currentRoomID].Rooms2gates;

                if (blockedRooms != null)
                {
                    var dangerousRoutes =
                        evacPlanPerRoom.Where(
                            pRoute => pRoute.RoomCombination.Any(pRoom => blockedRooms.Exists(pR => pR.ObjectID == pRoom))).ToList();

                    return evacPlanPerRoom.Except(dangerousRoutes).ToList().Count != 0;
                }

                return evacPlanPerRoom.Count != 0;
            }

            return false;
        }
    }
}
