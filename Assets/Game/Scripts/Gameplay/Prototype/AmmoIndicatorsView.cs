using UnityEngine;

namespace PiGame.Gameplay
{
    public class AmmoIndicatorsView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] _indicators;

        private void Awake()
        {
            for (int index = 0; index < _indicators.Length; index++)
            {
                _indicators[index].enabled = false;
                _indicators[index].gameObject.SetActive(false);
            }
        }

        public void SetCount(int count, bool visible, Color color)
        {
            for (int index = 0; index < _indicators.Length; index++)
            {
                bool showIndicator = visible && index < count;
                _indicators[index].color = color;
                _indicators[index].enabled = showIndicator;
                // O contorno serializado é filho do indicador e acompanha sua visibilidade.
                _indicators[index].gameObject.SetActive(showIndicator);
            }
        }
    }
}
