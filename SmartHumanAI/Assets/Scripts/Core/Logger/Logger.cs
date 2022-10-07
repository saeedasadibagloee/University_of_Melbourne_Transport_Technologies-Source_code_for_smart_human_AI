using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Core.Logger
{
    /// <summary>
    /// A Logging class implementing the Singleton pattern and an internal Queue to be flushed perdiodically
    /// </summary>
    public class LogWriter
    {
        private const bool enabled = false;

        public static string logDir = "";
        private static LogWriter _instance;
        private static Queue<Log> _logQueue;
        private const string LogFile = "log.txt";
        private const int MaxLogAge = 1;
        private const int QueueSize = 1;
        private static DateTime _lastFlushed = DateTime.Now;

        /// <summary>
        /// Private constructor to prevent instance creation
        /// </summary>
        private LogWriter() { }

        /// <summary>
        /// An LogWriter instance that exposes a single instance
        /// </summary>
        public static LogWriter Instance
        {
            get
            {
                // If the instance is null then create one and init the Queue
                if (_instance == null)
                {
                    _instance = new LogWriter();
                    _logQueue = new Queue<Log>();
                }
                return _instance;
            }
        }

        /// <summary>
        /// The single instance method that writes to the log file
        /// </summary>
        /// <param name="message">The message to write to the log</param>
        public void WriteToLog(string message)
        {
            // Lock the queue while writing to prevent contention for the log file
            lock (_logQueue)
            {
                // Create the entry and push to the Queue
                Log logEntry = new Log(message);
                _logQueue.Enqueue(logEntry);

                // If we have reached the Queue Size then flush the Queue
                if (_logQueue.Count >= QueueSize || DoPeriodicFlush())
                {
                    FlushLog();
                }
            }
        }

        private bool DoPeriodicFlush()
        {
            TimeSpan logAge = DateTime.Now - _lastFlushed;
            if (!(logAge.TotalSeconds >= MaxLogAge)) return false;
            _lastFlushed = DateTime.Now;
            return true;
        }

        /// <summary>
        /// Flushes the Queue to the physical log file
        /// </summary>
        private static void FlushLog()
        {
            while (enabled && _logQueue.Count > 0)
            {
                Log entry = _logQueue.Dequeue();

                string logPath = logDir + "Log/";
                Directory.CreateDirectory(logPath);

                logPath += entry.LogDate + "_" + LogFile;

                // This could be optimised to prevent opening and closing the file for each write
                using (FileStream fs = File.Open(logPath, FileMode.Append, FileAccess.Write))
                {
                    using (StreamWriter log = new StreamWriter(fs))
                    {
                        log.WriteLine("{0}\t{1}", entry.LogTime, entry.Message);
                    }
                }
            }
        }
    }

    /// <summary>
    /// A Log class to store the message and the Date and Time the log entry was created
    /// </summary>
    public class Log
    {
        public string Message { get; set; }
        public string LogTime { get; set; }
        public string LogDate { get; set; }

        public Log(string message)
        {
            Message = message;
            LogDate = DateTime.Now.ToString("yyyy-MM-dd");
            LogTime = DateTime.Now.ToString("hh:mm:ss.fff tt");
        }
    }
}