using System;
using System.Collections.Generic;
using System.Linq;
using DataFormats;
using Helper;
using UnityEngine;

namespace Assets.Scripts
{
    internal static class RoomGeneration
    {

        public static List<List<int[]>> GenerateRooms(Model model, Dictionary<int, Vertex> vIDs)
        {
            List<List<int[]>> roomRegions = new List<List<int[]>>();

            foreach (Level level in model.levels)
            {
                List<int[]> roomRegionsLevel = GenerateRooms(level, vIDs);
                RemoveDuplicateRooms(ref roomRegionsLevel, vIDs);
                roomRegions.Add(roomRegionsLevel);
            }

            return roomRegions;
        }

        public static List<int[]> GenerateRooms(Level level, Dictionary<int, Vertex> vIDs)
        {
            // Setup a new graph for this level.
            Graph<int> graph = new Graph<int>();
            List<int[]> roomRegionsLevel = new List<int[]>();

            // Fill the graph with model's walls and gates.
            foreach (Wall wall in level.wall_pkg.walls)
                graph.AddNodesAndConnect(wall.vertices[0].id, wall.vertices[1].id);
            foreach (Gate gate in level.gate_pkg.gates)
                graph.AddNodesAndConnect(gate.vertices[0].id, gate.vertices[1].id);

            //Debug.Log("Level " + level.id + ": " + graph.ToString());

            while (true)
            {
                // Pick a random node with an untraversed edge.
                GraphNode<int> startingNode = PickStartingNode(graph);
                List<int> currentRoom = new List<int>();

                if (startingNode == null)
                {
                    // No more nodes left, graph is completely processed.
                    break;
                }

                int nodeA = startingNode.Value;
                int nodeB = FindNodeB(ref graph, nodeA);
                int nodeC = FindNodeC(ref graph, nodeA, nodeB, vIDs);

                currentRoom.Add(nodeA);
                currentRoom.Add(nodeB);

                while (startingNode.Value != nodeC)
                {
                    currentRoom.Add(nodeC);
                    nodeA = nodeB;
                    nodeB = nodeC;
                    nodeC = FindNodeC(ref graph, nodeA, nodeB, vIDs);
                }

                //PrintCurrentRoom(currentRoom);

                roomRegionsLevel.Add(currentRoom.ToArray());

            }

            return roomRegionsLevel;
        }

        public static void RemoveDuplicateRooms(ref List<int[]> roomRegionsLevel, Dictionary<int, Vertex> vIDs)
        {
            Dictionary<int, float> roomAreas = new Dictionary<int, float>();

            List<int> roomsToRemove = new List<int>();
            List<int[]> roomsSorted = new List<int[]>();

            for (int roomNum = 0; roomNum < roomRegionsLevel.Count; roomNum++)
            {
                int[] room = roomRegionsLevel[roomNum];

                List<Vector2> roomVector2 = new List<Vector2>();
                Vertex v = null;

                foreach (int i in room)
                {
                    v = vIDs[i];
                    roomVector2.Add(new Vector2(v.X, v.Y));
                }

                roomsSorted.Add(room.OrderBy(i => i).ToArray());

                // Calculate area of room, and save to dictionary.
                Triangulator.Triangulate(roomVector2.ToArray());
                roomAreas.Add(roomNum, Triangulator.Area());
            }

            foreach (var item in roomAreas)
            {
                if (item.Value > 0)
                {
                    roomsToRemove.Add(item.Key);
                }
            }

            for (int i = roomRegionsLevel.Count - 1; i >= 0; i--)
            {
                if (roomsToRemove.Contains(i))
                    roomRegionsLevel.RemoveAt(i);
            }
        }

        private static int FindLargestRoom(Dictionary<int, float> roomAreas)
        {
            float largest = float.MinValue;
            int room = -1;

            foreach (KeyValuePair<int, float> kvp in roomAreas)
            {
                if (kvp.Value > largest)
                {
                    largest = kvp.Value;
                    room = kvp.Key;
                }
            }

            return room;
        }

        public static Dictionary<int, Vertex> SetupVertexIDs(Model currentModel)
        {
            //Setup a dictionary for quickly finding vertices by their id.
            Dictionary<int, Vertex> vertexIds = new Dictionary<int, Vertex>();
            foreach (Level l in currentModel.levels)
            {
                Dictionary<int, Vertex> vertexIdsForLevel = new Dictionary<int, Vertex>();

                foreach (Wall w in l.wall_pkg.walls)
                {
                    foreach (Vertex v in w.vertices)
                    {
                        vertexIds[v.id] = v;
                        vertexIdsForLevel[v.id] = v;
                    }
                }
                foreach (Gate g in l.gate_pkg.gates)
                {
                    foreach (Vertex v in g.vertices)
                    {
                        vertexIds[v.id] = v;
                        vertexIdsForLevel[v.id] = v;
                    }
                }

                //PrintVertexIDs("LEVEL " + l.id + " ", vertexIdsForLevel);
            }

            //PrintVertexIDs("", vertexIds);

            return vertexIds;
        }

        public static Dictionary<int, Vertex> SetupVertexIDs(Level currentLevel)
        {
            //Setup a dictionary for quickly finding vertices by their id.
            Dictionary<int, Vertex> vertexIds = new Dictionary<int, Vertex>();

            foreach (Wall w in currentLevel.wall_pkg.walls)
            {
                foreach (Vertex v in w.vertices)
                {
                    vertexIds[v.id] = v;
                }
            }
            foreach (Gate g in currentLevel.gate_pkg.gates)
            {
                foreach (Vertex v in g.vertices)
                {
                    vertexIds[v.id] = v;
                }
            }

            //PrintVertexIDs("", vertexIds);

            return vertexIds;
        }

        private static void PrintVertexIDs(string message, Dictionary<int, Vertex> vIDs)
        {
            string str = vIDs.Aggregate(message, (current, kvp) => current + (kvp.Key + " ") + kvp.Value.ToString() + Environment.NewLine);

            Debug.Log(str);
        }

        private static void PrintCurrentRoom(List<int> currentRoom)
        {
            string str = "Found Room: ";

            foreach (int i in currentRoom)
            {
                str += i + " ";
            }

            Debug.Log(str);
        }

        private static int FindNodeC(ref Graph<int> graph, int nodeA, int nodeB, Dictionary<int, Vertex> vIDs)
        {
            GraphNode<int> gNodeB = graph.GetNode(nodeB);

            Dictionary<int, float> possibleNodesAngles = new Dictionary<int, float>();

            for (int i = 0; i < gNodeB.Traversed.Count; i++)
            {
                int nodeC = gNodeB.Neighbors[i].Value;

                if (!gNodeB.Traversed[i] && nodeC != nodeA)
                {
                    // Found a possible NodeC to traverse to.
                    try
                    {
                        Vertex a = vIDs[nodeA];
                        Vertex b = vIDs[nodeB];
                        Vertex c = vIDs[nodeC];

                        float angle = Angle(c.X, c.Y, b.X, b.Y, a.X, a.Y);

                        if (possibleNodesAngles.ContainsKey(nodeC))
                        {
                            Debug.LogError("possibleNodesAngles already contains key??");
                        }
                        else
                        {
                            // Record node and angle
                            possibleNodesAngles.Add(nodeC, angle);
                        }

                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error in reading VertexID Dictionary: " + e.ToString());
                    }
                }
            }

            int nodeC_Final = -1;
            float smallestAngle = float.MaxValue;

            // Find the edge with the smallest angle
            foreach (KeyValuePair<int, float> kvp in possibleNodesAngles)
            {
                if (kvp.Value < smallestAngle)
                {
                    smallestAngle = kvp.Value;
                    nodeC_Final = kvp.Key;
                }
            }

            if (possibleNodesAngles.Count < 1)
                UIController.Instance.ShowGeneralDialog("Error in determining room layout. Please rectify your recent changes to ensure no overlapping walls.", "Room Generation");
            else
                graph.SetTraversedFrom(nodeB, nodeC_Final);

            return nodeC_Final;
        }

        private static int FindNodeB(ref Graph<int> graph, int nodeA)
        {
            GraphNode<int> gNodeA = graph.GetNode(nodeA);

            for (int i = 0; i < gNodeA.Traversed.Count; i++)
            {
                int nodeB = gNodeA.Neighbors[i].Value;

                if (!gNodeA.Traversed[i] && nodeB != nodeA)
                {
                    // Found a NodeB to traverse to.
                    graph.SetTraversedFrom(nodeA, nodeB);
                    return gNodeA.Neighbors[i].Value;
                }
            }

            Debug.LogError("Could not find a NodeB");
            return -1;
        }

        private static GraphNode<int> PickStartingNode(Graph<int> graph)
        {
            foreach (GraphNode<int> node in graph.Nodes)
            {
                foreach (bool trav in node.Traversed)
                    if (!trav)
                        return node;
            }

            return null;
        }

        private static float Angle(float x1, float y1, float x2, float y2, float x3, float y3)
        {
            Vector2 p1 = new Vector2(x1, y1);
            Vector2 p2 = new Vector2(x2, y2);
            Vector2 p3 = new Vector2(x3, y3);

            Vector2 v1 = p1 - p2;
            Vector2 v2 = p3 - p2;

            float angle = -(180 / Mathf.PI) * Mathf.Atan2(v1.x * v2.y - v1.y * v2.x, v1.x * v2.x + v1.y * v2.y);

            angle = angle < 0 ? angle + 360 : angle;

            return angle;
        }

    }
}
