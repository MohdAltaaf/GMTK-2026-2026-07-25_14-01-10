using UnityEngine;

// Attach to anything that should be able to catch a Throwable - player, enemy AI, etc.
// Kept separate from player input code so an enemy can reuse this exact same logic later.
public class Catcher : MonoBehaviour
{
    [Tooltip("Where caught/picked-up items snap to. Falls back to this object's own transform if left empty.")]
    [SerializeField] private Transform holdPoint;

    public bool IsReadyToCatch { get; set; }
    public event System.Action<Throwable> OnCaught;

    public Transform HoldPoint => holdPoint != null ? holdPoint : transform;

    public void ReceiveCatch(Throwable item)
    {
        item.PickUp(HoldPoint);
        OnCaught?.Invoke(item);
    }
}
