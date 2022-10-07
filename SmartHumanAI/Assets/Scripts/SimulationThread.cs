using System.Threading;
using Assets.Scripts;
using Core.GateChoice;
using DataFormats;
using eDriven.Core.Signals;
using UnityEngine;

public class SimulationThread : ThreadedJob
{
    private readonly Core.Core _core = null;
    private Thread _simulationThread = null;

    private readonly Signal _cancelSignal  = new Signal();
    private readonly Signal _completeSignal = new Signal();

    public SimulationThread(Signal updateDataSignal)
    {
        _core = new Core.Core(updateDataSignal);
        _cancelSignal.Connect(_core.CancelRunningSimulation);
        _completeSignal.Connect(_core.ForceCompleteSimulation);
    }

    internal bool Initialize(Model m, SimulationParams sP)
    {
        return _core.Initialize(m, sP);
    }

    public void StopSimulation()
    {       
        _cancelSignal.Emit();        
    }

    public void ForceCompleteSimulation()
    {
        _completeSignal.Emit();
    }

    protected override void ThreadFunction()
    {
        _simulationThread = new Thread(_core.Execute);
        _simulationThread.Start();
        _simulationThread.Join();
    }

    protected override void OnFinished()
    {
        Debug.Log("Simulation Thread Ended.");
    }
}