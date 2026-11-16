using UnityEngine;

public static class GameAutoBoot
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureBootstrapExists()
    {
        if (Object.FindAnyObjectByType<GameBootstrap>() != null) return;

        var go = new GameObject("GameBootstrap");
        go.AddComponent<GameBootstrap>();
    }
}