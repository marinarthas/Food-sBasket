using UnityEngine;
using UnityEngine.UI;
using System;

namespace FoodsBasketGame
{
    public enum NutrientMeterType
    {
        Glucose,
        Fats,
        Carbs,
    }

    public class NutritionMeterSystem : MonoBehaviour
    {
        [Header("Rules")]
        [SerializeField] private float maxValue = 5f;
        [SerializeField] private float capacityMultiplier = 2f;
        [SerializeField] [Range(0f, 1f)] private float baselineFillNormalized = 0.5f;
        [SerializeField] private float decayPerSecond = 0.35f;
        [SerializeField] [Range(0.05f, 1f)] private float decayScale = 0.25f;
        [SerializeField] private float depletionThreshold = 0.01f;

        [Header("UI")]
        [SerializeField] private Slider glucoseSlider;
        [SerializeField] private Slider fatsSlider;
        [SerializeField] private Slider carbsSlider;

        private float glucose;
        private float fats;
        private float carbs;

        public event Action<NutrientMeterType> MeterDepleted;

        public float Glucose => glucose;
        public float Fats => fats;
        public float Carbs => carbs;
        public float MaxValue => maxValue * Mathf.Max(1f, capacityMultiplier);
        public float BaselineValue => MaxValue * Mathf.Clamp01(baselineFillNormalized);
        public float EffectiveDecayPerSecond => Mathf.Max(0f, decayPerSecond * decayScale);

        private void Awake()
        {
            if (glucose <= 0f && fats <= 0f && carbs <= 0f)
            {
                ResetMeters();
            }
            else
            {
                RefreshUI();
            }
        }

        private void Update()
        {
            float decayAmount = EffectiveDecayPerSecond * Time.deltaTime;
            glucose = Mathf.MoveTowards(glucose, 0f, decayAmount);
            fats = Mathf.MoveTowards(fats, 0f, decayAmount);
            carbs = Mathf.MoveTowards(carbs, 0f, decayAmount);
            RefreshUI();
            CheckForDepletion();
        }

        public bool AddFood(FoodDefinition food)
        {
            float effectiveMax = MaxValue;
            float nextGlucose = glucose + food.glucosePoints;
            float nextFats = fats + food.fatsPoints;
            float nextCarbs = carbs + food.carbsPoints;
            bool overflowed = nextGlucose > effectiveMax || nextFats > effectiveMax || nextCarbs > effectiveMax;

            glucose = Mathf.Clamp(nextGlucose, 0f, effectiveMax);
            fats = Mathf.Clamp(nextFats, 0f, effectiveMax);
            carbs = Mathf.Clamp(nextCarbs, 0f, effectiveMax);
            RefreshUI();
            return overflowed;
        }

        public void ResetMeters()
        {
            RecenterMeters();
        }

        public void RecenterMeters()
        {
            float baseline = BaselineValue;
            glucose = baseline;
            fats = baseline;
            carbs = baseline;
            RefreshUI();
        }

        public void Configure(Slider glucoseMeter, Slider fatsMeter, Slider carbsMeter)
        {
            glucoseSlider = glucoseMeter;
            fatsSlider = fatsMeter;
            carbsSlider = carbsMeter;
            RefreshUI();
        }

        private void CheckForDepletion()
        {
            if (glucose <= depletionThreshold)
            {
                MeterDepleted?.Invoke(NutrientMeterType.Glucose);
                return;
            }

            if (fats <= depletionThreshold)
            {
                MeterDepleted?.Invoke(NutrientMeterType.Fats);
                return;
            }

            if (carbs <= depletionThreshold)
            {
                MeterDepleted?.Invoke(NutrientMeterType.Carbs);
            }
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
            slider.maxValue = MaxValue;
            slider.value = value;
        }
    }
}
