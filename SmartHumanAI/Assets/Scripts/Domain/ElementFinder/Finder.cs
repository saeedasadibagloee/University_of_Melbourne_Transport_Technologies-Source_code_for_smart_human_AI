using System.Collections.Generic;
using System.Linq;
using DataFormats;
using Domain.Elements;

namespace Domain.ElementFinder
{
    internal static class Finder
    {
        public static List<Vertex> FindAllCoordinates(List<RoomElement> elements, List<int> pVerticesIDs)
        {
            List<Vertex> coordinates = new List<Vertex>();

            foreach (var vertexId in pVerticesIDs)
            {
                var roomElement = 
                    elements.FirstOrDefault(p => p.VStart.id == vertexId || p.VEnd.id == vertexId);

                if (roomElement == null) continue;
                
                if (roomElement.VStart.id == vertexId)
                {
                    coordinates.Add(
                        new Vertex(roomElement.VStart.X, roomElement.VStart.Y, roomElement.VStart.id)
                    );
                }
                else if (roomElement.VEnd.id == vertexId)
                {
                    coordinates.Add(
                        new Vertex(roomElement.VEnd.X, roomElement.VEnd.Y, roomElement.VEnd.id)
                    );
                }
            }

            return coordinates;
        }

        public static RoomElement Find(List<RoomElement> pElements, int vertexStartId, int vertexEndId)
        {
            return pElements.FirstOrDefault(
                element =>
                    (element.VStart.id == vertexStartId || element.VStart.id == vertexEndId) &&
                    (element.VEnd.id == vertexStartId || element.VEnd.id == vertexEndId)
            );
        }

        public static List<int> CommonGatesIDs(List<int> pFirstRoomGateIDs, List<int> pSecondRoomGateIDs)
        {
            return pFirstRoomGateIDs.Where(pSecondRoomGateIDs.Contains).ToList();
        }

        public static List<RoomElement> CommonGates(List<RoomElement> pGates, List<int> pGateIDs)
        {
            return pGateIDs.Select(gateId => pGates.FirstOrDefault(gate => gate.ElementId == gateId)).Where(commonGate => commonGate != null).ToList();
        }

    }
}
