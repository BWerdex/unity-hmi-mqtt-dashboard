using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Builds the MainHMI scene matching the Figma "Modern Car Gauge Cluster" design.
/// Layout: [Speed circle] [Nav panel] [RPM circle]
///         [Fuel bar] [Temp bar] [Battery bar] [Drive Mode]
///         [Status bar]
/// Run via: HMI → Build Scene
/// </summary>
public static class HMISceneSetup
{
    // ── Design tokens (from Figma) ────────────────────────────────────────────
    private static readonly Color C_BG         = Hex("000000");
    private static readonly Color C_PANEL      = Hex("0D1020");
    private static readonly Color C_TRACK      = Hex("2C3140");
    private static readonly Color C_NAV_BG     = Hex("0D1B2E");
    private static readonly Color C_WHITE      = Hex("FFFFFF");
    private static readonly Color C_GREY       = Hex("888888");
    private static readonly Color C_SPEED_ARC  = Hex("4B9EF8"); // blue
    private static readonly Color C_RPM_ARC    = Hex("00D4A0"); // teal
    private static readonly Color C_FUEL_BAR   = Hex("F5A623"); // amber
    private static readonly Color C_TEMP_BAR   = Hex("4FC3F7"); // cyan
    private static readonly Color C_BATT_BAR   = Hex("00C853"); // green
    private static readonly Color C_FUEL_ICON  = Hex("5A3200");
    private static readonly Color C_TEMP_ICON  = Hex("0A2040");
    private static readonly Color C_BATT_ICON  = Hex("0A3020");
    private static readonly Color C_MODE_ICON  = Hex("2A1040");
    private static readonly Color C_STATUS_DOT = Hex("00C853");

    [MenuItem("HMI/Build Scene")]
    public static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Camera ───────────────────────────────────────────────────────────
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = C_BG;
        cam.orthographic = false;
        camGO.AddComponent<AudioListener>();

        // ── Canvas ───────────────────────────────────────────────────────────
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var ct = canvasGO.transform;

        // ── Full background ───────────────────────────────────────────────────
        MakeImage(ct, "Background", AnchorPreset.Stretch, Vector2.zero, Vector2.zero, C_BG);

        // ── Top row ───────────────────────────────────────────────────────────
        // Speed circle  (left)
        var speedPanel = MakeContainer(ct, "SpeedPanel", new Vector2(-610f, 80f), new Vector2(340f, 340f));
        var speedRefs = BuildCircleGauge(speedPanel.transform, C_SPEED_ARC, "km/h");

        // RPM circle (right)
        var rpmPanel = MakeContainer(ct, "RPMPanel", new Vector2(610f, 80f), new Vector2(340f, 340f));
        var rpmRefs = BuildCircleGauge(rpmPanel.transform, C_RPM_ARC, "x1000 RPM");

        // Nav panel (center)
        BuildNavPanel(ct);

        // ── Bottom row — 4 items ──────────────────────────────────────────────
        float barY = -340f;
        float[] barX = { -700f, -230f, 240f, 710f };
        const float BAR_W = 390f, BAR_H = 160f;

        var fuelPanel  = MakeContainer(ct, "FuelPanel",        new Vector2(barX[0], barY), new Vector2(BAR_W, BAR_H));
        var tempPanel  = MakeContainer(ct, "TempPanel",        new Vector2(barX[1], barY), new Vector2(BAR_W, BAR_H));
        var battPanel  = MakeContainer(ct, "BatteryPanel",     new Vector2(barX[2], barY), new Vector2(BAR_W, BAR_H));
        var modePanel  = MakeContainer(ct, "DriveModePanel",   new Vector2(barX[3], barY), new Vector2(BAR_W, BAR_H));

        var fuelRefs = BuildBarGauge(fuelPanel.transform, "Fuel",         C_FUEL_BAR, C_FUEL_ICON, "%" , showBar: true);
        var tempRefs = BuildBarGauge(tempPanel.transform, "Engine Temp",  C_TEMP_BAR, C_TEMP_ICON, "°C", showBar: true);
        var battRefs = BuildBarGauge(battPanel.transform, "Battery",      C_BATT_BAR, C_BATT_ICON, "%" , showBar: true);
        var modeRefs = BuildDriveModePanel(modePanel.transform);

        // ── Status bar ────────────────────────────────────────────────────────
        BuildStatusBar(ct);

        // ── Gauge scripts ─────────────────────────────────────────────────────
        var sg = speedPanel.AddComponent<SpeedometerGauge>();
        var rg = rpmPanel.AddComponent<RPMGauge>();
        var fg = fuelPanel.AddComponent<FuelGauge>();
        var tg = tempPanel.AddComponent<TemperatureGauge>();
        var bg = battPanel.AddComponent<BatteryGauge>();

        WireCircleGauge(sg, speedRefs);
        WireCircleGauge(rg, rpmRefs);
        WireBarScript(fg, fuelRefs);
        WireBarScript(tg, tempRefs);
        WireBarScript(bg, battRefs);

        // ── MQTTManager ──────────────────────────────────────────────────────
        new GameObject("MQTTManager").AddComponent<MQTTManager>();

        // ── VehicleSimulator ─────────────────────────────────────────────────
        new GameObject("VehicleSimulator").AddComponent<VehicleSimulator>();

        // ── HMIController ────────────────────────────────────────────────────
        var hmiGO = new GameObject("HMIController");
        var hmi = hmiGO.AddComponent<HMIController>();
        var so = new SerializedObject(hmi);
        so.FindProperty("speedGauge").objectReferenceValue   = sg;
        so.FindProperty("rpmGauge").objectReferenceValue     = rg;
        so.FindProperty("batteryGauge").objectReferenceValue = bg;
        so.FindProperty("tempGauge").objectReferenceValue    = tg;
        so.FindProperty("fuelGauge").objectReferenceValue    = fg;
        so.FindProperty("driveModeText").objectReferenceValue    = modeRefs.modeText;
        so.FindProperty("driveModeSubText").objectReferenceValue = modeRefs.subText;
        so.ApplyModifiedProperties();

        // ── EventSystem (new Input System) ───────────────────────────────────
        var existingES = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        GameObject esGO = existingES != null ? existingES.gameObject : new GameObject("EventSystem");
        if (existingES == null) esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        var oldModule = esGO.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        if (oldModule != null) Object.DestroyImmediate(oldModule);
        if (esGO.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // ── Save ─────────────────────────────────────────────────────────────
        System.IO.Directory.CreateDirectory(Application.dataPath + "/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainHMI.unity");
        AssetDatabase.Refresh();
        Debug.Log("[HMISceneSetup] Scene built: Assets/Scenes/MainHMI.unity");
        EditorUtility.DisplayDialog("Scene Built", "MainHMI.unity created!\n\n1. Start Mosquitto\n2. Press Play", "OK");
    }

    // ── Circle gauge builder ──────────────────────────────────────────────────
    private struct CircleRefs
    {
        public Image trackRing, arcFill;
        public TextMeshProUGUI valueText, unitText;
    }

    private static CircleRefs BuildCircleGauge(Transform parent, Color arcColor, string unitLabel)
    {
        const float SIZE = 320f;
        const float INNER = 220f;
        const float ARC_FILL_START = 0.75f; // 270° track

        // Track ring (dark grey, 270° radial fill, rotated so start = 7 o'clock)
        var track = MakeImage(parent, "TrackRing",
            AnchorPreset.Center, Vector2.zero, new Vector2(SIZE, SIZE), C_TRACK);
        track.type = Image.Type.Filled;
        track.fillMethod = Image.FillMethod.Radial360;
        track.fillOrigin = (int)Image.Origin360.Top;
        track.fillClockwise = true;
        track.fillAmount = ARC_FILL_START;
        track.rectTransform.localRotation = Quaternion.Euler(0, 0, -135f);

        // Arc fill (colored, same setup, amount driven by value)
        var arc = MakeImage(parent, "ArcFill",
            AnchorPreset.Center, Vector2.zero, new Vector2(SIZE, SIZE), arcColor);
        arc.type = Image.Type.Filled;
        arc.fillMethod = Image.FillMethod.Radial360;
        arc.fillOrigin = (int)Image.Origin360.Top;
        arc.fillClockwise = true;
        arc.fillAmount = 0f;
        arc.rectTransform.localRotation = Quaternion.Euler(0, 0, -135f);

        // Inner black circle — creates the donut hole
        MakeImage(parent, "InnerMask",
            AnchorPreset.Center, Vector2.zero, new Vector2(INNER, INNER), C_BG);

        // Value text (large white number)
        var valTxt = MakeTMP(parent, "ValueText",
            AnchorPreset.Center, new Vector2(0f, 18f), new Vector2(220f, 80f),
            "0", 72f, C_WHITE, FontStyles.Bold);
        valTxt.alignment = TextAlignmentOptions.Center;

        // Unit text (small grey label below value)
        var unitTxt = MakeTMP(parent, "UnitText",
            AnchorPreset.Center, new Vector2(0f, -28f), new Vector2(220f, 30f),
            unitLabel, 16f, C_GREY, FontStyles.Normal);
        unitTxt.alignment = TextAlignmentOptions.Center;

        return new CircleRefs { trackRing = track, arcFill = arc, valueText = valTxt, unitText = unitTxt };
    }

    // ── Bar gauge builder ─────────────────────────────────────────────────────
    private struct BarRefs
    {
        public Image fillBar, iconBg;
        public TextMeshProUGUI valueText, labelText;
    }

    private static BarRefs BuildBarGauge(Transform parent, string title,
        Color barColor, Color iconColor, string unit, bool showBar)
    {
        // Icon background (colored square, top-left)
        var icon = MakeImage(parent, "IconBg",
            AnchorPreset.TopLeft, new Vector2(0f, -8f), new Vector2(36f, 36f), iconColor);

        // Title label (next to icon)
        MakeTMP(parent, "TitleLabel",
            AnchorPreset.TopLeft, new Vector2(46f, -8f), new Vector2(280f, 36f),
            title, 15f, C_GREY, FontStyles.Normal);

        // Value text + unit
        var valTxt = MakeTMP(parent, "ValueText",
            AnchorPreset.TopLeft, new Vector2(0f, -60f), new Vector2(200f, 52f),
            "0", 40f, C_WHITE, FontStyles.Bold);

        MakeTMP(parent, "UnitText",
            AnchorPreset.TopLeft, new Vector2(90f, -76f), new Vector2(80f, 28f),
            unit, 18f, C_GREY, FontStyles.Normal);

        // Track bar
        var barTrack = MakeImage(parent, "BarTrack",
            AnchorPreset.BottomStretch, new Vector2(0f, 14f), new Vector2(0f, 8f), C_TRACK);

        // Fill bar (on top of track)
        var fillGO = new GameObject("FillBar");
        fillGO.transform.SetParent(parent, false);
        var fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(0f, 0f);
        fillRT.pivot = new Vector2(0f, 0f);
        fillRT.anchoredPosition = new Vector2(0f, 14f);
        fillRT.sizeDelta = new Vector2(390f, 8f);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = barColor;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0.7f;

        return new BarRefs { fillBar = fillImg, iconBg = icon, valueText = valTxt };
    }

    // ── Drive mode panel ──────────────────────────────────────────────────────
    private struct ModeRefs { public TextMeshProUGUI modeText, subText; }

    private static ModeRefs BuildDriveModePanel(Transform parent)
    {
        // Icon bg (purple)
        MakeImage(parent, "IconBg",
            AnchorPreset.TopLeft, new Vector2(0f, -8f), new Vector2(36f, 36f), C_MODE_ICON);

        MakeTMP(parent, "TitleLabel",
            AnchorPreset.TopLeft, new Vector2(46f, -8f), new Vector2(280f, 36f),
            "Drive Mode", 15f, C_GREY, FontStyles.Normal);

        var modeText = MakeTMP(parent, "ModeText",
            AnchorPreset.TopLeft, new Vector2(0f, -58f), new Vector2(300f, 52f),
            "Normal", 40f, C_WHITE, FontStyles.Bold);

        var subText = MakeTMP(parent, "SubText",
            AnchorPreset.TopLeft, new Vector2(0f, -110f), new Vector2(300f, 28f),
            "AWD Active", 15f, C_GREY, FontStyles.Normal);

        return new ModeRefs { modeText = modeText, subText = subText };
    }

    // ── Nav panel (center placeholder) ───────────────────────────────────────
    private static void BuildNavPanel(Transform parent)
    {
        var nav = MakeContainer(parent, "NavPanel", new Vector2(0f, 80f), new Vector2(460f, 360f));
        nav.GetComponent<Image>().color = C_NAV_BG;

        // Top row
        MakeTMP(nav.transform, "NextStreet",
            AnchorPreset.TopLeft, new Vector2(16f, -16f), new Vector2(240f, 24f),
            "→  24th Street", 15f, C_WHITE, FontStyles.Normal);
        MakeTMP(nav.transform, "ETA",
            AnchorPreset.TopRight, new Vector2(-16f, -16f), new Vector2(120f, 24f),
            "ETA  4 min", 13f, C_GREY, FontStyles.Normal);

        // Center placeholder
        MakeTMP(nav.transform, "MapPlaceholder",
            AnchorPreset.Center, Vector2.zero, new Vector2(340f, 40f),
            "[ Navigation Map ]", 16f, C_GREY, FontStyles.Normal);

        // Bottom row
        MakeTMP(nav.transform, "ContinueOn",
            AnchorPreset.BottomLeft, new Vector2(16f, 16f), new Vector2(240f, 24f),
            "↑  Continue on Main Street", 14f, C_WHITE, FontStyles.Normal);
        MakeTMP(nav.transform, "Distance",
            AnchorPreset.BottomRight, new Vector2(-16f, 16f), new Vector2(120f, 24f),
            "0.8 mi", 14f, C_GREY, FontStyles.Normal);
    }

    // ── Status bar ────────────────────────────────────────────────────────────
    private static void BuildStatusBar(Transform parent)
    {
        MakeTMP(parent, "StatusBar",
            AnchorPreset.Bottom, new Vector2(0f, 24f), new Vector2(1200f, 28f),
            "●  All Systems Normal    •    Range: 342 mi    •    Trip: 0.0 mi    •    Avg: 28.5 mpg",
            13f, C_GREY, FontStyles.Normal);
    }

    // ── Wire helpers ──────────────────────────────────────────────────────────
    private static void WireCircleGauge(BaseGauge gauge, CircleRefs r)
    {
        var so = new SerializedObject(gauge);
        so.FindProperty("trackRing").objectReferenceValue  = r.trackRing;
        so.FindProperty("arcFill").objectReferenceValue    = r.arcFill;
        so.FindProperty("valueText").objectReferenceValue  = r.valueText;
        so.FindProperty("labelText").objectReferenceValue  = r.unitText;
        so.ApplyModifiedProperties();
    }

    private static void WireBarScript(BaseGauge gauge, BarRefs r)
    {
        var so = new SerializedObject(gauge);
        so.FindProperty("fillBar").objectReferenceValue   = r.fillBar;
        so.FindProperty("iconBg").objectReferenceValue    = r.iconBg;
        so.FindProperty("valueText").objectReferenceValue = r.valueText;
        so.ApplyModifiedProperties();
    }

    // ── UI factory helpers ────────────────────────────────────────────────────
    private enum AnchorPreset { Center, TopLeft, TopRight, BottomLeft, BottomRight,
                                Bottom, BottomStretch, Stretch }

    private static Image MakeImage(Transform parent, string name,
        AnchorPreset anchor, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        ApplyAnchor(rt, anchor, pos, size);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static GameObject MakeContainer(Transform parent, string name,
        Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = Color.clear;
        return go;
    }

    private static TextMeshProUGUI MakeTMP(Transform parent, string name,
        AnchorPreset anchor, Vector2 pos, Vector2 size,
        string text, float fontSize, Color color, FontStyles style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        ApplyAnchor(rt, anchor, pos, size);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    private static void ApplyAnchor(RectTransform rt, AnchorPreset p, Vector2 pos, Vector2 size)
    {
        switch (p)
        {
            case AnchorPreset.Center:
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = pos; rt.sizeDelta = size; break;
            case AnchorPreset.TopLeft:
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = pos; rt.sizeDelta = size; break;
            case AnchorPreset.TopRight:
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = pos; rt.sizeDelta = size; break;
            case AnchorPreset.BottomLeft:
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = pos; rt.sizeDelta = size; break;
            case AnchorPreset.BottomRight:
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = pos; rt.sizeDelta = size; break;
            case AnchorPreset.Bottom:
                rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = pos; rt.sizeDelta = size; break;
            case AnchorPreset.BottomStretch:
                rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = pos; rt.sizeDelta = size; break;
            case AnchorPreset.Stretch:
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero; break;
        }
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }

    // ── Donut sprite generator ────────────────────────────────────────────────
    [MenuItem("HMI/Generate Donut Sprite")]
    public static void GenerateDonutSprite()
    {
        const int size = 256;
        const float outer = 0.5f;
        const float inner = 0.36f;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        var center = new Vector2(0.5f, 0.5f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2((float)x / size, (float)y / size), center);
                float a = Mathf.SmoothStep(outer, outer - 0.015f, dist)
                        * Mathf.SmoothStep(inner, inner + 0.015f, dist);
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(pixels); tex.Apply();
        System.IO.Directory.CreateDirectory(Application.dataPath + "/Sprites");
        System.IO.File.WriteAllBytes(Application.dataPath + "/Sprites/GaugeDonut.png", tex.EncodeToPNG());
        AssetDatabase.Refresh();
        var imp = AssetImporter.GetAtPath("Assets/Sprites/GaugeDonut.png") as TextureImporter;
        if (imp != null) { imp.textureType = TextureImporterType.Sprite; imp.alphaIsTransparency = true; imp.SaveAndReimport(); }
        Debug.Log("[HMISceneSetup] Donut sprite saved to Assets/Sprites/GaugeDonut.png");
    }
}
