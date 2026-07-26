using UnityEngine;

// Temporary test harness - shows fuse/state/event info on screen via OnGUI so you
// can verify Bomb's internals are working before the real countdown UI exists.
// Harmless to leave attached, or just delete this file once you've got real UI.
public class BombDebugDisplay : MonoBehaviour
{
    [SerializeField] private Bomb bomb;

    private float lastFuseValue = 1f;
    private string lastEvent = "";

    private void OnEnable()
    {
        bomb.OnFuseTick += HandleFuseTick;
        bomb.OnRetargeted += HandleRetargeted;
        bomb.OnDetonated += HandleDetonated;
    }

    private void OnDisable()
    {
        bomb.OnFuseTick -= HandleFuseTick;
        bomb.OnRetargeted -= HandleRetargeted;
        bomb.OnDetonated -= HandleDetonated;
    }

    private void HandleFuseTick(float normalized) => lastFuseValue = normalized;

    private void HandleRetargeted(Catcher newTarget)
    {
        lastEvent = $"Retargeted onto {newTarget.name}";
        Debug.Log(lastEvent);
    }

    private void HandleDetonated(Catcher loser)
    {
        lastEvent = loser != null ? $"DETONATED - {loser.name} loses" : "DETONATED (no catchers in scene)";
        Debug.Log(lastEvent);
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 400, 24), $"Fuse: {lastFuseValue:P0}");
        GUI.Label(new Rect(10, 34, 400, 24), $"State: {bomb.State}");
        GUI.Label(new Rect(10, 58, 400, 24), lastEvent);
    }
}
