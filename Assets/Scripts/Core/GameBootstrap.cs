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
        SetupBoardGrid();

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

    private void SetupBoardGrid()
    {
        var gridGo = new GameObject("BoardGrid");
        var sr = gridGo.AddComponent<SpriteRenderer>();
        sr.sprite = CreateBoardGridSprite();
        sr.color = new Color(1f, 1f, 1f, 0.22f);
        sr.sortingOrder = 2;
        gridGo.transform.localScale = new Vector3(7.4f, 7.4f, 1f);
        gridGo.transform.position = new Vector3(0f, 0.85f, 1f);
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

    private Sprite CreateBoardGridSprite()
    {
        const int size = 256;
        const int lines = 4;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var cols = new Color[size * size];
        var bg = new Color(0.04f, 0.12f, 0.2f, 0.08f);
        var line = new Color(0.12f, 0.28f, 0.4f, 0.48f);

        for (var i = 0; i < cols.Length; i++) cols[i] = bg;

        var step = size / lines;
        for (var l = 0; l <= lines; l++)
        {
            var x = Mathf.Clamp(l * step, 0, size - 1);
            var y = Mathf.Clamp(l * step, 0, size - 1);

            for (var t = -1; t <= 1; t++)
            {
                var xx = Mathf.Clamp(x + t, 0, size - 1);
                var yy = Mathf.Clamp(y + t, 0, size - 1);
                for (var p = 0; p < size; p++)
                {
                    cols[p * size + xx] = line;
                    cols[yy * size + p] = line;
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

}
