using Assets.Scripts.DataFormats;
using DataFormats;
using Domain.Elements;
using Gate = Domain.Elements.Gate;
using Wall = DataFormats.Wall;

namespace Domain.Level
{
    internal static class Builder
    {
        public static Gate CreateGate(DataFormats.Gate pGate)
        {
            var newGate = new Gate(
                pGate.vertices[0].X, pGate.vertices[0].Y,
                pGate.vertices[1].X, pGate.vertices[1].Y
            );            

            newGate.SetVerticesId(pGate.vertices[0].id, pGate.vertices[1].id);
            newGate.SetElementId(pGate.id);
            newGate.SetLength(pGate.length);
            newGate.SetAngle(pGate.angle);
            newGate.CalcCAPoints();

            if (pGate.waitingData != null)
            {
                newGate.WaitingData = new WaitingDataCore
                {
                    waitTime = pGate.waitingData.waitTime,
                    waitPosX = pGate.waitingData.waitPosX,
                    waitPosY = pGate.waitingData.waitPosY,
                    targetPosX = pGate.waitingData.targetPosX,
                    targetPosY = pGate.waitingData.targetPosY,
                    waitPos2X = pGate.waitingData.waitPos2X,
                    waitPos2Y = pGate.waitingData.waitPos2Y,
                    targetPos2X = pGate.waitingData.targetPos2X,
                    targetPos2Y = pGate.waitingData.targetPos2Y,
                    isBidirectional = pGate.waitingData.isBidirectional
                };
            }

            if (pGate.trainData != null)
            {
                newGate.TrainData = pGate.trainData;
            }

            newGate.DesignatedOnly = pGate.designatedOnly;

            return newGate;
        }

        public static Barricade CreateBarricade(Wall pWall)
        {
            var newBarricade = new Barricade(
               pWall.vertices[0].X, pWall.vertices[0].Y,
               pWall.vertices[1].X, pWall.vertices[1].Y
            );
            AssignWallData(newBarricade, pWall);

            return newBarricade;
        }

        public static Elements.Wall CreateWall(Wall pWall)
        {  
            var newWall = new Elements.Wall(
                pWall.vertices[0].X, pWall.vertices[0].Y,
                pWall.vertices[1].X, pWall.vertices[1].Y
            );
            AssignWallData(newWall, pWall);

            return newWall;
        }

        private static void AssignWallData(RoomElement pElement, Wall pWall)
        {
            pElement.SetVerticesId(pWall.vertices[0].id, pWall.vertices[1].id);
            pElement.SetElementId(pWall.id);
            pElement.SetLength(pWall.length);
            pElement.SetAngle(pWall.angle);
            pElement.SetIWLWG(pWall.iWlWG);
            pElement.SetIsLow(pWall.isLow);
        }
    }
}
