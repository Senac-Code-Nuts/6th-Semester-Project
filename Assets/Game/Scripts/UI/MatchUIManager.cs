
using TMPro;
using UnityEngine;
namespace PiGame.Gameplay
{
    public class MatchUIManager : MonoBehaviour
    {
        [SerializeField] private MatchController _matchController;
        [SerializeField] private TMP_Text _timerText;

        private void Update()
        {
            float time = _matchController.RemainingTime;

            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);

            _timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }
}

