using UnityEngine;

public class Seaweed : MonoBehaviour
{
    [Header("水草設定")]
    [SerializeField]
    private float _totalSize = 1f; // 水草的總大小
    [SerializeField]
    private float _eatAmountPerBite = 0.1f; // 每次被吃掉的量
    [SerializeField]
    private float _regrowSpeed = 0.05f; // 每秒再生的速度

    private float _currentSize;

    private void Start()
    {
        _currentSize = _totalSize;
        UpdateSeaweedSize();
    }

    private void Update()
    {
        // 水草慢慢長大
        if (_currentSize < _totalSize)
        {
            _currentSize += _regrowSpeed * Time.deltaTime;
            _currentSize = Mathf.Min(_currentSize, _totalSize);
            UpdateSeaweedSize();
        }
    }


    /// <summary>
    /// 水草是否被吃
    /// </summary>
    /// <returns>是否被吃</returns>
    public bool IsGetEaten()
    {
        if (_currentSize <= 0)
        {
            return false;
        }

        _currentSize -= _eatAmountPerBite;
        _currentSize = Mathf.Max(_currentSize, 0); // 不小於0

        UpdateSeaweedSize();

        Debug.Log($"水草剩餘大小: {_currentSize}");

        return true;
    }

    /// <summary>
    /// 檢查水草還能不能吃
    /// </summary>
    /// <returns>能不能吃</returns>
    public bool IsEatable()
    {
        return _currentSize > 0;
    }

    /// <summary>
    /// 更新水草大小
    /// </summary>
    private void UpdateSeaweedSize()
    {
        float sizeRatio = Mathf.Max(_currentSize / _totalSize, 0.01f);
        transform.localScale = Vector3.one * sizeRatio;
    }
}