using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Editor utility — builds the full MainHMI scene from scratch.
/// Run via: top menu → HMI → Build Scene
/// </summary>
public static class HMISceneSetup
{
    // Design tokens
    private static readonly Color BG_COLOR      = HexColor("0A0A0A");
    private static readonly Color PANEL_COLOR    = HexColor("111111");
    private static readonly Color ACCENT         = HexColor("00FFCC");
    private static readonly Color DIM_TEXT       = HexColor("888888");

    [MenuItem("HMI/Build Scene")]
    public static void BuildScene()
    {
        // Create & open a new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Camera ───────────────────────────────────────────────────────────
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BG_COLOR;
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

        // ── Full-screen background ───────────────────────────────────────────
        var bgPanel = CreatePanel(canvasGO.transform, "Background",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(1920, 1080), BG_COLOR);

        // ── Grid: 2 x 2 gauge panels ─────────────────────────────────────────
        //  [Speed]    [RPM]
        //  [Battery]  [Temp]
        float pw = 860f, ph = 480f;
        float ox = 480f, oy = 260f;

        var speedPanel   = CreateGaugePanel(canvasGO.transform, "SpeedPanel",    new Vector2(-ox,  oy), pw, ph);
        var rpmPanel     = CreateGaugePanel(canvasGO.transform, "RPMPanel",      new Vector2( ox,  oy), pw, ph);
        var battPanel    = CreateGaugePanel(canvasGO.transform, "BatteryPanel",  new Vector2(-ox, -oy), pw, ph);
        var tempPanel    = CreateGaugePanel(canvasGO.transform, "TempPanel",     new Vector2( ox, -oy), pw, ph);

        // ── Build each gauge ─────────────────────────────────────────────────
        var speedGauge = BuildCircularGauge(speedPanel, "SPEED",    "km/h");
        var rpmGauge   = BuildCircularGauge(rpmPanel,   "RPM",      "x1000");
        var battGauge  = BuildBarGauge(battPanel,       "BATTERY",  "%");
        var tempGauge  = BuildBarGauge(tempPanel,       "TEMPERATURE", "°C");

        // Attach gauge scripts
        var sg = speedPanel.AddComponent<SpeedometerGauge>();
        var rg = rpmPanel.AddComponent<RPMGauge>();
        var bg = battPanel.AddComponent<BatteryGauge>();
        var tg = tempPanel.AddComponent<TemperatureGauge>();

        WireCircularGauge(sg, speedGauge);
        WireCircularGauge(rg, rpmGauge);
        WireBarGauge(bg, battGauge);
        WireBarGauge(tg, tempGauge);

        // ── MQTTManager ──────────────────────────────────────────────────────
        var mqttGO = new GameObject("MQTTManager");
        mqttGO.AddComponent<MQTTManager>();

        // ── HMIController ────────────────────────────────────────────────────
        var hmiGO = new GameObject("HMIController");
        var hmi = hmiGO.AddComponent<HMIController>();

        // Wire gauges into HMIController via SerializedObject
        var so = new SerializedObject(hmi);
        so.FindProperty("speedGauge").objectReferenceValue   = sg;
        so.FindProperty("rpmGauge").objectReferenceValue     = rg;
        so.FindProperty("batteryGauge").objectReferenceValue = bg;
        so.FindProperty("tempGauge").objectReferenceValue    = tg;
        so.ApplyModifiedProperties();

        // No EventSystem needed — this HMI is display-only, no UI interaction required.

        // ── Save scene ───────────────────────────────────────────────────────
        System.IO.Directory.CreateDirectory(Application.dataPath + "/Scenes");
        string path = "Assets/Scenes/MainHMI.unity";
        EditorSceneManager.SaveScene(scene, path);
        AssetDatabase.Refresh();

        Debug.Log("[HMISceneSetup] Scene built and saved to " + path);
        EditorUtility.DisplayDialog("HMI Scene Built",
            "MainHMI.unity created successfully!\n\nNext steps:\n" +
            "1. Open Assets/Scenes/MainHMI.unity\n" +
            "2. Start Mosquitto: mosquitto -v\n" +
            "3. Press Play\n" +
            "4. Publish test data via mosquitto_pub", "OK");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static GameObject CreateGaugePanel(Transform parent, string name, Vector2 anchoredPos, float w, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(w, h);
        var img = go.AddComponent<Image>();
        img.color = PANEL_COLOR;
        return go;
    }

    private static (TextMeshProUGUI valueText, TextMeshProUGUI labelText,
                    Image arcFill, RectTransform needle, Image needleImg)
        BuildCircularGauge(GameObject panel, string label, string unit)
    {
        var rt = panel.GetComponent<RectTransform>();
        float w = rt.sizeDelta.x, h = rt.sizeDelta.y;

        // Arc background (grey ring)
        var arcBg = CreateImage(panel.transform, "ArcBackground",
            new Vector2(0.5f, 0.55f), new Vector2(320f, 320f), HexColor("1A1A1A"));
        arcBg.type = Image.Type.Filled;
        arcBg.fillMethod = Image.FillMethod.Radial360;
        arcBg.fillAmount = 0.75f;
        arcBg.fillOrigin = (int)Image.Origin360.Bottom;
        arcBg.fillClockwise = false;

        // Arc fill (cyan, on top)
        var arcFill = CreateImage(panel.transform, "ArcFill",
            new Vector2(0.5f, 0.55f), new Vector2(320f, 320f), ACCENT);
        arcFill.type = Image.Type.Filled;
        arcFill.fillMethod = Image.FillMethod.Radial360;
        arcFill.fillAmount = 0f;
        arcFill.fillOrigin = (int)Image.Origin360.Bottom;
        arcFill.fillClockwise = false;

        // Needle pivot (center of arc)
        var needlePivotGO = new GameObject("NeedlePivot");
        needlePivotGO.transform.SetParent(panel.transform, false);
        var needlePivotRT = needlePivotGO.AddComponent<RectTransform>();
        needlePivotRT.anchorMin = needlePivotRT.anchorMax = new Vector2(0.5f, 0.55f);
        needlePivotRT.pivot = new Vector2(0.5f, 0.5f);
        needlePivotRT.anchoredPosition = Vector2.zero;
        needlePivotRT.sizeDelta = new Vector2(10f, 150f);

        // Needle visual (child of pivot)
        var needleGO = new GameObject("Needle");
        needleGO.transform.SetParent(needlePivotGO.transform, false);
        var needleRT = needleGO.AddComponent<RectTransform>();
        needleRT.anchorMin = new Vector2(0.5f, 0.5f);
        needleRT.anchorMax = new Vector2(0.5f, 0.5f);
        needleRT.pivot = new Vector2(0.5f, 0f);
        needleRT.anchoredPosition = Vector2.zero;
        needleRT.sizeDelta = new Vector2(6f, 130f);
        var needleImg = needleGO.AddComponent<Image>();
        needleImg.color = Color.white;

        // Center dot
        CreateImage(panel.transform, "CenterDot",
            new Vector2(0.5f, 0.55f), new Vector2(18f, 18f), ACCENT);

        // Value text (large, center)
        var valueText = CreateTMP(panel.transform, "ValueText",
            new Vector2(0.5f, 0.52f), new Vector2(200f, 70f), "0", 42f, Color.white, FontStyles.Bold);

        // Unit text
        CreateTMP(panel.transform, "UnitText",
            new Vector2(0.5f, 0.38f), new Vector2(160f, 30f), unit, 18f, DIM_TEXT, FontStyles.Normal);

        // Label text (bottom)
        var labelText = CreateTMP(panel.transform, "LabelText",
            new Vector2(0.5f, 0.12f), new Vector2(300f, 36f), label, 20f, ACCENT, FontStyles.Bold);

        return (valueText, labelText, arcFill, needlePivotRT, needleImg);
    }

    private static (TextMeshProUGUI valueText, TextMeshProUGUI labelText,
                    Image fillBar, Image iconImage)
        BuildBarGauge(GameObject panel, string label, string unit)
    {
        // Track background
        var trackBg = CreateImage(panel.transform, "TrackBackground",
            new Vector2(0.5f, 0.55f), new Vector2(680f, 40f), HexColor("1A1A1A"));

        // Fill bar (on top, left-anchored for fill)
        var fillGO = new GameObject("FillBar");
        fillGO.transform.SetParent(panel.transform, false);
        var fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0.5f, 0.55f);
        fillRT.anchorMax = new Vector2(0.5f, 0.55f);
        fillRT.pivot = new Vector2(0.5f, 0.5f);
        fillRT.anchoredPosition = Vector2.zero;
        fillRT.sizeDelta = new Vector2(680f, 40f);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = ACCENT;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0f;

        // Icon placeholder
        var icon = CreateImage(panel.transform, "Icon",
            new Vector2(0.5f, 0.72f), new Vector2(48f, 48f), ACCENT);

        // Value text
        var valueText = CreateTMP(panel.transform, "ValueText",
            new Vector2(0.5f, 0.38f), new Vector2(200f, 60f), "0" + unit, 40f, Color.white, FontStyles.Bold);

        // Label text
        var labelText = CreateTMP(panel.transform, "LabelText",
            new Vector2(0.5f, 0.18f), new Vector2(300f, 36f), label, 20f, ACCENT, FontStyles.Bold);

        return (valueText, labelText, fillImg, icon);
    }

    // Wire circular gauge script fields via SerializedObject
    private static void WireCircularGauge(BaseGauge gauge,
        (TextMeshProUGUI vt, TextMeshProUGUI lt, Image arc, RectTransform needle, Image needleImg) parts)
    {
        var so = new SerializedObject(gauge);
        so.FindProperty("valueText").objectReferenceValue        = parts.vt;
        so.FindProperty("labelText").objectReferenceValue        = parts.lt;
        so.FindProperty("arcFillImage").objectReferenceValue     = parts.arc;
        so.FindProperty("needleTransform").objectReferenceValue  = parts.needle;
        so.FindProperty("needleImage").objectReferenceValue      = parts.needleImg;
        so.ApplyModifiedProperties();
    }

    // Wire bar gauge script fields via SerializedObject
    private static void WireBarGauge(BaseGauge gauge,
        (TextMeshProUGUI vt, TextMeshProUGUI lt, Image fill, Image icon) parts)
    {
        var so = new SerializedObject(gauge);
        so.FindProperty("valueText").objectReferenceValue = parts.vt;
        so.FindProperty("labelText").objectReferenceValue = parts.lt;
        so.FindProperty("fillBarImage").objectReferenceValue = parts.fill;

        // Battery or Temperature — different icon field name
        var iconProp = so.FindProperty("batteryIconImage") ?? so.FindProperty("thermometerIconImage");
        if (iconProp != null) iconProp.objectReferenceValue = parts.icon;

        so.ApplyModifiedProperties();
    }

    // ── UI factory helpers ────────────────────────────────────────────────────

    private static Image CreateImage(Transform parent, string name,
        Vector2 anchorPos, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchorPos;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offset, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = offset; rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    private static TextMeshProUGUI CreateTMP(Transform parent, string name,
        Vector2 anchorPos, Vector2 size, string text, float fontSize, Color color, FontStyles style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchorPos;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    private static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}
