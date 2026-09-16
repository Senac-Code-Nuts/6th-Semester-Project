using UnityEngine;

namespace PiGame.Gameplay
{
    [DisallowMultipleComponent]
    public class WesternWindDustEffect : MonoBehaviour
    {
        [Header("Rendering")]
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int orderInLayer = 10;

        [Header("Area & Count")]
        [SerializeField] private int maxParticles = 80;
        [SerializeField] private float emissionRate = 20f;
        [SerializeField] private Vector2 areaSize = new Vector2(28f, 16f);

        [Header("Wind & Appearance")]
        [SerializeField] private Vector2 horizontalSpeedRange = new Vector2(5f, 9f);
        [SerializeField] private Vector2 sizeRange = new Vector2(0.2f, 0.5f);
        [SerializeField] private Color dustColor = new Color(0.95f, 0.78f, 0.52f, 0.65f);

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
                shape.scale = new Vector3(Mathf.Max(0.1f, areaSize.x), Mathf.Max(0.1f, areaSize.y), 1f);

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
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.35f);
            Gizmos.DrawCube(transform.position, new Vector3(areaSize.x, areaSize.y, 0.1f));
            Gizmos.color = Color.yellow;
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
            main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0f);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
            main.startColor = dustColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;
            main.loop = true;

            var emission = _ps.emission;
            emission.rateOverTime = emissionRate;

            var shape = _ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(Mathf.Max(0.1f, areaSize.x), Mathf.Max(0.1f, areaSize.y), 1f);
            shape.position = Vector3.zero;
            shape.rotation = Vector3.zero;

            var velOverLifetime = _ps.velocityOverLifetime;
            velOverLifetime.enabled = true;
            velOverLifetime.x = new ParticleSystem.MinMaxCurve(horizontalSpeedRange.x, horizontalSpeedRange.y);
            velOverLifetime.y = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
            velOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var colorOverLifetime = _ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0.8f, 0.75f), new GradientAlphaKey(0f, 1f) }
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
