using UnityEngine;

/// <summary>
/// 素食魚 - 吃水草、漫遊
/// 使用射線避障
/// </summary>
public class HerbivoreFish : NPCFish
{
    [Header("素食魚特性")]
    [SerializeField] private float _idleTime = 2f;
    [SerializeField] private float _wanderTime = 5f;
    [SerializeField] private float _wanderRadius = 10f;
    [SerializeField] private float _hungerThreshold = 3f;

    private float _stateTimer = 0f;
    private float _hungerTimer = 0f;
    private FishState _currentState = FishState.Wandering;
    private Vector3 _wanderTarget;

    private enum FishState
    {
        Wandering,
        Seeking,
        Eating,
        Resting
    }

    protected override void Start()
    {
        base.Start();
        SetNewWanderTarget();
        _hungerTimer = _hungerThreshold;
        _stateTimer = _wanderTime;
    }

    protected override void Update()
    {
        if (_isDead) return;
        
        _hungerTimer -= Time.deltaTime;

        switch (_currentState)
        {
            case FishState.Wandering:
                UpdateWandering();
                break;
            case FishState.Seeking:
                UpdateSeeking();
                break;
            case FishState.Eating:
                UpdateEating();
                break;
            case FishState.Resting:
                UpdateResting();
                break;
        }
    }

    private void UpdateWandering()
    {
        if (_hungerTimer <= 0)
        {
            ChangeState(FishState.Seeking);
            return;
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

    private void UpdateSeeking()
    {
        FindTarget();

        if (_currentTarget != null)
        {
            MoveToPosition(_currentTarget.position);
            TryEat();
        }
        else
        {
            ChangeState(FishState.Wandering);
        }
    }

    private void UpdateEating()
    {
        TryEat();
    }

    private void UpdateResting()
    {
        _stateTimer -= Time.deltaTime;

        if (_stateTimer <= 0)
        {
            ChangeState(FishState.Wandering);
        }
    }

    private void ChangeState(FishState newState)
    {
        _currentState = newState;

        switch (newState)
        {
            case FishState.Wandering:
                SetNewWanderTarget();
                _stateTimer = _wanderTime;
                break;
            case FishState.Resting:
                _stateTimer = _idleTime;
                break;
        }
    }

    private void SetNewWanderTarget()
    {
        Vector3 currentPos = GetFishPosition();
        Vector2 randomCircle = Random.insideUnitCircle * _wanderRadius;
        _wanderTarget = currentPos + new Vector3(randomCircle.x, 0, randomCircle.y);
        _wanderTarget.y = currentPos.y;
    }

    protected override void FindTarget()
    {
        Vector3 fishPos = GetFishPosition();
        Collider[] colliders = Physics.OverlapSphere(fishPos, _detectionRange);

        float closestDistance = Mathf.Infinity;
        Transform closestSeaweed = null;

        foreach (Collider col in colliders)
        {
            Seaweed seaweed = col.GetComponent<Seaweed>();

            if (seaweed != null && seaweed.IsEatable())
            {
                float distance = Vector3.Distance(fishPos, col.transform.position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestSeaweed = col.transform;
                }
            }
        }

        _currentTarget = closestSeaweed;
    }

    protected override void TryEat()
    {
        if (_currentTarget == null) return;

        float distance = Vector3.Distance(GetFishPosition(), _currentTarget.position);

        if (distance <= _eatRange)
        {
            if (_currentState != FishState.Eating)
            {
                ChangeState(FishState.Eating);
            }
            Eat();
        }
    }

    public override void Eat()
    {
        if (_currentTarget == null) return;

        Seaweed seaweed = _currentTarget.GetComponent<Seaweed>();

        if (seaweed != null && seaweed.IsEatable())
        {
            bool eaten = seaweed.IsGetEaten();

            if (eaten)
            {
                _hungerTimer = _hungerThreshold;

                if (Random.value > 0.5f)
                {
                    _currentTarget = null;
                    ChangeState(FishState.Resting);
                }
            }
        }
        else
        {
            _currentTarget = null;
            ChangeState(FishState.Seeking);
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Vector3 fishPos = Application.isPlaying ? GetFishPosition() : transform.position;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(fishPos, _wanderRadius);

        if (_currentState == FishState.Wandering && Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(_wanderTarget, 0.3f);
            Gizmos.DrawLine(fishPos, _wanderTarget);
        }
    }
}
