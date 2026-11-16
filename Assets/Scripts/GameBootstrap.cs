using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Attach this ONE script to an empty GameObject. It builds the whole game from code.
public class GameBootstrap : MonoBehaviour
{
    public DialogueBox dialogue;

    RectTransform screenRoot;
    Image fader;
    GameObject currentScreen;
    bool transitioning;

    public bool IsTransitioning { get { return transitioning; } }

    void Awake()
    {
        Application.targetFrameRate = 60;
        UIButton.Locked = false;

        SoundFX.Init(gameObject);
        SetupCamera();

        var canvas = UIKit.CreateCanvas(transform);

        // order matters: screens at the bottom, dialogue above them, fade on top
        screenRoot = UIKit.Rect("Screens", canvas.transform);
        UIKit.Stretch(screenRoot);

        dialogue = DialogueBox.Create(canvas.transform);

        var fr = UIKit.Rect("Fader", canvas.transform);
        UIKit.Stretch(fr);
        fader = fr.gameObject.AddComponent<Image>();
        fader.color = new Color(0f, 0f, 0f, 0f);
        fader.raycastTarget = false;
        fr.gameObject.SetActive(false);

        ShowMainMenu();
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
        }

        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.orthographic = true;
        cam.orthographicSize = 7.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.02f, 0.03f, 1f);

        if (cam.GetComponent<AudioListener>() == null)
            cam.gameObject.AddComponent<AudioListener>();
    }

    // ---------------- Screen switching ----------------
    GameObject NewScreen(string screenName)
    {
        if (currentScreen != null) Destroy(currentScreen);
        dialogue.CloseImmediate();

        var go = new GameObject(screenName, typeof(RectTransform));
        go.transform.SetParent(screenRoot, false);
        UIKit.Stretch((RectTransform)go.transform);
        currentScreen = go;
        return go;
    }

    public void ShowMainMenu()
    {
        if (Camera.main != null) Camera.main.transform.position = new Vector3(0f, 0f, -10f);
        NewScreen("MainMenu").AddComponent<MainMenuScreen>().Build(this);
    }

    public void ShowIntro()
    {
        NewScreen("Intro").AddComponent<IntroScreen>().Build(this);
    }

    public void ShowLoadout()
    {
        GameData.Clear();
        NewScreen("Loadout").AddComponent<LoadoutScreen>().Build(this);
    }

    public void ShowBuild()
    {
        NewScreen("BaseWorld").AddComponent<BaseWorld>().Build(this);
    }

    // ---------------- Fade transitions ----------------
    public void Transition(Action swap)
    {
        if (transitioning) return;
        StartCoroutine(TransitionRoutine(swap));
    }

    IEnumerator TransitionRoutine(Action swap)
    {
        transitioning = true;
        UIButton.Locked = true;

        yield return Fade(0f, 1f, 0.35f);
        if (swap != null) swap();
        yield return Fade(1f, 0f, 0.35f);

        UIButton.Locked = false;
        transitioning = false;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        fader.gameObject.SetActive(true);
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            SetFaderAlpha(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        SetFaderAlpha(to);
        if (to <= 0f) fader.gameObject.SetActive(false);
    }

    void SetFaderAlpha(float a)
    {
        fader.color = new Color(0f, 0f, 0f, a);
    }
}