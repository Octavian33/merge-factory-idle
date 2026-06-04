using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    public static GameBootstrap Active { get; private set; }

    private static bool initialized;
    private readonly GameObject[] chapterBuildingRoots = new GameObject[5];

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
        Active = this;

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
        SetupChapterBuildings();

        var eco = managerGO.AddComponent<EconomySystem>();

        var hudGO = new GameObject("HUD");
        var hud = hudGO.AddComponent<UIHud>();
        hud.Build();

        gm.Initialize(board, eco, hud, this);
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
        DestroyIfExists("BackdropGlow");
        DestroyIfExists("BackdropHorizon");
        DestroyIfExists("GroundPlane");
        DestroyIfExists("Sawmill");
        DestroyIfExists("CoalMine");
        DestroyIfExists("Storage");
        DestroyIfExists("IronWorkshop");
        DestroyIfExists("CopperWorkshop");
        DestroyIfExists("Factory");
        DestroyIfExists("BoardGrid");
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
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 50f;

        if (cam.GetComponent<AudioListener>() == null)
        {
            cam.gameObject.AddComponent<AudioListener>();
        }
    }

    private void SetupBackground()
    {
        var bg = new GameObject("Backdrop");
        var sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = CreateGradientSprite();
        bg.transform.localScale = new Vector3(20, 20, 1);
        bg.transform.position = new Vector3(0, 0, 5);

        var glow = new GameObject("BackdropGlow");
        glow.transform.position = new Vector3(0f, 1.5f, 4f);
        var glowSr = glow.AddComponent<SpriteRenderer>();
        glowSr.sprite = CreateSoftCircleSprite();
        glowSr.color = new Color(1f, 0.97f, 0.84f, 0.08f);
        glowSr.sortingOrder = -6;
        glow.transform.localScale = new Vector3(9.5f, 6f, 1f);

        var haze = new GameObject("BackdropHorizon");
        haze.transform.position = new Vector3(0f, -0.42f, 4.5f);
        var hazeSr = haze.AddComponent<SpriteRenderer>();
        hazeSr.sprite = CreateSoftCircleSprite();
        hazeSr.color = new Color(0.73f, 0.86f, 0.64f, 0.36f);
        hazeSr.sortingOrder = -5;
        haze.transform.localScale = new Vector3(12.2f, 4.5f, 1f);
    }

    public void RefreshChapterOneVisuals(int factoryLevel)
    {
        for (var i = 0; i < chapterBuildingRoots.Length; i++)
        {
            if (chapterBuildingRoots[i] == null)
            {
                continue;
            }

            var buildingType = (ChapterOneBuildingType)i;
            chapterBuildingRoots[i].SetActive(ChapterOneData.IsBuildingUnlocked(factoryLevel, buildingType));
        }
    }

    private void SetupChapterBuildings()
    {
        chapterBuildingRoots[(int)ChapterOneBuildingType.Sawmill] = SetupSawmill();
        chapterBuildingRoots[(int)ChapterOneBuildingType.CoalMine] = SetupCoalMine();
        chapterBuildingRoots[(int)ChapterOneBuildingType.Storage] = SetupStorage();
        chapterBuildingRoots[(int)ChapterOneBuildingType.IronWorkshop] = SetupIronWorkshop();
        chapterBuildingRoots[(int)ChapterOneBuildingType.CopperWorkshop] = SetupCopperWorkshop();
        RefreshChapterOneVisuals(GameManager.Instance != null && GameManager.Instance.State != null ? GameManager.Instance.State.factoryLevel : 1);
    }

    private GameObject SetupSawmill()
    {
        var sawmill = new GameObject("Sawmill");
        var importedWorkshop = LoadBuildingSprite("Buildings/Workshop", 420f);

        var shadow = new GameObject("Shadow");
        shadow.transform.SetParent(sawmill.transform, false);
        var shadowSr = shadow.AddComponent<SpriteRenderer>();
        shadowSr.sprite = CreateSoftCircleSprite();
        shadowSr.color = new Color(0f, 0f, 0f, 0.22f);
        shadowSr.sortingOrder = 4;
        shadow.transform.localPosition = new Vector3(0.04f, -0.12f, 0f);
        shadow.transform.localScale = new Vector3(1.3f, 0.42f, 1f);

        var basePatch = new GameObject("BasePatch");
        basePatch.transform.SetParent(sawmill.transform, false);
        var basePatchSr = basePatch.AddComponent<SpriteRenderer>();
        basePatchSr.sprite = CreateSoftCircleSprite();
        basePatchSr.color = new Color(0.58f, 0.5f, 0.32f, 0.3f);
        basePatchSr.sortingOrder = 5;
        basePatch.transform.localPosition = new Vector3(0f, -0.16f, 0f);
        basePatch.transform.localScale = new Vector3(1.5f, 0.55f, 1f);

        var sr = sawmill.AddComponent<SpriteRenderer>();
        sr.sprite = importedWorkshop ?? CreateSawmillSprite();
        sr.color = importedWorkshop != null ? new Color(0.95f, 0.93f, 0.9f, 1f) : Color.white;
        sr.sortingOrder = 6;
        sawmill.transform.position = new Vector3(-1.82f, -1.82f, 0f);
        sawmill.transform.localScale = importedWorkshop != null ? new Vector3(0.64f, 0.64f, 1f) : new Vector3(1.35f, 1.35f, 1f);

        if (importedWorkshop == null)
        {
            var pile = new GameObject("LogPile");
            pile.transform.SetParent(sawmill.transform, false);
            pile.transform.localPosition = new Vector3(0.76f, -0.02f, -0.02f);
            var pileSr = pile.AddComponent<SpriteRenderer>();
            pileSr.sprite = CreateLogPileSprite();
            pileSr.sortingOrder = 5;
            pile.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
        }

        return sawmill;
    }

    private GameObject SetupStorage()
    {
        var factory = new GameObject("Storage");
        var importedStorage = LoadBuildingSprite("Buildings/Storage", 420f);

        var shadow = new GameObject("Shadow");
        shadow.transform.SetParent(factory.transform, false);
        shadow.transform.localPosition = new Vector3(-0.08f, -0.08f, 0f);
        var shadowSr = shadow.AddComponent<SpriteRenderer>();
        shadowSr.sprite = CreateSoftCircleSprite();
        shadowSr.color = new Color(0f, 0f, 0f, 0.22f);
        shadowSr.sortingOrder = 4;
        shadow.transform.localScale = new Vector3(1.85f, 0.56f, 1f);

        var basePatch = new GameObject("BasePatch");
        basePatch.transform.SetParent(factory.transform, false);
        var basePatchSr = basePatch.AddComponent<SpriteRenderer>();
        basePatchSr.sprite = CreateSoftCircleSprite();
        basePatchSr.color = new Color(0.56f, 0.49f, 0.34f, 0.28f);
        basePatchSr.sortingOrder = 5;
        basePatch.transform.localPosition = new Vector3(0f, -0.16f, 0f);
        basePatch.transform.localScale = new Vector3(1.65f, 0.5f, 1f);

        var body = new GameObject("Body");
        body.transform.SetParent(factory.transform, false);
        var bodySr = body.AddComponent<SpriteRenderer>();
        bodySr.sprite = importedStorage ?? CreateFactorySprite();
        bodySr.color = importedStorage != null ? new Color(0.94f, 0.95f, 0.96f, 1f) : Color.white;
        bodySr.sortingOrder = 6;
        body.transform.localScale = importedStorage != null ? new Vector3(0.72f, 0.72f, 1f) : new Vector3(1.8f, 1.8f, 1f);

        if (importedStorage == null)
        {
            var conveyor = new GameObject("Conveyor");
            conveyor.transform.SetParent(factory.transform, false);
            conveyor.transform.localPosition = new Vector3(-1.5f, -0.3f, -0.02f);
            var convSr = conveyor.AddComponent<SpriteRenderer>();
            convSr.sprite = CreateConveyorSprite();
            convSr.sortingOrder = 5;
            conveyor.transform.localScale = new Vector3(1.4f, 0.7f, 1f);
        }

        factory.transform.position = new Vector3(0f, -2.12f, 0f);
        return factory;
    }

    private GameObject SetupCoalMine()
    {
        return SetupWorkshopBuilding("CoalMine", new Vector3(-2.18f, 0.15f, 0f), new Color(0.33f, 0.31f, 0.28f, 1f), new Color(0.76f, 0.58f, 0.25f, 1f), new Color(0.12f, 0.12f, 0.13f, 1f), true);
    }

    private GameObject SetupIronWorkshop()
    {
        return SetupWorkshopBuilding("IronWorkshop", new Vector3(2.1f, 0.18f, 0f), new Color(0.57f, 0.66f, 0.74f, 1f), new Color(0.72f, 0.55f, 0.28f, 1f), new Color(0.65f, 0.8f, 0.95f, 1f), false);
    }

    private GameObject SetupCopperWorkshop()
    {
        return SetupWorkshopBuilding("CopperWorkshop", new Vector3(2.16f, -1.58f, 0f), new Color(0.64f, 0.45f, 0.26f, 1f), new Color(0.84f, 0.58f, 0.22f, 1f), new Color(0.95f, 0.65f, 0.35f, 1f), false);
    }

    private GameObject SetupWorkshopBuilding(string name, Vector3 position, Color wallColor, Color roofColor, Color accentColor, bool includeCart)
    {
        var root = new GameObject(name);

        var shadow = new GameObject("Shadow");
        shadow.transform.SetParent(root.transform, false);
        var shadowSr = shadow.AddComponent<SpriteRenderer>();
        shadowSr.sprite = CreateSoftCircleSprite();
        shadowSr.color = new Color(0f, 0f, 0f, 0.2f);
        shadowSr.sortingOrder = 4;
        shadow.transform.localPosition = new Vector3(0.04f, -0.12f, 0f);
        shadow.transform.localScale = new Vector3(1.35f, 0.44f, 1f);

        var basePatch = new GameObject("BasePatch");
        basePatch.transform.SetParent(root.transform, false);
        var patchSr = basePatch.AddComponent<SpriteRenderer>();
        patchSr.sprite = CreateSoftCircleSprite();
        patchSr.color = new Color(0.58f, 0.5f, 0.32f, 0.3f);
        patchSr.sortingOrder = 5;
        basePatch.transform.localPosition = new Vector3(0f, -0.16f, 0f);
        basePatch.transform.localScale = new Vector3(1.45f, 0.52f, 1f);

        var sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = CreateWorkshopBuildingSprite(wallColor, roofColor, accentColor, includeCart);
        sr.sortingOrder = 6;
        root.transform.position = position;
        root.transform.localScale = new Vector3(1.34f, 1.34f, 1f);
        return root;
    }

    private void SetupBoardGrid()
    {
        var boardRoot = new GameObject("BoardGrid");

        var topGo = new GameObject("BoardTop");
        topGo.transform.SetParent(boardRoot.transform, false);
        var topSr = topGo.AddComponent<SpriteRenderer>();
        topSr.sprite = CreateMeadowSprite();
        topSr.sortingOrder = 2;
        topGo.transform.localScale = new Vector3(1.06f, 1.06f, 1f);
        topGo.transform.position = new Vector3(0f, 0f, 0f);

        var lowerShadeGo = new GameObject("BoardLowerShade");
        lowerShadeGo.transform.SetParent(boardRoot.transform, false);
        var lowerShadeSr = lowerShadeGo.AddComponent<SpriteRenderer>();
        lowerShadeSr.sprite = CreateSoftCircleSprite();
        lowerShadeSr.color = new Color(0f, 0f, 0f, 0.07f);
        lowerShadeSr.sortingOrder = 3;
        lowerShadeGo.transform.localScale = new Vector3(7.4f, 1.4f, 1f);
        lowerShadeGo.transform.position = new Vector3(0f, -2.12f, 0f);

        var highlightGo = new GameObject("BoardHighlight");
        highlightGo.transform.SetParent(boardRoot.transform, false);
        var highlightSr = highlightGo.AddComponent<SpriteRenderer>();
        highlightSr.sprite = CreateSoftCircleSprite();
        highlightSr.color = new Color(1f, 1f, 1f, 0.05f);
        highlightSr.sortingOrder = 5;
        highlightGo.transform.localScale = new Vector3(5.8f, 1.2f, 1f);
        highlightGo.transform.position = new Vector3(0f, 2.02f, 0f);
    }

    private Sprite CreateMeadowSprite()
    {
        const int width = 384;
        const int height = 512;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var cols = new Color[width * height];
        var distant = new Color(0.78f, 0.87f, 0.67f, 1f);
        var mid = new Color(0.68f, 0.78f, 0.52f, 1f);
        var foreground = new Color(0.58f, 0.69f, 0.42f, 1f);
        var dirt = new Color(0.67f, 0.58f, 0.4f, 1f);
        var dryGrass = new Color(0.76f, 0.74f, 0.53f, 1f);

        for (var y = 0; y < height; y++)
        {
            var depth = y / (height - 1f);
            var horizonBlend = Mathf.SmoothStep(0f, 1f, depth);
            var row = Color.Lerp(foreground, mid, Mathf.Clamp01(depth * 1.15f));
            row = Color.Lerp(row, distant, Mathf.Clamp01((depth - 0.55f) * 2.1f));

            for (var x = 0; x < width; x++)
            {
                var worldX = x / (float)width;
                var worldY = y / (float)height;

                var broadNoise = Mathf.PerlinNoise(worldX * 2.2f + 11.3f, worldY * 2.6f + 3.7f) - 0.5f;
                var mediumNoise = Mathf.PerlinNoise(worldX * 6.1f + 7.9f, worldY * 7.4f + 14.2f) - 0.5f;
                var fineNoise = Mathf.PerlinNoise(worldX * 16.5f + 19.1f, worldY * 15.8f + 5.4f) - 0.5f;

                var color = new Color(
                    Mathf.Clamp01(row.r + broadNoise * 0.045f + mediumNoise * 0.028f + fineNoise * 0.01f),
                    Mathf.Clamp01(row.g + broadNoise * 0.06f + mediumNoise * 0.035f + fineNoise * 0.012f),
                    Mathf.Clamp01(row.b + broadNoise * 0.03f + mediumNoise * 0.018f + fineNoise * 0.008f),
                    1f);

                var dirtMask = Mathf.PerlinNoise(worldX * 3.15f + 28.4f, worldY * 3.6f + 9.6f);
                if (dirtMask > 0.61f)
                {
                    var t = Mathf.InverseLerp(0.61f, 0.86f, dirtMask) * (0.32f + (1f - horizonBlend) * 0.38f);
                    color = Color.Lerp(color, dirt, t);
                }

                var dryMask = Mathf.PerlinNoise(worldX * 5.7f + 2.2f, worldY * 4.4f + 17.8f);
                if (dryMask > 0.68f)
                {
                    var t = Mathf.InverseLerp(0.68f, 0.88f, dryMask) * 0.18f;
                    color = Color.Lerp(color, dryGrass, t);
                }

                var mainPath = DistanceToSegment(
                    new Vector2(worldX, worldY),
                    new Vector2(0.5f, 0.19f),
                    new Vector2(0.5f, 0.86f));
                var leftBranch = DistanceToSegment(
                    new Vector2(worldX, worldY),
                    new Vector2(0.5f, 0.34f),
                    new Vector2(0.28f, 0.46f));
                var rightBranch = DistanceToSegment(
                    new Vector2(worldX, worldY),
                    new Vector2(0.5f, 0.34f),
                    new Vector2(0.72f, 0.46f));

                var pathStrength = 0f;
                pathStrength = Mathf.Max(pathStrength, 1f - Mathf.SmoothStep(0.038f, 0.095f, mainPath));
                pathStrength = Mathf.Max(pathStrength, 1f - Mathf.SmoothStep(0.028f, 0.072f, leftBranch));
                pathStrength = Mathf.Max(pathStrength, 1f - Mathf.SmoothStep(0.028f, 0.072f, rightBranch));
                pathStrength *= 0.42f + (1f - horizonBlend) * 0.24f;

                if (pathStrength > 0.001f)
                {
                    var wornColor = Color.Lerp(dirt, dryGrass, 0.45f);
                    color = Color.Lerp(color, wornColor, pathStrength);
                }

                var sawmillPad = 1f - Mathf.SmoothStep(0.045f, 0.16f, Vector2.Distance(new Vector2(worldX, worldY), new Vector2(0.28f, 0.23f)));
                var factoryPad = 1f - Mathf.SmoothStep(0.045f, 0.16f, Vector2.Distance(new Vector2(worldX, worldY), new Vector2(0.72f, 0.23f)));
                var workerZone = 1f - Mathf.SmoothStep(0.08f, 0.28f, Vector2.Distance(new Vector2(worldX, worldY), new Vector2(0.5f, 0.55f)));

                if (sawmillPad > 0.001f)
                {
                    color = Color.Lerp(color, Color.Lerp(dirt, new Color(0.52f, 0.44f, 0.3f, 1f), 0.35f), sawmillPad * 0.36f);
                }
                if (factoryPad > 0.001f)
                {
                    color = Color.Lerp(color, Color.Lerp(dirt, new Color(0.57f, 0.51f, 0.38f, 1f), 0.4f), factoryPad * 0.32f);
                }
                if (workerZone > 0.001f)
                {
                    color = Color.Lerp(color, new Color(0.72f, 0.78f, 0.58f, 1f), workerZone * 0.06f);
                }

                var vignette = Mathf.Max(
                    Mathf.Abs(worldX - 0.5f) * 1.45f,
                    Mathf.Abs(worldY - 0.52f) * 1.12f);
                if (vignette > 0.42f)
                {
                    var edgeShade = Mathf.InverseLerp(0.42f, 0.85f, vignette) * 0.12f;
                    color = Color.Lerp(color, new Color(0.45f, 0.56f, 0.34f, 1f), edgeShade);
                }

                cols[y * width + x] = color;
            }
        }

        // Small grass accents stay subtle and denser in the foreground.
        for (var y = 16; y < height - 12; y += 18)
        {
            var depth = 1f - y / (float)height;
            var bladeHeight = Mathf.RoundToInt(Mathf.Lerp(2f, 5f, depth));
            var spacing = Mathf.RoundToInt(Mathf.Lerp(36f, 20f, depth));
            var grass = Color.Lerp(new Color(0.43f, 0.72f, 0.31f, 0.45f), new Color(0.25f, 0.56f, 0.18f, 0.72f), depth);
            var rowOffset = 8 + Mathf.RoundToInt(Mathf.PerlinNoise(0.12f, y * 0.037f) * 14f);

            for (var x = rowOffset; x < width - 8; x += spacing)
            {
                var mask = Mathf.PerlinNoise(x * 0.043f + 1.4f, y * 0.055f + 6.8f);
                if (mask < 0.42f)
                {
                    continue;
                }

                FillRect(cols, width, x, y, 1, bladeHeight, grass);
                FillRect(cols, width, x + 2, y + 1, 1, Mathf.Max(1, bladeHeight - 2), grass);
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 52f);
    }

    private float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var denominator = Vector2.Dot(ab, ab);
        if (denominator <= Mathf.Epsilon)
        {
            return Vector2.Distance(point, a);
        }

        var t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / denominator);
        var projection = a + ab * t;
        return Vector2.Distance(point, projection);
    }

    private Sprite CreateGradientSprite()
    {
        const int width = 32;
        const int height = 128;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var cols = new Color[width * height];
        var top = new Color(0.58f, 0.82f, 0.96f);
        var bottom = new Color(0.87f, 0.95f, 0.78f);
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

    private Sprite CreateBoardTopSprite()
    {
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var cols = new Color[size * size];
        var bg = new Color(0.78f, 0.9f, 0.74f, 1f);
        var line = new Color(0.69f, 0.82f, 0.63f, 0.85f);
        var edge = new Color(0.47f, 0.61f, 0.42f, 1f);
        var patchA = new Color(0.74f, 0.88f, 0.69f, 1f);
        var patchB = new Color(0.7f, 0.85f, 0.64f, 1f);

        for (var i = 0; i < cols.Length; i++) cols[i] = bg;

        for (var y = 0; y < size; y++)
        {
            var t = y / (float)(size - 1);
            var shade = Color.Lerp(new Color(0.84f, 0.98f, 0.76f, 1f), bg, t);
            for (var x = 0; x < size; x++)
            {
                cols[y * size + x] = shade;
            }
        }

        for (var y = 18; y < size - 18; y += 26)
        {
            for (var x = 14; x < size - 14; x += 30)
            {
                FillRect(cols, size, x, y, 10, 5, ((x + y) / 12) % 2 == 0 ? patchA : patchB);
            }
        }

        const int lines = 4;
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

        for (var x = 0; x < size; x++)
        {
            cols[x] = edge;
            cols[(size - 1) * size + x] = edge;
        }
        for (var y = 0; y < size; y++)
        {
            cols[y * size] = edge;
            cols[y * size + (size - 1)] = edge;
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

    private Sprite CreateBoardFrontSprite()
    {
        const int width = 256;
        const int height = 80;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var cols = new Color[width * height];
        var top = new Color(0.46f, 0.72f, 0.32f, 1f);
        var bottom = new Color(0.27f, 0.5f, 0.18f, 1f);

        for (var y = 0; y < height; y++)
        {
            var t = y / (height - 1f);
            var row = Color.Lerp(top, bottom, t);
            for (var x = 0; x < width; x++)
            {
                cols[y * width + x] = row;
            }
        }

        for (var y = 0; y < height; y++)
        {
            cols[y * width] = new Color(0.22f, 0.41f, 0.14f, 1f);
            cols[y * width + (width - 1)] = new Color(0.22f, 0.41f, 0.14f, 1f);
        }
        for (var x = 0; x < width; x++)
        {
            cols[x] = new Color(0.88f, 1f, 0.8f, 0.5f);
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 1f), 64f);
    }

    private Sprite CreateFloorSprite()
    {
        const int width = 256;
        const int height = 96;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var cols = new Color[width * height];
        var top = new Color(0.52f, 0.8f, 0.39f, 1f);
        var bottom = new Color(0.3f, 0.58f, 0.21f, 1f);
        var patchA = new Color(0.54f, 0.79f, 0.41f, 1f);
        var patchB = new Color(0.35f, 0.61f, 0.26f, 1f);

        for (var y = 0; y < height; y++)
        {
            var t = y / (height - 1f);
            var row = Color.Lerp(top, bottom, t);
            for (var x = 0; x < width; x++)
            {
                cols[y * width + x] = row;
            }
        }

        for (var y = 10; y < height - 10; y += 18)
        {
            for (var x = 12; x < width - 12; x += 32)
            {
                var color = ((x + y) / 18) % 2 == 0 ? patchA : patchB;
                FillRect(cols, width, x, y, 12, 5, color);
                FillRect(cols, width, x + 4, y - 4, 4, 4, new Color(0.66f, 0.9f, 0.48f, 1f));
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 64f);
    }

    private void CreateGrassPatch(Transform parent, Vector3 localPos, Vector3 scale, Color tint)
    {
        var patch = new GameObject("GrassPatch");
        patch.transform.SetParent(parent, false);
        patch.transform.localPosition = localPos;
        patch.transform.localScale = scale;

        var sr = patch.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSoftCircleSprite();
        sr.color = tint;
        sr.sortingOrder = -1;
    }

    private Sprite CreateSawmillSprite()
    {
        const int size = 96;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var cols = new Color[size * size];
        for (var i = 0; i < cols.Length; i++) cols[i] = Color.clear;

        var wood = new Color(0.65f, 0.43f, 0.22f, 1f);
        var roof = new Color(0.27f, 0.18f, 0.12f, 1f);
        var plank = new Color(0.78f, 0.61f, 0.34f, 1f);
        var outline = new Color(0.14f, 0.1f, 0.08f, 1f);

        FillRect(cols, size, 16, 14, 64, 40, wood);
        FillRect(cols, size, 12, 50, 72, 14, roof);
        FillRect(cols, size, 22, 24, 18, 18, plank);
        FillRect(cols, size, 54, 24, 18, 18, plank);
        FillRect(cols, size, 42, 14, 12, 20, new Color(0.3f, 0.19f, 0.12f, 1f));

        // log pile
        FillRect(cols, size, 70, 10, 18, 6, plank);
        FillRect(cols, size, 68, 6, 20, 4, wood);

        // simple outline pass
        var copy = (Color[])cols.Clone();
        for (var y = 1; y < size - 1; y++)
        {
            for (var x = 1; x < size - 1; x++)
            {
                var idx = y * size + x;
                if (copy[idx].a <= 0f) continue;
                for (var oy = -1; oy <= 1; oy++)
                {
                    for (var ox = -1; ox <= 1; ox++)
                    {
                        var n = (y + oy) * size + (x + ox);
                        if (copy[n].a <= 0f) cols[n] = outline;
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.35f, 0.05f), 96f);
    }

    private Sprite CreateLogPileSprite()
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var cols = new Color[size * size];
        for (var i = 0; i < cols.Length; i++) cols[i] = Color.clear;

        var bark = new Color(0.61f, 0.39f, 0.18f, 1f);
        var cut = new Color(0.9f, 0.75f, 0.48f, 1f);
        FillRect(cols, size, 12, 12, 26, 8, bark);
        FillRect(cols, size, 10, 22, 30, 8, bark);
        FillRect(cols, size, 18, 32, 24, 8, bark);
        FillRect(cols, size, 36, 13, 4, 6, cut);
        FillRect(cols, size, 40, 23, 4, 6, cut);
        FillRect(cols, size, 42, 33, 4, 6, cut);
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.08f), 64f);
    }

    private Sprite CreateFactorySprite()
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var cols = new Color[size * size];
        for (var i = 0; i < cols.Length; i++) cols[i] = Color.clear;

        var wall = new Color(0.67f, 0.79f, 0.86f, 1f);
        var wallShade = new Color(0.49f, 0.64f, 0.74f, 1f);
        var roof = new Color(0.24f, 0.42f, 0.5f, 1f);
        var window = new Color(0.88f, 0.98f, 1f, 1f);
        var metal = new Color(0.56f, 0.68f, 0.74f, 1f);
        var outline = new Color(0.12f, 0.22f, 0.29f, 1f);

        FillRect(cols, size, 14, 18, 78, 52, wall);
        FillRect(cols, size, 14, 18, 12, 52, wallShade);
        FillRect(cols, size, 10, 66, 86, 12, roof);
        FillRect(cols, size, 22, 34, 18, 14, window);
        FillRect(cols, size, 48, 34, 18, 14, window);
        FillRect(cols, size, 27, 18, 18, 20, metal);
        FillRect(cols, size, 70, 18, 12, 40, metal);
        FillRect(cols, size, 86, 44, 18, 42, metal);
        FillRect(cols, size, 90, 82, 10, 20, roof);

        var copy = (Color[])cols.Clone();
        for (var y = 1; y < size - 1; y++)
        {
            for (var x = 1; x < size - 1; x++)
            {
                var idx = y * size + x;
                if (copy[idx].a <= 0f) continue;
                for (var oy = -1; oy <= 1; oy++)
                {
                    for (var ox = -1; ox <= 1; ox++)
                    {
                        var n = (y + oy) * size + (x + ox);
                        if (copy[n].a <= 0f) cols[n] = outline;
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.46f, 0.05f), 96f);
    }

    private Sprite CreateWorkshopBuildingSprite(Color wallColor, Color roofColor, Color accentColor, bool includeCart)
    {
        const int size = 112;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var cols = new Color[size * size];
        for (var i = 0; i < cols.Length; i++) cols[i] = Color.clear;

        var wood = wallColor;
        var outline = new Color(0.12f, 0.1f, 0.08f, 1f);
        var stone = new Color(0.68f, 0.66f, 0.6f, 1f);
        var darkWood = Color.Lerp(wallColor, Color.black, 0.32f);

        FillRect(cols, size, 18, 18, 74, 38, wood);
        FillRect(cols, size, 14, 54, 82, 14, roofColor);
        FillRect(cols, size, 28, 18, 18, 18, darkWood);
        FillRect(cols, size, 52, 18, 18, 18, darkWood);
        FillRect(cols, size, 40, 18, 12, 22, new Color(0.24f, 0.18f, 0.12f, 1f));
        FillRect(cols, size, 16, 14, 10, 12, stone);
        FillRect(cols, size, 74, 14, 10, 12, stone);
        FillRect(cols, size, 84, 18, 10, 12, accentColor);

        if (includeCart)
        {
            FillRect(cols, size, 8, 12, 16, 8, accentColor);
            FillRect(cols, size, 6, 8, 20, 4, darkWood);
        }
        else
        {
            FillRect(cols, size, 82, 8, 14, 10, roofColor);
            FillRect(cols, size, 14, 10, 18, 6, accentColor);
        }

        var copy = (Color[])cols.Clone();
        for (var y = 1; y < size - 1; y++)
        {
            for (var x = 1; x < size - 1; x++)
            {
                var idx = y * size + x;
                if (copy[idx].a <= 0f) continue;
                for (var oy = -1; oy <= 1; oy++)
                {
                    for (var ox = -1; ox <= 1; ox++)
                    {
                        var n = (y + oy) * size + (x + ox);
                        if (copy[n].a <= 0f) cols[n] = outline;
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.42f, 0.06f), 96f);
    }

    private Sprite CreateConveyorSprite()
    {
        const int width = 96;
        const int height = 32;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var cols = new Color[width * height];
        for (var i = 0; i < cols.Length; i++) cols[i] = Color.clear;

        var belt = new Color(0.21f, 0.31f, 0.38f, 1f);
        var edge = new Color(0.62f, 0.74f, 0.8f, 1f);
        FillRect(cols, width, 4, 10, 88, 12, belt);
        FillRect(cols, width, 4, 8, 88, 2, edge);
        FillRect(cols, width, 4, 22, 88, 2, edge);
        for (var x = 10; x < 88; x += 16)
        {
            FillRect(cols, width, x, 12, 4, 8, edge);
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.05f, 0.5f), 64f);
    }

    private Sprite CreateSoftCircleSprite()
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var cols = new Color[size * size];
        var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        var maxDist = size * 0.5f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dist = Vector2.Distance(new Vector2(x, y), center);
                var alpha = Mathf.Clamp01(1f - dist / maxDist);
                alpha *= alpha;
                cols[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

    private Sprite CreateRectSprite()
    {
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var cols = new Color[16];
        for (var i = 0; i < cols.Length; i++) cols[i] = Color.white;
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    private Sprite LoadGeneratedSprite(string resourcePath)
    {
        return Resources.Load<Sprite>(resourcePath);
    }

    private Sprite GetGrassSprite()
    {
        var texture = Resources.Load<Texture2D>("Generated/GrassTile");
        if (texture == null)
        {
            return null;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Repeat;
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
    }

    private Sprite LoadBuildingSprite(string resourcePath, float pixelsPerUnit)
    {
        var texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            return null;
        }

        texture.filterMode = FilterMode.Bilinear;
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.08f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
    }

    private void CreateFactorySmoke(Transform parent)
    {
        var smokeGo = new GameObject("Smoke");
        smokeGo.transform.SetParent(parent, false);
        smokeGo.transform.localPosition = new Vector3(0.88f, 1.55f, 0f);
        var ps = smokeGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 2.4f;
        main.startSpeed = 0.38f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.32f);
        main.startColor = new Color(0.9f, 0.94f, 0.98f, 0.3f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 18;

        var emission = ps.emission;
        emission.rateOverTime = 4f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.04f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(0.88f, 0.93f, 0.98f), 0f), new GradientColorKey(new Color(0.9f, 0.95f, 1f), 1f) },
            new[] { new GradientAlphaKey(0.28f, 0f), new GradientAlphaKey(0.12f, 0.55f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingOrder = 7;
    }

    private void FillRect(Color[] cols, int width, int x, int y, int w, int h, Color color)
    {
        var height = cols.Length / width;
        for (var yy = y; yy < y + h; yy++)
        {
            for (var xx = x; xx < x + w; xx++)
            {
                if (xx < 0 || yy < 0 || xx >= width || yy >= height) continue;
                cols[yy * width + xx] = color;
            }
        }
    }

}
