using UnityEngine;

/// <summary>
/// NPC 魚的抽象基礎類別
/// 包含 AI 相關的共同功能和身體動畫整合
/// </summary>
public abstract class NPCFish : BaseFish
{
    [Header("身體動畫")]
    [SerializeField] protected FishBodyAnimation _bodyAnimation;
    [Tooltip("如果沒有手動指定，是否自動尋找 FishBodyAnimation 組件")]
    [SerializeField] protected bool _autoFindBodyAnimation = true;

    [Header("AI 設定")]
    [SerializeField] protected float _detectionRange = 5f; // 偵測範圍
    [SerializeField] protected float _eatRange = 1.5f; // 吃東西的範圍

    protected Transform _currentTarget; // 當前目標
    protected Transform _headTransform; // 頭部節點（用於移動）

    protected override void Start()
    {
        base.Start();
        InitializeBodyAnimation();
    }

    /// <summary>
    /// 初始化身體動畫組件
    /// </summary>
    protected virtual void InitializeBodyAnimation()
    {
        if (_bodyAnimation == null && _autoFindBodyAnimation)
        {
            // 嘗試在自己或子物件上尋找
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
            Debug.Log($"[NPCFish] {gameObject.name}: 身體動畫組件已初始化，頭部節點: {_headTransform?.name}");
        }
        else
        {
            // 如果沒有身體動畫，就用自己的 transform 作為頭部
            _headTransform = transform;
        }
    }

    protected virtual void Update()
    {
        FindTarget();
        Move();
        TryEat();
    }

    /// <summary>
    /// 抽象方法：尋找目標
    /// </summary>
    protected abstract void FindTarget();

    /// <summary>
    /// 試著吃東西
    /// </summary>
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

    protected override void Move()
    {
        if (_currentTarget == null) return;

        // 計算移動方向
        Vector3 direction = (_currentTarget.position - GetFishPosition()).normalized;
        direction.y = 0; // 保持在水平面移動

        // 移動頭部
        SetFishPosition(GetFishPosition() + direction * _moveSpeed * Time.deltaTime);

        // 旋轉頭部
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            SetFishRotation(Quaternion.RotateTowards(
                GetFishRotation(),
                targetRotation,
                _turnSpeed * Time.deltaTime
            ));
        }
    }

    /// <summary>
    /// 移動到指定目標位置（供子類別使用）
    /// </summary>
    protected void MoveToPosition(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - GetFishPosition()).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            // 移動頭部
            SetFishPosition(GetFishPosition() + direction * _moveSpeed * Time.deltaTime);

            // 旋轉頭部
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            SetFishRotation(Quaternion.RotateTowards(
                GetFishRotation(),
                targetRotation,
                _turnSpeed * Time.deltaTime
            ));
        }
    }

    /// <summary>
    /// 取得身體動畫組件（供子類別使用）
    /// </summary>
    protected FishBodyAnimation GetBodyAnimation()
    {
        return _bodyAnimation;
    }

    /// <summary>
    /// 檢查是否有身體動畫
    /// </summary>
    protected bool HasBodyAnimation()
    {
        return _bodyAnimation != null;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        // 使用頭部位置來畫 Gizmos
        Vector3 gizmoPosition = Application.isPlaying ? GetFishPosition() : transform.position;

        // 黃色圈：偵測範圍
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(gizmoPosition, _detectionRange);

        // 紅色圈：吃東西的範圍
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(gizmoPosition, _eatRange);

        // 綠色線：指向目標
        if (_currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(gizmoPosition, _currentTarget.position);
        }
    }
}
