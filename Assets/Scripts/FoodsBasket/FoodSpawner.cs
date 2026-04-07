using System.Collections.Generic;
using UnityEngine;

namespace FoodsBasketGame
{
    public class FoodSpawner : MonoBehaviour
    {
        [SerializeField] private List<FoodDefinition> foods = new List<FoodDefinition>();
        [Header("Spawn Pacing")]
        [SerializeField] private float earlySpawnInterval = 2.2f;
        [SerializeField] private float midSpawnInterval = 1.4f;
        [SerializeField] private float baseLateSpawnInterval = 1.1f;
        [SerializeField] private float lateSpawnIntervalReductionPerWave = 0.18f;
        [SerializeField] private float minimumLateSpawnInterval = 0.45f;
        [SerializeField] private float spawnHalfWidth = 5.2f;
        [SerializeField] private float spawnY = 6.4f;

        private FoodsBasketGameController controller;
        private float timer;

        public void Initialize(FoodsBasketGameController gameController)
        {
            controller = gameController;
        }

        public void SetFoods(List<FoodDefinition> definitions)
        {
            foods = definitions;
        }

        private void Update()
        {
            if (controller == null || !controller.IsPlaying || foods == null || foods.Count == 0)
            {
                return;
            }

            timer += Time.deltaTime;
            float currentSpawnInterval = GetCurrentSpawnInterval();

            if (timer < currentSpawnInterval)
            {
                return;
            }

            timer -= currentSpawnInterval;
            SpawnFood();
        }

        private float GetCurrentSpawnInterval()
        {
            if (controller == null)
            {
                return earlySpawnInterval;
            }

            float elapsed = controller.ElapsedTime;
            if (elapsed < 30f)
            {
                return earlySpawnInterval;
            }

            if (elapsed < 120f)
            {
                return midSpawnInterval;
            }

            int lateWaves = Mathf.FloorToInt((elapsed - 120f) / 120f);
            return Mathf.Max(minimumLateSpawnInterval, baseLateSpawnInterval - (lateWaves * lateSpawnIntervalReductionPerWave));
        }

        private void SpawnFood()
        {
            FoodDefinition definition = foods[Random.Range(0, foods.Count)];
            GameObject foodObject = new GameObject(definition.displayName);
            foodObject.transform.position = new Vector3(Random.Range(-spawnHalfWidth, spawnHalfWidth), spawnY, 0f);

            BoxCollider2D collider2D = foodObject.AddComponent<BoxCollider2D>();
            collider2D.isTrigger = true;

            FoodItem item = foodObject.AddComponent<FoodItem>();
            item.Initialize(controller, definition);
        }
    }
}
