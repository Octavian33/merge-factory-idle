using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
public class WorkerUnit : MonoBehaviour
{
    public int Level { get; private set; }
    public int SlotIndex { get; set; }

    private static Sprite toolBeltSprite;
    private static Sprite gogglesSprite;
    private static Sprite vestSprite;
    private static Sprite advancedHelmetSprite;
    private static Sprite badgeSprite;
    private static Sprite shadowBlobSprite;

    private static WorkerUnit activeDragUnit;
    private static int activeFingerId = -1;

    private SpriteRenderer sr;
    private SpriteRenderer shadowSr;
    private SpriteRenderer contactShadowSr;
    private CircleCollider2D col;
    private TextMesh rankLabel;
    private TextMesh levelSmallLabel;
    private MeshRenderer rankRenderer;
    private MeshRenderer levelRenderer;
    private SpriteRenderer accessoryA;
    private SpriteRenderer accessoryB;
    private SpriteRenderer infoPlate;

    private Vector3 dragOffset;
    private Vector3 pressStartPos;
    private float pressStartTime;
    private bool dragging;
    private bool movedWhileDragging;
    private Coroutine snapRoutine;
    private Vector3 snapVelocity;
    private Color baseWorkerColor;
    private Vector3 rankBaseLocalPos;
    private Vector3 levelBaseLocalPos;
    private Vector3 accABaseLocalPos;
    private Vector3 accBBaseLocalPos;
    private const int DragSortingBase = 120;
    private float levelScaleBase;

    public void Initialize(int level, int slotIndex, Sprite sprite)
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<CircleCollider2D>();
        Level = level;
        SlotIndex = slotIndex;
        sr.sprite = sprite;
        levelScaleBase = 0.85f + Mathf.Clamp(level, 1, 10) * 0.025f;
        transform.localScale = Vector3.one * levelScaleBase;
        gameObject.name = $"Worker_L{level}";

        EnsureAccessorySprites();
        SetupRankLabel();
        SetupLevelLabel();
        SetupAccessories();
        RefreshTierVisuals();
        UpdateVisualSorting(false);
        StartCoroutine(PulseRoutine());
    }

    private void Update()
    {
        if (!dragging)
        {
            UpdateVisualSorting(false);
        }

        HandleTouchInput();
        HandleMouseInput();
    }

    private void OnDisable()
    {
        if (activeDragUnit == this)
        {
            activeDragUnit = null;
            activeFingerId = -1;
        }
    }

    private void OnDestroy()
    {
        if (activeDragUnit == this)
        {
            activeDragUnit = null;
            activeFingerId = -1;
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount <= 0)
        {
            return;
        }

        for (var i = 0; i < Input.touchCount; i++)
        {
            var touch = Input.GetTouch(i);

            if (activeDragUnit == null && touch.phase == TouchPhase.Began && IsTouchOnThisWorker(touch))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    continue;
                }

                activeDragUnit = this;
                activeFingerId = touch.fingerId;
                BeginDrag(touch.position);
                return;
            }

            if (activeDragUnit == this && touch.fingerId == activeFingerId)
            {
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    ContinueDrag(touch.position);
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    EndDrag();
                    activeDragUnit = null;
                    activeFingerId = -1;
                }
                return;
            }
        }
    }

    private void HandleMouseInput()
    {
        if (Input.touchCount > 0)
        {
            return;
        }

        if (activeDragUnit == null && Input.GetMouseButtonDown(0) && IsMouseOnThisWorker())
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            activeDragUnit = this;
            BeginDrag(Input.mousePosition);
            return;
        }

        if (activeDragUnit == this)
        {
            if (Input.GetMouseButton(0))
            {
                ContinueDrag(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
                activeDragUnit = null;
            }
        }
    }

    private bool IsTouchOnThisWorker(Touch touch)
    {
        var cam = Camera.main;
        if (cam == null) return false;
        var world = cam.ScreenToWorldPoint(touch.position);
        world.z = 0f;
        var hits = Physics2D.OverlapPointAll(world);
        for (var i = 0; i < hits.Length; i++)
        {
            if (hits[i] == col)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsMouseOnThisWorker()
    {
        var cam = Camera.main;
        if (cam == null) return false;
        var world = cam.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;
        var hits = Physics2D.OverlapPointAll(world);
        for (var i = 0; i < hits.Length; i++)
        {
            if (hits[i] == col)
            {
                return true;
            }
        }
        return false;
    }

    private void BeginDrag(Vector2 screenPos)
    {
        var cam = Camera.main;
        if (cam == null) return;

        dragging = true;
        movedWhileDragging = false;
        pressStartPos = transform.position;
        pressStartTime = Time.time;
        if (snapRoutine != null)
        {
            StopCoroutine(snapRoutine);
            snapRoutine = null;
        }

        var world = cam.ScreenToWorldPoint(screenPos);
        world.z = 0f;
        dragOffset = transform.position - world;
        UpdateVisualSorting(true);
        sr.color = Color.Lerp(baseWorkerColor, Color.white, 0.25f);
        shadowSr.color = new Color(0f, 0f, 0f, 0.28f);
        contactShadowSr.color = new Color(0f, 0f, 0f, 0.16f);
    }

    private void ContinueDrag(Vector2 screenPos)
    {
        if (!dragging) return;
        var cam = Camera.main;
        if (cam == null) return;
        var world = cam.ScreenToWorldPoint(screenPos);
        world.z = 0f;
        if (Vector3.Distance(transform.position, world + dragOffset) > 0.05f)
        {
            movedWhileDragging = true;
        }
        transform.position = Vector3.Lerp(transform.position, world + dragOffset, 0.62f);
    }

    private void EndDrag()
    {
        if (!dragging) return;

        dragging = false;
        UpdateVisualSorting(false);
        sr.color = baseWorkerColor;
        shadowSr.color = new Color(0f, 0f, 0f, 0.18f);
        contactShadowSr.color = new Color(0f, 0f, 0f, 0.1f);

        var wasTap = !movedWhileDragging && Time.time - pressStartTime < 0.22f && Vector3.Distance(transform.position, pressStartPos) < 0.07f;
        if (wasTap)
        {
            var tapReward = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(Level)));
            var gmTap = GameManager.Instance;
            if (gmTap != null && gmTap.Economy != null)
            {
                gmTap.Economy.AddCoins(tapReward, true, transform.position);
                StartCoroutine(PunchRoutine());
            }
        }

        GameManager.Instance?.WorkerBoard?.HandleDrop(this);
    }

    private void RefreshTierVisuals()
    {
        var visualTier = GetVisualTier();
        baseWorkerColor = GetTierTint(visualTier);
        sr.color = baseWorkerColor;

        rankLabel.text = BuildTierMarkers(visualTier);
        rankLabel.color = new Color(0.98f, 0.95f, 0.84f);
        levelSmallLabel.text = $"Lv {Level}";

        ApplyAccessoryVariant(visualTier);
    }

    private int GetVisualTier()
    {
        if (Level <= 2) return 1;
        if (Level <= 4) return 2;
        if (Level <= 6) return 3;
        if (Level <= 8) return 4;
        return 5;
    }

    private string BuildTierMarkers(int visualTier)
    {
        return visualTier switch
        {
            1 => "^",
            2 => "^^",
            3 => "^^^",
            4 => "★★★★",
            _ => "★★★★★"
        };
    }

    private void SetupRankLabel()
    {
        var labelGo = new GameObject("RankLabel");
        labelGo.transform.SetParent(transform, false);
        labelGo.transform.localPosition = new Vector3(0.22f, 0.16f, -0.1f);
        rankLabel = labelGo.AddComponent<TextMesh>();
        rankLabel.alignment = TextAlignment.Right;
        rankLabel.anchor = TextAnchor.LowerRight;
        rankLabel.characterSize = 0.022f;
        rankLabel.fontSize = 28;
        rankRenderer = rankLabel.GetComponent<MeshRenderer>();
        rankBaseLocalPos = labelGo.transform.localPosition;
    }

    private void SetupLevelLabel()
    {
        var labelGo = new GameObject("LevelLabel");
        labelGo.transform.SetParent(transform, false);
        labelGo.transform.localPosition = new Vector3(0f, -0.002f, -0.1f);
        levelSmallLabel = labelGo.AddComponent<TextMesh>();
        levelSmallLabel.alignment = TextAlignment.Center;
        levelSmallLabel.anchor = TextAnchor.MiddleCenter;
        levelSmallLabel.characterSize = 0.034f;
        levelSmallLabel.fontSize = 38;
        levelSmallLabel.color = new Color(0.98f, 0.99f, 1f);
        levelRenderer = levelSmallLabel.GetComponent<MeshRenderer>();
        levelBaseLocalPos = labelGo.transform.localPosition;
    }

    private void SetupAccessories()
    {
        var sh = new GameObject("WorkerShadow");
        sh.transform.SetParent(transform, false);
        sh.transform.localPosition = new Vector3(0.02f, -0.12f, 0.04f);
        shadowSr = sh.AddComponent<SpriteRenderer>();
        shadowSr.sprite = shadowBlobSprite;
        shadowSr.color = new Color(0f, 0f, 0f, 0.24f);
        shadowSr.sortingOrder = 8;
        sh.transform.localScale = new Vector3(0.7f, 0.22f, 1f);

        var contact = new GameObject("ContactShadow");
        contact.transform.SetParent(transform, false);
        contact.transform.localPosition = new Vector3(0.02f, -0.1f, 0.03f);
        contactShadowSr = contact.AddComponent<SpriteRenderer>();
        contactShadowSr.sprite = shadowBlobSprite;
        contactShadowSr.color = new Color(0f, 0f, 0f, 0.14f);
        contactShadowSr.sortingOrder = 7;
        contact.transform.localScale = new Vector3(0.46f, 0.12f, 1f);

        var a = new GameObject("AccessoryA");
        a.transform.SetParent(transform, false);
        a.transform.localPosition = new Vector3(0.16f, 0.18f, -0.05f);
        accessoryA = a.AddComponent<SpriteRenderer>();
        accessoryA.sortingOrder = 12;
        accABaseLocalPos = a.transform.localPosition;

        var b = new GameObject("AccessoryB");
        b.transform.SetParent(transform, false);
        b.transform.localPosition = new Vector3(0f, 0.56f, -0.05f);
        accessoryB = b.AddComponent<SpriteRenderer>();
        accessoryB.sortingOrder = 12;
        accBBaseLocalPos = b.transform.localPosition;

        var plate = new GameObject("InfoPlate");
        plate.transform.SetParent(transform, false);
        plate.transform.localPosition = new Vector3(0f, -0.08f, -0.08f);
        infoPlate = plate.AddComponent<SpriteRenderer>();
        infoPlate.sprite = badgeSprite;
        infoPlate.color = new Color(0.14f, 0.24f, 0.3f, 0.84f);
        infoPlate.sortingOrder = 10;
        plate.transform.localScale = new Vector3(0.62f, 0.24f, 1f);
    }

    private void ApplyAccessoryVariant(int visualTier)
    {
        accessoryA.gameObject.SetActive(false);
        accessoryB.gameObject.SetActive(false);
        accessoryA.sprite = null;
        accessoryB.sprite = null;

        switch (visualTier)
        {
            case 2:
                accessoryA.sprite = toolBeltSprite;
                accessoryA.color = new Color(0.64f, 0.45f, 0.24f);
                accessoryA.transform.localPosition = new Vector3(0f, 0.08f, -0.05f);
                accessoryA.transform.localScale = Vector3.one;
                accessoryA.gameObject.SetActive(true);
                break;
            case 3:
                accessoryA.sprite = toolBeltSprite;
                accessoryA.color = new Color(0.64f, 0.45f, 0.24f);
                accessoryA.transform.localPosition = new Vector3(0f, 0.08f, -0.05f);
                accessoryA.gameObject.SetActive(true);

                accessoryB.sprite = gogglesSprite;
                accessoryB.color = new Color(0.88f, 0.95f, 1f);
                accessoryB.transform.localPosition = new Vector3(0f, 0.36f, -0.05f);
                accessoryB.gameObject.SetActive(true);
                break;
            case 4:
                accessoryA.sprite = vestSprite;
                accessoryA.color = new Color(1f, 0.55f, 0.22f);
                accessoryA.transform.localPosition = new Vector3(0f, 0.16f, -0.05f);
                accessoryA.transform.localScale = Vector3.one * 1.02f;
                accessoryA.gameObject.SetActive(true);

                accessoryB.sprite = gogglesSprite;
                accessoryB.color = new Color(0.88f, 0.95f, 1f);
                accessoryB.transform.localPosition = new Vector3(0f, 0.36f, -0.05f);
                accessoryB.gameObject.SetActive(true);
                break;
            case 5:
                accessoryA.sprite = vestSprite;
                accessoryA.color = new Color(1f, 0.55f, 0.22f);
                accessoryA.transform.localPosition = new Vector3(0f, 0.16f, -0.05f);
                accessoryA.transform.localScale = Vector3.one * 1.02f;
                accessoryA.gameObject.SetActive(true);

                accessoryB.sprite = advancedHelmetSprite;
                accessoryB.color = Color.white;
                accessoryB.transform.localPosition = new Vector3(0f, 0.58f, -0.05f);
                accessoryB.transform.localScale = Vector3.one * 1.02f;
                accessoryB.gameObject.SetActive(true);
                break;
        }
    }

    private Color GetTierTint(int visualTier)
    {
        return visualTier switch
        {
            1 => Color.white,
            2 => new Color(0.97f, 0.98f, 1f),
            3 => new Color(0.95f, 0.99f, 1f),
            4 => new Color(1f, 0.98f, 0.95f),
            _ => new Color(1f, 0.98f, 0.95f)
        };
    }

    private static void EnsureAccessorySprites()
    {
        if (toolBeltSprite == null) toolBeltSprite = CreateToolBeltSprite();
        if (gogglesSprite == null) gogglesSprite = CreateGogglesSprite();
        if (vestSprite == null) vestSprite = CreateVestSprite();
        if (advancedHelmetSprite == null) advancedHelmetSprite = CreateAdvancedHelmetSprite();
        if (badgeSprite == null) badgeSprite = CreateBadgeSprite();
        if (shadowBlobSprite == null) shadowBlobSprite = CreateShadowBlobSprite();
    }

    private static Sprite CreateBadgeSprite()
    {
        const int width = 40;
        const int height = 18;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var cols = new Color[width * height];
        var radius = 5f;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var dx = Mathf.Min(x, width - 1 - x);
                var dy = Mathf.Min(y, height - 1 - y);
                var inside = dx >= radius || dy >= radius || Vector2.Distance(new Vector2(dx, dy), new Vector2(radius, radius)) <= radius;
                cols[y * width + x] = inside ? Color.white : Color.clear;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 64f);
    }

    private static Sprite CreateShadowBlobSprite()
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var cols = new Color[size * size];
        var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var nx = (x - center.x) / (size * 0.42f);
                var ny = (y - center.y) / (size * 0.18f);
                var d = nx * nx + ny * ny;
                var a = Mathf.Clamp01(1f - d);
                a *= a;
                cols[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

    private static Sprite CreateToolBeltSprite()
    {
        const int width = 32;
        const int height = 12;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var cols = new Color[width * height];
        for (var i = 0; i < cols.Length; i++) cols[i] = Color.clear;
        FillRect(cols, width, 2, 4, 28, 4, Color.white);
        FillRect(cols, width, 9, 2, 5, 8, Color.white);
        FillRect(cols, width, 19, 2, 5, 8, Color.white);
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 64f);
    }

    private static Sprite CreateGogglesSprite()
    {
        const int width = 28;
        const int height = 14;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var cols = new Color[width * height];
        for (var i = 0; i < cols.Length; i++) cols[i] = Color.clear;
        FillRect(cols, width, 2, 5, 24, 4, Color.white);
        FillRect(cols, width, 4, 3, 7, 8, Color.white);
        FillRect(cols, width, 17, 3, 7, 8, Color.white);
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 64f);
    }

    private static Sprite CreateVestSprite()
    {
        const int width = 30;
        const int height = 24;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var cols = new Color[width * height];
        for (var i = 0; i < cols.Length; i++) cols[i] = Color.clear;
        FillRect(cols, width, 3, 2, 24, 20, Color.white);
        FillRect(cols, width, 12, 2, 6, 20, Color.clear);
        FillRect(cols, width, 6, 8, 6, 2, new Color(1f, 1f, 1f, 0.6f));
        FillRect(cols, width, 18, 8, 6, 2, new Color(1f, 1f, 1f, 0.6f));
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 64f);
    }

    private static Sprite CreateAdvancedHelmetSprite()
    {
        const int width = 30;
        const int height = 18;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var cols = new Color[width * height];
        for (var i = 0; i < cols.Length; i++) cols[i] = Color.clear;
        FillRect(cols, width, 2, 8, 26, 6, Color.white);
        FillRect(cols, width, 7, 4, 16, 5, Color.white);
        FillRect(cols, width, 12, 0, 6, 5, Color.white);
        FillRect(cols, width, 13, 2, 4, 3, new Color(0f, 0f, 0f, 0.25f));
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 64f);
    }

    private static void FillRect(Color[] cols, int width, int x, int y, int w, int h, Color color)
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

    public void SetDropTarget(Vector3 target)
    {
        snapVelocity = Vector3.zero;
        if (snapRoutine != null)
        {
            StopCoroutine(snapRoutine);
        }
        snapRoutine = StartCoroutine(SnapToRoutine(target));
    }

    private System.Collections.IEnumerator SnapToRoutine(Vector3 target)
    {
        while (!dragging && Vector3.Distance(transform.position, target) > 0.008f)
        {
            transform.position = Vector3.SmoothDamp(transform.position, target, ref snapVelocity, 0.045f);
            yield return null;
        }

        if (!dragging)
        {
            transform.position = target;
        }
        snapRoutine = null;
    }

    private System.Collections.IEnumerator PunchRoutine()
    {
        var baseScale = GetCurrentBaseScale();
        var up = baseScale * 1.12f;
        var t = 0f;
        while (t < 0.08f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(transform.localScale, up, 0.55f);
            yield return null;
        }

        t = 0f;
        while (t < 0.1f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale, 0.5f);
            yield return null;
        }
        transform.localScale = baseScale;
    }

    private System.Collections.IEnumerator PulseRoutine()
    {
        var bobPhase = Random.Range(0f, Mathf.PI * 2f);
        while (true)
        {
            var baseScale = GetCurrentBaseScale();
            var pulseScale = baseScale * 1.025f;
            var t = 0f;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                if (!dragging)
                {
                    transform.localScale = Vector3.Lerp(baseScale, pulseScale, Mathf.SmoothStep(0f, 1f, t / 0.45f));
                    ApplyIdleBob(bobPhase);
                }
                yield return null;
            }

            t = 0f;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                if (!dragging)
                {
                    transform.localScale = Vector3.Lerp(pulseScale, baseScale, Mathf.SmoothStep(0f, 1f, t / 0.45f));
                    ApplyIdleBob(bobPhase);
                }
                yield return null;
            }

            bobPhase += 0.65f;
        }
    }

    private void ApplyIdleBob(float phase)
    {
        var bob = Mathf.Sin((Time.time * 2.4f) + phase) * 0.026f;
        rankLabel.transform.localPosition = rankBaseLocalPos + new Vector3(0f, bob, 0f);
        levelSmallLabel.transform.localPosition = levelBaseLocalPos + new Vector3(0f, bob * 0.5f, 0f);
        accessoryA.transform.localPosition = accABaseLocalPos + new Vector3(0f, bob * 1.05f, 0f);
        accessoryB.transform.localPosition = accBBaseLocalPos + new Vector3(0f, bob * 0.9f, 0f);
        contactShadowSr.transform.localScale = new Vector3(0.46f + Mathf.Abs(bob) * 0.18f, 0.12f + Mathf.Abs(bob) * 0.03f, 1f);
    }

    private Vector3 GetCurrentBaseScale()
    {
        var depthT = Mathf.InverseLerp(2.7f, -1.0f, transform.position.y);
        var depthScale = Mathf.Lerp(0.92f, 1.05f, depthT);
        return Vector3.one * (levelScaleBase * depthScale);
    }

    private void UpdateVisualSorting(bool forceFront)
    {
        var baseOrder = forceFront ? DragSortingBase : Mathf.Clamp(90 - Mathf.RoundToInt(transform.position.y * 10f), 10, 110);
        sr.sortingOrder = baseOrder;
        shadowSr.sortingOrder = baseOrder - 2;
        contactShadowSr.sortingOrder = baseOrder - 3;
        infoPlate.sortingOrder = baseOrder;
        accessoryA.sortingOrder = baseOrder + 2;
        accessoryB.sortingOrder = baseOrder + 2;
        if (rankRenderer != null) rankRenderer.sortingOrder = baseOrder + 4;
        if (levelRenderer != null) levelRenderer.sortingOrder = baseOrder + 1;
    }
}
