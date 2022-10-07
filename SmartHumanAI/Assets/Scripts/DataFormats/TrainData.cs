using System;
using System.Collections.Generic;

namespace DataFormats
{
    [Serializable]
    public class TrainData
    {
        public float arrivalTime;
        public float departureTime;
        public int passengers;
        public List<int> passengersInCarriages;
    }
}
