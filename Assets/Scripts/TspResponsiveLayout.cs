using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Attached to the canvas in both supplied scenes. Runs before puzzle placement.
[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
public class TspResponsiveLayout : MonoBehaviour
{
    readonly Dictionary<string, RectTransform> items = new();
    CanvasScaler scaler;
    Rect lastSafe;
    int lastWidth, lastHeight;
    bool game;
    float left, top;

    void Awake()
    {
        scaler = GetComponent<CanvasScaler>();
        foreach (RectTransform rt in GetComponentsInChildren<RectTransform>(true))
        {
            if (!items.ContainsKey(rt.name)) items.Add(rt.name, rt);
        }
        game = items.ContainsKey("PuzzleArea");
        // The menu contains an unused duplicate inside the instruction panel.
        if (!game)
        {
            items["HowToPlayButton"] = transform.Find("HowToPlayButton") as RectTransform;
            var duplicate = transform.Find("HowToPlayPanel/HowToPlayButton");
            if (duplicate != null) duplicate.gameObject.SetActive(false);
        }
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            // Labels do not receive pointer events, so the button's own graphic must.
            // Some scene buttons previously relied on their labels as click targets.
            if (button.targetGraphic != null)
                button.targetGraphic.raycastTarget = true;

            foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
            {
                var rt = label.rectTransform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(6, 3); rt.offsetMax = new Vector2(-6, -3);
                label.enableAutoSizing = true; label.fontSizeMin = 18; label.fontSizeMax = 24;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
            }
        }
        Apply();
    }
    void Update()
    {
        if (Screen.width != lastWidth || Screen.height != lastHeight || Screen.safeArea != lastSafe) Apply();
    }
    void Box(string name, float x, float y, float w, float h, bool root = true)
    {
        if (!items.TryGetValue(name, out var rt) || rt == null) return;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x + w / 2 + (root ? left : 0), -y - h / 2 - (root ? top : 0));
    }
    void TextStyle(string name, float max, float min = 22)
    {
        if (!items.TryGetValue(name, out var rt)) return;
        var text = rt.GetComponent<TMP_Text>();
        if (text == null) return;
        text.enableAutoSizing = true; text.fontSizeMin = min; text.fontSizeMax = max;
        text.margin = new Vector4(6, 3, 6, 3);
        text.raycastTarget = false;
    }
    void Apply()
    {
        lastWidth = Screen.width; lastHeight = Screen.height; lastSafe = Screen.safeArea;
        if (lastWidth <= 0 || lastHeight <= 0) return;
        Rect safe = lastSafe.width > 0 && lastSafe.height > 0 ? lastSafe : new Rect(0, 0, lastWidth, lastHeight);
        bool landscape = safe.width / safe.height >= 1.25f;
        float scale = landscape
            ? Mathf.Min(safe.width / 800f, safe.height / 600f)
            : Mathf.Min(safe.width / 600f, safe.height / 930f);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = scale;
        left = safe.x / scale; top = (lastHeight - safe.yMax) / scale;
        float w = safe.width / scale, h = safe.height / scale;
        if (game) GameLayout(w, h); else MenuLayout(w, h);
        Canvas.ForceUpdateCanvases();
    }
    void GameLayout(float w, float h)
    {
        bool wide = w / h >= 1.25f;
        float board = wide ? Mathf.Min(h - 32, w * .56f) : Mathf.Min(w - 32, h - 350);
        float bx = wide ? 16 : (w - board) / 2, by = wide ? (h - board) / 2 : 94;
        Box("PuzzleArea", bx, by, board, board);
        Box("ResultPanel", bx, by, board, board);
        float x = wide ? bx + board + 18 : 16;
        float width = wide ? w - x - 16 : w - 32;
        Box("TitleText", wide ? x : 16, 12, width, 40);
        // Reserve space INSIDE the controls column for the label and a compact selector.
        float headerY = wide ? 60 : 54;
        const float labelWidth = 90, headerGap = 10;
        float dropdownWidth = Mathf.Clamp(width * .28f, 100, 140);
        float dropdownX = x + labelWidth + headerGap;
        Box("NodeCountDropdown", dropdownX, headerY, dropdownWidth, 36);
        // NodesHeading is a child of the dropdown, so use its local coordinates.
        Box("NodesHeading", -labelWidth - headerGap, 0, labelWidth, 36, false);
        float timerX = dropdownX + dropdownWidth + headerGap;
        Box("TimerText", timerX, headerY, x + width - timerX, 36);
        TextStyle("NodesHeading", 24, 18);
        float controlsY = wide ? 114 : by + board + 10;
        Box("StatusText", x, controlsY, width, wide ? 102 : 64);
        float buttonsY = controlsY + (wide ? 114 : 70);
        float bw = (width - 16) / 3;
        Box("StartButton", x, buttonsY, bw, 48);
        Box("UndoButton", x + bw + 8, buttonsY, bw, 48);
        Box("SubmitButton", x + 2 * (bw + 8), buttonsY, bw, 48);
        Box("RouteNavigationPanel", x, buttonsY + 60, width, 108);
        float half = (width - 8) / 2;
        Box("PlayerRouteButton", 0, 0, half, 48, false);
        Box("OptimalRouteButton", half + 8, 0, half, 48, false);
        Box("CompareRoutesButton", 0, 56, half, 48, false);
        Box("BackToResultsButton", half + 8, 56, half, 48, false);
        // Opaque results cover the board; route-view controls remain outside it.
        var panel = items["ResultPanel"].GetComponent<Image>();
        if (panel != null) { Color c = panel.color; c.a = 1; panel.color = c; panel.raycastTarget = true; }
        Box("ResultsPanelText", 12, 12, board - 24, 44, false);
        Box("ResultsMessageText", 16, 64, board - 32, 76, false);
        Box("ResultsStatsText", 20, 148, board - 40, board - 284, false);
        float actionWidth = (board - 40) / 2;
        Box("RetryPuzzleButton", 16, board - 120, actionWidth, 48, false);
        Box("NextPuzzleButton", 16, board - 64, actionWidth, 48, false);
        Box("MainMenuButton", 24 + actionWidth, board - 64, actionWidth, 48, false);
        TextStyle("TitleText", 26, 12);
        if (items.TryGetValue("TitleText", out var titleRect))
        {
            var title = titleRect.GetComponent<TMP_Text>();
            if (title != null)
            {
                title.textWrappingMode = TextWrappingModes.NoWrap;
                title.overflowMode = TextOverflowModes.Ellipsis;
                title.alignment = TextAlignmentOptions.Center;
            }
        } TextStyle("StatusText", 25, 20); TextStyle("TimerText", 26);
        TextStyle("ResultsPanelText", 32); TextStyle("ResultsMessageText", 28, 22); TextStyle("ResultsStatsText", 28, 22);
        var dropdown = items["NodeCountDropdown"].GetComponent<TMP_Dropdown>();
        if (dropdown != null)
        {
            dropdown.captionText.enableAutoSizing = true;
            dropdown.captionText.fontSizeMin = 16; dropdown.captionText.fontSizeMax = 24;
            dropdown.itemText.fontSize = 24;
        }
        // Limit the list to the remaining height; scrolling exposes other choices.
        var template = items["Template"];
        template.sizeDelta = new Vector2(template.sizeDelta.x, Mathf.Min(300, h - 116));
    }
    void MenuLayout(float w, float h)
    {
        float width = Mathf.Min(w - 40, 760), x = (w - width) / 2;
        Box("GameTitleText", x, h * .15f, width, 80);
        Box("SubtitleText", x, h * .15f + 92, width, 80);
        Box("PlayGameButton", w / 2 - 130, h * .58f, 260, 56);
        Box("HowToPlayButton", w / 2 - 130, h * .58f + 72, 260, 56);
        Box("HowToPlayPanel", x, 20, width, h - 40);
        Box("HowtoPlayTitleText", 16, 16, width - 32, 48, false);
        Box("HowToPlayText", 24, 76, width - 48, h - 216, false);
        Box("CloseHowToPlayButton", width / 2 - 110, h - 112, 220, 52, false);
        TextStyle("GameTitleText", 56, 32); TextStyle("SubtitleText", 30);
        TextStyle("HowtoPlayTitleText", 32); TextStyle("HowToPlayText", 27, 22);
    }
}
