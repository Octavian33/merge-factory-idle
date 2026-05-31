public static class WorkerBalance
{
    public static double GetIncomeForLevel(int level)
    {
        return 0.5d * System.Math.Pow(1.60d, level - 1);
    }
}
