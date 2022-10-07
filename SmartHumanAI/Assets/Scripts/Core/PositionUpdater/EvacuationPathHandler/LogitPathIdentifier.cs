using Core.Threat;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.PositionUpdater.EvacuationPathHandler
{
    using Domain.Room;

    internal class LogitPathIdentifier
    {
        private static readonly Random _rand = new Random(DateTime.Now.Millisecond);

        public static List<int> Path(int levelID, EvacuationPath data, IThreatHandler tHandler, List<EvacRoomCombination> evacPlan)
        {
            if (evacPlan.Count == 0)
                return null;

            List<Distance2GateIndexMap> roomGatesDistanceData =
                new List<Distance2GateIndexMap>();

            foreach (var r2gates in evacPlan)
            {
                var gateCombinationsSize = r2gates.GateCombinations.Count;

                // exclude the combination of gates if it's size is 1
                // and any of its gates is blocked
                if (gateCombinationsSize == 1 && tHandler.LevelHasBlockedObjects(levelID, new GateThreatHandlingStrategy()))
                {
                    var blockedGates = tHandler.BlockedObjects(new GateThreatHandlingStrategy(), levelID);
                    var gateCombination = r2gates.GateCombinations[0];

                    if (gateCombination.Gates.Any(pGate => blockedGates.Exists(pG => pG.ObjectID == pGate)))
                        continue;
                }

                var distanceDic = new Dictionary<int, float>();

                //for the given room connectivity, check each gate combination
                for (var index = 0; index < gateCombinationsSize; ++index)
                {
                    var distance = 0f;
                    distance += r2gates.GateCombinations[index].DistanceViaGates;

                    //add distance between current agent's position
                    if (data.GateData.ContainsKey(r2gates.GateCombinations[index].Gates[0])) //first gate
                        distance += data.GateData[r2gates.GateCombinations[index].Gates[0]];

                    //Save index and distance data into dictionary
                    distanceDic.Add(index, distance);
                }

                var keyR = distanceDic.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;

                roomGatesDistanceData.Add(
                    new Distance2GateIndexMap(keyR, distanceDic[keyR], r2gates.RoomCombination)
                );
            }

            if (roomGatesDistanceData.Count != 0)
            {
                List<double> logits = roomGatesDistanceData.Select(t => Math.Exp(-Params.Current.Theta * t.Distance)).ToList();

                //logit and path choice calculation
                var totalLogit = logits.Sum();

                List<float> probabilities = new List<float>();

                for (var j = 0; j < logits.Count; ++j)
                    probabilities.Add((float)(logits[j] / totalLogit));

                double rand = 0.5;

                try
                {
                    // Seems to throw some IndexOutOfRangeExceptions, thought I'd be safe and wrap it :)
                    rand = _rand.NextDouble();
                }
                catch (Exception e)
                {
                    Logger.LogWriter.Instance.WriteToLog("Minor error in random generation. Discarding... " + e);
                    rand = 0.5;
                }

                var evacPathIndex = Utils.ProbabilisticIndex(probabilities, rand);
                
                return roomGatesDistanceData[evacPathIndex].RoomConnection;
            }

            return null;
        }
    }
}
