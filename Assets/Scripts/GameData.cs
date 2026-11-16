using System.Collections.Generic;
using UnityEngine;

public enum ItemId
{
    Wall = 0,
    Door = 1,
    Solar = 2,
    Battery = 3,
    Purifier = 4,
    Console = 5,
    Water = 6,
    Grass = 7,
    Glue = 8,
    Rover = 9,
    Shield = 10,
    Tastifier = 11,
    Plushie = 12,
    Fridge = 13,
    Disco = 14,
    Cactus = 15,
    Rug = 16,
    Dish = 17,
    Lava = 18,
    Throne = 19
}

public class ItemDef
{
    public ItemId id;
    public string name;
    public string kind;
    public string desc;
    public float weight;
    public int min;
    public int max;

    public bool Required => min > 0;
}

public static class GameData
{
    public const float WeightLimit = 40f;

    // Order matches ItemId enum indices 0..19 exactly
    public static readonly ItemDef[] Items =
    {
        new ItemDef { id = ItemId.Wall,      name = "Metal Wall",           kind = "STRUCTURE",    desc = "Pressurized alloy wall for sealing your habitat.",                weight = 0.1f, min = 80, max = 120 },
        new ItemDef { id = ItemId.Door,      name = "Airlock Door",         kind = "STRUCTURE",    desc = "Maintains cabin pressure while letting your goat pass.",          weight = 1.0f, min = 1,  max = 4   },
        new ItemDef { id = ItemId.Solar,     name = "Solar Panel (2x1)",    kind = "POWER",        desc = "Outdoor 2x1 array that generates power from Martian sunlight.",   weight = 1.2f, min = 4,  max = 10  },
        new ItemDef { id = ItemId.Battery,   name = "Power Battery",        kind = "POWER",        desc = "Stores electrical charge for life support and night cycles.",     weight = 1.5f, min = 2,  max = 8   },
        new ItemDef { id = ItemId.Purifier,  name = "Air Purifier",         kind = "LIFE SUPPORT", desc = "Place against a wall to scrub CO2 and generate oxygen.",          weight = 2.0f, min = 1,  max = 4   },
        new ItemDef { id = ItemId.Console,   name = "Command Console",      kind = "LIFE SUPPORT", desc = "Place against a wall to monitor seal and launch survival.",       weight = 1.5f, min = 1,  max = 2   },
        new ItemDef { id = ItemId.Water,     name = "Water Module",         kind = "SUPPLIES",     desc = "Purified hydration supply (+38% H2O when consumed).",              weight = 0.5f, min = 8,  max = 16  },
        new ItemDef { id = ItemId.Grass,     name = "Grass Bale",           kind = "SUPPLIES",     desc = "Nutritious Martian fodder (+28% Food, +45% with Tastifier).",     weight = 0.4f, min = 10, max = 20  },
        new ItemDef { id = ItemId.Glue,      name = "Super Goat-Glue 4000", kind = "GEAR",         desc = "REQUIRED: Hold in hotbar and press [R] to patch damaged tiles.",  weight = 0.5f, min = 1,  max = 3   },
        new ItemDef { id = ItemId.Rover,     name = "Mars Rover",           kind = "VEHICLE",      desc = "Fast surface rover with hull shielding for meteorite runs.",      weight = 3.2f, min = 1,  max = 1   },
        new ItemDef { id = ItemId.Shield,    name = "Radiation Shield",     kind = "GEAR",         desc = "Hold in survival hotbar to block 90% of outdoor radiation.",      weight = 1.2f, min = 0,  max = 2   },
        new ItemDef { id = ItemId.Tastifier, name = "Food Tastifier",       kind = "UPGRADE",      desc = "Place against a wall to boost all meals by +60%!",                weight = 1.5f, min = 0,  max = 2   },
        new ItemDef { id = ItemId.Plushie,   name = "Goat Plushie",         kind = "COMFORT",      desc = "Emotional support plushie for lonely Martian nights.",            weight = 0.2f, min = 0,  max = 3   },
        new ItemDef { id = ItemId.Fridge,    name = "Mini Fridge",          kind = "COMFORT",      desc = "Keeps Martian grass crisp and refreshing.",                       weight = 2.5f, min = 0,  max = 2   },
        new ItemDef { id = ItemId.Disco,     name = "Disco Ball",           kind = "COMFORT",      desc = "Essential zero-gravity dance floor lighting.",                    weight = 0.8f, min = 0,  max = 2   },
        new ItemDef { id = ItemId.Cactus,    name = "Potted Cactus",        kind = "COMFORT",      desc = "Spiky green companion that reminds you of Earth.",                weight = 0.5f, min = 0,  max = 4   },
        new ItemDef { id = ItemId.Rug,       name = "Cozy Rug",             kind = "COMFORT",      desc = "Ties the pressurized habitat room together.",                     weight = 0.6f, min = 0,  max = 4   },
        new ItemDef { id = ItemId.Dish,      name = "Satellite Dish",       kind = "COMFORT",      desc = "Picks up interstellar goat radio broadcasts.",                     weight = 2.0f, min = 0,  max = 2   },
        new ItemDef { id = ItemId.Lava,      name = "Lava Lamp",            kind = "COMFORT",      desc = "Hypnotic retro glow for your Martian quarters.",                  weight = 0.4f, min = 0,  max = 3   },
        new ItemDef { id = ItemId.Throne,    name = "Golden Throne",        kind = "COMFORT",      desc = "Fit for the undisputed Ruler of the Red Planet.",                 weight = 4.5f, min = 0,  max = 1   }
    };

    public static readonly int[] Qty = new int[Items.Length];

    public static ItemDef Get(ItemId id)
    {
        int idx = (int)id;
        if (idx >= 0 && idx < Items.Length)
            return Items[idx];
        return Items[0];
    }

    public static int Count(ItemId id)
    {
        int idx = (int)id;
        if (idx >= 0 && idx < Qty.Length)
            return Qty[idx];
        return 0;
    }

    public static float TotalWeight()
    {
        float total = 0f;
        for (int i = 0; i < Items.Length; i++)
            total += Qty[i] * Items[i].weight;
        return total;
    }

    public static bool Fits()
    {
        return TotalWeight() <= WeightLimit + 0.001f;
    }

    public static bool MeetsMinimums()
    {
        for (int i = 0; i < Items.Length; i++)
            if (Qty[i] < Items[i].min)
                return false;
        return true;
    }

    public static string Fmt(float t)
    {
        return t.ToString("0.#");
    }

    public static string MissingSummary(int maxShown = 4)
    {
        var missing = new List<string>();
        for (int i = 0; i < Items.Length; i++)
        {
            int shortBy = Items[i].min - Qty[i];
            if (shortBy > 0)
                missing.Add(shortBy + "x " + Items[i].name);
        }

        if (missing.Count == 0) return "None";
        if (maxShown <= 0 || missing.Count <= maxShown) return string.Join(", ", missing);

        return string.Join(", ", missing.GetRange(0, maxShown)) + " (+" + (missing.Count - maxShown) + " more)";
    }

    public static void MinKit()
    {
        for (int i = 0; i < Items.Length; i++)
            Qty[i] = Items[i].min;
    }

    public static void Clear()
    {
        for (int i = 0; i < Items.Length; i++)
            Qty[i] = 0;
    }
}