using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// Mechaguru's dialogue box. Created once by GameBootstrap and reused for the whole game.
// Usage: dialogue.Open(new[] { "Line one", "Line two" }, () => Debug.Log("done"));
public class DialogueBox : MonoBehaviour
{
    const float CharsPerSecond = 45f;

    Text bodyText;
    Text nameText;
    Text arrowText;

    readonly Queue<string> lines = new Queue<string>();
    string full = "";
    float shown;
    int lastShown;
    bool typing;
    bool open;
    int openedFrame;
    int lineIndex;
    Action onFinished;
    Action<int> onLineStart;

    public static DialogueBox Instance;
    public static int ClosedFrame = -10; // frame the box last closed (so a click that closes it isn't reused)

    public bool IsOpen { get { return open; } }

    public static DialogueBox Create(Transform parent)
    {
        var rt = UIKit.Rect("DialogueBox", parent);
        UIKit.Anchor(rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1700f, 340f));
        var box = rt.gameObject.AddComponent<DialogueBox>();
        Instance = box;
        box.Build();
        rt.gameObject.SetActive(false);
        return box;
    }

    void Build()
    {
        var panel = gameObject.AddComponent<Image>();
        panel.sprite = PixelArt.ToSprite(PixelArt.Panel(170, 34, 7));
        panel.raycastTarget = false;

        // name tag sitting on the top-left edge of the box
        var tag = UIKit.Rect("NameTag", transform);
        UIKit.Anchor(tag, new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(50f, -6f), new Vector2(430f, 90f));
        var tagImg = tag.gameObject.AddComponent<Image>();
        tagImg.sprite = PixelArt.ToSprite(PixelArt.Panel(43, 9, 3));
        tagImg.raycastTarget = false;

        nameText = UIKit.Label("Name", tag, "MECHAGURU", 28, UIKit.Cyan, TextAnchor.MiddleCenter);
        UIKit.Stretch(nameText.rectTransform);

        // body text
        bodyText = UIKit.Label("Body", transform, "", 32, Color.white, TextAnchor.UpperLeft);
        var br = bodyText.rectTransform;
        br.anchorMin = Vector2.zero;
        br.anchorMax = Vector2.one;
        br.offsetMin = new Vector2(70f, 50f);
        br.offsetMax = new Vector2(-70f, -50f);
        bodyText.lineSpacing = 1.35f;

        // blinking "more" arrow
        arrowText = UIKit.Label("Arrow", transform, "v", 30, UIKit.Cyan, TextAnchor.MiddleCenter);
        UIKit.Anchor(arrowText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60f, 36f), new Vector2(60f, 60f));
        arrowText.gameObject.SetActive(false);
    }

    // pages: the lines to show. finished: called after the last line. onLine: called when each line starts.
    public void Open(string[] pages, Action finished = null, string speaker = "MECHAGURU", Action<int> onLine = null)
    {
        lines.Clear();
        foreach (var p in pages) lines.Enqueue(p);

        onFinished = finished;
        onLineStart = onLine;
        nameText.text = speaker;
        lineIndex = -1;
        open = true;
        openedFrame = Time.frameCount;
        gameObject.SetActive(true);
        NextLine();
    }

    void NextLine()
    {
        if (lines.Count == 0)
        {
            Close(true);
            return;
        }

        full = lines.Dequeue();
        lineIndex++;
        shown = 0f;
        lastShown = 0;
        typing = true;
        arrowText.gameObject.SetActive(false);
        if (onLineStart != null) onLineStart(lineIndex);
        Render(0);
    }

    void Close(bool invokeCallback)
    {
        ClosedFrame = Time.frameCount;
        open = false;
        typing = false;
        onLineStart = null;
        var cb = onFinished;
        onFinished = null;
        gameObject.SetActive(false);
        if (invokeCallback && cb != null) cb();
    }

    public void CloseImmediate()
    {
        lines.Clear();
        Close(false);
    }

    void Render(int n)
    {
        if (n >= full.Length)
        {
            bodyText.text = full;
        }
        else
        {
            // hidden remainder keeps word-wrap stable while typing
            bodyText.text = full.Substring(0, n) + "<color=#00000000>" + full.Substring(n) + "</color>";
        }
    }

    static bool AdvancePressed()
    {
        var k = Keyboard.current;
        var m = Mouse.current;
        if (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame ||
                          k.numpadEnterKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame)) return true;
        if (m != null && m.leftButton.wasPressedThisFrame) return true;
        return false;
    }

    void Update()
    {
        if (!open) return;

        bool advance = Time.frameCount > openedFrame && AdvancePressed();

        if (typing)
        {
            if (advance) shown = full.Length;
            else shown += CharsPerSecond * Time.unscaledDeltaTime;

            int n = Mathf.Min((int)shown, full.Length);
            if (n != lastShown)
            {
                if (n > 0 && full[n - 1] != ' ' && n % 2 == 0) SoundFX.Play(SoundFX.Blip, 0.35f);
                lastShown = n;
            }
            Render(n);

            if (n >= full.Length)
            {
                typing = false;
                arrowText.gameObject.SetActive(true);
            }
            return;
        }

        // waiting for the player
        float blink = Mathf.Repeat(Time.unscaledTime, 0.9f) < 0.5f ? 1f : 0.15f;
        arrowText.color = new Color(UIKit.Cyan.r, UIKit.Cyan.g, UIKit.Cyan.b, blink);

        if (advance)
        {
            SoundFX.Play(SoundFX.Move, 0.4f);
            NextLine();
        }
    }
}
