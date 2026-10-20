using UnityEngine;

namespace PiGame.Gameplay
{
    // Orienta somente a arte. O movimento e os colliders continuam nos scripts originais.
    public sealed class ProjectileVisual : MonoBehaviour
    {
        [SerializeField] private Transform _art;
        private Vector3 _previousPosition;
        private bool _hasMoved;

        private void Awake()
        {
            if (_art) return;
            Debug.LogError("Configure Art no ProjectileVisual do prefab.", this);
            enabled = false;
        }

        private void OnEnable()
        {
            _previousPosition = transform.position;
            _hasMoved = false;
        }

        private void LateUpdate()
        {
            Vector2 movement = transform.position - _previousPosition;
            _previousPosition = transform.position;
            if (movement.sqrMagnitude > 0.000001f)
            {
                _art.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg);
                _hasMoved = true;
            }
            else if (!_hasMoved) _art.rotation = transform.rotation;
        }
    }
}
