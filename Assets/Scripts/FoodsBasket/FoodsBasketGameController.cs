using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FoodsBasketGame
{
    public class FoodsBasketGameController : MonoBehaviour
    {
        [SerializeField] private FoodSpawner foodSpawner;
        [SerializeField] private BasketZone basketZone;
        [SerializeField] private NutritionMeterSystem nutritionMeterSystem;
        [SerializeField] private Image[] heartImages;
        [SerializeField] private GameObject startPanel;
        [SerializeField] private RectTransform startButtonRect;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private RectTransform replayButtonRect;
        [SerializeField] private float despawnY = -7.4f;
        [SerializeField] private int startingHearts = 5;

        private int heartsRemaining;

        public bool IsPlaying { get; private set; }
        public float DespawnY => despawnY;
        public float CatchLineY => basketZone == null ? -3.2f : basketZone.CatchLineY;

        private void Start()
        {
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

            if (startPanel != null)
            {
                startPanel.SetActive(true);
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            RefreshHearts();
        }

        private void Update()
        {
            if (!IsPlaying)
            {
                if (startPanel != null && startPanel.activeSelf && Input.GetMouseButtonDown(0))
                {
                    TryStartFromButton(Input.mousePosition);
                }

                if (gameOverPanel != null && gameOverPanel.activeSelf && Input.GetMouseButtonDown(0))
                {
                    TryReplayFromButton(Input.mousePosition);
                }

                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryShootAtScreenPosition(Input.mousePosition);
            }
        }

        public void OnFoodShot(FoodItem item)
        {
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

        public void Configure(FoodSpawner spawner, BasketZone basket, NutritionMeterSystem nutrition, Image[] hearts, GameObject startRoot, RectTransform startButton, GameObject gameOverRoot, RectTransform replayButton)
        {
            foodSpawner = spawner;
            basketZone = basket;
            nutritionMeterSystem = nutrition;
            heartImages = hearts;
            startPanel = startRoot;
            startButtonRect = startButton;
            gameOverPanel = gameOverRoot;
            replayButtonRect = replayButton;
        }

        public void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void StartGame()
        {
            IsPlaying = true;

            if (startPanel != null)
            {
                startPanel.SetActive(false);
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

        private void LoseHeart()
        {
            if (!IsPlaying)
            {
                return;
            }

            heartsRemaining = Mathf.Max(0, heartsRemaining - 1);
            RefreshHearts();

            if (heartsRemaining <= 0)
            {
                GameOver();
            }
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

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }

        private void TryStartFromButton(Vector2 screenPosition)
        {
            if (startButtonRect == null)
            {
                StartGame();
                return;
            }

            if (RectTransformUtility.RectangleContainsScreenPoint(startButtonRect, screenPosition, null))
            {
                StartGame();
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
    }
}
