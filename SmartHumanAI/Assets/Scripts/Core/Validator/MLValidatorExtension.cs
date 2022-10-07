using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Core.Handlers;
using Core.Logger;
using DataFormats;
using Domain.Elements;
using Domain.Room;
using Helper;

namespace Core.Validator
{
    internal class MlvExtension
    {
        public static void Execute(ILevelHandler pLevelHandler, IStairwayHandler pStairsHandler)
        {
            //get all levels
            var levels = pLevelHandler.Levels();
            //get all stairs
            var stairs = pStairsHandler.Stairs();

            var upperGatesToBeSaved = new List<RoomElement>();

            for (int j = 0; j < levels.Count; j++)
            {
                var level = levels[j];
                var rooms = level.TempRooms;

                // Set up room polygons
                Dictionary<PointF[], SimpleRoom> roomPolygons = new Dictionary<PointF[], SimpleRoom>();
                foreach (SimpleRoom room in rooms)
                {
                    var points = new PointF[room.Coordinates.Count];

                    for (int k = 0; k < room.Coordinates.Count; k++)
                    {
                        float x = room.Coordinates[k].X;
                        float y = room.Coordinates[k].Y;

                        points[k] = new PointF(x, y);
                    }

                    roomPolygons.Add(points, room);
                }

                //Process uppergates from the level below.
                foreach (RoomElement gateGoingDown in upperGatesToBeSaved)
                {
                    if (gateGoingDown == null)
                    {
                        UnityEngine.Debug.LogError("gateGoingDown is null");
                        continue;
                    }

                    RoomElement foundGateToGoDown = FindSameGateVertexes(gateGoingDown, rooms);

                    if (foundGateToGoDown != null)
                    {
                        // We need to set the gate already in the room to a stairway gate.
                        /*LogWriter.Instance.WriteToLog("Gate from previous level with vertex ids "
                                    + foundGateToGoDown.vStart.vertexID + " "
                                    + foundGateToGoDown.vEnd.vertexID + " exists in this room.");*/
                        UnityEngine.Debug.Log("Set Inner for Upper in current room.");
                        foundGateToGoDown.SetInnerType(gateGoingDown.GetInnerType());
                    }

                    foreach (var kvp in roomPolygons)
                    {
                        PolygonHelper poly = new PolygonHelper(kvp.Key);

                        bool vStartInPoly = false;
                        bool vEndInPoly = false;

                        vStartInPoly = poly.PointInPolygon(gateGoingDown.VStart.X, gateGoingDown.VStart.Y);
                        vEndInPoly = poly.PointInPolygon(gateGoingDown.VStart.X, gateGoingDown.VStart.Y);

                        if (vStartInPoly != vEndInPoly)
                            UnityEngine.Debug.LogError("One vertex is in the room, and the other isn't? Please check.");

                        if (vStartInPoly && vEndInPoly)
                        {
                            // We've found the room! Make a barrier in this room.
                            /*LogWriter.Instance.WriteToLog("Gate from previous level with vertex ids "
                                + gateGoingDown.vStart.vertexID + " "
                                + gateGoingDown.vEnd.vertexID + " placed as a port in room. "
                                + kvp.Value.RoomID);*/

                            SaveGateToStairPortInRoom(kvp.Value.RoomId, gateGoingDown, rooms);
                        }
                    }
                }

                //Clear gates from previous room.
                upperGatesToBeSaved.Clear();

                for (int i = rooms.Count - 1; i >= 0; i--)
                {
                    bool roomIsFound = false;
                    int spanFloors = 1;
                    var roomWalls = rooms[i].RoomWalls;
                    var roomGates = rooms[i].Gates;

                    RoomElement lowerGateToBeSaved = null;
                    RoomElement upperGateToBeSaved = null;

                    //check if all roomwalls belong to a stairway
                    foreach (var stair in stairs)
                    {
                        var wallsAreIdentical = stair.WallsAreIdentical(roomWalls);
                        //var portsAreIdentical = stair.PortsAreIdentical(roomGates);

                        if (wallsAreIdentical && level.LevelId == stair.BottomLevelId())
                        {
                            lowerGateToBeSaved = stair.GateByLevelId(stair.BottomLevelId());
                            upperGateToBeSaved = stair.GateByLevelId(stair.TopLevelId());
                            roomIsFound = true;
                            spanFloors = stair.SpanFloors;
                            break;
                        }                          
                    }

                    if (!roomIsFound)
                        continue;

                    if (lowerGateToBeSaved == null)
                    {
                        UnityEngine.Debug.LogError("Could not find lowerGateToBeSaved");
                        continue;
                    }
                    if (upperGateToBeSaved == null)
                    {
                        UnityEngine.Debug.LogError("Could not find upperGateToBeSaved");
                        continue;
                    }


                    //Found room which is in fact a part of a stairway...delete that room
                    SimpleRoom roomToBeDeleted = rooms[i];
                    LogCoordinatesToUnity(level, roomToBeDeleted, true);
                    level.RemoveRoomAt(i);

                    foreach (RoomElement wall in roomToBeDeleted.RoomWalls)
                    {
                        if (BothVertexesExistInAnotherRoom(wall, rooms))
                        {
                            // Do nothing with this wall.
                            /*LogWriter.Instance.WriteToLog("Wall with vertex ids "
                                            + wall.vStart.vertexID + " "
                                            + wall.vEnd.vertexID + " exists in another room, discarding.");*/
                        }
                        else
                        {
                            // Need to make a barrier out of this wall, however the question is.. in which room?
                            foreach (var kvp in roomPolygons)
                            {
                                PolygonHelper poly = new PolygonHelper(kvp.Key);

                                bool vStartInPoly = false;
                                bool vEndInPoly = false;

                                vStartInPoly = poly.PointInPolygon(wall.VStart.X, wall.VStart.Y);
                                vEndInPoly = poly.PointInPolygon(wall.VStart.X, wall.VStart.Y);

                                if (vStartInPoly != vEndInPoly)
                                    UnityEngine.Debug.LogError("One vertex is in the room, and the other isn't? Please check.");

                                if (vStartInPoly && vEndInPoly)
                                {
                                    // We've found the room! Make a barrier in this room.
                                    /*LogWriter.Instance.WriteToLog("Wall with vertex ids "
                                            + wall.vStart.vertexID + " "
                                            + wall.vEnd.vertexID + " should be a barrier in Room "
                                            + kvp.Value.RoomID);*/

                                    ConvertWallToBarrierInRoom(kvp.Value.RoomId, wall, rooms);
                                }
                            }
                        }
                    }

                    // Processing lower stair gate.
                    RoomElement foundGateLower = FindSameGateVertexes(lowerGateToBeSaved, rooms);

                    if (foundGateLower != null)
                    {
                        // We need to set the gate already in the room to a stairway gate.
                        /*LogWriter.Instance.WriteToLog("Gate with vertex ids "
                                        + lowerGateToBeSaved.vStart.vertexID + " "
                                        + lowerGateToBeSaved.vEnd.vertexID + " exists in another room on the same level.");*/
                        UnityEngine.Debug.Log("Set Inner for Lower.");
                        foundGateLower.SetInnerType(lowerGateToBeSaved.GetInnerType());
                    }
                    else
                    {
                        // Need to put this gate in the room, however the question is.. in which room?
                        //LogWriter.Instance.WriteToLog("Trying to find which room this gate belongs in.");

                        foreach (var kvp in roomPolygons)
                        {
                            PolygonHelper poly = new PolygonHelper(kvp.Key);

                            bool vStartInPoly = false;
                            bool vEndInPoly = false;

                            vStartInPoly = poly.PointInPolygon(lowerGateToBeSaved.VStart.X, lowerGateToBeSaved.VStart.Y);
                            vEndInPoly = poly.PointInPolygon(lowerGateToBeSaved.VStart.X, lowerGateToBeSaved.VStart.Y);

                            if (vStartInPoly != vEndInPoly)
                                UnityEngine.Debug.LogError("One vertex is in the room, and the other isn't? Please check.");

                            if (vStartInPoly && vEndInPoly)
                                SaveGateToStairPortInRoom(kvp.Value.RoomId, lowerGateToBeSaved, rooms);
                        }
                    }

                    // Processing upper stair gate.
                    upperGatesToBeSaved.Add(upperGateToBeSaved);
                    RoomElement foundGateUpper = FindSameGateVertexes(upperGateToBeSaved, levels[j + spanFloors].TempRooms);
                    if (foundGateUpper != null)
                    {
                        UnityEngine.Debug.Log("Set Inner for Upper in Temp Rooms above.");
                        foundGateUpper.SetInnerType(upperGateToBeSaved.GetInnerType());
                        /*LogWriter.Instance.WriteToLog("Gate with vertex ids "
                                        + upperGateToBeSaved.vStart.vertexID + " "
                                        + upperGateToBeSaved.vEnd.vertexID + " exists in another above room, such gate modified.");*/
                    }
                }
            }

            foreach (var stair in stairs)
            {
                var walls = stair.Walls;
                var ports = stair.Ports;

                for (var index = 0; index < walls.Count; ++index)
                {
                    var startGdCheck =
                        walls[index].VStart.X == ports[0].Data.VStart.X && walls[index].VStart.Y == ports[0].Data.VStart.Y
                        || walls[index].VStart.X == ports[0].Data.VEnd.X && walls[index].VStart.Y == ports[0].Data.VEnd.Y;

                    var endGdCheck =
                        walls[index].VEnd.X == ports[0].Data.VStart.X && walls[index].VEnd.Y == ports[0].Data.VStart.Y
                        || walls[index].VEnd.X == ports[0].Data.VEnd.X && walls[index].VEnd.Y == ports[0].Data.VEnd.Y;

                    var startGuCheck =
                         walls[index].VStart.X == ports[1].Data.VStart.X && walls[index].VStart.Y == ports[1].Data.VStart.Y
                        || walls[index].VStart.X == ports[1].Data.VEnd.X && walls[index].VStart.Y == ports[1].Data.VEnd.Y;

                    var endGuCheck =
                        walls[index].VEnd.X == ports[1].Data.VStart.X && walls[index].VEnd.Y == ports[1].Data.VStart.Y
                        || walls[index].VEnd.X == ports[1].Data.VEnd.X && walls[index].VEnd.Y == ports[1].Data.VEnd.Y;

                    if (startGdCheck && endGdCheck || startGuCheck && endGuCheck)
                        walls.RemoveAt(index);
                }
            }

            //UnityEngine.Debug.Log("Processed all levels.");
        }

        private static RoomElement FindSameGate(RoomElement gateToBeSaved, List<SimpleRoom> rooms)
        {

            foreach (var room in rooms)
            {
                var gates = room.Gates;
                var gate = gates.Find(pGates => pGates.ElementId == gateToBeSaved.ElementId);

                if (gate == null)
                    continue;

                return gate;
            }

            return null;
        }

        private static RoomElement FindSameGateVertexes(RoomElement r, List<SimpleRoom> rooms)
        {
            if (r == null)
            {
                UnityEngine.Debug.LogError("RoomElement is null");
                return null;
            }

            foreach (var room in rooms)
            {
                foreach (RoomElement g in room.Gates)
                {
                    if (HasSameVertexes(g, r))
                    {
                        return g;
                    }
                }
            }
            return null;
        }

        private static bool HasSameVertexes(RoomElement g, RoomElement r)
        {
            bool vEndMatch = false;
            bool vStartMatch = false;

            if (g.VEnd.X == r.VEnd.X && g.VEnd.Y == r.VEnd.Y || g.VEnd.X == r.VStart.X && g.VEnd.Y == r.VStart.Y)
            {
                vEndMatch = true;
            }
            if (g.VStart.X == r.VEnd.X && g.VStart.Y == r.VEnd.Y || g.VStart.X == r.VStart.X && g.VStart.Y == r.VStart.Y)
            {
                vStartMatch = true;
            }

            return vEndMatch && vStartMatch;
        }

        private static void ConvertWallToBarrierInRoom(int roomId, RoomElement element, List<SimpleRoom> rooms)
        {
            foreach (SimpleRoom room in rooms)
            {
                // If there is no room with this ID it won't change anything.
                if (room.RoomId == roomId)
                {
                    element.ChangeType(ElementType.Barricade);

                    room.AddElement(element);
                    break;
                }
            }
        }

        private static void SaveGateToStairPortInRoom(int roomId, RoomElement element, List<SimpleRoom> rooms)
        {
            foreach (SimpleRoom room in rooms)
            {
                // If there is no room with this ID it won't change anything.
                if (room.RoomId != roomId) continue;

                bool duplicate = false;

                foreach (RoomElement gate in room.Gates)
                {
                    if (HasSameVertexes(gate, element))
                    {
                        duplicate = true;
                    }
                }

                if (!duplicate)
                {
                    room.AddElement(element);
                }
                else
                {
                    //UnityEngine.Debug.Log("SaveGateToStairPortInRoom: duplicate gate, not saving");
                }

                break;
            }
        }

        private static bool BothVertexesExistInAnotherRoom(RoomElement w, List<SimpleRoom> rooms)
        {
            bool vEndMatch = false;
            bool vStartMatch = false;

            foreach (SimpleRoom room in rooms)
            {
                foreach (Vertex v in room.Coordinates)
                {
                    if (v.id == w.VEnd.id)
                    {
                        vEndMatch = true;
                    }
                    if (v.id == w.VStart.id)
                    {
                        vStartMatch = true;
                    }
                }
            }

            return vEndMatch && vStartMatch;
        }

        private static void LogCoordinatesToUnity(Domain.Level.SimpleLevel level, SimpleRoom roomToBeDeleted, bool toDelete)
        {
            string msg = "Level: " + level.LevelId + " Room: ";

            msg = roomToBeDeleted.Coordinates.Aggregate(msg, (current, v) => current + (v.id + " | "));

            if (toDelete)
                msg += "Marked for deletion.";

            //UnityEngine.Debug.Log(msg);
        }
    }
}
