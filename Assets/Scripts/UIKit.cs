using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// Helpers that build UI from code, so you never have to link anything by hand.
public static class UIKit
{
    public static readonly Color Cyan = new Color(0.47f, 0.86f, 1f, 1f);
    public static readonly Color Orange = new Color(1f, 0.52f, 0.25f, 1f);
    public static readonly Color Yellow = new Color(1f, 0.9f, 0.35f, 1f);
    public static readonly Color Dim = new Color(0.62f, 0.68f, 0.85f, 1f);
    public static readonly Color Green = new Color(0.45f, 1f, 0.55f, 1f);
    public static readonly Color Red = new Color(1f, 0.38f, 0.38f, 1f);

    static Font cachedFont;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        cachedFont = null;
        UIButton.Locked = false;
    }

    // Uses Assets/Resources/PressStart2P-Regular.ttf if you added it, otherwise a default font.
    public static Font GameFont
    {
        get
        {
            if (cachedFont == null)
            {
                cachedFont = Resources.Load<Font>("PressStart2P-Regular");
                if (cachedFont == null)
                {
                    try { cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
                    catch (Exception) { cachedFont = null; }
                }
                if (cachedFont == null) cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }
            return cachedFont;
        }
    }

    public static Canvas CreateCanvas(Transform parent)
    {
        var go = new GameObject("Canvas");
        go.transform.SetParent(parent, false);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    public static void Anchor(RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        r.anchorMin = anchor;
        r.anchorMax = anchor;
        r.pivot = pivot;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
    }

    public static Image Img(string name, Transform parent, Sprite sprite, Color color)
    {
        var r = Rect(name, parent);
        var img = r.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public static Text Label(string name, Transform parent, string text, int size, Color color, TextAnchor anchor)
    {
        var r = Rect(name, parent);
        var t = r.gameObject.AddComponent<Text>();
        t.font = GameFont;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = anchor;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    // A clickable dark-blue box with a label (used for +/- and menu buttons).
    public static UIButton Button(string name, Transform parent, string label, int fontSize,
        Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Action onClick)
    {
        var r = Rect(name, parent);
        Anchor(r, anchor, pivot, pos, size);

        var bg = r.gameObject.AddComponent<Image>();
        bg.raycastTarget = false;

        var outline = r.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.47f, 0.86f, 1f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);

        var txt = Label("Label", r, label, fontSize, Color.white, TextAnchor.MiddleCenter);
        Stretch(txt.rectTransform);

        var b = r.gameObject.AddComponent<UIButton>();
        b.Tint = bg;
        b.Label = txt;
        b.OnClick = onClick;
        bg.color = b.NormalColor;
        return b;
    }
}

// A button that checks the mouse itself (no EventSystem needed).
public class UIButton : MonoBehaviour
{
    public static bool Locked; // set true during screen transitions

    public Action OnClick;
    public Action OnHover; // fires once when the mouse enters
    public bool Hovered { get; private set; }
    public bool Interactable = true;

    // optional visuals (set by UIKit.Button)
    public Image Tint;
    public Text Label;
    public Color NormalColor = new Color(0.10f, 0.16f, 0.38f, 1f);
    public Color HoverColor = new Color(0.20f, 0.32f, 0.65f, 1f);
    public Color DisabledColor = new Color(0.12f, 0.12f, 0.18f, 0.85f);

    RectTransform rt;

    void Awake()
    {
        rt = (RectTransform)transform;
    }

    void Update()
    {
        bool blocked = Locked || Time.frameCount <= DialogueBox.ClosedFrame || (DialogueBox.Instance != null && DialogueBox.Instance.IsOpen);
        var mouse = Mouse.current;

        bool inside = false;
        if (mouse != null && !blocked)
            inside = RectTransformUtility.RectangleContainsScreenPoint(rt, mouse.position.ReadValue(), null);

        if (inside && !Hovered && OnHover != null) OnHover();
        Hovered = inside;

        if (Tint != null) Tint.color = !Interactable ? DisabledColor : (Hovered ? HoverColor : NormalColor);
        if (Label != null) Label.color = Interactable ? Color.white : new Color(0.5f, 0.55f, 0.65f, 1f);

        if (inside && Interactable && mouse.leftButton.wasPressedThisFrame && OnClick != null) OnClick();
    }
}
