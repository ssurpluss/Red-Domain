using System.Collections.Generic;
using UnityEngine;

// Procedural pixel art for the Martian surface, craters, boulders, mountains, cracks, meteorites, rover pad, base & goat.
public static class WorldArt
{
    const int S = 16;
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        cache.Clear();
    }

    static Sprite GetOrMake(string key, System.Func<Texture2D> maker, float ppu = S, Vector2? customPivot = null)
    {
        Sprite sp;
        if (cache.TryGetValue(key, out sp) && sp != null) return sp;
        var tex = maker();
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        Vector2 piv = customPivot ?? new Vector2(0.5f, 0.5f);
        sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), piv, ppu);
        cache[key] = sp;
        return sp;
    }

    static Texture2D Blank(int w = S, int h = S)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var clear = new Color32[w * h];
        t.SetPixels32(clear);
        return t;
    }

    static void FillRect(Color32[] px, int w, int h, int x0, int y0, int x1, int y1, Color32 c)
    {
        int minX = Mathf.Min(x0, x1);
        int maxX = Mathf.Max(x0, x1);
        int minY = Mathf.Min(y0, y1);
        int maxY = Mathf.Max(y0, y1);

        for (int y = Mathf.Max(0, minY); y <= Mathf.Min(h - 1, maxY); y++)
            for (int x = Mathf.Max(0, minX); x <= Mathf.Min(w - 1, maxX); x++)
                px[y * w + x] = c;
    }

    static void SetPx(Color32[] px, int w, int h, int x, int y, Color32 c)
    {
        if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = c;
    }

    // ---------------- Terrain, Craters, Rocks & Mountain Border ----------------
    public static Sprite MarsGround(int variant)
    {
        return GetOrMake("mars_" + variant, () =>
        {
            var t = Blank();
            var px = new Color32[S * S];
            var rnd = new System.Random(100 + variant * 37);

            var baseCol = new Color32(46, 20, 18, 255);
            var darkCol = new Color32(34, 14, 14, 255);
            var litCol = new Color32(64, 28, 22, 255);

            for (int i = 0; i < px.Length; i++)
            {
                int r = rnd.Next(10);
                px[i] = r == 0 ? darkCol : (r == 1 ? litCol : baseCol);
            }

            if (variant == 1)
            {
                SetPx(px, S, S, 4, 10, litCol); SetPx(px, S, S, 5, 10, darkCol);
                SetPx(px, S, S, 11, 4, litCol); SetPx(px, S, S, 12, 4, darkCol);
            }
            else if (variant == 2)
            {
                FillRect(px, S, S, 6, 6, 9, 9, darkCol);
                SetPx(px, S, S, 6, 10, litCol); SetPx(px, S, S, 7, 10, litCol);
            }

            t.SetPixels32(px);
            t.Apply();
            return t;
        });
    }

    public static Sprite MarsMountain(int variant)
    {
        return GetOrMake("mars_mtn_" + variant, () =>
        {
            var t = Blank();
            var px = new Color32[S * S];

            var deepShadow = new Color32(24, 10, 11, 255);
            var rockDark = new Color32(58, 25, 22, 255);
            var rockMid = new Color32(94, 42, 34, 255);
            var rockLight = new Color32(138, 66, 48, 255);
            var ridgeTop = new Color32(174, 94, 64, 255);

            FillRect(px, S, S, 0, 0, 15, 15, rockDark);

            int peakX = variant == 0 ? 7 : (variant == 1 ? 5 : 10);
            for (int y = 0; y < S; y++)
            {
                int halfSpan = Mathf.Max(1, (15 - y) / 2 + 2);
                int x0 = peakX - halfSpan;
                int x1 = peakX + halfSpan;

                FillRect(px, S, S, x0, y, peakX, y, rockLight);
                FillRect(px, S, S, peakX + 1, y, x1, y, rockMid);
                SetPx(px, S, S, peakX, y, ridgeTop);
                if (x0 - 1 >= 0) SetPx(px, S, S, x0 - 1, y, deepShadow);
                if (x1 + 1 < S) SetPx(px, S, S, x1 + 1, y, deepShadow);
            }

            FillRect(px, S, S, 0, 0, 15, 1, deepShadow);
            t.SetPixels32(px);
            t.Apply();
            return t;
        });
    }

    public static Sprite MarsBoulder(int variant)
    {
        return GetOrMake("mars_boulder_" + variant, () =>
        {
            var t = Blank();
            var px = new Color32[S * S];

            var shadow = new Color32(22, 9, 10, 190);
            var outline = new Color32(28, 13, 14, 255);
            var dark = new Color32(66, 30, 26, 255);
            var mid = new Color32(106, 50, 38, 255);
            var hi = new Color32(152, 78, 56, 255);

            if (variant == 0)
            {
                FillRect(px, S, S, 2, 1, 13, 3, shadow);
                FillRect(px, S, S, 3, 3, 12, 11, outline);
                FillRect(px, S, S, 4, 4, 11, 10, mid);
                FillRect(px, S, S, 4, 8, 9, 10, hi);
                FillRect(px, S, S, 8, 4, 11, 7, dark);
                FillRect(px, S, S, 5, 11, 9, 12, hi);
            }
            else
            {
                FillRect(px, S, S, 1, 1, 14, 3, shadow);
                FillRect(px, S, S, 2, 3, 9, 10, outline);
                FillRect(px, S, S, 3, 4, 8, 9, mid);
                FillRect(px, S, S, 3, 7, 6, 9, hi);

                FillRect(px, S, S, 8, 2, 13, 8, outline);
                FillRect(px, S, S, 9, 3, 12, 7, dark);
                SetPx(px, S, S, 9, 6, hi); SetPx(px, S, S, 10, 6, hi);
            }

            t.SetPixels32(px);
            t.Apply();
            return t;
        });
    }

    public static Sprite MarsPebbles(int variant)
    {
        return GetOrMake("mars_pebbles_" + variant, () =>
        {
            var t = Blank();
            var px = new Color32[S * S];
            var dark = new Color32(30, 13, 13, 255);
            var mid = new Color32(84, 38, 30, 255);
            var hi = new Color32(126, 62, 46, 255);

            int ox = variant == 0 ? 3 : 6;
            int oy = variant == 0 ? 4 : 9;
            FillRect(px, S, S, ox, oy, ox + 2, oy + 1, dark);
            SetPx(px, S, S, ox, oy + 1, hi); SetPx(px, S, S, ox + 1, oy + 1, mid);

            FillRect(px, S, S, 10, 3, 12, 4, dark);
            SetPx(px, S, S, 10, 4, mid);

            FillRect(px, S, S, 5, 12, 6, 13, mid);
            SetPx(px, S, S, 5, 13, hi);

            t.SetPixels32(px);
            t.Apply();
            return t;
        });
    }

    public static Sprite MarsCrater(int variant)
    {
        const int C = 32;
        return GetOrMake("mars_crater_" + variant, () =>
        {
            var t = Blank(C, C);
            var px = new Color32[C * C];

            var rimHi = new Color32(114, 54, 40, 225);
            var rimDark = new Color32(30, 12, 13, 225);
            var bowlShadow = new Color32(26, 11, 12, 235);
            var bowlFloor = new Color32(42, 18, 17, 220);

            float cx = 15.5f;
            float cy = 15.5f;
            float rx = variant == 0 ? 13.5f : 12.0f;
            float ry = variant == 0 ? 11.5f : 13.0f;

            for (int y = 0; y < C; y++)
            {
                for (int x = 0; x < C; x++)
                {
                    float nx = (x - cx) / rx;
                    float ny = (y - cy) / ry;
                    float d = Mathf.Sqrt(nx * nx + ny * ny);

                    if (d > 1.05f) continue;

                    if (d >= 0.84f)
                    {
                        bool sunlit = (nx + ny) > 0.1f;
                        px[y * C + x] = sunlit ? rimHi : rimDark;
                    }
                    else if (d >= 0.58f && ny > 0.05f)
                    {
                        px[y * C + x] = bowlShadow;
                    }
                    else
                    {
                        px[y * C + x] = bowlFloor;
                    }
                }
            }

            SetPx(px, C, C, 15, 14, rimHi);
            SetPx(px, C, C, 16, 14, rimDark);
            SetPx(px, C, C, 18, 16, rimHi);

            t.SetPixels32(px);
            t.Apply();
            return t;
        }, S);
    }

    // ---------------- Quake Ground Cracks, Dust Streaks, Meteorites & Rover Pad ----------------
    public static Sprite GroundCrack(int variant)
    {
        const int C = 32;
        return GetOrMake("quake_crack_" + variant, () =>
        {
            var t = Blank(C, C);
            var px = new Color32[C * C];
            var fissure = new Color32(14, 5, 6, 245);
            var rim = new Color32(118, 48, 36, 210);

            int x = variant == 0 ? 4 : 26;
            int stepX = variant == 0 ? 1 : -1;
            for (int y = 3; y <= 28; y++)
            {
                if (y % 4 == 0) x += stepX;
                if (y % 7 == 0) x -= stepX;
                SetPx(px, C, C, x - 1, y, rim);
                SetPx(px, C, C, x, y, fissure);
                SetPx(px, C, C, x + 1, y, fissure);
                SetPx(px, C, C, x + 2, y, rim);

                if (y == 14)
                {
                    for (int b = 1; b <= 6; b++)
                    {
                        SetPx(px, C, C, x + b * stepX, y + b / 2, fissure);
                        SetPx(px, C, C, x + b * stepX, y + b / 2 + 1, rim);
                    }
                }
            }

            t.SetPixels32(px);
            t.Apply();
            return t;
        }, S);
    }

    public static Sprite DustStreak()
    {
        const int W = 32, H = 6;
        return GetOrMake("red_dust_streak", () =>
        {
            var t = Blank(W, H);
            var px = new Color32[W * H];
            var streak = new Color32(245, 92, 62, 185);
            var core = new Color32(255, 145, 105, 225);

            FillRect(px, W, H, 2, 2, 29, 3, streak);
            FillRect(px, W, H, 8, 2, 23, 3, core);
            SetPx(px, W, H, 5, 1, streak);
            SetPx(px, W, H, 25, 4, streak);

            t.SetPixels32(px);
            t.Apply();
            return t;
        }, S);
    }

    // 3x3 High-Visibility Hazard Rover Landing Pad (48x48)
    public static Sprite RoverPad()
    {
        const int P = 48;
        return GetOrMake("rover_pad_3x3", () =>
        {
            var t = Blank(P, P);
            var px = new Color32[P * P];

            var dark = new Color32(24, 28, 38, 255);
            var asphalt = new Color32(44, 50, 64, 255);
            var yellow = new Color32(248, 198, 44, 255);
            var cyan = new Color32(90, 232, 255, 255);

            FillRect(px, P, P, 1, 1, P - 2, P - 2, dark);
            FillRect(px, P, P, 4, 4, P - 5, P - 5, asphalt);

            for (int i = 2; i < P - 2; i++)
            {
                bool isYellow = ((i / 4) % 2) == 0;
                var c = isYellow ? yellow : dark;
                SetPx(px, P, P, i, 2, c); SetPx(px, P, P, i, 3, c);
                SetPx(px, P, P, i, P - 3, c); SetPx(px, P, P, i, P - 4, c);
                SetPx(px, P, P, 2, i, c); SetPx(px, P, P, 3, i, c);
                SetPx(px, P, P, P - 3, i, c); SetPx(px, P, P, P - 4, i, c);
            }

            FillRect(px, P, P, 18, 14, 20, 33, yellow);
            FillRect(px, P, P, 20, 31, 28, 33, yellow);
            FillRect(px, P, P, 20, 23, 28, 25, yellow);
            FillRect(px, P, P, 27, 25, 29, 31, yellow);
            FillRect(px, P, P, 24, 14, 26, 22, yellow);
            FillRect(px, P, P, 27, 14, 29, 18, yellow);

            FillRect(px, P, P, 5, 5, 7, 7, cyan);
            FillRect(px, P, P, P - 8, 5, P - 6, 7, cyan);
            FillRect(px, P, P, 5, P - 8, 7, P - 6, cyan);
            FillRect(px, P, P, P - 8, P - 8, P - 6, P - 6, cyan);

            t.SetPixels32(px);
            t.Apply();
            return t;
        }, S);
    }

    // Falling blazing meteorite (24x24)
    public static Sprite FallingMeteor()
    {
        const int G = 24;
        return GetOrMake("falling_meteor", () =>
        {
            var t = Blank(G, G);
            var px = new Color32[G * G];
            var tail = new Color32(255, 95, 35, 210);
            var flame = new Color32(255, 195, 55, 245);
            var core = new Color32(255, 250, 210, 255);
            var rock = new Color32(72, 34, 28, 255);

            for (int i = 0; i < 12; i++)
            {
                FillRect(px, G, G, 8 + i, 8 + i, 12 + i, 12 + i, tail);
                if (i < 7) FillRect(px, G, G, 7 + i, 7 + i, 10 + i, 10 + i, flame);
            }
            FillRect(px, G, G, 4, 4, 11, 11, rock);
            FillRect(px, G, G, 5, 5, 9, 9, core);

            t.SetPixels32(px);
            t.Apply();
            return t;
        }, G);
    }

    // Landed Smoldering Meteorite with Glowing Resource Veins (24x24)
    public static Sprite MeteoriteSite()
    {
        const int G = 24;
        return GetOrMake("meteorite_site", () =>
        {
            var t = Blank(G, G);
            var px = new Color32[G * G];
            var scorch = new Color32(22, 10, 10, 210);
            var outline = new Color32(18, 12, 16, 255);
            var rockDark = new Color32(54, 32, 36, 255);
            var rockMid = new Color32(88, 52, 56, 255);
            var cyanGlow = new Color32(95, 242, 255, 255);
            var goldGlow = new Color32(255, 215, 75, 255);

            FillRect(px, G, G, 2, 2, 21, 8, scorch);
            FillRect(px, G, G, 5, 5, 18, 17, outline);
            FillRect(px, G, G, 6, 6, 17, 16, rockMid);
            FillRect(px, G, G, 11, 6, 17, 12, rockDark);

            FillRect(px, G, G, 8, 10, 10, 14, cyanGlow);
            FillRect(px, G, G, 12, 9, 15, 11, goldGlow);
            SetPx(px, G, G, 9, 18, cyanGlow);
            SetPx(px, G, G, 14, 19, goldGlow);

            t.SetPixels32(px);
            t.Apply();
            return t;
        }, G);
    }

    // ---------------- Grid Markers & Floors ----------------
    public static Sprite BuildZoneSlot()
    {
        return GetOrMake("build_slot", () =>
        {
            var t = Blank();
            var px = new Color32[S * S];
            var edge = new Color32(80, 150, 200, 45);
            for (int i = 0; i < S; i++)
            {
                SetPx(px, S, S, i, 0, edge);
                SetPx(px, S, S, i, S - 1, edge);
                SetPx(px, S, S, 0, i, edge);
                SetPx(px, S, S, S - 1, i, edge);
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        });
    }

    public static Sprite SolarZoneSlot()
    {
        return GetOrMake("solar_slot", () =>
        {
            var t = Blank();
            var px = new Color32[S * S];
            var edge = new Color32(255, 195, 65, 65);
            var dot = new Color32(255, 220, 95, 95);
            for (int i = 0; i < S; i++)
            {
                if (i % 2 == 0)
                {
                    SetPx(px, S, S, i, 0, edge);
                    SetPx(px, S, S, i, S - 1, edge);
                    SetPx(px, S, S, 0, i, edge);
                    SetPx(px, S, S, S - 1, i, edge);
                }
            }
            SetPx(px, S, S, 7, 7, dot); SetPx(px, S, S, 8, 8, dot);
            SetPx(px, S, S, 7, 8, dot); SetPx(px, S, S, 8, 7, dot);
            t.SetPixels32(px);
            t.Apply();
            return t;
        });
    }

    public static Sprite OutdoorSolarIndicator()
    {
        return GetOrMake("solar_outdoor_ind", () =>
        {
            var t = Blank();
            var px = new Color32[S * S];
            var corner = new Color32(255, 215, 70, 195);
            var tint = new Color32(255, 200, 60, 28);
            FillRect(px, S, S, 1, 1, 14, 14, tint);
            for (int i = 0; i < 4; i++)
            {
                SetPx(px, S, S, i, 0, corner); SetPx(px, S, S, 0, i, corner);
                SetPx(px, S, S, 15 - i, 0, corner); SetPx(px, S, S, 15, i, corner);
                SetPx(px, S, S, i, 15, corner); SetPx(px, S, S, 0, 15 - i, corner);
                SetPx(px, S, S, 15 - i, 15, corner); SetPx(px, S, S, 15, 15 - i, corner);
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        });
    }

    public static Sprite IndoorBlockedIndicator()
    {
        return GetOrMake("solar_indoor_block", () =>
        {
            var t = Blank();
            var px = new Color32[S * S];
            var fill = new Color32(255, 60, 60, 42);
            var cross = new Color32(255, 85, 85, 145);
            FillRect(px, S, S, 1, 1, 14, 14, fill);
            for (int i = 4; i <= 11; i++)
            {
                SetPx(px, S, S, i, i, cross);
                SetPx(px, S, S, i, 15 - i, cross);
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        });
    }

    public static Sprite DamagedOverlay()
    {
        return GetOrMake("damaged_overlay", () =>
        {
            var t = Blank();
            var px = new Color32[S * S];
            var red = new Color32(255, 60, 45, 195);
            var yellow = new Color32(255, 215, 60, 230);
            for (int i = 0; i < S; i++)
            {
                SetPx(px, S, S, i, 0, red); SetPx(px, S, S, i, 15, red);
                SetPx(px, S, S, 0, i, red); SetPx(px, S, S, 15, i, red);
            }
            FillRect(px, S, S, 7, 7, 8, 12, yellow);
            FillRect(px, S, S, 7, 4, 8, 5, yellow);
            t.SetPixels32(px);
            t.Apply();
            return t;
        });
    }

    public static Sprite AutoFloor()
    {
        return GetOrMake("auto_floor", () =>
        {
            var t = Blank();
            var px = new Color32[S * S];
            var plate = new Color32(34, 42, 60, 255);
            var seam = new Color32(22, 28, 42, 255);
            var rivet = new Color32(62, 76, 104, 255);

            FillRect(px, S, S, 0, 0, 15, 15, plate);
            for (int i = 0; i < S; i++)
            {
                SetPx(px, S, S, i, 0, seam);
                SetPx(px, S, S, 0, i, seam);
                SetPx(px, S, S, i, 8, seam);
                SetPx(px, S, S, 8, i, seam);
            }
            SetPx(px, S, S, 2, 2, rivet); SetPx(px, S, S, 10, 2, rivet);
            SetPx(px, S, S, 2, 10, rivet); SetPx(px, S, S, 10, 10, rivet);

            t.SetPixels32(px);
            t.Apply();
            return t;
        });
    }

    public static Sprite GhostTile()
    {
        return GetOrMake("ghost_tile", () =>
        {
            var t = Blank();
            var px = new Color32[S * S];
            var fill = new Color32(255, 255, 255, 70);
            var border = new Color32(255, 255, 255, 210);
            FillRect(px, S, S, 1, 1, 14, 14, fill);
            for (int i = 0; i < S; i++)
            {
                SetPx(px, S, S, i, 0, border); SetPx(px, S, S, i, 15, border);
                SetPx(px, S, S, 0, i, border); SetPx(px, S, S, 15, i, border);
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        });
    }

    public static Sprite DeleteGhostTile()
    {
        return GetOrMake("delete_ghost_tile", () =>
        {
            var t = Blank();
            var px = new Color32[S * S];
            var fill = new Color32(255, 70, 60, 85);
            var border = new Color32(255, 95, 85, 240);
            FillRect(px, S, S, 1, 1, 14, 14, fill);
            for (int i = 0; i < S; i++)
            {
                SetPx(px, S, S, i, 0, border); SetPx(px, S, S, i, 15, border);
                SetPx(px, S, S, 0, i, border); SetPx(px, S, S, 15, i, border);
                if (i >= 3 && i <= 12)
                {
                    SetPx(px, S, S, i, i, border);
                    SetPx(px, S, S, i, 15 - i, border);
                }
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        });
    }

    public static Sprite SolarGhostTile()
    {
        const int W = 32, H = 22;
        return GetOrMake("solar_ghost_2x1", () =>
        {
            var t = Blank(W, H);
            var px = new Color32[W * H];
            var fill = new Color32(255, 255, 255, 85);
            var border = new Color32(255, 255, 255, 230);

            for (int x = 0; x < W; x++)
            {
                SetPx(px, W, H, x, 0, border);
                SetPx(px, W, H, x, 15, border);
            }
            for (int y = 0; y < 16; y++)
            {
                SetPx(px, W, H, 0, y, border);
                SetPx(px, W, H, W - 1, y, border);
            }

            for (int y = 5; y <= 20; y++)
            {
                int skew = (y - 5) * 5 / 15;
                int x0 = 2 + skew;
                int x1 = 25 + skew;
                for (int x = x0; x <= x1; x++)
                {
                    bool edge = (y == 5 || y == 20 || x == x0 || x == x1);
                    SetPx(px, W, H, x, y, edge ? border : fill);
                }
            }

            t.SetPixels32(px);
            t.Apply();
            return t;
        }, S, new Vector2(0.5f, 8f / H));
    }

    // ---------------- Isometric 2x1 Solar Panel (32x22) ----------------
    public static Sprite IsometricSolarPanel()
    {
        const int W = 32, H = 22;
        return GetOrMake("iso_solar_2x1", () =>
        {
            var t = Blank(W, H);
            var px = new Color32[W * H];

            var shadow = new Color32(18, 8, 8, 150);
            var dark = new Color32(22, 28, 42, 255);
            var legDark = new Color32(72, 84, 108, 255);
            var legLight = new Color32(138, 156, 186, 255);
            var frameRim = new Color32(195, 212, 238, 255);
            var frameSide = new Color32(92, 108, 138, 255);
            var cellDeep = new Color32(22, 54, 132, 255);
            var cellMid = new Color32(34, 82, 182, 255);
            var cellGlint = new Color32(95, 175, 255, 255);
            var gridLine = new Color32(115, 228, 255, 255);

            FillRect(px, W, H, 3, 1, 28, 4, shadow);
            FillRect(px, W, H, 5, 0, 26, 5, shadow);

            FillRect(px, W, H, 8, 2, 10, 7, legDark);
            FillRect(px, W, H, 8, 2, 8, 7, legLight);
            FillRect(px, W, H, 21, 2, 23, 7, legDark);
            FillRect(px, W, H, 21, 2, 21, 7, legLight);

            FillRect(px, W, H, 2, 4, 25, 5, dark);
            FillRect(px, W, H, 3, 5, 24, 5, frameSide);

            for (int y = 6; y <= 20; y++)
            {
                int dy = y - 6;
                int skew = dy * 5 / 14;
                int x0 = 2 + skew;
                int x1 = 25 + skew;

                for (int x = x0; x <= x1; x++)
                {
                    int localX = x - x0;

                    if (y == 6 || y == 20 || x == x0 || x == x1)
                    {
                        SetPx(px, W, H, x, y, dark);
                        continue;
                    }
                    if (y == 19 || x == x0 + 1)
                    {
                        SetPx(px, W, H, x, y, frameRim);
                        continue;
                    }
                    if (y == 7 || x == x1 - 1)
                    {
                        SetPx(px, W, H, x, y, frameSide);
                        continue;
                    }

                    bool isBusbarV = (localX == 7 || localX == 12 || localX == 17);
                    bool isBusbarH = (dy == 5 || dy == 9);
                    if (isBusbarV || isBusbarH)
                    {
                        SetPx(px, W, H, x, y, gridLine);
                        continue;
                    }

                    int diag = localX + dy;
                    if (diag >= 16 && diag <= 18)
                        SetPx(px, W, H, x, y, cellGlint);
                    else if (dy > 7)
                        SetPx(px, W, H, x, y, cellMid);
                    else
                        SetPx(px, W, H, x, y, cellDeep);
                }
            }

            t.SetPixels32(px);
            t.Apply();
            return t;
        }, S, new Vector2(0.5f, 8f / H));
    }

    // ---------------- Light Auto-Tiling Metal Wall ----------------
    public static Sprite MetalWall(int mask)
    {
        return GetOrMake("wall_v2_" + mask, () =>
        {
            var t = Blank();
            var px = new Color32[S * S];

            var dark = new Color32(42, 52, 72, 255);
            var body = new Color32(142, 160, 190, 255);
            var hi = new Color32(204, 220, 244, 255);
            var glow = new Color32(115, 238, 255, 255);
            var glowCore = new Color32(235, 254, 255, 255);

            bool n = (mask & 1) != 0;
            bool e = (mask & 2) != 0;
            bool s = (mask & 4) != 0;
            bool w = (mask & 8) != 0;

            int x0 = w ? 0 : 2;
            int x1 = e ? 15 : 13;
            int y0 = s ? 0 : 2;
            int y1 = n ? 15 : 13;

            FillRect(px, S, S, x0, y0, x1, y1, dark);

            int ix0 = w ? 0 : 3;
            int ix1 = e ? 15 : 12;
            int iy0 = s ? 0 : 3;
            int iy1 = n ? 15 : 12;
            FillRect(px, S, S, ix0, iy0, ix1, iy1, body);
            if (!n) FillRect(px, S, S, ix0, iy1, ix1, iy1, hi);
            if (!w) FillRect(px, S, S, ix0, iy0, ix0, iy1, hi);

            if (!n && !e && !s && !w)
            {
                FillRect(px, S, S, 6, 6, 9, 9, glow);
                FillRect(px, S, S, 7, 7, 8, 8, glowCore);
            }
            else
            {
                FillRect(px, S, S, 7, 7, 8, 8, glowCore);
                if (n) { FillRect(px, S, S, 7, 8, 8, 15, glow); FillRect(px, S, S, 7, 8, 7, 15, glowCore); }
                if (s) { FillRect(px, S, S, 7, 0, 8, 7, glow);  FillRect(px, S, S, 7, 0, 7, 7, glowCore); }
                if (e) { FillRect(px, S, S, 8, 7, 15, 8, glow); FillRect(px, S, S, 8, 8, 15, 8, glowCore); }
                if (w) { FillRect(px, S, S, 0, 7, 7, 8, glow);  FillRect(px, S, S, 0, 8, 7, 8, glowCore); }
            }

            t.SetPixels32(px);
            t.Apply();
            return t;
        });
    }

    public static Sprite WallGlow()
    {
        return GetOrMake("wall_glow", () =>
        {
            const int G = 32;
            var t = Blank(G, G);
            var px = new Color32[G * G];
            float c = (G - 1) * 0.5f;
            for (int y = 0; y < G; y++)
            {
                for (int x = 0; x < G; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    if (d >= 1f) continue;
                    float a = Mathf.Pow(1f - d, 2.2f) * 0.24f;
                    px[y * G + x] = new Color(0.42f, 0.88f, 1f, a);
                }
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        });
    }

    // ---------------- Machines, Supplies & Distinct Decor Sprites ----------------
    public static Sprite ItemSprite(ItemId id)
    {
        if (id == ItemId.Wall) return MetalWall(10);
        if (id == ItemId.Solar) return IsometricSolarPanel();

        return GetOrMake("item_v2_" + id, () =>
        {
            var t = Blank();
            var px = new Color32[S * S];

            var dark = new Color32(18, 22, 34, 255);
            var steel = new Color32(130, 145, 175, 255);
            var white = new Color32(242, 245, 250, 255);
            var silver = new Color32(188, 198, 216, 255);
            var cyan = new Color32(90, 220, 255, 255);
            var green = new Color32(85, 235, 115, 255);
            var yellow = new Color32(255, 220, 80, 255);
            var orange = new Color32(255, 130, 60, 255);
            var red = new Color32(215, 48, 58, 255);
            var blue = new Color32(70, 150, 255, 255);
            var purple = new Color32(165, 78, 235, 255);
            var horn = new Color32(218, 172, 95, 255);
            var terracotta = new Color32(204, 102, 58, 255);

            switch (id)
            {
                case ItemId.Door:
                    FillRect(px, S, S, 0, 2, 15, 13, dark);
                    FillRect(px, S, S, 1, 3, 14, 12, new Color32(115, 132, 160, 255));
                    FillRect(px, S, S, 7, 3, 8, 12, dark);
                    FillRect(px, S, S, 3, 7, 5, 8, yellow);
                    FillRect(px, S, S, 10, 7, 12, 8, yellow);
                    break;

                case ItemId.Battery:
                    FillRect(px, S, S, 3, 1, 12, 13, dark);
                    FillRect(px, S, S, 6, 14, 9, 15, steel);
                    FillRect(px, S, S, 4, 2, 11, 12, steel);
                    FillRect(px, S, S, 5, 3, 10, 10, green);
                    break;

                case ItemId.Water:
                    FillRect(px, S, S, 3, 1, 12, 14, dark);
                    FillRect(px, S, S, 4, 2, 11, 13, steel);
                    FillRect(px, S, S, 5, 3, 10, 11, blue);
                    FillRect(px, S, S, 6, 9, 9, 10, cyan);
                    break;

                case ItemId.Grass:
                    FillRect(px, S, S, 2, 3, 13, 12, new Color32(120, 165, 50, 255));
                    FillRect(px, S, S, 3, 4, 12, 11, new Color32(165, 210, 70, 255));
                    FillRect(px, S, S, 5, 3, 5, 12, orange);
                    FillRect(px, S, S, 10, 3, 10, 12, orange);
                    break;

                case ItemId.Purifier:
                    FillRect(px, S, S, 2, 1, 13, 14, dark);
                    FillRect(px, S, S, 3, 2, 12, 13, steel);
                    FillRect(px, S, S, 4, 8, 11, 12, cyan);
                    FillRect(px, S, S, 5, 3, 10, 6, dark);
                    SetPx(px, S, S, 5, 13, green); SetPx(px, S, S, 7, 13, green);
                    break;

                case ItemId.Console:
                    FillRect(px, S, S, 1, 1, 14, 14, dark);
                    FillRect(px, S, S, 2, 2, 13, 13, new Color32(55, 70, 105, 255));
                    FillRect(px, S, S, 3, 7, 12, 12, new Color32(20, 45, 75, 255));
                    FillRect(px, S, S, 4, 8, 11, 11, green);
                    FillRect(px, S, S, 3, 3, 12, 5, dark);
                    SetPx(px, S, S, 4, 4, orange); SetPx(px, S, S, 6, 4, yellow); SetPx(px, S, S, 8, 4, cyan);
                    break;

                case ItemId.Tastifier:
                    FillRect(px, S, S, 2, 1, 13, 14, dark);
                    FillRect(px, S, S, 3, 2, 12, 13, orange);
                    FillRect(px, S, S, 5, 6, 10, 11, yellow);
                    FillRect(px, S, S, 5, 3, 10, 4, dark);
                    break;

                case ItemId.Plushie:
                    SetPx(px, S, S, 4, 14, horn); SetPx(px, S, S, 5, 13, horn); SetPx(px, S, S, 6, 12, horn);
                    SetPx(px, S, S, 11, 14, horn); SetPx(px, S, S, 10, 13, horn); SetPx(px, S, S, 9, 12, horn);
                    FillRect(px, S, S, 2, 9, 3, 10, silver);
                    FillRect(px, S, S, 12, 9, 13, 10, silver);
                    FillRect(px, S, S, 4, 7, 11, 12, dark);
                    FillRect(px, S, S, 5, 8, 10, 11, white);
                    FillRect(px, S, S, 4, 2, 11, 7, dark);
                    FillRect(px, S, S, 5, 3, 10, 6, white);
                    SetPx(px, S, S, 6, 10, dark); SetPx(px, S, S, 9, 10, dark);
                    FillRect(px, S, S, 6, 8, 9, 8, new Color32(245, 175, 185, 255));
                    SetPx(px, S, S, 7, 5, silver); SetPx(px, S, S, 8, 4, silver);
                    FillRect(px, S, S, 3, 1, 5, 3, silver);
                    FillRect(px, S, S, 10, 1, 12, 3, silver);
                    break;

                case ItemId.Fridge:
                    FillRect(px, S, S, 3, 1, 12, 14, dark);
                    FillRect(px, S, S, 4, 2, 11, 13, white);
                    FillRect(px, S, S, 10, 2, 11, 13, silver);
                    FillRect(px, S, S, 4, 9, 11, 9, dark);
                    FillRect(px, S, S, 5, 11, 5, 12, steel);
                    FillRect(px, S, S, 5, 4, 5, 7, steel);
                    SetPx(px, S, S, 9, 12, cyan);
                    break;

                case ItemId.Disco:
                    FillRect(px, S, S, 7, 13, 8, 15, steel);
                    FillRect(px, S, S, 4, 3, 11, 12, dark);
                    FillRect(px, S, S, 5, 4, 10, 11, silver);
                    FillRect(px, S, S, 3, 5, 12, 10, silver);
                    SetPx(px, S, S, 5, 9, white); SetPx(px, S, S, 7, 10, cyan);
                    SetPx(px, S, S, 9, 8, yellow); SetPx(px, S, S, 6, 6, cyan);
                    SetPx(px, S, S, 8, 5, white); SetPx(px, S, S, 10, 6, purple);
                    SetPx(px, S, S, 2, 11, yellow); SetPx(px, S, S, 13, 4, cyan);
                    break;

                case ItemId.Cactus:
                    FillRect(px, S, S, 4, 1, 11, 5, dark);
                    FillRect(px, S, S, 5, 2, 10, 4, terracotta);
                    FillRect(px, S, S, 4, 5, 11, 5, orange);
                    FillRect(px, S, S, 6, 6, 9, 13, green);
                    FillRect(px, S, S, 3, 8, 5, 10, green);
                    FillRect(px, S, S, 10, 7, 12, 11, green);
                    SetPx(px, S, S, 7, 14, new Color32(255, 115, 175, 255));
                    SetPx(px, S, S, 8, 14, yellow);
                    break;

                case ItemId.Rug:
                    FillRect(px, S, S, 1, 2, 14, 13, dark);
                    FillRect(px, S, S, 2, 3, 13, 12, red);
                    for (int x = 2; x <= 13; x++) { SetPx(px, S, S, x, 3, yellow); SetPx(px, S, S, x, 12, yellow); }
                    for (int y = 3; y <= 12; y += 2) { SetPx(px, S, S, 0, y, yellow); SetPx(px, S, S, 15, y, yellow); }
                    FillRect(px, S, S, 6, 6, 9, 9, yellow);
                    FillRect(px, S, S, 7, 7, 8, 8, blue);
                    break;

                case ItemId.Dish:
                    FillRect(px, S, S, 4, 1, 11, 2, steel);
                    FillRect(px, S, S, 7, 2, 8, 6, steel);
                    FillRect(px, S, S, 3, 6, 11, 12, white);
                    FillRect(px, S, S, 5, 7, 10, 11, silver);
                    SetPx(px, S, S, 11, 12, orange); SetPx(px, S, S, 12, 13, cyan);
                    SetPx(px, S, S, 14, 14, cyan);
                    break;

                case ItemId.Lava:
                    FillRect(px, S, S, 5, 1, 10, 3, steel);
                    FillRect(px, S, S, 6, 4, 9, 12, purple);
                    FillRect(px, S, S, 6, 13, 9, 14, silver);
                    FillRect(px, S, S, 7, 5, 8, 7, orange);
                    FillRect(px, S, S, 7, 9, 8, 11, yellow);
                    break;

                case ItemId.Throne:
                    FillRect(px, S, S, 3, 1, 12, 14, dark);
                    FillRect(px, S, S, 4, 2, 11, 13, yellow);
                    FillRect(px, S, S, 4, 12, 5, 14, orange);
                    FillRect(px, S, S, 10, 12, 11, 14, orange);
                    FillRect(px, S, S, 5, 4, 10, 6, red);
                    FillRect(px, S, S, 3, 5, 4, 9, orange);
                    FillRect(px, S, S, 11, 5, 12, 9, orange);
                    break;

                default:
                    FillRect(px, S, S, 3, 2, 12, 13, dark);
                    FillRect(px, S, S, 4, 3, 11, 12, purple);
                    break;
            }

            t.SetPixels32(px);
            t.Apply();
            return t;
        });
    }

    // ---------------- Mars Exploration Rover (24x24) ----------------
    public static Sprite RoverSprite(int dir, bool occupied)
    {
        const int G = 24;
        return GetOrMake("rover_" + dir + "_" + (occupied ? 1 : 0), () =>
        {
            var t = Blank(G, G);
            var px = new Color32[G * G];

            var dark = new Color32(18, 22, 32, 255);
            var wheel = new Color32(45, 50, 62, 255);
            var hub = new Color32(150, 165, 190, 255);
            var chassis = new Color32(235, 118, 48, 255);
            var trim = new Color32(245, 242, 232, 255);
            var glass = new Color32(95, 228, 255, 255);
            var headlight = new Color32(255, 240, 120, 255);

            if (dir == 2 || dir == 3)
            {
                bool right = dir == 3;
                System.Func<int, int> fx = x => right ? x : (G - 1 - x);

                int[] wx = { 4, 11, 18 };
                foreach (int cx in wx)
                {
                    FillRect(px, G, G, fx(cx - 2), 2, fx(cx + 2), 6, dark);
                    FillRect(px, G, G, fx(cx - 1), 3, fx(cx + 1), 5, wheel);
                    SetPx(px, G, G, fx(cx), 4, hub);
                }

                FillRect(px, G, G, fx(2), 6, fx(21), 12, dark);
                FillRect(px, G, G, fx(3), 7, fx(20), 11, chassis);
                FillRect(px, G, G, fx(4), 11, fx(16), 12, trim);

                FillRect(px, G, G, fx(12), 12, fx(18), 16, glass);
                FillRect(px, G, G, fx(7), 16, fx(17), 17, trim);

                FillRect(px, G, G, fx(4), 12, fx(4), 20, trim);
                SetPx(px, G, G, fx(4), 21, headlight);
                FillRect(px, G, G, fx(20), 8, fx(21), 10, headlight);

                if (occupied)
                {
                    FillRect(px, G, G, fx(9), 12, fx(12), 15, trim);
                    SetPx(px, G, G, fx(10), 16, headlight);
                }
            }
            else
            {
                FillRect(px, G, G, 2, 2, 5, 8, wheel);
                FillRect(px, G, G, 18, 2, 21, 8, wheel);
                FillRect(px, G, G, 5, 5, 18, 13, dark);
                FillRect(px, G, G, 6, 6, 17, 12, chassis);
                FillRect(px, G, G, 7, 12, 16, 16, glass);
                FillRect(px, G, G, 6, 16, 17, 17, trim);
                FillRect(px, G, G, 6, 8, 7, 10, headlight);
                FillRect(px, G, G, 16, 8, 17, 10, headlight);
                if (occupied)
                {
                    FillRect(px, G, G, 10, 12, 13, 15, trim);
                }
            }

            t.SetPixels32(px);
            t.Apply();
            return t;
        }, G);
    }

    // ---------------- Astronaut Goat (24x24, dir: 0=Down, 1=Up, 2=Left, 3=Right) ----------------
    public static Sprite Goat(int dir, int step)
    {
        const int G = 24;
        return GetOrMake("goat24_v2_" + dir + "_" + step, () =>
        {
            var t = Blank(G, G);
            var px = new Color32[G * G];

            var outline = new Color32(20, 20, 32, 255);
            var fur = new Color32(246, 244, 236, 255);
            var shade = new Color32(202, 198, 188, 255);
            var muzzle = new Color32(235, 205, 195, 255);
            var horn = new Color32(218, 172, 95, 255);
            var hornDark = new Color32(165, 122, 58, 255);
            var hoof = new Color32(45, 45, 58, 255);
            var visor = new Color32(95, 228, 255, 255);
            var visorHi = new Color32(210, 252, 255, 255);
            var pack = new Color32(232, 98, 52, 255);

            int bob = step == 1 ? 1 : 0;
            int stride = step == 1 ? 2 : 0;

            if (dir == 2 || dir == 3)
            {
                bool right = dir == 3;
                System.Func<int, int> fx = x => right ? x : (G - 1 - x);

                FillRect(px, G, G, fx(3), 10 + bob, fx(5), 13 + bob, fur);
                SetPx(px, G, G, fx(3), 13 + bob, shade);

                FillRect(px, G, G, fx(5), 5 + bob, fx(15), 11 + bob, outline);
                FillRect(px, G, G, fx(6), 6 + bob, fx(14), 10 + bob, fur);
                FillRect(px, G, G, fx(6), 6 + bob, fx(14), 7 + bob, shade);
                FillRect(px, G, G, fx(7), 9 + bob, fx(11), 12 + bob, pack);

                int l1 = 6 + stride;
                int l2 = 13 - stride;
                FillRect(px, G, G, fx(l1), 2, fx(l1 + 1), 5 + bob, fur);
                FillRect(px, G, G, fx(l1), 1, fx(l1 + 1), 1, hoof);
                FillRect(px, G, G, fx(l2), 2, fx(l2 + 1), 5 + bob, fur);
                FillRect(px, G, G, fx(l2), 1, fx(l2 + 1), 1, hoof);

                FillRect(px, G, G, fx(12), 10 + bob, fx(16), 14 + bob, fur);
                FillRect(px, G, G, fx(13), 12 + bob, fx(19), 18 + bob, outline);
                FillRect(px, G, G, fx(14), 13 + bob, fx(18), 17 + bob, fur);
                FillRect(px, G, G, fx(18), 12 + bob, fx(21), 15 + bob, muzzle);
                SetPx(px, G, G, fx(21), 14 + bob, outline);

                FillRect(px, G, G, fx(18), 9 + bob, fx(19), 11 + bob, fur);
                SetPx(px, G, G, fx(19), 8 + bob, shade);

                FillRect(px, G, G, fx(15), 14 + bob, fx(18), 16 + bob, visor);
                SetPx(px, G, G, fx(16), 16 + bob, visorHi);

                FillRect(px, G, G, fx(11), 15 + bob, fx(13), 16 + bob, shade);

                FillRect(px, G, G, fx(14), 18 + bob, fx(15), 20 + bob, horn);
                FillRect(px, G, G, fx(12), 20 + bob, fx(14), 21 + bob, horn);
                FillRect(px, G, G, fx(10), 21 + bob, fx(12), 22 + bob, hornDark);
            }
            else
            {
                FillRect(px, G, G, 3, 14 + bob, 5, 16 + bob, shade);
                FillRect(px, G, G, 18, 14 + bob, 20, 16 + bob, shade);

                FillRect(px, G, G, 7, 18 + bob, 8, 20 + bob, horn);
                FillRect(px, G, G, 5, 20 + bob, 7, 21 + bob, horn);
                FillRect(px, G, G, 4, 21 + bob, 5, 22 + bob, hornDark);

                FillRect(px, G, G, 15, 18 + bob, 16, 20 + bob, horn);
                FillRect(px, G, G, 16, 20 + bob, 18, 21 + bob, horn);
                FillRect(px, G, G, 18, 21 + bob, 19, 22 + bob, hornDark);

                FillRect(px, G, G, 6, 11 + bob, 17, 18 + bob, outline);
                FillRect(px, G, G, 7, 12 + bob, 16, 17 + bob, fur);

                FillRect(px, G, G, 7, 4 + bob, 16, 11 + bob, outline);
                FillRect(px, G, G, 8, 5 + bob, 15, 10 + bob, fur);

                int leftY = step == 1 ? 2 : 1;
                int rightY = step == 1 ? 1 : 2;
                FillRect(px, G, G, 8, leftY + 1, 10, 4 + bob, fur);
                FillRect(px, G, G, 8, leftY, 10, leftY, hoof);
                FillRect(px, G, G, 13, rightY + 1, 15, 4 + bob, fur);
                FillRect(px, G, G, 13, rightY, 15, rightY, hoof);

                if (dir == 0)
                {
                    FillRect(px, G, G, 8, 14 + bob, 15, 16 + bob, visor);
                    FillRect(px, G, G, 9, 16 + bob, 11, 16 + bob, visorHi);
                    FillRect(px, G, G, 9, 12 + bob, 14, 13 + bob, muzzle);
                    FillRect(px, G, G, 11, 9 + bob, 12, 11 + bob, fur);
                    SetPx(px, G, G, 11, 8 + bob, shade);
                }
                else
                {
                    FillRect(px, G, G, 9, 7 + bob, 14, 11 + bob, pack);
                    FillRect(px, G, G, 11, 5 + bob, 12, 7 + bob, fur);
                }
            }

            t.SetPixels32(px);
            t.Apply();
            return t;
        }, G);
    }

    // ---------------- Food Tastifier Aroma & Sparkle Visuals ----------------
    public static Sprite TastifierAura()
    {
        const int G = 32;
        return GetOrMake("tastifier_aura", () =>
        {
            var t = Blank(G, G);
            var px = new Color32[G * G];
            float c = (G - 1) * 0.5f;
            for (int y = 0; y < G; y++)
            {
                for (int x = 0; x < G; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    if (d >= 1f) continue;
                    float ring = Mathf.Max(0f, 1f - Mathf.Abs(d - 0.65f) * 3.5f);
                    float a = (Mathf.Pow(1f - d, 2.0f) * 0.22f) + (ring * 0.25f);
                    px[y * G + x] = new Color(1f, 0.78f, 0.22f, Mathf.Clamp01(a));
                }
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        });
    }

    public static Sprite FlavorSparkle()
    {
        const int G = 16;
        return GetOrMake("flavor_sparkle", () =>
        {
            var t = Blank(G, G);
            var px = new Color32[G * G];
            var gold = new Color32(255, 225, 75, 255);
            var pink = new Color32(255, 115, 195, 255);
            var white = new Color32(255, 255, 240, 255);

            FillRect(px, G, G, 7, 4, 8, 11, gold);
            FillRect(px, G, G, 4, 7, 11, 8, gold);
            FillRect(px, G, G, 7, 7, 8, 8, white);

            SetPx(px, G, G, 3, 12, pink); SetPx(px, G, G, 4, 12, gold);
            SetPx(px, G, G, 12, 3, pink); SetPx(px, G, G, 11, 3, gold);
            SetPx(px, G, G, 12, 12, white);

            t.SetPixels32(px);
            t.Apply();
            return t;
        });
    }
    static Sprite cachedGoatLandingPod;

    public static Sprite GoatLandingPod()
    {
        if (cachedGoatLandingPod != null) return cachedGoatLandingPod;

        const int W = 80;
        const int H = 80;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        var px = new Color[W * H];

        void Set(int x, int y, Color c)
        {
            if (x >= 0 && x < W && y >= 0 && y < H)
                px[y * W + x] = c;
        }

        void Rect(int x0, int y0, int x1, int y1, Color c)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    Set(x, y, c);
        }

        void Ellipse(float cx, float cy, float rx, float ry, Color c)
        {
            int minX = Mathf.FloorToInt(cx - rx);
            int maxX = Mathf.CeilToInt(cx + rx);
            int minY = Mathf.FloorToInt(cy - ry);
            int maxY = Mathf.CeilToInt(cy + ry);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = (x - cx) / rx;
                    float dy = (y - cy) / ry;
                    if (dx * dx + dy * dy <= 1f)
                        Set(x, y, c);
                }
            }
        }

        Color outline   = new Color(0.12f, 0.14f, 0.20f, 1f);
        Color scorch    = new Color(0.18f, 0.11f, 0.10f, 0.55f);
        Color legDark   = new Color(0.28f, 0.32f, 0.40f, 1f);
        Color legLight  = new Color(0.55f, 0.60f, 0.70f, 1f);
        Color goldPad   = new Color(0.96f, 0.76f, 0.20f, 1f);
        Color hullBase  = new Color(0.92f, 0.94f, 0.97f, 1f);
        Color hullShade = new Color(0.72f, 0.76f, 0.84f, 1f);
        Color hullDark  = new Color(0.52f, 0.57f, 0.66f, 1f);
        Color orange    = new Color(0.95f, 0.48f, 0.14f, 1f);
        Color glassDark = new Color(0.10f, 0.30f, 0.52f, 1f);
        Color glassBlue = new Color(0.22f, 0.68f, 0.94f, 1f);
        Color glassGlow = new Color(0.75f, 0.94f, 1.00f, 1f);
        Color hornLight = new Color(0.94f, 0.84f, 0.64f, 1f);
        Color hornRidge = new Color(0.70f, 0.57f, 0.38f, 1f);
        Color earPink   = new Color(0.94f, 0.62f, 0.68f, 1f);
        Color beaconRed = new Color(1.00f, 0.22f, 0.22f, 1f);

        // 1. Ground scorch ring underneath the lander
        Ellipse(40f, 7f, 34f, 5f, scorch);

        // 2. Center main thruster bell
        Rect(31, 9, 48, 18, outline);
        Rect(33, 10, 46, 18, legDark);
        Rect(36, 10, 43, 17, legLight);
        Rect(34, 8, 45, 9, glassBlue);

        // 3. Four heavy splayed hydraulic landing legs + gold shock pads
        for (int i = 0; i < 11; i++)
        {
            // Outer left leg
            Rect(11 + i, 7 + i, 15 + i, 8 + i, outline);
            Rect(12 + i, 7 + i, 14 + i, 8 + i, legLight);
            // Inner left strut
            Rect(23 + (i / 2), 9 + i, 26 + (i / 2), 10 + i, legDark);

            // Outer right leg
            Rect(64 - i, 7 + i, 68 - i, 8 + i, outline);
            Rect(65 - i, 7 + i, 67 - i, 8 + i, legLight);
            // Inner right strut
            Rect(53 - (i / 2), 9 + i, 56 - (i / 2), 10 + i, legDark);
        }
        Rect(8, 5, 17, 7, outline);
        Rect(9, 6, 16, 7, goldPad);
        Rect(20, 7, 27, 9, outline);
        Rect(21, 8, 26, 9, goldPad);
        Rect(52, 7, 59, 9, outline);
        Rect(53, 8, 58, 9, goldPad);
        Rect(62, 5, 71, 7, outline);
        Rect(63, 6, 70, 7, goldPad);

        // 4. Main pressurized capsule hull
        Rect(18, 18, 61, 56, outline);
        Rect(20, 57, 59, 59, outline);
        Rect(19, 19, 60, 56, hullBase);
        Rect(21, 57, 58, 58, hullBase);

        // Left/right cylindrical shading & bottom heat-shield skirt
        Rect(19, 19, 23, 55, hullShade);
        Rect(56, 19, 60, 55, hullShade);
        Rect(19, 19, 60, 22, hullDark);
        Rect(20, 20, 59, 21, legDark);

        // Horizontal panel seams & orange mission stripe
        Rect(19, 39, 60, 39, hullDark);
        Rect(19, 36, 60, 38, orange);
        Rect(25, 23, 25, 55, hullShade);
        Rect(54, 23, 54, 55, hullShade);

        // 5. Boarding airlock hatch + golden goat-horn crest
        Rect(32, 22, 47, 34, outline);
        Rect(33, 23, 46, 33, hullShade);
        Rect(35, 24, 44, 32, legDark);
        // Mini golden goat crest on hatch
        Rect(38, 26, 41, 29, goldPad);
        Set(37, 30, goldPad);
        Set(42, 30, goldPad);

        // 6. Goat-head panoramic cockpit viewport
        Rect(28, 41, 51, 53, outline);
        Rect(29, 42, 50, 52, glassDark);
        Rect(30, 43, 49, 51, glassBlue);
        // Specular reflection + horizontal goat-visor accent
        Rect(31, 49, 35, 50, glassGlow);
        Rect(34, 46, 38, 47, glassDark);
        Rect(41, 46, 45, 47, glassDark);

        // 7. Perky goat ears below the horns
        Rect(11, 44, 18, 49, outline);
        Rect(12, 45, 18, 48, hullBase);
        Rect(13, 46, 17, 47, earPink);

        Rect(61, 44, 68, 49, outline);
        Rect(61, 45, 67, 48, hullBase);
        Rect(62, 46, 66, 47, earPink);

        // 8. Giant curved ram/goat horns on upper left & right flanks
        Vector2[] hornPath =
        {
            new Vector2(19, 51), new Vector2(15, 52), new Vector2(11, 54),
            new Vector2(8, 57),  new Vector2(6, 61),  new Vector2(6, 65),
            new Vector2(8, 69),  new Vector2(11, 71), new Vector2(15, 72)
        };
        float[] hornRadii = { 4.2f, 4.0f, 3.8f, 3.5f, 3.2f, 2.8f, 2.4f, 1.9f, 1.4f };

        for (int s = 0; s < hornPath.Length; s++)
        {
            float lx = hornPath[s].x;
            float rx = 79f - lx;
            float hy = hornPath[s].y;
            float r = hornRadii[s];

            Ellipse(lx, hy, r + 1.1f, r + 1.1f, outline);
            Ellipse(rx, hy, r + 1.1f, r + 1.1f, outline);
        }
        for (int s = 0; s < hornPath.Length; s++)
        {
            float lx = hornPath[s].x;
            float rx = 79f - lx;
            float hy = hornPath[s].y;
            float r = hornRadii[s];
            Color fill = (s % 2 == 1) ? hornRidge : hornLight;

            Ellipse(lx, hy, r, r, fill);
            Ellipse(rx, hy, r, r, fill);
        }

        // 9. Upper nose cone & blinking beacon spire
        Rect(25, 59, 54, 62, outline);
        Rect(26, 59, 53, 61, orange);
        Rect(30, 63, 49, 66, outline);
        Rect(31, 63, 48, 65, hullShade);
        Rect(35, 67, 44, 69, outline);
        Rect(36, 67, 43, 68, orange);

        Rect(39, 70, 40, 75, outline);
        Rect(38, 75, 41, 78, outline);
        Rect(39, 76, 40, 77, beaconRed);

        tex.SetPixels(px);
        tex.Apply();

        cachedGoatLandingPod = Sprite.Create(tex, new Rect(0f, 0f, W, H), new Vector2(0.5f, 0.5f), 16f);
        return cachedGoatLandingPod;
    }
}