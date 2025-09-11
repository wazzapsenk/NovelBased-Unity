using System.Collections.Generic;
using UnityEngine;

namespace Nullframes.Intrigues.Demo
{
    public class AudioScaler : MonoBehaviour
    {
        public List<AudioSource> zoomInSounds;
        public List<AudioSource> zoomOutSounds;
        
        public Camera mainCamera;

        public float maxZoomOutRef = 8f;
        public float maxZoomInRef = 16f;
        public float minVolume = 0.1f;
        public float maxVolume = 0.7f;

        void Update()
        {
            float z = mainCamera.orthographicSize;

            float zoomInVolume = z.Remap(maxZoomOutRef, maxZoomInRef, minVolume, maxVolume -0.3f);
            float zoomOutVolume = z.Remap(maxZoomOutRef, maxZoomInRef, maxVolume, minVolume);

            foreach (var zoomOutSound in zoomOutSounds)
            {
                zoomOutSound.volume = zoomOutVolume;
            }
            
            foreach (var zoomInSound in zoomInSounds)
            {
                zoomInSound.volume = zoomInVolume;
            }
        }
    }
}