using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameStateData State { get; private set; }
    public WorkerBoard WorkerBoard { get; private set; }
    public EconomySystem Economy { get; private set; }
    public OrderSystem Orders { get; private set; }
    public UIHud Hud { get; private set; }

    private float saveTimer;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        State = SaveSystem.Load();
    }

    public void Initialize(WorkerBoard board, EconomySystem economy, UIHud hud)
    {
        WorkerBoard = board;
        Economy = economy;
        Hud = hud;
        Orders = GetComponent<OrderSystem>();
        if (Orders == null)
        {
            Orders = gameObject.AddComponent<OrderSystem>();
        }

        WorkerBoard.RestoreWorkers(State.workers);
        Orders.InitializeFromState(State);
        Orders.NotifyHighestLevel(WorkerBoard.GetHighestLevel());
        var offline = Economy.ApplyOfflineEarnings(State);
        if (offline > 0)
        {
            Hud.ShowToast($"Offline +{Economy.Format(offline)}");
        }

        Hud.RefreshAll();
    }

    private void Update()
    {
        if (Economy == null || Orders == null || State == null)
        {
            return;
        }

        Economy.Tick(Time.deltaTime);
        Orders.Tick();
        saveTimer += Time.deltaTime;
        if (saveTimer >= 5f)
        {
            SaveNow();
            saveTimer = 0f;
        }
    }

    public void SaveNow()
    {
        if (State == null || WorkerBoard == null || Orders == null)
        {
            return;
        }

        State.workers = WorkerBoard.ExportWorkers();
        Orders.WriteToState(State);
        SaveSystem.Save(State);
    }

    public void NotifyWorkerBought()
    {
        if (Orders != null) Orders.NotifyWorkerBought();
        if (Hud != null) Hud.RefreshAll();
    }

    public void NotifyMerged(int newLevel)
    {
        if (Orders != null)
        {
            Orders.NotifyMerged();
            Orders.NotifyHighestLevel(newLevel);
        }
        if (Hud != null) Hud.NotifyMerge();
        if (Hud != null) Hud.RefreshAll();
    }

    public void ResetProgress()
    {
        State = new GameStateData();

        if (WorkerBoard == null || Orders == null)
        {
            SaveNow();
            return;
        }

        WorkerBoard.ClearBoard();
        WorkerBoard.RestoreWorkers(new List<WorkerSaveData>());
        Orders.InitializeFromState(State);
        Orders.NotifyHighestLevel(WorkerBoard.GetHighestLevel());

        if (Hud != null)
        {
            Hud.ShowToast("Progress reset");
            Hud.RefreshAll();
        }
        SaveNow();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) SaveNow();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveNow();
        }
    }

    private void OnApplicationQuit()
    {
        SaveNow();
    }
}
