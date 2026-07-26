using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string playSceneName;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip buttonClickSound;

    public void PlayGame()
    {
        StartCoroutine(PlayGameRoutine());
    }

    private IEnumerator PlayGameRoutine()
    {
        PlayClickSound();

        // Brief delay so the click sound plays before the scene destroys itself
        yield return new WaitForSecondsRealtime(0.15f);

        if (!string.IsNullOrEmpty(playSceneName))
        {
            SceneManager.LoadScene(playSceneName);
        }
        else
        {
            Debug.LogWarning("MainMenu: Play scene name is missing in the Inspector!");
        }
    }

    public void ExitGame()
    {
        PlayClickSound();
        
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    // Call this if you add other menu buttons (Options, Credits, etc.)
    public void PlayClickSound()
    {
        if (sfxSource != null && buttonClickSound != null)
        {
            sfxSource.PlayOneShot(buttonClickSound);
        }
    }
}