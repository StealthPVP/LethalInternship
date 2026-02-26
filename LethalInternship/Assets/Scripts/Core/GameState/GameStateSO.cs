using System;
using UnityEngine;

namespace Core.GameState
{
    [CreateAssetMenu(menuName = "Core/GameState/GameStateSO")]
    public class GameStateSO : ScriptableObject
    {
        public enum GameState
        {
            Menu,
            Lobby,
            Playing,
            GameOver,
            Victory
        }

        private GameState _currentState = GameState.Menu;
        public GameState CurrentState => _currentState;

        public event Action<GameState> OnStateChanged;

        public void SetState(GameState p_newState)
        {
            if (_currentState == p_newState)
                return;
            _currentState = p_newState;
            OnStateChanged?.Invoke(_currentState);
        }
    }
}
