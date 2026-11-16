using UnityEngine;

// Generates all placeholder art in code (no image files needed).
public static class PixelArt
{
    static Texture2D NewTex(int w, int h, bool repeat = false)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Point;
        t.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        return t;
    }

    public static Sprite ToSprite(Texture2D t, float ppu = 100f)
    {
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), ppu);
    }

    // ---------------- Stars ----------------
    public static Texture2D StarLayer(int w, int h, int count, int seed, bool withBackground)
    {
        var tex = NewTex(w, h, true);
        var px = new Color32[w * h];

        if (withBackground)
        {
            for (int y = 0; y < h; y++)
            {
                float k = y / (float)h;
                var bg = new Color32(
                    (byte)Mathf.Lerp(6, 24, k),
                    (byte)Mathf.Lerp(6, 12, k),
                    (byte)Mathf.Lerp(24, 58, k),
                    255);
                for (int x = 0; x < w; x++) px[y * w + x] = bg;
            }
        }

        var rnd = new System.Random(seed);
        for (int i = 0; i < count; i++)
        {
            int x = rnd.Next(w);
            int y = rnd.Next(h);
            byte v = (byte)rnd.Next(130, 256);
            Color32 c = rnd.Next(5) == 0 ? new Color32(255, 214, 170, 255) : new Color32(v, v, 255, 255);
            px[y * w + x] = c;

            if (rnd.Next(10) == 0)
            {
                var dim = new Color32((byte)(c.r / 2), (byte)(c.g / 2), (byte)(c.b / 2), 200);
                px[y * w + (x + 1) % w] = dim;
                px[y * w + (x + w - 1) % w] = dim;
                px[((y + 1) % h) * w + x] = dim;
                px[((y + h - 1) % h) * w + x] = dim;
            }
        }

        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    // ---------------- Mars ----------------
    static Color32 QuantMars(float v)
    {
        if (v < 0.32f) return new Color32(70, 28, 30, 255);
        if (v < 0.50f) return new Color32(120, 48, 36, 255);
        if (v < 0.68f) return new Color32(172, 72, 44, 255);
        if (v < 0.85f) return new Color32(208, 104, 58, 255);
        return new Color32(232, 136, 78, 255);
    }

    public static Sprite Mars(int size, int seed)
    {
        var tex = NewTex(size, size);
        var px = new Color32[size * size];
        float c = (size - 1) / 2f;
        float rad = size / 2f - 0.5f;
        var rnd = new System.Random(seed);

        const int nc = 10;
        var cx = new float[nc];
        var cy = new float[nc];
        var cr = new float[nc];
        for (int i = 0; i < nc; i++)
        {
            float ang = (float)rnd.NextDouble() * Mathf.PI * 2f;
            float dist = Mathf.Sqrt((float)rnd.NextDouble()) * rad * 0.75f;
            cx[i] = c + Mathf.Cos(ang) * dist;
            cy[i] = c + Mathf.Sin(ang) * dist;
            cr[i] = 2f + (float)rnd.NextDouble() * size * 0.06f;
        }

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - c;
                float dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > rad) continue;

                float light = Mathf.Clamp01(0.5f + 0.5f * ((-dx + dy) / (rad * 1.4f)));
                float v = 0.25f + 0.75f * light;
                v += Mathf.Sin(y * 0.45f + Mathf.Sin(x * 0.2f) * 2f) * 0.06f;

                for (int i = 0; i < nc; i++)
                {
                    float ddx = x - cx[i];
                    float ddy = y - cy[i];
                    float cd = Mathf.Sqrt(ddx * ddx + ddy * ddy);
                    if (cd < cr[i]) v -= 0.14f;
                    else if (cd < cr[i] + 1.2f) v += 0.08f;
                }

                Color32 col = QuantMars(v);
                if (y > c + rad * 0.88f) col = new Color32(235, 225, 225, 255); // polar cap
                if (d > rad - 1.5f) col = Color32.Lerp(col, new Color32(255, 150, 100, 255), 0.5f); // thin atmosphere
                px[y * size + x] = col;
            }
        }

        tex.SetPixels32(px);
        tex.Apply();
        return ToSprite(tex, 100f);
    }

    // ---------------- Rocket (points right) ----------------
    public static Sprite Rocket()
    {
        const int W = 28, H = 13;
        var tex = NewTex(W, H);
        var px = new Color32[W * H];

        var white = new Color32(235, 240, 250, 255);
        var grey = new Color32(160, 170, 190, 255);
        var red = new Color32(220, 60, 60, 255);
        var dark = new Color32(70, 76, 96, 255);
        var glass = new Color32(90, 200, 255, 255);
        var flameO = new Color32(255, 140, 40, 255);
        var flameY = new Color32(255, 230, 90, 255);

        for (int y = 0; y < H; y++)
        {
            int dy = Mathf.Abs(y - 6);
            for (int x = 0; x < W; x++)
            {
                Color32 c = new Color32(0, 0, 0, 0);
                if (x <= 5 && dy <= (x + 2) / 3) c = x >= 3 ? flameY : flameO;
                else if (x >= 6 && x <= 7 && dy <= 2) c = dark;
                else if (x >= 8 && x <= 21 && dy <= 3) c = dy == 3 ? grey : white;
                else if (x >= 22 && dy <= 3 - (x - 21) / 2) c = red;

                if (x >= 8 && x <= 12 && dy == 4) c = red;   // fins
                if (x >= 8 && x <= 10 && dy == 5) c = red;
                if (x >= 15 && x <= 17 && dy <= 1) c = glass; // window

                px[y * W + x] = c;
            }
        }

        tex.SetPixels32(px);
        tex.Apply();
        return ToSprite(tex, 100f);
    }

    // ---------------- Space-themed dialogue panel ----------------
    public static Texture2D Panel(int w, int h, int seed)
    {
        var tex = NewTex(w, h);
        var px = new Color32[w * h];
        var rnd = new System.Random(seed);

        var dark = new Color32(6, 6, 20, 255);
        var edge = new Color32(110, 200, 255, 255);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int dx = Mathf.Min(x, w - 1 - x);
                int dy = Mathf.Min(y, h - 1 - y);
                int d = Mathf.Min(dx, dy);

                if (dx < 2 && dy < 2 && (dx + dy) < 2)
                {
                    px[y * w + x] = new Color32(0, 0, 0, 0); // rounded corner
                    continue;
                }

                if (d == 0) px[y * w + x] = dark;
                else if (d == 1) px[y * w + x] = edge;
                else if (d == 2) px[y * w + x] = dark;
                else
                {
                    float k = y / (float)h;
                    px[y * w + x] = new Color32(
                        (byte)Mathf.Lerp(10, 24, k),
                        (byte)Mathf.Lerp(12, 20, k),
                        (byte)Mathf.Lerp(46, 92, k),
                        255);
                }
            }
        }

        // a few dim stars inside the box
        int stars = (w * h) / (w * h > 8000 ? 300 : 60);
        for (int i = 0; i < stars; i++)
        {
            int x = rnd.Next(4, Mathf.Max(5, w - 4));
            int y = rnd.Next(4, Mathf.Max(5, h - 4));
            px[y * w + x] = new Color32(90, 100, 170, 255);
        }

        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }
}
