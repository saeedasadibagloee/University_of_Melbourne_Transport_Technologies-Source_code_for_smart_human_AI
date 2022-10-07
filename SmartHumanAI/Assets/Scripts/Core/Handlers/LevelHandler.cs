using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Assets.Scripts;
using Core.Logger;
using DataFormats;
using Domain.Elements;
using Domain.Level;
using Domain.Room;
using CircularObstacle = Domain.Elements.CircularObstacle;
using System.Linq;

namespace Core.Handlers
{
    interface IDestinationLevel
    {
        void SetDistinationLevelID(int levelID);       
        int GetDestinationLevelId();

        List<int> DestinationLevelIDs { get; }
    }

    interface ILevelFeatures
    {
        SimpleLevel Find(int levelId);
        List<SimpleRoom> GetRoomsByLevelId(int levelId);
    }
        }

        public List<int> DestinationLevelIDs
        {
            get { return _destinationLevelIDs; }
        }

        public bool SetLevel(SimpleLevel newLevel, Level level)
        {
            foreach (var wall in level.wall_pkg.walls)
            {
                if (wall.vertices.Count < _defaultVerticesCount) //there should be 2 points
                    return false;

                var newWall = Builder.CreateWall(wall);
                newLevel.GeneralElements.Add(newWall);
            }

            foreach (var gate in level.gate_pkg.gates)
            {
                if (gate.counter)
                    continue;

                if (gate.vertices.Count < _defaultVerticesCount) //there should be 2 points
                    return false;

                var newGate = Builder.CreateGate(gate);
                newLevel.GeneralElements.Add(newGate);
            }

            foreach (var obst in level.obstacle_pkg.Obstacles)
            {
                RoomElement cirecularElement =
                    new CircularObstacle(
                        obst.XPosition, obst.YPosition, obst.Radius
                );
                newLevel.GeneralElements.Add(cirecularElement);
            }

            foreach (var barricade in level.barricade_pkg.barricade_walls)
            {
                if (barricade.vertices.Count < _defaultVerticesCount) //there should be 2 points
                    return false;

                var newBarricade = Builder.CreateBarricade(barricade);
                newLevel.GeneralElements.Add(newBarricade);
            }

            foreach (var train in level.train_pkg.trains)
            {
                if (train.destinationGateID == -1)
                    return false;
                
                newLevel.Trains.Add(train);
            }

            // Detect and generate the rooms
            var vIDs = RoomGeneration.SetupVertexIDs(level);
            var roomRegions = RoomGeneration.GenerateRooms(level, vIDs);

            RoomGeneration.RemoveDuplicateRooms(ref roomRegions, vIDs);

            if (roomRegions.Count == 0)
                UnityEngine.Debug.Log("There are no rooms on level " + level.id);

            foreach(var room in roomRegions)
            {
                List<int> newRegion = new List<int>();
                newRegion.AddRange(room);
                newLevel.Regions.Add(newRegion);
            }

            newLevel.CreateLevelRooms();
            return true;
        }

        public List<SimpleRoom> GetRoomsByLevelId(int levelId)
        {           
            var level = _levels.FirstOrDefault(pLevel => pLevel.LevelId == levelId);

            return level != null ? level._Rooms.Values.ToList() : null;
        }

        public void Add(SimpleLevel level)
        {
            _levels.Add(level);
        }

        public SimpleLevel Find(int levelId)
        {
            return _levels.Find(lev => lev.LevelId == levelId);
        }

        public List<SimpleLevel> Levels()
        {
            return _levels;
        }

        public void Clear()
        {
            _levels.Clear();
            _destinationLevelIDs.Clear();
        }

        public int GetDestinationLevelId()
        {
            foreach (var level in _levels)
            {
                if (level.HasDestinationGates())
                    return level.LevelId;
            }

            return -1;
        }
    }
}
