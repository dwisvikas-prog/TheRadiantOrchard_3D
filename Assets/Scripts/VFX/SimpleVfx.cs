using UnityEngine;

namespace RadiantOrchard
{
    // Lightweight, asset-free particle bursts (no imported VFX Graphs/textures
    // needed) so the core loop has actual particle feedback instead of zero
    // ParticleSystems in the scene. Swap for authored VFX later — call sites
    // (FruitHarvester, StickmanController, QuizManager) don't need to change.
    public static class SimpleVfx
    {
        private static Material sharedParticleMaterial;

        private static Material GetMaterial()
        {
            if (sharedParticleMaterial != null) return sharedParticleMaterial;
            bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
            var shader = Shader.Find(isURP ? "Universal Render Pipeline/Particles/Unlit" : "Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            sharedParticleMaterial = new Material(shader) { enableInstancing = true };
            return sharedParticleMaterial;
        }

        public static void Burst(Vector3 position, Color color, int count = 16, float speed = 3f, float size = 0.18f, float lifetime = 0.8f)
        {
            var go = new GameObject("VFX_Burst");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = GetMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Play();
        }

        // Expanding ring for Peace / nervous-system heal (proposal "wave rings").
        public static void Ring(Vector3 position, Color color, float radius = 0.5f, int count = 20)
        {
            var go = new GameObject("VFX_Ring");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.9f;
            main.startSpeed = 2.8f;
            main.startSize = 0.14f;
            main.startColor = color;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation3D = true;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.arc = 360f;
            shape.rotation = new Vector3(90f, 0f, 0f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = GetMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Play();
        }
    }
}
