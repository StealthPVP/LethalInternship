using System.Collections.Generic;
using UnityEngine;

namespace Core.Events
{
    [CreateAssetMenu(menuName = "Events/GameEvent")]
    public class GameEvent : ScriptableObject
    {
        private List<GameEventListener> _listeners = new List<GameEventListener>();

        public void Raise()
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                if (_listeners[i] == null)
                    _listeners.RemoveAt(i);
                else
                    _listeners[i].OnEventRaised();
            }
        }

        public void RegisterListener(GameEventListener p_listener)
        {
            if (!_listeners.Contains(p_listener))
                _listeners.Add(p_listener);
        }

        public void UnregisterListener(GameEventListener p_listener)
        {
            if (_listeners.Contains(p_listener))
                _listeners.Remove(p_listener);
        }
    }
}

