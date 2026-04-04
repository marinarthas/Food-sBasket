using UnityEngine;

namespace FoodsBasketGame
{
    public class BasketZone : MonoBehaviour
    {
        [Header("Basket Rules")]
        [SerializeField] private int overflowStartsAt = 10;
        [SerializeField] private int resetAtCount = 15;

        private FoodsBasketGameController controller;
        private int storedFoodCount;
        private SpriteRenderer basketRenderer;
        private BoxCollider2D basketCollider;

        public int StoredFoodCount => storedFoodCount;
        public int OverflowStartsAt => overflowStartsAt;
        public int ResetAtCount => resetAtCount;

        public float CatchLineY
        {
            get
            {
                if (basketCollider != null)
                {
                    Bounds bounds = basketCollider.bounds;
                    return bounds.min.y + (bounds.size.y * 0.52f);
                }

                return transform.position.y + 1f;
            }
        }

        private void Awake()
        {
            basketRenderer = GetComponent<SpriteRenderer>();
            basketCollider = GetComponent<BoxCollider2D>();
        }

        public void Initialize(FoodsBasketGameController gameController)
        {
            controller = gameController;
        }

        public void CatchFood(FoodItem item)
        {
            Vector3 basketPosition = GetSlotPosition(storedFoodCount);
            int sortingOrder = GetSortingOrder(storedFoodCount);
            item.PlaceInBasket(basketPosition, transform, sortingOrder);
            storedFoodCount++;

            if (controller != null)
            {
                controller.OnFoodCaught(item);
            }

            if (storedFoodCount >= resetAtCount)
            {
                ResetBasketContents();
            }
        }

        private Vector3 GetSlotPosition(int index)
        {
            if (index < overflowStartsAt)
            {
                return GetInteriorSlotPosition(index);
            }

            return GetOverflowSlotPosition(index - overflowStartsAt);
        }

        private Vector3 GetInteriorSlotPosition(int index)
        {
            int columns = 5;
            int row = index / columns;
            int column = index % columns;

            float xStart = -1.45f;
            float xStep = 0.72f;
            float yBase = -0.55f;
            float yStep = 0.07f;

            float x = xStart + (column * xStep);
            float y = yBase + (row * yStep);
            return new Vector3(x, y, 0f);
        }

        private Vector3 GetOverflowSlotPosition(int overflowIndex)
        {
            int columns = 5;
            int row = overflowIndex / columns;
            int column = overflowIndex % columns;

            float xStart = -1.45f;
            float xStep = 0.72f;
            float yBase = -0.06f;
            float yStep = 0.07f;

            float x = xStart + (column * xStep);
            float y = yBase + (row * yStep);
            return new Vector3(x, y, 0f);
        }

        private int GetSortingOrder(int index)
        {
            if (basketRenderer == null)
            {
                return 0;
            }

            return basketRenderer.sortingOrder - 1;
        }

        private void ResetBasketContents()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                FoodItem foodItem = child.GetComponent<FoodItem>();
                if (foodItem != null)
                {
                    Destroy(child.gameObject);
                }
            }

            storedFoodCount = 0;
        }

        private void OnValidate()
        {
            overflowStartsAt = Mathf.Max(0, overflowStartsAt);
            resetAtCount = Mathf.Max(1, resetAtCount);

            if (resetAtCount < overflowStartsAt)
            {
                resetAtCount = overflowStartsAt;
            }
        }
    }
}
