#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ProjectAutoSetupEditor
{
    private const string ScenePath = "Assets/Scenes/Main.unity";
    private const string PrefabPath = "Assets/Prefabs/WorkerPlaceholder.prefab";

    static ProjectAutoSetupEditor()
    {
        EditorApplication.delayCall += EnsureProjectAssets;
    }

    [MenuItem("Merge Factory Idle/Rebuild Scene and Prefab")]
    public static void EnsureProjectAssets()
    {
        EnsureWorkerPrefab();
        EnsureMainScene();
        AssetDatabase.SaveAssets();
    }

    private static void EnsureWorkerPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            return;
        }

        var go = new GameObject("WorkerPlaceholder");
        go.AddComponent<SpriteRenderer>();
        go.AddComponent<CircleCollider2D>();
        go.AddComponent<WorkerUnit>();
        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
    }

    private static void EnsureMainScene()
    {
        if (System.IO.File.Exists(ScenePath))
        {
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var bootstrap = new GameObject("GameBootstrap");
        bootstrap.AddComponent<GameBootstrap>();
        EditorSceneManager.SaveScene(scene, ScenePath);
    }
}
#endif
