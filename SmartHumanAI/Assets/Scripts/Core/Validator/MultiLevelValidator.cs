using System.Collections.Generic;
using Core.Handlers;
using Domain.Elements;
using Domain.Stairway;

namespace Core.Validator
{
    internal class MultiLevelValidator : DistributionValidator, IModelValidator
    {
        private readonly IStairwayHandler _stairsHandler = null;

        public MultiLevelValidator(ILevelHandler pLevelHandler, IStairwayHandler pStairsHandler)
            : base(pLevelHandler)
        {
            _stairsHandler = pStairsHandler;
        }

        public bool Validate()
        {
            var modelStairs = _stairsHandler.Stairs();
            if (modelStairs.Count == 0)
                return false;

            MlvExtension.Execute(_levelHandler, _stairsHandler);

            var levels = _levelHandler.Levels();
            foreach (var level in levels)
            {
                level.CreateLevelInfo();

                if (level.HasDestinationGates())
                {
                    _levelHandler.SetDistinationLevelID(level.LevelId);
                    level.CreateRoomRouteInfo();
                }
                else if (level.HasDestinationOrTrainGates())
                {
                    level.CreateRoomRouteInfo();
                }

                level.CreateRoomRouteInfo(InnerType.StairwayGateUp);
                level.CreateRoomRouteInfo(InnerType.StairwayGateDown);

                level.CreateRealRooms();
            }

            return ValidateDistribution();
        }
    }
}
