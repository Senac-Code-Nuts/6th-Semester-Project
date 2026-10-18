
using PiGame.Gameplay;
using Unity.Netcode;
using UnityEngine;
namespace PiGame.Enviroment
{
    public class ParallaxEnviroment : MonoBehaviour
    {
        private float _length;
        private float _startPos;

        private Transform _localPlayer;
        [SerializeField] private float parallaxEffect;
        [SerializeField, Min(0f)] private float _smoothSpeed = 3f;
        
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _startPos = transform.position.x;

            if(_spriteRenderer != null)
            {
                _length = _spriteRenderer.bounds.size.x;
            }

            FindLocalPlayer();
        }

        private void FindLocalPlayer()
        {
            NetworkPlayerState[] players = FindObjectsByType<NetworkPlayerState>(FindObjectsSortMode.None);

            foreach(NetworkPlayerState player in players)
            {
                NetworkObject networkObject = player.GetComponent<NetworkObject>();

                if(networkObject != null && networkObject.IsOwner)
                {
                    _localPlayer = player.transform;
                    return;
                }
            }
        }

        private void LateUpdate()
        {
            if(_localPlayer == null)
            {
                FindLocalPlayer();
                if(_localPlayer == null)
                    return;
            }

            float playerX = _localPlayer.position.x;

            float temp = playerX * (1f - parallaxEffect);

            float distance = playerX * parallaxEffect;

            float targetX = _startPos + distance;

            if(_length > 0f)
            {
                if(temp > _startPos + _length)
                {
                    _startPos += _length;
                }
                else if(temp < _startPos - _length)
                {
                    _startPos -= _length;
                }
            }

            targetX = _startPos + distance;
            
            Vector3 targetPosition = new Vector3(targetX, transform.position.y, transform.position.z);
            
            transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Exp(-_smoothSpeed * Time.deltaTime));
        }

    }
    
}
