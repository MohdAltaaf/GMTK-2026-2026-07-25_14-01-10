using UnityEngine;

// Attach to anything that should be able to catch a Throwable - player, enemy AI, etc.
// Kept separate from player input code so an enemy can reuse this exact same logic later.
public class Catcher : MonoBehaviour
{
    [Tooltip("Where caught/picked-up items snap to. Falls back to this object's own transform if left empty.")]
    [SerializeField] private Transform holdPoint;

    // All active catchers in the scene - lets a bomb/etc find every valid target
    // without an expensive FindObjectsOfType call every frame.
    public static readonly System.Collections.Generic.List<Catcher> All = new System.Collections.Generic.List<Catcher>();

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    public bool IsReadyToCatch { get; set; }
    public event System.Action<Throwable> OnCaught;

    public Transform HoldPoint => holdPoint != null ? holdPoint : transform;

    public void ReceiveCatch(Throwable item)
    {
        item.PickUp(HoldPoint);
        OnCaught?.Invoke(item);
    }
}