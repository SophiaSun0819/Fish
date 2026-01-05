using UnityEngine;

/// <summary>
/// 危險物件介面
/// 會攻擊/吃玩家的生物實作這個介面
/// </summary>
public interface IDangerous
{
    /// <summary>
    /// 取得危險等級（可用於判斷攻擊優先順序）
    /// </summary>
    float GetThreatLevel();

    /// <summary>
    /// 取得攻擊範圍
    /// </summary>
    float GetAttackRange();

    /// <summary>
    /// 是否正在追逐目標
    /// </summary>
    bool IsChasing();

    /// <summary>
    /// 對玩家造成傷害（縮小玩家）
    /// </summary>
    /// <param name="target">攻擊目標</param>
    /// <returns>造成的傷害量</returns>
    float Attack(Transform target);
}
