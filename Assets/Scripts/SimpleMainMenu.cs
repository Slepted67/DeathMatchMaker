using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleMainMenu : MonoBehaviour
{
    [Header("Target Scene To Load")]
    public string gameplaySceneName = "GameScene";

    public void OnPlayClicked()
    {
        Time.timeScale = 1f; // just in case
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void OnQuitClicked()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
