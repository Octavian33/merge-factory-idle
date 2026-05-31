using System;
using System.Collections.Generic;

[Serializable]
public class GameStateData
{
    public double coins = 50;
    public int prestigeLevel;
    public int buyUpgradeLevel;
    public int incomeUpgradeLevel;
    public long lastSaveUnix;
    public int orderType = -1;
    public int orderTarget;
    public int orderProgress;
    public long orderStartUnix;
    public int completedOrders;
    public List<WorkerSaveData> workers = new();
}

[Serializable]
public class WorkerSaveData
{
    public int slotIndex;
    public int level;
}
