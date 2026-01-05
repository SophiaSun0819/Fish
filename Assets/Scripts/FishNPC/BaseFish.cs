using UnityEngine;

/// <summary>
/// 所有魚的基礎抽象類別
/// 實作 IEdible 介面讓魚可以被吃
/// </summary>
public abstract class BaseFish : MonoBehaviour, IEdible
{
    [Header("基礎屬性")]
    [SerializeField]
    protected float _currentSize = 1f; // 當前大小
    [SerializeField]
    protected float _moveSpeed = 3f; // 移動速度
    [SerializeField]
    protected float _turnSpeed = 90f; // 轉向速度

    [Header("被吃設定")]
    [SerializeField]
    protected float _nutritionValue = 0.1f; // 被吃掉時提供的營養值
    [SerializeField]
    protected bool _canBeEaten = true; // 是否可以被吃

    protected bool _isDead = false; // 是否已死亡

    protected virtual void Start()
    {
        UpdateFishSize();
    }

    protected void UpdateFishSize()
    {
        transform.localScale = Vector3.one * _currentSize;
    }

    public float GetCurrentSize()
    {
        return _currentSize;
    }

    protected abstract void Move();

    public abstract void Eat();

    #region IEdible Implementation

    /// <summary>
    /// IEdible: 取得大小
    /// </summary>
    public float GetSize()
    {
        return _currentSize;
    }

    /// <summary>
    /// IEdible: 是否可以被吃
    /// </summary>
    public virtual bool CanBeEaten()
    {
        return _canBeEaten && !_isDead;
    }

    /// <summary>
    /// IEdible: 被吃的時候呼叫
    /// </summary>
    /// <param name="eater">吃掉這隻魚的 Transform</param>
    /// <returns>營養值</returns>
    public virtual float OnEaten(Transform eater)
    {
        if (!CanBeEaten()) return 0f;

        _isDead = true;
        Debug.Log($"{gameObject.name} 被 {eater.name} 吃掉了！");
        
        // 可以在這裡加入死亡動畫或特效
        Destroy(gameObject, 0.1f);
        
        return _nutritionValue;
    }

    /// <summary>
    /// IEdible: 取得 Transform
    /// </summary>
    public Transform GetTransform()
    {
        return transform;
    }

    #endregion
}
