using System;
using System.Collections.Generic;
using System.IO;

namespace InputOutput
{
    
                _logQueue.Enqueue(cell);

                // If we have reached the Queue Size then flush the Queue
                if (_logQueue.Count >= _queueSize || DoPeriodicFlush())
                {
                    FlushLog();
                }
            }
        }

        public void WriteToCsvLine(List<string> cells)
        {
            // Lock the queue while writing to prevent contention for the log file
            lock (_logQueue)
            {
                // Create the entry and push to the Queue
                string line = "";

                foreach (var cell in cells)
                {
                    line += cell + ",";
                }

                _logQueue.Enqueue(line);

                // If we have reached the Queue Size then flush the Queue
                if (_logQueue.Count >= _queueSize || DoPeriodicFlush())
                {
                    FlushLog();
                }
            }
        }

        private bool DoPeriodicFlush()
        {
            TimeSpan logAge = DateTime.Now - _lastFlushed;
            if (logAge.TotalSeconds >= _maxLogAge)
            {
                _lastFlushed = DateTime.Now;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Flushes the Queue to the physical log file
        /// </summary>
        private void FlushLog()
        {
            while (_logQueue.Count > 0)
            {
                string entry = _logQueue.Dequeue();
                string logPath = _logDir + _logFile;

                try
                {
                    // This could be optimised to prevent opening and closing the file for each write
                    using (FileStream fs = File.Open(logPath, FileMode.Append, FileAccess.Write))
                    {
                        using (StreamWriter log = new StreamWriter(fs))
                        {
                            log.WriteLine(entry);
                        }
                    }
                } catch(IOException e)
                {
                    UnityEngine.Debug.LogError("Could not write to CSV file, is it open elsewhere? " + e.ToString());
                }
            }
        }
    }

    public static class LogToCsvCollisionAvoidance
    {
        private static StreamWriter _streamOut = null;
        public static bool Initialised = false;

        public static void Initialise()
        {
            string path = "CSVLog.csv";

            try
            {
                _streamOut = new StreamWriter(path, true);

                List<string> line = new List<string>
                {
                    "Agent",
                    "AgentX",
                    "AgentY",
                    "OtherX",
                    "OtherY",
                    "VIx",
                    "VIy",
                    "VJx",
                    "VJy",
                    "DJIx",
                    "DJIy",
                    "DJIcrossVIx",
                    "DJIcrossVIy",
                    "DJIcrossVIz",
                    "NumerX",
                    "NumerY",
                    "NumerZ",
                    "Denomi",
                    "TJx",
                    "TJy",
                    "DOT",
                    "FinalVectorX",
                    "FinalVectorY"
                };

                WriteLine(line);

                _streamOut.AutoFlush = true;

            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log(e.ToString());
            }
            Initialised = true;
        }

        public static void WriteLine(List<string> line)
        {
            if (!Initialised)
                Initialise();

            string wholeLine = "";
            foreach (string cell in line)
            {
                wholeLine += cell + ",";
            }
            _streamOut.WriteLine(wholeLine);
        }

        public static void Complete()
        {
            if (_streamOut != null)
            {
                _streamOut.Flush();
                _streamOut.Close();
            }
        }
    }
}