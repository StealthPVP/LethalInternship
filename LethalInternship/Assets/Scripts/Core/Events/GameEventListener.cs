using Core.Events;
using UnityEngine;
using UnityEngine.Events;

namespace Core.Events
{
    public class GameEventListener : MonoBehaviour
    {
        [SerializeField] private GameEvent _GameEvent;
        [SerializeField] private UnityEvent _Response;

        private void OnEnable() => _GameEvent.RegisterListener(this);
        private void OnDisable() => _GameEvent.UnregisterListener(this);

        public void OnEventRaised() => _Response.Invoke();
    }
}
