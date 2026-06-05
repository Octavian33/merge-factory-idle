using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIHud : MonoBehaviour
{
    private const string BuiltinFontName = "LegacyRuntime.ttf";

    private Text coinsText;
    private Text incomeText;
    private Text toastText;
    private Text factoryLevelText;
    private Text factoryDetailText;
    private Text statsText;
    private Text woodCountText;
    private Text coalCountText;
    private Text ironCountText;
    private Text copperCountText;

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
    private bool buyAffordable;
    private bool incomeAffordable;
    private bool discountAffordable;

    private RectTransform floatRoot;
    private RectTransform orderPanel;
    private RectTransform settingsPanel;
    private RectTransform factoryCostTooltip;
    private RectTransform settingsGearIcon;
    private RectTransform tutorialPanel;
    private Image factoryProgressFill;
    private Image orderProgressFill;
    private Image nextUnlockIconImage;
    private Text factoryCostTooltipTitleText;
    private Text tutorialText;
    private Slider volumeSlider;
    private Sprite circleSprite;
    private Sprite gearSprite;
    private Sprite coinIconSprite;
    private Sprite woodIconSprite;
    private Sprite coalIconSprite;
    private Sprite ironIconSprite;
    private Sprite copperIconSprite;
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
    private Coroutine tutorialRoutine;
    private Vector3 coinsBaseScale = Vector3.one;
    private bool hapticEnabled = true;
    private int sessionMerges;
    private bool guidedTutorialActive;
    private int guidedTutorialStep;
    private readonly Image[] factoryCostTooltipIcons = new Image[4];
    private readonly Text[] factoryCostTooltipAmounts = new Text[4];

    public void Build()
    {
        EnsureEventSystem();
        var canvas = CreateCanvas();
        floatRoot = canvas.transform as RectTransform;
        circleSprite = CreateCircleSprite();
        gearSprite = CreateGearSprite();
        coinIconSprite = LoadResourceIconSprite("Generated/Icons/ResourceCoin") ?? CreateCoinIconSprite();
        woodIconSprite = LoadResourceIconSprite("Generated/Icons/ResourceWood") ?? CreateMaterialIconSprite(0);
        coalIconSprite = LoadResourceIconSprite("Generated/Icons/ResourceCoal") ?? CreateMaterialIconSprite(1);
        ironIconSprite = LoadResourceIconSprite("Generated/Icons/ResourceIron") ?? CreateMaterialIconSprite(2);
        copperIconSprite = LoadResourceIconSprite("Generated/Icons/ResourceCopper") ?? CreateMaterialIconSprite(3);

        CreateTopStrip();
        CreateBottomControls();
        CreateOrderUI();
        CreateSettingsUI();
        CreateTutorialUI();
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
        sessionMerges = 0;
        TryShowFirstTimeTutorial();
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
        var factoryCost = gm.GetFactoryUpgradeCost();
        var canUpgradeFactory = gm.State.factoryLevel < ChapterOneData.MaxFactoryLevel && gm.CanAfford(factoryCost);
        var canAutomate = gm.CanAutomateChapterOne();
        var nextUnlockLabel = BuildNextUnlockLabel(gm.State.factoryLevel);

        coinsText.text = eco.Format(gm.State.coins);
        incomeText.text = $"Income/s: {eco.Format(eco.CurrentIncomePerSecond)}";
        if (woodCountText != null) woodCountText.text = eco.Format(gm.State.wood);
        if (coalCountText != null) coalCountText.text = eco.Format(gm.State.coal);
        if (ironCountText != null) ironCountText.text = eco.Format(gm.State.iron);
        if (copperCountText != null) copperCountText.text = eco.Format(gm.State.copper);
        if (factoryLevelText != null)
        {
            factoryLevelText.text = $"Factory Lv {gm.State.factoryLevel}";
        }
        if (factoryDetailText != null)
        {
            var storageBonusPct = Mathf.RoundToInt((ChapterOneData.GetStorageBonusMultiplier(gm.State.factoryLevel) - 1f) * 100f);
            factoryDetailText.text = $"{gm.GetCurrentFactoryObjective()}   |   {nextUnlockLabel}   |   Storage +{storageBonusPct}%";
        }
        if (factoryProgressFill != null)
        {
            factoryProgressFill.fillAmount = gm.GetFactoryProgressRatio();
        }

        buyButtonText.text = $"Hire\n{eco.Format(buyCost)}";
        incomeUpgradeButtonText.text = BuildFactoryUpgradeButtonText(gm, eco, factoryCost);
        discountUpgradeButtonText.text = BuildInfoButtonText(gm.State.factoryLevel, gm.State.chapterOneAutomated);
        prestigeButtonText.text = BuildAutomationButtonText(gm);
        if (nextUnlockIconImage != null)
        {
            nextUnlockIconImage.sprite = GetNextUnlockIcon(gm.State.factoryLevel);
            nextUnlockIconImage.color = gm.State.chapterOneAutomated
                ? new Color(0.76f, 0.92f, 0.78f, 1f)
                : new Color(0.96f, 0.94f, 0.86f, 1f);
        }

        orderToggleText.text = order.IsReadyToClaim ? "Order !" : "Order";
        var progressPct = order.Target > 0 ? Mathf.RoundToInt((order.Progress / (float)order.Target) * 100f) : 0;
        var rewardText = eco.Format(order.CurrentReward);
        orderTitleText.text = order.Description;
        orderProgressText.text = $"{order.BuildRequirementSummary(eco.Format)}\nStored {order.Progress}/{order.Target}  ({progressPct}%)";
        orderProgressText.color = order.IsReadyToClaim ? new Color(0.62f, 1f, 0.66f) : new Color(0.95f, 1f, 0.92f);
        orderTimerText.text = order.IsReadyToClaim ? $"Ship now for +{rewardText}" : $"Time {order.TimeLeftSeconds / 60:00}:{order.TimeLeftSeconds % 60:00}";
        orderClaimText.text = order.IsReadyToClaim ? $"Ship\n+{rewardText}" : "Wait";
        if (orderProgressFill != null)
        {
            orderProgressFill.fillAmount = order.Progress01;
        }

        if (volumeValueText != null)
        {
            volumeValueText.text = $"Volume: {Mathf.RoundToInt(AudioListener.volume * 100f)}%";
        }

        if (statsText != null)
        {
            statsText.text = $"Merges: {sessionMerges}   Orders: {gm.State.completedOrders}   Workers: {gm.WorkerBoard.WorkerCount}";
        }

        buyAffordable = gm.State.coins >= buyCost;
        incomeAffordable = canUpgradeFactory;
        discountAffordable = canAutomate;

        SetButtonState(buyButtonImage, buyAffordable, new Color(0.99f, 0.62f, 0.24f, 0.98f));
        SetButtonState(incomeUpgradeButtonImage, incomeAffordable, new Color(0.22f, 0.69f, 0.78f, 0.98f));
        SetButtonState(discountUpgradeButtonImage, true, gm.State.chapterOneAutomated ? new Color(0.29f, 0.64f, 0.42f, 0.98f) : new Color(0.19f, 0.63f, 0.72f, 0.98f));
        SetButtonState(prestigeButtonImage, gm.State.factoryLevel >= ChapterOneData.MaxFactoryLevel, gm.State.chapterOneAutomated ? new Color(0.22f, 0.68f, 0.4f, 0.98f) : new Color(0.58f, 0.42f, 0.84f, 0.98f));
        SetButtonState(orderToggleButtonImage, true, order.IsReadyToClaim ? new Color(0.23f, 0.7f, 0.34f, 0.98f) : new Color(0.14f, 0.52f, 0.75f, 0.98f));
        SetButtonState(orderClaimButtonImage, order.IsReadyToClaim, new Color(0.2f, 0.72f, 0.4f, 0.98f));
        UpdateClaimButtonPulse(order.IsReadyToClaim);
        UpdateAffordablePulse();
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

    private void TryShowFirstTimeTutorial()
    {
        if (tutorialPanel == null || tutorialText == null)
        {
            return;
        }

        if (PlayerPrefs.GetInt("tutorial_guided_seen_v1", 0) == 1)
        {
            tutorialPanel.gameObject.SetActive(false);
            guidedTutorialActive = false;
            return;
        }

        guidedTutorialActive = true;
        guidedTutorialStep = 0;
        tutorialPanel.gameObject.SetActive(true);
        ShowGuidedStep();
    }

    private enum TutorialAction
    {
        BoughtWorker,
        MergedWorker,
        OpenedOrder
    }

    private void NotifyTutorialAction(TutorialAction action)
    {
        if (!guidedTutorialActive)
        {
            return;
        }

        if (guidedTutorialStep == 0 && action == TutorialAction.BoughtWorker)
        {
            guidedTutorialStep = 1;
            ShowGuidedStep();
            return;
        }

        if (guidedTutorialStep == 1 && action == TutorialAction.MergedWorker)
        {
            guidedTutorialStep = 2;
            ShowGuidedStep();
            return;
        }

        if (guidedTutorialStep == 2 && action == TutorialAction.OpenedOrder)
        {
            CompleteGuidedTutorial();
        }
    }

    private void ShowGuidedStep()
    {
        if (!guidedTutorialActive || tutorialText == null || tutorialPanel == null)
        {
            return;
        }

        tutorialPanel.gameObject.SetActive(true);
        var panelImage = tutorialPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = new Color(0.03f, 0.08f, 0.14f, 0.72f);
        }
        tutorialText.color = new Color(0.95f, 1f, 0.9f, 1f);

        if (guidedTutorialStep == 0)
        {
            tutorialText.text = "Step 1/3: Tap BUY to hire a worker";
        }
        else if (guidedTutorialStep == 1)
        {
            tutorialText.text = "Step 2/3: Drag identical workers to MERGE";
        }
        else if (guidedTutorialStep == 2)
        {
            tutorialText.text = "Step 3/3: Open ORDER to check rewards";
        }
    }

    private void CompleteGuidedTutorial()
    {
        guidedTutorialActive = false;
        if (tutorialPanel != null)
        {
            tutorialPanel.gameObject.SetActive(false);
        }
        PlayerPrefs.SetInt("tutorial_guided_seen_v1", 1);
        PlayerPrefs.Save();
        ShowToast("Tutorial complete");
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
        var panel = CreatePanel("TopStrip", new Vector2(0.5f, 1f), new Vector2(836, 236), new Color(0.07f, 0.15f, 0.17f, 0.34f));
        panel.pivot = new Vector2(0.5f, 1f);

        CreateResourceDisplay(panel, "CoinRes", coinIconSprite, new Color(1f, 0.9f, 0.4f, 1f), new Vector2(0.02f, 0.82f), out coinsText, true);
        CreateResourceDisplay(panel, "WoodRes", woodIconSprite, new Color(0.58f, 0.8f, 0.52f, 1f), new Vector2(0.22f, 0.82f), out woodCountText, true);
        CreateResourceDisplay(panel, "CoalRes", coalIconSprite, new Color(0.4f, 0.44f, 0.5f, 1f), new Vector2(0.42f, 0.82f), out coalCountText, true);
        CreateResourceDisplay(panel, "IronRes", ironIconSprite, new Color(0.56f, 0.74f, 0.9f, 1f), new Vector2(0.62f, 0.82f), out ironCountText, true);
        CreateResourceDisplay(panel, "CopperRes", copperIconSprite, new Color(0.95f, 0.63f, 0.38f, 1f), new Vector2(0.82f, 0.82f), out copperCountText, true);
        incomeText = CreateLabel(panel, "Income", new Vector2(0.05f, 0.59f), 28, TextAnchor.MiddleLeft, new Color(0.96f, 1f, 0.78f), new Vector2(250, 42));
        toastText = CreateLabel(panel, "", new Vector2(0.85f, 0.59f), 24, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.35f), new Vector2(170, 42));
        factoryLevelText = CreateLabel(panel, "FactoryLevel", new Vector2(0.05f, 0.42f), 24, TextAnchor.MiddleLeft, new Color(1f, 0.96f, 0.82f), new Vector2(220, 36));
        factoryDetailText = CreateLabel(panel, "FactoryDetail", new Vector2(0.05f, 0.3f), 16, TextAnchor.MiddleLeft, new Color(0.88f, 0.96f, 1f), new Vector2(710, 28));
        CreateBar(panel, "FactoryProgress", new Vector2(0.05f, 0.17f), new Vector2(710, 18), new Color(0.08f, 0.19f, 0.25f, 0.72f), new Color(0.98f, 0.72f, 0.28f, 1f), out factoryProgressFill);
        statsText = CreateLabel(panel, "Stats", new Vector2(0.05f, 0.05f), 17, TextAnchor.MiddleLeft, new Color(0.88f, 0.96f, 1f), new Vector2(710, 26));
        coinsBaseScale = coinsText.rectTransform.localScale;
    }

    private void CreateResourceDisplay(Transform parent, string name, Sprite iconSprite, Color tint, Vector2 anchor, out Text countText, bool compactTopRow = false)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = compactTopRow ? new Vector2(152f, 56f) : new Vector2(176f, 48f);

        var plate = new GameObject("Plate");
        plate.transform.SetParent(root.transform, false);
        var plateImg = plate.AddComponent<Image>();
        plateImg.color = new Color(0.09f, 0.18f, 0.2f, 0.45f);
        plateImg.raycastTarget = false;
        var prt = plateImg.rectTransform;
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(root.transform, false);
        var icon = iconGo.AddComponent<Image>();
        icon.sprite = iconSprite;
        icon.color = tint;
        icon.raycastTarget = false;
        var irt = icon.rectTransform;
        irt.anchorMin = new Vector2(0f, 0.5f);
        irt.anchorMax = new Vector2(0f, 0.5f);
        irt.pivot = new Vector2(0f, 0.5f);
        irt.sizeDelta = compactTopRow ? new Vector2(64f, 64f) : new Vector2(40f, 40f);
        irt.anchoredPosition = Vector2.zero;

        countText = CreateLabel(root.transform, "Count", new Vector2(0f, 0.5f), compactTopRow ? 28 : 26, TextAnchor.MiddleLeft, Color.white, compactTopRow ? new Vector2(82f, 46f) : new Vector2(124f, 40f));
        countText.rectTransform.anchoredPosition = compactTopRow ? new Vector2(70f, 0f) : new Vector2(50f, 0f);
    }

    private void CreateBar(Transform parent, string name, Vector2 anchor, Vector2 size, Color backgroundColor, Color fillColor, out Image fillImage)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = anchor;
        rootRt.anchorMax = anchor;
        rootRt.pivot = new Vector2(0f, 0.5f);
        rootRt.sizeDelta = size;

        var bg = root.AddComponent<Image>();
        bg.color = backgroundColor;
        bg.raycastTarget = false;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(root.transform, false);
        fillImage = fillGo.AddComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillAmount = 0f;
        fillImage.raycastTarget = false;
        var fillRt = fillImage.rectTransform;
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(1f, 1f);
        fillRt.offsetMin = new Vector2(2f, 2f);
        fillRt.offsetMax = new Vector2(-2f, -2f);
    }

    private void CreateTutorialUI()
    {
        tutorialPanel = CreatePanel("TutorialPanel", new Vector2(0.5f, 0.28f), new Vector2(860, 116), new Color(0.03f, 0.08f, 0.14f, 0.6f));
        tutorialPanel.GetComponent<Image>().raycastTarget = false;
        tutorialText = CreateLabel(tutorialPanel, "TutorialText", new Vector2(0.5f, 0.5f), 30, TextAnchor.MiddleCenter, new Color(0.95f, 1f, 0.9f), new Vector2(780, 90));
        tutorialPanel.gameObject.SetActive(false);
    }

    private void CreateBottomControls()
    {
        CreatePillButton(floatRoot, "Factory", new Vector2(0.24f, 0.068f), new Vector2(224, 108), out incomeUpgradeButtonText, out incomeUpgradeButtonImage, () =>
        {
            PlayButtonPop(incomeUpgradeButtonImage.rectTransform);
            PlayUiTap();
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.Economy != null)
            {
                ShowFactoryCostTooltip(gm.GetFactoryUpgradeCost(), gm.Economy);
            }
            if (!gm.TryUpgradeFactory()) ShowToast("Need resources for upgrade");
            RefreshAll();
        });
        incomeUpgradeButtonText.fontSize = 30;
        incomeUpgradeButtonText.rectTransform.sizeDelta = new Vector2(190f, 90f);
        CreateFactoryCostTooltip();

        CreateRoundButton("Buy", new Vector2(0.50f, 0.06f), 168, out buyButtonText, out buyButtonImage, () =>
        {
            if (!CanAcceptUiPress(ref lastBuyPressTime, 0.14f)) return;
            PlayButtonPop(buyButtonImage.rectTransform);
            PlayUiTap();
            var gm = GameManager.Instance;
            if (gm == null || gm.Economy == null) return;
            if (!gm.Economy.TryBuyWorker()) ShowToast("Need coins or free slot");
            else NotifyTutorialAction(TutorialAction.BoughtWorker);
            RefreshAll();
        });

        CreatePillButton(floatRoot, "Info", new Vector2(0.76f, 0.068f), new Vector2(224, 108), out discountUpgradeButtonText, out discountUpgradeButtonImage, () =>
        {
            PlayButtonPop(discountUpgradeButtonImage.rectTransform);
            PlayUiTap();
            var gm = GameManager.Instance;
            if (gm == null) return;
            ShowToast(gm.GetCurrentFactoryObjective());
            RefreshAll();
        });
        discountUpgradeButtonText.fontSize = 24;
        discountUpgradeButtonText.rectTransform.sizeDelta = new Vector2(174f, 82f);
        discountUpgradeButtonText.rectTransform.anchoredPosition = new Vector2(14f, -4f);
        nextUnlockIconImage = CreateButtonIcon(discountUpgradeButtonImage.rectTransform, gearSprite, new Color(0.96f, 0.94f, 0.86f, 1f), new Vector2(-78f, 0f), new Vector2(34f, 34f));

        CreateRoundButton("Auto", new Vector2(0.905f, 0.205f), 153, out prestigeButtonText, out prestigeButtonImage, () =>
        {
            if (!CanAcceptUiPress(ref lastPrestigePressTime, 0.2f)) return;
            PlayButtonPop(prestigeButtonImage.rectTransform);
            PlayUiTap();
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (!gm.TryAutomateChapterOne())
            {
                ShowToast(gm.State.factoryLevel < ChapterOneData.MaxFactoryLevel
                    ? "Reach Factory Lv20 first"
                    : $"Need {gm.FormatResourceCost(ChapterOneData.GetAutomationCost())}");
            }
            RefreshAll();
        });
        prestigeButtonImage.color = new Color(0.44f, 0.38f, 0.78f, 0.98f);
    }

    private void CreateOrderUI()
    {
        CreateRoundButton("Order", new Vector2(0.095f, 0.205f), 153, out orderToggleText, out orderToggleButtonImage, () =>
        {
            PlayButtonPop(orderToggleButtonImage.rectTransform);
            PlayUiTap();
            orderPanelOpen = !orderPanelOpen;
            SetOrderPanelVisible(orderPanelOpen);
            if (orderPanelOpen) NotifyTutorialAction(TutorialAction.OpenedOrder);
            RefreshAll();
        });
        orderToggleButtonImage.color = new Color(0.19f, 0.58f, 0.72f, 0.98f);

        orderPanel = CreatePanel("OrderPanel", new Vector2(0.5f, 0.315f), new Vector2(860, 184), new Color(0.08f, 0.16f, 0.12f, 0.64f));
        orderPanelBasePos = orderPanel.anchoredPosition;
        orderTitleText = CreateLabel(orderPanel, "OrderTitle", new Vector2(0.03f, 0.78f), 34, TextAnchor.MiddleLeft, new Color(0.95f, 1f, 0.92f), new Vector2(560, 52));
        CreateBar(orderPanel, "OrderProgressBar", new Vector2(0.03f, 0.5f), new Vector2(560, 20), new Color(0.08f, 0.18f, 0.16f, 0.92f), new Color(0.38f, 0.88f, 0.5f, 1f), out orderProgressFill);
        orderProgressText = CreateLabel(orderPanel, "OrderProgress", new Vector2(0.03f, 0.34f), 24, TextAnchor.MiddleLeft, new Color(0.95f, 1f, 0.92f), new Vector2(560, 64));
        orderTimerText = CreateLabel(orderPanel, "OrderTimer", new Vector2(0.03f, 0.12f), 26, TextAnchor.MiddleLeft, new Color(0.95f, 1f, 0.92f), new Vector2(560, 40));

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
        CreateRoundButton("SET", new Vector2(0.92f, 0.06f), 120, out settingsButtonText, out settingsToggleButtonImage, () =>
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

        settingsPanel = CreatePanel("SettingsPanel", new Vector2(0.5f, 0.54f), new Vector2(790, 610), new Color(0.05f, 0.09f, 0.16f, 0.94f));
        settingsPanel.GetComponent<Image>().raycastTarget = true;

        CreateLabel(settingsPanel, "SettingsTitle", new Vector2(0.5f, 0.9f), 56, TextAnchor.MiddleCenter, Color.white, new Vector2(420, 84)).text = "Settings";

        volumeValueText = CreateLabel(settingsPanel, "VolumeLabel", new Vector2(0.5f, 0.77f), 38, TextAnchor.MiddleCenter, new Color(0.95f, 1f, 0.95f), new Vector2(420, 64));

        volumeSlider = CreateSlider(settingsPanel, new Vector2(0.5f, 0.68f), new Vector2(560, 50));
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.wholeNumbers = false;
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        Text resetText;
        Image resetImage;
        CreatePillButton(settingsPanel, "Reset", new Vector2(0.5f, 0.48f), new Vector2(560, 82), out resetText, out resetImage, () =>
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
        CreatePillButton(settingsPanel, "Exit", new Vector2(0.5f, 0.34f), new Vector2(560, 82), out exitText, out exitImage, () =>
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
        CreatePillButton(settingsPanel, "Close", new Vector2(0.5f, 0.08f), new Vector2(560, 78), out closeText, out closeImage, () =>
        {
            PlayUiTap();
            settingsOpen = false;
            SetSettingsVisible(false);
            RefreshAll();
        });
        closeText.text = "Close";
        closeImage.color = new Color(0.35f, 0.64f, 0.48f, 0.98f);

        CreatePillButton(settingsPanel, "Haptic", new Vector2(0.5f, 0.58f), new Vector2(560, 82), out hapticToggleText, out var hapticImage, () =>
        {
            hapticEnabled = !hapticEnabled;
            PlayerPrefs.SetInt("haptic_enabled", hapticEnabled ? 1 : 0);
            PlayerPrefs.Save();
            hapticToggleText.text = hapticEnabled ? "Haptic: ON" : "Haptic: OFF";
            ShowToast(hapticEnabled ? "Haptic enabled" : "Haptic disabled");
        });
        hapticImage.color = new Color(0.34f, 0.58f, 0.82f, 0.98f);
    }

    private string BuildFactoryUpgradeButtonText(GameManager gm, EconomySystem eco, ResourceCost cost)
    {
        if (gm.State.factoryLevel >= ChapterOneData.MaxFactoryLevel)
        {
            return "Factory\nMAXED";
        }

        return $"Factory\nLv {gm.State.factoryLevel + 1}";
    }

    private string BuildInfoButtonText(int factoryLevel, bool automated)
    {
        if (automated)
        {
            return "Region\nAutomated";
        }

        if (factoryLevel >= ChapterOneData.MaxFactoryLevel)
        {
            return "Automation\nReady";
        }

        return $"Next Unlock\n{BuildNextUnlockName(factoryLevel)}";
    }

    private string BuildAutomationButtonText(GameManager gm)
    {
        if (gm.State.chapterOneAutomated)
        {
            return "Auto\nON";
        }

        if (gm.State.factoryLevel < ChapterOneData.MaxFactoryLevel)
        {
            return "Auto\nLv20";
        }

        return "Automate";
    }

    private string BuildNextUnlockLabel(int factoryLevel)
    {
        if (factoryLevel < 3) return "Coal @3";
        if (factoryLevel < 4) return "Storage @4";
        if (factoryLevel < 5) return "Iron @5";
        if (factoryLevel < 7) return "Copper @7";
        return "Auto @20";
    }

    private string BuildNextUnlockName(int factoryLevel)
    {
        if (factoryLevel < 3) return "Coal Mine";
        if (factoryLevel < 4) return "Storage";
        if (factoryLevel < 5) return "Iron Workshop";
        if (factoryLevel < 7) return "Copper Workshop";
        return "Automation";
    }

    private Sprite GetNextUnlockIcon(int factoryLevel)
    {
        if (factoryLevel < 3) return coalIconSprite;
        if (factoryLevel < 4) return woodIconSprite;
        if (factoryLevel < 5) return ironIconSprite;
        if (factoryLevel < 7) return copperIconSprite;
        return gearSprite;
    }

    private string BuildCompactCost(ResourceCost cost, EconomySystem eco)
    {
        var parts = string.Empty;
        if (cost.wood > 0) parts += $"W{eco.Format(cost.wood)} ";
        if (cost.coal > 0) parts += $"C{eco.Format(cost.coal)} ";
        if (cost.iron > 0) parts += $"I{eco.Format(cost.iron)} ";
        if (cost.copper > 0) parts += $"Cu{eco.Format(cost.copper)}";
        return parts.Trim();
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

    private void CreateFactoryCostTooltip()
    {
        factoryCostTooltip = CreatePanel("FactoryCostTooltip", new Vector2(0.24f, 0.16f), new Vector2(300, 114), new Color(0.05f, 0.1f, 0.14f, 0.92f));
        factoryCostTooltip.GetComponent<Image>().raycastTarget = false;
        factoryCostTooltipTitleText = CreateLabel(factoryCostTooltip, "FactoryCostTitle", new Vector2(0.5f, 0.78f), 24, TextAnchor.MiddleCenter, new Color(1f, 0.97f, 0.85f), new Vector2(240, 34));
        factoryCostTooltipTitleText.text = "Upgrade Cost";

        CreateInlineCostEntry(factoryCostTooltip, 0, woodIconSprite, new Color(0.58f, 0.8f, 0.52f), new Vector2(0.13f, 0.34f));
        CreateInlineCostEntry(factoryCostTooltip, 1, coalIconSprite, new Color(0.4f, 0.44f, 0.5f), new Vector2(0.38f, 0.34f));
        CreateInlineCostEntry(factoryCostTooltip, 2, ironIconSprite, new Color(0.56f, 0.74f, 0.9f), new Vector2(0.63f, 0.34f));
        CreateInlineCostEntry(factoryCostTooltip, 3, copperIconSprite, new Color(0.95f, 0.63f, 0.38f), new Vector2(0.83f, 0.34f));
        factoryCostTooltip.gameObject.SetActive(false);
    }

    private void CreateInlineCostEntry(Transform parent, int index, Sprite iconSprite, Color tint, Vector2 anchor)
    {
        var root = new GameObject($"CostEntry{index}");
        root.transform.SetParent(parent, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(66f, 44f);

        var icon = CreateButtonIcon(root.transform, iconSprite, tint, new Vector2(0f, 10f), new Vector2(24f, 24f));
        factoryCostTooltipIcons[index] = icon;

        var amount = CreateLabel(root.transform, "Amount", new Vector2(0.5f, 0.05f), 18, TextAnchor.MiddleCenter, Color.white, new Vector2(64f, 24f));
        amount.rectTransform.anchoredPosition = new Vector2(0f, -8f);
        factoryCostTooltipAmounts[index] = amount;
    }

    private Image CreateButtonIcon(Transform parent, Sprite sprite, Color tint, Vector2 anchoredPos, Vector2 size)
    {
        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(parent, false);
        var icon = iconGo.AddComponent<Image>();
        icon.sprite = sprite;
        icon.color = tint;
        icon.raycastTarget = false;
        var rt = icon.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        return icon;
    }

    private void ShowFactoryCostTooltip(ResourceCost cost, EconomySystem eco)
    {
        if (factoryCostTooltip == null)
        {
            return;
        }

        factoryCostTooltip.gameObject.SetActive(true);
        factoryCostTooltipTitleText.text = "Upgrade Cost";
        UpdateCostEntry(0, cost.wood, eco, new Color(0.58f, 0.8f, 0.52f));
        UpdateCostEntry(1, cost.coal, eco, new Color(0.4f, 0.44f, 0.5f));
        UpdateCostEntry(2, cost.iron, eco, new Color(0.56f, 0.74f, 0.9f));
        UpdateCostEntry(3, cost.copper, eco, new Color(0.95f, 0.63f, 0.38f));

        StopCoroutine(nameof(HideFactoryCostTooltipRoutine));
        StartCoroutine(nameof(HideFactoryCostTooltipRoutine));
    }

    private void UpdateCostEntry(int index, int amount, EconomySystem eco, Color tint)
    {
        if (index < 0 || index >= factoryCostTooltipIcons.Length)
        {
            return;
        }

        var visible = amount > 0;
        if (factoryCostTooltipIcons[index] != null)
        {
            factoryCostTooltipIcons[index].gameObject.SetActive(visible);
            factoryCostTooltipIcons[index].color = tint;
        }

        if (factoryCostTooltipAmounts[index] != null)
        {
            factoryCostTooltipAmounts[index].gameObject.SetActive(visible);
            factoryCostTooltipAmounts[index].text = eco.Format(amount);
        }
    }

    private System.Collections.IEnumerator HideFactoryCostTooltipRoutine()
    {
        yield return new WaitForSecondsRealtime(1.35f);
        if (factoryCostTooltip != null)
        {
            factoryCostTooltip.gameObject.SetActive(false);
        }
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
        ApplyPanelStyle(rt);
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
        ApplyButton3DStyle(rt, true);

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
        ApplyButton3DStyle(rt, false);

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

    private Sprite CreateMaterialIconSprite(int materialType)
    {
        const int size = 24;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var cols = new Color[size * size];
        for (var i = 0; i < cols.Length; i++) cols[i] = Color.clear;

        switch (materialType)
        {
            case 0: // wood log
                FillRect(cols, size, 3, 8, 18, 8, Color.white);
                FillRect(cols, size, 1, 10, 3, 4, Color.white);
                FillRect(cols, size, 20, 10, 3, 4, Color.white);
                break;
            case 1: // coal lump
                FillRect(cols, size, 6, 6, 12, 12, Color.white);
                FillRect(cols, size, 4, 10, 3, 4, Color.white);
                FillRect(cols, size, 17, 10, 3, 4, Color.white);
                break;
            case 2: // iron ingot
                FillRect(cols, size, 4, 7, 16, 10, Color.white);
                FillRect(cols, size, 6, 9, 12, 2, new Color(1f, 1f, 1f, 0.6f));
                break;
            default: // copper chunk
                FillRect(cols, size, 5, 6, 14, 12, Color.white);
                FillRect(cols, size, 8, 8, 8, 2, new Color(1f, 1f, 1f, 0.65f));
                break;
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 24f);
    }

    private Sprite CreateCoinIconSprite()
    {
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var cols = new Color[size * size];
        for (var i = 0; i < cols.Length; i++) cols[i] = Color.clear;

        var gold = new Color(1f, 0.8f, 0.18f, 1f);
        var dark = new Color(0.77f, 0.53f, 0.05f, 1f);
        var shine = new Color(1f, 0.95f, 0.58f, 1f);
        FillRect(cols, size, 7, 8, 18, 12, dark);
        FillRect(cols, size, 5, 10, 18, 12, gold);
        FillRect(cols, size, 9, 6, 18, 12, dark);
        FillRect(cols, size, 7, 8, 18, 12, gold);
        FillRect(cols, size, 12, 11, 8, 2, shine);
        FillRect(cols, size, 14, 14, 5, 2, shine);
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    private Sprite LoadResourceIconSprite(string resourcePath)
    {
        var sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            return sprite;
        }

        var texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            return null;
        }

        texture.filterMode = FilterMode.Bilinear;
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            256f,
            0,
            SpriteMeshType.FullRect);
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
        rt.localScale = Vector3.one;
    }

    private void UpdateAffordablePulse()
    {
        if (buyButtonImage != null)
        {
            buyButtonImage.rectTransform.localScale = Vector3.one;
        }

        if (buyAffordable)
        {
            buyButtonText.text = $"{buyButtonText.text.Split('\n')[0]}\n{buyButtonText.text.Split('\n')[1]}\nREADY";
        }
        else
        {
            if (buyButtonText != null && buyButtonText.text.EndsWith("\nREADY"))
            {
                buyButtonText.text = buyButtonText.text.Replace("\nREADY", "");
            }
        }

        if (incomeUpgradeButtonImage != null)
        {
            incomeUpgradeButtonImage.rectTransform.localScale = Vector3.one;
        }

        if (discountUpgradeButtonImage != null)
        {
            discountUpgradeButtonImage.rectTransform.localScale = Vector3.one;
        }
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
        // Disabled by request: keep coin text fixed (no tremble/scale pulse).
        return;
    }

    public void NotifyMerge()
    {
        sessionMerges++;
        NotifyTutorialAction(TutorialAction.MergedWorker);
        if (statsText != null)
        {
            RefreshAll();
        }
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
        // Buttons remain fixed in place; 3D feel is handled by static layered styling.
        return;
    }

    private void ApplyButton3DStyle(RectTransform buttonRt, bool round)
    {
        if (buttonRt == null)
        {
            return;
        }

        var shadowGo = new GameObject("Shadow3D");
        shadowGo.transform.SetParent(buttonRt, false);
        shadowGo.transform.SetAsFirstSibling();
        var shadow = shadowGo.AddComponent<Image>();
        shadow.raycastTarget = false;
        shadow.color = new Color(0f, 0f, 0f, 0.28f);
        var srt = shadow.rectTransform;
        srt.anchorMin = Vector2.zero;
        srt.anchorMax = Vector2.one;
        srt.offsetMin = new Vector2(round ? 6f : 4f, -8f);
        srt.offsetMax = new Vector2(round ? 6f : 4f, -8f);
        if (round)
        {
            shadow.sprite = circleSprite;
        }

        var highlightGo = new GameObject("Highlight3D");
        highlightGo.transform.SetParent(buttonRt, false);
        highlightGo.transform.SetAsFirstSibling();
        var highlight = highlightGo.AddComponent<Image>();
        highlight.raycastTarget = false;
        highlight.color = new Color(1f, 1f, 1f, 0.2f);
        var hrt = highlight.rectTransform;
        hrt.anchorMin = new Vector2(0.08f, 0.58f);
        hrt.anchorMax = new Vector2(0.92f, 0.92f);
        hrt.offsetMin = Vector2.zero;
        hrt.offsetMax = Vector2.zero;
        if (round)
        {
            highlight.sprite = circleSprite;
        }
    }

    private void ApplyPanelStyle(RectTransform panelRt)
    {
        if (panelRt == null)
        {
            return;
        }

        var shadowGo = new GameObject("PanelShadow");
        shadowGo.transform.SetParent(panelRt, false);
        shadowGo.transform.SetAsFirstSibling();
        var shadow = shadowGo.AddComponent<Image>();
        shadow.color = new Color(0f, 0f, 0f, 0.22f);
        shadow.raycastTarget = false;
        var srt = shadow.rectTransform;
        srt.anchorMin = Vector2.zero;
        srt.anchorMax = Vector2.one;
        srt.offsetMin = new Vector2(8f, -10f);
        srt.offsetMax = new Vector2(8f, -10f);

        var glossGo = new GameObject("PanelGloss");
        glossGo.transform.SetParent(panelRt, false);
        glossGo.transform.SetAsFirstSibling();
        var gloss = glossGo.AddComponent<Image>();
        gloss.color = new Color(1f, 1f, 1f, 0.04f);
        gloss.raycastTarget = false;
        var grt = gloss.rectTransform;
        grt.anchorMin = new Vector2(0.02f, 0.62f);
        grt.anchorMax = new Vector2(0.98f, 0.95f);
        grt.offsetMin = Vector2.zero;
        grt.offsetMax = Vector2.zero;
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
