using System.Collections.Generic;
using System.Diagnostics;
using InputOutput;

namespace Helper
{
    public static class MethodTimer
    {
        public static Dictionary<string, List<float>> MethodTimes = new Dictionary<string, List<float>>();
        private static readonly object DictionaryLock = new object();
        private const string Format = "f";

        public static void AddTime(string methodName, Stopwatch stopwatch)
        {
            float time = stopwatch.ElapsedTicks * 1000000000 / Stopwatch.Frequency;
            stopwatch.Reset();

            lock (DictionaryLock)
            {
                if (!MethodTimes.ContainsKey(methodName))
                    MethodTimes.Add(methodName, new List<float>());
                MethodTimes[methodName].Add(time);
            }
        }

        public static void OutputMethodTimesToCsv()
        {
            List<string> cells =
                new List<string> { "Method Name", "Total Time", "Calls", "Average", "Shortest", "Longest" };
            
            LogToCsv.Instance.WriteToCsvLine(cells);

            lock (DictionaryLock)
            {
                foreach (KeyValuePair<string, List<float>> kvp in MethodTimes)
                {
                    cells.Clear();
                    cells.Add(kvp.Key);

                    float totalTime = 0f;
                    float shortest = float.MaxValue;
                    float longest = float.MinValue;

                    foreach (float singleTime in kvp.Value)
                    {
                        totalTime += singleTime;
                        if (singleTime < shortest)
                            shortest = singleTime;
                        if (singleTime > longest)
                            longest = singleTime;
                    }

                    cells.Add(totalTime.ToString(Format));
                    var calls = kvp.Value.Count;
                    cells.Add(calls.ToString(Format));
                    cells.Add((totalTime / calls).ToString(Format));
                    cells.Add(shortest.ToString(Format));
                    cells.Add(longest.ToString(Format));
                    LogToCsv.Instance.WriteToCsvLine(cells);
                }
            }
            Clear();
        }

        public static void Clear()
        {
            lock (DictionaryLock)
            {
                MethodTimes.Clear();
            }
        }
    }
}
