using UnityEngine;

/// <summary>
/// 肉食魚 - 會追逐並攻擊玩家
/// 使用射線避障
/// </summary>
public class CarnivoreFish : NPCFish, IDangerous
{
    [Header("肉食魚特性")]
    [SerializeField] private float _chaseSpeed = 5f;
    [SerializeField] private float _fleeSpeed = 6f;
    [SerializeField] private float _attackCooldown = 2f;
    [SerializeField] private float _sizeAdvantage = 0.2f;
    
    [Header("巡邏設定")]
    [SerializeField] private float _wanderRadius = 8f;
    [SerializeField] private float _wanderTime = 4f;
    
    [Header("威脅設定")]
    [SerializeField] private float _threatLevel = 1f;
    [SerializeField] private float _aggroRange = 8f;

    private PlayerFishController _playerTarget;
    private float _lastAttackTime;
    private float _stateTimer;
    private Vector3 _wanderTarget;
    private CarnivoreState _currentState = CarnivoreState.Wandering;

    private enum CarnivoreState
    {
        Wandering,
        Chasing,
        Attacking,
        Fleeing
    }

    protected override void Start()
    {
        base.Start();
        SetNewWanderTarget();
        _stateTimer = _wanderTime;
    }

    protected override void Update()
    {
        if (_isDead) return;
        
        FindPlayer();
        
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

    private void FindPlayer()
    {
        if (_playerTarget == null)
        {
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

    private void UpdateWandering()
    {
        if (_playerTarget != null)
        {
            float distanceToPlayer = Vector3.Distance(GetFishPosition(), _playerTarget.transform.position);
            
            if (distanceToPlayer <= _aggroRange)
            {
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

        // 使用父類別的 MoveToPosition（帶避障）
        MoveToPosition(_wanderTarget);

        _stateTimer -= Time.deltaTime;
        float distance = Vector3.Distance(GetFishPosition(), _wanderTarget);
        
        if (distance < 1f || _stateTimer <= 0)
        {
            SetNewWanderTarget();
            _stateTimer = _wanderTime;
        }
    }

    private void UpdateChasing()
    {
        if (_playerTarget == null)
        {
            ChangeState(CarnivoreState.Wandering);
            return;
        }

        if (ShouldFleeFromPlayer())
        {
            ChangeState(CarnivoreState.Fleeing);
            return;
        }

        float distanceToPlayer = Vector3.Distance(GetFishPosition(), _playerTarget.transform.position);

        if (distanceToPlayer > _detectionRange)
        {
            ChangeState(CarnivoreState.Wandering);
            return;
        }

        if (distanceToPlayer <= _eatRange)
        {
            ChangeState(CarnivoreState.Attacking);
            return;
        }

        // 使用父類別的 MoveToPosition（帶避障，指定速度）
        MoveToPosition(_playerTarget.transform.position, _chaseSpeed);
    }

    private void UpdateAttacking()
    {
        if (_playerTarget == null)
        {
            ChangeState(CarnivoreState.Wandering);
            return;
        }

        if (ShouldFleeFromPlayer())
        {
            ChangeState(CarnivoreState.Fleeing);
            return;
        }

        float distanceToPlayer = Vector3.Distance(GetFishPosition(), _playerTarget.transform.position);

        if (distanceToPlayer > _eatRange * 1.5f)
        {
            ChangeState(CarnivoreState.Chasing);
            return;
        }

        if (Time.time - _lastAttackTime >= _attackCooldown)
        {
            Attack(_playerTarget.transform);
            _lastAttackTime = Time.time;
        }

        MoveToPosition(_playerTarget.transform.position, _chaseSpeed);
    }

    private void UpdateFleeing()
    {
        if (_playerTarget == null)
        {
            ChangeState(CarnivoreState.Wandering);
            return;
        }

        float distanceToPlayer = Vector3.Distance(GetFishPosition(), _playerTarget.transform.position);
        
        if (distanceToPlayer > _detectionRange)
        {
            ChangeState(CarnivoreState.Wandering);
            return;
        }

        if (ShouldChasePlayer())
        {
            ChangeState(CarnivoreState.Chasing);
            return;
        }

        // 使用父類別的 MoveAwayFrom（帶避障）
        MoveAwayFrom(_playerTarget.transform.position, _fleeSpeed);
    }

    private bool ShouldChasePlayer()
    {
        if (_playerTarget == null) return false;
        if (_playerTarget.IsSuperMode()) return false; // 新增
        return _currentSize > _playerTarget.GetCurrentSize() + _sizeAdvantage;
    }
    private bool ShouldFleeFromPlayer()
    {
        if (_playerTarget == null) return false;
        if (_playerTarget.IsSuperMode()) return true; // 新增
        return _playerTarget.GetCurrentSize() > _currentSize + _sizeAdvantage;
    }

    private void SetNewWanderTarget()
    {
        Vector3 currentPos = GetFishPosition();
        Vector2 randomCircle = Random.insideUnitCircle * _wanderRadius;
        _wanderTarget = currentPos + new Vector3(randomCircle.x, 0, randomCircle.y);
        _wanderTarget.y = currentPos.y;
    }

    private void ChangeState(CarnivoreState newState)
    {
        _currentState = newState;

        if (newState == CarnivoreState.Wandering)
        {
            SetNewWanderTarget();
            _stateTimer = _wanderTime;
        }
    }

    #region Override Methods

    protected override void FindTarget() { }

    public override void Eat() { }

    #endregion

    #region IDangerous Implementation

    public float GetThreatLevel() => _threatLevel;

    public float GetAttackRange() => _eatRange;

    public bool IsChasing() => _currentState == CarnivoreState.Chasing || _currentState == CarnivoreState.Attacking;

    public float Attack(Transform target)
    {

        if (_playerTarget != null && _playerTarget.IsSuperMode())
        {
            ChangeState(CarnivoreState.Fleeing);
            return 0f;
        }
        PlayerFishController player = target.GetComponent<PlayerFishController>();
        if (player != null)
        {
            player.OnBeingEaten(gameObject.name);
            _playerTarget = null;
            ChangeState(CarnivoreState.Wandering);
            return _currentSize;
        }
        return 0f;
    }

    #endregion

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Vector3 fishPos = Application.isPlaying ? GetFishPosition() : transform.position;

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(fishPos, _aggroRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(fishPos, _wanderRadius);

        if (Application.isPlaying && _currentState == CarnivoreState.Wandering)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(_wanderTarget, 0.3f);
            Gizmos.DrawLine(fishPos, _wanderTarget);
        }

        if (Application.isPlaying && _playerTarget != null && 
            (_currentState == CarnivoreState.Chasing || _currentState == CarnivoreState.Attacking))
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(fishPos, _playerTarget.transform.position);
        }
    }
}
