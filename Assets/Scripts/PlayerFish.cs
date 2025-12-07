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

    public float GetCurrentSize()
    {
        return _currentSize;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _eatRange);
    }
}