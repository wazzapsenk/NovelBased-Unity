using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nullframes.Intrigues.Demo
{
    public class SpeedControl : MonoBehaviour
    {
        public enum SpeedMode
        {
            Paused,
            Normal,
            SpeedUp,
        }

        [SerializeField] private Button slowDownBtn;
        [SerializeField] private Button normalizeBtn;
        [SerializeField] private Button speedUpBtn;

        [Space(10)] 
        
        [SerializeField] private Sprite pause_Active;
        [SerializeField] private Sprite pause_Disable;
        
        [Space(10)] 
        
        [SerializeField] private Sprite normalize_Active;
        [SerializeField] private Sprite normalize_Disable;
        
        [Space(10)] 
        
        [SerializeField] private Sprite speedUp_Active;
        [SerializeField] private Sprite speedUp_Disable;

        private Image slowDownImage;
        private Image normalizeImage;
        private Image speedUpImage;

        public UnityEvent onTimePaused;
        public UnityEvent onTimeNormalized;
        public UnityEvent onTimeSpeedUp;

        public SpeedMode State { get; private set; } = SpeedMode.Normal;

        private void Start()
        {
            slowDownImage = (Image)slowDownBtn.targetGraphic;
            normalizeImage = (Image)normalizeBtn.targetGraphic;
            speedUpImage = (Image)speedUpBtn.targetGraphic;

            slowDownBtn.onClick.AddListener(Pause);
            normalizeBtn.onClick.AddListener(Normalize);
            speedUpBtn.onClick.AddListener(SpeedUp);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        public void Pause()
        {
            DisableAll();

            slowDownImage.sprite = pause_Active;
            
            Time.timeScale = 0f;
            
            State = SpeedMode.Paused;
            onTimePaused.Invoke();
        }
        
        public void Normalize()
        {
            DisableAll();
            
            normalizeImage.sprite = normalize_Active;
            
            Time.timeScale = 1f;
            
            State = SpeedMode.Normal;
            onTimeNormalized.Invoke();
        }

        public void SpeedUp()
        {
            DisableAll();
            
            speedUpImage.sprite = speedUp_Active;
            
            Time.timeScale = 4f;
            
            State = SpeedMode.SpeedUp;
            onTimeSpeedUp.Invoke();
        }

        private void DisableAll()
        {
            slowDownImage.sprite = pause_Disable;
            normalizeImage.sprite = normalize_Disable;
            speedUpImage.sprite = speedUp_Disable;
        }

    }
}