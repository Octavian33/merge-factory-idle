using UnityEngine;

public readonly struct OfflineEarningsResult
{
    public readonly double Coins;
    public readonly int Wood;
    public readonly int Coal;
    public readonly int Iron;
    public readonly int Copper;

    public OfflineEarningsResult(double coins, int wood, int coal, int iron, int copper)
    {
        Coins = coins;
        Wood = wood;
        Coal = coal;
        Iron = iron;
        Copper = copper;
    }

    public bool HasAny => Coins >= 1d || Wood > 0 || Coal > 0 || Iron > 0 || Copper > 0;

    public string BuildToast(System.Func<double, string> formatter)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (Coins >= 1d) parts.Add($"+{formatter(Coins)} coins");
        if (Wood > 0) parts.Add($"+{Wood} wood");
        if (Coal > 0) parts.Add($"+{Coal} coal");
        if (Iron > 0) parts.Add($"+{Iron} iron");
        if (Copper > 0) parts.Add($"+{Copper} copper");
        return "Offline " + string.Join("  ", parts);
    }
}

public class EconomySystem : MonoBehaviour
{
    private const double BaseBuyCost = 28d;
    private const double BuyGrowth = 1.21d;

    private const double WoodRate = 0.24d;
    private const double CoalRate = 0.16d;
    private const double IronRate = 0.11d;
    private const double CopperRate = 0.085d;

    private readonly double[] resourceRemainders = new double[4];

    public double CurrentIncomePerSecond => CalculateCoinIncomePerSecond();

    public void Tick(float deltaTime)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.WorkerBoard == null || gm.State == null)
        {
            return;
        }

        AddCoins(CurrentIncomePerSecond * deltaTime, false);
        ProduceResources(deltaTime, showPopups: false);
    }

    public double GetBuyCost()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.WorkerBoard == null)
        {
            return BaseBuyCost;
        }

        return BaseBuyCost * System.Math.Pow(BuyGrowth, gm.WorkerBoard.WorkerCount);
    }

    public bool TryBuyWorker()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State == null || gm.WorkerBoard == null)
        {
            return false;
        }

        var cost = GetBuyCost();
        if (gm.State.coins < cost || !gm.WorkerBoard.HasEmptySlot())
        {
            return false;
        }

        AddCoins(-cost, false);
        gm.WorkerBoard.SpawnAtFirstEmpty(1);
        gm.NotifyWorkerBought();
        return true;
    }

    public OfflineEarningsResult ApplyOfflineEarnings(GameStateData state)
    {
        if (state == null || state.lastSaveUnix <= 0)
        {
            return default;
        }

        var now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var seconds = (int)Mathf.Clamp(now - state.lastSaveUnix, 0, 8 * 60 * 60);
        if (seconds <= 0)
        {
            return default;
        }

        var coins = CurrentIncomePerSecond * seconds;
        AddCoins(coins, false);

        var wood = ProduceOfflineResource(0, seconds);
        var coal = ProduceOfflineResource(1, seconds);
        var iron = ProduceOfflineResource(2, seconds);
        var copper = ProduceOfflineResource(3, seconds);

        return new OfflineEarningsResult(coins, wood, coal, iron, copper);
    }

    public void AddCoins(double amount, bool popText, Vector3 worldPos = default)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State == null)
        {
            return;
        }

        gm.State.coins = System.Math.Max(0d, gm.State.coins + amount);
        if (popText && amount > 0d && gm.Hud != null)
        {
            gm.Hud.SpawnFloatingCoin(worldPos, "+" + Format(amount));
        }
        if (gm.Hud != null)
        {
            gm.Hud.RefreshAll();
        }
    }

    public void AddResource(int materialType, int amount, bool showPopup = false)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State == null || amount <= 0)
        {
            return;
        }

        switch (Mathf.Clamp(materialType, 0, 3))
        {
            case 0:
                gm.State.wood += amount;
                break;
            case 1:
                gm.State.coal += amount;
                break;
            case 2:
                gm.State.iron += amount;
                break;
            case 3:
                gm.State.copper += amount;
                break;
        }

        if (showPopup && gm.Hud != null)
        {
            gm.Hud.SpawnFloatingCoin(Vector3.zero, $"+{amount} {GetResourceName(materialType)}");
        }
    }

    public double GetResourcePerSecond(int materialType)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State == null)
        {
            return 0d;
        }

        if (!ChapterOneData.IsBuildingUnlocked(gm.State.factoryLevel, (ChapterOneBuildingType)materialType))
        {
            return 0d;
        }

        var totalPower = GetTotalWorkerEfficiency();
        var share = ChapterOneData.GetBuildingShare(gm.State.factoryLevel, materialType);
        if (share <= 0f)
        {
            return 0d;
        }

        var storageBonus = ChapterOneData.GetStorageBonusMultiplier(gm.State.factoryLevel);
        var factoryBonus = 1d + Mathf.Max(0, gm.State.factoryLevel - 1) * 0.02d;
        return totalPower * share * GetBaseRate(materialType) * storageBonus * factoryBonus;
    }

    public string Format(double value)
    {
        if (value >= 1_000_000d) return (value / 1_000_000d).ToString("0.0") + "M";
        if (value >= 1_000d) return (value / 1_000d).ToString("0.0") + "K";
        return value.ToString("0");
    }

    private double CalculateCoinIncomePerSecond()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State == null)
        {
            return 0d;
        }

        var totalPower = GetTotalWorkerEfficiency();
        var factoryBonus = 1d + Mathf.Max(0, gm.State.factoryLevel - 1) * 0.03d;
        var automationBonus = gm.State.chapterOneAutomated ? 1.15d : 1d;
        return totalPower * 0.9d * factoryBonus * automationBonus;
    }

    private double GetTotalWorkerEfficiency()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.WorkerBoard == null)
        {
            return 0d;
        }

        double total = 0d;
        for (var i = 0; i < gm.WorkerBoard.Workers.Count; i++)
        {
            total += WorkerBalance.GetEfficiencyForLevel(gm.WorkerBoard.Workers[i].Level);
        }

        return total;
    }

    private void ProduceResources(float deltaTime, bool showPopups)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State == null)
        {
            return;
        }

        for (var materialType = 0; materialType < 4; materialType++)
        {
            var perSecond = GetResourcePerSecond(materialType);
            if (perSecond <= 0d)
            {
                continue;
            }

            resourceRemainders[materialType] += perSecond * deltaTime;
            var wholeAmount = Mathf.FloorToInt((float)resourceRemainders[materialType]);
            if (wholeAmount <= 0)
            {
                continue;
            }

            resourceRemainders[materialType] -= wholeAmount;
            AddResource(materialType, wholeAmount, showPopups);
        }

        gm.Hud?.RefreshAll();
    }

    private int ProduceOfflineResource(int materialType, int seconds)
    {
        var produced = GetResourcePerSecond(materialType) * seconds;
        var whole = Mathf.FloorToInt((float)produced);
        if (whole > 0)
        {
            AddResource(materialType, whole, false);
        }
        return whole;
    }

    private double GetBaseRate(int materialType)
    {
        return materialType switch
        {
            0 => WoodRate,
            1 => CoalRate,
            2 => IronRate,
            _ => CopperRate
        };
    }

    private string GetResourceName(int materialType)
    {
        return materialType switch
        {
            0 => "wood",
            1 => "coal",
            2 => "iron",
            _ => "copper"
        };
    }
}
