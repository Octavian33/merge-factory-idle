using System;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private const string FileName = "merge_factory_idle_save.json";
    private const string TempSuffix = ".tmp";
    private const string BackupSuffix = ".bak";

    public static void Save(GameStateData data)
    {
        try
        {
            data.lastSaveUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var json = JsonUtility.ToJson(data, true);
            var path = GetPath();
            var tempPath = path + TempSuffix;
            var backupPath = path + BackupSuffix;

            File.WriteAllText(tempPath, json);

            if (File.Exists(path))
            {
                File.Copy(path, backupPath, true);
            }

            File.Copy(tempPath, path, true);
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Save failed: {ex.Message}");
        }
    }

    public static GameStateData Load()
    {
        var path = GetPath();
        if (!File.Exists(path))
        {
            return new GameStateData();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonUtility.FromJson<GameStateData>(json) ?? new GameStateData();
        }
        catch
        {
            var backupPath = path + BackupSuffix;
            if (File.Exists(backupPath))
            {
                try
                {
                    var backupJson = File.ReadAllText(backupPath);
                    return JsonUtility.FromJson<GameStateData>(backupJson) ?? new GameStateData();
                }
                catch
                {
                    return new GameStateData();
                }
            }
            return new GameStateData();
        }
    }

    private static string GetPath()
    {
        return Path.Combine(Application.persistentDataPath, FileName);
    }
}
