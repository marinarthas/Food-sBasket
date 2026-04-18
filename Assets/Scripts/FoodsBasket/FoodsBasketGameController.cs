using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FoodsBasketGame
{
    [RequireComponent(typeof(AudioSource))]
    public class FoodsBasketGameController : MonoBehaviour
    {
        [SerializeField] private FoodSpawner foodSpawner;
        [SerializeField] private BasketZone basketZone;
        [SerializeField] private NutritionMeterSystem nutritionMeterSystem;
        [SerializeField] private Image[] heartImages;
        [SerializeField] private Text timerLabel;
        [SerializeField] private Text bestTimeLabel;
        [SerializeField] private Text highScoreNoticeLabel;
        [SerializeField] private RectTransform infoButtonRect;
        [SerializeField] private GameObject infoPanel;
        [SerializeField] private RectTransform closeInfoButtonRect;
        [SerializeField] private Text infoPanelText;
        [SerializeField] private GameObject startPanel;
        [SerializeField] private RectTransform startButtonRect;
        [SerializeField] private RectTransform highScoresButtonRect;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private RectTransform replayButtonRect;
        [SerializeField] private GameObject highScoresPanel;
        [SerializeField] private RectTransform closeHighScoresButtonRect;
        [SerializeField] private Text highScoresText;
        [SerializeField] private float despawnY = -7.4f;
        [SerializeField] private int startingHearts = 5;
        [SerializeField] private bool recenterMetersAfterHeartLoss = true;
        [Header("Audio")]
        [SerializeField] private AudioClip backgroundLoop;
        [SerializeField] [Range(0f, 1f)] private float backgroundVolume = 0.5f;
        [SerializeField] private AudioClip clickSoundEffect;
        [SerializeField] [Range(0f, 1f)] private float clickSoundVolume = 0.5f;
        [SerializeField] private AudioClip highScoreSoundEffect;
        [SerializeField] [Range(0f, 1f)] private float highScoreSoundVolume = 0.5f;

        private int heartsRemaining;
        private float elapsedTime;
        private float previousBestTime;
        private bool hasTriggeredHighScoreThisRun;
        private bool resumeAfterInfoPanel;
        private AudioSource backgroundAudioSource;
        private AudioSource soundEffectAudioSource;
        private bool hasSubscribedToNutritionEvents;

        private const string HighScoreKey = "FoodsBasket.TopTimes";
        private const int MaxHighScores = 5;
        private const string BackgroundAudioResourcePath = "Audio/Audio_background";
        private const string ClickAudioResourcePath = "Audio/audio_click";
        private const string HighScoreAudioResourcePath = "Audio/audio_highscore";

        public bool IsPlaying { get; private set; }
        public float ElapsedTime => elapsedTime;
        public float DespawnY => despawnY;
        public float CatchLineY => basketZone == null ? -3.2f : basketZone.CatchLineY;
        public float CurrentFallSpeedMultiplier
        {
            get
            {
                if (elapsedTime < 30f)
                {
                    return 0.75f;
                }

                int extraSteps = Mathf.FloorToInt((elapsedTime - 30f) / 30f) + 1;
                return Mathf.Min(1.55f, 0.75f + (extraSteps * 0.12f));
            }
        }

        private void OnEnable()
        {
            SubscribeToNutritionEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromNutritionEvents();
        }

        private void Start()
        {
            LoadAudioClipsFromResources();
            ConfigureAudioSources();

            heartsRemaining = startingHearts;
            IsPlaying = false;

            if (foodSpawner != null)
            {
                foodSpawner.Initialize(this);
            }

            if (basketZone != null)
            {
                basketZone.Initialize(this);
            }

            SubscribeToNutritionEvents();

            if (startPanel != null)
            {
                startPanel.SetActive(true);
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            if (highScoresPanel != null)
            {
                highScoresPanel.SetActive(false);
            }

            if (infoPanel != null)
            {
                infoPanel.SetActive(false);
            }

            elapsedTime = 0f;
            previousBestTime = GetBestTime();
            hasTriggeredHighScoreThisRun = false;
            RefreshHearts();
            RefreshTimerLabel();
            RefreshBestTimeLabel();
            RefreshHighScoreNotice(false);
            RefreshHighScoresDisplay();
        }

        private void Update()
        {
            if (!IsPlaying)
            {
                if (infoPanel != null && infoPanel.activeSelf && Input.GetMouseButtonDown(0))
                {
                    TryCloseInfoFromButton(Input.mousePosition);
                    return;
                }

                if (highScoresPanel != null && highScoresPanel.activeSelf && Input.GetMouseButtonDown(0))
                {
                    TryCloseHighScoresFromButton(Input.mousePosition);
                    return;
                }

                if (startPanel != null && startPanel.activeSelf && Input.GetMouseButtonDown(0))
                {
                    TryStartPanelInteraction(Input.mousePosition);
                }

                if (gameOverPanel != null && gameOverPanel.activeSelf && Input.GetMouseButtonDown(0))
                {
                    TryReplayFromButton(Input.mousePosition);
                }

                return;
            }

            elapsedTime += Time.deltaTime;
            RefreshTimerLabel();
            CheckForLiveHighScore();

            if (Input.GetMouseButtonDown(0))
            {
                if (infoButtonRect != null &&
                    RectTransformUtility.RectangleContainsScreenPoint(infoButtonRect, Input.mousePosition, null))
                {
                    ShowInfoPanel();
                    return;
                }

                TryShootAtScreenPosition(Input.mousePosition);
            }
        }

        public void OnFoodShot(FoodItem item)
        {
            PlayClickSoundEffect();
            ShotEffectFactory.CreatePopEffect(item.transform.position, item.Definition.sprite);
        }

        public void OnFoodCaught(FoodItem item)
        {
            if (nutritionMeterSystem != null)
            {
                bool overflowed = nutritionMeterSystem.AddFood(item.Definition);
                if (overflowed)
                {
                    LoseHeart();
                }
            }
        }

        public void OnFoodMissed(FoodItem item)
        {
        }

        public void CatchFood(FoodItem item)
        {
            if (basketZone != null)
            {
                basketZone.CatchFood(item);
            }
        }

        public void Configure(
            FoodSpawner spawner,
            BasketZone basket,
            NutritionMeterSystem nutrition,
            Image[] hearts,
            Text timerDisplay,
            Text bestTimeDisplay,
            Text highScoreNoticeDisplay,
            RectTransform infoButton,
            GameObject infoPopup,
            RectTransform closeInfoButton,
            Text infoText,
            GameObject startRoot,
            RectTransform startButton,
            RectTransform scoresButton,
            GameObject gameOverRoot,
            RectTransform replayButton,
            GameObject scoresPanel,
            RectTransform closeScoresButton,
            Text scoresText)
        {
            foodSpawner = spawner;
            basketZone = basket;

            if (!ReferenceEquals(nutritionMeterSystem, nutrition))
            {
                UnsubscribeFromNutritionEvents();
                nutritionMeterSystem = nutrition;
                SubscribeToNutritionEvents();
            }
            else
            {
                nutritionMeterSystem = nutrition;
            }

            heartImages = hearts;
            timerLabel = timerDisplay;
            bestTimeLabel = bestTimeDisplay;
            highScoreNoticeLabel = highScoreNoticeDisplay;
            infoButtonRect = infoButton;
            infoPanel = infoPopup;
            closeInfoButtonRect = closeInfoButton;
            infoPanelText = infoText;
            startPanel = startRoot;
            startButtonRect = startButton;
            highScoresButtonRect = scoresButton;
            gameOverPanel = gameOverRoot;
            replayButtonRect = replayButton;
            highScoresPanel = scoresPanel;
            closeHighScoresButtonRect = closeScoresButton;
            highScoresText = scoresText;
            RefreshTimerLabel();
            RefreshBestTimeLabel();
            RefreshHighScoreNotice(false);
            RefreshInfoPanelText();
            RefreshHighScoresDisplay();
        }

        public void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void StartGame()
        {
            IsPlaying = true;
            resumeAfterInfoPanel = false;
            elapsedTime = 0f;
            previousBestTime = GetBestTime();
            hasTriggeredHighScoreThisRun = false;
            RefreshTimerLabel();
            RefreshHighScoreNotice(false);
            StartBackgroundLoop();

            if (nutritionMeterSystem != null)
            {
                nutritionMeterSystem.ResetMeters();
            }

            if (startPanel != null)
            {
                startPanel.SetActive(false);
            }

            if (highScoresPanel != null)
            {
                highScoresPanel.SetActive(false);
            }

            if (infoPanel != null)
            {
                infoPanel.SetActive(false);
            }
        }

        public void ShowHighScores()
        {
            RefreshHighScoresDisplay();

            if (highScoresPanel != null)
            {
                highScoresPanel.SetActive(true);
            }
        }

        public void HideHighScores()
        {
            if (highScoresPanel != null)
            {
                highScoresPanel.SetActive(false);
            }
        }

        public void ShowInfoPanel()
        {
            RefreshInfoPanelText();

            resumeAfterInfoPanel = IsPlaying;
            if (resumeAfterInfoPanel)
            {
                IsPlaying = false;
            }

            if (infoPanel != null)
            {
                infoPanel.SetActive(true);
            }
        }

        public void HideInfoPanel()
        {
            if (infoPanel != null)
            {
                infoPanel.SetActive(false);
            }

            if (resumeAfterInfoPanel)
            {
                IsPlaying = true;
                resumeAfterInfoPanel = false;
            }
        }

        private void TryShootAtScreenPosition(Vector3 screenPosition)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
            Vector2 point = new Vector2(worldPosition.x, worldPosition.y);
            Collider2D[] hits = Physics2D.OverlapPointAll(point);

            for (int i = hits.Length - 1; i >= 0; i--)
            {
                if (hits[i] != null && hits[i].TryGetComponent(out FoodItem foodItem))
                {
                    foodItem.TryShoot();
                    return;
                }
            }
        }

        private void ConfigureAudioSources()
        {
            backgroundAudioSource = GetComponent<AudioSource>();
            if (backgroundAudioSource != null)
            {
                backgroundAudioSource.playOnAwake = false;
                backgroundAudioSource.loop = true;
                backgroundAudioSource.clip = backgroundLoop;
                backgroundAudioSource.volume = backgroundVolume;
            }

            soundEffectAudioSource = gameObject.AddComponent<AudioSource>();
            soundEffectAudioSource.playOnAwake = false;
            soundEffectAudioSource.loop = false;
            soundEffectAudioSource.volume = clickSoundVolume;
        }

        private void LoadAudioClipsFromResources()
        {
            backgroundLoop = Resources.Load<AudioClip>(BackgroundAudioResourcePath);
            clickSoundEffect = Resources.Load<AudioClip>(ClickAudioResourcePath);
            highScoreSoundEffect = Resources.Load<AudioClip>(HighScoreAudioResourcePath);
        }

        private void StartBackgroundLoop()
        {
            if (backgroundAudioSource == null || backgroundLoop == null)
            {
                return;
            }

            backgroundAudioSource.clip = backgroundLoop;
            backgroundAudioSource.volume = backgroundVolume;
            backgroundAudioSource.time = 0f;
            backgroundAudioSource.Play();
        }

        private void StopBackgroundLoop()
        {
            if (backgroundAudioSource == null)
            {
                return;
            }

            backgroundAudioSource.Stop();
            backgroundAudioSource.time = 0f;
        }

        private void PlayClickSoundEffect()
        {
            if (soundEffectAudioSource == null || clickSoundEffect == null)
            {
                return;
            }

            soundEffectAudioSource.volume = clickSoundVolume;
            soundEffectAudioSource.PlayOneShot(clickSoundEffect, clickSoundVolume);
        }

        private void PlayHighScoreSoundEffect()
        {
            if (soundEffectAudioSource == null || highScoreSoundEffect == null)
            {
                return;
            }

            soundEffectAudioSource.PlayOneShot(highScoreSoundEffect, highScoreSoundVolume);
        }

        private void LoseHeart()
        {
            if (!IsPlaying)
            {
                return;
            }

            heartsRemaining = Mathf.Max(0, heartsRemaining - 1);
            RefreshHearts();

            if (recenterMetersAfterHeartLoss && nutritionMeterSystem != null)
            {
                nutritionMeterSystem.RecenterMeters();
            }

            if (heartsRemaining <= 0)
            {
                GameOver();
            }
        }

        private void HandleMeterDepleted(NutrientMeterType depletedMeter)
        {
            if (!IsPlaying)
            {
                return;
            }

            LoseHeart();
        }

        private void SubscribeToNutritionEvents()
        {
            if (nutritionMeterSystem == null || hasSubscribedToNutritionEvents)
            {
                return;
            }

            nutritionMeterSystem.MeterDepleted += HandleMeterDepleted;
            hasSubscribedToNutritionEvents = true;
        }

        private void UnsubscribeFromNutritionEvents()
        {
            if (nutritionMeterSystem == null || !hasSubscribedToNutritionEvents)
            {
                return;
            }

            nutritionMeterSystem.MeterDepleted -= HandleMeterDepleted;
            hasSubscribedToNutritionEvents = false;
        }

        private void RefreshHearts()
        {
            if (heartImages == null)
            {
                return;
            }

            for (int i = 0; i < heartImages.Length; i++)
            {
                if (heartImages[i] != null)
                {
                    heartImages[i].enabled = i < heartsRemaining;
                }
            }
        }

        private void GameOver()
        {
            IsPlaying = false;
            resumeAfterInfoPanel = false;
            StopBackgroundLoop();
            SaveHighScore(elapsedTime);
            RefreshBestTimeLabel();
            RefreshHighScoresDisplay();

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }

        private void TryStartPanelInteraction(Vector2 screenPosition)
        {
            if (startButtonRect == null)
            {
                StartGame();
                return;
            }

            if (RectTransformUtility.RectangleContainsScreenPoint(startButtonRect, screenPosition, null))
            {
                StartGame();
                return;
            }

            if (highScoresButtonRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(highScoresButtonRect, screenPosition, null))
            {
                ShowHighScores();
                return;
            }

            if (infoButtonRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(infoButtonRect, screenPosition, null))
            {
                ShowInfoPanel();
            }
        }

        private void TryReplayFromButton(Vector2 screenPosition)
        {
            if (replayButtonRect == null)
            {
                RestartGame();
                return;
            }

            if (RectTransformUtility.RectangleContainsScreenPoint(replayButtonRect, screenPosition, null))
            {
                RestartGame();
            }
        }

        private void TryCloseHighScoresFromButton(Vector2 screenPosition)
        {
            if (closeHighScoresButtonRect == null)
            {
                HideHighScores();
                return;
            }

            if (RectTransformUtility.RectangleContainsScreenPoint(closeHighScoresButtonRect, screenPosition, null))
            {
                HideHighScores();
            }
        }

        private void TryCloseInfoFromButton(Vector2 screenPosition)
        {
            if (closeInfoButtonRect == null)
            {
                HideInfoPanel();
                return;
            }

            if (RectTransformUtility.RectangleContainsScreenPoint(closeInfoButtonRect, screenPosition, null))
            {
                HideInfoPanel();
            }
        }

        private void RefreshTimerLabel()
        {
            if (timerLabel != null)
            {
                timerLabel.text = "Time: " + FormatTime(elapsedTime);
            }
        }

        private void CheckForLiveHighScore()
        {
            if (hasTriggeredHighScoreThisRun)
            {
                return;
            }

            if (previousBestTime <= 0f)
            {
                return;
            }

            if (elapsedTime < previousBestTime)
            {
                return;
            }

            hasTriggeredHighScoreThisRun = true;
            RefreshHighScoreNotice(true);
            PlayHighScoreSoundEffect();
        }

        private void RefreshBestTimeLabel()
        {
            if (bestTimeLabel == null)
            {
                return;
            }

            float bestTime = GetBestTime();
            bestTimeLabel.text = bestTime > 0f ? "Best: " + FormatTime(bestTime) : "Best: --:--";
        }

        private void RefreshHighScoreNotice(bool isVisible)
        {
            if (highScoreNoticeLabel == null)
            {
                return;
            }

            highScoreNoticeLabel.text = isVisible ? "HIGH SCORE!" : string.Empty;
        }

        private void RefreshInfoPanelText()
        {
            if (infoPanelText != null)
            {
                infoPanelText.text = "Everything in your basket counts! Keep glucose, fats, and carbs near the middle as they slowly drain over time. Let the right foods fall in so the bars do not drop too low, but do not let them spike and overflow either. How long can you keep the balance?";
            }
        }

        private void RefreshHighScoresDisplay()
        {
            if (highScoresText == null)
            {
                return;
            }

            List<float> scores = LoadHighScores();
            if (scores.Count == 0)
            {
                highScoresText.text = "No times recorded yet.";
                return;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < scores.Count; i++)
            {
                if (i > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(i + 1);
                builder.Append(". ");
                builder.Append(FormatTime(scores[i]));
            }

            highScoresText.text = builder.ToString();
        }

        private void SaveHighScore(float timeValue)
        {
            List<float> scores = LoadHighScores();
            scores.Add(timeValue);
            scores.Sort((left, right) => right.CompareTo(left));

            if (scores.Count > MaxHighScores)
            {
                scores.RemoveRange(MaxHighScores, scores.Count - MaxHighScores);
            }

            PlayerPrefs.SetString(HighScoreKey, SerializeHighScores(scores));
            PlayerPrefs.Save();
        }

        private List<float> LoadHighScores()
        {
            List<float> scores = new List<float>();
            string serialized = PlayerPrefs.GetString(HighScoreKey, string.Empty);
            if (string.IsNullOrWhiteSpace(serialized))
            {
                return scores;
            }

            string[] parts = serialized.Split('|');
            for (int i = 0; i < parts.Length; i++)
            {
                if (float.TryParse(parts[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value))
                {
                    scores.Add(value);
                }
            }

            scores.Sort((left, right) => right.CompareTo(left));
            if (scores.Count > MaxHighScores)
            {
                scores.RemoveRange(MaxHighScores, scores.Count - MaxHighScores);
            }

            return scores;
        }

        private float GetBestTime()
        {
            List<float> scores = LoadHighScores();
            return scores.Count > 0 ? scores[0] : 0f;
        }

        private static string SerializeHighScores(List<float> scores)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < scores.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append('|');
                }

                builder.Append(scores[i].ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private static string FormatTime(float timeValue)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(timeValue));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:00}:{seconds:00}";
        }
    }
}
