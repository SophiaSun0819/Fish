using UnityEngine;

public class PlayerFish : MonoBehaviour
{
    [Header("成長設定")]
    [SerializeField] private float _currentSize = 1f;
    [SerializeField] private float _minSize = 0.5f;
    [SerializeField] private float _maxSize = 3f;
    [SerializeField] private float _growthPerBite = 0.05f;

    [Header("吃東西設定")]
    [SerializeField] private float _eatRange = 2f;

    [Header("縮小設定")]
    [SerializeField] private float _shrinkRate = 0.02f; // 每秒縮小量
    private bool _isInPollutedWater = false; // 是否在汙染水域中


    public AudioSource eatNothingSFX;
    public AudioSource eatSeedweedSFX;

    void Start()
    {
        UpdateFishSize();
    }

    void Update()
    {
        // C鍵吃飯
        if (Input.GetKeyDown(KeyCode.C))
        {
            EatSeaweed();
        }

        // 如果在污染水中，持續縮小
        if (_isInPollutedWater)
        {
            ShrinkOverTime();
        }
    }

    /// <summary>
    /// 吃水草
    /// </summary>
    private void EatSeaweed()
    {

        

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _eatRange);

        foreach (Collider collider in hitColliders)
        {
            Seaweed seaweed = collider.GetComponent<Seaweed>();

            if (seaweed != null && seaweed.IsEatable())
            {
                //吃水草音效
                eatSeedweedSFX.Play();

                // 吃掉這株水草
                if (seaweed.IsGetEaten())
                {
                    // 主角魚吃水草會長大
                    Grow(_growthPerBite);
                    return; // 一次只吃一株
                }
            }
        }

        //沒吃到音效
        eatNothingSFX.Play();
        Debug.Log("沒有水草可以吃！");
    }

    /// <summary>
    /// 更新小魚的體積
    /// </summary>
    private void UpdateFishSize()
    {
        transform.localScale = Vector3.one * _currentSize;
    }

    private void Grow(float amount)
    {
        _currentSize += amount;
        _currentSize = Mathf.Clamp(_currentSize, _minSize, _maxSize);
        UpdateFishSize();

        Debug.Log("小魚當前大小: " + _currentSize);
    }

    private void ShrinkOverTime()
    {
        // deltaTime 代表每幀的時間，這樣縮小速度才會隨時間而非 FPS 決定
        float shrinkAmount = _shrinkRate * Time.deltaTime;

        _currentSize -= shrinkAmount;
        _currentSize = Mathf.Clamp(_currentSize, _minSize, _maxSize);

        UpdateFishSize();
    }

    public float GetCurrentSize()
    {
        return _currentSize;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _eatRange);
    }


    //汙染水池判斷
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PollutedWater"))
        {
            _isInPollutedWater = true;
            Debug.Log("小魚進入污染水池，開始縮小！");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PollutedWater"))
        {
            _isInPollutedWater = false;
            Debug.Log("小魚離開污染水池，停止縮小！");
        }
    }
}