using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIHud : MonoBehaviour
{
    private const string BuiltinFontName = "LegacyRuntime.ttf";

    private Text coinsText;
    private Text incomeText;
    private Text toastText;

    private Text buyButtonText;
    private Text prestigeButtonText;
    private Text incomeUpgradeButtonText;
    private Text discountUpgradeButtonText;

    private Text orderToggleText;
    private Text orderTitleText;
    private Text orderProgressText;
    private Text orderTimerText;
    private Text orderClaimText;

    private Text settingsButtonText;
    private Text volumeValueText;
    private Text hapticToggleText;

    private Image buyButtonImage;
    private Image prestigeButtonImage;
    private Image incomeUpgradeButtonImage;
    private Image discountUpgradeButtonImage;
    private Image orderToggleButtonImage;
    private Image orderClaimButtonImage;
    private Image settingsToggleButtonImage;

    private RectTransform floatRoot;
    private RectTransform orderPanel;
    private RectTransform settingsPanel;
    private RectTransform settingsGearIcon;
    private Slider volumeSlider;
    private Sprite circleSprite;
    private Sprite gearSprite;
    private Coroutine toastRoutine;
    private bool orderPanelOpen;
    private bool settingsOpen;
    private AudioSource uiAudioSource;
    private AudioClip uiTapClip;
    private AudioClip uiSuccessClip;
    private float lastHapticTime;
    private float pendingVolumeValue = -1f;
    private float volumeSaveDelayTimer;
    private float lastBuyPressTime;
    private float lastClaimPressTime;
    private float lastPrestigePressTime;
    private Vector2 orderPanelBasePos;
    private Coroutine orderPanelAnimRoutine;
    private Coroutine coinsPulseRoutine;
    private Vector3 coinsBaseScale = Vector3.one;
    private bool hapticEnabled = true;

    public void Build()
    {
        EnsureEventSystem();
        var canvas = CreateCanvas();
        floatRoot = canvas.transform as RectTransform;
        circleSprite = CreateCircleSprite();
        gearSprite = CreateGearSprite();

        CreateTopStrip();
        CreateBottomControls();
        CreateOrderUI();
        CreateSettingsUI();
        BuildUiAudio();

        orderPanelOpen = false;
        settingsOpen = false;
        SetOrderPanelVisible(false);
        SetSettingsVisible(false);

        var storedVolume = PlayerPrefs.GetFloat("master_volume", 1f);
        AudioListener.volume = Mathf.Clamp01(storedVolume);
        hapticEnabled = PlayerPrefs.GetInt("haptic_enabled", 1) == 1;
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
        }
        if (hapticToggleText != null)
        {
            hapticToggleText.text = hapticEnabled ? "Haptic: ON" : "Haptic: OFF";
        }
    }

    private void Update()
    {
        HandleBackKey();
        UpdateGearSpin();

        if (settingsOpen)
        {
            HandleCloseSettingsOutsideTap();
            return;
        }

        if (orderPanelOpen)
        {
            HandleCloseOrderOutsideTap();
        }

        FlushPendingVolumeSave();
    }

    private void OnDisable()
    {
        FlushPendingVolumeSaveImmediate();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            FlushPendingVolumeSaveImmediate();
        }
    }

    private void HandleBackKey()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (settingsOpen)
        {
            settingsOpen = false;
            SetSettingsVisible(false);
            RefreshAll();
            return;
        }

        if (orderPanelOpen)
        {
            orderPanelOpen = false;
            SetOrderPanelVisible(false);
            RefreshAll();
        }
    }

    public void RefreshAll()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.Economy == null || gm.Orders == null || gm.State == null)
        {
            if (volumeValueText != null)
            {
                volumeValueText.text = $"Volume: {Mathf.RoundToInt(AudioListener.volume * 100f)}%";
            }
            return;
        }

        var eco = gm.Economy;
        var order = gm.Orders;
        if (coinsText == null || incomeText == null || buyButtonText == null || prestigeButtonText == null || incomeUpgradeButtonText == null || discountUpgradeButtonText == null || orderToggleText == null || orderTitleText == null || orderProgressText == null || orderTimerText == null || orderClaimText == null)
        {
            return;
        }

        var buyCost = eco.GetBuyCost();
        var incomeCost = eco.GetIncomeUpgradeCost();
        var discountCost = eco.GetDiscountUpgradeCost();

        coinsText.text = $"Coins: {eco.Format(gm.State.coins)}";
        incomeText.text = $"Income/s: {eco.Format(eco.CurrentIncomePerSecond)}";

        buyButtonText.text = $"Buy\n{eco.Format(buyCost)}";
        incomeUpgradeButtonText.text = $"Income\nLv {gm.State.incomeUpgradeLevel}\n{eco.Format(incomeCost)}";
        discountUpgradeButtonText.text = $"Discount\nLv {gm.State.buyUpgradeLevel}\n{eco.Format(discountCost)}";
        prestigeButtonText.text = "Prestige";

        orderToggleText.text = order.IsReadyToClaim ? "Order !" : "Order";
        var progressPct = order.Target > 0 ? Mathf.RoundToInt((order.Progress / (float)order.Target) * 100f) : 0;
        var rewardText = eco.Format(order.CurrentReward);
        orderTitleText.text = order.Description;
        orderProgressText.text = order.IsReadyToClaim ? $"Progress {order.Progress}/{order.Target}  READY TO CLAIM" : $"Progress {order.Progress}/{order.Target}  ({progressPct}%)";
        orderProgressText.color = order.IsReadyToClaim ? new Color(0.62f, 1f, 0.66f) : new Color(0.95f, 1f, 0.92f);
        orderTimerText.text = order.IsReadyToClaim ? $"Reward +{rewardText}" : $"Time {order.TimeLeftSeconds / 60:00}:{order.TimeLeftSeconds % 60:00}";
        orderClaimText.text = order.IsReadyToClaim ? $"Claim\n+{rewardText}" : "...";

        if (volumeValueText != null)
        {
            volumeValueText.text = $"Volume: {Mathf.RoundToInt(AudioListener.volume * 100f)}%";
        }

        SetButtonState(buyButtonImage, gm.State.coins >= buyCost, new Color(0.99f, 0.62f, 0.24f, 0.98f));
        SetButtonState(incomeUpgradeButtonImage, gm.State.coins >= incomeCost, new Color(0.22f, 0.69f, 0.78f, 0.98f));
        SetButtonState(discountUpgradeButtonImage, gm.State.coins >= discountCost, new Color(0.19f, 0.63f, 0.72f, 0.98f));
        SetButtonState(prestigeButtonImage, gm.WorkerBoard.GetHighestLevel() >= 8, new Color(0.58f, 0.42f, 0.84f, 0.98f));
        SetButtonState(orderToggleButtonImage, true, order.IsReadyToClaim ? new Color(0.23f, 0.7f, 0.34f, 0.98f) : new Color(0.14f, 0.52f, 0.75f, 0.98f));
        SetButtonState(orderClaimButtonImage, order.IsReadyToClaim, new Color(0.2f, 0.72f, 0.4f, 0.98f));
        UpdateClaimButtonPulse(order.IsReadyToClaim);
    }

    public void SpawnFloatingCoin(Vector3 worldPos, string text)
    {
        var cam = Camera.main;
        if (cam == null || floatRoot == null)
        {
            return;
        }

        var go = new GameObject("FloatingCoin");
        go.transform.SetParent(floatRoot, false);
        go.transform.localScale = Vector3.one * 0.85f;
        var label = go.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>(BuiltinFontName);
        label.fontSize = 28;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(1f, 0.95f, 0.3f);
        label.text = text;
        label.raycastTarget = false;
        AddTextOutline(label);

        var rt = label.rectTransform;
        var screen = cam.WorldToScreenPoint(worldPos + Vector3.up * 0.4f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(floatRoot, screen, null, out var local);
        local += new Vector2(Random.Range(-26f, 26f), Random.Range(-8f, 14f));
        var rect = floatRoot.rect;
        local.x = Mathf.Clamp(local.x, rect.xMin + 70f, rect.xMax - 70f);
        local.y = Mathf.Clamp(local.y, rect.yMin + 90f, rect.yMax - 90f);
        rt.anchoredPosition = local;
        StartCoroutine(FloatRoutine(label));
    }

    public void ShowToast(string message)
    {
        if (toastText == null)
        {
            return;
        }

        if (toastRoutine != null)
        {
            StopCoroutine(toastRoutine);
        }
        toastText.text = message;
        toastRoutine = StartCoroutine(ToastRoutine());
    }

    private Canvas CreateCanvas()
    {
        var go = new GameObject("Canvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private void CreateTopStrip()
    {
        var panel = CreatePanel("TopStrip", new Vector2(0.5f, 1f), new Vector2(980, 168), new Color(0.04f, 0.12f, 0.2f, 0.34f));
        panel.pivot = new Vector2(0.5f, 1f);

        coinsText = CreateLabel(panel, "Coins", new Vector2(0.04f, 0.7f), 58, TextAnchor.MiddleLeft, Color.white, new Vector2(620, 70));
        incomeText = CreateLabel(panel, "Income", new Vector2(0.04f, 0.28f), 42, TextAnchor.MiddleLeft, new Color(0.96f, 1f, 0.78f), new Vector2(620, 62));
        toastText = CreateLabel(panel, "", new Vector2(0.72f, 0.5f), 36, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.35f), new Vector2(470, 68));
        coinsBaseScale = coinsText.rectTransform.localScale;
    }

    private void CreateBottomControls()
    {
        CreatePillButton(floatRoot, "Income", new Vector2(0.24f, 0.068f), new Vector2(224, 108), out incomeUpgradeButtonText, out incomeUpgradeButtonImage, () =>
        {
            PlayButtonPop(incomeUpgradeButtonImage.rectTransform);
            PlayUiTap();
            var gm = GameManager.Instance;
            if (gm == null || gm.Economy == null) return;
            if (!gm.Economy.TryUpgradeIncome()) ShowToast("Need coins for Income");
            RefreshAll();
        });

        CreateRoundButton("Buy", new Vector2(0.50f, 0.06f), 168, out buyButtonText, out buyButtonImage, () =>
        {
            if (!CanAcceptUiPress(ref lastBuyPressTime, 0.14f)) return;
            PlayButtonPop(buyButtonImage.rectTransform);
            PlayUiTap();
            var gm = GameManager.Instance;
            if (gm == null || gm.Economy == null) return;
            if (!gm.Economy.TryBuyWorker()) ShowToast("Need coins or free slot");
            RefreshAll();
        });

        CreatePillButton(floatRoot, "Discount", new Vector2(0.76f, 0.068f), new Vector2(224, 108), out discountUpgradeButtonText, out discountUpgradeButtonImage, () =>
        {
            PlayButtonPop(discountUpgradeButtonImage.rectTransform);
            PlayUiTap();
            var gm = GameManager.Instance;
            if (gm == null || gm.Economy == null) return;
            if (!gm.Economy.TryUpgradeBuyDiscount()) ShowToast("Need coins for Discount");
            RefreshAll();
        });

        CreateRoundButton("Prestige", new Vector2(0.905f, 0.205f), 153, out prestigeButtonText, out prestigeButtonImage, () =>
        {
            if (!CanAcceptUiPress(ref lastPrestigePressTime, 0.2f)) return;
            PlayButtonPop(prestigeButtonImage.rectTransform);
            PlayUiTap();
            var gm = GameManager.Instance;
            if (gm == null || gm.Economy == null) return;
            if (!gm.Economy.TryPrestige()) ShowToast("Reach worker Lv8 first");
            RefreshAll();
        });
    }

    private void CreateOrderUI()
    {
        CreateRoundButton("Order", new Vector2(0.095f, 0.205f), 153, out orderToggleText, out orderToggleButtonImage, () =>
        {
            PlayButtonPop(orderToggleButtonImage.rectTransform);
            PlayUiTap();
            orderPanelOpen = !orderPanelOpen;
            SetOrderPanelVisible(orderPanelOpen);
            RefreshAll();
        });

        orderPanel = CreatePanel("OrderPanel", new Vector2(0.5f, 0.315f), new Vector2(840, 138), new Color(0.1f, 0.2f, 0.12f, 0.38f));
        orderPanelBasePos = orderPanel.anchoredPosition;
        orderTitleText = CreateLabel(orderPanel, "OrderTitle", new Vector2(0.03f, 0.72f), 34, TextAnchor.MiddleLeft, new Color(0.95f, 1f, 0.92f), new Vector2(560, 52));
        orderProgressText = CreateLabel(orderPanel, "OrderProgress", new Vector2(0.03f, 0.42f), 30, TextAnchor.MiddleLeft, new Color(0.95f, 1f, 0.92f), new Vector2(560, 48));
        orderTimerText = CreateLabel(orderPanel, "OrderTimer", new Vector2(0.03f, 0.16f), 28, TextAnchor.MiddleLeft, new Color(0.95f, 1f, 0.92f), new Vector2(560, 44));

        CreatePillButton(orderPanel, "Claim", new Vector2(0.84f, 0.5f), new Vector2(150, 66), out orderClaimText, out orderClaimButtonImage, () =>
        {
            if (!CanAcceptUiPress(ref lastClaimPressTime, 0.2f)) return;
            PlayButtonPop(orderClaimButtonImage.rectTransform);
            PlayUiTap();
            var gm = GameManager.Instance;
            if (gm == null || gm.Orders == null)
            {
                return;
            }
            if (!gm.Orders.TryClaim()) ShowToast("Complete order first");
            else
            {
                PlayUiSuccess();
                orderPanelOpen = false;
                SetOrderPanelVisible(false);
            }
            RefreshAll();
        });
    }

    private void CreateSettingsUI()
    {
        CreateRoundButton("SET", new Vector2(0.93f, 0.935f), 100, out settingsButtonText, out settingsToggleButtonImage, () =>
        {
            PlayButtonPop(settingsToggleButtonImage.rectTransform);
            PlayUiTap();
            settingsOpen = !settingsOpen;
            SetSettingsVisible(settingsOpen);
            if (!settingsOpen)
            {
                RefreshAll();
            }
        });
        settingsToggleButtonImage.color = new Color(0.3f, 0.34f, 0.52f, 0.98f);
        settingsButtonText.text = string.Empty;
        settingsGearIcon = CreateSettingsGearIcon(settingsToggleButtonImage.rectTransform);

        settingsPanel = CreatePanel("SettingsPanel", new Vector2(0.5f, 0.54f), new Vector2(780, 560), new Color(0.05f, 0.09f, 0.16f, 0.94f));
        settingsPanel.GetComponent<Image>().raycastTarget = true;

        CreateLabel(settingsPanel, "SettingsTitle", new Vector2(0.5f, 0.9f), 56, TextAnchor.MiddleCenter, Color.white, new Vector2(420, 84)).text = "Settings";

        volumeValueText = CreateLabel(settingsPanel, "VolumeLabel", new Vector2(0.5f, 0.72f), 38, TextAnchor.MiddleCenter, new Color(0.95f, 1f, 0.95f), new Vector2(420, 64));

        volumeSlider = CreateSlider(settingsPanel, new Vector2(0.5f, 0.62f), new Vector2(520, 50));
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.wholeNumbers = false;
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        Text resetText;
        Image resetImage;
        CreatePillButton(settingsPanel, "Reset", new Vector2(0.5f, 0.43f), new Vector2(300, 90), out resetText, out resetImage, () =>
        {
            PlayUiTap();
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.ResetProgress();
            ShowToast("Reset complete");
            RefreshAll();
        });
        resetText.text = "Reset Progress";
        resetImage.color = new Color(0.78f, 0.36f, 0.32f, 0.98f);

        Text exitText;
        Image exitImage;
        CreatePillButton(settingsPanel, "Exit", new Vector2(0.5f, 0.26f), new Vector2(300, 90), out exitText, out exitImage, () =>
        {
            PlayUiTap();
            var gm = GameManager.Instance;
            if (gm != null) gm.SaveNow();
            Application.Quit();
        });
        exitText.text = "Exit Game";
        exitImage.color = new Color(0.32f, 0.48f, 0.72f, 0.98f);

        Text closeText;
        Image closeImage;
        CreatePillButton(settingsPanel, "Close", new Vector2(0.5f, 0.1f), new Vector2(220, 76), out closeText, out closeImage, () =>
        {
            PlayUiTap();
            settingsOpen = false;
            SetSettingsVisible(false);
            RefreshAll();
        });
        closeText.text = "Close";
        closeImage.color = new Color(0.35f, 0.64f, 0.48f, 0.98f);

        CreatePillButton(settingsPanel, "Haptic", new Vector2(0.5f, 0.58f), new Vector2(300, 78), out hapticToggleText, out var hapticImage, () =>
        {
            hapticEnabled = !hapticEnabled;
            PlayerPrefs.SetInt("haptic_enabled", hapticEnabled ? 1 : 0);
            PlayerPrefs.Save();
            hapticToggleText.text = hapticEnabled ? "Haptic: ON" : "Haptic: OFF";
            ShowToast(hapticEnabled ? "Haptic enabled" : "Haptic disabled");
        });
        hapticImage.color = new Color(0.34f, 0.58f, 0.82f, 0.98f);
    }

    private Slider CreateSlider(Transform parent, Vector2 anchor, Vector2 size)
    {
        var sliderGo = new GameObject("VolumeSlider");
        sliderGo.transform.SetParent(parent, false);
        var sliderRt = sliderGo.AddComponent<RectTransform>();
        sliderRt.anchorMin = anchor;
        sliderRt.anchorMax = anchor;
        sliderRt.pivot = new Vector2(0.5f, 0.5f);
        sliderRt.sizeDelta = size;

        var slider = sliderGo.AddComponent<Slider>();

        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(sliderGo.transform, false);
        var bg = bgGo.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.24f, 0.34f, 1f);
        var bgRt = bg.rectTransform;
        bgRt.anchorMin = new Vector2(0f, 0.35f);
        bgRt.anchorMax = new Vector2(1f, 0.65f);
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        var fillAreaGo = new GameObject("Fill Area");
        fillAreaGo.transform.SetParent(sliderGo.transform, false);
        var fillAreaRt = fillAreaGo.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0f, 0f);
        fillAreaRt.anchorMax = new Vector2(1f, 1f);
        fillAreaRt.offsetMin = new Vector2(12f, 0f);
        fillAreaRt.offsetMax = new Vector2(-12f, 0f);

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        var fill = fillGo.AddComponent<Image>();
        fill.color = new Color(0.3f, 0.9f, 0.6f, 1f);
        var fillRt = fill.rectTransform;
        fillRt.anchorMin = new Vector2(0f, 0.25f);
        fillRt.anchorMax = new Vector2(1f, 0.75f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        var handleSlideAreaGo = new GameObject("Handle Slide Area");
        handleSlideAreaGo.transform.SetParent(sliderGo.transform, false);
        var handleSlideAreaRt = handleSlideAreaGo.AddComponent<RectTransform>();
        handleSlideAreaRt.anchorMin = new Vector2(0f, 0f);
        handleSlideAreaRt.anchorMax = new Vector2(1f, 1f);
        handleSlideAreaRt.offsetMin = new Vector2(12f, 0f);
        handleSlideAreaRt.offsetMax = new Vector2(-12f, 0f);

        var handleGo = new GameObject("Handle");
        handleGo.transform.SetParent(handleSlideAreaGo.transform, false);
        var handle = handleGo.AddComponent<Image>();
        handle.sprite = circleSprite;
        handle.color = new Color(1f, 1f, 1f, 1f);
        var handleRt = handle.rectTransform;
        handleRt.sizeDelta = new Vector2(42f, 42f);

        slider.targetGraphic = handle;
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.direction = Slider.Direction.LeftToRight;

        return slider;
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
        pendingVolumeValue = AudioListener.volume;
        volumeSaveDelayTimer = 0.2f;
        if (volumeValueText != null)
        {
            volumeValueText.text = $"Volume: {Mathf.RoundToInt(AudioListener.volume * 100f)}%";
        }
    }

    private void FlushPendingVolumeSave()
    {
        if (pendingVolumeValue < 0f)
        {
            return;
        }

        volumeSaveDelayTimer -= Time.unscaledDeltaTime;
        if (volumeSaveDelayTimer > 0f)
        {
            return;
        }

        PlayerPrefs.SetFloat("master_volume", pendingVolumeValue);
        PlayerPrefs.Save();
        pendingVolumeValue = -1f;
    }

    private void FlushPendingVolumeSaveImmediate()
    {
        if (pendingVolumeValue < 0f)
        {
            return;
        }

        PlayerPrefs.SetFloat("master_volume", pendingVolumeValue);
        PlayerPrefs.Save();
        pendingVolumeValue = -1f;
        volumeSaveDelayTimer = 0f;
    }

    private void SetOrderPanelVisible(bool visible)
    {
        if (orderPanel == null)
        {
            return;
        }

        if (orderPanelAnimRoutine != null)
        {
            StopCoroutine(orderPanelAnimRoutine);
            orderPanelAnimRoutine = null;
        }

        orderPanelAnimRoutine = StartCoroutine(OrderPanelAnimRoutine(visible));
    }

    private void SetSettingsVisible(bool visible)
    {
        if (settingsPanel != null)
        {
            settingsPanel.gameObject.SetActive(visible);
        }
    }

    private void HandleCloseOrderOutsideTap()
    {
        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    return;
                }
                TryCloseOrderPanelFromScreenPoint(touch.position);
            }
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            TryCloseOrderPanelFromScreenPoint(Input.mousePosition);
        }
    }

    private void HandleCloseSettingsOutsideTap()
    {
        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    return;
                }
                TryCloseSettingsFromScreenPoint(touch.position);
            }
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            TryCloseSettingsFromScreenPoint(Input.mousePosition);
        }
    }

    private void TryCloseOrderPanelFromScreenPoint(Vector2 screenPoint)
    {
        if (orderPanel == null || orderToggleButtonImage == null)
        {
            return;
        }

        var overPanel = RectTransformUtility.RectangleContainsScreenPoint(orderPanel, screenPoint, null);
        var overToggle = RectTransformUtility.RectangleContainsScreenPoint(orderToggleButtonImage.rectTransform, screenPoint, null);
        if (overPanel || overToggle)
        {
            return;
        }

        orderPanelOpen = false;
        SetOrderPanelVisible(false);
        RefreshAll();
    }

    private void TryCloseSettingsFromScreenPoint(Vector2 screenPoint)
    {
        if (settingsPanel == null)
        {
            return;
        }

        var overPanel = RectTransformUtility.RectangleContainsScreenPoint(settingsPanel, screenPoint, null);
        var overToggle = settingsToggleButtonImage != null && RectTransformUtility.RectangleContainsScreenPoint(settingsToggleButtonImage.rectTransform, screenPoint, null);
        if (overPanel || overToggle)
        {
            return;
        }

        settingsOpen = false;
        SetSettingsVisible(false);
        RefreshAll();
    }

    private RectTransform CreatePanel(string name, Vector2 anchor, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(floatRoot, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        return rt;
    }

    private void CreateRoundButton(string label, Vector2 anchor, float size, out Text textComp, out Image imageComp, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label + "RoundButton");
        go.transform.SetParent(floatRoot, false);
        var img = go.AddComponent<Image>();
        img.sprite = circleSprite;
        img.type = Image.Type.Simple;
        img.color = new Color(0.2f, 0.5f, 0.9f, 0.98f);

        var btn = go.AddComponent<Button>();
        ConfigureButtonTransition(btn);
        btn.onClick.AddListener(onClick);

        var rt = img.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);

        var fontSize = size > 170 ? 34 : (size >= 150 ? 31 : 26);
        var txt = CreateLabel(rt, "Text", new Vector2(0.5f, 0.5f), fontSize, TextAnchor.MiddleCenter, Color.white, new Vector2(size * 0.8f, size * 0.8f));
        textComp = txt;
        imageComp = img;
    }

    private void CreatePillButton(Transform parent, string label, Vector2 anchor, Vector2 size, out Text textComp, out Image imageComp, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label + "PillButton");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.69f, 0.78f, 0.98f);

        var btn = go.AddComponent<Button>();
        ConfigureButtonTransition(btn);
        btn.onClick.AddListener(onClick);

        var rt = img.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;

        var txt = CreateLabel(rt, "Text", new Vector2(0.5f, 0.5f), 28, TextAnchor.MiddleCenter, Color.white, new Vector2(size.x - 8f, size.y - 6f));
        textComp = txt;
        imageComp = img;
    }

    private Text CreateLabel(Transform parent, string name, Vector2 anchor, int fontSize, TextAnchor align, Color color, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>(BuiltinFontName);
        txt.fontSize = fontSize;
        txt.alignment = align;
        txt.color = color;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.raycastTarget = false;
        AddTextOutline(txt);

        var rt = txt.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(align == TextAnchor.UpperLeft || align == TextAnchor.MiddleLeft ? 0f : 0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        return txt;
    }

    private Sprite CreateCircleSprite()
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var cols = new Color[size * size];
        var c = new Vector2(size * 0.5f, size * 0.5f);
        var r = size * 0.47f;
        var r2 = r * r;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - c.x;
                var dy = y - c.y;
                cols[y * size + x] = (dx * dx + dy * dy <= r2) ? Color.white : Color.clear;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 128f);
    }

    private Sprite CreateGearSprite()
    {
        const int size = 96;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var cols = new Color[size * size];
        for (var i = 0; i < cols.Length; i++) cols[i] = Color.clear;

        var center = new Vector2(size * 0.5f, size * 0.5f);
        var outerR = size * 0.28f;
        var innerR = size * 0.17f;
        var holeR = size * 0.08f;
        var outerR2 = outerR * outerR;
        var innerR2 = innerR * innerR;
        var holeR2 = holeR * holeR;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - center.x;
                var dy = y - center.y;
                var d2 = dx * dx + dy * dy;
                if (d2 <= outerR2 && d2 >= innerR2)
                {
                    cols[y * size + x] = Color.white;
                }
                if (d2 <= holeR2)
                {
                    cols[y * size + x] = Color.clear;
                }
            }
        }

        // 8 teeth
        FillRect(cols, size, 44, 8, 8, 14, Color.white);
        FillRect(cols, size, 44, 74, 8, 14, Color.white);
        FillRect(cols, size, 8, 44, 14, 8, Color.white);
        FillRect(cols, size, 74, 44, 14, 8, Color.white);
        FillRect(cols, size, 18, 18, 10, 10, Color.white);
        FillRect(cols, size, 68, 18, 10, 10, Color.white);
        FillRect(cols, size, 18, 68, 10, 10, Color.white);
        FillRect(cols, size, 68, 68, 10, 10, Color.white);

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 96f);
    }

    private RectTransform CreateSettingsGearIcon(Transform parent)
    {
        var iconGo = new GameObject("SettingsGearIcon");
        iconGo.transform.SetParent(parent, false);
        var image = iconGo.AddComponent<Image>();
        image.sprite = gearSprite;
        image.color = new Color(0.95f, 0.97f, 1f, 1f);
        image.raycastTarget = false;

        var rt = image.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(48, 48);
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    private void FillRect(Color[] cols, int width, int x, int y, int w, int h, Color color)
    {
        for (var yy = y; yy < y + h; yy++)
        {
            for (var xx = x; xx < x + w; xx++)
            {
                if (xx < 0 || yy < 0 || xx >= width || yy >= width)
                {
                    continue;
                }
                cols[yy * width + xx] = color;
            }
        }
    }

    private void AddTextOutline(Text text)
    {
        var outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
        outline.effectDistance = new Vector2(3f, -3f);
    }

    private void SetButtonState(Image buttonImage, bool enabled, Color enabledColor)
    {
        if (buttonImage == null)
        {
            return;
        }

        var button = buttonImage.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = enabled;
        }

        buttonImage.color = enabled ? enabledColor : new Color(0.2f, 0.22f, 0.26f, 0.82f);
        var text = buttonImage.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.color = enabled ? new Color(1f, 1f, 1f, 1f) : new Color(0.9f, 0.93f, 0.97f, 0.98f);
            text.fontStyle = enabled ? FontStyle.Bold : FontStyle.Normal;
            var outline = text.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = enabled ? new Color(0f, 0f, 0f, 0.7f) : new Color(0f, 0f, 0f, 0.92f);
                outline.effectDistance = enabled ? new Vector2(3f, -3f) : new Vector2(4f, -4f);
            }
        }
    }

    private void ConfigureButtonTransition(Button button)
    {
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
        colors.selectedColor = colors.normalColor;
        colors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 0.9f);
        colors.fadeDuration = 0.06f;
        button.colors = colors;
    }

    private void UpdateClaimButtonPulse(bool ready)
    {
        if (orderClaimButtonImage == null)
        {
            return;
        }

        var rt = orderClaimButtonImage.rectTransform;
        if (!ready)
        {
            rt.localScale = Vector3.one;
            return;
        }

        var pulse = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.05f;
        rt.localScale = new Vector3(pulse, pulse, 1f);
    }

    private void BuildUiAudio()
    {
        uiAudioSource = gameObject.GetComponent<AudioSource>();
        if (uiAudioSource == null)
        {
            uiAudioSource = gameObject.AddComponent<AudioSource>();
        }
        uiAudioSource.playOnAwake = false;
        uiAudioSource.loop = false;
        uiAudioSource.spatialBlend = 0f;
        uiAudioSource.volume = 0.2f;

        uiTapClip = CreateToneClip(900f, 0.04f, 0.07f);
        uiSuccessClip = CreateToneClip(1250f, 0.06f, 0.1f);
    }

    private bool CanAcceptUiPress(ref float lastPressTime, float cooldown)
    {
        var now = Time.unscaledTime;
        if (now - lastPressTime < cooldown)
        {
            return false;
        }
        lastPressTime = now;
        return true;
    }

    private void PlayUiTap()
    {
        if (uiAudioSource != null && uiTapClip != null)
        {
            uiAudioSource.PlayOneShot(uiTapClip, 0.7f);
        }
        TryHapticLight();
    }

    private void PlayUiSuccess()
    {
        if (uiAudioSource != null && uiSuccessClip != null)
        {
            uiAudioSource.PlayOneShot(uiSuccessClip, 1f);
        }
        TryHapticLight();
    }

    private void TryHapticLight()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (!hapticEnabled)
        {
            return;
        }
        if (Time.unscaledTime - lastHapticTime < 0.12f)
        {
            return;
        }
        lastHapticTime = Time.unscaledTime;
        Handheld.Vibrate();
#endif
    }

    private AudioClip CreateToneClip(float freq, float duration, float amp)
    {
        var rate = 44100;
        var sampleCount = Mathf.Max(1, Mathf.RoundToInt(rate * duration));
        var samples = new float[sampleCount];
        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)rate;
            var env = 1f - (i / (float)sampleCount);
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * amp * env;
        }
        var clip = AudioClip.Create($"tone_{freq}_{duration}", sampleCount, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private System.Collections.IEnumerator ToastRoutine()
    {
        var t = 1.2f;
        var baseColor = toastText.color;
        while (t > 0)
        {
            t -= Time.deltaTime;
            var p = 1f - Mathf.Clamp01(t / 1.2f);
            var c = baseColor;
            c.a = Mathf.Lerp(1f, 0.75f, p);
            toastText.color = c;
            yield return null;
        }
        toastText.color = baseColor;
        toastText.text = "";
        toastRoutine = null;
    }

    private System.Collections.IEnumerator FloatRoutine(Text label)
    {
        var rt = label.rectTransform;
        var c = label.color;
        var t = 0f;
        var drift = new Vector2(Random.Range(-20f, 20f), 58f);
        while (t < 0.8f)
        {
            t += Time.deltaTime;
            rt.anchoredPosition += drift * Time.deltaTime;
            label.transform.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one * 0.92f, t / 0.8f);
            c.a = Mathf.Lerp(1f, 0f, t / 0.8f);
            label.color = c;
            yield return null;
        }
        Destroy(label.gameObject);
    }

    public void PulseCoins()
    {
        if (coinsText == null)
        {
            return;
        }

        if (coinsPulseRoutine != null)
        {
            StopCoroutine(coinsPulseRoutine);
        }
        coinsPulseRoutine = StartCoroutine(CoinsPulseRoutine());
    }

    private void UpdateGearSpin()
    {
        if (settingsGearIcon == null)
        {
            return;
        }

        var speed = settingsOpen ? -340f : -110f;
        settingsGearIcon.Rotate(0f, 0f, speed * Time.unscaledDeltaTime);
    }

    private void PlayButtonPop(RectTransform rt)
    {
        if (rt == null)
        {
            return;
        }
        StartCoroutine(ButtonPopRoutine(rt));
    }

    private System.Collections.IEnumerator ButtonPopRoutine(RectTransform rt)
    {
        var baseScale = rt.localScale;
        var down = baseScale * 0.86f;
        var up = baseScale * 1.1f;
        var t = 0f;
        while (t < 0.045f)
        {
            t += Time.unscaledDeltaTime;
            if (rt == null) yield break;
            rt.localScale = Vector3.Lerp(baseScale, down, t / 0.045f);
            yield return null;
        }
        t = 0f;
        while (t < 0.085f)
        {
            t += Time.unscaledDeltaTime;
            if (rt == null) yield break;
            rt.localScale = Vector3.Lerp(down, up, t / 0.085f);
            yield return null;
        }
        t = 0f;
        while (t < 0.07f)
        {
            t += Time.unscaledDeltaTime;
            if (rt == null) yield break;
            rt.localScale = Vector3.Lerp(up, baseScale, t / 0.07f);
            yield return null;
        }
        if (rt != null) rt.localScale = baseScale;
    }

    private System.Collections.IEnumerator CoinsPulseRoutine()
    {
        if (coinsText == null) yield break;
        var rt = coinsText.rectTransform;
        var up = coinsBaseScale * 1.18f;
        var t = 0f;
        while (t < 0.09f)
        {
            t += Time.unscaledDeltaTime;
            if (rt == null) yield break;
            rt.localScale = Vector3.Lerp(coinsBaseScale, up, t / 0.09f);
            yield return null;
        }
        t = 0f;
        while (t < 0.14f)
        {
            t += Time.unscaledDeltaTime;
            if (rt == null) yield break;
            rt.localScale = Vector3.Lerp(up, coinsBaseScale, t / 0.14f);
            yield return null;
        }
        if (rt != null) rt.localScale = coinsBaseScale;
        coinsPulseRoutine = null;
    }

    private System.Collections.IEnumerator OrderPanelAnimRoutine(bool visible)
    {
        if (orderPanel == null) yield break;

        var img = orderPanel.GetComponent<Image>();
        var hiddenPos = orderPanelBasePos + new Vector2(0f, -76f);
        var shownPos = orderPanelBasePos;

        if (visible)
        {
            orderPanel.gameObject.SetActive(true);
            orderPanel.anchoredPosition = hiddenPos;
            if (img != null)
            {
                var c0 = img.color;
                c0.a = 0f;
                img.color = c0;
            }

            var t = 0f;
            while (t < 0.19f)
            {
                t += Time.unscaledDeltaTime;
                var p = Mathf.SmoothStep(0f, 1f, t / 0.19f);
                orderPanel.anchoredPosition = Vector2.Lerp(hiddenPos, shownPos, p);
                if (img != null)
                {
                    var c = img.color;
                    c.a = Mathf.Lerp(0f, 0.38f, p);
                    img.color = c;
                }
                yield return null;
            }
            orderPanel.anchoredPosition = shownPos;
        }
        else
        {
            var startPos = orderPanel.anchoredPosition;
            var startAlpha = img != null ? img.color.a : 0.38f;
            var t = 0f;
            while (t < 0.14f)
            {
                t += Time.unscaledDeltaTime;
                var p = Mathf.SmoothStep(0f, 1f, t / 0.14f);
                orderPanel.anchoredPosition = Vector2.Lerp(startPos, hiddenPos, p);
                if (img != null)
                {
                    var c = img.color;
                    c.a = Mathf.Lerp(startAlpha, 0f, p);
                    img.color = c;
                }
                yield return null;
            }
            orderPanel.gameObject.SetActive(false);
            orderPanel.anchoredPosition = shownPos;
            if (img != null)
            {
                var c = img.color;
                c.a = 0.38f;
                img.color = c;
            }
        }
        orderPanelAnimRoutine = null;
    }
}
