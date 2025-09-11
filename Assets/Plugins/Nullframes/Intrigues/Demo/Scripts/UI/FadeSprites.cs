using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Nullframes.Intrigues.Demo
{
    public class FadeSprites : MonoBehaviour
    {
        public List<SpriteRenderer> renderers;
        public List<TextMeshPro> texts;
        private Camera mainCamera;

        public bool visible;

        public float minZoom = 14f;
        public float maxZoom = 15f;
        public float minAlpha = 0f;
        public float maxAlpha = 1f;

        public UnityEvent onVisible;
        public UnityEvent onHide;

        private void Start()
        {
            visible = true;
            mainCamera = Camera.main;
        }

        void LateUpdate()
        {
            float z = mainCamera.orthographicSize;

            float targetAlpha = z.Remap(minZoom, maxZoom, maxAlpha, minAlpha);

            //Sprite Renderers
            foreach (var spriteRenderer in renderers)
            {
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b,
                    targetAlpha);
            }

            //Texts
            foreach (var spriteRenderer in texts)
            {
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b,
                    targetAlpha);
            }

            //Events
            var currentVisibleBeforeCheck = visible;

            //Check
            visible = !(targetAlpha < .1f);

            if (currentVisibleBeforeCheck != visible)
            {
                if (visible) onVisible.Invoke();
                if (!visible) onHide.Invoke();
            }
        }
    }
}