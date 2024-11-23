using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugInfoBuilder
{
    public GameObject root;
    public Canvas defaultCanvas, overlayCanvas;
    public VerticalLayoutGroup vertLayout;

    public DebugInfoBuilder()
    {
        root = new GameObject("DebugCanvas");
        var defaultHost = new GameObject("Default");
        var overlayHost = new GameObject("Overlay");

        defaultHost.transform.parent = root.transform;
        overlayHost.transform.parent = root.transform;

        defaultCanvas = defaultHost.AddComponent<Canvas>();
        defaultCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        defaultCanvas.sortingOrder = 100;
        
        vertLayout = defaultHost.AddComponent<VerticalLayoutGroup>();
        vertLayout.childForceExpandHeight = false;
        vertLayout.childControlHeight = false;
        vertLayout.childControlWidth = false;

        overlayCanvas = overlayHost.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 100;
    }

    public T AddPair<T>(string key, string value, float fontSize, bool isOverlay) where T : IKeyValueUI, new()
    {
        var go = new GameObject(key);
        var rt = go.AddComponent<RectTransform>();
        rt.transform.SetParent(isOverlay ? overlayCanvas.transform : defaultCanvas.transform, false);
        rt.sizeDelta = new Vector2(100, 26);
        rt.pivot = Vector2.zero;
        var horiLayout = go.AddComponent<HorizontalLayoutGroup>();
        horiLayout.padding.left = 10;
        horiLayout.padding.right = 10;

        var contentFitter = go.AddComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.5f);

        var keyLabel = AddText(go, key, fontSize, TextAlignmentOptions.Left);
        keyLabel.name = "Key";

        var separeatorObject = new GameObject("Seperator");
        separeatorObject.transform.SetParent(go.transform, false);
        var element = separeatorObject.AddComponent<LayoutElement>();
        element.minWidth = 10;

        var valueLabel = AddText(go, value, fontSize, TextAlignmentOptions.Right);
        valueLabel.name = "Value";

        if (isOverlay)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
        }

        return new T { KeyLabel = keyLabel, ValueLabel = valueLabel, Background = bg };
    }

    TextMeshProUGUI AddText(GameObject parent, string text, float fontSize, TextAlignmentOptions alignment)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent.transform, false);
        var tmpText = go.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = fontSize;
        tmpText.enableWordWrapping = false;
        tmpText.alignment = alignment;

        return tmpText;
    }

    public void Destroy()
    {
        Object.Destroy(root);
    }
}

public interface IKeyValueUI
{
    Image Background { get; set; }
    TMP_Text KeyLabel { get; set; }
    TMP_Text ValueLabel { get; set; }
}