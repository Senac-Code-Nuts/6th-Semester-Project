using UnityEngine;

namespace PiGame.Gameplay
{
    [DisallowMultipleComponent]
    public class HydrothermalVentParticlesEffect : MonoBehaviour
    {
        [Header("Rendering")]
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int orderInLayer = 8;

        [Header("Area & Count")]
        [SerializeField] private Vector2 areaSize = new Vector2(26f, 15f);
        [SerializeField] private int maxParticles = 70;
        [SerializeField] private float emissionRate = 14f;

        [Header("Hydrothermal Movement")]
        [SerializeField] private Vector2 upwardSpeedRange = new Vector2(2.0f, 4.5f);
        [SerializeField] private float horizontalDrift = 0.8f;
        [SerializeField] private Vector2 sizeRange = new Vector2(0.1f, 0.28f);

        [Header("Colors (Ember / Ash)")]
        [SerializeField] private Color emberColor = new Color(1f, 0.45f, 0.1f, 0.9f);
        [SerializeField] private Color ashColor = new Color(0.2f, 0.15f, 0.12f, 0.6f);

        private ParticleSystem _ps;

        private void Awake()
        {
            SetupParticleSystem();
        }

        private void OnValidate()
        {
            if (_ps == null)
            {
                _ps = GetComponent<ParticleSystem>();
            }

            if (_ps != null)
            {
                var shape = _ps.shape;
                shape.scale = new Vector3(Mathf.Max(0.1f, areaSize.x), 1f, 1f);
                shape.position = new Vector3(0f, -areaSize.y * 0.5f, 0f);

                var renderer = GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    renderer.sortingLayerName = sortingLayerName;
                    renderer.sortingOrder = orderInLayer;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.25f);
            Gizmos.DrawCube(transform.position, new Vector3(areaSize.x, areaSize.y, 0.1f));
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, areaSize.y, 0.1f));
        }

        private void SetupParticleSystem()
        {
            _ps = gameObject.GetComponent<ParticleSystem>();
            if (_ps == null)
            {
                _ps = gameObject.AddComponent<ParticleSystem>();
            }

            var main = _ps.main;
            main.maxParticles = maxParticles;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
            main.startColor = new ParticleSystem.MinMaxGradient(ashColor, emberColor);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;
            main.loop = true;

            var emission = _ps.emission;
            emission.rateOverTime = emissionRate;

            var shape = _ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(Mathf.Max(0.1f, areaSize.x), 1f, 1f);
            shape.position = new Vector3(0f, -areaSize.y * 0.5f, 0f);
            shape.rotation = Vector3.zero;

            var velOverLifetime = _ps.velocityOverLifetime;
            velOverLifetime.enabled = true;
            velOverLifetime.x = new ParticleSystem.MinMaxCurve(-horizontalDrift, horizontalDrift);
            velOverLifetime.y = new ParticleSystem.MinMaxCurve(upwardSpeedRange.x, upwardSpeedRange.y);
            velOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var colorOverLifetime = _ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.2f),
                    new GradientAlphaKey(0.8f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = grad;

            var renderer = GetComponent<ParticleSystemRenderer>();
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = orderInLayer;

            if (renderer.sharedMaterial == null)
            {
                Shader defaultShader = Shader.Find("Sprites/Default") ?? Shader.Find("Particles/Standard Unlit");
                if (defaultShader != null)
                {
                    renderer.material = new Material(defaultShader);
                }
            }
        }
    }
}
