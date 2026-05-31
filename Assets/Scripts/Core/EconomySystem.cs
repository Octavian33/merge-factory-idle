using UnityEngine;

public class EconomySystem : MonoBehaviour
{
    private const double BaseBuyCost = 36;
    private const double BuyGrowth = 1.24;

    public double CurrentIncomePerSecond => CalculateIncomePerSecond();

    public void Tick(float deltaTime)
    {
        if (GameManager.Instance == null || GameManager.Instance.WorkerBoard == null || GameManager.Instance.State == null)
        {
            return;
        }
        var income = CurrentIncomePerSecond * deltaTime;
        AddCoins(income, false);
    }

    public double GetBuyCost()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.WorkerBoard == null || gm.State == null)
        {
            return BaseBuyCost;
        }
        var rawCost = BaseBuyCost * System.Math.Pow(BuyGrowth, gm.WorkerBoard.WorkerCount);
        var discountFactor = 1d - gm.State.buyUpgradeLevel * 0.035d;
        return rawCost * System.Math.Max(0.65d, discountFactor);
    }

    public double GetIncomeUpgradeCost()
    {
        var gm = GameManager.Instance;
        var level = gm != null && gm.State != null ? gm.State.incomeUpgradeLevel : 0;
        return 150 * System.Math.Pow(1.82, level);
    }

    public double GetDiscountUpgradeCost()
    {
        var gm = GameManager.Instance;
        var level = gm != null && gm.State != null ? gm.State.buyUpgradeLevel : 0;
        return 125 * System.Math.Pow(1.78, level);
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

    public bool TryUpgradeIncome()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State == null) return false;
        return TryBuyUpgrade(
            gm.State.incomeUpgradeLevel,
            _ => GetIncomeUpgradeCost(),
            () => gm.State.incomeUpgradeLevel++);
    }

    public bool TryUpgradeBuyDiscount()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State == null) return false;
        return TryBuyUpgrade(
            gm.State.buyUpgradeLevel,
            _ => GetDiscountUpgradeCost(),
            () => gm.State.buyUpgradeLevel++);
    }

    public bool TryPrestige()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.WorkerBoard == null || gm.State == null)
        {
            return false;
        }

        if (gm.WorkerBoard.GetHighestLevel() < 8)
        {
            return false;
        }

        gm.State.prestigeLevel++;
        gm.State.coins = 50;
        gm.State.buyUpgradeLevel = 0;
        gm.State.incomeUpgradeLevel = 0;
        gm.WorkerBoard.ClearBoard();
        gm.WorkerBoard.SpawnAtFirstEmpty(1);
        if (gm.Hud != null) gm.Hud.ShowToast("Prestige! Global income boosted.");
        return true;
    }

    public double ApplyOfflineEarnings(GameStateData state)
    {
        if (state.lastSaveUnix <= 0)
        {
            return 0;
        }

        var now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var seconds = Mathf.Clamp(now - state.lastSaveUnix, 0, 8 * 60 * 60);
        var earned = CurrentIncomePerSecond * seconds;
        AddCoins(earned, false);
        return earned;
    }

    public void AddCoins(double amount, bool popText, Vector3 worldPos = default)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State == null)
        {
            return;
        }

        gm.State.coins = System.Math.Max(0, gm.State.coins + amount);
        if (popText && amount > 0 && gm.Hud != null)
        {
            gm.Hud.SpawnFloatingCoin(worldPos, "+" + Format(amount));
        }
        if (amount > 0 && gm.Hud != null)
        {
            gm.Hud.PulseCoins();
        }
        if (gm.Hud != null) gm.Hud.RefreshAll();
    }

    public string Format(double value)
    {
        if (value >= 1_000_000) return (value / 1_000_000d).ToString("0.0") + "M";
        if (value >= 1_000) return (value / 1_000d).ToString("0.0") + "K";
        return value.ToString("0");
    }

    private double CalculateIncomePerSecond()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.WorkerBoard == null || gm.State == null)
        {
            return 0;
        }

        var board = gm.WorkerBoard;
        double income = 0;
        for (var i = 0; i < board.Workers.Count; i++)
        {
            income += WorkerBalance.GetIncomeForLevel(board.Workers[i].Level);
        }

        var incomeUpgrade = 1 + gm.State.incomeUpgradeLevel * 0.09;
        var prestige = 1 + gm.State.prestigeLevel * 0.4;
        return income * incomeUpgrade * prestige;
    }

    private bool TryBuyUpgrade(int level, System.Func<int, double> costFunc, System.Action apply)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State == null || gm.Hud == null)
        {
            return false;
        }

        var cost = costFunc(level);
        if (gm.State.coins < cost) return false;

        AddCoins(-cost, false);
        apply();
        gm.Hud.RefreshAll();
        return true;
    }
}
