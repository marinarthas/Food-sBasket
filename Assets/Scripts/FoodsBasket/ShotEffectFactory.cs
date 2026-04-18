using UnityEngine;

namespace FoodsBasketGame
{
    public static class ShotEffectFactory
    {
        public static void CreatePopEffect(Vector3 position, Sprite sourceSprite)
        {
            GameObject effectObject = new GameObject("PopEffect");
            effectObject.transform.position = position;

            ParticleSystem particleSystem = effectObject.AddComponent<ParticleSystem>();
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particleSystem.main;
            main.playOnAwake = false;
            main.duration = 0.35f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 2.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.maxParticles = 18;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.03f;
            main.startColor = new ParticleSystem.MinMaxGradient(GetEffectColor(sourceSprite), Color.white);

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.08f;

            ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 5;
            renderer.material = new Material(Shader.Find("Sprites/Default"));

            particleSystem.Emit(16);
            particleSystem.Play();

            Object.Destroy(effectObject, 0.8f);
        }

        private static Color GetEffectColor(Sprite sourceSprite)
        {
            if (sourceSprite == null || sourceSprite.texture == null || !sourceSprite.texture.isReadable)
            {
                return new Color(1f, 0.95f, 0.8f, 1f);
            }

            Texture2D texture = sourceSprite.texture;
            int x = Mathf.Clamp(texture.width / 2, 0, texture.width - 1);
            int y = Mathf.Clamp(texture.height / 2, 0, texture.height - 1);
            return texture.GetPixel(x, y);
        }
    }
}
