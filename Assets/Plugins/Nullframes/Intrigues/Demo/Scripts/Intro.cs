using UnityEngine;
using UnityEngine.Playables;

namespace Nullframes.Intrigues.Demo
{
    public class Intro : MonoBehaviour
    {
        private PlayableDirector playableDirector;
        private bool sceneSkipped;

        private void Awake()
        {
#if !UNITY_EDITOR
            // if (!PlayerPrefs.HasKey("IntroOpened") || PlayerPrefs.GetInt("IntroOpened") == 1) return;
            if (Time.realtimeSinceStartup < 10)
            {
                playableDirector = GetComponent<PlayableDirector>();
                PlayIntro();
            }
#endif
        }
#if !UNITY_EDITOR
        private void PlayIntro()
        {
            playableDirector.Play();
        }
#endif

        private void Update()
        {
            if (playableDirector == null) return;
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (playableDirector.state == PlayState.Playing && !sceneSkipped)
                {
                    playableDirector.time = 12.5f;
                    sceneSkipped = true;
                }
            }
        }
    }
}