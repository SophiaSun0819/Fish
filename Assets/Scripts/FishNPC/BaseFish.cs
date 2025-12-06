using UnityEngine;

public abstract class BaseFish : MonoBehaviour
{
    [Header("基礎屬性")]
    [SerializeField]
    protected float _currentSize = 1f; // 當前大小
    [SerializeField]
    protected float _moveSpeed = 3f; // 移動速度
    [SerializeField]
    protected float _turnSpeed = 90f; // 轉向速度

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
}
