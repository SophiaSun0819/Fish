using UnityEngine;

/// <summary>
/// 可被吃的物件介面
/// 水草、NPC魚都可以實作這個介面
/// </summary>
public interface IEdible
{
    /// <summary>
    /// 取得這個物件的大小（用於判斷能不能被吃）
    /// </summary>
    float GetSize();

    /// <summary>
    /// 檢查是否可以被吃
    /// </summary>
    bool CanBeEaten();

    /// <summary>
    /// 被吃的時候呼叫
    /// </summary>
    /// <param name="eater">吃掉這個物件的 Transform</param>
    /// <returns>吃成功後獲得的營養值（用於成長）</returns>
    float OnEaten(Transform eater);

    /// <summary>
    /// 取得這個物件的 Transform
    /// </summary>
    Transform GetTransform();
}
