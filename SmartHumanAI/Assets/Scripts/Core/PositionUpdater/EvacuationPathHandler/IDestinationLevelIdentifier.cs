using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.PositionUpdater.EvacuationPathHandler
{
    internal interface IDestinationLevelIdentifier
    {
        int LevelID(int currentLevelID, List<int> testLevels);
    }

    internal class MultilLevelDestinationLevelID : IDestinationLevelIdentifier
    {
        public int LevelID(int currentLevelID, List<int> testLevels)
        {
            var utilities = new List<float>();
            var probabilities = new List<float>();

            foreach (var levelId in testLevels)
            {
                //check the distance between agent's current Level and the test Level
                var dist = (levelId > currentLevelID) ? levelId - currentLevelID : currentLevelID - levelId;
                //identify the direction of movenent (1 for upward, 0 for downward)
                var direction = (levelId > currentLevelID) ? 1 : 0;
                //Calculate the utility for the given level
                utilities.Add(Params.Current.DestinationLevelTheta1 * dist + Params.Current.DestinationLevelTheta2 *  direction);
            }

            //get total utility
            var totalUtility = utilities.Sum();

            //calculate probabilities
            foreach (var util in utilities)           
                probabilities.Add((float)Math.Abs(Math.Pow(Math.E, util) / totalUtility));

            var probSize = probabilities.Count;
            var index = Utils.ProbabilisticIndex(probabilities, Utils.GetNextRnd());

            return (index > 0 && index < probSize) ? testLevels[index] : testLevels[0];
        }
    }
}
