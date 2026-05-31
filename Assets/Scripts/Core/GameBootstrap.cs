using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    private static bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        initialized = false;
    }

    private void Awake()
    {
        if (initialized)
        {
            Destroy(gameObject);
            return;
        }
        initialized = true;

        RemoveLegacyAmbientParticles();
        RemoveLegacyRuntimeObjects();
        SetupCamera();
        SetupBackground();

        var managerGO = new GameObject("GameManager");
        var gm = managerGO.AddComponent<GameManager>();
        managerGO.AddComponent<RuntimePerformanceSettings>();

        var boardGO = new GameObject("WorkerBoard");
        var board = boardGO.AddComponent<WorkerBoard>();
        board.BuildBoard(4, 4, 1.25f);

        var eco = managerGO.AddComponent<EconomySystem>();

        var hudGO = new GameObject("HUD");
        var hud = hudGO.AddComponent<UIHud>();
        hud.Build();

        gm.Initialize(board, eco, hud);
    }

    private void RemoveLegacyAmbientParticles()
    {
        var legacy = GameObject.Find("AmbientParticles");
        if (legacy != null)
        {
            Destroy(legacy);
        }

        var legacyMerge = GameObject.Find("MergeParticles");
        if (legacyMerge != null)
        {
            Destroy(legacyMerge);
        }
    }

    private void RemoveLegacyRuntimeObjects()
    {
        DestroyIfExists("HUD");
        DestroyIfExists("WorkerBoard");
        DestroyIfExists("Backdrop");
    }

    private void DestroyIfExists(string name)
    {
        var go = GameObject.Find(name);
        if (go != null)
        {
            Destroy(go);
        }
    }

    private void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            cam.tag = "MainCamera";
        }

        cam.orthographic = true;
        cam.orthographicSize = 5.2f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.backgroundColor = new Color(0.39f, 0.83f, 0.95f);
    }

    private void SetupBackground()
    {
        var bg = new GameObject("Backdrop");
        var sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = CreateGradientSprite();
        bg.transform.localScale = new Vector3(20, 20, 1);
        bg.transform.position = new Vector3(0, 0, 5);
    }

    private Sprite CreateGradientSprite()
    {
        const int width = 32;
        const int height = 128;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var cols = new Color[width * height];
        var top = new Color(0.55f, 0.89f, 0.98f);
        var bottom = new Color(0.74f, 0.96f, 0.94f);
        for (var y = 0; y < height; y++)
        {
            var t = y / (height - 1f);
            var rowColor = Color.Lerp(bottom, top, t);
            for (var x = 0; x < width; x++)
            {
                cols[y * width + x] = rowColor;
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 8f);
    }

}
