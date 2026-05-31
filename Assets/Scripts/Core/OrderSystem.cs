using UnityEngine;

public enum FactoryOrderType
{
    BuyWorkers = 0,
    MergeWorkers = 1,
    ReachWorkerLevel = 2
}

public class OrderSystem : MonoBehaviour
{
    private const int OrderDurationSeconds = 8 * 60;

    public bool IsReadyToClaim => progress >= target;
    public string Description => BuildDescription();
    public int Progress => progress;
    public int Target => target;
    public int TimeLeftSeconds => Mathf.Max(0, (int)(orderEndUnix - System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
    public double CurrentReward => CalculateReward();

    private FactoryOrderType currentType;
    private int progress;
    private int target;
    private long orderEndUnix;

    public void InitializeFromState(GameStateData state)
    {
        if (state.orderType < 0)
        {
            GenerateNewOrder();
            return;
        }

        currentType = (FactoryOrderType)state.orderType;
        target = Mathf.Max(1, state.orderTarget);
        progress = Mathf.Clamp(state.orderProgress, 0, target);
        orderEndUnix = state.orderStartUnix + OrderDurationSeconds;

        if (TimeLeftSeconds <= 0)
        {
            GenerateNewOrder();
        }
    }

    public void Tick()
    {
        if (TimeLeftSeconds <= 0)
        {
            GenerateNewOrder();
            var gm = GameManager.Instance;
            if (gm != null && gm.Hud != null)
            {
                gm.Hud.ShowToast("New Factory Order");
            }
        }
    }

    public void NotifyWorkerBought()
    {
        if (currentType == FactoryOrderType.BuyWorkers)
        {
            AddProgress(1);
        }
    }

    public void NotifyMerged()
    {
        if (currentType == FactoryOrderType.MergeWorkers)
        {
            AddProgress(1);
        }
    }

    public void NotifyHighestLevel(int highestLevel)
    {
        if (currentType != FactoryOrderType.ReachWorkerLevel)
        {
            return;
        }

        progress = Mathf.Clamp(highestLevel, 0, target);
    }

    public bool TryClaim()
    {
        if (!IsReadyToClaim)
        {
            return false;
        }

        var gm = GameManager.Instance;
        if (gm == null || gm.Economy == null || gm.State == null)
        {
            return false;
        }

        var reward = CalculateReward();
        gm.Economy.AddCoins(reward, false);
        gm.State.completedOrders++;
        if (gm.Hud != null)
        {
            gm.Hud.ShowToast($"Order Complete! +{gm.Economy.Format(reward)}");
        }
        GenerateNewOrder();
        return true;
    }

    public void WriteToState(GameStateData state)
    {
        state.orderType = (int)currentType;
        state.orderTarget = target;
        state.orderProgress = progress;
        state.orderStartUnix = orderEndUnix - OrderDurationSeconds;
    }

    private void AddProgress(int amount)
    {
        progress = Mathf.Clamp(progress + amount, 0, target);
    }

    private void GenerateNewOrder()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State == null || gm.WorkerBoard == null)
        {
            currentType = FactoryOrderType.BuyWorkers;
            target = 4;
            progress = 0;
            orderEndUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() + OrderDurationSeconds;
            return;
        }

        var s = gm.State;
        var rngSeed = s.completedOrders + s.prestigeLevel + gm.WorkerBoard.GetHighestLevel();
        var pick = Mathf.Abs((int)(System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() + rngSeed)) % 3;
        currentType = (FactoryOrderType)pick;

        if (currentType == FactoryOrderType.BuyWorkers)
        {
            target = 4 + s.completedOrders % 5;
            progress = 0;
        }
        else if (currentType == FactoryOrderType.MergeWorkers)
        {
            target = 3 + s.completedOrders % 4;
            progress = 0;
        }
        else
        {
            target = Mathf.Clamp(3 + s.completedOrders / 2, 3, 12);
            progress = Mathf.Min(gm.WorkerBoard.GetHighestLevel(), target);
        }

        orderEndUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() + OrderDurationSeconds;
    }

    private double CalculateReward()
    {
        var gm = GameManager.Instance;
        var completed = gm != null && gm.State != null ? gm.State.completedOrders : 0;
        var prestige = gm != null && gm.State != null ? gm.State.prestigeLevel : 0;
        var baseReward = 22 + target * 8 + completed * 4;
        var prestigeBoost = 1 + prestige * 0.08;
        return baseReward * prestigeBoost;
    }

    private string BuildDescription()
    {
        if (currentType == FactoryOrderType.BuyWorkers)
        {
            return "Buy Workers";
        }

        if (currentType == FactoryOrderType.MergeWorkers)
        {
            return "Merge Workers";
        }

        return "Reach Worker Level";
    }
}
