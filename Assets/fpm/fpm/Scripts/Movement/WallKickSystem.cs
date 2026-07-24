using UnityEngine;

namespace FastFPSMovement.Movement
{
    /// <summary>
    /// Detects nearby walls via raycasts while airborne and, on jump input, launches
    /// the player away from the wall with a combined vertical + outward horizontal kick.
    /// Can be fully disabled via the inspector for game modes that don't want wall kicks.
    /// </summary>
    public class WallKickSystem : MonoBehaviour
    {
        [Header("Enable")]
        [Tooltip("Master toggle for the entire wall kick system.")]
        [SerializeField] private bool enableWallKick = true;

        [Header("Detection")]
        [Tooltip("Max distance from the player's side to detect a wall.")]
        [SerializeField] private float wallDetectionDistance = 0.8f;

        [Tooltip("Layers considered valid walls.")]
        [SerializeField] private LayerMask wallLayers = ~0;

        [Header("Kick Force")]
        [Tooltip("Outward horizontal force applied away from the wall normal.")]
        [SerializeField] private float wallJumpForce = 9f;

        [Tooltip("Upward vertical force applied on a wall kick.")]
        [SerializeField] private float wallJumpUpForce = 7f;

        [Tooltip("0 = kick pushes purely away from the wall (old behavior). 1 = kick pushes purely toward wherever the player is currently facing/moving. In between blends the two, so you get launched more in the direction you're going instead of always straight off the wall.")]
        [Range(0f, 1f)]
        [SerializeField] private float directionBlend = 0.6f;

        [Tooltip("Minimum seconds between consecutive wall kicks, to prevent chaining off the same wall instantly.")]
        [SerializeField] private float wallKickCooldown = 0.25f;

        [Header("Balance")]
        [Tooltip("Max wall kicks allowed in a single airborne stretch (resets the moment the player touches the ground). Prevents infinite wall-climbing.")]
        [SerializeField] private int maxWallKicksBeforeGrounded = 2;

        private Transform _playerTransform;
        private float _cooldownTimer;
        private int _wallKicksSinceGrounded;

        public bool WallKickEnabled => enableWallKick;

        public void Initialize(Transform playerTransform)
        {
            _playerTransform = playerTransform;
        }

        private void Update()
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
            }
        }

        /// <summary>
        /// Call every frame the player is grounded, so the wall-kick chain count
        /// refills once the player lands instead of persisting forever.
        /// </summary>
        public void NotifyGrounded()
        {
            _wallKicksSinceGrounded = 0;
        }

        /// <summary>
        /// Casts rays to the four horizontal sides of the player to find a nearby wall.
        /// Returns true and outputs the wall normal if one is found within range.
        /// </summary>
        public bool DetectWall(out Vector3 wallNormal)
        {
            wallNormal = Vector3.zero;

            if (!enableWallKick || _playerTransform == null)
            {
                return false;
            }

            Vector3 origin = _playerTransform.position;
            Vector3[] directions =
            {
                _playerTransform.forward,
                -_playerTransform.forward,
                _playerTransform.right,
                -_playerTransform.right
            };

            foreach (Vector3 dir in directions)
            {
                if (Physics.Raycast(origin, dir, out RaycastHit hit, wallDetectionDistance, wallLayers))
                {
                    wallNormal = hit.normal;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts a wall kick given a detected wall normal. Blends the pure outward
        /// wall-normal push with the player's current facing/movement direction (see
        /// directionBlend) so the kick sends you toward where you're heading along the
        /// wall, not always straight sideways off it. Returns the resulting
        /// horizontal+vertical velocity to apply, or false if on cooldown/disabled/capped.
        /// </summary>
        /// <param name="wallNormal">Surface normal of the detected wall.</param>
        /// <param name="playerDirection">Player's current facing or movement direction (world space, will be flattened to horizontal).</param>
        public bool TryWallKick(Vector3 wallNormal, Vector3 playerDirection, out Vector3 resultVelocity)
        {
            resultVelocity = Vector3.zero;

            if (!enableWallKick || _cooldownTimer > 0f)
            {
                return false;
            }

            if (_wallKicksSinceGrounded >= maxWallKicksBeforeGrounded)
            {
                return false;
            }

            Vector3 flatNormal = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;
            Vector3 flatDirection = new Vector3(playerDirection.x, 0f, playerDirection.z);

            // Discard the component of the player's direction that points INTO the wall
            // (that would just push you back into it) - keep only the part running
            // along the wall's surface plus whatever already points outward.
            float intoWall = Vector3.Dot(flatDirection, -flatNormal);
            if (intoWall > 0f)
            {
                flatDirection += flatNormal * intoWall;
            }

            Vector3 blendedDirection = flatDirection.sqrMagnitude > 0.01f
                ? Vector3.Lerp(flatNormal, flatDirection.normalized, directionBlend).normalized
                : flatNormal;

            Vector3 horizontalKick = blendedDirection * wallJumpForce;
            resultVelocity = new Vector3(horizontalKick.x, wallJumpUpForce, horizontalKick.z);
            _cooldownTimer = wallKickCooldown;
            _wallKicksSinceGrounded++;
            return true;
        }
    }
}
