using System.Collections;
using Nullframes.Intrigues.Utils;
using UnityEngine;
using TMPro;

namespace Nullframes.Intrigues.UI {
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TypewriterEffect : IIEditor {
        private TextMeshProUGUI _textBox;

        // Basic Typewriter Functionality
        private Coroutine _typewriterCoroutine;
        private bool _readyForNewText = true;

        [Header("Typewriter Settings")] 
        public float seconds = 1f;

        private void Awake() {
            _textBox = GetComponent<TextMeshProUGUI>();
        }

        private void OnDestroy() {
            if (_typewriterCoroutine != null) {
                CoroutineManager.StopRoutine(_typewriterCoroutine);
            }
        }

        private void Start() {
            if (_typewriterCoroutine != null) {
                _typewriterCoroutine = CoroutineManager.StartRoutine(Typewriter());
            }
        }

        public void StartTypeWriter() {
            _textBox.maxVisibleCharacters = 0;
            _textBox.ForceMeshUpdate();

            if (!_readyForNewText || _textBox.maxVisibleCharacters >= _textBox.textInfo.characterCount)
                return;
            
            _readyForNewText = false;

            if (_typewriterCoroutine != null)
                StopCoroutine(_typewriterCoroutine);

            _textBox.maxVisibleCharacters = 0;

            _typewriterCoroutine = CoroutineManager.StartRoutine(Typewriter());
        }

        private IEnumerator Typewriter() {
            TMP_TextInfo textInfo = _textBox.textInfo;
            float startTime = Time.time;
            int totalCharacterCount = textInfo.characterCount;

            while (Time.time - startTime < seconds) {
                float elapsedTime = Time.time - startTime;
                float progress = elapsedTime / seconds;

                int visibleCharacters = Mathf.Min((int)(progress * totalCharacterCount), totalCharacterCount);
                _textBox.maxVisibleCharacters = visibleCharacters;

                if (visibleCharacters >= totalCharacterCount) {
                    _readyForNewText = true;
                    yield break;
                }

                yield return null;
            }

            _textBox.maxVisibleCharacters = totalCharacterCount;
            _readyForNewText = true;
        }
    }
}