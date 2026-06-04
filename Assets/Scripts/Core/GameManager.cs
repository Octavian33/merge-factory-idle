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
    public GameBootstrap Bootstrap { get; private set; }

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
        State.factoryLevel = Mathf.Clamp(State.factoryLevel, 1, ChapterOneData.MaxFactoryLevel);
    }

    public void Initialize(WorkerBoard board, EconomySystem economy, UIHud hud, GameBootstrap bootstrap)
    {
        WorkerBoard = board;
        Economy = economy;
        Hud = hud;
        Bootstrap = bootstrap;

        Orders = GetComponent<OrderSystem>();
        if (Orders == null)
        {
            Orders = gameObject.AddComponent<OrderSystem>();
        }

        WorkerBoard.RestoreWorkers(State.workers);
        Orders.InitializeFromState(State);

        var offline = Economy.ApplyOfflineEarnings(State);
        Bootstrap?.RefreshChapterOneVisuals(State.factoryLevel);
        if (offline.HasAny && Hud != null)
        {
            Hud.ShowToast(offline.BuildToast(Economy.Format));
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
        Orders?.NotifyWorkerBought();
        Hud?.RefreshAll();
    }

    public void NotifyMerged(int newLevel)
    {
        Orders?.NotifyMerged();
        Hud?.NotifyMerge();
        Hud?.RefreshAll();
    }

    public void ResetProgress()
    {
        State = new GameStateData();

        if (WorkerBoard != null)
        {
            WorkerBoard.ClearBoard();
            WorkerBoard.RestoreWorkers(new List<WorkerSaveData>());
        }

        Orders?.InitializeFromState(State);
        Bootstrap?.RefreshChapterOneVisuals(State.factoryLevel);

        if (Hud != null)
        {
            Hud.ShowToast("Progress reset");
            Hud.RefreshAll();
        }

        SaveNow();
    }

    public ResourceCost GetFactoryUpgradeCost()
    {
        return ChapterOneData.GetFactoryUpgradeCost(State != null ? State.factoryLevel : 1);
    }

    public string GetCurrentFactoryObjective()
    {
        return ChapterOneData.GetFactoryObjective(State != null ? State.factoryLevel : 1);
    }

    public string GetNextBuildingUnlockLabel()
    {
        return ChapterOneData.GetNextBuildingUnlockLabel(State != null ? State.factoryLevel : 1);
    }

    public bool IsBuildingUnlocked(ChapterOneBuildingType buildingType)
    {
        return ChapterOneData.IsBuildingUnlocked(State != null ? State.factoryLevel : 1, buildingType);
    }

    public bool CanAfford(ResourceCost cost)
    {
        if (State == null)
        {
            return false;
        }

        return State.wood >= cost.wood
            && State.coal >= cost.coal
            && State.iron >= cost.iron
            && State.copper >= cost.copper;
    }

    public void SpendResources(ResourceCost cost)
    {
        if (State == null)
        {
            return;
        }

        State.wood = Mathf.Max(0, State.wood - cost.wood);
        State.coal = Mathf.Max(0, State.coal - cost.coal);
        State.iron = Mathf.Max(0, State.iron - cost.iron);
        State.copper = Mathf.Max(0, State.copper - cost.copper);
    }

    public float GetFactoryProgressRatio()
    {
        var cost = GetFactoryUpgradeCost();
        if (cost.IsEmpty || State == null)
        {
            return 1f;
        }

        float total = 0f;
        var parts = 0;
        if (cost.wood > 0)
        {
            total += Mathf.Clamp01(State.wood / (float)cost.wood);
            parts++;
        }
        if (cost.coal > 0)
        {
            total += Mathf.Clamp01(State.coal / (float)cost.coal);
            parts++;
        }
        if (cost.iron > 0)
        {
            total += Mathf.Clamp01(State.iron / (float)cost.iron);
            parts++;
        }
        if (cost.copper > 0)
        {
            total += Mathf.Clamp01(State.copper / (float)cost.copper);
            parts++;
        }

        return parts > 0 ? total / parts : 1f;
    }

    public bool TryUpgradeFactory()
    {
        if (State == null || State.factoryLevel >= ChapterOneData.MaxFactoryLevel)
        {
            return false;
        }

        var cost = GetFactoryUpgradeCost();
        if (!CanAfford(cost))
        {
            return false;
        }

        SpendResources(cost);
        State.factoryLevel++;
        State.factoryBuildProgress = 0;
        Bootstrap?.RefreshChapterOneVisuals(State.factoryLevel);

        var unlockedBuilding = ChapterOneData.GetBuildingUnlockedAt(State.factoryLevel);
        if (Hud != null)
        {
            Hud.ShowToast(unlockedBuilding.HasValue
                ? $"{ChapterOneData.GetBuildingName(unlockedBuilding.Value)} unlocked"
                : $"Factory Lv {State.factoryLevel}");
            Hud.RefreshAll();
        }

        return true;
    }

    public bool CanAutomateChapterOne()
    {
        return State != null
            && !State.chapterOneAutomated
            && State.factoryLevel >= ChapterOneData.MaxFactoryLevel
            && CanAfford(ChapterOneData.GetAutomationCost());
    }

    public bool TryAutomateChapterOne()
    {
        if (State == null || State.chapterOneAutomated || State.factoryLevel < ChapterOneData.MaxFactoryLevel)
        {
            return false;
        }

        var cost = ChapterOneData.GetAutomationCost();
        if (!CanAfford(cost))
        {
            return false;
        }

        SpendResources(cost);
        State.chapterOneAutomated = true;
        Hud?.ShowToast("Frontier Workshop automated");
        Hud?.RefreshAll();
        return true;
    }

    public string FormatResourceCost(ResourceCost cost)
    {
        return Economy != null
            ? ChapterOneData.FormatResourceCost(cost, Economy.Format)
            : ChapterOneData.FormatResourceCost(cost, value => value.ToString("0"));
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            SaveNow();
        }
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
