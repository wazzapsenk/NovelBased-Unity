using System;
using UnityEngine;

namespace Nullframes.Threading.Coroutines
{
    [DisallowMultipleComponent]
    internal sealed class DisableCallback : MonoBehaviour, IDisableCallback
    {
        public event Action Disabled;

        private void OnDisable()
        {
            Disabled?.Invoke();
            Disabled = null;
        }
    }
}