namespace Core
{
    internal enum ThreadSignal { EnableThreat, DisableThreat, GroupUpdate, NewGroup }

    /// <summary>
    /// Interface to be used when creating worker threads
    /// based on the input data
    /// </summary>
    internal interface IWorkerThread
    {
        void SetThreadParams(ThreadParams threadParams);
        void HandleUpdateSignal(object[] parameters);
        void Join();
    }
}