using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Overimagined.Common;
using System.Threading;

public class DebugInfo : SingletonMonoManager<DebugInfo>
{
    static Vector3 nPos = new Vector3(float.MinValue, float.MinValue, float.MinValue);
    const float flashDuration = 0.3f;

    public Color defaultColor = Color.white;
    public bool disableInEditor;
    public Vector2 position;

    DebugInfoBuilder builder;

    protected Dictionary<string, Info> infos;
    HashSet<string> overlayInfos;
    HashSet<string> activeKeys;
    List<string> inactiveKeys;

    List<SetData> pendingSets1, pendingSets2;
    bool pendingFlip;

    Thread mainThread;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        infos = new Dictionary<string, Info>();
        overlayInfos = new HashSet<string>();
        activeKeys = new HashSet<string>();
        inactiveKeys = new List<string>();
        pendingSets1 = new List<SetData>();
        pendingSets2 = new List<SetData>();

        builder = new DebugInfoBuilder();

        mainThread = Thread.CurrentThread;
    }

    public static void SetLeftPadding(int pad)
    {
        Instance.builder.vertLayout.padding.left = pad;
    }

    public static void SetTopPadding(int pad)
    {
        Instance.builder.vertLayout.padding.left = pad;
    }

    public static void Set(object key, Color color)
    {
        Set(key.ToString(), null, color, nPos);
    }

    public static void Set(string key, Color color)
    {
        Set(key, null, color, nPos);
    }

    public static void Set(object key, object content)
    {
        Set(key.ToString(), content, Instance.defaultColor);
    }

    public static void Set(string key, object content)
    {
        Set(key, content, Instance.defaultColor);
    }

    public static void Set(object key, object content, Color color)
    {
        Set(key.ToString(), content, color);
    }

    public static void Set(string key, object content, Color color)
    {
        Set(key, content, color, nPos);
    }

    public static void Set(string key, float content, int significantDigit, int placeHolder = 10)
    {
        Set(key, string.Format($"{{0,{placeHolder}:N{significantDigit}}}", content), Instance.defaultColor, nPos);
    }

    public static void Set(string key, double content, int significantDigit, int placeHolder = 10)
    {
        Set(key, string.Format($"{{0,{placeHolder}:N{significantDigit}}}", content), Instance.defaultColor, nPos);
    }

    public static void Set(string key, object content, Vector3 worldPosition)
    {
        Set(key, content, Instance.defaultColor, worldPosition);
    }

    public static void Set(string key, object content, Color color, Vector3 worldPosition)
    {
        var s = content == null ? "null" : content.ToString();

        if (Thread.CurrentThread != Instance.mainThread)
        {
            QueueSet(key, content, color, worldPosition);
            return;
        }

        Info info;
        var isOverlay = worldPosition != nPos;


        if (Instance.infos.ContainsKey(key))
        {
            info = Instance.infos[key];
            if (!isOverlay && Instance.overlayInfos.Contains(key))
                Instance.overlayInfos.Remove(key);

            if (info.ValueLabel.text == s)
                return;
        }
        else
        {
            info = Instance.builder.AddPair<Info>(key, s, 16, isOverlay);
            if (isOverlay)
                Instance.overlayInfos.Add(key);
        }

        info.ValueLabel.text = s;
        info.KeyLabel.color = color;
        info.worldPosition = worldPosition;

        if (!Instance.activeKeys.Contains(key))
            Instance.activeKeys.Add(key);


        info.lastUpdated = Time.time;
        Instance.infos[key] = info;
    }

    public static void QueueSet(string key, object content, Color color, Vector3 worldPosition)
    {
        var list = Instance.pendingFlip ? Instance.pendingSets1 : Instance.pendingSets2;
        list?.Add(new SetData { key = key, content = content, color = color, worldPosition = worldPosition });
    }

    public static void RemoveText(string name)
    {
        if (Instance.infos.ContainsKey(name))
            Instance.infos.Remove(name);
    }

    public static void Clear(string key)
    {
        if (Instance.infos.ContainsKey(key))
        {
            Object.Destroy(Instance.infos[key].KeyLabel.transform.parent.gameObject);
            Instance.infos.Remove(key);
            Instance.activeKeys.Remove(key);
            Instance.inactiveKeys.Remove(key);
            Instance.overlayInfos.Remove(key);
        }
    }

    public static void Clear()
    {
        foreach (var info in Instance.infos.Values)
            Object.Destroy(info.KeyLabel.transform.parent.gameObject);

        Instance.infos.Clear();
    }

    void Update()
    {
        var list = Instance.pendingFlip ? Instance.pendingSets2 : Instance.pendingSets1;
        foreach (var s in list)
            Set(s.key, s.content, s.color, s.worldPosition);
        list.Clear();
        pendingFlip = !pendingFlip;

        inactiveKeys.Clear();

        foreach (var key in activeKeys)
        {
            var info = infos[key];
            var elasped = Time.time - info.lastUpdated;
            var rgb = info.Background.color;
            info.Background.color = new Color(rgb.r, rgb.g, rgb.b, Mathf.Clamp(1 - (float)elasped / flashDuration, 0.5f, 1f));

            if (elasped > flashDuration)
                inactiveKeys.Add(key);
        }

        foreach (var key in inactiveKeys)
            activeKeys.Remove(key);

        foreach (var key in overlayInfos)
        {
            var info = infos[key];
            var point = Camera.main.WorldToScreenPoint(info.worldPosition);
            ((RectTransform)info.KeyLabel.transform.parent).anchoredPosition = point;
        }
    }

    void OnDestroy()
    {
        builder.Destroy();
    }

    protected struct Info : IKeyValueUI
    {
        public Color color;
        public double lastUpdated;
        public Vector3 worldPosition;

        public TMP_Text KeyLabel { get; set; }
        public TMP_Text ValueLabel { get; set; }
        public Image Background { get; set; }
    }

    protected struct SetData
    {
        public string key;
        public object content;
        public Color color;
        public Vector3 worldPosition;
    }
}