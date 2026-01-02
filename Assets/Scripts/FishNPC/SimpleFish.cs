using UnityEngine;

public class SimpleFish : MonoBehaviour
{
    [SerializeField] private Transform[] _segments;
    [SerializeField] private float _segmentDistance = 0.5f;
    [SerializeField] private float _speed = 2f;
    [SerializeField] private float _rotationSpeed = 5f;  // 轉向速度
    [SerializeField] private float _tailSwingSpeed = 5f;
    [SerializeField] private float _tailSwingAmount = 0.3f;

    // 滑鼠控制相關
    [SerializeField] private Camera _camera;
    [SerializeField] private float _mouseHeight = 0f;  // 滑鼠在Y軸的高度

    private float _swimTime;
    private Vector3 _targetDirection;

    void Start()
    {
        if (_segments == null || _segments.Length == 0)
        {
            Debug.LogError("請設定 Segments！");
            return;
        }

        // 如果沒有指定相機，使用主相機
        if (_camera == null)
        {
            _camera = Camera.main;
        }

        _targetDirection = _segments[0].forward;
    }

    void Update()
    {
        _swimTime += Time.deltaTime;

        UpdateMouseTarget();  // 新增：更新滑鼠目標
        MoveHead();
        FollowHead();
        AddTailSwing();
    }

    private void UpdateMouseTarget()
    {
        // 創建一個從相機到滑鼠位置的射線
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);

        // 創建一個水平面（在 _mouseHeight 的高度）
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, _mouseHeight, 0));

        // 檢查射線是否打到這個平面
        if (groundPlane.Raycast(ray, out float distance))
        {
            // 計算滑鼠在世界中的位置
            Vector3 mouseWorldPosition = ray.GetPoint(distance);

            // 計算從魚頭到滑鼠位置的方向
            Vector3 directionToMouse = (mouseWorldPosition - _segments[0].position).normalized;

            // 忽略 Y 軸，只在水平面上移動
            directionToMouse.y = 0;

            if (directionToMouse != Vector3.zero)
            {
                _targetDirection = directionToMouse;
            }

            // Debug 用：畫一條線顯示目標位置
            Debug.DrawLine(_segments[0].position, mouseWorldPosition, Color.green);
        }
    }

    private void MoveHead()
    {
        // 平滑轉向目標方向
        Vector3 currentForward = _segments[0].forward;
        Vector3 newForward = Vector3.Slerp(currentForward, _targetDirection, _rotationSpeed * Time.deltaTime);

        // 設定魚頭的旋轉
        if (newForward != Vector3.zero)
        {
            _segments[0].rotation = Quaternion.LookRotation(newForward);
        }

        // 往前移動
        _segments[0].position += _segments[0].forward * _speed * Time.deltaTime;
    }

    private void FollowHead()
    {
        for (int i = 1; i < _segments.Length; i++)
        {
            Vector3 targetPosition = _segments[i - 1].position;
            Vector3 currentPosition = _segments[i].position;

            Vector3 direction = (currentPosition - targetPosition).normalized;

            _segments[i].position = targetPosition + direction * _segmentDistance;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                _segments[i].rotation = targetRotation;
            }
        }
    }

    private void AddTailSwing()
    {
        for (int i = 1; i < _segments.Length; i++)
        {
            float swingMultiplier = (float)i / _segments.Length;
            float swing = Mathf.Sin(_swimTime * _tailSwingSpeed + i) * _tailSwingAmount * swingMultiplier;

            Vector3 rightDirection = _segments[i].right;
            _segments[i].position += rightDirection * swing * Time.deltaTime;
        }
    }
}