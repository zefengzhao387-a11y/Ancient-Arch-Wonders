using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏主菜单：背景 + 游戏开始、退出游戏按钮
/// </summary>
public class GameMenuController : MonoBehaviour
{
    [Header("按钮")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button exitGameButton;

    [Header("下一场景")]
    [SerializeField] private string nextSceneName = "OpeningVideo";

    private void Start()
    {
        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartGame);
        if (exitGameButton != null)
            exitGameButton.onClick.AddListener(OnExitGame);
    }

    private void OnStartGame()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    private void OnExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
