using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Nullframes.Intrigues
{
    [DisallowMultipleComponent]
    public class SignalReceiver : MonoBehaviour
    {
        public List<IEvent> reactions = new List<IEvent>();

        private void Awake()
        {
            IM.onSignalReceive += OnSignalReceive;
        }

        private void OnSignalReceive(Signal signal)
        {
            foreach (var reaction in reactions)
            {
                reaction.Invoke(signal);
            }
        }

        public void AddSignal()
        {
            reactions.Add(new IEvent());
        }
        
        public void RemoveSignal(int index)
        {
            reactions.RemoveAt(index);
        }
    }

    [System.Serializable]
    public struct IEvent
    {
        public UnityEvent m_event;
        public Signal m_signal;

        public void Invoke(Signal signal)
        {
            if (m_signal == signal)
            {
                m_event.Invoke();
            }
        }
    }

}