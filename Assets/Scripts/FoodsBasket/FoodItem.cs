using UnityEngine;

namespace FoodsBasketGame
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class FoodItem : MonoBehaviour
    {
        private FoodDefinition definition;
        private FoodsBasketGameController controller;
        private bool resolved;
        private SpriteRenderer spriteRenderer;
        private BoxCollider2D boxCollider;

        public FoodDefinition Definition => definition;

        public void Initialize(FoodsBasketGameController gameController, FoodDefinition foodDefinition)
        {
            controller = gameController;
            definition = foodDefinition;

            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = foodDefinition.sprite;
            spriteRenderer.sortingOrder = 2;

            transform.localScale = Vector3.one * foodDefinition.visualScale;
            boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider != null && spriteRenderer.sprite != null)
            {
                boxCollider.size = spriteRenderer.sprite.bounds.size;
                boxCollider.offset = spriteRenderer.sprite.bounds.center;
            }

            gameObject.name = string.IsNullOrWhiteSpace(foodDefinition.displayName) ? "Food" : foodDefinition.displayName;
        }

        private void Update()
        {
            if (resolved || controller == null || !controller.IsPlaying)
            {
                return;
            }

            transform.position += Vector3.down * (definition.moveSpeed * Time.deltaTime);

            if (transform.position.y <= controller.CatchLineY)
            {
                resolved = true;
                controller.CatchFood(this);
                return;
            }

            if (transform.position.y < controller.DespawnY)
            {
                resolved = true;
                controller.OnFoodMissed(this);
                Destroy(gameObject);
            }
        }

        public void TryShoot()
        {
            if (resolved || controller == null || !controller.IsPlaying)
            {
                return;
            }

            resolved = true;
            controller.OnFoodShot(this);
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (resolved || controller == null || !controller.IsPlaying)
            {
                return;
            }

            if (!other.TryGetComponent(out BasketZone basketZone))
            {
                return;
            }

            resolved = true;
            basketZone.CatchFood(this);
        }

        public void PlaceInBasket(Vector3 localPosition, Transform basketTransform, int sortingOrder)
        {
            resolved = true;
            transform.SetParent(basketTransform, false);
            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.identity;

            if (boxCollider != null)
            {
                boxCollider.enabled = false;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = sortingOrder;
            }
        }
    }
}
