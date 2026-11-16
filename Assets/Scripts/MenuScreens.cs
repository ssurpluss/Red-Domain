using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// ---------------------------------------------------------------
// Scrolling starfield (three layers)
// ---------------------------------------------------------------
public class Starfield : MonoBehaviour
{
    RawImage far;
    RawImage near;

    public static Starfield Create(Transform parent)
    {
        var rt = UIKit.Rect("Starfield", parent);
        UIKit.Stretch(rt);
        var sf = rt.gameObject.AddComponent<Starfield>();
        sf.Build();
        return sf;
    }

    void Build()
    {
        AddLayer("Back", PixelArt.StarLayer(480, 270, 70, 11, true));
        far = AddLayer("Far", PixelArt.StarLayer(480, 270, 90, 21, false));
        near = AddLayer("Near", PixelArt.StarLayer(240, 135, 25, 33, false));
    }

    RawImage AddLayer(string layerName, Texture2D tex)
    {
        var rt = UIKit.Rect(layerName, transform);
        UIKit.Stretch(rt);
        var ri = rt.gameObject.AddComponent<RawImage>();
        ri.texture = tex;
        ri.raycastTarget = false;
        return ri;
    }

    void Update()
    {
        float t = Time.unscaledTime;
        far.uvRect = new Rect(t * 0.004f, 0f, 1f, 1f);
        near.uvRect = new Rect(t * 0.012f, 0f, 1f, 1f);
    }
}

// ---------------------------------------------------------------
// Main menu
// ---------------------------------------------------------------
public class MainMenuScreen : MonoBehaviour
{
    GameBootstrap game;
    Text[] labels;
    int index;
    readonly string[] names = { "PLAY", "QUIT" };

    public void Build(GameBootstrap g)
    {
        game = g;
        Starfield.Create(transform);

        var mars = UIKit.Img("Mars", transform, PixelArt.Mars(96, 5), Color.white);
        UIKit.Anchor(mars.rectTransform, new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-250f, 120f), new Vector2(960f, 960f));

        var title = UIKit.Label("Title", transform, "RED DOMAIN", 120, UIKit.Orange, TextAnchor.MiddleLeft);
        UIKit.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(110f, -110f), new Vector2(1700f, 160f));
        var shadow = title.gameObject.AddComponent<Shadow>();
        shadow.effectDistance = new Vector2(8f, -8f);
        shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);

        var sub = UIKit.Label("Subtitle", transform, "A JUNIOR ASTRONAUT TRAINING SIMULATOR", 26, UIKit.Cyan, TextAnchor.MiddleLeft);
        UIKit.Anchor(sub.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(116f, -290f), new Vector2(1500f, 50f));

        var tag = UIKit.Label("Tagline", transform, "GOAT MISSION TO MARS", 34, UIKit.Yellow, TextAnchor.MiddleLeft);
        UIKit.Anchor(tag.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(116f, -350f), new Vector2(1500f, 60f));

        labels = new Text[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            int captured = i;
            var l = UIKit.Label("Item" + i, transform, names[i], 52, Color.white, TextAnchor.MiddleLeft);
            UIKit.Anchor(l.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(116f, -540f - i * 100f), new Vector2(500f, 80f));
            var b = l.gameObject.AddComponent<UIButton>();
            b.OnHover = () => SetIndex(captured);
            b.OnClick = () => Activate(captured);
            labels[i] = l;
        }

        var hint = UIKit.Label("Hint", transform, "W/S OR ARROWS: MOVE    ENTER: SELECT    MOUSE WORKS TOO", 20, UIKit.Dim, TextAnchor.LowerLeft);
        UIKit.Anchor(hint.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(116f, 60f), new Vector2(1500f, 40f));

        var credit = UIKit.Label("Credit", transform, "MADE FOR THE NASA SPACE APPS CHALLENGE", 18, UIKit.Dim, TextAnchor.LowerLeft);
        UIKit.Anchor(credit.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(116f, 24f), new Vector2(1500f, 30f));

        Refresh();
    }

    void SetIndex(int i)
    {
        int n = names.Length;
        i = ((i % n) + n) % n;
        if (i == index) return;
        index = i;
        SoundFX.Play(SoundFX.Move, 0.4f);
        Refresh();
    }

    void Refresh()
    {
        for (int i = 0; i < labels.Length; i++)
        {
            bool sel = i == index;
            labels[i].text = (sel ? "> " : "  ") + names[i];
            labels[i].color = sel ? UIKit.Yellow : Color.white;
        }
    }

    void Activate(int i)
    {
        if (game.IsTransitioning) return;
        SoundFX.Play(SoundFX.Confirm);

        if (i == 0)
        {
            game.Transition(game.ShowIntro);
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k == null) return;

        if (k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame) SetIndex(index - 1);
        if (k.sKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame) SetIndex(index + 1);
        if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) Activate(index);
    }
}

// ---------------------------------------------------------------
// Text intro: Mechaguru explains the lore while a rocket flies to Mars
// ---------------------------------------------------------------
public class IntroScreen : MonoBehaviour
{
    static readonly string[] Pages =
    {
        "INCOMING TRANSMISSION... Hello, little goat. I am MECHAGURU, the AI that will guide your mission.",
        "Long ago, humans experimented on goats. A lot. Like, a LOT. Nobody asked the goats how they felt about it.",
        "Then it worked. The SUPER-GOATS were born, a name proudly coined by President Don himself.",
        "Soon the super-goats unlocked advanced intelligence. Turns out they were just waiting for a good reason to be smart.",
        "So humanity gave them a mission: go to Mars and build a brand new civilization.",
        "And YOU are the GOAT. The first. The only goat sent to Mars for now. No pressure.",
        "Pack up to 40 tons of supplies, build a sealed base, and survive 30 minutes on the Red Planet.",
        "Dust storms, marsquakes, space debris and radiation are coming. Keep your panels running, your walls sealed and your belly full. Good luck, GOAT!"
    };

    GameBootstrap game;
    RectTransform rocket;
    RectTransform mars;
    float target;
    float current;
    bool leaving;

    public void Build(GameBootstrap g)
    {
        game = g;
        Starfield.Create(transform);

        var marsImg = UIKit.Img("Mars", transform, PixelArt.Mars(96, 5), Color.white);
        mars = marsImg.rectTransform;
        UIKit.Anchor(mars, new Vector2(1f, 0.58f), new Vector2(0.5f, 0.5f), new Vector2(260f, 0f), new Vector2(380f, 380f));

        var rocketImg = UIKit.Img("Rocket", transform, PixelArt.Rocket(), Color.white);
        rocket = rocketImg.rectTransform;
        UIKit.Anchor(rocket, new Vector2(0f, 0.58f), new Vector2(0.5f, 0.5f), new Vector2(160f, 0f), new Vector2(336f, 156f));

        var skip = UIKit.Label("Skip", transform, "ESC: SKIP", 22, UIKit.Dim, TextAnchor.UpperRight);
        UIKit.Anchor(skip.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(400f, 40f));

        g.dialogue.Open(Pages, Finish, "MECHAGURU", OnLine);
    }

    void OnLine(int i)
    {
        target = i / (float)(Pages.Length - 1);
    }

    void Finish()
    {
        Leave();
    }

    void Leave()
    {
        if (leaving) return;
        leaving = true;
        game.dialogue.CloseImmediate();
        game.Transition(game.ShowLoadout);
    }

    void Update()
    {
        if (leaving) return;

        var k = Keyboard.current;
        if (k != null && k.escapeKey.wasPressedThisFrame)
        {
            Leave();
            return;
        }

        current = Mathf.MoveTowards(current, target, Time.unscaledDeltaTime * 0.07f);
        float e = current * current * (3f - 2f * current);
        float t = Time.unscaledTime;

        rocket.anchoredPosition = new Vector2(Mathf.Lerp(160f, 1230f, e), Mathf.Sin(t * 2f) * 14f);
        mars.sizeDelta = Vector2.one * Mathf.Lerp(380f, 1000f, e);
        mars.anchoredPosition = new Vector2(Mathf.Lerp(260f, -150f, e), 0f);
    }
}

// ---------------------------------------------------------------
// Temporary screen for things we build in later milestones
// ---------------------------------------------------------------
public class PlaceholderScreen : MonoBehaviour
{
    GameBootstrap game;

    public void Build(GameBootstrap g, string title, string message)
    {
        game = g;
        Starfield.Create(transform);

        var t = UIKit.Label("Title", transform, title, 90, UIKit.Orange, TextAnchor.MiddleCenter);
        UIKit.Anchor(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(1700f, 140f));

        var m = UIKit.Label("Message", transform, message, 34, UIKit.Cyan, TextAnchor.MiddleCenter);
        UIKit.Anchor(m.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1700f, 100f));

        var h = UIKit.Label("Hint", transform, "ESC: BACK TO MENU", 22, UIKit.Dim, TextAnchor.MiddleCenter);
        UIKit.Anchor(h.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1000f, 40f));
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k != null && k.escapeKey.wasPressedThisFrame && !game.IsTransitioning)
        {
            SoundFX.Play(SoundFX.Back);
            game.Transition(game.ShowMainMenu);
        }
    }
}
