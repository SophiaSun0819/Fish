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
        // ?????O?_?I???P?P
        if (other.CompareTag("Star"))
        {
            CollectStar(other.gameObject);
        }
    }

    void CollectStar(GameObject star)
    {
        // ?????P?P????
        Star starComponent = star.GetComponent<Star>();
        if (starComponent != null)
        {
            // ?q??GameManager?W?[????
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(starComponent.pointValue);
            }

            // ????????????
            if (collectSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(collectSound);
            }

            // ?????????S??
            if (collectEffect != null)
            {
                Instantiate(collectEffect, star.transform.position, Quaternion.identity);
            }

            // ?P???P?P
            Destroy(star);

            // ???d?O?_?F??10???P?P?A?????L?????A
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