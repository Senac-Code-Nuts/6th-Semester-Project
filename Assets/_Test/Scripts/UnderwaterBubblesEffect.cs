using UnityEngine;

namespace PiGame.Gameplay
{
    [DisallowMultipleComponent]
    public class UnderwaterBubblesEffect : MonoBehaviour
    {
        [Header("Rendering")]
        [SerializeField] private Sprite bubbleSprite;
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int orderInLayer = 8;

        [Header("Area & Count")]
        [SerializeField] private Vector2 areaSize = new Vector2(26f, 15f);
        [SerializeField] private int maxParticles = 60;
        [SerializeField] private float emissionRate = 12f;

        [Header("Bubble Movement")]
        [SerializeField] private Vector2 upwardSpeedRange = new Vector2(1.5f, 3.5f);
        [SerializeField] private float horizontalWobble = 0.6f;
        [SerializeField] private Vector2 sizeRange = new Vector2(0.25f, 0.6f);
        [SerializeField] private Color bubbleColor = new Color(0.85f, 0.96f, 1f, 0.85f);

        private ParticleSystem _ps;
        private Material _bubbleMaterial;

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
                    UpdateSpriteMaterial(renderer);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.25f);
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
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
            main.startColor = bubbleColor;
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
            velOverLifetime.x = new ParticleSystem.MinMaxCurve(-horizontalWobble, horizontalWobble);
            velOverLifetime.y = new ParticleSystem.MinMaxCurve(upwardSpeedRange.x, upwardSpeedRange.y);
            velOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var sizeOverLifetime = _ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.4f),
                new Keyframe(0.2f, 1f),
                new Keyframe(0.9f, 1.15f),
                new Keyframe(1f, 0.1f)
            );
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var colorOverLifetime = _ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.15f),
                    new GradientAlphaKey(0.9f, 0.85f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = grad;

            var renderer = GetComponent<ParticleSystemRenderer>();
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = orderInLayer;
            UpdateSpriteMaterial(renderer);
        }

        private void UpdateSpriteMaterial(ParticleSystemRenderer renderer)
        {
            if (_bubbleMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Particles/Standard Unlit");
                _bubbleMaterial = new Material(shader);
            }

            if (bubbleSprite != null)
            {
                _bubbleMaterial.mainTexture = bubbleSprite.texture;
            }

            renderer.material = _bubbleMaterial;
        }
    }
}
