using Assets.Scripts.Core.Handlers.DynamicAgentGenerationHandler;
using Core;
using DataFormats;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Domain.Distribution
{
    internal interface IDynamicGeneration
    {
        int AmountToGenerate(int _nCycle);
    }

    internal class DynamicGenerationDiscrete : IDynamicGeneration
    {
        internal List<TimePopulation> PopulationTimetable;

        internal DynamicGenerationDiscrete(DynamicData dynData)
        {
            PopulationTimetable = dynData.PopulationTimetable;
        }

        public int AmountToGenerate(int _nCycle)
        {
            if (PopulationTimetable == null || PopulationTimetable.Count == 0)
                return 0;

            int nIncrease = 0;

            if (_nCycle * Params.Current.TimeStep >= PopulationTimetable[0].time)
            {
                nIncrease = PopulationTimetable[0].numberOfPeople;
                PopulationTimetable.RemoveAt(0);
            }

            return nIncrease;
        }
    }

    internal class DynamicGenerationContinuous : IDynamicGeneration
    {
        internal List<TimePopulation> PopulationTimetable;
        internal float timePeriod = 1;
        internal float perSecond = 0;
        internal float leftOver = 0;

        internal DynamicGenerationContinuous(DynamicData dynData)
        {
            PopulationTimetable = dynData.PopulationTimetable;
        }

        public int AmountToGenerate(int _nCycle)
        {
            if (PopulationTimetable == null || PopulationTimetable.Count == 0)
                return 0;

            if (_nCycle * Params.Current.TimeStep >= PopulationTimetable[0].time)
            {
                if (PopulationTimetable.Count >= 2)
                    timePeriod = PopulationTimetable[1].time - PopulationTimetable[0].time;
                perSecond = PopulationTimetable[0].numberOfPeople / timePeriod;
                PopulationTimetable.RemoveAt(0);
            }

            float targetNumber = leftOver + perSecond;
            int nIncrease = Mathf.FloorToInt(targetNumber);
            leftOver = targetNumber - nIncrease;

            return nIncrease;
        }
    }

    internal class DynamicGenerationPoisson : IDynamicGeneration
    {
        internal List<TimePopulation> PopulationTimetable;
        internal float timePeriod = 1;
        internal float perSecond = 0;
        internal PoissonEvaluator poisson;
        List<double> probabilityMassFunctions;

        internal DynamicGenerationPoisson(DynamicData dynData)
        {
            PopulationTimetable = dynData.PopulationTimetable;
        }

        public int AmountToGenerate(int _nCycle)
        {
            if (PopulationTimetable == null || PopulationTimetable.Count == 0)
                return 0;

            if (_nCycle * Params.Current.TimeStep >= PopulationTimetable[0].time)
            {
                if (PopulationTimetable.Count >= 2)
                    timePeriod = PopulationTimetable[1].time - PopulationTimetable[0].time;
                perSecond = PopulationTimetable[0].numberOfPeople / timePeriod;
                PopulationTimetable.RemoveAt(0);

                poisson = new PoissonEvaluator(perSecond);
                probabilityMassFunctions = poisson.GenerateList(0.0001);
            }

            int nIncrease = 0;

            if (probabilityMassFunctions != null)
                nIncrease = RunProbabilities(probabilityMassFunctions);

            return nIncrease;
        }

        private int RunProbabilities(List<double> probabilityMassFunctions)
        {
            var randomNum = Utils.GetNextRnd(0f, 1f);
            double sum = 0;

            for (int i = 0; i < probabilityMassFunctions.Count; i++)
            {
                sum += probabilityMassFunctions[i];
                if (randomNum <= sum)
                    return i;
            }

            return probabilityMassFunctions.Count - 1;
        }
    }
}