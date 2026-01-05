using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 飼料魚 - 群聚行為、快速移動、會逃跑
/// 行為模式：
/// 1. 群聚移動（Flocking）
/// 2. 逃跑（發現玩家或掠食者時）
/// 不吃水草也不吃其他魚，只會被吃
/// </summary>
public class PreyFish : NPCFish
{
    [Header("飼料魚特性")]
    [SerializeField] private float _normalSpeed = 3f;       // 正常速度
    [SerializeField] private float _fleeSpeed = 8f;         // 逃跑速度
    [SerializeField] private float _fleeDistance = 3f;      // 開始逃跑的距離
    [SerializeField] private float _safeDistance = 8f;      // 安全距離（超過就停止逃跑）
    
    [Header("群聚設定 (Flocking)")]
    [SerializeField] private float _neighborRadius = 4f;    // 鄰居感知半徑
    [SerializeField] private float _separationWeight = 1.5f;  // 分離權重
    [SerializeField] private float _alignmentWeight = 1f;   // 對齊權重
    [SerializeField] private float _cohesionWeight = 1f;    // 聚合權重
    [SerializeField] private float _separationDistance = 1f; // 分離距離
    
    [Header("漫遊設定")]
    [SerializeField] private float _wanderRadius = 10f;     // 漫遊範圍
    [SerializeField] private float _wanderStrength = 0.5f;  // 漫遊強度

    private Vector3 _currentVelocity;
    private Transform _threatTarget;  // 威脅目標
    private PreyState _currentState = PreyState.Schooling;
    
    // 靜態列表，用於追蹤所有飼料魚（用於群聚計算）
    private static List<PreyFish> _allPreyFish = new List<PreyFish>();

    private enum PreyState
    {
        Schooling,  // 群聚游動
        Fleeing     // 逃跑
    }

    protected override void Start()
    {
        base.Start();
        _allPreyFish.Add(this);
        _currentVelocity = transform.forward * _normalSpeed;
    }

    private void OnDestroy()
    {
        _allPreyFish.Remove(this);
    }

    protected override void Update()
    {
        // 檢查威脅
        CheckForThreats();

        switch (_currentState)
        {
            case PreyState.Schooling:
                UpdateSchooling();
                break;
            case PreyState.Fleeing:
                UpdateFleeing();
                break;
        }

        // 應用移動
        ApplyMovement();
    }

    /// <summary>
    /// 檢查附近是否有威脅
    /// </summary>
    private void CheckForThreats()
    {
        Vector3 myPos = GetFishPosition();
        float closestThreatDist = float.MaxValue;
        Transform closestThreat = null;

        Collider[] colliders = Physics.OverlapSphere(myPos, _detectionRange);
        
        foreach (Collider col in colliders)
        {
            // 檢查是不是玩家
            PlayerFishController player = col.GetComponent<PlayerFishController>();
            if (player != null)
            {
                float dist = Vector3.Distance(myPos, player.transform.position);
                if (dist < closestThreatDist)
                {
                    closestThreatDist = dist;
                    closestThreat = player.transform;
                }
                continue;
            }

            // 檢查是不是肉食魚
            CarnivoreFish carnivore = col.GetComponent<CarnivoreFish>();
            if (carnivore != null && carnivore.GetSize() >= _currentSize)
            {
                float dist = Vector3.Distance(myPos, carnivore.transform.position);
                if (dist < closestThreatDist)
                {
                    closestThreatDist = dist;
                    closestThreat = carnivore.transform;
                }
            }
        }

        _threatTarget = closestThreat;

        // 根據威脅距離決定狀態
        if (_threatTarget != null && closestThreatDist <= _fleeDistance)
        {
            if (_currentState != PreyState.Fleeing)
            {
                ChangeState(PreyState.Fleeing);
            }
        }
        else if (_currentState == PreyState.Fleeing && 
                 (_threatTarget == null || closestThreatDist > _safeDistance))
        {
            ChangeState(PreyState.Schooling);
        }
    }

    /// <summary>
    /// 群聚游動狀態
    /// </summary>
    private void UpdateSchooling()
    {
        Vector3 separation = CalculateSeparation();
        Vector3 alignment = CalculateAlignment();
        Vector3 cohesion = CalculateCohesion();
        Vector3 wander = CalculateWander();

        // 組合所有力
        Vector3 acceleration = 
            separation * _separationWeight +
            alignment * _alignmentWeight +
            cohesion * _cohesionWeight +
            wander * _wanderStrength;

        // 更新速度
        _currentVelocity += acceleration * Time.deltaTime;
        _currentVelocity = Vector3.ClampMagnitude(_currentVelocity, _normalSpeed);
        
        // 保持在水平面
        _currentVelocity.y = 0;
    }

    /// <summary>
    /// 逃跑狀態
    /// </summary>
    private void UpdateFleeing()
    {
        if (_threatTarget == null)
        {
            ChangeState(PreyState.Schooling);
            return;
        }

        // 計算逃跑方向（遠離威脅）
        Vector3 fleeDirection = (GetFishPosition() - _threatTarget.position).normalized;
        fleeDirection.y = 0;

        // 加入一點隨機性，讓逃跑路線不那麼直線
        Vector3 randomness = new Vector3(
            Random.Range(-0.3f, 0.3f),
            0,
            Random.Range(-0.3f, 0.3f)
        );
        fleeDirection = (fleeDirection + randomness).normalized;

        // 更新速度為逃跑方向
        _currentVelocity = fleeDirection * _fleeSpeed;
    }

    /// <summary>
    /// 應用移動
    /// </summary>
    private void ApplyMovement()
    {
        if (_currentVelocity.magnitude < 0.1f)
        {
            _currentVelocity = transform.forward * _normalSpeed * 0.5f;
        }

        // 移動
        SetFishPosition(GetFishPosition() + _currentVelocity * Time.deltaTime);

        // 旋轉朝向移動方向
        if (_currentVelocity != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_currentVelocity.normalized);
            SetFishRotation(Quaternion.RotateTowards(
                GetFishRotation(),
                targetRotation,
                _turnSpeed * Time.deltaTime
            ));
        }
    }

    #region Flocking 計算

    /// <summary>
    /// 計算分離力 - 避免與鄰居太靠近
    /// </summary>
    private Vector3 CalculateSeparation()
    {
        Vector3 separation = Vector3.zero;
        int count = 0;
        Vector3 myPos = GetFishPosition();

        foreach (PreyFish other in _allPreyFish)
        {
            if (other == this || other._isDead) continue;

            float distance = Vector3.Distance(myPos, other.GetFishPosition());
            
            if (distance < _separationDistance && distance > 0)
            {
                Vector3 diff = myPos - other.GetFishPosition();
                diff /= distance; // 距離越近，力越大
                separation += diff;
                count++;
            }
        }

        if (count > 0)
        {
            separation /= count;
        }

        return separation;
    }

    /// <summary>
    /// 計算對齊力 - 與鄰居方向一致
    /// </summary>
    private Vector3 CalculateAlignment()
    {
        Vector3 alignment = Vector3.zero;
        int count = 0;
        Vector3 myPos = GetFishPosition();

        foreach (PreyFish other in _allPreyFish)
        {
            if (other == this || other._isDead) continue;

            float distance = Vector3.Distance(myPos, other.GetFishPosition());
            
            if (distance < _neighborRadius)
            {
                alignment += other._currentVelocity.normalized;
                count++;
            }
        }

        if (count > 0)
        {
            alignment /= count;
            alignment = alignment.normalized;
        }

        return alignment;
    }

    /// <summary>
    /// 計算聚合力 - 向群體中心移動
    /// </summary>
    private Vector3 CalculateCohesion()
    {
        Vector3 center = Vector3.zero;
        int count = 0;
        Vector3 myPos = GetFishPosition();

        foreach (PreyFish other in _allPreyFish)
        {
            if (other == this || other._isDead) continue;

            float distance = Vector3.Distance(myPos, other.GetFishPosition());
            
            if (distance < _neighborRadius)
            {
                center += other.GetFishPosition();
                count++;
            }
        }

        if (count > 0)
        {
            center /= count;
            return (center - myPos).normalized;
        }

        return Vector3.zero;
    }

    /// <summary>
    /// 計算漫遊力 - 隨機移動增加自然感
    /// </summary>
    private Vector3 CalculateWander()
    {
        // 在當前方向周圍隨機偏移
        Vector3 wanderTarget = GetFishPosition() + transform.forward * 2f;
        wanderTarget += new Vector3(
            Random.Range(-_wanderRadius, _wanderRadius),
            0,
            Random.Range(-_wanderRadius, _wanderRadius)
        ) * 0.1f;

        return (wanderTarget - GetFishPosition()).normalized;
    }

    #endregion

    /// <summary>
    /// 切換狀態
    /// </summary>
    private void ChangeState(PreyState newState)
    {
        _currentState = newState;

        switch (newState)
        {
            case PreyState.Schooling:
                Debug.Log($"{gameObject.name}: 恢復群聚游動");
                break;
            case PreyState.Fleeing:
                Debug.Log($"{gameObject.name}: 發現威脅，逃跑！");
                break;
        }
    }

    #region 覆寫父類別方法

    protected override void FindTarget()
    {
        // 飼料魚不主動尋找目標
    }

    public override void Eat()
    {
        // 飼料魚不吃東西
    }

    /// <summary>
    /// 覆寫被吃的方法，可以加入特殊效果
    /// </summary>
    public override float OnEaten(Transform eater)
    {
        // 通知附近的同伴逃跑
        NotifyNearbyFish(eater);
        return base.OnEaten(eater);
    }

    /// <summary>
    /// 通知附近的飼料魚有危險
    /// </summary>
    private void NotifyNearbyFish(Transform threat)
    {
        foreach (PreyFish other in _allPreyFish)
        {
            if (other == this || other._isDead) continue;

            float distance = Vector3.Distance(GetFishPosition(), other.GetFishPosition());
            if (distance < _neighborRadius * 2f)
            {
                other._threatTarget = threat;
                other.ChangeState(PreyState.Fleeing);
            }
        }
    }

    #endregion

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Vector3 fishPos = Application.isPlaying ? GetFishPosition() : transform.position;

        // 紫色圈：鄰居感知半徑
        Gizmos.color = new Color(0.5f, 0f, 0.5f, 0.5f);
        Gizmos.DrawWireSphere(fishPos, _neighborRadius);

        // 粉紅色圈：逃跑距離
        Gizmos.color = new Color(1f, 0.5f, 0.5f);
        Gizmos.DrawWireSphere(fishPos, _fleeDistance);

        // 顯示速度方向
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(fishPos, _currentVelocity.normalized * 2f);
        }

        // 逃跑時顯示威脅位置
        if (Application.isPlaying && _threatTarget != null && _currentState == PreyState.Fleeing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(fishPos, _threatTarget.position);
        }
    }
}
