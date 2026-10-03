using LastSeenWearing.Core.Config;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSeenWearing.Gameplay.Bootstrap
{
    /// <summary>
    /// The only object in <c>Bootstrap.unity</c>, the first scene in the build. Holds the
    /// <see cref="GameConfig"/> root that systems are handed their configs from
    /// (docs/CONVENTIONS.md), survives scene loads, and loads the first content scene.
    /// Which scene that is becomes the lobby's call once P0.10 brings networking in.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameConfig _gameConfig;

        // A scene name, not a tunable: which content opens, not how the game behaves.
        [SerializeField] private string _firstSceneName;

        public GameConfig GameConfig => _gameConfig;

        private void Awake()
        {
            if (_gameConfig == null)
            {
                Debug.LogError("[GameBootstrap] GameConfig is not assigned.", this);
            }

            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(_firstSceneName))
            {
                Debug.LogError("[GameBootstrap] No first scene is set.", this);
                return;
            }

            SceneManager.LoadScene(_firstSceneName, LoadSceneMode.Single);
        }
    }
}
