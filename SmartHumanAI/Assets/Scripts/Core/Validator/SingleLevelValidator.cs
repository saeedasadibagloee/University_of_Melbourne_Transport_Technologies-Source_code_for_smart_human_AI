using Core.Handlers;

using System;
using System.Collections.Generic;

namespace Core.Validator
{
    internal class SingleLevelValidator : DistributionValidator, IModelValidator
    {
        public SingleLevelValidator(ILevelHandler pHandler) 
            : base(pHandler) { }   

        public bool Validate()
        {
            foreach (var level in _levelHandler.Levels())
            {
                level.CreateLevelInfo();
                level.CreateRoomRouteInfo();
                level.CreateRealRooms();
            }

            return ValidateDistribution();
        }
    }
}
