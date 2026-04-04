using UnityEngine;
using UnityEngine.UI;

namespace FoodsBasketGame
{
    public class NutritionMeterSystem : MonoBehaviour
    {
        [Header("Rules")]
        [SerializeField] private float maxValue = 5f;
        [SerializeField] private float decayPerSecond = 0.35f;

        [Header("UI")]
        [SerializeField] private Slider glucoseSlider;
        [SerializeField] private Slider fatsSlider;
        [SerializeField] private Slider carbsSlider;

        private float glucose;
        private float fats;
        private float carbs;

        private void Update()
        {
            glucose = Mathf.MoveTowards(glucose, 0f, decayPerSecond * Time.deltaTime);
            fats = Mathf.MoveTowards(fats, 0f, decayPerSecond * Time.deltaTime);
            carbs = Mathf.MoveTowards(carbs, 0f, decayPerSecond * Time.deltaTime);
            RefreshUI();
        }

        public bool AddFood(FoodDefinition food)
        {
            float nextGlucose = glucose + food.glucosePoints;
            float nextFats = fats + food.fatsPoints;
            float nextCarbs = carbs + food.carbsPoints;
            bool overflowed = nextGlucose > maxValue || nextFats > maxValue || nextCarbs > maxValue;

            glucose = Mathf.Clamp(nextGlucose, 0f, maxValue);
            fats = Mathf.Clamp(nextFats, 0f, maxValue);
            carbs = Mathf.Clamp(nextCarbs, 0f, maxValue);
            RefreshUI();
            return overflowed;
        }

        public void ResetMeters()
        {
            glucose = 0f;
            fats = 0f;
            carbs = 0f;
            RefreshUI();
        }

        public void Configure(Slider glucoseMeter, Slider fatsMeter, Slider carbsMeter)
        {
            glucoseSlider = glucoseMeter;
            fatsSlider = fatsMeter;
            carbsSlider = carbsMeter;
            RefreshUI();
        }

        private void RefreshUI()
        {
            Apply(glucoseSlider, glucose);
            Apply(fatsSlider, fats);
            Apply(carbsSlider, carbs);
        }

        private void Apply(Slider slider, float value)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = 0f;
            slider.maxValue = maxValue;
            slider.value = value;
        }
    }
}
