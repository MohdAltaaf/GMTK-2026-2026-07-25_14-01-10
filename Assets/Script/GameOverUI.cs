using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Bomb bomb;
    [Tooltip("The local player's Catcher - compared against the loser from OnDetonated to tell win from lose.")]
    [SerializeField] private Catcher playerCatcher;

    [Header("UI")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private Button restartButton;

    private void Awake()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (restartButton != null) restartButton.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (bomb != null) bomb.OnDetonated += HandleDetonated;
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
    }

    private void OnDisable()
    {
        if (bomb != null) bomb.OnDetonated -= HandleDetonated;
        if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
    }

    private void HandleDetonated(Catcher loser)
    {
        if (loser == null) return;

        bool playerLost = loser == playerCatcher;

        if (playerLost && losePanel != null) losePanel.SetActive(true);
        else if (!playerLost && winPanel != null) winPanel.SetActive(true);

        if (restartButton != null) restartButton.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0.3f;
    }

    private void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}