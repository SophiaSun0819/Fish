using UnityEngine;

public class SeaweedSpawner : MonoBehaviour
{
    [Header("生成設定")]
    [SerializeField] private GameObject _seaweedPrefab;
    [SerializeField] private int _minSeaweedsPerPoint = 3;
    [SerializeField] private int _maxSeaweedsPerPoint = 5;
    [SerializeField] private float _spawnRadius = 1f;

    private Transform[] _spawnPoints;

    private void Awake()
    {
        InitializeSpawnPoints();
    }

    private void Start()
    {
        SpawnAllSeaweeds();
    }

    /// <summary>
    /// 初始化生成點
    /// </summary>
    private void InitializeSpawnPoints()
    {
        int childCount = transform.childCount;
        _spawnPoints = new Transform[childCount];

        for (int i = 0; i < childCount; i++)
        {
            _spawnPoints[i] = transform.GetChild(i);
        }

        Debug.Log($"SeaweedSpawner: 找到 {childCount} 個生成點");
    }

    /// <summary>
    /// 將水草丟到指定位置
    /// </summary>
    public void SpawnAllSeaweeds()
    {
        if (_seaweedPrefab == null)
        {
            Debug.LogError("SeaweedSpawner: 沒有設定水草 Prefab！");
            return;
        }

        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            Debug.LogError("SeaweedSpawner: 沒有找到任何子物件作為生成點！");
            return;
        }

        int totalSpawned = 0;

        foreach (Transform spawnPoint in _spawnPoints)
        {
            if (spawnPoint != null)
            {
                // 隨機生成幾株水草
                int count = Random.Range(_minSeaweedsPerPoint, _maxSeaweedsPerPoint + 1);

                for (int i = 0; i < count; i++)
                {
                    SpawnSeaweedAt(spawnPoint);
                    totalSpawned++;
                }
            }
        }

        Debug.Log($"在 {_spawnPoints.Length} 個生成點總共生成了 {totalSpawned} 株水草！");
    }

    /// <summary>
    /// 設定水草的隨機生成位置
    /// </summary>
    /// <param name="spawnPoint">生成位置</param>
    private void SpawnSeaweedAt(Transform spawnPoint)
    {
        // 在生成點周圍隨機一個位置
        Vector3 randomOffset = Random.insideUnitSphere * _spawnRadius;
        randomOffset.y = 0; // 統一高度 之後要不同高度可以刪掉

        Vector3 spawnPosition = spawnPoint.position + randomOffset;

        GameObject seaweed = Instantiate(_seaweedPrefab, spawnPosition, spawnPoint.rotation);
        seaweed.transform.parent = spawnPoint; // 設定父物件為生成點
    }

    private void OnDrawGizmos()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            int childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(child.position, _spawnRadius);
            }
        }
        else
        {
            foreach (Transform spawnPoint in _spawnPoints)
            {
                if (spawnPoint != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(spawnPoint.position, _spawnRadius);
                }
            }
        }
    }
}