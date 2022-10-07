using Assets.Scripts;
using Core;
using DataFormats;
using UnityEngine;

public class SimulationTimeKeeper : MonoBehaviour
{
    public float SimTimeDelta = 10f;

    internal float previousTime = 0;
    internal long previousTimeReal = -1;

    internal bool repeat = true;
    internal bool isPaused = false;
    internal float time = 0f;
    internal float maxTime = float.MaxValue;

    private static SimulationTimeKeeper instance = null;
    private static readonly object padlock = new object();

    public static SimulationTimeKeeper Instance
    {
        get
        {
            lock (padlock)
            {
                return instance ?? (instance = FindObjectOfType<SimulationTimeKeeper>());
            }
        }
    }

    void Start()
    {
        instance = FindObjectOfType<SimulationTimeKeeper>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isPaused && SimulationController.Instance.SimState != (int)Def.SimulationState.Started)
        {
            time += Time.deltaTime * Consts.SpeedMultiplier;

            if (repeat && time > maxTime)
                time = 0;
        }

        if (SimulationController.Instance.SimState != (int)Def.SimulationState.Started)
        {
            previousTime = 0;
            previousTimeReal = -1;
        }
    }

    internal void Restart()
    {
        time = 0f;
        maxTime = SimulationController.Instance.FindMaxTime();
        previousTime = 0;
        previousTimeReal = -1;
    }

    internal void SetTime(TimePackage timePackage)
    {
        if (previousTimeReal == -1)
            previousTimeReal = System.DateTime.Now.Millisecond - 100;

        time = timePackage.cycleNum * Params.Current.TimeStep;

        if (time - previousTime >= 0.5)
        {
            SimTimeDelta = (time - previousTime) * (System.DateTime.Now.Ticks - previousTimeReal) / 10000;

            previousTime = time;
            previousTimeReal = System.DateTime.Now.Ticks;
        }
    }
}