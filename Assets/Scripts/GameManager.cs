using TMPro;
using UnityEngine;
using UnityEngine.UI;




public class GameManager : MonoBehaviour
{

    [Header("????")]
    [SerializeField] private AudioSource _bgmAudioSource;
    [SerializeField] private AudioClip _normalBGM;
    [SerializeField] private AudioClip _superModeBGM;

    private PlayerFishController _player;
    private bool _wasInSuperMode = false;

    [Header("????")]
    [SerializeField] private int _totalFishCount = 20; // ??20??
    public int _eatenFishCount = 0; // ???????

    [Header("UI??")]
    [SerializeField] private GameObject _victoryUI; // ??UI??

    public static GameManager Instance;

    //public TextMeshPro scoreText;
    public TMPro.TextMeshProUGUI scoreText;
    
    public int totalScore = 0;

    void Awake()
    {
        // ????????
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

        _player = FindObjectOfType<PlayerFishController>();

        // ????BGM
        if (_bgmAudioSource != null && _normalBGM != null)
        {
            _bgmAudioSource.clip = _normalBGM;
            _bgmAudioSource.loop = true;
            _bgmAudioSource.Play();
        }

        if (_victoryUI != null)
        {
            _victoryUI.SetActive(false);
        }

        UpdateScoreUI();
    }

    void Update()
    {
        // ????????
        if (_player != null)
        {
            bool isInSuperMode = _player.IsSuperMode();

            if (isInSuperMode != _wasInSuperMode)
            {
                _wasInSuperMode = isInSuperMode;

                if (isInSuperMode)
                {
                    // ?????BGM
                    _bgmAudioSource.clip = _superModeBGM;
                    _bgmAudioSource.Play();
                }
                else
                {
                    // ?????BGM
                    _bgmAudioSource.clip = _normalBGM;
                    _bgmAudioSource.Play();
                }
            }
        }
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

    public void OnFishEaten()
    {
        _eatenFishCount++;
        Debug.Log($"???? {_eatenFishCount}/{_totalFishCount} ??");

        // ??????
        if (_eatenFishCount >= _totalFishCount)
        {
            Victory();
        }
    }

    /// <summary>
    /// ????
    /// </summary>
    private void Victory()
    {
        Debug.Log("??? ????????????????");

        // ????UI
        if (_victoryUI != null)
        {
            _victoryUI.SetActive(true);
        }

        

        // ????????
        if (GameEvents.Instance != null)
        {
            // GameEvents.Instance.PlayerVictory(); // ????????
        }
    }
}
