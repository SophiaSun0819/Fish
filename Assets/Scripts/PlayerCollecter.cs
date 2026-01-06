using Unity.VisualScripting;
using UnityEngine;

public class PlayerCollecter : MonoBehaviour
{
    public AudioClip collectSound;
    public GameObject collectEffect;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void OnTriggerEnter(Collider other)
    {
        // 檢測是否碰到星星
        if (other.CompareTag("Star"))
        {
            CollectStar(other.gameObject);
        }
    }

    void CollectStar(GameObject star)
    {
        // 獲取星星組件
        Star starComponent = star.GetComponent<Star>();
        if (starComponent != null)
        {
            // 通知GameManager增加分數
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(starComponent.pointValue);
            }

            // 播放收集音效
            if (collectSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(collectSound);
            }

            // 生成收集特效
            if (collectEffect != null)
            {
                Instantiate(collectEffect, star.transform.position, Quaternion.identity);
            }

            // 銷毀星星
            Destroy(star);

            // 檢查是否達到10顆星星，啟動無敵狀態
            if (GameManager.Instance != null && GameManager.Instance.GetScore() >= 10)
            {
                PlayerFishController fishController = GetComponent<PlayerFishController>();
                if (fishController != null && !fishController.IsSuperMode())
                {
                    fishController.ActivateSuperMode();
                }
            }
        }
    }
}