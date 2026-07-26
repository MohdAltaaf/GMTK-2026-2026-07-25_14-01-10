using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [Header("Audio Sources")]
    [Tooltip("Audio Source configured for looping movement sounds")]
    [SerializeField] private AudioSource footstepSource;
    [Tooltip("Audio Source for one-shot sounds (Throwing, Jumping, etc.)")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip walkLoopClip;
    [SerializeField] private AudioClip throwClip;

    private void Start()
    {
        // Set up footstep loop source
        if (footstepSource != null && walkLoopClip != null)
        {
            footstepSource.clip = walkLoopClip;
            footstepSource.loop = true;
        }
    }

    /// <summary>
    /// Call this from your movement script every frame.
    /// Pass 'true' if the player is actively walking/moving, 'false' if standing still.
    /// </summary>
    public void SetWalking(bool isMoving)
    {
        if (footstepSource == null) return;

        if (isMoving && !footstepSource.isPlaying)
        {
            footstepSource.Play();
        }
        else if (!isMoving && footstepSource.isPlaying)
        {
            footstepSource.Pause();
        }
    }

    /// <summary>
    /// Call this right when the player throws an object.
    /// </summary>
    public void PlayThrowSound()
    {
        if (sfxSource != null && throwClip != null)
        {
            // Pitch randomization adds subtle variety so repeating the sound feels natural
            sfxSource.pitch = Random.Range(0.9f, 1.1f);
            sfxSource.PlayOneShot(throwClip);
        }
    }
}