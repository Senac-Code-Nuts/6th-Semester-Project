using UnityEngine;

namespace PiGame.Gameplay
{
    [DisallowMultipleComponent]
    public class WesternWindGustEffect : MonoBehaviour
    {
        [Header("Rendering")]
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int orderInLayer = 15;

        [Header("Area")]
        [SerializeField] private Vector2 areaSize = new Vector2(25f, 12f);

        [Header("Trail Settings")]
        [SerializeField] private int maxParticles = 15;
        [SerializeField] private float emissionRate = 3f;
        [SerializeField] private float horizontalSpeed = 10f;
        [SerializeField] private float swirlStrength = 5f;
        [SerializeField] private float ribbonWidth = 0.35f;
        [SerializeField] private float trailTime = 0.6f;
        [SerializeField] private Color windColor = new Color(1f, 1f, 1f, 0.9f);

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
            Gizmos.color = new Color(0.6f, 0.85f, 1f, 0.35f);
            Gizmos.DrawCube(transform.position, new Vector3(areaSize.x, areaSize.y, 0.1f));
            Gizmos.color = Color.cyan;
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
            main.startLifetime = 2.5f;
            main.startSpeed = 0f;
            main.startSize = 0.1f;
            main.startColor = Color.white;
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

            var trails = _ps.trails;
            trails.enabled = true;
            trails.mode = ParticleSystemTrailMode.PerParticle;
            trails.ratio = 1f;
            trails.lifetime = new ParticleSystem.MinMaxCurve(trailTime);
            trails.minVertexDistance = 0.1f;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, CreateTaperCurve());
            trails.colorOverLifetime = CreateAlphaGradient();

            AnimationCurve curveX = AnimationCurve.Constant(0f, 1f, horizontalSpeed);

            Keyframe[] loopYKeys = new Keyframe[]
            {
                new Keyframe(0.0f, 0f),
                new Keyframe(0.3f, 0f),
                new Keyframe(0.42f, swirlStrength),
                new Keyframe(0.55f, 0f),
                new Keyframe(0.68f, -swirlStrength),
                new Keyframe(0.8f, 0f),
                new Keyframe(1.0f, 0f)
            };
            AnimationCurve curveY = new AnimationCurve(loopYKeys);
            AnimationCurve curveZ = AnimationCurve.Constant(0f, 1f, 0f);

            var velOverLifetime = _ps.velocityOverLifetime;
            velOverLifetime.enabled = true;
            velOverLifetime.x = new ParticleSystem.MinMaxCurve(1f, curveX);
            velOverLifetime.y = new ParticleSystem.MinMaxCurve(1f, curveY);
            velOverLifetime.z = new ParticleSystem.MinMaxCurve(1f, curveZ);

            var renderer = GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.None;
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = orderInLayer;

            Material trailMat = new Material(Shader.Find("Sprites/Default"));
            renderer.trailMaterial = trailMat;
        }

        private AnimationCurve CreateTaperCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0.05f),
                new Keyframe(0.2f, ribbonWidth),
                new Keyframe(0.8f, ribbonWidth * 0.7f),
                new Keyframe(1f, 0.0f)
            );
        }

        private Gradient CreateAlphaGradient()
        {
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(windColor, 0f), new GradientColorKey(windColor, 1f) },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(windColor.a, 0.2f),
                    new GradientAlphaKey(windColor.a * 0.8f, 0.8f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            return grad;
        }
    }
}
