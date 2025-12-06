using UnityEngine;

/// <summary>
/// NPC 魚的抽象基礎類別
/// 包含 AI 相關的共同功能
/// </summary>
public abstract class NPCFish : BaseFish
{
    [Header("AI 設定")]
    [SerializeField] protected float _detectionRange = 5f; // 偵測範圍
    [SerializeField] protected float _eatRange = 1.5f; // 吃東西的範圍

    protected Transform _currentTarget; // 當前目標

    protected virtual void Update()
    {
        FindTarget();
        Move();
        TryEat();
    }

    /// <summary>
    /// 抽象方法 尋找目標
    /// </summary>
    protected abstract void FindTarget();

    /// <summary>
    /// 試著吃東西
    /// </summary>
    protected virtual void TryEat()
    {
        if (_currentTarget == null) return;

        float distance = Vector3.Distance(transform.position, _currentTarget.position);

        if (distance <= _eatRange)
        {
            Eat();
        }
    }

    protected override void Move()
    {
        if (_currentTarget == null) return;

        // 計算移動方向
        Vector3 direction = (_currentTarget.position - transform.position).normalized;
        direction.y = 0; // 保持在水平面移動（可以刪）

        transform.position += direction * _moveSpeed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _turnSpeed * Time.deltaTime
            );
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        // 黃色圈：偵測範圍
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);

        // 紅色圈：吃東西的範圍
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _eatRange);

        // 綠色線：指向目標
        if (_currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _currentTarget.position);
        }
    }
}