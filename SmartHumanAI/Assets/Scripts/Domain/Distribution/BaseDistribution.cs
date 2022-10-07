using DataFormats;
using Domain;
using Domain.Distribution;
using Domain.Elements;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Domain.Distribution
{
    abstract class BaseDistribution : IDistribution
    {
        protected Color _color = Color.white;
        protected List<RoomElement> _walls = new List<RoomElement>();
        protected System.Random _rand = new System.Random();
        protected int _population = -1;

        protected DynamicData _dynamicData = null;
        /// <summary>
        /// Designated gates over time. Key is time in sec.
        /// </summary>
        protected List<DesignatedGatesData> _designatedGatesData = null;
        protected List<int> _groupIDs = new List<int>();
        protected IDynamicGeneration dynamicGeneration = null;

        public abstract DistributionType Type { get; }

        public int AmountToGenerate(int _nCycle)
        {
            if (dynamicGeneration == null)
                GenerateGenerator();

            return dynamicGeneration.AmountToGenerate(_nCycle);
        }

        private void GenerateGenerator()
        {
            switch ((Def.TimetableType)_dynamicData.TimetableType)
            {
                case Def.TimetableType.Discrete:
                    dynamicGeneration = new DynamicGenerationDiscrete(_dynamicData);
                    break;
                case Def.TimetableType.Continuous:
                    dynamicGeneration = new DynamicGenerationContinuous(_dynamicData);
                    break;
                case Def.TimetableType.Poisson:
                    dynamicGeneration = new DynamicGenerationPoisson(_dynamicData);
                    break;
            }
        }

        public abstract CpmPair GenerateLocation();

        public Color GetColor()
        {
            return _color;
        }

        public List<DesignatedGatesData> GetDesignatedGatesData()
        {
            return _designatedGatesData;
        }

        public DynamicData GetDynamicDistributionData()
        {
            return _dynamicData;
        }

        public List<int> GroupIDs()
        {
            return _groupIDs;
        }

        public bool HasPopulation()
        {
            if (_dynamicData != null) {
                if (_dynamicData.PopulationTimetable != null)
                    if (_dynamicData.PopulationTimetable.Count > 0)
                        return true;

                if (_dynamicData.MaxPopulation > 0 && _dynamicData.Times.Count > 1
                    && _dynamicData.PopulationIncremental.Count > 0
                    && _dynamicData.PopulationIncremental[0] > 0)
                    return true;
            }

            if (_population < 1 && _groupIDs.Count < 1)
                return false;

            return true;
        }

        public int Population()
        {
            return _population;
        }

        public void SetColor(Color color)
        {
            _color = color;
        }

        public void SetDesignatedGatesData(List<DesignatedGatesData> dData)
        {
            _designatedGatesData = dData;
        }

        public void SetDynamicDistributionData(DynamicData dData)
        {
            _dynamicData = dData;
        }

        public void SetGroupID(int groupID)
        {
            _groupIDs.Add(groupID);
        }

        public void SetPopulation(int population)
        {
            _population = population;
        }

        public void SetWalls(List<RoomElement> pWalls)
        {
            _walls = pWalls;
        }
    }
}
