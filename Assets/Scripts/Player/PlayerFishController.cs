using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家魚控制器 - Fishy 3D 風格操作
/// </summary>
using Input = UnityEngine.Input;
public class PlayerFishController : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _sprintSpeed = 10f;
    [SerializeField] private float _rotationSpeed = 5f;  // 自動轉向速度

    [Header("潛水設定")]
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

    [Header("體積影響設定")]
    [SerializeField] private float _minSpeedMultiplier = 0.5f;
    [SerializeField] private float _maxSpeedMultiplier = 1.5f;
    [SerializeField] private float _minJumpMultiplier = 0.6f;
    [SerializeField] private float _maxJumpMultiplier = 1.0f;

    [Header("吃東西設定")]
    [SerializeField] private float _eatRange = 3f;  // 提高基礎範圍
    [SerializeField] private float _eatRangeMultiplierWhenSmall = 1.5f;  // 小魚時範圍加成

    [Header("縮小設定")]
    [SerializeField] private float _shrinkRate = 0.02f;

    [Header("跳躍旋轉設定")]
    [SerializeField] private float _maxJumpRotation = 45f;
    [SerializeField] private float _rotationSmoothSpeed = 5f;

    [Header("無敵狀態設定")]
    [SerializeField] private float _superSpeedMultiplier = 2f;
    [SerializeField] private float _superJumpMultiplier = 2f;
    [SerializeField] private float _superSizeMultiplier = 3f;
    [SerializeField] private float _superStateDuration = 10f;

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

    private float _currentSpeedMultiplier = 1f;
    private float _currentJumpMultiplier = 1f;

    private bool _isSuperMode = false;
    private float _superModeTimer = 0f;
    private float _originalSize = 1f;

    private FishBodyAnimation _bodyAnimation;

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

        _bodyAnimation = GetComponent<FishBodyAnimation>();
        if (_bodyAnimation == null)
        {
            _bodyAnimation = GetComponentInChildren<FishBodyAnimation>();
        }

        UpdateFishSize();
    }

    void Update()
    {
        if (_isDead) return;

        // 處理無敵狀態計時
        if (_isSuperMode)
        {
            _superModeTimer -= Time.deltaTime;
            if (_superModeTimer <= 0f)
            {
                DeactivateSuperMode();
            }
        }

        // 獲取輸入
        _input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        // Fishy 3D 風格移動
        HandleFishyMovement();
        HandleDiving();
        UpdateFishRotation();

        if (Input.GetKeyDown(KeyCode.C)) TryEat();

        if (_isInPollutedWater)
        {
            _currentSize = Mathf.Clamp(_currentSize - _shrinkRate * Time.deltaTime, _minSize, _maxSize);
            UpdateFishSize();
        }
    }

    /// <summary>
    /// Fishy 3D 風格移動：按方向鍵就往該方向移動並自動轉向
    /// </summary>
    private void HandleFishyMovement()
    {
        if (_input.magnitude < 0.1f) return;

        // 計算移動速度
        float baseSpeed = Input.GetKey(KeyCode.LeftShift) ? _sprintSpeed : _moveSpeed;
        float adjustedSpeed = baseSpeed * _currentSpeedMultiplier;

        if (_isSuperMode)
        {
            adjustedSpeed *= _superSpeedMultiplier;
        }

        // 根據輸入計算目標方向（世界空間）
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;

        // 保持在水平面
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // 計算移動方向
        Vector3 moveDirection = (cameraForward * _input.y + cameraRight * _input.x).normalized;

        // 移動
        _controller.Move(moveDirection * adjustedSpeed * Time.deltaTime);

        // 自動轉向移動方向
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }
    }

    private void HandleDiving()
    {
        bool isDiving = Input.GetKey(KeyCode.Space);
        Vector3 verticalMove = Vector3.zero;
        float currentY = transform.position.y;

        if (isDiving)
        {
            float diveSpeed = _moveSpeed * _currentSpeedMultiplier;
            if (_isSuperMode) diveSpeed *= _superSpeedMultiplier;

            verticalMove.y = -diveSpeed * Time.deltaTime;
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
                    _verticalVelocity = _jumpOutSpeed * _currentJumpMultiplier;

                    if (_isSuperMode)
                    {
                        _verticalVelocity *= _superJumpMultiplier;
                    }

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

                float adjustedJumpHeight = _jumpOutHeight * _currentJumpMultiplier;

                if (_isSuperMode)
                {
                    adjustedJumpHeight *= _superJumpMultiplier;
                }

                if (currentY + verticalMove.y >= _waterSurfaceY + adjustedJumpHeight)
                {
                    verticalMove.y = (_waterSurfaceY + adjustedJumpHeight) - currentY;
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
            float adjustedJumpOutSpeed = _jumpOutSpeed * _currentJumpMultiplier;
            if (_isSuperMode)
            {
                adjustedJumpOutSpeed *= _superJumpMultiplier;
            }
            targetPitch = Mathf.Lerp(0, _maxJumpRotation, _verticalVelocity / adjustedJumpOutSpeed);
        }
        else if (transform.position.y > _waterSurfaceY)
        {
            float fallSpeed = Mathf.Abs(_verticalVelocity);
            float adjustedJumpOutSpeed = _jumpOutSpeed * _currentJumpMultiplier;
            if (_isSuperMode)
            {
                adjustedJumpOutSpeed *= _superJumpMultiplier;
            }
            targetPitch = Mathf.Lerp(0, -_maxJumpRotation, fallSpeed / adjustedJumpOutSpeed);
        }
        else if (Input.GetKey(KeyCode.Space))
        {
            targetPitch = -_maxJumpRotation * 0.5f;
        }
        else
        {
            targetPitch = 0f;
        }

        _currentPitch = Mathf.Lerp(_currentPitch, targetPitch, Time.deltaTime * _rotationSmoothSpeed);
        transform.localRotation = Quaternion.Euler(_currentPitch, transform.localEulerAngles.y, 0);
    }

    /// <summary>
    /// 嘗試吃東西（根據體型動態調整範圍，使用魚頭位置檢測）
    /// </summary>
    private void TryEat()
    {
        // 根據體型動態調整吃東西的範圍：小魚範圍比較大，大魚範圍比較小
        float sizeRatio = Mathf.InverseLerp(_minSize, _maxSize, _currentSize);
        float eatRangeMultiplier = Mathf.Lerp(_eatRangeMultiplierWhenSmall, 1f, sizeRatio);
        float effectiveEatRange = _eatRange * eatRangeMultiplier;
        
        // 無敵時範圍再加倍
        if (_isSuperMode)
        {
            effectiveEatRange *= _superSizeMultiplier;
        }
        
        // 使用魚頭（嘴巴）位置做檢測，而不是 transform 中心
        Vector3 mouthPosition = transform.position;
        if (_bodyAnimation != null && _bodyAnimation.GetHead() != null)
        {
            // 有身體動畫：使用頭部位置，並稍微往前延伸
            mouthPosition = _bodyAnimation.GetHead().position + _bodyAnimation.GetHead().forward * 0.3f;
        }
        else
        {
            // 沒有身體動畫：用魚前方位置
            mouthPosition = transform.position + transform.forward * 0.5f;
        }
        
        Collider[] hitColliders = Physics.OverlapSphere(mouthPosition, effectiveEatRange);

        foreach (Collider col in hitColliders)
        {
            BaseFish fish = col.GetComponent<BaseFish>();
            if (fish != null)
            {
                if (_isSuperMode || fish.GetSize() < _currentSize)
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
                    if (!_isSuperMode)
                    {
                        Debug.Log($"{col.name} 太大了，吃不下！");
                    }
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
        float displaySize = _currentSize;

        if (_isSuperMode)
        {
            displaySize = 1f * _superSizeMultiplier;  // 固定為初始大小(1)的3倍
        }

        transform.localScale = Vector3.one * displaySize;

        float sizeRatio = Mathf.InverseLerp(_minSize, _maxSize, _currentSize);
        _currentSpeedMultiplier = Mathf.Lerp(_maxSpeedMultiplier, _minSpeedMultiplier, sizeRatio);
        _currentJumpMultiplier = Mathf.Lerp(_maxJumpMultiplier, _minJumpMultiplier, sizeRatio);

        Debug.Log($"體積: {_currentSize:F2} | 速度倍率: {_currentSpeedMultiplier:F2} | 跳躍倍率: {_currentJumpMultiplier:F2}");
    }

    public float GetCurrentSize() => _currentSize;

    public void ActivateSuperMode()
    {
        if (_isSuperMode) return;

        _isSuperMode = true;
        _superModeTimer = _superStateDuration;
        _originalSize = _currentSize;

        UpdateFishSize();

        if (_bodyAnimation != null)
        {
            _bodyAnimation.SetSegmentDistance(
                _bodyAnimation.GetSegmentDistance() * _superSizeMultiplier
            );
        }

        Debug.Log("★★★ 無敵狀態啟動！體積變三倍，可以吃掉所有魚！★★★");
    }

    private void DeactivateSuperMode()
    {
        _isSuperMode = false;
        _superModeTimer = 0f;

        UpdateFishSize();

        if (_bodyAnimation != null)
        {
            _bodyAnimation.SetSegmentDistance(
                _bodyAnimation.GetSegmentDistance() / _superSizeMultiplier
            );
        }

        Debug.Log("無敵狀態結束，體積恢復正常");
    }

    public bool IsSuperMode() => _isSuperMode;

    public void OnBeingEaten(string eaterName)
    {
        if (_isDead) return;

        if (_isSuperMode)
        {
            Debug.Log($"{eaterName} 試圖吃掉無敵狀態的玩家，但失敗了！");
            return;
        }

        _isDead = true;
        Debug.Log($"玩家被 {eaterName} 吃掉了！");

        if (deathSFX != null) deathSFX.Play();

        gameObject.SetActive(false);

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
        // 顯示實際的吃東西範圍（根據體型動態調整）
        float sizeRatio = Mathf.InverseLerp(_minSize, _maxSize, _currentSize);
        float eatRangeMultiplier = Mathf.Lerp(_eatRangeMultiplierWhenSmall, 1f, sizeRatio);
        float effectiveEatRange = _eatRange * eatRangeMultiplier;
        
        if (_isSuperMode)
        {
            effectiveEatRange *= _superSizeMultiplier;
        }
        
        // 顯示嘴巴位置的檢測範圍
        Vector3 mouthPosition = transform.position;
        if (_bodyAnimation != null && _bodyAnimation.GetHead() != null)
        {
            mouthPosition = _bodyAnimation.GetHead().position + _bodyAnimation.GetHead().forward * 0.3f;
        }
        else
        {
            mouthPosition = transform.position + transform.forward * 0.5f;
        }
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(mouthPosition, effectiveEatRange);
    }
}
