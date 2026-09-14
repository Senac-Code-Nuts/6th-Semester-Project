using UnityEngine;

namespace PiGame.Gameplay
{
    [RequireComponent(typeof(NetworkPlayerState))]
    public class PlayerIndicator : MonoBehaviour
    {
        [SerializeField] private Vector3 _localPosition = new Vector3(0f, 1.45f, 0f);

        private NetworkPlayerState _playerState;
        private MeshRenderer _renderer;

        private void Awake()
        {
            _playerState = GetComponent<NetworkPlayerState>();
            CreateIndicator();
        }

        private void OnEnable()
        {
            _playerState.StateChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            _playerState.StateChanged -= Refresh;
        }

        private void CreateIndicator()
        {
            GameObject indicator = new GameObject("PlayerIndicator");
            indicator.transform.SetParent(transform, false);
            indicator.transform.localPosition = _localPosition;

            Mesh mesh = new Mesh
            {
                name = "PlayerIndicatorTriangle",
                vertices = new[]
                {
                    new Vector3(-0.22f, 0.18f, 0f),
                    new Vector3(0.22f, 0.18f, 0f),
                    new Vector3(0f, -0.18f, 0f)
                },
                triangles = new[] { 0, 1, 2 }
            };

            indicator.AddComponent<MeshFilter>().sharedMesh = mesh;
            _renderer = indicator.AddComponent<MeshRenderer>();
            _renderer.material = new Material(Shader.Find("Sprites/Default"));
            _renderer.sortingOrder = 30;
        }

        private void Refresh()
        {
            if (_renderer == null)
            {
                return;
            }

            _renderer.enabled = _playerState.IsAlive;
            _renderer.material.color = _playerState.IndicatorColor;
        }
    }
}
