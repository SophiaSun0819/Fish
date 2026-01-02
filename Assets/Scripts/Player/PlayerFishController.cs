using UnityEngine;

/// <summary>
/// 玩家魚控制器 - 移動整個魚物件
/// </summary>
public class PlayerFishController : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _turnSpeed = 90f;
    [SerializeField] private float _sprintSpeed = 10f;

    [Header("潛水設定")]
    [SerializeField] private float _diveSpeed = 3f;
    [SerializeField] private float _waterSurfaceY = 0f;
    [SerializeField] private float _maxDiveDepth = -10f;

    [Header("跳出水面設定")]
    [SerializeField] private float _jumpOutHeight = 1.5f;
    [SerializeField] private float _jumpOutSpeed = 6f;
    [SerializeField] private float _gravity = 10f;

    [Header("成長設定")]
    [SerializeField] private float _currentSize = 1f;
    [SerializeField] private float _minSize = 0.5f;
    [SerializeField] private float _maxSize = 3f;
    [SerializeField] private float _growthPerBite = 0.05f;

    [Header("吃東西設定")]
    [SerializeField] private float _eatRange = 2f;

    [Header("縮小設定")]
    [SerializeField] private float _shrinkRate = 0.02f;

    [Header("音效")]
    public AudioSource eatNothingSFX;
    public AudioSource eatSeaweedSFX;
    public AudioSource jumpSFX;

    private bool _isInPollutedWater = false;
    private bool _wasDiving = false;
    private float _verticalVelocity = 0f;
    private Vector2 _input;

    void Start()
    {
        UpdateFishSize();
    }

    void Update()
    {
        _input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        
        // 移動整個物件
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? _sprintSpeed : _moveSpeed;
        transform.position += transform.forward * _input.y * currentSpeed * Time.deltaTime;
        transform.Rotate(Vector3.up, _input.x * _turnSpeed * Time.deltaTime);

        // 潛水
        HandleDiving();

        // 吃東西
        if (Input.GetKeyDown(KeyCode.C)) EatSeaweed();

        // 污染縮小
        if (_isInPollutedWater)
        {
            _currentSize = Mathf.Clamp(_currentSize - _shrinkRate * Time.deltaTime, _minSize, _maxSize);
            UpdateFishSize();
        }
    }

    private void HandleDiving()
    {
        bool isDiving = Input.GetKey(KeyCode.Space);
        Vector3 pos = transform.position;

        if (isDiving)
        {
            pos.y -= _diveSpeed * Time.deltaTime;
            pos.y = Mathf.Max(pos.y, _maxDiveDepth);
            _wasDiving = true;
            _verticalVelocity = 0;
        }
        else
        {
            if (_wasDiving)
            {
                _verticalVelocity = _jumpOutSpeed;
                _wasDiving = false;
                if (jumpSFX != null) jumpSFX.Play();
            }

            if (_verticalVelocity > 0)
            {
                pos.y += _verticalVelocity * Time.deltaTime;
                _verticalVelocity -= _gravity * Time.deltaTime;
                if (pos.y >= _waterSurfaceY + _jumpOutHeight)
                {
                    pos.y = _waterSurfaceY + _jumpOutHeight;
                    _verticalVelocity = 0;
                }
            }
            else
            {
                pos.y -= _gravity * Time.deltaTime;
                if (pos.y <= _waterSurfaceY)
                {
                    pos.y = _waterSurfaceY;
                    _verticalVelocity = 0;
                }
            }
        }
        transform.position = pos;
    }

    private void EatSeaweed()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _eatRange);
        foreach (Collider col in hitColliders)
        {
            Seaweed seaweed = col.GetComponent<Seaweed>();
            if (seaweed != null && seaweed.IsEatable())
            {
                if (eatSeaweedSFX != null) eatSeaweedSFX.Play();
                if (seaweed.IsGetEaten())
                {
                    _currentSize = Mathf.Clamp(_currentSize + _growthPerBite, _minSize, _maxSize);
                    UpdateFishSize();
                    Debug.Log("小魚當前大小: " + _currentSize);
                    return;
                }
            }
        }
        if (eatNothingSFX != null) eatNothingSFX.Play();
        Debug.Log("沒有水草可以吃！");
    }

    private void UpdateFishSize()
    {
        transform.localScale = Vector3.one * _currentSize;
    }

    public float GetCurrentSize() => _currentSize;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PollutedWater"))
        {
            _isInPollutedWater = true;
            Debug.Log("小魚進入污染水池，開始縮小！");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PollutedWater"))
        {
            _isInPollutedWater = false;
            Debug.Log("小魚離開污染水池，停止縮小！");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _eatRange);
    }
}
