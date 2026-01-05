using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    //public TextMeshPro scoreText;
    public TMPro.TextMeshProUGUI scoreText;
    
    private int totalScore = 0;

    void Awake()
    {
        // ³æ¨Ò¼Ò¦¡
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        UpdateScoreUI();
    }

    public void AddScore(int points)
    {
        totalScore += points;
        UpdateScoreUI();
    }

    public int GetScore()
    {
        return totalScore;
    }

    public void ResetScore()
    {
        totalScore = 0;
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + totalScore;
        }
    }
}
