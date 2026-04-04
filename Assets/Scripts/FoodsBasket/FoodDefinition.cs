using System;
using UnityEngine;

namespace FoodsBasketGame
{
    [Serializable]
    public class FoodDefinition
    {
        public string id;
        public string displayName;
        public Sprite sprite;
        [Range(0f, 5f)] public float glucosePoints;
        [Range(0f, 5f)] public float carbsPoints;
        [Range(0f, 5f)] public float fatsPoints;
        [Range(0.8f, 3f)] public float moveSpeed = 2f;
        [Range(0.2f, 1.2f)] public float visualScale = 0.5f;
    }
}
