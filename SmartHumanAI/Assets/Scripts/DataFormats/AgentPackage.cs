using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataFormats
{
    [Serializable]
    public class DynamicData
    {
        public List<Group> DymanicGroupData;
        public List<TimePopulation> PopulationTimetable;
        public int TimetableType = (int)Def.TimetableType.Discrete;
        
        public int MaxPopulation = 0;
        public List<float> PopulationIncremental;
        public List<int> Times;

        public DynamicData()
        {
        }

        public DynamicData(List<Group> dynGroups)
        {
            DymanicGroupData = new List<Group>();
            DymanicGroupData.AddRange(dynGroups);
        }
    }

    [Serializable]
    public class TimePopulation
    {
        public float time;
        public int numberOfPeople;

        public TimePopulation()
        {
        }

        
    }

    [Serializable]
    public class Group
    {
        /// <summary>
        /// How many people in a group.
        /// </summary>
        public int groupNum = 0;
        /// <summary>
        /// Number of groups with this many people.
        /// </summary>
        public float numGroups = 0;

        public Group(int groupNum, float numGroups)
        {
            this.groupNum = groupNum;
            this.numGroups = numGroups;
        }

        public Group() { }
    }

    [Serializable]
    public class DistributionPackage : EventArgs
    {
        public List<DistributionData> distributions { get; set; }

        public DistributionPackage()
        {
            distributions = new List<DistributionData>();
        }
    }
}