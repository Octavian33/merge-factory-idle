using System;
using System.Collections.Generic;

[Serializable]
public class GameStateData
{
    public double coins = 50;
    public int wood;
    public int coal;
    public int iron;
    public int copper;
    public int factoryLevel = 1;
    public int factoryBuildProgress;
    public bool chapterOneAutomated;
    public long lastSaveUnix;
    public int orderWoodRequired = -1;
    public int orderCoalRequired;
    public int orderIronRequired;
    public int orderCopperRequired;
    public double orderRewardCoins;
    public long orderEndUnix;
    public int completedOrders;
    public List<WorkerSaveData> workers = new();
}

[Serializable]
public class WorkerSaveData
{
    public int slotIndex;
    public int level;
}
