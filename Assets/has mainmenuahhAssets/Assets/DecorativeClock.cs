using UnityEngine;

/// <summary>
/// Purely decorative clock hands - just spins two Transforms at whatever speed you set in the
/// Inspector. Not tied to System.DateTime or real-world time in any way; if you ever want it to
/// show the actual time later, that's a separate script, this one is just for looks/animation.
/// </summary>
public class DecorativeClock : MonoBehaviour
{
    [Header("Hands")]
    [Tooltip("The long hand (usually the faster/more prominent one visually - doesn't have to be 'minutes', just whichever hand you want spinning quicker).")]
    [SerializeField] private Transform primaryHand;

    [Tooltip("The short hand (usually the slower one).")]
    [SerializeField] private Transform secondaryHand;

    [Header("Rotation")]
    [Tooltip("Which local axis each hand rotates around. Z is the usual pick for a flat clock face (like a UI clock or a wall clock facing the camera).")]
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;

    [Tooltip("Degrees per second for the primary hand. Negative = counter-clockwise.")]
    [SerializeField] private float primarySpeed = 60f;

    [Tooltip("Degrees per second for the secondary hand. Negative = counter-clockwise.")]
    [SerializeField] private float secondarySpeed = 5f;

    [Tooltip("Multiplies both speeds - handy single knob to speed up/slow down the whole clock without changing the ratio between hands.")]
    [SerializeField] private float globalSpeedMultiplier = 1f;

    private void Update()
    {
        float dt = Time.deltaTime * globalSpeedMultiplier;

        if (primaryHand != null)
        {
            primaryHand.Rotate(rotationAxis, primarySpeed * dt, Space.Self);
        }

        if (secondaryHand != null)
        {
            secondaryHand.Rotate(rotationAxis, secondarySpeed * dt, Space.Self);
        }
    }
}
