using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class shootingScript : MonoBehaviour
{
    private Camera playerCam;

    [Header("Shooting")]
    [Tooltip("Everything the shot can physically hit: enemies AND buildings/environment. Should NOT include the DeadEnemy layer, or corpses will block future shots.")]
    [SerializeField] private LayerMask shootableMask;
    [Tooltip("Just the enemy layer. Used to check whether the raycast hit was an enemy. Must match the layer your enemy capsules are actually on.")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float shotForce = 15f;
    [SerializeField] private float maxDistance = 500f;

    [Header("Teleport")]
    [Tooltip("Pause after a kill before snapping to the new vantage point, so the death/blood registers.")]
    [SerializeField] private float teleportDelay = 1.5f;

    [Header("Restart")]
    [Tooltip("Small delay so a miss reads visually before the level reloads.")]
    [SerializeField] private float restartDelay = 0.6f;

    private bool canFire = true;

    void Start()
    {
        playerCam = GetComponentInChildren<Camera>();
    }

    // Fires exactly once per press - NOT polled every frame in Update().
    // Polling "is the button currently held" was the bug: attackPressed
    // stays true for as long as the mouse is physically down, so the
    // instant canFire flipped back to true after a kill, the very next
    // Update() frame fired again automatically - before you'd aimed
    // anywhere - which almost always missed and restarted the level
    // right after what looked like a successful hit.
    void OnAttack(InputValue value)
    {
        if (value.isPressed && canFire)
        {
            Fire();
        }
    }

    private void Fire()
    {
        canFire = false;

        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, shootableMask))
        {
            bool hitEnemy = ((1 << hit.collider.gameObject.layer) & enemyLayer) != 0;

            if (hitEnemy)
            {
                EnemyBehaviour enemy = hit.collider.GetComponentInParent<EnemyBehaviour>();
                if (enemy != null)
                {
                    Vector3 forceDir = ray.direction;
                    enemy.Die(hit.point, forceDir * shotForce);
                    StartCoroutine(TeleportAfterDelay(enemy.vantagePoint));
                    return;
                }
            }
        }

        // Hit a building, or hit nothing at all -> miss -> restart.
        Invoke(nameof(RestartLevel), restartDelay);
    }

    private IEnumerator TeleportAfterDelay(Transform vantagePoint)
    {
        // canFire stays false for the whole delay, so a second press
        // during the transition can't sneak a shot in from the old spot.
        yield return new WaitForSeconds(teleportDelay);
        TeleportTo(vantagePoint);
        canFire = true; // new position, new bullet
    }

    private void TeleportTo(Transform vantagePoint)
    {
        transform.position = vantagePoint.position;
        transform.rotation = vantagePoint.rotation;
    }

    private void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}