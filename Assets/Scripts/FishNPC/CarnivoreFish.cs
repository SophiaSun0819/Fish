using UnityEngine;

/// <summary>
/// 肉食魚 - 會追逐並攻擊玩家
/// 行為模式：
/// 1. 巡邏（漫遊）
/// 2. 追逐玩家（當玩家比自己小時）
/// 3. 逃跑（當玩家比自己大時）
/// 4. 攻擊玩家
/// </summary>
public class CarnivoreFish : NPCFish, IDangerous
{
    [Header("肉食魚特性")]
    [SerializeField] private float _chaseSpeed = 5f;        // 追逐速度
    [SerializeField] private float _fleeSpeed = 6f;         // 逃跑速度
    [SerializeField] private float _attackDamage = 0.1f;    // 攻擊傷害
    [SerializeField] private float _attackCooldown = 2f;    // 攻擊冷卻
    [SerializeField] private float _sizeAdvantage = 0.2f;   // 體型優勢閾值（大多少才會追）
    
    [Header("巡邏設定")]
    [SerializeField] private float _wanderRadius = 8f;      // 漫遊範圍
    [SerializeField] private float _wanderTime = 4f;        // 漫遊間隔
    
    [Header("威脅設定")]
    [SerializeField] private float _threatLevel = 1f;       // 威脅等級
    [SerializeField] private float _aggroRange = 8f;        // 仇恨範圍（開始追逐的距離）

    private PlayerFishController _playerTarget;
    private float _lastAttackTime;
    private float _stateTimer;
    private Vector3 _wanderTarget;
    private CarnivoreState _currentState = CarnivoreState.Wandering;

    private enum CarnivoreState
    {
        Wandering,  // 漫遊
        Chasing,    // 追逐
        Attacking,  // 攻擊
        Fleeing     // 逃跑
    }

    protected override void Start()
    {
        base.Start();
        SetNewWanderTarget();
        _stateTimer = _wanderTime;
    }

    protected override void Update()
    {
        // 尋找玩家
        FindPlayer();
        
        // 根據狀態行動
        switch (_currentState)
        {
            case CarnivoreState.Wandering:
                UpdateWandering();
                break;
            case CarnivoreState.Chasing:
                UpdateChasing();
                break;
            case CarnivoreState.Attacking:
                UpdateAttacking();
                break;
            case CarnivoreState.Fleeing:
                UpdateFleeing();
                break;
        }
    }

    /// <summary>
    /// 尋找玩家
    /// </summary>
    private void FindPlayer()
    {
        if (_playerTarget == null)
        {
            // 在偵測範圍內找玩家
            Collider[] colliders = Physics.OverlapSphere(GetFishPosition(), _detectionRange);
            foreach (Collider col in colliders)
            {
                PlayerFishController player = col.GetComponent<PlayerFishController>();
                if (player != null)
                {
                    _playerTarget = player;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 漫遊狀態
    /// </summary>
    private void UpdateWandering()
    {
        // 檢查是否發現玩家
        if (_playerTarget != null)
        {
            float distanceToPlayer = Vector3.Distance(GetFishPosition(), _playerTarget.transform.position);
            
            if (distanceToPlayer <= _aggroRange)
            {
                // 比較體型決定追還是逃
                if (ShouldChasePlayer())
                {
                    ChangeState(CarnivoreState.Chasing);
                    return;
                }
                else if (ShouldFleeFromPlayer())
                {
                    ChangeState(CarnivoreState.Fleeing);
                    return;
                }
            }
        }

        // 繼續漫遊
        MoveToPosition(_wanderTarget);

        _stateTimer -= Time.deltaTime;
        float distance = Vector3.Distance(GetFishPosition(), _wanderTarget);
        
        if (distance < 1f || _stateTimer <= 0)
        {
            SetNewWanderTarget();
            _stateTimer = _wanderTime;
        }
    }

    /// <summary>
    /// 追逐狀態
    /// </summary>
    private void UpdateChasing()
    {
        if (_playerTarget == null)
        {
            ChangeState(CarnivoreState.Wandering);
            return;
        }

        // 檢查是否應該逃跑（玩家變大了）
        if (ShouldFleeFromPlayer())
        {
            ChangeState(CarnivoreState.Fleeing);
            return;
        }

        float distanceToPlayer = Vector3.Distance(GetFishPosition(), _playerTarget.transform.position);

        // 超出追逐範圍，回到漫遊
        if (distanceToPlayer > _detectionRange)
        {
            ChangeState(CarnivoreState.Wandering);
            return;
        }

        // 在攻擊範圍內，開始攻擊
        if (distanceToPlayer <= _eatRange)
        {
            ChangeState(CarnivoreState.Attacking);
            return;
        }

        // 追逐玩家
        ChaseTarget(_playerTarget.transform.position);
    }

    /// <summary>
    /// 攻擊狀態
    /// </summary>
    private void UpdateAttacking()
    {
        if (_playerTarget == null)
        {
            ChangeState(CarnivoreState.Wandering);
            return;
        }

        // 檢查是否應該逃跑
        if (ShouldFleeFromPlayer())
        {
            ChangeState(CarnivoreState.Fleeing);
            return;
        }

        float distanceToPlayer = Vector3.Distance(GetFishPosition(), _playerTarget.transform.position);

        // 玩家跑遠了，繼續追
        if (distanceToPlayer > _eatRange * 1.5f)
        {
            ChangeState(CarnivoreState.Chasing);
            return;
        }

        // 攻擊玩家
        if (Time.time - _lastAttackTime >= _attackCooldown)
        {
            Attack(_playerTarget.transform);
            _lastAttackTime = Time.time;
        }

        // 繼續靠近
        ChaseTarget(_playerTarget.transform.position);
    }

    /// <summary>
    /// 逃跑狀態
    /// </summary>
    private void UpdateFleeing()
    {
        if (_playerTarget == null)
        {
            ChangeState(CarnivoreState.Wandering);
            return;
        }

        // 檢查是否可以停止逃跑
        float distanceToPlayer = Vector3.Distance(GetFishPosition(), _playerTarget.transform.position);
        
        if (distanceToPlayer > _detectionRange)
        {
            ChangeState(CarnivoreState.Wandering);
            return;
        }

        // 如果玩家變小了，可以反過來追
        if (ShouldChasePlayer())
        {
            ChangeState(CarnivoreState.Chasing);
            return;
        }

        // 往相反方向逃跑
        FleeFromTarget(_playerTarget.transform.position);
    }

    /// <summary>
    /// 是否應該追逐玩家
    /// </summary>
    private bool ShouldChasePlayer()
    {
        if (_playerTarget == null) return false;
        return _currentSize > _playerTarget.GetCurrentSize() + _sizeAdvantage;
    }

    /// <summary>
    /// 是否應該逃離玩家
    /// </summary>
    private bool ShouldFleeFromPlayer()
    {
        if (_playerTarget == null) return false;
        return _playerTarget.GetCurrentSize() > _currentSize + _sizeAdvantage;
    }

    /// <summary>
    /// 追逐目標
    /// </summary>
    private void ChaseTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - GetFishPosition()).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            SetFishPosition(GetFishPosition() + direction * _chaseSpeed * Time.deltaTime);

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            SetFishRotation(Quaternion.RotateTowards(
                GetFishRotation(),
                targetRotation,
                _turnSpeed * Time.deltaTime
            ));
        }
    }

    /// <summary>
    /// 逃離目標
    /// </summary>
    private void FleeFromTarget(Vector3 targetPosition)
    {
        Vector3 direction = (GetFishPosition() - targetPosition).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            SetFishPosition(GetFishPosition() + direction * _fleeSpeed * Time.deltaTime);

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            SetFishRotation(Quaternion.RotateTowards(
                GetFishRotation(),
                targetRotation,
                _turnSpeed * Time.deltaTime
            ));
        }
    }

    /// <summary>
    /// 設定新的漫遊目標
    /// </summary>
    private void SetNewWanderTarget()
    {
        Vector3 currentPos = GetFishPosition();
        Vector2 randomCircle = Random.insideUnitCircle * _wanderRadius;
        _wanderTarget = currentPos + new Vector3(randomCircle.x, 0, randomCircle.y);
        _wanderTarget.y = currentPos.y;
    }

    /// <summary>
    /// 切換狀態
    /// </summary>
    private void ChangeState(CarnivoreState newState)
    {
        _currentState = newState;

        switch (newState)
        {
            case CarnivoreState.Wandering:
                SetNewWanderTarget();
                _stateTimer = _wanderTime;
                Debug.Log($"{gameObject.name}: 開始漫遊");
                break;
            case CarnivoreState.Chasing:
                Debug.Log($"{gameObject.name}: 開始追逐玩家！");
                break;
            case CarnivoreState.Attacking:
                Debug.Log($"{gameObject.name}: 攻擊玩家！");
                break;
            case CarnivoreState.Fleeing:
                Debug.Log($"{gameObject.name}: 逃跑中！");
                break;
        }
    }

    #region 覆寫父類別方法

    protected override void FindTarget()
    {
        // 肉食魚主要找玩家，這裡可以留空
        // 或者可以擴展為也會吃其他小魚
    }

    public override void Eat()
    {
        // 肉食魚不主動吃東西，而是攻擊
    }

    #endregion

    #region IDangerous Implementation

    public float GetThreatLevel()
    {
        return _threatLevel;
    }

    public float GetAttackRange()
    {
        return _eatRange;
    }

    public bool IsChasing()
    {
        return _currentState == CarnivoreState.Chasing || _currentState == CarnivoreState.Attacking;
    }

    public float Attack(Transform target)
    {
        PlayerFishController player = target.GetComponent<PlayerFishController>();
        if (player != null)
        {
            // 直接吃掉玩家，觸發 Game Over
            player.OnBeingEaten(gameObject.name);
            Debug.Log($"{gameObject.name} 吃掉了玩家！");
            
            // 清除目標
            _playerTarget = null;
            ChangeState(CarnivoreState.Wandering);
            
            return _currentSize; // 回傳自己的大小作為營養值
        }
        return 0f;
    }

    #endregion

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Vector3 fishPos = Application.isPlaying ? GetFishPosition() : transform.position;

        // 橘色圈：仇恨範圍
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(fishPos, _aggroRange);

        // 藍色圈：漫遊範圍
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(fishPos, _wanderRadius);

        // 顯示當前狀態
        if (Application.isPlaying && _currentState == CarnivoreState.Wandering)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(_wanderTarget, 0.3f);
            Gizmos.DrawLine(fishPos, _wanderTarget);
        }

        // 追逐時顯示到玩家的線
        if (Application.isPlaying && _playerTarget != null && 
            (_currentState == CarnivoreState.Chasing || _currentState == CarnivoreState.Attacking))
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(fishPos, _playerTarget.transform.position);
        }
    }
}
