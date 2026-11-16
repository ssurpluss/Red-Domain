using UnityEngine;

// Generates simple retro beeps and survival SFX in code (no audio files needed).
public static class SoundFX
{
    static AudioSource src;
    static bool ready;

    public static AudioClip Blip, Move, Confirm, Back, Alarm, Airlock, Eat, Drink, Repair, Scavenge, Storm;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        ready = false;
        src = null;
    }

    public static void Init(GameObject host)
    {
        if (ready && src != null) return;

        src = host.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;

        Blip = Sequence("blip", new[] { 620f }, 0.04f, 0.5f);
        Move = Sequence("move", new[] { 440f }, 0.05f, 0.5f);
        Confirm = Sequence("confirm", new[] { 523f, 784f }, 0.07f, 0.6f);
        Back = Sequence("back", new[] { 440f, 330f }, 0.07f, 0.6f);
        Alarm = Sequence("alarm", new[] { 880f, 660f, 880f, 660f }, 0.12f, 0.6f);
        Airlock = Sequence("airlock", new[] { 294f, 440f, 587f }, 0.055f, 0.55f);
        Eat = Sequence("eat", new[] { 330f, 392f, 523f }, 0.05f, 0.5f);
        Drink = Sequence("drink", new[] { 440f, 587f, 659f }, 0.05f, 0.5f);
        Repair = Sequence("repair", new[] { 523f, 659f, 784f, 1046f }, 0.045f, 0.55f);
        Scavenge = Sequence("scavenge", new[] { 587f, 784f, 988f }, 0.06f, 0.6f);
        Storm = Sequence("storm", new[] { 196f, 165f, 147f, 131f }, 0.14f, 0.55f);

        ready = true;
    }

    public static void Play(AudioClip clip, float volume = 0.6f)
    {
        if (src == null || clip == null) return;
        src.PlayOneShot(clip, volume);
    }

    static AudioClip Sequence(string name, float[] freqs, float noteSeconds, float vol)
    {
        const int rate = 44100;
        int per = Mathf.CeilToInt(rate * noteSeconds);
        var data = new float[per * freqs.Length];

        for (int n = 0; n < freqs.Length; n++)
        {
            for (int i = 0; i < per; i++)
            {
                float t = i / (float)rate;
                float env = 1f - i / (float)per;
                float s = Mathf.Sin(2f * Mathf.PI * freqs[n] * t) >= 0f ? 1f : -1f;
                data[n * per + i] = s * env * vol * 0.3f;
            }
        }

        var clip = AudioClip.Create(name, data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}