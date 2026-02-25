using UnityEngine;

namespace Core.GameState
{
    public class GameStateManager : MonoBehaviour
    {
        [SerializeField] private GameStateSO _GameState;

        private void OnEnable()
        {
            _GameState.SetState(GameStateSO.GameState.Menu);
        }
        public void ChangeGameState(int p_newState)
        {
            _GameState.SetState((GameStateSO.GameState)p_newState);
        }
    }
}
