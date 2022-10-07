using Core.Handlers;
using Core.Logger;

namespace Core.Validator
{
    internal interface IModelValidator
    {
        bool Validate();
    }

    abstract class DistributionValidator
    {
        protected ILevelHandler _levelHandler = null;

        protected DistributionValidator(ILevelHandler levelHandler)
        {
            this._levelHandler = levelHandler;
        }

        public bool ValidateDistribution()
        {
            var levels = _levelHandler.Levels();

            //there should be at least one evacuation gate
            if (levels.Find(pLevel => pLevel.HasDestinationGates()) == null)
            {
                LogWriter.Instance.WriteToLog("ModelValidator: no evacuation gates found.");
                return false;
            }

            if (levels.Find(pLevel => pLevel.Distributions.Count != 0) == null)
            {
                LogWriter.Instance.WriteToLog("ModelValidator: there should be at least one distribution.");
                return false;
            }

            bool distributionPopulationExists = false;

            foreach (var level in levels)
                foreach (var distribution in level.Distributions)
                    if (distribution.HasPopulation())
                    {
                        distributionPopulationExists = true;
                        break;
                    }

            return distributionPopulationExists;
        }
    }
}
