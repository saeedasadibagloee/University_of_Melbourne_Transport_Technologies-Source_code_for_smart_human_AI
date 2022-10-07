using System;
using System.Collections.Generic;
using DataFormats;
using Domain.Elements;
using Domain.Level;
using Domain.Stairway;

namespace Core.Handlers
{
    interface IStairMapAreaHandler
    {
        void SetStairGridAreaMap(Dictionary<int, uint> data);
        uint GetAreaID(int stairID);
    }

    interface IStairwayHandler : IStairMapAreaHandler
    {
        void HandleStairway(Stair pStair);        
        bool PortIsCommon(int id);

        List<AStairway> Stairs();
        AStairway StairwayToUse(float x, float y, int levelID, InnerType portType, int gateID = -1);
        AStairway StairwayToUse(float x, float y, int levelID, Dictionary<int, StairEntries> prevStairs);

        void Clear();
    }


            AddNewStairway(newStairway);
        }

        public void SetStairGridAreaMap(Dictionary<int, uint> pMap)
        {
            stairGridAreaMap = pMap;
        }

        public uint GetAreaID(int stairID)
        {
            if (stairGridAreaMap.ContainsKey(stairID))
                return stairGridAreaMap[stairID];

            return 0;
        }

        public AStairway StairwayToUse(float x, float y, int levelID, InnerType portType, int gateID)
        {
            if (gateID != -1)
            {
                return stairs.Find(pStair =>
                    pStair.TopLevelId() == levelID && pStair.TopLevelGate().GetInnerType() == portType && pStair.TopLevelGate().ElementId == gateID
                    || pStair.BottomLevelId() == levelID && pStair.BottomLevelGate().GetInnerType() == portType && pStair.BottomLevelGate().ElementId == gateID
                );
            }

            List<AStairway> stairsWithPortType = null;

            if (portType == InnerType.StairwayGateDown)
            {
                stairsWithPortType = stairs.FindAll(pStair =>
                    pStair.TopLevelId() == levelID && pStair.TopLevelGate().GetInnerType() == portType                
                );
            }
            else
            {
                stairsWithPortType = stairs.FindAll(pStair =>
                    pStair.BottomLevelId() == levelID && pStair.BottomLevelGate().GetInnerType() == portType
                );
            }              

            var targetIndex = -1;

            for (var index = 0; index < stairsWithPortType.Count; ++index)
            {
                if (!stairsWithPortType[index].PointIsInside(x, y))
                    continue;

                targetIndex = index;
                break;
            }

            return targetIndex != -1 ? stairsWithPortType[targetIndex] : null;         
        }

        public AStairway StairwayToUse(float x, float y, int levelID, Dictionary<int, StairEntries> prevStairs)
        {
            var testingStairs =
                stairs.FindAll(pStar =>
                    !prevStairs.ContainsKey(pStar.StairwayID)
                    && (pStar.BottomLevelId() == levelID || pStar.TopLevelId() == levelID)
            );

            foreach (var stair in testingStairs)
            {
                var lowLevelGate = stair.BottomLevelGate();
                var topLevelGate = stair.TopLevelGate();

                //agent must leave the staircase via the nearest port
                var lowLevelExitDistance =
                        Utils.DistanceBetween(x, y, lowLevelGate.VMiddle.X, lowLevelGate.VMiddle.Y
                );

                var topLevelExitDistance =
                    Utils.DistanceBetween(x, y, topLevelGate.VMiddle.X, topLevelGate.VMiddle.Y
                );

                var exitLevelID = 
                    (lowLevelExitDistance < topLevelExitDistance) ? 
                        stair.BottomLevelId() : stair.TopLevelId();

                if (exitLevelID == levelID && stair.PointIsInside(x, y))
                    return stair;
            }            
          
            return null;
        }

        public void Clear()
        {
            stairs.Clear();
        }

        private void AddNewStairway(AStairway pStairtway)
        {
            stairs.Add(pStairtway);
        }

        public bool PortIsCommon(int id)
        {
            return stairs.Find(pStair => pStair.PortIdExists(id)) != null;
        }

        public List<AStairway> Stairs() { return stairs; }
    }
}
