using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家魚控制器 - 移動整個魚物件
/// </summary>
using Input = UnityEngine.Input;
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

    [Header("跳躍旋轉設定")]
    [SerializeField] private float _maxJumpRotation = 45f;
    [SerializeField] private float _rotationSpeed = 5f;

    private Quaternion _targetRotation;
    private float _currentPitch = 0f;

    [Header("音效")]
    public AudioSource eatNothingSFX;
    public AudioSource eatSeaweedSFX;
    public AudioSource jumpSFX;
    public AudioSource deathSFX;

    private bool _isInPollutedWater = false;
    private bool _wasDiving = false;
    private float _verticalVelocity = 0f;
    private Vector2 _input;
    private bool _isDead = false;

    private CharacterController _controller;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        if (_controller == null)
        {
            _controller = gameObject.AddComponent<CharacterController>();
            _controller.radius = 0.5f;
            _controller.height = 1f;
            _controller.center = Vector3.zero;
        }
        UpdateFishSize();
    }

    void Update()
    {
        if (_isDead) return;

        _input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? _sprintSpeed : _moveSpeed;
        Vector3 moveDirection = transform.forward * _input.y * currentSpeed * Time.deltaTime;

        _controller.Move(moveDirection);
        transform.Rotate(Vector3.up, _input.x * _turnSpeed * Time.deltaTime);

        HandleDiving();
        UpdateFishRotation();

        if (Input.GetKeyDown(KeyCode.C)) TryEat();

        if (_isInPollutedWater)
        {
            _currentSize = Mathf.Clamp(_currentSize - _shrinkRate * Time.deltaTime, _minSize, _maxSize);
            UpdateFishSize();
        }
    }

    private void HandleDiving()
    {
        bool isDiving = Input.GetKey(KeyCode.Space);
        Vector3 verticalMove = Vector3.zero;
        float currentY = transform.position.y;

        if (isDiving)
        {
            verticalMove.y = -_diveSpeed * Time.deltaTime;
            if (currentY + verticalMove.y < _maxDiveDepth)
                verticalMove.y = _maxDiveDepth - currentY;

            _wasDiving = true;
            _verticalVelocity = 0;
        }
        else
        {
            if (_wasDiving)
            {
                if (currentY <= _waterSurfaceY)
                {
                    _verticalVelocity = _jumpOutSpeed;
                    _wasDiving = false;
                    if (jumpSFX != null) jumpSFX.Play();
                }
                else
                {
                    _wasDiving = false;
                    _verticalVelocity = 0;
                }
            }

            if (_verticalVelocity > 0)
            {
                verticalMove.y = _verticalVelocity * Time.deltaTime;
                _verticalVelocity -= _gravity * Time.deltaTime;

                if (currentY + verticalMove.y >= _waterSurfaceY + _jumpOutHeight)
                {
                    verticalMove.y = (_waterSurfaceY + _jumpOutHeight) - currentY;
                    _verticalVelocity = 0;
                }
            }
            else if (currentY > _waterSurfaceY)
            {
                verticalMove.y = -_gravity * Time.deltaTime;

                if (currentY + verticalMove.y <= _waterSurfaceY)
                {
                    verticalMove.y = _waterSurfaceY - currentY;
                    _verticalVelocity = 0;
                }
            }
            else
            {
                verticalMove.y = _waterSurfaceY - currentY;
                _verticalVelocity = 0;
            }
        }

        _controller.Move(verticalMove);
    }

    private void UpdateFishRotation()
    {
        float targetPitch = 0f;

        if (_verticalVelocity > 0)
        {
            targetPitch = Mathf.Lerp(0, _maxJumpRotation, _verticalVelocity / _jumpOutSpeed);
        }
        else if (transform.position.y > _waterSurfaceY)
        {
            float fallSpeed = Mathf.Abs(_verticalVelocity);
            targetPitch = Mathf.Lerp(0, -_maxJumpRotation, fallSpeed / _jumpOutSpeed);
        }
        else if (Input.GetKey(KeyCode.Space))
        {
            targetPitch = -_maxJumpRotation * 0.5f;
        }
        else
        {
            targetPitch = 0f;
        }

        _currentPitch = Mathf.Lerp(_currentPitch, targetPitch, Time.deltaTime * _rotationSpeed);
        transform.localRotation = Quaternion.Euler(_currentPitch, transform.localEulerAngles.y, 0);
    }

    /// <summary>
    /// 嘗試吃東西 (水草或比自己小的魚)
    /// </summary>
    private void TryEat()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _eatRange);

        foreach (Collider col in hitColliders)
        {
            BaseFish fish = col.GetComponent<BaseFish>();
            if (fish != null)
            {
                if (fish.GetSize() < _currentSize)
                {
                    IEdible edible = col.GetComponent<IEdible>();
                    float nutrition = edible.OnEaten(transform);
                    if (nutrition > 0)
                    {
                        if (eatSeaweedSFX != null) eatSeaweedSFX.Play();
                        _currentSize = Mathf.Clamp(_currentSize + nutrition, _minSize, _maxSize);
                        UpdateFishSize();
                        Debug.Log($"吃掉了 {col.name}！當前大小: {_currentSize}");
                        return;
                    }
                }
                else
                {
                    Debug.Log($"{col.name} 太大了，吃不下！");
                }
                continue;
            }

            Seaweed seaweed = col.GetComponent<Seaweed>();
            if (seaweed != null && seaweed.IsEatable())
            {
                if (eatSeaweedSFX != null) eatSeaweedSFX.Play();
                if (seaweed.IsGetEaten())
                {
                    _currentSize = Mathf.Clamp(_currentSize + _growthPerBite, _minSize, _maxSize);
                    UpdateFishSize();
                    Debug.Log("吃了水草！當前大小: " + _currentSize);
                    return;
                }
            }
        }

        if (eatNothingSFX != null) eatNothingSFX.Play();
        Debug.Log("附近沒有可以吃的東西！");
    }

    private void UpdateFishSize()
    {
        transform.localScale = Vector3.one * _currentSize;
    }

    public float GetCurrentSize() => _currentSize;

    /// <summary>
    /// 被肉食魚吃掉時呼叫
    /// </summary>
    public void OnBeingEaten(string eaterName)
    {
        if (_isDead) return;

        _isDead = true;
        Debug.Log($"玩家被 {eaterName} 吃掉了！");

        if (deathSFX != null) deathSFX.Play();

        // 隱藏玩家
        gameObject.SetActive(false);

        // 發送玩家死亡事件
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.PlayerDied(eaterName);
        }
    }

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
