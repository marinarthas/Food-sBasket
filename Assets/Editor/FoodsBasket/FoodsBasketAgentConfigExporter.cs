using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FoodsBasketGame;
using UnityEditor;
using UnityEngine;

namespace FoodsBasketEditor
{
    public static class FoodsBasketAgentConfigExporter
    {
        private const string OutputRelativePath = "AgentTesting/config/foods_basket_config.json";

        [MenuItem("Tools/Foods Basket/Export Agent Config")]
        public static void ExportAgentConfig()
        {
            List<FoodDefinition> foods = BuildFoodDefinitionsFromSceneBuilder();

            GameObject controllerObject = new GameObject("FoodsBasketAgentConfigExport");
            FoodsBasketGameController controller = controllerObject.AddComponent<FoodsBasketGameController>();
            FoodSpawner spawner = controllerObject.AddComponent<FoodSpawner>();
            NutritionMeterSystem nutrition = controllerObject.AddComponent<NutritionMeterSystem>();

            FoodsBasketAgentConfig config = new FoodsBasketAgentConfig
            {
                exportedAtUtc = DateTime.UtcNow.ToString("O"),
                startingHearts = GetPrivateField(controller, "startingHearts", 5),
                maxBarValue = GetPrivateField(nutrition, "maxValue", 5f),
                decayPerSecond = GetPrivateField(nutrition, "decayPerSecond", 0.35f),
                earlySpawnInterval = GetPrivateField(spawner, "earlySpawnInterval", 2.2f),
                midSpawnInterval = GetPrivateField(spawner, "midSpawnInterval", 1.4f),
                baseLateSpawnInterval = GetPrivateField(spawner, "baseLateSpawnInterval", 1.1f),
                lateSpawnIntervalReductionPerWave = GetPrivateField(spawner, "lateSpawnIntervalReductionPerWave", 0.18f),
                minimumLateSpawnInterval = GetPrivateField(spawner, "minimumLateSpawnInterval", 0.45f),
                fallSpeedLearningDurationSeconds = 30f,
                fallSpeedStepIntervalSeconds = 30f,
                fallSpeedBaseMultiplier = 0.75f,
                fallSpeedStepDelta = 0.12f,
                fallSpeedMaxMultiplier = 1.55f,
                foods = ConvertFoods(foods)
            };

            UnityEngine.Object.DestroyImmediate(controllerObject);

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outputPath = Path.Combine(projectRoot, OutputRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, JsonUtility.ToJson(config, true));
            AssetDatabase.Refresh();
            Debug.Log("FoodsBasket agent config exported to: " + outputPath);
        }

        private static List<FoodDefinition> BuildFoodDefinitionsFromSceneBuilder()
        {
            Type builderType = typeof(FoodsBasketSceneBuilder);
            MethodInfo method = builderType.GetMethod("BuildFoodDefinitions", BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new InvalidOperationException("Could not find FoodsBasketSceneBuilder.BuildFoodDefinitions.");
            }

            object result = method.Invoke(null, null);
            if (result is List<FoodDefinition> foods)
            {
                return foods;
            }

            throw new InvalidOperationException("BuildFoodDefinitions did not return a List<FoodDefinition>.");
        }

        private static FoodConfigEntry[] ConvertFoods(List<FoodDefinition> foods)
        {
            FoodConfigEntry[] entries = new FoodConfigEntry[foods.Count];
            for (int i = 0; i < foods.Count; i++)
            {
                FoodDefinition food = foods[i];
                entries[i] = new FoodConfigEntry
                {
                    id = food.id,
                    displayName = food.displayName,
                    glucosePoints = food.glucosePoints,
                    carbsPoints = food.carbsPoints,
                    fatsPoints = food.fatsPoints,
                    moveSpeed = food.moveSpeed,
                    visualScale = food.visualScale
                };
            }

            return entries;
        }

        private static T GetPrivateField<T>(object target, string fieldName, T fallback)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                return fallback;
            }

            object rawValue = field.GetValue(target);
            if (rawValue is T value)
            {
                return value;
            }

            return fallback;
        }

        [Serializable]
        private class FoodsBasketAgentConfig
        {
            public string exportedAtUtc;
            public int startingHearts;
            public float maxBarValue;
            public float decayPerSecond;
            public float earlySpawnInterval;
            public float midSpawnInterval;
            public float baseLateSpawnInterval;
            public float lateSpawnIntervalReductionPerWave;
            public float minimumLateSpawnInterval;
            public float fallSpeedLearningDurationSeconds;
            public float fallSpeedStepIntervalSeconds;
            public float fallSpeedBaseMultiplier;
            public float fallSpeedStepDelta;
            public float fallSpeedMaxMultiplier;
            public FoodConfigEntry[] foods;
        }

        [Serializable]
        private class FoodConfigEntry
        {
            public string id;
            public string displayName;
            public float glucosePoints;
            public float carbsPoints;
            public float fatsPoints;
            public float moveSpeed;
            public float visualScale;
        }
    }
}
