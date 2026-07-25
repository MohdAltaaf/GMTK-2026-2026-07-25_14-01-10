using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class EnemyBehaviour : MonoBehaviour
{
    public ParticleSystem bloodSplatterPrefab;

    [Tooltip("Empty child transform placed at head height, facing wherever you want the player looking after teleporting here. Set at level-design time - does NOT move when the body ragdolls.")]
    public Transform vantagePoint;

    private float destroyDelay = 3f;
    private Rigidbody rb;
    private Collider col;
    private bool isDead = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        rb.isKinematic = true;
    }

    public void Die(Vector3 hitPoint, Vector3 force)
    {
        if (isDead) return;
        isDead = true;

        // Hand control to physics for the death tumble.
        rb.isKinematic = false;
        rb.AddForceAtPosition(force, hitPoint, ForceMode.Impulse);

        if (bloodSplatterPrefab != null)
        {
            ParticleSystem fx = Instantiate(
                bloodSplatterPrefab,
                hitPoint,
                Quaternion.LookRotation(force.normalized)
            );
            fx.Play();
            Destroy(fx.gameObject, fx.main.duration);
        }

        // IMPORTANT: keep the collider enabled - disabling it (like the
        // earlier version did) removes the corpse from ALL physics
        // collision, including the ground, which is why it was falling
        // through the floor. Instead, just move it to a dedicated layer
        // so it's invisible to the shooting raycast but still lands
        // normally. Requires a "DeadEnemy" layer (Project Settings >
        // Tags and Layers) excluded from shootingScript's Shootable Mask.
        gameObject.layer = LayerMask.NameToLayer("DeadEnemy");

        Destroy(gameObject, destroyDelay);
    }
}