using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
public class WorkerUnit : MonoBehaviour
{
    public int Level { get; private set; }
    public int SlotIndex { get; set; }

    private static Sprite shovelSprite;
    private static Sprite armorSprite;
    private static Sprite helmetStripeSprite;

    private static WorkerUnit activeDragUnit;
    private static int activeFingerId = -1;

    private SpriteRenderer sr;
    private SpriteRenderer shadowSr;
    private CircleCollider2D col;
    private TextMesh rankLabel;
    private TextMesh levelSmallLabel;
    private SpriteRenderer accessoryA;
    private SpriteRenderer accessoryB;

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

    public void Initialize(int level, int slotIndex, Sprite sprite)
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<CircleCollider2D>();
        Level = level;
        SlotIndex = slotIndex;
        sr.sprite = sprite;
        baseWorkerColor = GetTierTint(GetStarTier());
        sr.color = baseWorkerColor;
        transform.localScale = Vector3.one * (0.85f + level * 0.03f);
        gameObject.name = $"Worker_L{level}";

        EnsureAccessorySprites();
        SetupRankLabel();
        SetupLevelLabel();
        SetupAccessories();
        RefreshRankAndStyle();

        StartCoroutine(PulseRoutine());
    }

    private void Update()
    {
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
        world.z = 0;
        dragOffset = transform.position - world;
        sr.sortingOrder = 50;
        sr.color = Color.Lerp(baseWorkerColor, Color.white, 0.25f);
        shadowSr.color = new Color(0f, 0f, 0f, 0.28f);
    }

    private void ContinueDrag(Vector2 screenPos)
    {
        if (!dragging) return;
        var cam = Camera.main;
        if (cam == null) return;
        var world = cam.ScreenToWorldPoint(screenPos);
        world.z = 0;
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
        sr.sortingOrder = 10;
        sr.color = baseWorkerColor;
        shadowSr.color = new Color(0f, 0f, 0f, 0.18f);

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

        var gm = GameManager.Instance;
        if (gm != null && gm.WorkerBoard != null)
        {
            gm.WorkerBoard.HandleDrop(this);
        }
    }

    private void RefreshRankAndStyle()
    {
        var starTier = GetStarTier();
        var stepInTier = GetStepInTier();

        if (starTier > 0)
        {
            var stars = Mathf.Clamp(starTier, 1, 5);
            rankLabel.text = new string('★', stars);
            rankLabel.color = new Color(1f, 0.88f, 0.28f);
        }
        else
        {
            var arrows = Mathf.Max(1, stepInTier);
            rankLabel.text = BuildArrowStack(arrows);
            rankLabel.color = new Color(0.82f, 0.92f, 1f);
        }

        levelSmallLabel.text = $"Lv {Level}";
        baseWorkerColor = GetTierTint(starTier);
        sr.color = baseWorkerColor;

        ApplyAccessoryVariant(starTier);
    }

    private int GetStarTier() => Level / 5;
    private int GetStepInTier() => Level % 5;

    private string BuildArrowStack(int count)
    {
        if (count <= 1) return "^";
        var s = "^";
        for (var i = 1; i < count; i++) s += "\n^";
        return s;
    }

    private void SetupRankLabel()
    {
        var labelGo = new GameObject("RankLabel");
        labelGo.transform.SetParent(transform, false);
        labelGo.transform.localPosition = new Vector3(0.3f, 0.15f, -0.1f);
        rankLabel = labelGo.AddComponent<TextMesh>();
        rankLabel.alignment = TextAlignment.Right;
        rankLabel.anchor = TextAnchor.LowerRight;
        rankLabel.characterSize = 0.018f;
        rankLabel.fontSize = 30;
        rankLabel.color = new Color(0.95f, 0.95f, 1f);
        rankBaseLocalPos = labelGo.transform.localPosition;
    }

    private void SetupLevelLabel()
    {
        var labelGo = new GameObject("LevelLabel");
        labelGo.transform.SetParent(transform, false);
        labelGo.transform.localPosition = new Vector3(0f, -0.03f, -0.1f);
        levelSmallLabel = labelGo.AddComponent<TextMesh>();
        levelSmallLabel.alignment = TextAlignment.Center;
        levelSmallLabel.anchor = TextAnchor.MiddleCenter;
        levelSmallLabel.characterSize = 0.032f;
        levelSmallLabel.fontSize = 32;
        levelSmallLabel.color = new Color(0.04f, 0.07f, 0.12f);
        levelBaseLocalPos = labelGo.transform.localPosition;
    }

    private void SetupAccessories()
    {
        var sh = new GameObject("WorkerShadow");
        sh.transform.SetParent(transform, false);
        sh.transform.localPosition = new Vector3(0.02f, -0.06f, 0.04f);
        shadowSr = sh.AddComponent<SpriteRenderer>();
        shadowSr.sprite = sr.sprite;
        shadowSr.color = new Color(0f, 0f, 0f, 0.18f);
        shadowSr.sortingOrder = 8;

        var a = new GameObject("AccessoryA");
        a.transform.SetParent(transform, false);
        a.transform.localPosition = new Vector3(0.25f, 0.2f, -0.05f);
        accessoryA = a.AddComponent<SpriteRenderer>();
        accessoryA.sortingOrder = 12;
        accABaseLocalPos = a.transform.localPosition;

        var b = new GameObject("AccessoryB");
        b.transform.SetParent(transform, false);
        b.transform.localPosition = new Vector3(0f, 0.54f, -0.05f);
        accessoryB = b.AddComponent<SpriteRenderer>();
        accessoryB.sortingOrder = 12;
        accBBaseLocalPos = b.transform.localPosition;
    }

    private void ApplyAccessoryVariant(int starTier)
    {
        accessoryA.sprite = null;
        accessoryB.sprite = null;

        if (starTier >= 1)
        {
            accessoryA.sprite = shovelSprite;
            accessoryA.color = new Color(0.88f, 0.66f, 0.38f);
            accessoryA.transform.localScale = Vector3.one * 0.9f;
            accessoryA.gameObject.SetActive(true);
        }
        else
        {
            accessoryA.gameObject.SetActive(false);
        }

        if (starTier >= 2)
        {
            accessoryB.sprite = armorSprite;
            accessoryB.color = new Color(0.75f, 0.86f, 1f);
            accessoryB.transform.localPosition = new Vector3(0f, 0.28f, -0.05f);
            accessoryB.transform.localScale = Vector3.one * 0.95f;
            accessoryB.gameObject.SetActive(true);
        }
        else if (starTier >= 1)
        {
            accessoryB.sprite = helmetStripeSprite;
            accessoryB.color = new Color(1f, 0.42f, 0.3f);
            accessoryB.transform.localPosition = new Vector3(0f, 0.54f, -0.05f);
            accessoryB.transform.localScale = Vector3.one * 1.0f;
            accessoryB.gameObject.SetActive(true);
        }
        else
        {
            accessoryB.gameObject.SetActive(false);
        }

        if (starTier >= 3)
        {
            accessoryA.color = new Color(1f, 0.82f, 0.38f);
            accessoryB.color = new Color(0.85f, 0.92f, 1f);
        }

        if (starTier >= 4)
        {
            accessoryA.transform.localScale = Vector3.one * 1.05f;
            accessoryB.transform.localScale = Vector3.one * 1.05f;
        }

        if (starTier >= 5)
        {
            rankLabel.color = new Color(1f, 0.97f, 0.58f);
        }
    }

    private Color GetTierTint(int starTier)
    {
        return starTier switch
        {
            0 => Color.white,
            1 => new Color(1f, 1f, 1f),
            2 => new Color(0.93f, 0.97f, 1f),
            3 => new Color(1f, 0.97f, 0.9f),
            4 => new Color(0.92f, 1f, 0.92f),
            _ => new Color(1f, 0.95f, 0.86f)
        };
    }

    private static void EnsureAccessorySprites()
    {
        if (shovelSprite == null) shovelSprite = CreateShovelSprite();
        if (armorSprite == null) armorSprite = CreateArmorSprite();
        if (helmetStripeSprite == null) helmetStripeSprite = CreateHelmetStripeSprite();
    }

    private static Sprite CreateShovelSprite()
    {
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var cols = new Color[size * size];
        for (var i = 0; i < cols.Length; i++) cols[i] = Color.clear;
        FillRect(cols, size, 14, 7, 3, 16, Color.white);
        FillRect(cols, size, 10, 20, 11, 6, Color.white);
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.15f), 64f);
    }

    private static Sprite CreateArmorSprite()
    {
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var cols = new Color[size * size];
        for (var i = 0; i < cols.Length; i++) cols[i] = Color.clear;
        FillRect(cols, size, 8, 11, 16, 10, Color.white);
        FillRect(cols, size, 12, 8, 8, 4, Color.white);
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

    private static Sprite CreateHelmetStripeSprite()
    {
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var cols = new Color[size * size];
        for (var i = 0; i < cols.Length; i++) cols[i] = Color.clear;
        FillRect(cols, size, 7, 15, 18, 3, Color.white);
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

    private static void FillRect(Color[] cols, int width, int x, int y, int w, int h, Color color)
    {
        for (var yy = y; yy < y + h; yy++)
        for (var xx = x; xx < x + w; xx++)
        {
            if (xx < 0 || yy < 0 || xx >= width || yy >= width) continue;
            cols[yy * width + xx] = color;
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
        var baseScale = transform.localScale;
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
            var baseScale = Vector3.one * (0.85f + Level * 0.03f);
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
        accessoryA.transform.localPosition = accABaseLocalPos + new Vector3(0f, bob * 1.1f, 0f);
        accessoryB.transform.localPosition = accBBaseLocalPos + new Vector3(0f, bob * 0.9f, 0f);
    }
}
