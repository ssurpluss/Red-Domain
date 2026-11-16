using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// Choose what to bring to Mars. 40 ton limit. Required items must meet their minimums.
public class LoadoutScreen : MonoBehaviour
{
    const int Rows = 7;
    const float RowH = 80f;
    static readonly Vector2 TL = new Vector2(0f, 1f);

    class Row
    {
        public RectTransform rect;
        public Image bg;
        public Text nameText;
        public Text qtyText;
        public Text weightText;
        public UIButton minus;
        public UIButton plus;
    }

    GameBootstrap game;
    readonly Row[] rows = new Row[Rows];
    int selected;
    int offset;
    bool leaving;
    int dialogueSeenFrame = -10;

    Text detailName, detailKind, detailStats, detailDesc, detailInCargo;
    Text statusText, barText, scrollText;
    RectTransform barFill;
    Image barFillImg;
    UIButton launchBtn;

    public void Build(GameBootstrap g)
    {
        game = g;
        Starfield.Create(transform);

        // ----- title -----
        var title = UIKit.Label("Title", transform, "LOADOUT", 64, UIKit.Orange, TextAnchor.MiddleLeft);
        UIKit.Anchor(title.rectTransform, TL, TL, new Vector2(60f, -30f), new Vector2(900f, 90f));

        var sub = UIKit.Label("Sub", transform, "PACK UP TO 40 TONS FOR MARS.   * = REQUIRED", 24, UIKit.Cyan, TextAnchor.MiddleLeft);
        UIKit.Anchor(sub.rectTransform, TL, TL, new Vector2(64f, -112f), new Vector2(1700f, 40f));

        // ----- left panel: item list -----
        var left = UIKit.Img("ListPanel", transform, PixelArt.ToSprite(PixelArt.Panel(180, 115, 21)), Color.white);
        UIKit.Anchor(left.rectTransform, TL, TL, new Vector2(60f, -165f), new Vector2(1080f, 690f));

        AddHeader(left.transform, "ITEM", 40f);
        AddHeader(left.transform, "QTY", 700f);
        AddHeader(left.transform, "WEIGHT", 880f);

        for (int i = 0; i < Rows; i++)
        {
            int slot = i;
            var r = new Row();

            r.rect = UIKit.Rect("Row" + i, left.transform);
            UIKit.Anchor(r.rect, TL, TL, new Vector2(24f, -70f - i * RowH), new Vector2(1032f, RowH - 4f));

            r.bg = r.rect.gameObject.AddComponent<Image>();
            r.bg.color = new Color(0f, 0f, 0f, 0f);
            r.bg.raycastTarget = false;

            var pick = r.rect.gameObject.AddComponent<UIButton>();
            pick.OnClick = () => Select(offset + slot);

            r.nameText = UIKit.Label("Name", r.rect, "", 24, Color.white, TextAnchor.MiddleLeft);
            UIKit.Anchor(r.nameText.rectTransform, TL, TL, new Vector2(16f, 0f), new Vector2(600f, RowH - 4f));

            r.minus = UIKit.Button("Minus", r.rect, "-", 36, TL, TL, new Vector2(624f, -8f), new Vector2(60f, 60f),
                () => Change(offset + slot, -Step()));

            r.qtyText = UIKit.Label("Qty", r.rect, "0", 30, Color.white, TextAnchor.MiddleCenter);
            UIKit.Anchor(r.qtyText.rectTransform, TL, TL, new Vector2(688f, 0f), new Vector2(90f, RowH - 4f));

            r.plus = UIKit.Button("Plus", r.rect, "+", 36, TL, TL, new Vector2(782f, -8f), new Vector2(60f, 60f),
                () => Change(offset + slot, Step()));

            r.weightText = UIKit.Label("Weight", r.rect, "", 24, Color.white, TextAnchor.MiddleRight);
            UIKit.Anchor(r.weightText.rectTransform, TL, TL, new Vector2(850f, 0f), new Vector2(172f, RowH - 4f));

            rows[i] = r;
        }

        scrollText = UIKit.Label("ScrollHint", left.transform, "", 18, UIKit.Dim, TextAnchor.MiddleLeft);
        UIKit.Anchor(scrollText.rectTransform, TL, TL, new Vector2(40f, -645f), new Vector2(1000f, 30f));

        // ----- right panel: item details -----
        var right = UIKit.Img("DetailPanel", transform, PixelArt.ToSprite(PixelArt.Panel(115, 115, 5)), Color.white);
        UIKit.Anchor(right.rectTransform, TL, TL, new Vector2(1170f, -165f), new Vector2(690f, 690f));

        detailName = UIKit.Label("DName", right.transform, "", 32, UIKit.Yellow, TextAnchor.UpperLeft);
        UIKit.Anchor(detailName.rectTransform, TL, TL, new Vector2(30f, -30f), new Vector2(630f, 110f));

        detailKind = UIKit.Label("DKind", right.transform, "", 20, UIKit.Cyan, TextAnchor.UpperLeft);
        UIKit.Anchor(detailKind.rectTransform, TL, TL, new Vector2(30f, -145f), new Vector2(630f, 30f));

        detailStats = UIKit.Label("DStats", right.transform, "", 22, Color.white, TextAnchor.UpperLeft);
        detailStats.lineSpacing = 1.3f;
        UIKit.Anchor(detailStats.rectTransform, TL, TL, new Vector2(30f, -190f), new Vector2(630f, 120f));

        detailDesc = UIKit.Label("DDesc", right.transform, "", 22, UIKit.Dim, TextAnchor.UpperLeft);
        detailDesc.lineSpacing = 1.3f;
        UIKit.Anchor(detailDesc.rectTransform, TL, TL, new Vector2(30f, -330f), new Vector2(630f, 270f));

        detailInCargo = UIKit.Label("DCargo", right.transform, "", 24, UIKit.Green, TextAnchor.UpperLeft);
        UIKit.Anchor(detailInCargo.rectTransform, TL, TL, new Vector2(30f, -620f), new Vector2(630f, 50f));

        // ----- weight bar -----
        var barFrame = UIKit.Img("BarFrame", transform, null, UIKit.Cyan);
        UIKit.Anchor(barFrame.rectTransform, TL, TL, new Vector2(60f, -872f), new Vector2(1800f, 60f));

        var barBack = UIKit.Img("BarBack", barFrame.transform, null, new Color(0.04f, 0.05f, 0.16f, 1f));
        var bb = barBack.rectTransform;
        bb.anchorMin = Vector2.zero;
        bb.anchorMax = Vector2.one;
        bb.offsetMin = new Vector2(5f, 5f);
        bb.offsetMax = new Vector2(-5f, -5f);

        barFillImg = UIKit.Img("Fill", barBack.transform, null, UIKit.Green);
        barFill = barFillImg.rectTransform;
        barFill.anchorMin = Vector2.zero;
        barFill.anchorMax = new Vector2(0f, 1f);
        barFill.offsetMin = Vector2.zero;
        barFill.offsetMax = Vector2.zero;

        barText = UIKit.Label("BarText", barFrame.transform, "", 28, Color.white, TextAnchor.MiddleCenter);
        UIKit.Stretch(barText.rectTransform);
        var sh = barText.gameObject.AddComponent<Shadow>();
        sh.effectDistance = new Vector2(3f, -3f);
        sh.effectColor = new Color(0f, 0f, 0f, 0.9f);

        // ----- status + buttons -----
        statusText = UIKit.Label("Status", transform, "", 22, UIKit.Yellow, TextAnchor.MiddleLeft);
        UIKit.Anchor(statusText.rectTransform, TL, TL, new Vector2(60f, -948f), new Vector2(980f, 80f));

        UIKit.Button("MinKit", transform, "MIN KIT", 24, TL, TL, new Vector2(1070f, -950f), new Vector2(250f, 76f), DoMinKit);
        UIKit.Button("Clear", transform, "CLEAR", 24, TL, TL, new Vector2(1340f, -950f), new Vector2(200f, 76f), DoClear);
        launchBtn = UIKit.Button("Launch", transform, "LAUNCH!", 32, TL, TL, new Vector2(1560f, -950f), new Vector2(300f, 76f), TryLaunch);

        var hint = UIKit.Label("Hint", transform,
            "W/S: SELECT    A/D: AMOUNT    SHIFT: x5    WHEEL: SCROLL    ENTER: LAUNCH    ESC: MENU",
            18, UIKit.Dim, TextAnchor.MiddleLeft);
        UIKit.Anchor(hint.rectTransform, TL, TL, new Vector2(60f, -1040f), new Vector2(1800f, 30f));

        Refresh();

        g.dialogue.Open(new[]
        {
            "Cargo time! Your ship can carry 40 tons. Walls, a door, solar panels, batteries, water, grass and a few big machines are REQUIRED. The rest is your call.",
            "Use W and S to pick an item and A and D to change the amount. Hold SHIFT to move 5 at a time. Press MIN KIT if you just want the basics.",
            "Warning: the bare minimum will not last 30 minutes. Extra grass, water and batteries could save your life. Glue is nice too. A disco ball is not."
        });
    }

    void AddHeader(Transform parent, string text, float x)
    {
        var h = UIKit.Label("Header", parent, text, 18, UIKit.Dim, TextAnchor.MiddleLeft);
        UIKit.Anchor(h.rectTransform, TL, TL, new Vector2(x, -30f), new Vector2(300f, 30f));
    }

    // ---------------- actions ----------------
    static int Step()
    {
        var k = Keyboard.current;
        return (k != null && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed)) ? 5 : 1;
    }

    void Select(int index)
    {
        index = Mathf.Clamp(index, 0, GameData.Items.Length - 1);
        if (index != selected) SoundFX.Play(SoundFX.Move, 0.4f);
        selected = index;
        if (selected < offset) offset = selected;
        if (selected >= offset + Rows) offset = selected - Rows + 1;
        Refresh();
    }

    void Scroll(int dir)
    {
        int maxOffset = Mathf.Max(0, GameData.Items.Length - Rows);
        offset = Mathf.Clamp(offset + dir, 0, maxOffset);
        if (selected < offset) selected = offset;
        if (selected >= offset + Rows) selected = offset + Rows - 1;
        Refresh();
    }

    void Change(int index, int delta)
    {
        if (index < 0 || index >= GameData.Items.Length) return;
        var def = GameData.Items[index];
        int before = GameData.Qty[index];
        int after = Mathf.Clamp(before + delta, 0, def.max);

        selected = index;
        if (after != before)
        {
            GameData.Qty[index] = after;
            SoundFX.Play(SoundFX.Move, 0.4f);
        }
        Refresh();
    }

    void DoMinKit()
    {
        GameData.MinKit();
        SoundFX.Play(SoundFX.Confirm, 0.5f);
        Refresh();
    }

    void DoClear()
    {
        GameData.Clear();
        SoundFX.Play(SoundFX.Back, 0.5f);
        Refresh();
    }

    void TryLaunch()
    {
        if (leaving || game.IsTransitioning) return;

        if (!GameData.Fits() || !GameData.MeetsMinimums())
        {
            SoundFX.Play(SoundFX.Alarm, 0.4f);
            return;
        }

        leaving = true;
        SoundFX.Play(SoundFX.Confirm);
        game.Transition(game.ShowBuild);
    }

    // ---------------- drawing ----------------
    void Refresh()
    {
        var items = GameData.Items;

        for (int i = 0; i < Rows; i++)
        {
            var r = rows[i];
            int idx = offset + i;
            bool has = idx < items.Length;
            r.rect.gameObject.SetActive(has);
            if (!has) continue;

            var d = items[idx];
            int q = GameData.Qty[idx];
            bool sel = idx == selected;

            r.bg.color = sel ? new Color(0.16f, 0.26f, 0.55f, 0.95f) : new Color(0f, 0f, 0f, 0f);
            r.nameText.text = (sel ? "> " : "  ") + d.name.ToUpperInvariant() + (d.Required ? " *" : "");
            r.nameText.color = sel ? UIKit.Yellow : Color.white;

            r.qtyText.text = q.ToString();
            if (d.Required) r.qtyText.color = q >= d.min ? UIKit.Green : UIKit.Red;
            else r.qtyText.color = q > 0 ? Color.white : UIKit.Dim;

            r.weightText.text = GameData.Fmt(q * d.weight) + " T";
            r.minus.Interactable = q > 0;
            r.plus.Interactable = q < d.max;
        }

        // details of the selected item
        var sd = items[selected];
        int sq = GameData.Qty[selected];
        detailName.text = sd.name.ToUpperInvariant();
        detailKind.text = sd.kind;
        detailStats.text =
            "WEIGHT: " + GameData.Fmt(sd.weight) + " T EACH\n" +
            "REQUIRED: " + (sd.min > 0 ? sd.min + " MINIMUM" : "NO") + "\n" +
            "MAX: " + (sd.max >= 99 ? "UNLIMITED" : sd.max.ToString());
        detailDesc.text = sd.desc;
        detailInCargo.text = "IN CARGO: " + sq + "  (" + GameData.Fmt(sq * sd.weight) + " T)";

        int maxOffset = Mathf.Max(0, items.Length - Rows);
        scrollText.text = "MOUSE WHEEL OR W/S TO SCROLL   " + (offset > 0 ? "^ " : "") + (offset < maxOffset ? "v" : "");

        // weight bar
        float total = GameData.TotalWeight();
        float frac = Mathf.Clamp01(total / GameData.WeightLimit);
        bool over = !GameData.Fits();
        bool meets = GameData.MeetsMinimums();

        barFill.anchorMax = new Vector2(frac, 1f);
        barFill.offsetMin = Vector2.zero;
        barFill.offsetMax = Vector2.zero;
        barFillImg.color = over ? UIKit.Red : (frac > 0.85f ? UIKit.Yellow : UIKit.Green);
        barText.text = "LOAD " + GameData.Fmt(total) + " / " + GameData.Fmt(GameData.WeightLimit) + " TONS" + (over ? "   OVER LIMIT!" : "");

        if (over)
        {
            statusText.text = "OVER THE LIMIT BY " + GameData.Fmt(total - GameData.WeightLimit) + " T. REMOVE SOMETHING.";
            statusText.color = UIKit.Red;
        }
        else if (!meets)
        {
            statusText.text = GameData.MissingSummary(3);
            statusText.color = UIKit.Yellow;
        }
        else
        {
            statusText.text = "READY FOR LAUNCH!  " + GameData.Fmt(GameData.WeightLimit - total) + " T TO SPARE.";
            statusText.color = UIKit.Green;
        }

        launchBtn.Interactable = !over && meets;
    }

    void Update()
    {
        if (leaving || game.IsTransitioning) return;

        // ignore input while Mechaguru is talking (and for a moment after)
        if (game.dialogue.IsOpen)
        {
            dialogueSeenFrame = Time.frameCount;
            return;
        }
        if (Time.frameCount - dialogueSeenFrame < 3) return;

        var k = Keyboard.current;
        if (k != null)
        {
            if (k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame) Select(selected - 1);
            if (k.sKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame) Select(selected + 1);
            if (k.aKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame) Change(selected, -Step());
            if (k.dKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame) Change(selected, Step());
            if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame) TryLaunch();
            if (k.escapeKey.wasPressedThisFrame)
            {
                leaving = true;
                SoundFX.Play(SoundFX.Back);
                game.Transition(game.ShowMainMenu);
                return;
            }
        }

        var m = Mouse.current;
        if (m != null)
        {
            float wheel = m.scroll.ReadValue().y;
            if (wheel > 0.1f) Scroll(-1);
            else if (wheel < -0.1f) Scroll(1);
        }
    }
}
