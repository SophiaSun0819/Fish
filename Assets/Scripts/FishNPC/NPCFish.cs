using UnityEngine;

/// <summary>
/// NPC 魚的抽象基礎類別
/// 包含 AI 相關的共同功能、身體動畫整合、射線避障（帶平滑處理）
/// </summary>
public abstract class NPCFish : BaseFish
{
    [Header("身體動畫")]
    [SerializeField] protected FishBodyAnimation _bodyAnimation;
    [SerializeField] protected bool _autoFindBodyAnimation = true;

    [Header("AI 設定")]
    [SerializeField] protected float _detectionRange = 5f;
    [SerializeField] protected float _eatRange = 1.5f;

    [Header("避障設定")]
    [SerializeField] protected float _obstacleDetectDistance = 2f;
    [SerializeField] protected float _avoidStrength = 2f;           // 降低預設值
    [SerializeField] protected LayerMask _obstacleLayer;
    [SerializeField] protected int _rayCount = 5;
    [SerializeField] protected float _raySpreadAngle = 60f;
    [SerializeField] protected float _smoothTime = 0.3f;            // 平滑時間

    protected Transform _currentTarget;
    protected Transform _headTransform;
    
    // 平滑用的變數
    private Vector3 _smoothedDirection;
    private Vector3 _directionVelocity;

    protected override void Start()
    {
        base.Start();
        InitializeBodyAnimation();
        
        if (_obstacleLayer == 0)
        {
            _obstacleLayer = LayerMask.GetMask("Default");
        }
        
        // 初始化平滑方向
        _smoothedDirection = transform.forward;
    }

    protected virtual void InitializeBodyAnimation()
    {
        if (_bodyAnimation == null && _autoFindBodyAnimation)
        {
            _bodyAnimation = GetComponent<FishBodyAnimation>();
            
            if (_bodyAnimation == null)
            {
                _bodyAnimation = GetComponentInChildren<FishBodyAnimation>();
            }
        }

        if (_bodyAnimation != null)
        {
            _bodyAnimation.Initialize();
            _headTransform = _bodyAnimation.GetHead();
        }
        else
        {
            _headTransform = transform;
        }
    }

    protected virtual void Update()
    {
        if (_isDead) return;
        
        FindTarget();
        Move();
        TryEat();
    }

    protected abstract void FindTarget();

    protected virtual void TryEat()
    {
        if (_currentTarget == null) return;

        float distance = Vector3.Distance(GetFishPosition(), _currentTarget.position);

        if (distance <= _eatRange)
        {
            Eat();
        }
    }

    /// <summary>
    /// 取得魚的實際位置（頭部位置）
    /// </summary>
    protected Vector3 GetFishPosition()
    {
        return _headTransform != null ? _headTransform.position : transform.position;
    }

    /// <summary>
    /// 設定魚的位置（移動頭部）
    /// </summary>
    protected void SetFishPosition(Vector3 position)
    {
        if (_headTransform != null)
        {
            _headTransform.position = position;
        }
        else
        {
            transform.position = position;
        }
    }

    /// <summary>
    /// 設定魚的旋轉（旋轉頭部）
    /// </summary>
    protected void SetFishRotation(Quaternion rotation)
    {
        if (_headTransform != null)
        {
            _headTransform.rotation = rotation;
        }
        else
        {
            transform.rotation = rotation;
        }
    }

    /// <summary>
    /// 取得魚的旋轉
    /// </summary>
    protected Quaternion GetFishRotation()
    {
        return _headTransform != null ? _headTransform.rotation : transform.rotation;
    }

    /// <summary>
    /// 計算避障方向
    /// </summary>
    protected Vector3 CalculateAvoidDirection()
    {
        Vector3 avoidDirection = Vector3.zero;
        Vector3 fishPos = GetFishPosition();
        Vector3 forward = GetFishRotation() * Vector3.forward;
        
        float halfSpread = _raySpreadAngle / 2f;
        float angleStep = _rayCount > 1 ? _raySpreadAngle / (_rayCount - 1) : 0;
        
        int hitCount = 0;
        
        for (int i = 0; i < _rayCount; i++)
        {
            float angle = -halfSpread + (angleStep * i);
            Vector3 rayDirection = Quaternion.Euler(0, angle, 0) * forward;
            
            RaycastHit hit;
            if (Physics.Raycast(fishPos, rayDirection, out hit, _obstacleDetectDistance, _obstacleLayer))
            {
                float weight = 1f - (hit.distance / _obstacleDetectDistance);
                
                // 使用法線方向來計算閃避方向（更自然）
                Vector3 avoidDir = Vector3.Reflect(rayDirection, hit.normal);
                avoidDir.y = 0;
                avoidDirection += avoidDir.normalized * weight;
                hitCount++;
            }
        }

        if (hitCount > 0)
        {
            avoidDirection /= hitCount;
        }

        return avoidDirection.normalized;
    }

    /// <summary>
    /// 檢查前方是否有障礙物
    /// </summary>
    protected bool HasObstacleAhead()
    {
        Vector3 fishPos = GetFishPosition();
        Vector3 forward = GetFishRotation() * Vector3.forward;
        
        return Physics.Raycast(fishPos, forward, _obstacleDetectDistance, _obstacleLayer);
    }

    /// <summary>
    /// 計算最終移動方向（帶平滑處理）
    /// </summary>
    private Vector3 CalculateFinalDirection(Vector3 targetDirection)
    {
        targetDirection.y = 0;
        
        Vector3 avoidDirection = CalculateAvoidDirection();
        
        Vector3 desiredDirection;
        if (avoidDirection != Vector3.zero)
        {
            // 混合目標方向和避障方向
            desiredDirection = (targetDirection + avoidDirection * _avoidStrength).normalized;
        }
        else
        {
            desiredDirection = targetDirection;
        }

        // 使用 SmoothDamp 平滑方向變化
        _smoothedDirection = Vector3.SmoothDamp(
            _smoothedDirection, 
            desiredDirection, 
            ref _directionVelocity, 
            _smoothTime
        ).normalized;

        return _smoothedDirection;
    }

    protected override void Move()
    {
        if (_currentTarget == null) return;

        Vector3 targetDirection = (_currentTarget.position - GetFishPosition()).normalized;
        Vector3 finalDirection = CalculateFinalDirection(targetDirection);

        if (finalDirection != Vector3.zero)
        {
            SetFishPosition(GetFishPosition() + finalDirection * _moveSpeed * Time.deltaTime);

            Quaternion targetRotation = Quaternion.LookRotation(finalDirection);
            SetFishRotation(Quaternion.RotateTowards(
                GetFishRotation(),
                targetRotation,
                _turnSpeed * Time.deltaTime
            ));
        }
    }

    /// <summary>
    /// 移動到指定位置（帶避障和平滑）
    /// </summary>
    protected void MoveToPosition(Vector3 targetPosition)
    {
        Vector3 targetDirection = (targetPosition - GetFishPosition()).normalized;
        Vector3 finalDirection = CalculateFinalDirection(targetDirection);

        if (finalDirection != Vector3.zero)
        {
            SetFishPosition(GetFishPosition() + finalDirection * _moveSpeed * Time.deltaTime);

            Quaternion targetRotation = Quaternion.LookRotation(finalDirection);
            SetFishRotation(Quaternion.RotateTowards(
                GetFishRotation(),
                targetRotation,
                _turnSpeed * Time.deltaTime
            ));
        }
    }

    /// <summary>
    /// 移動到指定位置（帶避障和平滑，可指定速度）
    /// </summary>
    protected void MoveToPosition(Vector3 targetPosition, float speed)
    {
        Vector3 targetDirection = (targetPosition - GetFishPosition()).normalized;
        Vector3 finalDirection = CalculateFinalDirection(targetDirection);

        if (finalDirection != Vector3.zero)
        {
            SetFishPosition(GetFishPosition() + finalDirection * speed * Time.deltaTime);

            Quaternion targetRotation = Quaternion.LookRotation(finalDirection);
            SetFishRotation(Quaternion.RotateTowards(
                GetFishRotation(),
                targetRotation,
                _turnSpeed * Time.deltaTime
            ));
        }
    }

    /// <summary>
    /// 遠離目標位置（帶避障和平滑）
    /// </summary>
    protected void MoveAwayFrom(Vector3 targetPosition, float speed)
    {
        Vector3 awayDirection = (GetFishPosition() - targetPosition).normalized;
        Vector3 finalDirection = CalculateFinalDirection(awayDirection);

        if (finalDirection != Vector3.zero)
        {
            SetFishPosition(GetFishPosition() + finalDirection * speed * Time.deltaTime);

            Quaternion targetRotation = Quaternion.LookRotation(finalDirection);
            SetFishRotation(Quaternion.RotateTowards(
                GetFishRotation(),
                targetRotation,
                _turnSpeed * Time.deltaTime
            ));
        }
    }

    protected FishBodyAnimation GetBodyAnimation()
    {
        return _bodyAnimation;
    }

    protected bool HasBodyAnimation()
    {
        return _bodyAnimation != null;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Vector3 gizmoPosition = Application.isPlaying ? GetFishPosition() : transform.position;
        Vector3 forward = Application.isPlaying ? GetFishRotation() * Vector3.forward : transform.forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(gizmoPosition, _detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(gizmoPosition, _eatRange);

        if (_currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(gizmoPosition, _currentTarget.position);
        }

        // 繪製平滑後的方向
        if (Application.isPlaying)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawRay(gizmoPosition, _smoothedDirection * 2f);
        }

        // 繪製避障射線
        float halfSpread = _raySpreadAngle / 2f;
        float angleStep = _rayCount > 1 ? _raySpreadAngle / (_rayCount - 1) : 0;
        
        for (int i = 0; i < _rayCount; i++)
        {
            float angle = -halfSpread + (angleStep * i);
            Vector3 rayDirection = Quaternion.Euler(0, angle, 0) * forward;
            
            RaycastHit hit;
            if (Physics.Raycast(gizmoPosition, rayDirection, out hit, _obstacleDetectDistance, _obstacleLayer))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(gizmoPosition, hit.point);
                Gizmos.DrawWireSphere(hit.point, 0.1f);
            }
            else
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(gizmoPosition, rayDirection * _obstacleDetectDistance);
            }
        }
    }
}
