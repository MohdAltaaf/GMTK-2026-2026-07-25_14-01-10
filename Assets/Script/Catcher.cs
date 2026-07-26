using UnityEngine;

public class Catcher : MonoBehaviour
{
    [SerializeField] private Transform holdPoint;

    public static readonly System.Collections.Generic.List<Catcher> All = new System.Collections.Generic.List<Catcher>();

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    public bool IsReadyToCatch { get; set; }
    public event System.Action<Throwable> OnCaught;

    // Single source of truth for "what am I currently holding" - both ground
    // pickups and mid-air catches go through here now, so nothing can silently
    // overwrite a reference and orphan an item.
    public Throwable HeldItem { get; private set; }
    public bool IsHoldingSomething => HeldItem != null;

    public Transform HoldPoint => holdPoint != null ? holdPoint : transform;

    public void ReceiveCatch(Throwable item)
    {
        item.PickUp(HoldPoint);
        HeldItem = item;
        OnCaught?.Invoke(item);
    }

    // Call right after Throw()-ing HeldItem, so the catcher knows its hands are free.
    public void NotifyThrown()
    {
        HeldItem = null;
    }
}