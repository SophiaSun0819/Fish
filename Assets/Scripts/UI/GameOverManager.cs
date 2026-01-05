using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Game Over 管理器
/// 訂閱玩家死亡事件，負責顯示 Game Over UI
/// </summary>
public class GameOverManager : MonoBehaviour
{
    [Header("UI 設定")]
    [SerializeField] private GameObject _gameOverPanel;
    
    [Header("延遲設定")]
    [SerializeField] private float _showDelay = 1.5f;  // 延遲顯示 UI 的時間

    [Header("顯示資訊")]
    [SerializeField] private UnityEngine.UI.Text _killerNameText;  // 可選：顯示兇手名稱

    private void Start()
    {
        // 確保一開始是隱藏的
        if (_gameOverPanel != null)
        {
            _gameOverPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // 訂閱玩家死亡事件
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.OnPlayerDeath += HandlePlayerDeath;
        }
    }

    private void OnDisable()
    {
        // 取消訂閱
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.OnPlayerDeath -= HandlePlayerDeath;
        }
    }

    /// <summary>
    /// 處理玩家死亡事件
    /// </summary>
    private void HandlePlayerDeath(string killerName)
    {
        Debug.Log($"[GameOverManager] 收到玩家死亡事件，兇手: {killerName}");
        
        // 如果有顯示兇手名稱的 Text
        if (_killerNameText != null)
        {
            _killerNameText.text = $"被 {killerName} 吃掉了！";
        }

        // 延遲顯示 UI
        StartCoroutine(ShowGameOverDelayed());
    }

    /// <summary>
    /// 延遲顯示 Game Over UI
    /// </summary>
    private IEnumerator ShowGameOverDelayed()
    {
        yield return new WaitForSeconds(_showDelay);
        ShowGameOver();
    }

    /// <summary>
    /// 顯示 Game Over 介面
    /// </summary>
    public void ShowGameOver()
    {
        if (_gameOverPanel != null)
        {
            _gameOverPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 隱藏 Game Over 介面
    /// </summary>
    public void HideGameOver()
    {
        if (_gameOverPanel != null)
        {
            _gameOverPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 重新開始遊戲（供 UI 按鈕呼叫）
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("重新開始遊戲");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// 退出遊戲（供 UI 按鈕呼叫）
    /// </summary>
    public void ExitGame()
    {
        Debug.Log("退出遊戲");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
