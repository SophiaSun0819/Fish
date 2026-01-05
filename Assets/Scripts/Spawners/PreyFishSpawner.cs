using UnityEngine;

/// <summary>
/// 飼料魚生成器
/// 類似水草生成器，可以指定生成範圍和數量
/// </summary>
public class PreyFishSpawner : MonoBehaviour
{
    [Header("生成設定")]
    [SerializeField] private GameObject _preyFishPrefab;  // 飼料魚 Prefab
    [SerializeField] private int _minSpawnCount = 5;      // 最少生成數量
    [SerializeField] private int _maxSpawnCount = 8;      // 最多生成數量
    
    [Header("生成範圍")]
    [SerializeField] private float _spawnRadius = 10f;    // 生成半徑
    [SerializeField] private float _spawnHeight = 0f;     // 生成高度 (Y 軸)
    [SerializeField] private bool _useBoxArea = false;    // 使用方形區域而非圓形
    [SerializeField] private Vector3 _boxSize = new Vector3(10f, 2f, 10f);  // 方形區域大小
    
    [Header("魚群設定")]
    [SerializeField] private float _minSize = 0.3f;       // 最小體型
    [SerializeField] private float _maxSize = 0.5f;       // 最大體型
    [SerializeField] private bool _randomizeSize = true;  // 是否隨機體型
    
    [Header("重生設定")]
    [SerializeField] private bool _autoRespawn = true;    // 是否自動補充
    [SerializeField] private float _respawnCheckInterval = 5f;  // 檢查間隔
    [SerializeField] private int _minAliveCount = 3;      // 最少存活數量（低於此數會補充）

    private int _currentCount = 0;
    private float _respawnTimer = 0f;

    void Start()
    {
        SpawnInitialFish();
    }

    void Update()
    {
        if (_autoRespawn)
        {
            _respawnTimer += Time.deltaTime;
            
            if (_respawnTimer >= _respawnCheckInterval)
            {
                _respawnTimer = 0f;
                CheckAndRespawn();
            }
        }
    }

    /// <summary>
    /// 生成初始魚群
    /// </summary>
    private void SpawnInitialFish()
    {
        if (_preyFishPrefab == null)
        {
            Debug.LogError("[PreyFishSpawner] 請設定飼料魚 Prefab！");
            return;
        }

        int spawnCount = Random.Range(_minSpawnCount, _maxSpawnCount + 1);
        
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnOneFish();
        }

        Debug.Log($"[PreyFishSpawner] 生成了 {spawnCount} 條飼料魚");
    }

    /// <summary>
    /// 生成一條魚
    /// </summary>
    private void SpawnOneFish()
    {
        Vector3 spawnPosition = GetRandomSpawnPosition();
        Quaternion spawnRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        GameObject fish = Instantiate(_preyFishPrefab, spawnPosition, spawnRotation, transform);
        
        // 設定隨機大小
        if (_randomizeSize)
        {
            float randomSize = Random.Range(_minSize, _maxSize);
            
            // 嘗試取得 BaseFish 組件來設定大小
            BaseFish baseFish = fish.GetComponent<BaseFish>();
            if (baseFish != null)
            {
                // 使用反射或公開方法設定大小
                fish.transform.localScale = Vector3.one * randomSize;
            }
            else
            {
                fish.transform.localScale = Vector3.one * randomSize;
            }
        }

        _currentCount++;
    }

    /// <summary>
    /// 取得隨機生成位置
    /// </summary>
    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 basePosition = transform.position;
        
        if (_useBoxArea)
        {
            // 方形區域
            float x = Random.Range(-_boxSize.x / 2f, _boxSize.x / 2f);
            float y = Random.Range(-_boxSize.y / 2f, _boxSize.y / 2f);
            float z = Random.Range(-_boxSize.z / 2f, _boxSize.z / 2f);
            
            return basePosition + new Vector3(x, y + _spawnHeight, z);
        }
        else
        {
            // 圓形區域
            Vector2 randomCircle = Random.insideUnitCircle * _spawnRadius;
            return basePosition + new Vector3(randomCircle.x, _spawnHeight, randomCircle.y);
        }
    }

    /// <summary>
    /// 檢查並補充魚群
    /// </summary>
    private void CheckAndRespawn()
    {
        // 計算當前存活的飼料魚數量
        int aliveCount = 0;
        PreyFish[] allPreyFish = GetComponentsInChildren<PreyFish>();
        
        foreach (PreyFish fish in allPreyFish)
        {
            if (fish != null && fish.gameObject.activeInHierarchy)
            {
                aliveCount++;
            }
        }

        _currentCount = aliveCount;

        // 如果低於最少數量，補充到最少數量
        if (aliveCount < _minAliveCount)
        {
            int spawnCount = _minAliveCount - aliveCount;
            
            for (int i = 0; i < spawnCount; i++)
            {
                SpawnOneFish();
            }

            Debug.Log($"[PreyFishSpawner] 補充了 {spawnCount} 條飼料魚");
        }
    }

    /// <summary>
    /// 手動生成一批魚（可由外部呼叫）
    /// </summary>
    public void SpawnBatch(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnOneFish();
        }
    }

    /// <summary>
    /// 清除所有魚
    /// </summary>
    public void ClearAllFish()
    {
        PreyFish[] allPreyFish = GetComponentsInChildren<PreyFish>();
        
        foreach (PreyFish fish in allPreyFish)
        {
            if (fish != null)
            {
                Destroy(fish.gameObject);
            }
        }

        _currentCount = 0;
    }

    private void OnDrawGizmosSelected()
    {
        // 顯示生成範圍
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        
        Vector3 center = transform.position + Vector3.up * _spawnHeight;
        
        if (_useBoxArea)
        {
            Gizmos.DrawWireCube(center, _boxSize);
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.1f);
            Gizmos.DrawCube(center, _boxSize);
        }
        else
        {
            // 畫圓形（用多條線段模擬）
            int segments = 32;
            float angleStep = 360f / segments;
            
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep * Mathf.Deg2Rad;
                float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
                
                Vector3 point1 = center + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * _spawnRadius;
                Vector3 point2 = center + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * _spawnRadius;
                
                Gizmos.DrawLine(point1, point2);
            }
        }

        // 顯示生成器位置
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
