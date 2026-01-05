using UnityEngine;
using System;

/// <summary>
/// 遊戲事件管理器
/// 使用單例模式，負責管理遊戲中的各種事件
/// </summary>
public class GameEvents : MonoBehaviour
{
    public static GameEvents Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ===== 玩家事件 =====
    
    /// <summary>
    /// 玩家死亡事件
    /// 參數：吃掉玩家的敵人名稱
    /// </summary>
    public event Action<string> OnPlayerDeath;

    /// <summary>
    /// 觸發玩家死亡事件
    /// </summary>
    public void PlayerDied(string killerName)
    {
        Debug.Log($"[GameEvents] 玩家死亡事件觸發，兇手: {killerName}");
        OnPlayerDeath?.Invoke(killerName);
    }

    // ===== 未來可擴充其他事件 =====
    
    // 例如：玩家吃到東西、玩家升級、遊戲暫停等
    // public event Action<float> OnPlayerGrow;
    // public event Action OnGamePause;
}
