using UnityEngine;

namespace RadiantOrchard
{
    // Per-fruit heal VFX matching the proposal Virtue-Health Matrix
    // (heart sparks, sun flares, wave rings, etc.) — procedural so no
    // authored particle assets are required yet.
    public static class HealVfx
    {
        public static void Play(FruitData fruit, Vector3 worldPos)
        {
            var entry = FruitHealthMatrix.Resolve(fruit);
            Play(entry.healthStat, entry.themeColor, worldPos);
        }

        public static void Play(HealthStat stat, Color color, Vector3 worldPos)
        {
            Vector3 chest = worldPos + Vector3.up * 1.15f;

            switch (stat)
            {
                case HealthStat.HeartCirculation:
                    // Red heart sparks
                    SimpleVfx.Burst(chest, color, count: 28, speed: 4.5f, size: 0.2f, lifetime: 0.9f);
                    SimpleVfx.Burst(chest + Vector3.up * 0.3f, Color.white, count: 10, speed: 2f, size: 0.12f, lifetime: 0.5f);
                    break;

                case HealthStat.ImmunityEnergy:
                    // Golden sun flares
                    SimpleVfx.Burst(chest, color, count: 32, speed: 5.5f, size: 0.24f, lifetime: 1.0f);
                    SimpleVfx.Burst(chest, new Color(1f, 0.95f, 0.6f), count: 12, speed: 2.5f, size: 0.3f, lifetime: 0.7f);
                    break;

                case HealthStat.NervousSystem:
                    // Teal wave rings
                    SimpleVfx.Ring(chest, color, radius: 0.4f, count: 24);
                    SimpleVfx.Burst(chest, color, count: 16, speed: 3f, size: 0.16f, lifetime: 0.8f);
                    break;

                case HealthStat.DetoxDigestion:
                    // Bright yellow bubbles (slow float)
                    SimpleVfx.Burst(chest, color, count: 22, speed: 2.2f, size: 0.26f, lifetime: 1.2f);
                    break;

                case HealthStat.RespiratoryBreath:
                    // Soft purple mist
                    SimpleVfx.Burst(chest, new Color(color.r, color.g, color.b, 0.7f),
                        count: 36, speed: 1.8f, size: 0.32f, lifetime: 1.4f);
                    break;

                case HealthStat.BrainFocus:
                    // Blue synapse sparks
                    SimpleVfx.Burst(chest + Vector3.up * 0.35f, color, count: 30, speed: 5f, size: 0.12f, lifetime: 0.7f);
                    SimpleVfx.Burst(chest + Vector3.up * 0.35f, Color.white, count: 8, speed: 3f, size: 0.08f, lifetime: 0.4f);
                    break;

                case HealthStat.SkinGlow:
                    // Peach pastel dust
                    SimpleVfx.Burst(chest, color, count: 26, speed: 2.8f, size: 0.2f, lifetime: 1.1f);
                    SimpleVfx.Burst(chest, Color.white, count: 10, speed: 1.5f, size: 0.18f, lifetime: 0.9f);
                    break;

                case HealthStat.MuscleStrength:
                    // Yellow velocity trails
                    SimpleVfx.Burst(chest, color, count: 20, speed: 6.5f, size: 0.14f, lifetime: 0.55f);
                    SimpleVfx.Burst(chest, new Color(1f, 0.7f, 0.1f), count: 12, speed: 4f, size: 0.18f, lifetime: 0.7f);
                    break;

                case HealthStat.BoneStability:
                    // Ruby crystal shards
                    SimpleVfx.Burst(chest, color, count: 24, speed: 4f, size: 0.15f, lifetime: 0.85f);
                    SimpleVfx.Burst(chest, new Color(1f, 0.4f, 0.5f), count: 14, speed: 3f, size: 0.1f, lifetime: 0.6f);
                    break;

                default:
                    SimpleVfx.Burst(chest, color, count: 20, speed: 4f, size: 0.2f, lifetime: 0.8f);
                    break;
            }
        }
    }
}
