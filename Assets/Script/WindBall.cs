using UnityEngine;

// Same generic Throwable pipeline (GDD section 8) - one-shot AoE on impact.
// Knocks back nearby rigidbodies; if the Bomb's caught in the blast, deflects
// and retargets it too, even without a direct physical touch between the two.
public class WindBall : Throwable
{
    [Header("Wind Ball")]
    [SerializeField] private float explosionRadius = 6f;
    [SerializeField] private float explosionForce = 15f;
    [SerializeField] private float explosionUpwardsModifier = 1f;

        protected override void OnHitEffect(Collision collision)
    {
        Vector3 origin = transform.position;
        Collider[] hits = Physics.OverlapSphere(origin, explosionRadius);

        foreach (Collider hit in hits)
        {
            // Every rigidbody in range gets the same push, bomb included - no
            // special-casing its physics, it's just another object in the blast.
            Rigidbody hitRb = hit.attachedRigidbody;
            if (hitRb != null && hitRb != rb)
            {
                hitRb.AddExplosionForce(explosionForce, origin, explosionRadius, explosionUpwardsModifier, ForceMode.Impulse);
            }

            // On top of that: if it was specifically the bomb, also retarget it.
            Bomb bomb = hit.GetComponentInParent<Bomb>();
            if (bomb != null)
            {
                Catcher thrower = CurrentThrower != null ? CurrentThrower.GetComponentInParent<Catcher>() : null;
                bomb.NotifyExplosionHit(thrower);
                Debug.Log("Wind ball caught the bomb in its blast - retargeting");
            }
        }
    }
}