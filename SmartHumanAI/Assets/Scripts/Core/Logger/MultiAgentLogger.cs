using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Core.Logger
{
    /// <summary>
    /// A Log class to store the message and the Date and Time the log entry was created
    /// </summary>
    internal class AgentLogEntry
    {
        public int AgentId { get; set; }

        public string Message { get; set; }
        public string LogTime { get; set; }
        public string LogDate { get; set; }

        public AgentLogEntry(int agentId, string message)
        {
            AgentId = agentId;
            Message = message;
            LogDate = DateTime.Now.ToString("yyyy-MM-dd");
            LogTime = DateTime.Now.ToString("hh:mm:ss.fff tt");
        }
    }

    internal class MultiAgentLogger
    {
        private static MultiAgentLogger _instance;
        private static Queue<AgentLogEntry> _logQueue;

        private const string LogDir = "logs";
        private const string LogExtension = ".txt";
        private const int MaxLogAge = 5;
        private static readonly int QueueSize = 50;
        private static DateTime _lastFlushed = DateTime.Now;
        private static readonly Dictionary<int, string> LogMap = new Dictionary<int, string>();

        /// <summary>
        /// Private constructor to prevent instance creation
        /// </summary>
        private MultiAgentLogger() { }

        /// <summary>
        /// An LogWriter instance that exposes a single instance
        /// </summary>
        public static MultiAgentLogger Instance
        {
            get
            {
                // If the instance is null then create one and init the Queue
                if (_instance != null) return _instance;
                _instance = new MultiAgentLogger();
                _logQueue = new Queue<AgentLogEntry>();

                return _instance;
            }
        }

        public void Initialise(int nAgents)
        {
            if (LogMap != null)
                LogMap.Clear();

            if (!Directory.Exists(LogDir))
                Directory.CreateDirectory(LogDir);
            else
            {
                ClearFolder(LogDir);
            }

            for (var id = 0; id < nAgents; ++id)
            {
                var fileName = id + LogExtension;
                var filePath = Path.Combine(LogDir, fileName);

                if (File.Exists(filePath)) continue;
                using (File.Create(filePath))
                {
                    LogMap.Add(id, filePath);
                }
            }
        }

        public void AddNewAgentDetails(int agentId)
        {
            lock (_logQueue)
            {
                if (LogMap.ContainsKey(agentId)) return;
                var fileName = agentId + LogExtension;
                var filePath = Path.Combine(LogDir, fileName);

                if (File.Exists(filePath)) return;
                using (File.Create(filePath))
                {
                    LogMap.Add(agentId, filePath);
                }
            }
        }

        /// <summary>
        /// The single instance method that writes to the log file
        /// </summary>
        /// <param name="agentId"></param>
        /// <param name="message">The message to write to the log</param>
        public void WriteToLog(int agentId, string message)
        {
            // Lock the queue while writing to prevent contention for the log file
            lock (_logQueue)
            {
                // Create the entry and push to the Queue
                var logEntry = new AgentLogEntry(agentId, message); 
                _logQueue.Enqueue(logEntry);

                // If we have reached the Queue Size then flush the Queue
                if (_logQueue.Count >= QueueSize || DoPeriodicFlush())
                {
                    ThreadPool.QueueUserWorkItem(delegate { FlushLog(); }, null);
                }
            }
        }

        /// <summary>
        /// Flushes the Queue to the physical log file
        /// </summary>
        public static void FlushLog()
        {
            lock (_logQueue)
            {
                while (_logQueue.Count > 0)
                {
                    var entry = _logQueue.Dequeue();

                    if (!LogMap.ContainsKey(entry.AgentId)) continue;
                    var fileName = LogMap[entry.AgentId];
                    // This could be optimised to prevent opening and closing the file for each write
                    using (FileStream fs = File.Open(fileName, FileMode.Append, FileAccess.Write))
                    {
                        using (StreamWriter log = new StreamWriter(fs))
                        {
                            log.WriteLine("{0}\t{1}", entry.LogTime, entry.Message);
                        }
                    }
                }
            }
        }

        private static void ClearFolder(string folderName)
        {
            DirectoryInfo dir = new DirectoryInfo(folderName);

            foreach (FileInfo fi in dir.GetFiles())
            {
                fi.Delete();
            }

            foreach (DirectoryInfo di in dir.GetDirectories())
            {
                ClearFolder(di.FullName);
                di.Delete();
            }
        }

        private bool DoPeriodicFlush()
        {
            TimeSpan logAge = DateTime.Now - _lastFlushed;
            if (logAge.TotalSeconds >= MaxLogAge)
            {
                _lastFlushed = DateTime.Now;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
