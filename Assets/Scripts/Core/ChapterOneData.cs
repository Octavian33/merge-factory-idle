using UnityEngine;

public enum ChapterOneBuildingType
{
    Sawmill = 0,
    CoalMine = 1,
    Storage = 2,
    IronWorkshop = 3,
    CopperWorkshop = 4
}

[System.Serializable]
public struct ResourceCost
{
    public int wood;
    public int coal;
    public int iron;
    public int copper;

    public ResourceCost(int wood, int coal, int iron, int copper)
    {
        this.wood = wood;
        this.coal = coal;
        this.iron = iron;
        this.copper = copper;
    }

    public bool IsEmpty => wood <= 0 && coal <= 0 && iron <= 0 && copper <= 0;
}

public static class ChapterOneData
{
    public const int MaxFactoryLevel = 20;

    private static readonly ResourceCost[] FactoryUpgradeCosts =
    {
        new ResourceCost(0, 0, 0, 0),
        new ResourceCost(60, 0, 0, 0),
        new ResourceCost(120, 0, 0, 0),
        new ResourceCost(180, 70, 0, 0),
        new ResourceCost(260, 140, 0, 0),
        new ResourceCost(320, 220, 80, 0),
        new ResourceCost(420, 300, 160, 0),
        new ResourceCost(520, 380, 220, 90),
        new ResourceCost(650, 460, 320, 180),
        new ResourceCost(780, 560, 420, 260),
        new ResourceCost(920, 700, 520, 340),
        new ResourceCost(1080, 820, 640, 430),
        new ResourceCost(1260, 980, 760, 540),
        new ResourceCost(1460, 1160, 900, 660),
        new ResourceCost(1680, 1360, 1060, 800),
        new ResourceCost(1920, 1580, 1240, 960),
        new ResourceCost(2180, 1820, 1440, 1140),
        new ResourceCost(2460, 2080, 1660, 1340),
        new ResourceCost(2780, 2360, 1900, 1560),
        new ResourceCost(3140, 2680, 2160, 1820)
    };

    private static readonly string[] FactoryObjectives =
    {
        "",
        "Start lumber production and build the frontier yard.",
        "Reinforce the workshop and prepare for fuel extraction.",
        "Open the coal supply line.",
        "Bring storage online for mixed deliveries.",
        "Expand into ironworking.",
        "Stabilize heavy production.",
        "Complete the full Chapter 1 production network.",
        "Balance all four resources.",
        "Push the first major bottleneck.",
        "Upgrade the frontier factory core.",
        "Expand logistics and stockpiles.",
        "Support advanced workshop output.",
        "Specialize for high-demand resources.",
        "Reinforce the industrial yard.",
        "Reach a high-capacity factory state.",
        "Prepare the region for automation.",
        "Build deep stockpiles for regional autonomy.",
        "Complete the final industrial contracts.",
        "Finish the frontier expansion plan.",
        "Automate the Frontier Workshop region."
    };

    public static ResourceCost GetFactoryUpgradeCost(int currentFactoryLevel)
    {
        var level = Mathf.Clamp(currentFactoryLevel, 1, MaxFactoryLevel);
        return level >= MaxFactoryLevel ? default : FactoryUpgradeCosts[level];
    }

    public static string GetFactoryObjective(int factoryLevel)
    {
        return FactoryObjectives[Mathf.Clamp(factoryLevel, 1, MaxFactoryLevel)];
    }

    public static ChapterOneBuildingType? GetBuildingUnlockedAt(int factoryLevel)
    {
        return factoryLevel switch
        {
            1 => ChapterOneBuildingType.Sawmill,
            3 => ChapterOneBuildingType.CoalMine,
            4 => ChapterOneBuildingType.Storage,
            5 => ChapterOneBuildingType.IronWorkshop,
            7 => ChapterOneBuildingType.CopperWorkshop,
            _ => null
        };
    }

    public static bool IsBuildingUnlocked(int factoryLevel, ChapterOneBuildingType buildingType)
    {
        var level = Mathf.Max(1, factoryLevel);
        return buildingType switch
        {
            ChapterOneBuildingType.Sawmill => level >= 1,
            ChapterOneBuildingType.CoalMine => level >= 3,
            ChapterOneBuildingType.Storage => level >= 4,
            ChapterOneBuildingType.IronWorkshop => level >= 5,
            ChapterOneBuildingType.CopperWorkshop => level >= 7,
            _ => false
        };
    }

    public static bool ProducesResource(ChapterOneBuildingType buildingType)
    {
        return buildingType != ChapterOneBuildingType.Storage;
    }

    public static string GetBuildingName(ChapterOneBuildingType buildingType)
    {
        return buildingType switch
        {
            ChapterOneBuildingType.Sawmill => "Sawmill",
            ChapterOneBuildingType.CoalMine => "Coal Mine",
            ChapterOneBuildingType.Storage => "Storage",
            ChapterOneBuildingType.IronWorkshop => "Iron Workshop",
            ChapterOneBuildingType.CopperWorkshop => "Copper Workshop",
            _ => "Unknown"
        };
    }

    public static string GetNextBuildingUnlockLabel(int factoryLevel)
    {
        for (var level = Mathf.Max(1, factoryLevel) + 1; level <= MaxFactoryLevel; level++)
        {
            var unlocked = GetBuildingUnlockedAt(level);
            if (unlocked.HasValue)
            {
                return $"{GetBuildingName(unlocked.Value)} at Lv {level}";
            }
        }

        return "All Chapter 1 buildings unlocked";
    }

    public static float GetBuildingShare(int factoryLevel, int resourceType)
    {
        var hasCoal = IsBuildingUnlocked(factoryLevel, ChapterOneBuildingType.CoalMine);
        var hasIron = IsBuildingUnlocked(factoryLevel, ChapterOneBuildingType.IronWorkshop);
        var hasCopper = IsBuildingUnlocked(factoryLevel, ChapterOneBuildingType.CopperWorkshop);

        if (!hasCoal)
        {
            return resourceType == 0 ? 1f : 0f;
        }

        if (!hasIron)
        {
            return resourceType switch
            {
                0 => 0.62f,
                1 => 0.38f,
                _ => 0f
            };
        }

        if (!hasCopper)
        {
            return resourceType switch
            {
                0 => 0.42f,
                1 => 0.33f,
                2 => 0.25f,
                _ => 0f
            };
        }

        return resourceType switch
        {
            0 => 0.30f,
            1 => 0.26f,
            2 => 0.24f,
            3 => 0.20f,
            _ => 0f
        };
    }

    public static float GetStorageBonusMultiplier(int factoryLevel)
    {
        if (!IsBuildingUnlocked(factoryLevel, ChapterOneBuildingType.Storage))
        {
            return 1f;
        }

        return 1.12f + Mathf.Clamp(factoryLevel - 4, 0, 16) * 0.008f;
    }

    public static ResourceCost GetAutomationCost()
    {
        return new ResourceCost(1800, 1500, 1200, 900);
    }

    public static string FormatResourceCost(ResourceCost cost, System.Func<double, string> formatter)
    {
        if (formatter == null || cost.IsEmpty)
        {
            return "None";
        }

        var parts = new System.Collections.Generic.List<string>(4);
        if (cost.wood > 0) parts.Add($"Wood {formatter(cost.wood)}");
        if (cost.coal > 0) parts.Add($"Coal {formatter(cost.coal)}");
        if (cost.iron > 0) parts.Add($"Iron {formatter(cost.iron)}");
        if (cost.copper > 0) parts.Add($"Copper {formatter(cost.copper)}");
        return string.Join("  ", parts);
    }
}
