using UnityEngine;

/// <summary>
/// 魚身體節點跟隨動畫組件
/// 使用簡單的「跟隨前一個節點」邏輯
/// </summary>
public class FishBodyAnimation : MonoBehaviour
{
    [Header("節點設定")]
    [Tooltip("身體節點陣列，從頭到尾排列")]
    [SerializeField] private Transform[] _segments;
    
    [Tooltip("節點之間的距離")]
    [SerializeField] private float _segmentDistance = 0.5f;

    [Header("尾巴擺動設定")]
    [Tooltip("尾巴擺動速度")]
    [SerializeField] private float _tailSwingSpeed = 5f;
    
    [Tooltip("尾巴擺動幅度")]
    [SerializeField] private float _tailSwingAmount = 0.3f;

    [Header("進階設定")]
    [Tooltip("是否自動偵測子物件作為節點")]
    [SerializeField] private bool _autoDetectSegments = false;
    
    [Tooltip("節點名稱前綴（用於自動偵測）")]
    [SerializeField] private string _segmentPrefix = "Segment";

    private float _swimTime;
    private bool _isInitialized = false;

    void Start()
    {
        Initialize();
    }

    /// <summary>
    /// 初始化動畫系統
    /// </summary>
    public void Initialize()
    {
        if (_autoDetectSegments)
        {
            AutoDetectSegments();
        }

        if (_segments == null || _segments.Length == 0)
        {
            Debug.LogWarning($"[FishBodyAnimation] {gameObject.name}: 沒有設定節點！");
            return;
        }

        _isInitialized = true;
        Debug.Log($"[FishBodyAnimation] {gameObject.name}: 初始化完成，共 {_segments.Length} 個節點");
    }

    /// <summary>
    /// 自動偵測子物件中的節點
    /// </summary>
    private void AutoDetectSegments()
    {
        var segmentList = new System.Collections.Generic.List<Transform>();
        
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith(_segmentPrefix))
            {
                segmentList.Add(child);
            }
        }

        // 按名稱排序（Segment0, Segment1, Segment2...）
        segmentList.Sort((a, b) => a.name.CompareTo(b.name));
        _segments = segmentList.ToArray();

        if (_segments.Length > 0)
        {
            Debug.Log($"[FishBodyAnimation] {gameObject.name}: 自動偵測到 {_segments.Length} 個節點");
        }
    }

    void LateUpdate()
    {
        if (!_isInitialized) return;
        if (_segments == null || _segments.Length < 2) return;

        _swimTime += Time.deltaTime;
        
        // 讓每個節點跟隨前一個節點
        for (int i = 1; i < _segments.Length; i++)
        {
            FollowSegment(i);
        }
        
        // 加入尾巴擺動
        AddTailSwing();
    }

    /// <summary>
    /// 讓指定索引的節點跟隨前一個節點
    /// </summary>
    private void FollowSegment(int index)
    {
        if (_segments[index] == null || _segments[index - 1] == null) return;

        Transform current = _segments[index];
        Transform target = _segments[index - 1];

        // 計算當前節點到目標節點的方向
        Vector3 direction = current.position - target.position;
        
        // 如果距離太近，避免除以零
        if (direction.magnitude < 0.001f)
        {
            direction = -target.forward;
        }
        else
        {
            direction = direction.normalized;
        }

        // 設定位置：在目標節點後方固定距離
        current.position = target.position + direction * _segmentDistance;

        // 設定旋轉：面向目標節點（也就是面向前方）
        Vector3 lookDir = target.position - current.position;
        if (lookDir != Vector3.zero)
        {
            current.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
        }
    }

    /// <summary>
    /// 加入尾巴擺動效果
    /// </summary>
    private void AddTailSwing()
    {
        for (int i = 1; i < _segments.Length; i++)
        {
            if (_segments[i] == null) continue;

            // 越靠近尾巴，擺動越大
            float swingMultiplier = (float)i / (_segments.Length - 1);
            
            // 使用 sin 波產生左右擺動
            float swing = Mathf.Sin(_swimTime * _tailSwingSpeed - i * 0.8f) * _tailSwingAmount * swingMultiplier;

            // 沿著節點的右方向偏移
            _segments[i].position += _segments[i].right * swing * Time.deltaTime;
        }
    }

    /// <summary>
    /// 取得頭部節點
    /// </summary>
    public Transform GetHead()
    {
        if (_segments != null && _segments.Length > 0)
        {
            return _segments[0];
        }
        return transform;
    }

    /// <summary>
    /// 設定節點陣列（供外部程式碼使用）
    /// </summary>
    public void SetSegments(Transform[] segments)
    {
        _segments = segments;
        _isInitialized = segments != null && segments.Length > 0;
    }

    /// <summary>
    /// 設定擺動參數
    /// </summary>
    public void SetSwingParameters(float speed, float amount)
    {
        _tailSwingSpeed = speed;
        _tailSwingAmount = amount;
    }

    /// <summary>
    /// 設定節點距離
    /// </summary>
    public void SetSegmentDistance(float distance)
    {
        _segmentDistance = distance;
    }

    private void OnDrawGizmosSelected()
    {
        if (_segments == null || _segments.Length == 0) return;

        for (int i = 0; i < _segments.Length; i++)
        {
            if (_segments[i] == null) continue;

            // 畫出節點位置
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_segments[i].position, 0.1f);

            // 畫出節點之間的連線
            if (i > 0 && _segments[i - 1] != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(_segments[i - 1].position, _segments[i].position);
            }

            // 畫出節點的前方向
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(_segments[i].position, _segments[i].forward * 0.3f);
        }
    }
}
