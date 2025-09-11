using TMPro;
using UnityEngine;

namespace Nullframes.Intrigues
{
    public class NotificationSystem : MonoBehaviour
    {
        public static NotificationSystem instance;
        public TextMeshProUGUI textItemRef;
        public AudioClip soundFX;
        public float volume = .5f;

        public float messageDuration = 5f;

        private static readonly int In = Animator.StringToHash("In");
        private static readonly int Out = Animator.StringToHash("Out");

        private void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(this);
            textItemRef.gameObject.SetActive(false);
        }

        public static void ShowNotification(string text, bool toUpper)
        {
            if (instance == null) return;
            var upperText = toUpper ? NullUtils.ToUpperPreserveTags(text) : text;
            var textItem = instance.textItemRef.gameObject.Duplicate<TextMeshProUGUI>();
            textItem.text = upperText;
            textItem.gameObject.SetActive(true);

            IM.SetupAudio(instance.soundFX, instance.volume)?.Play();

            var animator = textItem.GetComponent<Animator>();
            animator.SetTrigger(In);

            NullUtils.DelayedCall(new DelayedCallParams {
                Delay = instance.messageDuration,
                Call = () => animator.SetTrigger(Out),
                UnscaledTime = true,
            });
            // NullUtils.DelayedCall(instance.messageDuration, () => { animator.SetTrigger(Out); }, 0, true);
        }

        public static void ShowNotification(string text) => ShowNotification(text, true);
    }
}