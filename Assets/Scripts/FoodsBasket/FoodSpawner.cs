using System.Collections.Generic;
using UnityEngine;

namespace FoodsBasketGame
{
    public class FoodSpawner : MonoBehaviour
    {
        [SerializeField] private List<FoodDefinition> foods = new List<FoodDefinition>();
        [SerializeField] private float spawnInterval = 0.9f;
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

            if (timer < spawnInterval)
            {
                return;
            }

            timer = 0f;
            SpawnFood();
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
