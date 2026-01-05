using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 飼料魚 - 群聚行為、會逃跑
/// 使用父類別的平滑避障
/// </summary>
public class PreyFish : NPCFish
{
    [Header("飼料魚特性")]
    [SerializeField] private float _normalSpeed = 3f;
    [SerializeField] private float _fleeSpeed = 6f;
    [SerializeField] private float _fleeDistance = 3f;
    [SerializeField] private float _safeDistance = 8f;
    
    [Header("群聚設定 (Flocking)")]
    [SerializeField] private float _neighborRadius = 4f;
    [SerializeField] private float _separationWeight = 1.5f;
    [SerializeField] private float _alignmentWeight = 1f;
    [SerializeField] private float _cohesionWeight = 1f;
    [SerializeField] private float _separationDistance = 1f;
    
    [Header("漫遊設定")]
    [SerializeField] private float _wanderInterval = 2f;     // 改變漫遊方向的間隔
    [SerializeField] private float _wanderStrength = 0.3f;

    private Vector3 _currentVelocity;
    private Vector3 _wanderDirection;
    private float _wanderTimer;
    private Transform _threatTarget;
    private PreyState _currentState = PreyState.Schooling;
    
    // 平滑用
    private Vector3 _smoothedVelocity;
    private Vector3 _velocityRef;
    
    private static List<PreyFish> _allPreyFish = new List<PreyFish>();

    private enum PreyState
    {
        Schooling,
        Fleeing
    }

    protected override void Start()
    {
        base.Start();
        _allPreyFish.Add(this);
        _currentVelocity = transform.forward * _normalSpeed;
        _smoothedVelocity = _currentVelocity;
        _wanderDirection = Random.insideUnitSphere;
        _wanderDirection.y = 0;
        _wanderDirection = _wanderDirection.normalized;
    }

    private void OnDestroy()
    {
        _allPreyFish.Remove(this);
    }

    protected override void Update()
    {
        if (_isDead) return;
        
        CheckForThreats();
        UpdateWanderDirection();

        switch (_currentState)
        {
            case PreyState.Schooling:
                UpdateSchooling();
                break;
            case PreyState.Fleeing:
                UpdateFleeing();
                break;
        }

        ApplyMovement();
    }

    /// <summary>
    /// 定時更新漫遊方向（避免每幀隨機造成抖動）
    /// </summary>
    private void UpdateWanderDirection()
    {
        _wanderTimer -= Time.deltaTime;
        if (_wanderTimer <= 0)
        {
            _wanderDirection = Random.insideUnitSphere;
            _wanderDirection.y = 0;
            _wanderDirection = _wanderDirection.normalized;
            _wanderTimer = _wanderInterval;
        }
    }

    private void CheckForThreats()
    {
        Vector3 myPos = GetFishPosition();
        float closestThreatDist = float.MaxValue;
        Transform closestThreat = null;

        Collider[] colliders = Physics.OverlapSphere(myPos, _detectionRange);
        
        foreach (Collider col in colliders)
        {
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

        if (_threatTarget != null && closestThreatDist <= _fleeDistance)
        {
            if (_currentState != PreyState.Fleeing)
            {
                _currentState = PreyState.Fleeing;
            }
        }
        else if (_currentState == PreyState.Fleeing && 
                 (_threatTarget == null || closestThreatDist > _safeDistance))
        {
            _currentState = PreyState.Schooling;
        }
    }

    private void UpdateSchooling()
    {
        Vector3 separation = CalculateSeparation();
        Vector3 alignment = CalculateAlignment();
        Vector3 cohesion = CalculateCohesion();

        // 計算目標速度（不再每幀隨機）
        Vector3 targetVelocity = 
            separation * _separationWeight +
            alignment * _alignmentWeight +
            cohesion * _cohesionWeight +
            _wanderDirection * _wanderStrength;

        // 如果沒有其他力量，保持當前方向
        if (targetVelocity.magnitude < 0.1f)
        {
            targetVelocity = _currentVelocity.normalized;
        }

        targetVelocity = targetVelocity.normalized * _normalSpeed;
        targetVelocity.y = 0;

        _currentVelocity = targetVelocity;
    }

    private void UpdateFleeing()
    {
        if (_threatTarget == null)
        {
            _currentState = PreyState.Schooling;
            return;
        }

        Vector3 fleeDirection = (GetFishPosition() - _threatTarget.position).normalized;
        fleeDirection.y = 0;

        _currentVelocity = fleeDirection * _fleeSpeed;
    }

    private void ApplyMovement()
    {
        // 加入避障
        Vector3 avoidDirection = CalculateAvoidDirection();
        Vector3 desiredVelocity = _currentVelocity;
        
        if (avoidDirection != Vector3.zero)
        {
            desiredVelocity = (_currentVelocity.normalized + avoidDirection * _avoidStrength).normalized;
            float speed = _currentState == PreyState.Fleeing ? _fleeSpeed : _normalSpeed;
            desiredVelocity *= speed;
        }

        // 平滑速度變化（這是關鍵！）
        _smoothedVelocity = Vector3.SmoothDamp(
            _smoothedVelocity,
            desiredVelocity,
            ref _velocityRef,
            _smoothTime
        );

        // 移動
        SetFishPosition(GetFishPosition() + _smoothedVelocity * Time.deltaTime);

        // 旋轉（使用平滑後的速度）
        if (_smoothedVelocity.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_smoothedVelocity.normalized);
            SetFishRotation(Quaternion.RotateTowards(
                GetFishRotation(),
                targetRotation,
                _turnSpeed * Time.deltaTime
            ));
        }
    }

    #region Flocking

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
                diff /= distance;
                separation += diff;
                count++;
            }
        }

        if (count > 0) separation /= count;
        return separation;
    }

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
                alignment += other._smoothedVelocity.normalized;  // 使用平滑後的速度
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

    #endregion

    #region Override Methods

    protected override void FindTarget() { }

    public override void Eat() { }

    public override float OnEaten(Transform eater)
    {
        NotifyNearbyFish(eater);
        return base.OnEaten(eater);
    }

    private void NotifyNearbyFish(Transform threat)
    {
        foreach (PreyFish other in _allPreyFish)
        {
            if (other == this || other._isDead) continue;

            float distance = Vector3.Distance(GetFishPosition(), other.GetFishPosition());
            if (distance < _neighborRadius * 2f)
            {
                other._threatTarget = threat;
                other._currentState = PreyState.Fleeing;
            }
        }
    }

    #endregion

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Vector3 fishPos = Application.isPlaying ? GetFishPosition() : transform.position;

        Gizmos.color = new Color(0.5f, 0f, 0.5f, 0.5f);
        Gizmos.DrawWireSphere(fishPos, _neighborRadius);

        Gizmos.color = new Color(1f, 0.5f, 0.5f);
        Gizmos.DrawWireSphere(fishPos, _fleeDistance);

        if (Application.isPlaying)
        {
            // 顯示平滑後的速度方向
            Gizmos.color = Color.green;
            Gizmos.DrawRay(fishPos, _smoothedVelocity.normalized * 2f);
        }

        if (Application.isPlaying && _threatTarget != null && _currentState == PreyState.Fleeing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(fishPos, _threatTarget.position);
        }
    }
}
