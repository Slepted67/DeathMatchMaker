using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("Refs")]
    public GameObject pausePanel;
    [Header("Input")]
    public KeyCode pauseKey = KeyCode.Escape;

    public static bool IsPaused { get; private set; }

    void Start()
    {
        if (pausePanel) pausePanel.SetActive(false);
        IsPaused = false;
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            if (IsPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;
        if (pausePanel) pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        if (pausePanel) pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        var scene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(scene);
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu"); // change if your menu is named differently
    }
}
