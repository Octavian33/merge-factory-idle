using UnityEngine;

public class OrderSystem : MonoBehaviour
{
    private const int OrderDurationSeconds = 8 * 60;

    public bool IsReadyToClaim => GameManager.Instance != null && GameManager.Instance.CanAfford(CurrentCost);
    public string Description => currentTitle;
    public int Progress => currentProgress;
    public int Target => currentTarget;
    public int TimeLeftSeconds => Mathf.Max(0, (int)(orderEndUnix - System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
    public double CurrentReward => rewardCoins;
    public ResourceCost CurrentCost => currentCost;
    public float Progress01 => currentTarget > 0 ? currentProgress / (float)currentTarget : 0f;

    private ResourceCost currentCost;
    private string currentTitle;
    private int currentProgress;
    private int currentTarget;
    private double rewardCoins;
    private long orderEndUnix;

    public void InitializeFromState(GameStateData state)
    {
        if (state == null || state.orderWoodRequired < 0)
        {
            GenerateNewOrder();
            return;
        }

        currentCost = new ResourceCost(state.orderWoodRequired, state.orderCoalRequired, state.orderIronRequired, state.orderCopperRequired);
        rewardCoins = state.orderRewardCoins > 0d ? state.orderRewardCoins : 60d;
        orderEndUnix = state.orderEndUnix > 0 ? state.orderEndUnix : System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() + OrderDurationSeconds;
        currentTitle = BuildTitle();
        RefreshProgress();

        if (TimeLeftSeconds <= 0)
        {
            GenerateNewOrder();
        }
    }

    public void Tick()
    {
        RefreshProgress();
        if (TimeLeftSeconds <= 0)
        {
            GenerateNewOrder();
            GameManager.Instance?.Hud?.ShowToast("New factory order");
        }
    }

    public void NotifyWorkerBought()
    {
    }

    public void NotifyMerged()
    {
    }

    public bool TryClaim()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.Economy == null || gm.State == null || !gm.CanAfford(currentCost))
        {
            return false;
        }

        gm.SpendResources(currentCost);
        gm.Economy.AddCoins(rewardCoins, false);
        gm.State.completedOrders++;
        gm.Hud?.ShowToast($"Order shipped +{gm.Economy.Format(rewardCoins)}");
        GenerateNewOrder();
        return true;
    }

    public void WriteToState(GameStateData state)
    {
        state.orderWoodRequired = currentCost.wood;
        state.orderCoalRequired = currentCost.coal;
        state.orderIronRequired = currentCost.iron;
        state.orderCopperRequired = currentCost.copper;
        state.orderRewardCoins = rewardCoins;
        state.orderEndUnix = orderEndUnix;
    }

    public string BuildRequirementSummary(System.Func<double, string> formatter)
    {
        return ChapterOneData.FormatResourceCost(currentCost, formatter);
    }

    private void GenerateNewOrder()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State == null)
        {
            currentCost = new ResourceCost(20, 0, 0, 0);
            rewardCoins = 50d;
            currentTitle = "Frontier Shipment";
            orderEndUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() + OrderDurationSeconds;
            RefreshProgress();
            return;
        }

        var level = Mathf.Clamp(gm.State.factoryLevel, 1, ChapterOneData.MaxFactoryLevel);
        var completed = gm.State.completedOrders;
        var variant = Mathf.Abs(level * 13 + completed * 7 + (int)(System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 11)) % 4;

        var wood = 0;
        var coal = 0;
        var iron = 0;
        var copper = 0;

        var tierBase = 16 + level * 6;
        wood = tierBase;

        if (level >= 3)
        {
            coal = 10 + level * 4;
        }
        if (level >= 5)
        {
            iron = 6 + level * 3;
        }
        if (level >= 7)
        {
            copper = 4 + level * 2;
        }

        switch (variant)
        {
            case 0:
                wood += Mathf.RoundToInt(level * 3.2f);
                break;
            case 1:
                coal += Mathf.RoundToInt(level * 3f);
                if (level >= 5) iron += Mathf.RoundToInt(level * 1.8f);
                break;
            case 2:
                if (level >= 5) iron += Mathf.RoundToInt(level * 3.5f);
                if (level >= 7) copper += Mathf.RoundToInt(level * 2.5f);
                break;
            case 3:
                wood += Mathf.RoundToInt(level * 1.5f);
                coal += Mathf.RoundToInt(level * 1.5f);
                if (level >= 5) iron += Mathf.RoundToInt(level * 1.5f);
                if (level >= 7) copper += Mathf.RoundToInt(level * 1.5f);
                break;
        }

        currentCost = new ResourceCost(wood, coal, iron, copper);
        currentTitle = BuildTitle();
        rewardCoins = CalculateReward(currentCost, level, completed);
        orderEndUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() + OrderDurationSeconds;
        RefreshProgress();
    }

    private void RefreshProgress()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State == null)
        {
            currentProgress = 0;
            currentTarget = 1;
            return;
        }

        currentTarget = currentCost.wood + currentCost.coal + currentCost.iron + currentCost.copper;
        currentProgress =
            Mathf.Min(gm.State.wood, currentCost.wood) +
            Mathf.Min(gm.State.coal, currentCost.coal) +
            Mathf.Min(gm.State.iron, currentCost.iron) +
            Mathf.Min(gm.State.copper, currentCost.copper);
    }

    private string BuildTitle()
    {
        if (currentCost.copper > 0)
        {
            return "Precision Contract";
        }
        if (currentCost.iron > 0)
        {
            return "Workshop Shipment";
        }
        if (currentCost.coal > 0)
        {
            return "Fuel Delivery";
        }
        return "Lumber Contract";
    }

    private double CalculateReward(ResourceCost cost, int factoryLevel, int completedOrders)
    {
        var weightedValue =
            cost.wood * 1.1d +
            cost.coal * 1.8d +
            cost.iron * 2.5d +
            cost.copper * 3.1d;

        return 30d + weightedValue * 0.85d + factoryLevel * 8d + completedOrders * 4d;
    }
}
