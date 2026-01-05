using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Windows;
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
    [SerializeField] private float _maxJumpRotation = 45f;  // 最大仰角
    [SerializeField] private float _rotationSpeed = 5f;     // 旋轉速度

    private Quaternion _targetRotation;
    private float _currentPitch = 0f;  // 當前俯仰角

    [Header("音效")]
    public AudioSource eatNothingSFX;
    public AudioSource eatSeaweedSFX;
    public AudioSource jumpSFX;



    private bool _isInPollutedWater = false;
    private bool _wasDiving = false;
    private float _verticalVelocity = 0f;
    private Vector2 _input;

    private CharacterController _controller;


    void Start()
    {
        _controller = GetComponent<CharacterController>();
        if (_controller == null)
        {
            _controller = gameObject.AddComponent<CharacterController>();
            // 設定碰撞器大小
            _controller.radius = 0.5f;
            _controller.height = 1f;
            _controller.center = Vector3.zero;
        }
        UpdateFishSize();
    }



    void Update()
    {
        _input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        // 用 Move 取代直接修改 position
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? _sprintSpeed : _moveSpeed;
        Vector3 moveDirection = transform.forward * _input.y * currentSpeed * Time.deltaTime;

        _controller.Move(moveDirection);

        transform.Rotate(Vector3.up, _input.x * _turnSpeed * Time.deltaTime);

        HandleDiving();
        UpdateFishRotation();

        if (Input.GetKeyDown(KeyCode.C)) EatSeaweed();

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
            // 潛水中
            verticalMove.y = -_diveSpeed * Time.deltaTime;
            if (currentY + verticalMove.y < _maxDiveDepth)
                verticalMove.y = _maxDiveDepth - currentY;

            _wasDiving = true;
            _verticalVelocity = 0;
        }
        else
        {
            // 沒有按空格
            if (_wasDiving)
            {
                // 🔧 新增條件:只有在水面或水下才能跳
                if (currentY <= _waterSurfaceY)
                {
                    // 剛放開空格,跳出水面
                    _verticalVelocity = _jumpOutSpeed;
                    _wasDiving = false;
                    if (jumpSFX != null) jumpSFX.Play();
                }
                else
                {
                    // 在空中放開空格,不觸發跳躍
                    _wasDiving = false;
                    _verticalVelocity = 0;
                }
            }

            if (_verticalVelocity > 0)
            {
                // 正在上升
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
                // 在水面上方,需要下落
                verticalMove.y = -_gravity * Time.deltaTime;

                if (currentY + verticalMove.y <= _waterSurfaceY)
                {
                    verticalMove.y = _waterSurfaceY - currentY;
                    _verticalVelocity = 0;
                }
            }
            else
            {
                // 已經在水面或以下,保持在水面
                verticalMove.y = _waterSurfaceY - currentY;
                _verticalVelocity = 0;
            }
        }

        _controller.Move(verticalMove);
    }

    private void UpdateFishRotation()
    {
        float targetPitch = 0f;

        // 根據垂直速度計算目標俯仰角
        if (_verticalVelocity > 0)
        {
            // 向上跳 - 抬頭
            targetPitch = Mathf.Lerp(0, _maxJumpRotation, _verticalVelocity / _jumpOutSpeed);
        }
        else if (transform.position.y > _waterSurfaceY)
        {
            // 在空中下落 - 低頭
            float fallSpeed = Mathf.Abs(_verticalVelocity);
            targetPitch = Mathf.Lerp(0, -_maxJumpRotation, fallSpeed / _jumpOutSpeed);
        }
        else if (Input.GetKey(KeyCode.Space))
        {
            // 潛水中 - 低頭
            targetPitch = -_maxJumpRotation * 0.5f;
        }
        else
        {
            // 在水面 - 水平
            targetPitch = 0f;
        }

        // 平滑過渡到目標角度
        _currentPitch = Mathf.Lerp(_currentPitch, targetPitch, Time.deltaTime * _rotationSpeed);

        // 應用旋轉 (保持當前的 Y 軸旋轉,只改變 X 軸)
        transform.localRotation = Quaternion.Euler(_currentPitch, transform.localEulerAngles.y, 0);
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
