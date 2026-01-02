using UnityEngine;

/// <summary>
/// 素食魚
/// 兩種主要行為模式
/// 1.漫遊(移動速度慢)
/// 2.吃水草
/// </summary>
public class HerbivoreFish : NPCFish
{
    [Header("素食魚特性")]
    [SerializeField]
    private float _idleTime = 2f; // 吃完的休息時間
    [SerializeField]
    private float _wanderTime = 5f; // 漫遊時間
    [SerializeField]
    private float _wanderRadius = 10f; // 漫遊範圍
    [SerializeField]
    private float _hungerThreshold = 3f; // 多久會餓

    private float _stateTimer = 0f; // 狀態計時器
    private float _hungerTimer = 0f; // 飢餓計時器
    private FishState _currentState = FishState.Wandering;
    private Vector3 _wanderTarget; // 漫遊目標點

    /// <summary>
    /// 魚的狀態
    /// </summary>
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
        _stateTimer = _wanderTime; // 初始化漫遊超時計時器
    }

    protected override void Update()
    {
        // 更新飢餓計時器
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

    /// <summary>
    /// 漫遊的狀態
    /// </summary>
    private void UpdateWandering()
    {
        // 如果餓了，切換到尋找食物狀態
        if (_hungerTimer <= 0)
        {
            ChangeState(FishState.Seeking);
            return;
        }

        MoveToTarget(_wanderTarget);

        // 更新狀態計時器（用於超時檢測）
        _stateTimer -= Time.deltaTime;

        // 到達目標點或超時就設定新的漫遊點
        float distance = Vector3.Distance(GetFishPosition(), _wanderTarget);
        if (distance < 1f || _stateTimer <= 0)
        {
            SetNewWanderTarget();
            _stateTimer = _wanderTime; // 重置超時計時器
        }
    }

    private void UpdateSeeking()
    {
        FindTarget();

        if (_currentTarget != null)
        {
            Move();
            TryEat();
        }
        else
        {
            // 找不到食物，回到漫遊
            ChangeState(FishState.Wandering);
        }
    }

    private void UpdateEating()
    {
        // 繼續吃
        TryEat();
    }

    private void UpdateResting()
    {
        _stateTimer -= Time.deltaTime;

        if (_stateTimer <= 0)
        {
            // 休息結束，開始漫遊
            ChangeState(FishState.Wandering);
        }
    }

    /// <summary>
    /// 切換魚的狀態
    /// </summary>
    /// <param name="newState"></param>
    private void ChangeState(FishState newState)
    {
        _currentState = newState;

        switch (newState)
        {
            case FishState.Wandering:
                SetNewWanderTarget();
                _stateTimer = _wanderTime; // 設定漫遊超時時間
                Debug.Log($"{gameObject.name}: 開始漫遊");
                break;
            case FishState.Seeking:
                Debug.Log($"{gameObject.name}: 尋找食物");
                break;
            case FishState.Eating:
                Debug.Log($"{gameObject.name}: 開始進食");
                break;
            case FishState.Resting:
                _stateTimer = _idleTime;
                Debug.Log($"{gameObject.name}: 休息中");
                break;
        }
    }

    /// <summary>
    /// 設定新的漫遊位置
    /// </summary>
    private void SetNewWanderTarget()
    {
        // 在當前位置附近隨機一個點
        Vector3 currentPos = GetFishPosition();
        Vector2 randomCircle = Random.insideUnitCircle * _wanderRadius;
        _wanderTarget = currentPos + new Vector3(randomCircle.x, 0, randomCircle.y);
        _wanderTarget.y = currentPos.y; // 保持相同高度
    }

    private void MoveToTarget(Vector3 target)
    {
        // 使用父類別的 MoveToPosition 方法來移動頭部節點
        MoveToPosition(target);
    }

    /// <summary>
    /// 尋找水草
    /// </summary>
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
                Debug.Log($"{gameObject.name} 吃了一口水草！");

                // 重置飢餓計時器
                _hungerTimer = _hungerThreshold;

                // 有機率吃飽就切換到休息狀態
                if (Random.value > 0.5f) // 50% 機率吃飽
                {
                    _currentTarget = null;
                    ChangeState(FishState.Resting);
                }
            }
        }
        else
        {
            // 水草沒了 清空目標
            _currentTarget = null;
            ChangeState(FishState.Seeking);
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // 使用魚的實際位置
        Vector3 fishPos = Application.isPlaying ? GetFishPosition() : transform.position;

        // 藍色圈：漫遊範圍
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(fishPos, _wanderRadius);

        // 粉色球：當前漫遊目標
        if (_currentState == FishState.Wandering && Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(_wanderTarget, 0.3f);
            Gizmos.DrawLine(fishPos, _wanderTarget);
        }
    }
}