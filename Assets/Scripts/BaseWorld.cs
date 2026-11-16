using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// Complete Base Construction + Survival, Disasters, Meteorites, Fixed Wire Repair Puzzle, Goat Landing Pod & Shelter Logic.
public class BaseWorld : MonoBehaviour
{
    const int MapW = 112;
    const int MapH = 112;
    const int MountainBorder = 6; // Unclimbable perimeter mountain range

    // Dedicated Outdoor Solar Array Zone (6x16) just west of the main base grid
    const int SolarX = 41;
    const int SolarW = 6;

    // Main Base Surveyor Grid (24x16)
    const int BuildX = 48;
    const int BuildY = 48;
    const int BuildW = 24;
    const int BuildH = 16;

    const int HotbarSlots = 9;
    const int SecondaryCell = -2; // Right-hand half of a 2x1 Solar Panel

    // Expedition ration caps so the player must venture out for meteorites instead of base-camping
    const int MaxStartingWater = 5;
    const int MaxStartingGrass = 5;

    GameBootstrap game;
    GameObject worldRoot;

    // Grid state
    readonly int[,] grid = new int[MapW, MapH];
    readonly bool[,] terrainSolid = new bool[MapW, MapH];
    readonly bool[,] cellDamaged = new bool[MapW, MapH];
    readonly SpriteRenderer[,] cellRenderers = new SpriteRenderer[MapW, MapH];
    readonly GameObject[,] wallGlows = new GameObject[MapW, MapH];
    readonly GameObject[,] machineEffects = new GameObject[MapW, MapH];
    readonly SpriteRenderer[,] floorRenderers = new SpriteRenderer[MapW, MapH];
    readonly SpriteRenderer[,] indicatorRenderers = new SpriteRenderer[MapW, MapH];
    readonly bool[,] enclosed = new bool[MapW, MapH];   // Air-tight sealed tiles (all perimeter walls intact)
    readonly bool[,] indoorTile = new bool[MapW, MapH]; // Physically indoors (sheltered from storms/quakes even if a wall cracks)

    // Build phase inventory
    readonly int[] unplaced = new int[GameData.Items.Length];
    readonly List<ItemId> buildPalette = new List<ItemId>();
    int paletteIndex;
    int hotbarOffset;
    float scrollCooldown;

    // Survival phase hotbar (equipment + collected non-placeable consumables)
    class SurvivalSlot
    {
        public string id;
        public string name;
        public int count;
        public bool isEquipment;
    }
    readonly List<SurvivalSlot> survivalSlots = new List<SurvivalSlot>();
    int survSlotIndex;
    int survHotbarOffset;

    // Goat state
    Transform goatTf;
    SpriteRenderer goatSr;
    Vector2 goatPos;
    int goatDir;
    float walkTimer;
    bool wasOnDoor;
    float goatFlavorGlowTimer;

    // Floating world popups
    class FloatingNote
    {
        public Transform tf;
        public TextMesh tm;
        public float elapsed;
        public float duration;
    }
    readonly List<FloatingNote> floatingNotes = new List<FloatingNote>();

    // Build mode & click-through protection
    bool deleteMode;
    bool blockPlacementUntilMouseUp;
    SpriteRenderer ghostSr;
    Vector2Int lastDragCell = new Vector2Int(-1, -1);
    int lastDragBtn;

    // Base & UI state
    bool baseSealed;
    bool greetedSeal;
    bool consoleOpen;
    bool clearPopupOpen;
    bool repairPopupOpen;
    bool survivalStarted;
    bool gameEnded;

    // Survival Vitals & Timer
    float survivalTimeLeft = 30f * 60f; // 30:00
    float timeScaleMult = 1f;
    float health = 100f;
    float oxygen = 100f;
    float power = 100f;
    float hunger = 100f;
    float thirst = 100f;
    float radiation = 0f;

    // Disasters (Red Dust Storms & Marsquakes with Ground Cracks)
    bool dustStormActive;
    bool quakeActive;
    float disasterRemaining;
    float nextDisasterTimer = 48f; // Longer calm window between disasters
    float cameraShakeTimer;
    int disastersSurvived;
    int disastersTriggered;
    bool lastDisasterWasDustStorm;
    readonly List<GameObject> quakeCracks = new List<GameObject>();

    // Red Pokemon-style Sandstorm Overlay
    Image stormTintImg;
    readonly RectTransform[] dustStreaks = new RectTransform[26];
    readonly Vector2[] dustSpeeds = new Vector2[26];

    // Rover, Landing Pad & Goat Landing Pod
    Transform roverTf;
    SpriteRenderer roverSr;
    SpriteRenderer roverBeaconSr;
    Vector2 roverPadPos;
    Vector2 landingPodPos;
    Vector2 roverPos;
    int roverDir = 3;
    bool drivingRover;

    // Rare Unannounced Falling Meteorites
    class FallingMeteor
    {
        public Vector2 startPos;
        public Vector2 targetPos;
        public float elapsed;
        public float duration;
        public Transform tf;
    }
    class MeteorSite
    {
        public Vector2 pos;
        public GameObject go;
    }
    readonly List<FallingMeteor> fallingMeteors = new List<FallingMeteor>();
    readonly List<MeteorSite> meteorSites = new List<MeteorSite>();
    float nextMeteorTimer = 26f;
    int meteoritesHarvested;

    // Among Us Wiring Repair Puzzle State
    Vector2Int repairTargetCell;
    int selectedLeftWire = -1;
    readonly int[] rightWireColor = new int[4];
    readonly bool[] wireConnected = new bool[4];
    readonly UIButton[] leftWireBtns = new UIButton[4];
    readonly UIButton[] rightWireBtns = new UIButton[4];
    readonly Image[] leftWireImgs = new Image[4];
    readonly Image[] rightWireImgs = new Image[4];
    readonly Image[] leftWireSwatches = new Image[4];
    readonly Image[] rightWireSwatches = new Image[4];
    readonly Text[] leftWireTexts = new Text[4];
    readonly Text[] rightWireTexts = new Text[4];
    readonly RectTransform[] wireLinks = new RectTransform[4];
    readonly Image[] wireLinkImgs = new Image[4];
    Text repairTitleText, repairStatusText;
    float repairCloseDelay;

    static readonly Color[] WireColors =
    {
        new Color(0.96f, 0.24f, 0.24f, 1f), // Red
        new Color(0.20f, 0.85f, 1.00f, 1f), // Cyan
        new Color(1.00f, 0.84f, 0.20f, 1f), // Gold
        new Color(0.26f, 0.94f, 0.42f, 1f)  // Green
    };
    static readonly string[] WireNames = { "RED", "CYAN", "GOLD", "GREEN" };

    // HUD Vitals Progress Bars (HP, O2, PWR, FOOD, H2O, RAD)
    GameObject vitalsBarRoot;
    readonly RectTransform[] vitalFillRects = new RectTransform[6];
    readonly Image[] vitalFillImgs = new Image[6];
    readonly Text[] vitalValueTexts = new Text[6];

    // HUD & End/Death Screen references
    Text phaseText, objectiveText, radarText, cursorText, hotbarHintText, consoleBodyText;
    Text endTitleText, endCauseText, endTipText, endStatsText;
    GameObject consolePopup, clearPopup, repairPopup, endPopup;
    UIButton startSurvivalBtn, modeToggleBtn, clearBaseBtn, speedBtn, consumeBtn;
    readonly Image[] slotBgs = new Image[HotbarSlots];
    readonly Text[] slotNums = new Text[HotbarSlots];
    readonly Text[] slotNames = new Text[HotbarSlots];
    readonly Text[] slotCounts = new Text[HotbarSlots];

    static readonly ItemId[] EssentialInsideItems =
    {
        ItemId.Battery, ItemId.Water, ItemId.Grass, ItemId.Purifier, ItemId.Console
    };

    static int GetRequiredInsideCount(ItemId id)
    {
        if (id == ItemId.Water || id == ItemId.Grass) return 1;
        return GameData.Get(id).min;
    }

    static bool IsWallMountedAppliance(ItemId id)
    {
        return id == ItemId.Purifier || id == ItemId.Console || id == ItemId.Tastifier;
    }

    public void Build(GameBootstrap g)
    {
        game = g;

        if (GameData.TotalWeight() <= 0.01f)
        {
            GameData.MinKit();
            if (GameData.Qty[(int)ItemId.Glue] < 1)
                GameData.Qty[(int)ItemId.Glue] = 1;
        }

        // Enforce expedition ration limits (max 5 Water & 5 Grass) so meteorites remain vital
        if (GameData.Qty[(int)ItemId.Water] > MaxStartingWater)
            GameData.Qty[(int)ItemId.Water] = MaxStartingWater;
        if (GameData.Qty[(int)ItemId.Grass] > MaxStartingGrass)
            GameData.Qty[(int)ItemId.Grass] = MaxStartingGrass;

        for (int i = 0; i < GameData.Items.Length; i++)
        {
            var id = GameData.Items[i].id;
            int idx = (int)id;
            unplaced[idx] = GameData.Qty[idx];
            if (id != ItemId.Rover && id != ItemId.Glue && id != ItemId.Shield && unplaced[idx] > 0)
                buildPalette.Add(id);
        }

        BuildWorldObjects();
        BuildHUD();
        RefreshHotbar();
        RecomputeBaseState(false);

        game.dialogue.Open(new[]
        {
            "Touchdown on Mars! Your G.O.A.T.-1 Landing Pod is parked beside the Rover Pad.",
            "Left-click in the blue grid to place items, or press F for Delete Mode. Use keys 1 to 9 or the hotbar arrows to switch items.",
            "Build an air-tight room with Metal Walls and 1 Airlock Door. Place Solar Panels OUTSIDE, and place wall appliances (Purifier, Console, Tastifier) RIGHT NEXT TO A WALL."
        });
    }

    void OnDestroy()
    {
        if (worldRoot != null)
        {
            Destroy(worldRoot);
            worldRoot = null;
        }
    }

    // ---------------- World Construction ----------------
    void BuildWorldObjects()
    {
        var oldWorld = GameObject.Find("MarsWorld");
        if (oldWorld != null) Destroy(oldWorld);

        worldRoot = new GameObject("MarsWorld");
        var rnd = new System.Random(42);

        roverPadPos = new Vector2(BuildX + BuildW * 0.5f, BuildY - 3.0f);
        landingPodPos = new Vector2(roverPadPos.x + 6.5f, roverPadPos.y - 0.5f);

        for (int y = 0; y < MapH; y++)
        {
            for (int x = 0; x < MapW; x++)
            {
                grid[x, y] = -1;

                var tileGo = new GameObject("T_" + x + "_" + y);
                tileGo.transform.SetParent(worldRoot.transform, false);
                tileGo.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0f);
                var sr = tileGo.AddComponent<SpriteRenderer>();
                sr.sprite = WorldArt.MarsGround(rnd.Next(3));
                sr.sortingOrder = 0;

                bool isBorderMountain = x < MountainBorder || x >= MapW - MountainBorder ||
                                        y < MountainBorder || y >= MapH - MountainBorder;
                if (isBorderMountain)
                {
                    terrainSolid[x, y] = true;
                    var mtnGo = new GameObject("Mtn");
                    mtnGo.transform.SetParent(tileGo.transform, false);
                    var msr = mtnGo.AddComponent<SpriteRenderer>();
                    msr.sprite = WorldArt.MarsMountain(rnd.Next(3));
                    msr.sortingOrder = 2;
                    continue;
                }

                if (InMainBuildZone(x, y) || InSolarZone(x, y))
                {
                    var slotGo = new GameObject("Slot");
                    slotGo.transform.SetParent(tileGo.transform, false);
                    var ssr = slotGo.AddComponent<SpriteRenderer>();
                    ssr.sprite = InSolarZone(x, y) ? WorldArt.SolarZoneSlot() : WorldArt.BuildZoneSlot();
                    ssr.sortingOrder = 1;
                    floorRenderers[x, y] = ssr;

                    var indGo = new GameObject("Indicator");
                    indGo.transform.SetParent(tileGo.transform, false);
                    var isr = indGo.AddComponent<SpriteRenderer>();
                    isr.sortingOrder = 2;
                    isr.enabled = false;
                    indicatorRenderers[x, y] = isr;
                }
                else if (!NearBaseClearing(x, y))
                {
                    int roll = rnd.Next(100);
                    if (roll < 3)
                    {
                        terrainSolid[x, y] = true;
                        var bGo = new GameObject("Boulder");
                        bGo.transform.SetParent(tileGo.transform, false);
                        var bsr = bGo.AddComponent<SpriteRenderer>();
                        bsr.sprite = WorldArt.MarsBoulder(rnd.Next(2));
                        bsr.sortingOrder = 2;
                    }
                    else if (roll < 11)
                    {
                        var pGo = new GameObject("Pebbles");
                        pGo.transform.SetParent(tileGo.transform, false);
                        var psr = pGo.AddComponent<SpriteRenderer>();
                        psr.sprite = WorldArt.MarsPebbles(rnd.Next(2));
                        psr.sortingOrder = 1;
                    }
                }

                var objGo = new GameObject("Obj");
                objGo.transform.SetParent(tileGo.transform, false);
                var osr = objGo.AddComponent<SpriteRenderer>();
                osr.sortingOrder = 3;
                osr.enabled = false;
                cellRenderers[x, y] = osr;
            }
        }

        // High-visibility 3x3 Hazard Rover Landing Pad right outside the south base entrance
        var padGo = new GameObject("RoverLandingPad");
        padGo.transform.SetParent(worldRoot.transform, false);
        padGo.transform.position = new Vector3(roverPadPos.x, roverPadPos.y, 0f);
        var padSr = padGo.AddComponent<SpriteRenderer>();
        padSr.sprite = WorldArt.RoverPad();
        padSr.sortingOrder = 1;

        // Big 5x5 Goat-Themed Landing Pod right next to the Rover Pad
        var podGo = new GameObject("GoatLandingPod");
        podGo.transform.SetParent(worldRoot.transform, false);
        podGo.transform.position = new Vector3(landingPodPos.x, landingPodPos.y, 0f);
        var podSr = podGo.AddComponent<SpriteRenderer>();
        podSr.sprite = WorldArt.GoatLandingPod();
        podSr.sortingOrder = 3;

        // Solid hull collision for the landing pod footprint
        int podMinX = Mathf.RoundToInt(landingPodPos.x - 2.5f);
        int podMinY = Mathf.RoundToInt(landingPodPos.y - 2.5f);
        for (int py = podMinY; py < podMinY + 4; py++)
            for (int px = podMinX; px < podMinX + 5; px++)
                if (px >= 0 && px < MapW && py >= 0 && py < MapH)
                    terrainSolid[px, py] = true;

        // Scatter 28 Impact Craters across the Martian plains
        int cratersPlaced = 0;
        for (int attempt = 0; attempt < 300 && cratersPlaced < 28; attempt++)
        {
            int cx = rnd.Next(MountainBorder + 3, MapW - MountainBorder - 4);
            int cy = rnd.Next(MountainBorder + 3, MapH - MountainBorder - 4);
            if (NearBaseClearing(cx, cy) || NearBaseClearing(cx + 1, cy + 1)) continue;
            if (terrainSolid[cx, cy] || terrainSolid[cx + 1, cy] || terrainSolid[cx, cy + 1] || terrainSolid[cx + 1, cy + 1]) continue;

            var crGo = new GameObject("Crater_" + cratersPlaced);
            crGo.transform.SetParent(worldRoot.transform, false);
            crGo.transform.position = new Vector3(cx + 1f, cy + 1f, 0f);
            var csr = crGo.AddComponent<SpriteRenderer>();
            csr.sprite = WorldArt.MarsCrater(cratersPlaced % 2);
            csr.sortingOrder = 1;
            cratersPlaced++;
        }

        // Spawn Rover right on the Hazard Landing Pad if packed in loadout
        if (GameData.Count(ItemId.Rover) > 0)
        {
            var rGo = new GameObject("MarsRover");
            rGo.transform.SetParent(worldRoot.transform, false);
            roverTf = rGo.transform;
            roverPos = roverPadPos;
            roverTf.position = new Vector3(roverPos.x, roverPos.y, 0f);
            roverSr = rGo.AddComponent<SpriteRenderer>();
            roverSr.sprite = WorldArt.RoverSprite(3, false);
            roverSr.sortingOrder = 5;

            var bGo = new GameObject("RoverBeacon");
            bGo.transform.SetParent(rGo.transform, false);
            bGo.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            roverBeaconSr = bGo.AddComponent<SpriteRenderer>();
            roverBeaconSr.sprite = WorldArt.WallGlow();
            roverBeaconSr.color = new Color(1f, 0.85f, 0.2f, 0.95f);
            roverBeaconSr.sortingOrder = 6;
        }

        var ghostGo = new GameObject("GhostCursor");
        ghostGo.transform.SetParent(worldRoot.transform, false);
        ghostSr = ghostGo.AddComponent<SpriteRenderer>();
        ghostSr.sprite = WorldArt.GhostTile();
        ghostSr.sortingOrder = 4;

        var goatGo = new GameObject("PlayerGoat");
        goatGo.transform.SetParent(worldRoot.transform, false);
        goatTf = goatGo.transform;
        goatPos = new Vector2(BuildX + BuildW * 0.5f, BuildY - 1.2f);
        goatTf.position = new Vector3(goatPos.x, goatPos.y, 0f);
        goatSr = goatGo.AddComponent<SpriteRenderer>();
        goatSr.sprite = WorldArt.Goat(0, 0);
        goatSr.sortingOrder = 5;

        SnapCamera();
    }

    bool NearBaseClearing(int x, int y)
    {
        return x >= SolarX - 4 && x <= BuildX + BuildW + 4 && y >= BuildY - 7 && y <= BuildY + BuildH + 4;
    }

    // ---------------- HUD, Sandstorm Overlay & Visual Vitals Bars ----------------
    void BuildHUD()
    {
        Vector2 TL = new Vector2(0f, 1f);
        Vector2 TR = new Vector2(1f, 1f);
        Vector2 BC = new Vector2(0.5f, 0f);

        // Full-screen Red Pokemon-style Desert Sandstorm Overlay (placed behind HUD banners)
        var stormRoot = UIKit.Rect("RedSandstormOverlay", transform);
        stormRoot.SetAsFirstSibling();
        stormRoot.anchorMin = Vector2.zero;
        stormRoot.anchorMax = Vector2.one;
        stormRoot.offsetMin = Vector2.zero;
        stormRoot.offsetMax = Vector2.zero;

        stormTintImg = stormRoot.gameObject.AddComponent<Image>();
        stormTintImg.color = new Color(0.82f, 0.14f, 0.08f, 0f);
        stormTintImg.raycastTarget = false;

        var streakSprite = WorldArt.DustStreak();
        var rnd = new System.Random(77);
        for (int i = 0; i < dustStreaks.Length; i++)
        {
            var sImg = UIKit.Img("DustStreak_" + i, stormRoot, streakSprite, new Color(1f, 0.42f, 0.30f, 0f));
            var sr = sImg.rectTransform;
            float w = rnd.Next(130, 260);
            float h = rnd.Next(16, 28);
            sr.sizeDelta = new Vector2(w, h);
            sr.anchoredPosition = new Vector2(rnd.Next(-960, 960), rnd.Next(-540, 540));
            sr.localRotation = Quaternion.Euler(0f, 0f, 16f);
            dustStreaks[i] = sr;
            dustSpeeds[i] = new Vector2(-rnd.Next(950, 1550), -rnd.Next(260, 440));
        }

        // Top Status Banner (taller to cleanly house the 6 visual vital bars + sensor strip)
        var topBox = UIKit.Img("TopBanner", transform, PixelArt.ToSprite(PixelArt.Panel(126, 22, 9)), Color.white);
        UIKit.Anchor(topBox.rectTransform, TL, TL, new Vector2(24f, -20f), new Vector2(1210f, 198f));

        phaseText = UIKit.Label("Phase", topBox.transform, "PHASE: BASE CONSTRUCTION", 22, UIKit.Orange, TextAnchor.UpperLeft);
        UIKit.Anchor(phaseText.rectTransform, TL, TL, new Vector2(28f, -16f), new Vector2(1150f, 32f));

        objectiveText = UIKit.Label("Objective", topBox.transform, "", 14, UIKit.Cyan, TextAnchor.UpperLeft);
        objectiveText.lineSpacing = 1.45f;
        UIKit.Anchor(objectiveText.rectTransform, TL, TL, new Vector2(28f, -54f), new Vector2(1150f, 120f));

        // Visual Vitals Bars (2 rows x 3 columns)
        vitalsBarRoot = UIKit.Rect("VitalsBarGrid", topBox.transform).gameObject;
        var vRect = (RectTransform)vitalsBarRoot.transform;
        UIKit.Anchor(vRect, TL, TL, new Vector2(28f, -54f), new Vector2(1154f, 94f));

        var whiteSp = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
        string[] barLabels = { "HP", "O2", "PWR", "FOOD", "H2O", "RAD" };
        Color[] barColors =
        {
            new Color(0.24f, 0.90f, 0.42f, 1f), // HP - Green
            new Color(0.24f, 0.86f, 1.00f, 1f), // O2 - Cyan
            new Color(1.00f, 0.82f, 0.22f, 1f), // PWR - Gold
            new Color(1.00f, 0.58f, 0.20f, 1f), // FOOD - Orange
            new Color(0.26f, 0.64f, 1.00f, 1f), // H2O - Blue
            new Color(0.85f, 0.30f, 0.95f, 1f)  // RAD - Violet/Crimson
        };

        float colW = 372f;
        float colGap = 18f;
        float rowH = 40f;
        float rowGap = 10f;

        for (int i = 0; i < 6; i++)
        {
            int col = i % 3;
            int row = i / 3;
            float bx = col * (colW + colGap);
            float by = -row * (rowH + rowGap);

            var cell = UIKit.Rect("VitalCell_" + barLabels[i], vRect);
            UIKit.Anchor(cell, TL, TL, new Vector2(bx, by), new Vector2(colW, rowH));

            // Dark recessed bar background
            var bgImg = UIKit.Img("TrackBg", cell, whiteSp, new Color(0.05f, 0.08f, 0.17f, 0.95f));
            UIKit.Anchor(bgImg.rectTransform, TL, TL, Vector2.zero, new Vector2(colW, rowH));

            // Subtle inner border
            var borderImg = UIKit.Img("TrackFrame", bgImg.transform, whiteSp, new Color(0.28f, 0.42f, 0.72f, 0.55f));
            var bfr = borderImg.rectTransform;
            bfr.anchorMin = Vector2.zero;
            bfr.anchorMax = Vector2.one;
            bfr.offsetMin = new Vector2(2f, 2f);
            bfr.offsetMax = new Vector2(-2f, -2f);

            var innerDark = UIKit.Img("InnerDark", borderImg.transform, whiteSp, new Color(0.04f, 0.06f, 0.14f, 0.95f));
            var idr = innerDark.rectTransform;
            idr.anchorMin = Vector2.zero;
            idr.anchorMax = Vector2.one;
            idr.offsetMin = new Vector2(2f, 2f);
            idr.offsetMax = new Vector2(-2f, -2f);

            // Dynamic fill bar
            var fillImg = UIKit.Img("Fill", innerDark.transform, whiteSp, barColors[i]);
            var fr = fillImg.rectTransform;
            fr.anchorMin = new Vector2(0f, 0f);
            fr.anchorMax = new Vector2(1f, 1f);
            fr.offsetMin = Vector2.zero;
            fr.offsetMax = Vector2.zero;
            vitalFillRects[i] = fr;
            vitalFillImgs[i] = fillImg;

            // Left label ("HP", "O2", etc.)
            var lbl = UIKit.Label("Lbl", cell, barLabels[i], 16, Color.white, TextAnchor.MiddleLeft);
            UIKit.Anchor(lbl.rectTransform, TL, TL, new Vector2(12f, 0f), new Vector2(140f, rowH));

            // Right value ("100%")
            var val = UIKit.Label("Val", cell, "100%", 17, Color.white, TextAnchor.MiddleRight);
            UIKit.Anchor(val.rectTransform, TL, TL, new Vector2(150f, 0f), new Vector2(colW - 162f, rowH));
            vitalValueTexts[i] = val;
        }
        vitalsBarRoot.SetActive(false);

        radarText = UIKit.Label("Radar", topBox.transform, "", 14, UIKit.Cyan, TextAnchor.UpperLeft);
        UIKit.Anchor(radarText.rectTransform, TL, TL, new Vector2(28f, -156f), new Vector2(1154f, 30f));
        radarText.gameObject.SetActive(false);

        // Top-right buttons
        modeToggleBtn = UIKit.Button("ModeToggleBtn", transform, "MODE: PLACE [F]", 16, TR, TR, new Vector2(-364f, -20f), new Vector2(276f, 54f), () =>
        {
            blockPlacementUntilMouseUp = true;
            ToggleDeleteMode();
        });

        clearBaseBtn = UIKit.Button("ClearBaseBtn", transform, "CLEAR BASE", 16, TR, TR, new Vector2(-164f, -20f), new Vector2(190f, 54f), () =>
        {
            if (game.IsTransitioning || consoleOpen || survivalStarted) return;
            blockPlacementUntilMouseUp = true;
            ToggleClearPopup(true);
        });

        speedBtn = UIKit.Button("SpeedBtn", transform, "SPEED: 1X [TAB]", 16, TR, TR, new Vector2(-164f, -20f), new Vector2(260f, 54f), ToggleTimeScale);
        speedBtn.gameObject.SetActive(false);

        UIKit.Button("MenuBtn", transform, "MENU", 16, TR, TR, new Vector2(-24f, -20f), new Vector2(130f, 54f), () =>
        {
            if (game.IsTransitioning) return;
            SoundFX.Play(SoundFX.Back);
            game.Transition(game.ShowMainMenu);
        });

        // Direct Consume / Equip button for Survival Hotbar
        consumeBtn = UIKit.Button("ConsumeBtn", transform, "USE / CONSUME [C]", 16, BC, BC, new Vector2(0f, 194f), new Vector2(360f, 44f), () =>
        {
            if (survivalStarted) UseSelectedSurvivalSlot();
        });
        consumeBtn.gameObject.SetActive(false);

        cursorText = UIKit.Label("CursorHint", transform, "", 16, UIKit.Yellow, TextAnchor.LowerCenter);
        UIKit.Anchor(cursorText.rectTransform, BC, BC, new Vector2(0f, 158f), new Vector2(1820f, 30f));

        hotbarHintText = UIKit.Label("HotbarHint", transform, "", 14, UIKit.Dim, TextAnchor.LowerCenter);
        UIKit.Anchor(hotbarHintText.rectTransform, BC, BC, new Vector2(0f, 134f), new Vector2(1700f, 22f));

        // Bottom 9-slot hotbar with < and > navigation buttons
        var bar = UIKit.Img("Hotbar", transform, PixelArt.ToSprite(PixelArt.Panel(152, 12, 12)), Color.white);
        UIKit.Anchor(bar.rectTransform, BC, BC, new Vector2(0f, 16f), new Vector2(1600f, 112f));

        UIKit.Button("HotbarPrev", bar.transform, "<", 24, TL, TL, new Vector2(12f, -10f), new Vector2(62f, 92f), () =>
        {
            blockPlacementUntilMouseUp = true;
            StepPalette(-1);
        });

        for (int i = 0; i < HotbarSlots; i++)
        {
            int slot = i;
            var r = UIKit.Rect("Slot" + i, bar.transform);
            UIKit.Anchor(r, TL, TL, new Vector2(84f + i * 160f, -10f), new Vector2(150f, 92f));

            slotBgs[i] = r.gameObject.AddComponent<Image>();
            slotBgs[i].raycastTarget = false;

            var btn = r.gameObject.AddComponent<UIButton>();
            btn.OnClick = () =>
            {
                blockPlacementUntilMouseUp = true;
                if (survivalStarted) SelectSurvivalSlot(survHotbarOffset + slot);
                else SelectPalette(hotbarOffset + slot);
            };

            slotNums[i] = UIKit.Label("Num", r, (i + 1).ToString(), 15, UIKit.Dim, TextAnchor.UpperLeft);
            UIKit.Anchor(slotNums[i].rectTransform, TL, TL, new Vector2(8f, -6f), new Vector2(30f, 22f));

            slotNames[i] = UIKit.Label("Name", r, "", 13, Color.white, TextAnchor.MiddleCenter);
            slotNames[i].lineSpacing = 1.15f;
            UIKit.Anchor(slotNames[i].rectTransform, TL, TL, new Vector2(6f, -24f), new Vector2(138f, 44f));

            slotCounts[i] = UIKit.Label("Count", r, "", 14, UIKit.Green, TextAnchor.LowerRight);
            UIKit.Anchor(slotCounts[i].rectTransform, TL, TL, new Vector2(6f, -66f), new Vector2(136f, 22f));
        }

        UIKit.Button("HotbarNext", bar.transform, ">", 24, TL, TL, new Vector2(1526f, -10f), new Vector2(62f, 92f), () =>
        {
            blockPlacementUntilMouseUp = true;
            StepPalette(1);
        });

        // Command Console Popup
        consolePopup = UIKit.Img("ConsolePopup", transform, PixelArt.ToSprite(PixelArt.Panel(110, 70, 31)), Color.white).gameObject;
        var pr = (RectTransform)consolePopup.transform;
        UIKit.Anchor(pr, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1140f, 660f));

        var cTitle = UIKit.Label("CTitle", pr, "COMMAND CONSOLE", 38, UIKit.Orange, TextAnchor.UpperCenter);
        UIKit.Anchor(cTitle.rectTransform, TL, TL, new Vector2(40f, -32f), new Vector2(1060f, 56f));

        consoleBodyText = UIKit.Label("CBody", pr, "", 20, Color.white, TextAnchor.UpperLeft);
        consoleBodyText.lineSpacing = 1.4f;
        UIKit.Anchor(consoleBodyText.rectTransform, TL, TL, new Vector2(50f, -115f), new Vector2(1040f, 390f));

        startSurvivalBtn = UIKit.Button("StartSurv", pr, "START SURVIVAL PHASE", 23, BC, BC, new Vector2(-180f, 44f), new Vector2(540f, 76f), OnStartSurvivalClicked);
        UIKit.Button("CloseConsole", pr, "CLOSE [ESC]", 22, BC, BC, new Vector2(280f, 44f), new Vector2(300f, 76f), () =>
        {
            blockPlacementUntilMouseUp = true;
            ToggleConsole(false);
        });
        consolePopup.SetActive(false);

        // Clear Base Failsafe Popup
        clearPopup = UIKit.Img("ClearPopup", transform, PixelArt.ToSprite(PixelArt.Panel(96, 44, 19)), Color.white).gameObject;
        var cr = (RectTransform)clearPopup.transform;
        UIKit.Anchor(cr, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 440f));

        var clrTitle = UIKit.Label("ClrTitle", cr, "CLEAR ENTIRE BASE?", 34, UIKit.Red, TextAnchor.UpperCenter);
        UIKit.Anchor(clrTitle.rectTransform, TL, TL, new Vector2(40f, -32f), new Vector2(880f, 50f));

        var clrBody = UIKit.Label("ClrBody", cr,
            "FAILSAFE WARNING: This will pick up every wall, door, solar panel, and machine and return them to your cargo.",
            20, Color.white, TextAnchor.MiddleCenter);
        clrBody.lineSpacing = 1.35f;
        UIKit.Anchor(clrBody.rectTransform, TL, TL, new Vector2(50f, -110f), new Vector2(860f, 170f));

        UIKit.Button("ConfirmClear", cr, "YES, CLEAR ALL", 22, BC, BC, new Vector2(-190f, 40f), new Vector2(340f, 74f), ExecuteClearBase);
        UIKit.Button("CancelClear", cr, "CANCEL", 22, BC, BC, new Vector2(190f, 40f), new Vector2(280f, 74f), () =>
        {
            blockPlacementUntilMouseUp = true;
            ToggleClearPopup(false);
        });
        clearPopup.SetActive(false);

        // Among Us Wiring Repair Mini-Game Popup
        BuildRepairPuzzlePopup();

        // Upgraded Victory / Game Over Screen with Clear Cause + Solution Tip
        BuildEndPopup();
    }

    void BuildEndPopup()
    {
        Vector2 TL = new Vector2(0f, 1f);
        Vector2 BC = new Vector2(0.5f, 0f);

        endPopup = UIKit.Img("EndPopup", transform, PixelArt.ToSprite(PixelArt.Panel(124, 78, 31)), Color.white).gameObject;
        var er = (RectTransform)endPopup.transform;
        UIKit.Anchor(er, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1260f, 760f));

        endTitleText = UIKit.Label("EndTitle", er, "MISSION FAILED", 40, UIKit.Red, TextAnchor.UpperCenter);
        UIKit.Anchor(endTitleText.rectTransform, TL, TL, new Vector2(40f, -30f), new Vector2(1180f, 56f));

        var causeBox = UIKit.Img("CauseBox", er, PixelArt.ToSprite(PixelArt.Panel(112, 12, 12)), new Color(0.95f, 0.72f, 0.72f, 1f));
        UIKit.Anchor(causeBox.rectTransform, TL, TL, new Vector2(55f, -104f), new Vector2(1150f, 84f));

        endCauseText = UIKit.Label("EndCause", causeBox.transform, "", 21, UIKit.Yellow, TextAnchor.MiddleCenter);
        UIKit.Anchor(endCauseText.rectTransform, TL, TL, new Vector2(24f, -10f), new Vector2(1102f, 64f));

        var tipBox = UIKit.Img("TipBox", er, PixelArt.ToSprite(PixelArt.Panel(112, 26, 19)), new Color(0.75f, 0.92f, 1f, 1f));
        UIKit.Anchor(tipBox.rectTransform, TL, TL, new Vector2(55f, -206f), new Vector2(1150f, 240f));

        endTipText = UIKit.Label("EndTip", tipBox.transform, "", 18, UIKit.Cyan, TextAnchor.UpperLeft);
        endTipText.lineSpacing = 1.45f;
        UIKit.Anchor(endTipText.rectTransform, TL, TL, new Vector2(32f, -22f), new Vector2(1086f, 196f));

        endStatsText = UIKit.Label("EndStats", er, "", 17, Color.white, TextAnchor.UpperCenter);
        endStatsText.lineSpacing = 1.4f;
        UIKit.Anchor(endStatsText.rectTransform, TL, TL, new Vector2(40f, -468f), new Vector2(1180f, 150f));

        UIKit.Button("EndMenuBtn", er, "RETURN TO LOADOUT / MENU", 23, BC, BC, new Vector2(0f, 38f), new Vector2(540f, 74f), () =>
        {
            if (game.IsTransitioning) return;
            SoundFX.Play(SoundFX.Confirm);
            game.Transition(game.ShowMainMenu);
        });
        endPopup.SetActive(false);
    }

    void BuildRepairPuzzlePopup()
    {
        Vector2 TL = new Vector2(0f, 1f);
        Vector2 BC = new Vector2(0.5f, 0f);

        repairPopup = UIKit.Img("RepairPopup", transform, PixelArt.ToSprite(PixelArt.Panel(104, 68, 31)), Color.white).gameObject;
        var rr = (RectTransform)repairPopup.transform;
        UIKit.Anchor(rr, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1060f, 660f));

        repairTitleText = UIKit.Label("RepTitle", rr, "SUPER GOAT-GLUE 4000 — WIRE & HULL PATCH", 28, UIKit.Orange, TextAnchor.UpperCenter);
        UIKit.Anchor(repairTitleText.rectTransform, TL, TL, new Vector2(30f, -28f), new Vector2(1000f, 46f));

        repairStatusText = UIKit.Label("RepStatus", rr, "CLICK A LEFT TERMINAL, THEN CLICK THE MATCHING COLOR ON THE RIGHT", 16, UIKit.Cyan, TextAnchor.UpperCenter);
        UIKit.Anchor(repairStatusText.rectTransform, TL, TL, new Vector2(30f, -80f), new Vector2(1000f, 32f));

        var board = UIKit.Img("WireBoard", rr, PixelArt.ToSprite(PixelArt.Panel(92, 42, 19)), new Color(0.75f, 0.8f, 0.9f, 1f));
        UIKit.Anchor(board.rectTransform, TL, TL, new Vector2(70f, -125f), new Vector2(920f, 410f));

        var whiteSp = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));

        for (int i = 0; i < 4; i++)
        {
            var linkImg = UIKit.Img("WireLink_" + i, board.transform, whiteSp, WireColors[i]);
            var lr = linkImg.rectTransform;
            lr.anchorMin = new Vector2(0.5f, 0.5f);
            lr.anchorMax = new Vector2(0.5f, 0.5f);
            lr.pivot = new Vector2(0.5f, 0.5f);
            lr.sizeDelta = new Vector2(520f, 16f);
            linkImg.gameObject.SetActive(false);
            wireLinks[i] = lr;
            wireLinkImgs[i] = linkImg;
        }

        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            float yOff = -32f - i * 92f;

            leftWireBtns[i] = UIKit.Button("LeftTerm_" + i, board.transform, WireNames[i], 18, TL, TL, new Vector2(26f, yOff), new Vector2(210f, 68f), () =>
            {
                OnLeftTerminalClicked(idx);
            });
            leftWireImgs[i] = leftWireBtns[i].GetComponent<Image>();
            leftWireTexts[i] = leftWireBtns[i].GetComponentInChildren<Text>(true);

            var lSwatch = UIKit.Img("LeftSwatch_" + i, leftWireBtns[i].transform, whiteSp, WireColors[i]);
            UIKit.Anchor(lSwatch.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(22f, 44f));
            lSwatch.raycastTarget = false;
            leftWireSwatches[i] = lSwatch;

            rightWireBtns[i] = UIKit.Button("RightTerm_" + i, board.transform, WireNames[i], 18, TL, TL, new Vector2(684f, yOff), new Vector2(210f, 68f), () =>
            {
                OnRightTerminalClicked(idx);
            });
            rightWireImgs[i] = rightWireBtns[i].GetComponent<Image>();
            rightWireTexts[i] = rightWireBtns[i].GetComponentInChildren<Text>(true);

            var rSwatch = UIKit.Img("RightSwatch_" + i, rightWireBtns[i].transform, whiteSp, WireColors[i]);
            UIKit.Anchor(rSwatch.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(22f, 44f));
            rSwatch.raycastTarget = false;
            rightWireSwatches[i] = rSwatch;
        }

        UIKit.Button("CancelRepair", rr, "ABORT REPAIR [ESC]", 20, BC, BC, new Vector2(0f, 34f), new Vector2(360f, 66f), () =>
        {
            CloseRepairPopup();
        });

        repairPopup.SetActive(false);
    }

    // ---------------- Update Loop ----------------
    void Update()
    {
        if (game.IsTransitioning || gameEnded) return;

        var k = Keyboard.current;
        if (k != null && k.escapeKey.wasPressedThisFrame)
        {
            if (repairPopupOpen) { CloseRepairPopup(); return; }
            if (clearPopupOpen) { ToggleClearPopup(false); return; }
            if (consoleOpen) { ToggleConsole(false); return; }
        }

        if (repairPopupOpen)
        {
            ghostSr.enabled = false;
            if (repairCloseDelay > 0f)
            {
                repairCloseDelay -= Time.unscaledDeltaTime;
                if (repairCloseDelay <= 0f)
                {
                    SetCellDamaged(repairTargetCell.x, repairTargetCell.y, false);
                    RecomputeBaseState(false);
                    CloseRepairPopup();
                }
            }
            return;
        }

        if (game.dialogue.IsOpen || consoleOpen || clearPopupOpen)
        {
            ghostSr.enabled = false;
            lastDragBtn = 0;
            return;
        }

        if (k != null)
        {
            if (!survivalStarted && k.fKey.wasPressedThisFrame) ToggleDeleteMode();
            if (survivalStarted && k.tabKey.wasPressedThisFrame) ToggleTimeScale();
            if (survivalStarted && k.cKey.wasPressedThisFrame) UseSelectedSurvivalSlot();
        }

        HandleHotbarInput(k);
        HandleGoatMovement(k);
        UpdateMachineVisuals();
        UpdateFloatingNotes();

        if (roverBeaconSr != null)
        {
            roverBeaconSr.enabled = !drivingRover;
            if (!drivingRover)
            {
                float pulse = 0.55f + 0.45f * Mathf.Sin(Time.time * 5f);
                roverBeaconSr.color = new Color(1f, 0.85f, 0.2f, pulse);
            }
        }

        if (survivalStarted)
        {
            UpdateSurvivalSimulation();
            UpdateFallingMeteors();
            UpdateSandstormOverlay();
            HandleSurvivalInteractions(k);
        }
        else
        {
            HandleBuildingAndInteract(k);
        }

        SnapCamera();
    }

    void UpdateMachineVisuals()
    {
        float pulse = 0.78f + 0.22f * Mathf.Sin(Time.time * 4.5f);
        for (int y = BuildY; y < BuildY + BuildH; y++)
        {
            for (int x = BuildX; x < BuildX + BuildW; x++)
            {
                var fx = machineEffects[x, y];
                if (fx != null)
                {
                    fx.transform.localScale = new Vector3(pulse, pulse, 1f);
                    fx.transform.Rotate(0f, 0f, 25f * Time.deltaTime);
                }
            }
        }

        if (goatFlavorGlowTimer > 0f)
        {
            goatFlavorGlowTimer -= Time.deltaTime;
            goatSr.color = goatFlavorGlowTimer > 0f
                ? Color.Lerp(Color.white, new Color(1f, 0.88f, 0.35f, 1f), Mathf.PingPong(Time.time * 10f, 1f))
                : Color.white;
        }
    }

    void UpdateFloatingNotes()
    {
        for (int i = floatingNotes.Count - 1; i >= 0; i--)
        {
            var fn = floatingNotes[i];
            fn.elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(fn.elapsed / fn.duration);
            fn.tf.position += new Vector3(0f, 1.35f * Time.deltaTime, 0f);
            Color c = fn.tm.color;
            c.a = 1f - t;
            fn.tm.color = c;

            if (t >= 1f)
            {
                Destroy(fn.tf.gameObject);
                floatingNotes.RemoveAt(i);
            }
        }
    }

    void SpawnWorldPopup(Vector2 worldPos, string label, Color color)
    {
        var txtGo = new GameObject("WorldFloatText");
        txtGo.transform.SetParent(worldRoot.transform, false);
        txtGo.transform.position = new Vector3(worldPos.x, worldPos.y + 0.75f, 0f);
        var tm = txtGo.AddComponent<TextMesh>();
        tm.text = label;
        tm.fontSize = 32;
        tm.characterSize = 0.085f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.color = color;
        var mr = txtGo.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 10;

        floatingNotes.Add(new FloatingNote
        {
            tf = txtGo.transform,
            tm = tm,
            elapsed = 0f,
            duration = 1.4f
        });
    }

    void SpawnTastifierBurst(Vector2 worldPos, string label)
    {
        goatFlavorGlowTimer = 1.35f;

        for (int i = 0; i < 6; i++)
        {
            float ang = i * (Mathf.PI * 2f / 6f);
            var spGo = new GameObject("FlavorSparkle");
            spGo.transform.SetParent(worldRoot.transform, false);
            spGo.transform.position = new Vector3(worldPos.x + Mathf.Cos(ang) * 0.55f, worldPos.y + Mathf.Sin(ang) * 0.55f, 0f);
            var sr = spGo.AddComponent<SpriteRenderer>();
            sr.sprite = WorldArt.FlavorSparkle();
            sr.sortingOrder = 9;
            Destroy(spGo, 1.1f);
        }

        SpawnWorldPopup(worldPos, label, new Color(1f, 0.88f, 0.25f, 1f));
    }

    bool IsFoodTastifierActive()
    {
        if (GameData.Count(ItemId.Tastifier) > 0) return true;
        for (int y = BuildY; y < BuildY + BuildH; y++)
            for (int x = BuildX; x < BuildX + BuildW; x++)
                if (grid[x, y] == (int)ItemId.Tastifier)
                    return true;
        return false;
    }

    void ToggleDeleteMode()
    {
        deleteMode = !deleteMode;
        SetBtnText(modeToggleBtn, deleteMode ? "MODE: DELETE [F]" : "MODE: PLACE [F]");
        SoundFX.Play(deleteMode ? SoundFX.Back : SoundFX.Move, 0.45f);
        RefreshPlacementIndicators();
    }

    void ToggleTimeScale()
    {
        timeScaleMult = timeScaleMult > 1.5f ? 1f : 3f;
        SetBtnText(speedBtn, "SPEED: " + (int)timeScaleMult + "X [TAB]");
        SoundFX.Play(SoundFX.Blip, 0.45f);
    }

    static void SetBtnText(UIButton btn, string label)
    {
        if (btn == null) return;
        var t = btn.GetComponentInChildren<Text>(true);
        if (t != null) t.text = label;
    }

    void StepPalette(int delta)
    {
        if (survivalStarted)
        {
            if (survivalSlots.Count == 0) return;
            int next = (survSlotIndex + delta + survivalSlots.Count) % survivalSlots.Count;
            SelectSurvivalSlot(next);
            return;
        }

        if (buildPalette.Count == 0) return;
        int nextBuild = (paletteIndex + delta + buildPalette.Count) % buildPalette.Count;
        SelectPalette(nextBuild);
    }

    void HandleHotbarInput(Keyboard k)
    {
        if (k != null)
        {
            int digit = -1;
            if (k.digit1Key.wasPressedThisFrame) digit = 0;
            else if (k.digit2Key.wasPressedThisFrame) digit = 1;
            else if (k.digit3Key.wasPressedThisFrame) digit = 2;
            else if (k.digit4Key.wasPressedThisFrame) digit = 3;
            else if (k.digit5Key.wasPressedThisFrame) digit = 4;
            else if (k.digit6Key.wasPressedThisFrame) digit = 5;
            else if (k.digit7Key.wasPressedThisFrame) digit = 6;
            else if (k.digit8Key.wasPressedThisFrame) digit = 7;
            else if (k.digit9Key.wasPressedThisFrame) digit = 8;

            if (digit >= 0)
            {
                if (survivalStarted) SelectSurvivalSlot(survHotbarOffset + digit);
                else SelectPalette(hotbarOffset + digit);
            }

            if (k.zKey.wasPressedThisFrame || k.leftBracketKey.wasPressedThisFrame) StepPalette(-1);
            if (k.xKey.wasPressedThisFrame || k.rightBracketKey.wasPressedThisFrame) StepPalette(1);
        }

        if (scrollCooldown > 0f)
            scrollCooldown -= Time.unscaledDeltaTime;

        var m = Mouse.current;
        if (m != null && scrollCooldown <= 0f)
        {
            float w = m.scroll.ReadValue().y;
            if (w > 2f)
            {
                scrollCooldown = 0.16f;
                StepPalette(-1);
            }
            else if (w < -2f)
            {
                scrollCooldown = 0.16f;
                StepPalette(1);
            }
        }
    }

    void HandleGoatMovement(Keyboard k)
    {
        Vector2 move = Vector2.zero;
        if (k != null)
        {
            if (k.wKey.isPressed || k.upArrowKey.isPressed) move.y += 1f;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) move.y -= 1f;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) move.x -= 1f;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) move.x += 1f;
        }

        if (move.sqrMagnitude > 0.001f)
        {
            move.Normalize();
            if (Mathf.Abs(move.x) > Mathf.Abs(move.y)) goatDir = move.x < 0f ? 2 : 3;
            else goatDir = move.y > 0f ? 1 : 0;

            float speed = drivingRover ? 13.0f : 5.5f;
            Vector2 delta = move * (speed * Time.deltaTime);

            Vector2 tryX = new Vector2(goatPos.x + delta.x, goatPos.y);
            if (CanStandAt(tryX)) goatPos.x = tryX.x;

            Vector2 tryY = new Vector2(goatPos.x, goatPos.y + delta.y);
            if (CanStandAt(tryY)) goatPos.y = tryY.y;

            goatPos.x = Mathf.Clamp(goatPos.x, MountainBorder + 0.5f, MapW - MountainBorder - 0.5f);
            goatPos.y = Mathf.Clamp(goatPos.y, MountainBorder + 0.5f, MapH - MountainBorder - 0.5f);

            if (drivingRover && roverTf != null)
            {
                roverPos = goatPos;
                roverDir = goatDir;
                roverTf.position = new Vector3(roverPos.x, roverPos.y, 0f);
                roverSr.sprite = WorldArt.RoverSprite(roverDir, true);
                goatTf.position = new Vector3(goatPos.x, goatPos.y, 0f);
            }
            else
            {
                goatTf.position = new Vector3(goatPos.x, goatPos.y, 0f);
                walkTimer += Time.deltaTime * 8f;
                goatSr.sprite = WorldArt.Goat(goatDir, ((int)walkTimer) % 2);
            }
        }
        else if (!drivingRover)
        {
            walkTimer = 0f;
            goatSr.sprite = WorldArt.Goat(goatDir, 0);
        }

        int gx = Mathf.FloorToInt(goatPos.x);
        int gy = Mathf.FloorToInt(goatPos.y);
        bool onDoor = gx >= 0 && gx < MapW && gy >= 0 && gy < MapH && grid[gx, gy] == (int)ItemId.Door;
        if (onDoor && !wasOnDoor)
            SoundFX.Play(SoundFX.Airlock, 0.65f);
        wasOnDoor = onDoor;
    }

    bool CanStandAt(Vector2 p)
    {
        float r = drivingRover ? 0.52f : 0.28f;
        return !IsSolidCell(Mathf.FloorToInt(p.x - r), Mathf.FloorToInt(p.y - r)) &&
               !IsSolidCell(Mathf.FloorToInt(p.x + r), Mathf.FloorToInt(p.y - r)) &&
               !IsSolidCell(Mathf.FloorToInt(p.x - r), Mathf.FloorToInt(p.y + r)) &&
               !IsSolidCell(Mathf.FloorToInt(p.x + r), Mathf.FloorToInt(p.y + r));
    }

    bool IsSolidCell(int x, int y)
    {
        if (x < 0 || x >= MapW || y < 0 || y >= MapH) return true;
        if (terrainSolid[x, y]) return true;
        int id = grid[x, y];
        if (id == -1) return false;
        if (id == SecondaryCell) return true;
        if ((ItemId)id == ItemId.Door)
        {
            if (dustStormActive || drivingRover) return true;
            return false;
        }
        return true;
    }

    // ---------------- Build Phase Input ----------------
    void HandleBuildingAndInteract(Keyboard k)
    {
        bool nearConsole = IsNearItem(ItemId.Console, 1.8f);
        bool nearPod = Vector2.Distance(goatPos, landingPodPos) <= 3.6f;

        if (k != null && k.eKey.wasPressedThisFrame && nearConsole)
        {
            ToggleConsole(true);
            return;
        }

        var m = Mouse.current;
        var cam = Camera.main;
        if (m == null || cam == null) return;

        bool lmbPressed = m.leftButton.wasPressedThisFrame;
        bool lmbHeld = m.leftButton.isPressed;
        bool rmbHeld = m.rightButton.isPressed;

        if (!lmbHeld && !rmbHeld)
        {
            blockPlacementUntilMouseUp = false;
            lastDragCell = new Vector2Int(-1, -1);
            lastDragBtn = 0;
        }

        if (blockPlacementUntilMouseUp)
        {
            ghostSr.enabled = false;
            return;
        }

        Vector2 screenPos = m.position.ReadValue();
        if (screenPos.y < 140f || (screenPos.y > Screen.height - 90f && screenPos.x > Screen.width - 660f))
        {
            ghostSr.enabled = false;
            cursorText.color = UIKit.Yellow;
            cursorText.text = nearConsole
                ? "PRESS [E] TO OPEN COMMAND CONSOLE"
                : (nearPod ? "G.O.A.T.-1 MARS LANDER — TOUCHDOWN CAPSULE" : "");
            return;
        }

        Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
        int gx = Mathf.FloorToInt(wp.x);
        int gy = Mathf.FloorToInt(wp.y);

        bool inAnyGrid = InMainBuildZone(gx, gy) || InSolarZone(gx, gy);
        if (!inAnyGrid || buildPalette.Count == 0)
        {
            ghostSr.enabled = false;
            cursorText.color = UIKit.Yellow;
            if (nearConsole)
                cursorText.text = "PRESS [E] TO OPEN COMMAND CONSOLE";
            else if (nearPod)
                cursorText.text = "G.O.A.T.-1 MARS LANDER — TOUCHDOWN CAPSULE";
            else if (deleteMode)
                cursorText.text = "DELETE MODE [F]: CLICK ANY PLACED ITEM TO REMOVE";
            else if (buildPalette.Count > 0 && buildPalette[paletteIndex] == ItemId.Solar)
                cursorText.text = "SOLAR PANELS GO OUTSIDE IN THE AMBER SOLAR ZONE";
            else if (buildPalette.Count > 0 && IsWallMountedAppliance(buildPalette[paletteIndex]))
                cursorText.text = GameData.Get(buildPalette[paletteIndex]).name.ToUpperInvariant() + " MUST BE PLACED AGAINST A METAL WALL (NOT IN THE MIDDLE)";
            else
                cursorText.text = "MOVE CURSOR INSIDE THE BLUE SURVEYOR GRID TO BUILD";
            return;
        }

        bool wantDelete = deleteMode || rmbHeld;
        Vector2Int cell = new Vector2Int(gx, gy);

        if (wantDelete)
        {
            bool hasItem = grid[gx, gy] != -1;
            ghostSr.enabled = true;
            ghostSr.sprite = WorldArt.DeleteGhostTile();
            ghostSr.transform.position = new Vector3(gx + 0.5f, gy + 0.5f, 0f);
            ghostSr.color = hasItem ? new Color(1f, 0.35f, 0.35f, 0.95f) : new Color(1f, 0.55f, 0.55f, 0.45f);
            cursorText.color = UIKit.Yellow;

            string delHint = hasItem
                ? "DELETE MODE [F]: CLICK OR DRAG TO PICK UP ITEM"
                : "DELETE MODE [F]: EMPTY TILE (PRESS [F] FOR PLACE MODE)";
            cursorText.text = nearConsole ? ("PRESS [E] FOR COMMAND CONSOLE  |  " + delHint) : delHint;

            if ((lmbHeld || rmbHeld) && (cell != lastDragCell || lastDragBtn != 2) && hasItem)
            {
                lastDragCell = cell;
                lastDragBtn = 2;
                RemoveItem(gx, gy);
            }
            return;
        }

        ItemId selectedId = buildPalette[paletteIndex];
        string reason;
        bool valid = CanPlaceAt(selectedId, gx, gy, out reason);

        ghostSr.enabled = true;
        if (selectedId == ItemId.Solar)
        {
            ghostSr.sprite = WorldArt.SolarGhostTile();
            ghostSr.transform.position = new Vector3(gx + 1.0f, gy + 0.5f, 0f);
        }
        else
        {
            ghostSr.sprite = WorldArt.GhostTile();
            ghostSr.transform.position = new Vector3(gx + 0.5f, gy + 0.5f, 0f);
        }
        ghostSr.color = valid ? new Color(0.35f, 1f, 0.5f, 0.9f) : new Color(1f, 0.32f, 0.32f, 0.9f);
        cursorText.color = valid ? UIKit.Yellow : UIKit.Red;

        cursorText.text = nearConsole ? ("PRESS [E] FOR COMMAND CONSOLE  |  " + reason) : reason;

        if (lmbHeld && (cell != lastDragCell || lastDragBtn != 1) && valid)
        {
            lastDragCell = cell;
            lastDragBtn = 1;
            PlaceItem(selectedId, gx, gy);
        }
        else if (lmbPressed && !valid)
        {
            SoundFX.Play(SoundFX.Back, 0.55f);
            if (IsWallMountedAppliance(selectedId) && InMainBuildZone(gx, gy) && grid[gx, gy] == -1 && !TouchesWall(gx, gy))
                SpawnWorldPopup(new Vector2(gx + 0.5f, gy + 0.5f), "MUST PLACE NEXT TO A METAL WALL!", new Color(1f, 0.35f, 0.35f, 1f));
        }
    }

    // ---------------- Survival Phase Initialization & Hotbar ----------------
    void StartSurvivalAndExploration()
    {
        survivalStarted = true;
        deleteMode = false;
        ghostSr.enabled = false;
        modeToggleBtn.gameObject.SetActive(false);
        clearBaseBtn.gameObject.SetActive(false);
        speedBtn.gameObject.SetActive(true);
        consumeBtn.gameObject.SetActive(true);

        objectiveText.gameObject.SetActive(false);
        vitalsBarRoot.SetActive(true);
        radarText.gameObject.SetActive(true);

        survivalSlots.Clear();
        if (HasGoatGlue())
            AddOrIncrementSurvivalSlot("glue", "GOAT-GLUE 4000", 1, true);
        if (GameData.Count(ItemId.Shield) > 0)
            AddOrIncrementSurvivalSlot("shield", "RAD SHIELD", 1, true);

        int carryGrass = Mathf.Clamp(unplaced[(int)ItemId.Grass], 0, MaxStartingGrass);
        int carryWater = Mathf.Clamp(unplaced[(int)ItemId.Water], 0, MaxStartingWater);
        if (carryGrass > 0)
            AddOrIncrementSurvivalSlot("grass", "GRASS BALE", carryGrass, false);
        if (carryWater > 0)
            AddOrIncrementSurvivalSlot("water", "WATER MODULE", carryWater, false);

        survSlotIndex = 0;
        survHotbarOffset = 0;
        RefreshHotbar();

        RecomputeBaseState(false);

        // Seed 3 initial meteorite crash sites across Mars so scavenging is immediately worthwhile
        SpawnLandedMeteorite(new Vector2(BuildX + 34f, BuildY + 26f));
        SpawnLandedMeteorite(new Vector2(BuildX - 28f, BuildY - 24f));
        SpawnLandedMeteorite(new Vector2(BuildX + 26f, BuildY - 32f));
    }

    void AddOrIncrementSurvivalSlot(string id, string displayName, int amount, bool isEquip)
    {
        for (int i = 0; i < survivalSlots.Count; i++)
        {
            if (survivalSlots[i].id == id)
            {
                survivalSlots[i].count += amount;
                RefreshHotbar();
                return;
            }
        }
        survivalSlots.Add(new SurvivalSlot
        {
            id = id,
            name = displayName,
            count = amount,
            isEquipment = isEquip
        });
        RefreshHotbar();
    }

    bool IsRadiationShieldEquipped()
    {
        if (!survivalStarted || survivalSlots.Count == 0) return false;
        var s = survivalSlots[Mathf.Clamp(survSlotIndex, 0, survivalSlots.Count - 1)];
        return s.id == "shield" && s.count > 0;
    }

    bool IsGoatGlueEquipped()
    {
        if (!survivalStarted || survivalSlots.Count == 0) return false;
        var s = survivalSlots[Mathf.Clamp(survSlotIndex, 0, survivalSlots.Count - 1)];
        return s.id == "glue" && s.count > 0;
    }

    bool HasGoatGlue()
    {
        return GameData.Count(ItemId.Glue) > 0;
    }

    void SelectSurvivalSlot(int idx)
    {
        if (survivalSlots.Count == 0) return;
        survSlotIndex = Mathf.Clamp(idx, 0, survivalSlots.Count - 1);
        if (survSlotIndex < survHotbarOffset) survHotbarOffset = survSlotIndex;
        if (survSlotIndex >= survHotbarOffset + HotbarSlots) survHotbarOffset = survSlotIndex - HotbarSlots + 1;
        SoundFX.Play(SoundFX.Move, 0.35f);
        RefreshHotbar();
    }

    void UseSelectedSurvivalSlot()
    {
        if (!survivalStarted || survivalSlots.Count == 0) return;
        var s = survivalSlots[survSlotIndex];

        if (s.isEquipment)
        {
            SoundFX.Play(SoundFX.Blip, 0.45f);
            RefreshHotbar();
            return;
        }

        if (s.count <= 0) return;

        bool tastifierOn = IsFoodTastifierActive();

        if (s.id == "grass")
        {
            float gain = tastifierOn ? 34f : 22f;
            hunger = Mathf.Clamp(hunger + gain, 0f, 100f);
            health = Mathf.Clamp(health + (tastifierOn ? 14f : 8f), 0f, 100f);
            SoundFX.Play(SoundFX.Eat, 0.65f);
            if (tastifierOn) SpawnTastifierBurst(goatPos, "TASTIFIER BOOST! +34% FOOD");
        }
        else if (s.id == "water")
        {
            thirst = Mathf.Clamp(thirst + 32f, 0f, 100f);
            health = Mathf.Clamp(health + 8f, 0f, 100f);
            SoundFX.Play(SoundFX.Drink, 0.65f);
        }
        else if (s.id == "ration")
        {
            float gain = tastifierOn ? 62f : 42f;
            hunger = Mathf.Clamp(hunger + gain, 0f, 100f);
            health = Mathf.Clamp(health + (tastifierOn ? 20f : 14f), 0f, 100f);
            SoundFX.Play(SoundFX.Eat, 0.65f);
            if (tastifierOn) SpawnTastifierBurst(goatPos, "TASTIFIER BOOST! +62% FOOD");
        }
        else if (s.id == "ice")
        {
            thirst = Mathf.Clamp(thirst + 46f, 0f, 100f);
            health = Mathf.Clamp(health + 12f, 0f, 100f);
            SoundFX.Play(SoundFX.Drink, 0.65f);
        }
        else if (s.id == "o2cell")
        {
            oxygen = Mathf.Clamp(oxygen + 40f, 0f, 100f);
            SoundFX.Play(SoundFX.Repair, 0.65f);
        }

        s.count--;
        if (s.count <= 0)
        {
            survivalSlots.RemoveAt(survSlotIndex);
            survSlotIndex = Mathf.Clamp(survSlotIndex, 0, Mathf.Max(0, survivalSlots.Count - 1));
            if (survHotbarOffset > survSlotIndex) survHotbarOffset = survSlotIndex;
        }
        RefreshHotbar();
    }

    // ---------------- Survival, Disasters, Radiation & Meteorites ----------------
    void UpdateSurvivalSimulation()
    {
        float dt = Time.deltaTime * timeScaleMult;
        survivalTimeLeft = Mathf.Max(0f, survivalTimeLeft - dt);

        if (survivalTimeLeft <= 0f)
        {
            TriggerEndScreen(
                true,
                "SURVIVED 30 MARTIAN MINUTES!",
                "MISSION REPORT:\n" +
                "  • You endured Red Dust Storms, Marsquakes, and lethal surface radiation.\n" +
                "  • Your habitat engineering and meteorite scavenging kept the first Martian goat alive!"
            );
            return;
        }

        if (dustStormActive || quakeActive)
        {
            disasterRemaining -= dt;
            if (disasterRemaining <= 0f)
            {
                dustStormActive = false;
                quakeActive = false;
                disastersSurvived++;
                nextDisasterTimer = 78f; // Lower disaster frequency (~78s calm window between events)
                SoundFX.Play(SoundFX.Confirm, 0.5f);
            }
        }
        else
        {
            nextDisasterTimer -= dt;
            if (nextDisasterTimer <= 0f)
            {
                TriggerRandomDisaster();
                nextDisasterTimer = 78f;
            }
        }

        int gx = Mathf.FloorToInt(goatPos.x);
        int gy = Mathf.FloorToInt(goatPos.y);

        // Physically inside the habitat structure (sheltered from storms/quakes even if a wall cracked)
        bool goatIndoors = !drivingRover &&
            gx >= 0 && gx < MapW && gy >= 0 && gy < MapH && indoorTile[gx, gy];

        // Inside AND air-tight sealed (all walls intact so Air Purifier pressurizes O2 & blocks radiation)
        bool goatInSealedBase = goatIndoors && baseSealed &&
            gx >= 0 && gx < MapW && gy >= 0 && gy < MapH && (enclosed[gx, gy] || grid[gx, gy] == (int)ItemId.Door);

        int intactSolar = 0;
        int totalSolar = 0;
        int batteries = 0;
        int purifiers = 0;

        for (int y = BuildY; y < BuildY + BuildH; y++)
        {
            for (int x = SolarX; x < BuildX + BuildW; x++)
            {
                int v = grid[x, y];
                if (v == (int)ItemId.Solar)
                {
                    totalSolar++;
                    if (!cellDamaged[x, y]) intactSolar++;
                }
                else if (v == (int)ItemId.Battery) batteries++;
                else if (v == (int)ItemId.Purifier && enclosed[x, y]) purifiers++;
            }
        }

        float powerGen = intactSolar * 0.45f;
        float powerDrain = 0.85f + (drivingRover ? 0.35f : 0f);
        float capScale = Mathf.Max(1f, batteries * 0.5f);
        power = Mathf.Clamp(power + ((powerGen - powerDrain) / capScale) * dt, 0f, 100f);

        if (goatInSealedBase && power > 5f && purifiers > 0)
            oxygen = Mathf.Clamp(oxygen + 1.9f * dt, 0f, 100f);
        else
            oxygen = Mathf.Clamp(oxygen - (drivingRover ? 0.45f : (goatIndoors ? 0.55f : 0.95f)) * dt, 0f, 100f);

        // Tuned so max 5 Water + 5 Grass sustain ~10-12 minutes, requiring meteorite runs for the full 30m
        hunger = Mathf.Clamp(hunger - 0.36f * dt, 0f, 100f);
        thirst = Mathf.Clamp(thirst - 0.42f * dt, 0f, 100f);

        if (goatInSealedBase)
        {
            radiation = Mathf.Clamp(radiation - 1.2f * dt, 0f, 100f);
        }
        else if (goatIndoors)
        {
            // Cracked wall hull breach: minor radiation leak until repaired with Goat-Glue
            radiation = Mathf.Clamp(radiation + 0.35f * dt, 0f, 100f);
        }
        else
        {
            bool shieldEquipped = IsRadiationShieldEquipped();
            float radRate;
            if (drivingRover)
                radRate = shieldEquipped ? 0.15f : 0.55f;
            else
                radRate = shieldEquipped ? 0.35f : 3.20f;

            radiation = Mathf.Clamp(radiation + radRate * dt, 0f, 100f);
        }

        if ((dustStormActive || quakeActive) && !goatIndoors)
        {
            health = Mathf.Clamp(health - 6.5f * dt, 0f, 100f);
        }
        else if (oxygen <= 0f || radiation >= 100f || hunger <= 0f || thirst <= 0f)
        {
            health = Mathf.Clamp(health - 8.5f * dt, 0f, 100f);
        }
        else if (goatInSealedBase && oxygen > 25f)
        {
            health = Mathf.Clamp(health + 1.2f * dt, 0f, 100f);
        }

        UpdateSurvivalHUD(intactSolar, totalSolar, goatIndoors, goatInSealedBase);

        if (health <= 0f)
            TriggerContextualDeathScreen(goatIndoors);
    }

    void TriggerContextualDeathScreen(bool goatIndoors)
    {
        if (thirst <= 0f)
        {
            TriggerEndScreen(
                false,
                "CAUSE OF DEATH: SEVERE DEHYDRATION (H2O HIT 0%)",
                "HOW TO SOLVE IT NEXT RUN:\n" +
                "  • Starting cargo is capped at 5 Water Modules—drive the Mars Rover to smoking meteorite crash sites [E] to harvest Cryo Ice Flasks!\n" +
                "  • Select Cryo Ice Flasks in your Survival Hotbar and press [C] to drink them."
            );
        }
        else if (hunger <= 0f)
        {
            TriggerEndScreen(
                false,
                "CAUSE OF DEATH: STARVATION (FOOD HIT 0%)",
                "HOW TO SOLVE IT NEXT RUN:\n" +
                "  • Starting cargo is capped at 5 Grass Bales—pack the Food Tastifier (+60% boost) and hunt down fallen meteorites [E] with your Mars Rover!\n" +
                "  • Select harvested Meteor Rations in your Survival Hotbar and press [C] to eat them."
            );
        }
        else if (oxygen <= 0f)
        {
            TriggerEndScreen(
                false,
                "CAUSE OF DEATH: ASPHYXIATION (OXYGEN HIT 0%)",
                "HOW TO SOLVE IT NEXT RUN:\n" +
                "  • Stay inside your sealed base with a powered Air Purifier to replenish Oxygen.\n" +
                "  • When Dust Storms break Solar Panels or Marsquakes crack walls, hold Super Goat-Glue 4000 in your hotbar and press [R] to repair them!"
            );
        }
        else if (radiation >= 100f)
        {
            TriggerEndScreen(
                false,
                "CAUSE OF DEATH: ACUTE RADIATION OVERDOSE (RAD HIT 100%)",
                "HOW TO SOLVE IT NEXT RUN:\n" +
                "  • Radiation climbs rapidly when walking outside on foot! Select the Radiation Shield in your Survival Hotbar before stepping outside.\n" +
                "  • Drive the Mars Rover for hull protection, or return inside your sealed base to flush radiation back down."
            );
        }
        else if ((dustStormActive || quakeActive) && !goatIndoors)
        {
            TriggerEndScreen(
                false,
                "CAUSE OF DEATH: CAUGHT OUTSIDE IN A MARTIAN DISASTER",
                "HOW TO SOLVE IT NEXT RUN:\n" +
                "  • When a Red Dust Storm or Marsquake begins, sprint back inside your base immediately—staying outside drains massive HP!\n" +
                "  • Once the disaster ends, hold Super Goat-Glue 4000 in your hotbar and press [R] to weld broken solar panels and walls."
            );
        }
        else
        {
            TriggerEndScreen(
                false,
                "CAUSE OF DEATH: SUIT INTEGRITY FAILURE (HP HIT 0%)",
                "HOW TO SOLVE IT NEXT RUN:\n" +
                "  • Keep your base sealed and powered, take shelter indoors during Red Dust Storms and Marsquakes, and scavenge meteorites with the Rover!"
            );
        }
    }

    void TriggerRandomDisaster()
    {
        // Prevent repeating the exact same disaster twice in a row so Red Dust Storms always occur reliably
        bool triggerDustStorm;
        if (disastersTriggered == 0)
            triggerDustStorm = Random.value < 0.5f;
        else
            triggerDustStorm = !lastDisasterWasDustStorm;

        disastersTriggered++;
        lastDisasterWasDustStorm = triggerDustStorm;

        if (triggerDustStorm)
        {
            dustStormActive = true;
            quakeActive = false;
            disasterRemaining = 27f; // +5s longer (was 22s)

            // Break outdoor solar panels so power drops until repaired with Goat-Glue 4000
            for (int y = BuildY; y < BuildY + BuildH; y++)
                for (int x = SolarX; x < BuildX + BuildW; x++)
                    if (grid[x, y] == (int)ItemId.Solar)
                        SetCellDamaged(x, y, true);

            SoundFX.Play(SoundFX.Storm, 0.75f);
        }
        else
        {
            quakeActive = true;
            dustStormActive = false;
            disasterRemaining = 21f; // +5s longer (was 16s)
            cameraShakeTimer = 3.5f;

            SpawnQuakeGroundCracks(12);

            var walls = new List<Vector2Int>();
            for (int y = BuildY; y < BuildY + BuildH; y++)
                for (int x = BuildX; x < BuildX + BuildW; x++)
                    if (grid[x, y] == (int)ItemId.Wall && !cellDamaged[x, y])
                        walls.Add(new Vector2Int(x, y));

            int wallsToBreak = Mathf.Min(walls.Count, Random.Range(2, 5));
            for (int i = 0; i < wallsToBreak; i++)
            {
                int pick = Random.Range(0, walls.Count);
                var w = walls[pick];
                walls.RemoveAt(pick);
                SetCellDamaged(w.x, w.y, true);
            }

            RecomputeBaseState(false);
            SoundFX.Play(SoundFX.Alarm, 0.75f);
        }
    }

    void SpawnQuakeGroundCracks(int count)
    {
        var rnd = new System.Random(System.Environment.TickCount + quakeCracks.Count * 31);
        for (int i = 0; i < count; i++)
        {
            int cx = rnd.Next(MountainBorder + 3, MapW - MountainBorder - 3);
            int cy = rnd.Next(MountainBorder + 3, MapH - MountainBorder - 3);
            if (InMainBuildZone(cx, cy) || InSolarZone(cx, cy)) continue;

            var cGo = new GameObject("QuakeCrack");
            cGo.transform.SetParent(worldRoot.transform, false);
            cGo.transform.position = new Vector3(cx + 0.5f, cy + 0.5f, 0f);
            var sr = cGo.AddComponent<SpriteRenderer>();
            sr.sprite = WorldArt.GroundCrack(i % 2);
            sr.sortingOrder = 1;
            quakeCracks.Add(cGo);
        }
    }

    void UpdateSandstormOverlay()
    {
        if (stormTintImg == null) return;

        if (!dustStormActive)
        {
            stormTintImg.color = new Color(0.82f, 0.14f, 0.08f, 0f);
            for (int i = 0; i < dustStreaks.Length; i++)
                dustStreaks[i].gameObject.SetActive(false);
            return;
        }

        int gx = Mathf.FloorToInt(goatPos.x);
        int gy = Mathf.FloorToInt(goatPos.y);
        bool indoors = !drivingRover && gx >= 0 && gx < MapW && gy >= 0 && gy < MapH && indoorTile[gx, gy];

        float pulse = Mathf.Sin(Time.unscaledTime * 5f) * 0.04f;
        float tintAlpha = (indoors ? 0.14f : 0.38f) + pulse;
        stormTintImg.color = new Color(0.84f, 0.15f, 0.09f, Mathf.Clamp01(tintAlpha));

        float dt = Time.unscaledDeltaTime * timeScaleMult;
        float streakAlpha = indoors ? 0.35f : 0.88f;
        for (int i = 0; i < dustStreaks.Length; i++)
        {
            var sr = dustStreaks[i];
            sr.gameObject.SetActive(true);
            var img = sr.GetComponent<Image>();
            if (img != null) img.color = new Color(1f, 0.42f, 0.28f, streakAlpha);

            Vector2 pos = sr.anchoredPosition + dustSpeeds[i] * dt;
            if (pos.x < -1120f) pos.x += 2240f;
            if (pos.y < -660f) pos.y += 1320f;
            sr.anchoredPosition = pos;
        }
    }

    // ---------------- Rare Unannounced Falling Meteorites ----------------
    void UpdateFallingMeteors()
    {
        float dt = Time.deltaTime * timeScaleMult;

        nextMeteorTimer -= dt;
        if (nextMeteorTimer <= 0f)
        {
            nextMeteorTimer = Random.Range(24f, 34f);
            if (meteorSites.Count < 8)
                LaunchUnannouncedMeteor();
        }

        for (int i = fallingMeteors.Count - 1; i >= 0; i--)
        {
            var m = fallingMeteors[i];
            m.elapsed += dt;
            float t = Mathf.Clamp01(m.elapsed / m.duration);
            m.tf.position = Vector2.Lerp(m.startPos, m.targetPos, t);

            if (t >= 1f)
            {
                Destroy(m.tf.gameObject);
                fallingMeteors.RemoveAt(i);
                cameraShakeTimer = Mathf.Max(cameraShakeTimer, 0.45f);
                SoundFX.Play(SoundFX.Storm, 0.5f);
                SpawnLandedMeteorite(m.targetPos);
            }
        }
    }

    void LaunchUnannouncedMeteor()
    {
        var rnd = new System.Random(System.Environment.TickCount + meteorSites.Count * 53);
        for (int attempt = 0; attempt < 60; attempt++)
        {
            int px = rnd.Next(MountainBorder + 3, MapW - MountainBorder - 3);
            int py = rnd.Next(MountainBorder + 3, MapH - MountainBorder - 3);
            if (NearBaseClearing(px, py) || terrainSolid[px, py]) continue;

            Vector2 target = new Vector2(px + 0.5f, py + 0.5f);
            Vector2 start = target + new Vector2(18f, 24f);

            var go = new GameObject("FallingMeteor");
            go.transform.SetParent(worldRoot.transform, false);
            go.transform.position = new Vector3(start.x, start.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WorldArt.FallingMeteor();
            sr.sortingOrder = 8;

            fallingMeteors.Add(new FallingMeteor
            {
                startPos = start,
                targetPos = target,
                elapsed = 0f,
                duration = 0.75f,
                tf = go.transform
            });
            return;
        }
    }

    void SpawnLandedMeteorite(Vector2 pos)
    {
        var go = new GameObject("MeteoriteSite");
        go.transform.SetParent(worldRoot.transform, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = WorldArt.MeteoriteSite();
        sr.sortingOrder = 3;

        meteorSites.Add(new MeteorSite { pos = pos, go = go });
    }

    void HarvestMeteorite(MeteorSite site)
    {
        meteoritesHarvested++;
        SoundFX.Play(SoundFX.Scavenge, 0.7f);

        AddOrIncrementSurvivalSlot("ration", "METEOR RATION", 1, false);
        AddOrIncrementSurvivalSlot("ice", "CRYO ICE FLASK", 1, false);
        if (Random.value < 0.65f)
            AddOrIncrementSurvivalSlot("o2cell", "O2 CANISTER", 1, false);

        if (site.go != null) Destroy(site.go);
        meteorSites.Remove(site);
    }

    void SetCellDamaged(int x, int y, bool damaged)
    {
        cellDamaged[x, y] = damaged;
        cellRenderers[x, y].color = damaged ? new Color(1f, 0.42f, 0.42f, 1f) : Color.white;
        if (indicatorRenderers[x, y] != null)
        {
            indicatorRenderers[x, y].enabled = damaged;
            if (damaged) indicatorRenderers[x, y].sprite = WorldArt.DamagedOverlay();
        }
    }

    // ---------------- Survival Interactions & Among Us Repair Popup ----------------
    void HandleSurvivalInteractions(Keyboard k)
    {
        bool pressE = k != null && k.eKey.wasPressedThisFrame;
        bool pressR = k != null && k.rKey.wasPressedThisFrame;
        cursorText.color = UIKit.Yellow;

        MeteorSite nearMeteor = null;
        float bestDist = 2.1f;
        foreach (var m in meteorSites)
        {
            float d = Vector2.Distance(goatPos, m.pos);
            if (d <= bestDist)
            {
                bestDist = d;
                nearMeteor = m;
            }
        }

        if (nearMeteor != null)
        {
            cursorText.text = "PRESS [E] TO SALVAGE FALLEN METEORITE RESOURCES";
            if (pressE)
            {
                HarvestMeteorite(nearMeteor);
                return;
            }
        }

        if (roverTf != null)
        {
            if (drivingRover)
            {
                if (nearMeteor == null)
                    cursorText.text = "DRIVING ROVER   |   PRESS [E] TO EXIT ROVER   |   [C] CONSUME HOTBAR ITEM";
                if (pressE)
                {
                    drivingRover = false;
                    goatSr.enabled = true;
                    roverSr.sprite = WorldArt.RoverSprite(roverDir, false);
                    goatPos = roverPos + new Vector2(0f, -1.1f);
                    goatTf.position = new Vector3(goatPos.x, goatPos.y, 0f);
                    SoundFX.Play(SoundFX.Move, 0.5f);
                }
                return;
            }
            else if (Vector2.Distance(goatPos, roverPos) <= 2.1f)
            {
                cursorText.text = "PRESS [E] TO BOARD MARS ROVER";
                if (pressE)
                {
                    drivingRover = true;
                    goatSr.enabled = false;
                    goatPos = roverPos;
                    roverSr.sprite = WorldArt.RoverSprite(roverDir, true);
                    SoundFX.Play(SoundFX.Confirm, 0.55f);
                }
                return;
            }
        }

        Vector2Int dmgCell;
        if (FindNearbyDamagedCell(2.1f, out dmgCell))
        {
            if (!HasGoatGlue())
            {
                cursorText.text = "BROKEN STRUCTURE — SUPER GOAT-GLUE 4000 MISSING!";
                if (pressR) SoundFX.Play(SoundFX.Back, 0.55f);
            }
            else if (!IsGoatGlueEquipped())
            {
                cursorText.text = "BROKEN STRUCTURE — HOLD SUPER GOAT-GLUE 4000 IN HOTBAR (SLOT 1) TO REPAIR!";
                if (pressR) SoundFX.Play(SoundFX.Back, 0.55f);
            }
            else
            {
                cursorText.text = "HOLDING GOAT-GLUE 4000 — PRESS [R] TO OPEN WIRE & HULL REPAIR PUZZLE";
                if (pressR)
                    OpenRepairPuzzle(dmgCell);
            }
            return;
        }

        Vector2Int itemCell;
        ItemId nearId;
        if (FindNearbyBaseItem(1.75f, out itemCell, out nearId))
        {
            if (nearId == ItemId.Grass)
            {
                bool tastifierOn = IsFoodTastifierActive();
                cursorText.text = tastifierOn
                    ? "PRESS [E] TO EAT GRASS BALE (FOOD TASTIFIER BOOST: +34% FOOD!)"
                    : "PRESS [E] TO EAT GRASS BALE";
                if (pressE)
                {
                    float gain = tastifierOn ? 34f : 22f;
                    hunger = Mathf.Clamp(hunger + gain, 0f, 100f);
                    health = Mathf.Clamp(health + (tastifierOn ? 14f : 8f), 0f, 100f);
                    ConsumeGridItem(itemCell.x, itemCell.y);
                    SoundFX.Play(SoundFX.Eat, 0.6f);
                    if (tastifierOn) SpawnTastifierBurst(goatPos, "TASTIFIER BOOST! +34% FOOD");
                }
                return;
            }
            if (nearId == ItemId.Water)
            {
                cursorText.text = "PRESS [E] TO DRINK WATER MODULE";
                if (pressE)
                {
                    thirst = Mathf.Clamp(thirst + 32f, 0f, 100f);
                    health = Mathf.Clamp(health + 8f, 0f, 100f);
                    ConsumeGridItem(itemCell.x, itemCell.y);
                    SoundFX.Play(SoundFX.Drink, 0.6f);
                }
                return;
            }
            if (nearId == ItemId.Purifier)
            {
                cursorText.text = baseSealed
                    ? "AIR PURIFIER ONLINE (GENERATING OXYGEN WHILE POWERED)"
                    : "AIR PURIFIER STANDBY — REPAIR CRACKED WALLS WITH GOAT-GLUE [R] TO RESTORE SEAL!";
                return;
            }
            if (nearId == ItemId.Tastifier)
            {
                cursorText.text = "FOOD TASTIFIER ONLINE — ALL MEALS RESTORE +60% EXTRA HUNGER!";
                return;
            }
        }

        if (nearMeteor == null)
        {
            int gx = Mathf.FloorToInt(goatPos.x);
            int gy = Mathf.FloorToInt(goatPos.y);
            bool indoors = !drivingRover && gx >= 0 && gx < MapW && gy >= 0 && gy < MapH && indoorTile[gx, gy];

            if ((dustStormActive || quakeActive) && !indoors)
                cursorText.text = "DANGER! GET INSIDE YOUR BASE IMMEDIATELY OR LOSE MASSIVE HEALTH!";
            else if (!baseSealed && indoors)
                cursorText.text = "SHELTERED INDOORS — WALK TO RED-GLOWING CRACKED WALLS & HOLD GOAT-GLUE [R] TO SEAL BREACH!";
            else if (Vector2.Distance(goatPos, landingPodPos) <= 3.6f)
                cursorText.text = "G.O.A.T.-1 MARS LANDER — TOUCHDOWN CAPSULE (HULL INTACT)";
            else
                cursorText.text = "SELECT SHIELD IN HOTBAR OUTSIDE, HOLD GOAT-GLUE TO REPAIR [R], OR DRIVE ROVER TO METEORITES";
        }
    }

    void OpenRepairPuzzle(Vector2Int cell)
    {
        repairTargetCell = cell;
        repairPopupOpen = true;
        repairCloseDelay = 0f;
        selectedLeftWire = -1;

        for (int i = 0; i < 4; i++)
        {
            wireConnected[i] = false;
            wireLinks[i].gameObject.SetActive(false);
            rightWireColor[i] = i;
        }

        for (int i = 3; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = rightWireColor[i];
            rightWireColor[i] = rightWireColor[j];
            rightWireColor[j] = tmp;
        }

        RefreshRepairPuzzleUI();
        repairStatusText.text = "CLICK A LEFT TERMINAL, THEN CLICK THE MATCHING COLOR & LABEL ON THE RIGHT";
        repairStatusText.color = UIKit.Cyan;
        repairPopup.SetActive(true);
        SoundFX.Play(SoundFX.Confirm, 0.5f);
    }

    void CloseRepairPopup()
    {
        repairPopupOpen = false;
        repairPopup.SetActive(false);
        blockPlacementUntilMouseUp = true;
    }

    void OnLeftTerminalClicked(int leftIdx)
    {
        if (repairCloseDelay > 0f || wireConnected[leftIdx]) return;
        selectedLeftWire = leftIdx;
        SoundFX.Play(SoundFX.Blip, 0.45f);
        repairStatusText.text = "SELECTED " + WireNames[leftIdx] + " WIRE — CLICK MATCHING " + WireNames[leftIdx] + " TERMINAL ON RIGHT";
        repairStatusText.color = WireColors[leftIdx];
        RefreshRepairPuzzleUI();
    }

    void OnRightTerminalClicked(int rightIdx)
    {
        if (repairCloseDelay > 0f || selectedLeftWire < 0) return;

        int targetColor = rightWireColor[rightIdx];
        if (targetColor == selectedLeftWire)
        {
            wireConnected[selectedLeftWire] = true;
            PositionWireLink(selectedLeftWire, rightIdx);
            selectedLeftWire = -1;
            SoundFX.Play(SoundFX.Move, 0.55f);
            RefreshRepairPuzzleUI();

            bool allDone = true;
            for (int i = 0; i < 4; i++)
                if (!wireConnected[i]) { allDone = false; break; }

            if (allDone)
            {
                repairStatusText.text = "CIRCUIT WELDED & SEALED WITH SUPER GOAT-GLUE 4000!";
                repairStatusText.color = UIKit.Green;
                repairCloseDelay = 0.45f;
                SoundFX.Play(SoundFX.Repair, 0.7f);
            }
            else
            {
                repairStatusText.text = "WIRE WELDED! CONNECT REMAINING TERMINALS";
                repairStatusText.color = UIKit.Green;
            }
        }
        else
        {
            selectedLeftWire = -1;
            SoundFX.Play(SoundFX.Back, 0.55f);
            repairStatusText.text = "WRONG TERMINAL! MATCH THE SAME WIRE COLOR & LABEL";
            repairStatusText.color = UIKit.Red;
            RefreshRepairPuzzleUI();
        }
    }

    void PositionWireLink(int leftIdx, int rightIdx)
    {
        float leftY = 205f - (32f + leftIdx * 92f + 34f);
        float rightY = 205f - (32f + rightIdx * 92f + 34f);
        Vector2 a = new Vector2(-225f, leftY);
        Vector2 b = new Vector2(225f, rightY);
        Vector2 diff = b - a;

        var lr = wireLinks[leftIdx];
        lr.gameObject.SetActive(true);
        lr.anchoredPosition = (a + b) * 0.5f;
        lr.sizeDelta = new Vector2(diff.magnitude, 18f);
        lr.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);
        wireLinkImgs[leftIdx].color = WireColors[leftIdx];
    }

    void RefreshRepairPuzzleUI()
    {
        for (int i = 0; i < 4; i++)
        {
            Color lc = WireColors[i];
            leftWireTexts[i].text = WireNames[i];
            leftWireTexts[i].color = wireConnected[i] ? (lc * 0.75f) : Color.white;
            leftWireSwatches[i].color = lc;

            if (wireConnected[i])
                leftWireImgs[i].color = new Color(0.55f, 0.75f, 0.6f, 1f);
            else if (selectedLeftWire == i)
                leftWireImgs[i].color = Color.Lerp(lc, Color.white, 0.5f);
            else
                leftWireImgs[i].color = Color.white;

            int rColorIdx = rightWireColor[i];
            Color rc = WireColors[rColorIdx];
            rightWireTexts[i].text = WireNames[rColorIdx];
            rightWireTexts[i].color = wireConnected[rColorIdx] ? (rc * 0.75f) : Color.white;
            rightWireSwatches[i].color = rc;
            rightWireImgs[i].color = wireConnected[rColorIdx] ? new Color(0.55f, 0.75f, 0.6f, 1f) : Color.white;
        }
    }

    void ConsumeGridItem(int x, int y)
    {
        grid[x, y] = -1;
        cellRenderers[x, y].enabled = false;
        RecomputeBaseState(false);
    }

    bool FindNearbyDamagedCell(float maxDist, out Vector2Int found)
    {
        found = Vector2Int.zero;
        for (int y = BuildY; y < BuildY + BuildH; y++)
        {
            for (int x = SolarX; x < BuildX + BuildW; x++)
            {
                if (cellDamaged[x, y])
                {
                    float d = Vector2.Distance(goatPos, new Vector2(x + 0.5f, y + 0.5f));
                    if (d <= maxDist)
                    {
                        found = new Vector2Int(x, y);
                        return true;
                    }
                }
            }
        }
        return false;
    }

    bool FindNearbyBaseItem(float maxDist, out Vector2Int found, out ItemId itemId)
    {
        found = Vector2Int.zero;
        itemId = ItemId.Wall;
        for (int y = BuildY; y < BuildY + BuildH; y++)
        {
            for (int x = BuildX; x < BuildX + BuildW; x++)
            {
                int v = grid[x, y];
                if (v == (int)ItemId.Grass || v == (int)ItemId.Water || v == (int)ItemId.Purifier || v == (int)ItemId.Tastifier)
                {
                    float d = Vector2.Distance(goatPos, new Vector2(x + 0.5f, y + 0.5f));
                    if (d <= maxDist)
                    {
                        found = new Vector2Int(x, y);
                        itemId = (ItemId)v;
                        return true;
                    }
                }
            }
        }
        return false;
    }

    void SetVitalBar(int idx, float value, Color normalColor, bool isCritical)
    {
        float pct = Mathf.Clamp01(value / 100f);
        if (vitalFillRects[idx] != null)
            vitalFillRects[idx].anchorMax = new Vector2(pct, 1f);

        if (vitalFillImgs[idx] != null)
            vitalFillImgs[idx].color = isCritical ? UIKit.Red : normalColor;

        if (vitalValueTexts[idx] != null)
        {
            vitalValueTexts[idx].text = Mathf.RoundToInt(value) + "%";
            vitalValueTexts[idx].color = isCritical ? UIKit.Yellow : Color.white;
        }
    }

    void UpdateSurvivalHUD(int intactSolar, int totalSolar, bool goatIndoors, bool goatInSealedBase)
    {
        int mins = Mathf.FloorToInt(survivalTimeLeft / 60f);
        int secs = Mathf.FloorToInt(survivalTimeLeft % 60f);
        string status;
        if (dustStormActive)
            status = goatIndoors
                ? "RED DUST STORM (" + Mathf.CeilToInt(disasterRemaining) + "s) — SHELTERED INDOORS"
                : "RED DUST STORM (" + Mathf.CeilToInt(disasterRemaining) + "s) — GET INDOORS!";
        else if (quakeActive)
            status = goatIndoors
                ? "MARSQUAKE (" + Mathf.CeilToInt(disasterRemaining) + "s) — SHELTERED INDOORS"
                : "MARSQUAKE (" + Mathf.CeilToInt(disasterRemaining) + "s) — GET INDOORS!";
        else
            status = baseSealed ? "BASE SEALED" : "HULL BREACH — PATCH WALLS [R]";

        phaseText.text = string.Format("SURVIVE: {0:00}:{1:00}   |   {2}", mins, secs, status);
        phaseText.color = ((dustStormActive || quakeActive) && !goatIndoors) || !baseSealed ? UIKit.Red : UIKit.Orange;

        // Update the 6 visual vital progress bars
        SetVitalBar(0, health,    new Color(0.24f, 0.90f, 0.42f, 1f), health < 35f);
        SetVitalBar(1, oxygen,    new Color(0.24f, 0.86f, 1.00f, 1f), oxygen < 25f);
        SetVitalBar(2, power,     new Color(1.00f, 0.82f, 0.22f, 1f), power < 20f);
        SetVitalBar(3, hunger,    new Color(1.00f, 0.58f, 0.20f, 1f), hunger < 25f);
        SetVitalBar(4, thirst,    new Color(0.26f, 0.64f, 1.00f, 1f), thirst < 25f);
        SetVitalBar(5, radiation, new Color(0.82f, 0.32f, 0.96f, 1f), radiation > 70f);

        string meteorInfo = "SCANNING SKY...";
        float nearestDist = 999f;
        foreach (var m in meteorSites)
        {
            float d = Vector2.Distance(goatPos, m.pos);
            if (d < nearestDist)
            {
                nearestDist = d;
                Vector2 dir = (m.pos - goatPos).normalized;
                string ns = dir.y > 0.35f ? "N" : (dir.y < -0.35f ? "S" : "");
                string ew = dir.x > 0.35f ? "E" : (dir.x < -0.35f ? "W" : "");
                meteorInfo = "METEORITE (" + ns + ew + " " + Mathf.RoundToInt(d) + "m)";
            }
        }

        string radStatus;
        if (goatInSealedBase)
            radStatus = "INDOORS (SAFE)";
        else if (goatIndoors)
            radStatus = "HULL BREACH (LEAK)";
        else if (IsRadiationShieldEquipped())
            radStatus = "RAD SHIELD ON";
        else if (drivingRover)
            radStatus = "ROVER HULL";
        else
            radStatus = "EXPOSED!";

        string tastifierTag = IsFoodTastifierActive() ? "   |   TASTIFIER: +60% FOOD" : "";

        radarText.text = string.Format(
            "SOLAR: {0}/{1}   |   SUIT: {2}   |   SENSOR: {3}{4}",
            intactSolar, totalSolar, radStatus, meteorInfo, tastifierTag);
    }

    void TriggerEndScreen(bool victory, string causeHeadline, string tipBody)
    {
        gameEnded = true;
        ghostSr.enabled = false;
        if (repairPopup != null) repairPopup.SetActive(false);
        endPopup.SetActive(true);

        endTitleText.text = victory ? "SURVIVED — MARTIAN PIONEER!" : "MISSION FAILED — GOAT DOWN";
        endTitleText.color = victory ? UIKit.Green : UIKit.Red;

        endCauseText.text = causeHeadline;
        endCauseText.color = victory ? UIKit.Green : UIKit.Yellow;

        endTipText.text = tipBody;
        endTipText.color = victory ? Color.white : UIKit.Cyan;

        float elapsedSecs = Mathf.Max(0f, 30f * 60f - survivalTimeLeft);
        int sMins = Mathf.FloorToInt(elapsedSecs / 60f);
        int sSecs = Mathf.FloorToInt(elapsedSecs % 60f);

        endStatsText.text = string.Format(
            "EXPEDITION SUMMARY:\n" +
            "TIME SURVIVED: {0:00}:{1:00}  |  METEORITES HARVESTED: {2}  |  DISASTERS SURVIVED: {3}\n" +
            "FINAL VITALS: HP {4}% • O2 {5}% • FOOD {6}% • H2O {7}% • RAD {8}%",
            sMins, sSecs, meteoritesHarvested, disastersSurvived,
            Mathf.RoundToInt(health), Mathf.RoundToInt(oxygen),
            Mathf.RoundToInt(hunger), Mathf.RoundToInt(thirst), Mathf.RoundToInt(radiation));

        SoundFX.Play(victory ? SoundFX.Confirm : SoundFX.Alarm, 0.75f);
    }

    // ---------------- Placement Rules & Auto-Tiling ----------------
    bool InMainBuildZone(int x, int y)
    {
        return x >= BuildX && x < BuildX + BuildW && y >= BuildY && y < BuildY + BuildH;
    }

    bool InSolarZone(int x, int y)
    {
        return x >= SolarX && x < SolarX + SolarW && y >= BuildY && y < BuildY + BuildH;
    }

    bool CanPlaceAt(ItemId id, int x, int y, out string reason)
    {
        if (unplaced[(int)id] <= 0)
        {
            reason = "NONE LEFT IN CARGO (PRESS [F] FOR DELETE MODE)";
            return false;
        }

        if (id == ItemId.Solar)
        {
            bool bothInSolar = InSolarZone(x, y) && InSolarZone(x + 1, y);
            bool bothInMain = InMainBuildZone(x, y) && InMainBuildZone(x + 1, y);
            if (!bothInSolar && !bothInMain)
            {
                reason = "SOLAR PANEL NEEDS 2 HORIZONTAL TILES INSIDE THE OUTDOOR GRID";
                return false;
            }
            if (grid[x, y] != -1 || grid[x + 1, y] != -1)
            {
                reason = "BOTH TILES MUST BE CLEAR (PRESS [F] TO DELETE)";
                return false;
            }
            if (enclosed[x, y] || enclosed[x + 1, y])
            {
                reason = "INDOOR TILE! SOLAR PANELS MUST BE PLACED OUTSIDE THE BASE";
                return false;
            }

            int gx = Mathf.FloorToInt(goatPos.x);
            int gy = Mathf.FloorToInt(goatPos.y);
            if (gy == y && (gx == x || gx == x + 1))
            {
                reason = "STEP ASIDE BEFORE PLACING SOLAR PANEL";
                return false;
            }

            reason = "CLICK / DRAG: PLACE 2x1 OUTDOOR SOLAR PANEL  |  [F]: DELETE MODE";
            return true;
        }

        if (!InMainBuildZone(x, y))
        {
            reason = "RESERVED FOR OUTDOOR SOLAR PANELS — BUILD BASE IN BLUE GRID";
            return false;
        }
        if (grid[x, y] != -1)
        {
            reason = "TILE OCCUPIED (PRESS [F] TO DELETE)";
            return false;
        }

        int goatX = Mathf.FloorToInt(goatPos.x);
        int goatY = Mathf.FloorToInt(goatPos.y);
        if (id != ItemId.Door && x == goatX && y == goatY)
        {
            reason = "STEP ASIDE BEFORE PLACING HERE";
            return false;
        }

        if (IsWallMountedAppliance(id))
        {
            if (!TouchesWall(x, y))
            {
                reason = "CANNOT PLACE IN MIDDLE OF ROOM — " + GameData.Get(id).name.ToUpperInvariant() + " MUST TOUCH A METAL WALL!";
                return false;
            }
        }

        reason = "CLICK / DRAG: PLACE " + GameData.Get(id).name.ToUpperInvariant() + "  |  [F]: DELETE MODE";
        return true;
    }

    bool TouchesWall(int x, int y)
    {
        return IsWall(x, y + 1) || IsWall(x + 1, y) || IsWall(x, y - 1) || IsWall(x - 1, y);
    }

    bool IsWall(int x, int y)
    {
        if (x < 0 || x >= MapW || y < 0 || y >= MapH) return false;
        return grid[x, y] == (int)ItemId.Wall && !cellDamaged[x, y];
    }

    // Intact wall or door (used for air-tight pressurization check)
    bool IsBoundary(int x, int y)
    {
        if (x < 0 || x >= MapW || y < 0 || y >= MapH) return false;
        if (cellDamaged[x, y]) return false;
        int v = grid[x, y];
        return v == (int)ItemId.Wall || v == (int)ItemId.Door;
    }

    // Any wall (intact or cracked) or door (used for physical indoor shelter check)
    bool IsStructureTile(int x, int y)
    {
        if (x < 0 || x >= MapW || y < 0 || y >= MapH) return false;
        int v = grid[x, y];
        return v == (int)ItemId.Wall || v == (int)ItemId.Door;
    }

    void PlaceItem(ItemId id, int x, int y)
    {
        grid[x, y] = (int)id;
        cellDamaged[x, y] = false;
        unplaced[(int)id]--;

        var sr = cellRenderers[x, y];
        sr.enabled = true;
        sr.color = Color.white;
        sr.sprite = WorldArt.ItemSprite(id);

        if (id == ItemId.Solar)
        {
            grid[x + 1, y] = SecondaryCell;
            sr.transform.localPosition = new Vector3(0.5f, 0f, 0f);
        }
        else
        {
            sr.transform.localPosition = Vector3.zero;
        }

        if (id == ItemId.Wall)
        {
            var glowGo = new GameObject("WallGlow");
            glowGo.transform.SetParent(sr.transform, false);
            var gsr = glowGo.AddComponent<SpriteRenderer>();
            gsr.sprite = WorldArt.WallGlow();
            gsr.sortingOrder = 2;
            wallGlows[x, y] = glowGo;
            RefreshWallNeighbors(x, y);
        }
        else if (id == ItemId.Door)
        {
            RefreshWallNeighbors(x, y);
        }
        else if (id == ItemId.Tastifier)
        {
            var auraGo = new GameObject("TastifierAura");
            auraGo.transform.SetParent(sr.transform, false);
            var asr = auraGo.AddComponent<SpriteRenderer>();
            asr.sprite = WorldArt.TastifierAura();
            asr.sortingOrder = 4;

            var spGo = new GameObject("TastifierSparkle");
            spGo.transform.SetParent(auraGo.transform, false);
            spGo.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            var ssr = spGo.AddComponent<SpriteRenderer>();
            ssr.sprite = WorldArt.FlavorSparkle();
            ssr.sortingOrder = 5;

            machineEffects[x, y] = auraGo;
        }

        SoundFX.Play(SoundFX.Move, 0.45f);
        RefreshHotbar();
        RecomputeBaseState(true);
    }

    void RemoveItem(int x, int y)
    {
        int raw = grid[x, y];
        if (raw == -1) return;

        if (raw == SecondaryCell)
        {
            if (x > 0 && grid[x - 1, y] == (int)ItemId.Solar)
                RemoveItem(x - 1, y);
            return;
        }

        ItemId id = (ItemId)raw;
        grid[x, y] = -1;
        cellDamaged[x, y] = false;
        unplaced[raw]++;

        cellRenderers[x, y].enabled = false;
        cellRenderers[x, y].transform.localPosition = Vector3.zero;

        if (id == ItemId.Solar && x + 1 < MapW && grid[x + 1, y] == SecondaryCell)
            grid[x + 1, y] = -1;

        if (wallGlows[x, y] != null)
        {
            Destroy(wallGlows[x, y]);
            wallGlows[x, y] = null;
        }

        if (machineEffects[x, y] != null)
        {
            Destroy(machineEffects[x, y]);
            machineEffects[x, y] = null;
        }

        if (id == ItemId.Wall || id == ItemId.Door) RefreshWallNeighbors(x, y);

        SoundFX.Play(SoundFX.Back, 0.4f);
        RefreshHotbar();
        RecomputeBaseState(false);
    }

    void RefreshWallNeighbors(int x, int y)
    {
        UpdateWallSprite(x, y);
        UpdateWallSprite(x, y + 1);
        UpdateWallSprite(x + 1, y);
        UpdateWallSprite(x, y - 1);
        UpdateWallSprite(x - 1, y);
    }

    void UpdateWallSprite(int x, int y)
    {
        if (x < 0 || x >= MapW || y < 0 || y >= MapH) return;
        if (grid[x, y] != (int)ItemId.Wall) return;
        int mask = 0;
        if (IsStructureTile(x, y + 1)) mask |= 1;
        if (IsStructureTile(x + 1, y)) mask |= 2;
        if (IsStructureTile(x, y - 1)) mask |= 4;
        if (IsStructureTile(x - 1, y)) mask |= 8;
        cellRenderers[x, y].sprite = WorldArt.MetalWall(mask);
    }

    // ---------------- Enclosure Flood-Fill, Indicators & Auto-Floors ----------------
    static string ShortName(ItemId id)
    {
        switch (id)
        {
            case ItemId.Solar: return "OUTDOOR SOLAR";
            case ItemId.Battery: return "BATTERY";
            case ItemId.Water: return "WATER";
            case ItemId.Grass: return "GRASS";
            case ItemId.Purifier: return "PURIFIER (WALL)";
            case ItemId.Console: return "CONSOLE (WALL)";
            default: return GameData.Get(id).name.ToUpperInvariant();
        }
    }

    void FloodFillOutside(int minX, int maxX, int minY, int maxY, bool[,] outside, System.Func<int, int, bool> isBlocker)
    {
        var q = new Queue<Vector2Int>();

        void TryEnqueue(int x, int y)
        {
            if (x < minX || x > maxX || y < minY || y > maxY) return;
            if (outside[x, y] || isBlocker(x, y)) return;
            outside[x, y] = true;
            q.Enqueue(new Vector2Int(x, y));
        }

        for (int x = minX; x <= maxX; x++)
        {
            TryEnqueue(x, minY);
            TryEnqueue(x, maxY);
        }
        for (int y = minY; y <= maxY; y++)
        {
            TryEnqueue(minX, y);
            TryEnqueue(maxX, y);
        }

        while (q.Count > 0)
        {
            var p = q.Dequeue();
            TryEnqueue(p.x + 1, p.y);
            TryEnqueue(p.x - 1, p.y);
            TryEnqueue(p.x, p.y + 1);
            TryEnqueue(p.x, p.y - 1);
        }
    }

    void RecomputeBaseState(bool announceOnSeal)
    {
        int minX = BuildX - 1, maxX = BuildX + BuildW;
        int minY = BuildY - 1, maxY = BuildY + BuildH;

        bool[,] outsideSealed = new bool[MapW, MapH];
        FloodFillOutside(minX, maxX, minY, maxY, outsideSealed, IsBoundary);

        bool[,] outsidePhysical = new bool[MapW, MapH];
        FloodFillOutside(minX, maxX, minY, maxY, outsidePhysical, IsStructureTile);

        int interiorCount = 0;
        int doorsPlaced = 0;
        int outdoorSolarCount = 0;
        bool indoorSolarPlaced = false;
        bool exposedEquipment = false;
        var insideCounts = new int[GameData.Items.Length];

        for (int y = BuildY; y < BuildY + BuildH; y++)
        {
            for (int x = SolarX; x < BuildX + BuildW; x++)
            {
                enclosed[x, y] = false;
                indoorTile[x, y] = false;
            }
        }

        for (int y = BuildY; y < BuildY + BuildH; y++)
        {
            for (int x = BuildX; x < BuildX + BuildW; x++)
            {
                int v = grid[x, y];
                bool isClosedTile = !outsideSealed[x, y] && !IsBoundary(x, y);
                bool isShelteredTile = (!outsidePhysical[x, y] && !IsStructureTile(x, y)) || v == (int)ItemId.Door;

                enclosed[x, y] = isClosedTile;
                indoorTile[x, y] = isShelteredTile;
                if (isClosedTile) interiorCount++;

                if (v == (int)ItemId.Door)
                {
                    doorsPlaced++;
                }
                else if (v == (int)ItemId.Solar)
                {
                    bool panelInside = isClosedTile || (x + 1 < MapW && !outsideSealed[x + 1, y] && !IsBoundary(x + 1, y));
                    if (panelInside) indoorSolarPlaced = true;
                    else outdoorSolarCount++;
                }
                else if (v >= 0 && v != (int)ItemId.Wall)
                {
                    if (isClosedTile) insideCounts[v]++;
                    else exposedEquipment = true;
                }
            }
        }

        for (int y = BuildY; y < BuildY + BuildH; y++)
            for (int x = SolarX; x < SolarX + SolarW; x++)
                if (grid[x, y] == (int)ItemId.Solar)
                    outdoorSolarCount++;

        var structMissing = new List<string>();
        if (!HasGoatGlue()) structMissing.Add("1 GOAT-GLUE 4000 (CARGO)");
        if (interiorCount < 12) structMissing.Add("SEAL ROOM");
        if (doorsPlaced < 1) structMissing.Add("1 DOOR");
        if (indoorSolarPlaced) structMissing.Add("MOVE SOLAR OUTSIDE");
        if (exposedEquipment) structMissing.Add("MOVE ITEMS INSIDE");

        int minSolar = GameData.Get(ItemId.Solar).min;
        if (outdoorSolarCount < minSolar)
            structMissing.Add((minSolar - outdoorSolarCount) + " " + ShortName(ItemId.Solar));

        var equipMissing = new List<string>();
        foreach (var req in EssentialInsideItems)
        {
            int needed = GetRequiredInsideCount(req);
            if (insideCounts[(int)req] < needed)
                equipMissing.Add((needed - insideCounts[(int)req]) + " " + ShortName(req));
        }

        baseSealed = structMissing.Count == 0 && equipMissing.Count == 0;

        for (int y = BuildY; y < BuildY + BuildH; y++)
        {
            for (int x = SolarX; x < BuildX + BuildW; x++)
            {
                var fsr = floorRenderers[x, y];
                if (fsr == null) continue;

                if (survivalStarted)
                {
                    if (indoorTile[x, y])
                    {
                        fsr.enabled = true;
                        fsr.sprite = WorldArt.AutoFloor();
                    }
                    else
                    {
                        fsr.enabled = false;
                    }
                }
                else
                {
                    fsr.enabled = true;
                    if (InMainBuildZone(x, y))
                        fsr.sprite = (baseSealed && enclosed[x, y]) ? WorldArt.AutoFloor() : WorldArt.BuildZoneSlot();
                    else
                        fsr.sprite = WorldArt.SolarZoneSlot();
                }
            }
        }

        RefreshPlacementIndicators();

        if (survivalStarted) return;

        if (baseSealed)
        {
            phaseText.text = "PHASE: BASE SEALED!";
            objectiveText.text = "Floors & outdoor solar online! Walk to COMMAND CONSOLE and press [E] to start Survival.";
            objectiveText.color = UIKit.Green;

            if (announceOnSeal && !greetedSeal)
            {
                greetedSeal = true;
                SoundFX.Play(SoundFX.Confirm);
                game.dialogue.Open(new[]
                {
                    "Magnificent! Your base is air-tight, auto-floors are online, and your outdoor solar array is soaking up Mars sunlight.",
                    "Walk up to the Command Console and press E to begin the Martian Survival Phase!"
                });
            }
        }
        else
        {
            phaseText.text = "PHASE: BASE CONSTRUCTION";
            string line1 = structMissing.Count > 0 ? ("STRUCTURE: " + string.Join(", ", structMissing)) : "STRUCTURE: COMPLETE";
            string line2 = equipMissing.Count > 0 ? ("INSIDE BASE: " + string.Join(", ", equipMissing)) : "INSIDE BASE: COMPLETE";
            objectiveText.text = line1 + "\n" + line2;
            objectiveText.color = UIKit.Cyan;
        }
    }

    void RefreshPlacementIndicators()
    {
        ItemId selectedId = buildPalette.Count > 0 ? buildPalette[paletteIndex] : ItemId.Wall;
        bool solarSelected = !survivalStarted && !deleteMode && buildPalette.Count > 0 && selectedId == ItemId.Solar;
        bool wallApplianceSelected = !survivalStarted && !deleteMode && buildPalette.Count > 0 && IsWallMountedAppliance(selectedId);

        for (int y = BuildY; y < BuildY + BuildH; y++)
        {
            for (int x = SolarX; x < BuildX + BuildW; x++)
            {
                var isr = indicatorRenderers[x, y];
                if (isr == null) continue;

                if (cellDamaged[x, y])
                {
                    isr.enabled = true;
                    isr.sprite = WorldArt.DamagedOverlay();
                    continue;
                }

                if (solarSelected)
                {
                    if (enclosed[x, y])
                    {
                        isr.enabled = true;
                        isr.sprite = WorldArt.IndoorBlockedIndicator();
                    }
                    else if (grid[x, y] == -1)
                    {
                        isr.enabled = true;
                        isr.sprite = WorldArt.OutdoorSolarIndicator();
                    }
                    else
                    {
                        isr.enabled = false;
                    }
                    continue;
                }

                if (wallApplianceSelected && InMainBuildZone(x, y))
                {
                    if (grid[x, y] != -1)
                    {
                        isr.enabled = false;
                    }
                    else if (TouchesWall(x, y))
                    {
                        isr.enabled = true;
                        isr.sprite = WorldArt.OutdoorSolarIndicator();
                    }
                    else
                    {
                        isr.enabled = true;
                        isr.sprite = WorldArt.IndoorBlockedIndicator();
                    }
                    continue;
                }

                isr.enabled = false;
            }
        }
    }

    bool IsNearItem(ItemId id, float maxDist)
    {
        for (int y = BuildY; y < BuildY + BuildH; y++)
        {
            for (int x = BuildX; x < BuildX + BuildW; x++)
            {
                if (grid[x, y] == (int)id)
                {
                    float d = Vector2.Distance(goatPos, new Vector2(x + 0.5f, y + 0.5f));
                    if (d <= maxDist) return true;
                }
            }
        }
        return false;
    }

    // ---------------- Popups & Dual-Mode Hotbar ----------------
    void ToggleClearPopup(bool show)
    {
        clearPopupOpen = show;
        clearPopup.SetActive(show);
        SoundFX.Play(show ? SoundFX.Alarm : SoundFX.Back, 0.45f);
    }

    void ExecuteClearBase()
    {
        blockPlacementUntilMouseUp = true;
        lastDragCell = new Vector2Int(-1, -1);
        lastDragBtn = 0;

        for (int y = BuildY; y < BuildY + BuildH; y++)
        {
            for (int x = SolarX; x < BuildX + BuildW; x++)
            {
                int raw = grid[x, y];
                if (raw >= 0)
                    unplaced[raw]++;

                grid[x, y] = -1;
                cellDamaged[x, y] = false;
                cellRenderers[x, y].enabled = false;
                cellRenderers[x, y].transform.localPosition = Vector3.zero;

                if (wallGlows[x, y] != null)
                {
                    Destroy(wallGlows[x, y]);
                    wallGlows[x, y] = null;
                }

                if (machineEffects[x, y] != null)
                {
                    Destroy(machineEffects[x, y]);
                    machineEffects[x, y] = null;
                }
            }
        }

        greetedSeal = false;
        ToggleClearPopup(false);
        SoundFX.Play(SoundFX.Confirm, 0.55f);
        RefreshHotbar();
        RecomputeBaseState(false);
    }

    void ToggleConsole(bool show)
    {
        consoleOpen = show;
        consolePopup.SetActive(show);
        if (!show) return;

        SoundFX.Play(SoundFX.Confirm, 0.5f);
        startSurvivalBtn.Interactable = baseSealed && !survivalStarted;

        consoleBodyText.text =
            "BASE DIAGNOSTICS:\n\n" +
            "  * AIR-TIGHT ENCLOSURE & FLOORS:  " + (baseSealed ? "ONLINE" : "INCOMPLETE") + "\n" +
            "  * OUTDOOR 2x1 SOLAR ARRAY:       " + (baseSealed ? "ONLINE" : "INCOMPLETE / INDOORS") + "\n" +
            "  * SUPER GOAT-GLUE 4000 (REQ):    " + (HasGoatGlue() ? "VERIFIED IN CARGO" : "MISSING! RETURN TO MENU & PACK GLUE") + "\n" +
            "  * RATION CAP (WATER / GRASS):    MAX 5 EACH (SCAVENGE METEORITES TO SURVIVE)\n" +
            "  * FOOD TASTIFIER MODULE:         " + (IsFoodTastifierActive() ? "ONLINE (+60% FOOD BOOST)" : "NOT PACKED") + "\n" +
            "  * MARS ROVER ON SOUTH PAD:       " + (GameData.Count(ItemId.Rover) > 0 ? "READY ON PAD" : "NOT PACKED") + "\n\n" +
            (baseSealed
                ? "All required systems green. No rescue is coming — survive 30 Martian minutes on the frontier!"
                : "Seal your walls, keep Solar Panels OUTSIDE, and ensure Super Goat-Glue 4000 + required gear are packed.");
    }

    void OnStartSurvivalClicked()
    {
        if (!baseSealed || survivalStarted) return;
        blockPlacementUntilMouseUp = true;
        ToggleConsole(false);
        StartSurvivalAndExploration();
        SoundFX.Play(SoundFX.Confirm);

        game.dialogue.Open(new[]
        {
            "SURVIVAL PHASE STARTED! Nobody is coming to rescue us — survive 30 Martian minutes on the Red Planet!",
            "WATCH THE WEATHER: Red Dust Storms break ALL solar panels, and Marsquakes crack the ground and break walls. Get INDOORS during disasters or lose massive HP!",
            "Your Water & Grass reserves are capped at 5 — drive the Mars Rover out to fallen meteorites [E] to collect Cryo Ice Flasks and Meteor Rations!"
        });
    }

    void SelectPalette(int idx)
    {
        if (buildPalette.Count == 0) return;
        paletteIndex = Mathf.Clamp(idx, 0, buildPalette.Count - 1);
        if (paletteIndex < hotbarOffset) hotbarOffset = paletteIndex;
        if (paletteIndex >= hotbarOffset + HotbarSlots) hotbarOffset = paletteIndex - HotbarSlots + 1;
        SoundFX.Play(SoundFX.Move, 0.35f);
        RefreshHotbar();
        RefreshPlacementIndicators();
    }

    void RefreshHotbar()
    {
        if (survivalStarted)
        {
            for (int i = 0; i < HotbarSlots; i++)
            {
                int idx = survHotbarOffset + i;
                bool has = idx < survivalSlots.Count;
                slotNums[i].gameObject.SetActive(has);
                slotNames[i].gameObject.SetActive(has);
                slotCounts[i].gameObject.SetActive(has);

                if (!has)
                {
                    slotBgs[i].color = new Color(0.05f, 0.07f, 0.16f, 0.6f);
                    continue;
                }

                var s = survivalSlots[idx];
                bool sel = idx == survSlotIndex;

                slotBgs[i].color = sel ? new Color(0.22f, 0.36f, 0.72f, 0.95f) : new Color(0.08f, 0.12f, 0.28f, 0.85f);
                slotNames[i].text = s.name;
                slotNames[i].color = sel ? UIKit.Yellow : Color.white;

                if (s.isEquipment)
                {
                    slotCounts[i].text = sel ? "HOLDING" : "GEAR";
                    slotCounts[i].color = sel ? UIKit.Green : UIKit.Cyan;
                }
                else
                {
                    slotCounts[i].text = "x" + s.count;
                    slotCounts[i].color = UIKit.Green;
                }
            }

            if (survivalSlots.Count > 0)
            {
                var cur = survivalSlots[survSlotIndex];
                if (cur.isEquipment)
                {
                    SetBtnText(consumeBtn, cur.id == "shield" ? "HOLDING RAD SHIELD" : "HOLDING GOAT-GLUE [R]");
                    consumeBtn.Interactable = false;
                }
                else
                {
                    SetBtnText(consumeBtn, "CONSUME " + cur.name + " [C]");
                    consumeBtn.Interactable = true;
                }
                hotbarHintText.text = "SURVIVAL GEAR & SUPPLIES (" + (survSlotIndex + 1) + "/" + survivalSlots.Count + ")   |   [C] TO CONSUME   |   < > OR [Z]/[X] TO BROWSE";
            }
            else
            {
                SetBtnText(consumeBtn, "NO SUPPLIES HELD");
                consumeBtn.Interactable = false;
                hotbarHintText.text = "HARVEST FALLEN METEORITES OUTSIDE TO COLLECT CONSUMABLE SUPPLIES";
            }
            return;
        }

        // Build Phase Hotbar
        for (int i = 0; i < HotbarSlots; i++)
        {
            int idx = hotbarOffset + i;
            bool has = idx < buildPalette.Count;
            slotNums[i].gameObject.SetActive(has);
            slotNames[i].gameObject.SetActive(has);
            slotCounts[i].gameObject.SetActive(has);

            if (!has)
            {
                slotBgs[i].color = new Color(0.05f, 0.07f, 0.16f, 0.6f);
                continue;
            }

            ItemId id = buildPalette[idx];
            int left = unplaced[(int)id];
            bool sel = idx == paletteIndex;

            slotBgs[i].color = sel ? new Color(0.22f, 0.36f, 0.72f, 0.95f) : new Color(0.08f, 0.12f, 0.28f, 0.85f);
            slotNames[i].text = GameData.Get(id).name.ToUpperInvariant();
            slotNames[i].color = sel ? UIKit.Yellow : Color.white;
            slotCounts[i].text = "x" + left;
            slotCounts[i].color = left > 0 ? UIKit.Green : UIKit.Red;
        }

        if (buildPalette.Count > HotbarSlots)
            hotbarHintText.text = "CARGO ITEM " + (paletteIndex + 1) + "/" + buildPalette.Count + "   |   CLICK < > OR PRESS [Z]/[X] TO BROWSE ALL ITEMS";
        else if (buildPalette.Count > 0)
            hotbarHintText.text = "CARGO ITEM " + (paletteIndex + 1) + "/" + buildPalette.Count;
        else
            hotbarHintText.text = "";
    }

    void SnapCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        float cx = Mathf.Clamp(goatPos.x, halfW, MapW - halfW);
        float cy = Mathf.Clamp(goatPos.y, halfH, MapH - halfH);

        if (cameraShakeTimer > 0f)
        {
            cameraShakeTimer -= Time.unscaledDeltaTime;
            cx += Random.Range(-0.35f, 0.35f);
            cy += Random.Range(-0.35f, 0.35f);
        }

        cam.transform.position = new Vector3(cx, cy, -10f);
    }
}