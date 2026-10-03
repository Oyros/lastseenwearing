using UnityEngine;

namespace LastSeenWearing.Gameplay.Network
{
    /// <summary>
    /// A developer panel for <see cref="NetworkSession"/>: host, join, invite, leave. Editor and
    /// development builds only; its text is debug tooling, exempt from localization
    /// (docs/LOCALIZATION.md §1). The real lobby screen is UI work in a later phase.
    /// </summary>
    public sealed class SessionDebugPanel : MonoBehaviour
    {
        [SerializeField] private NetworkSession _session;

        private void Awake()
        {
            if (!Debug.isDebugBuild)
            {
                enabled = false;
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 220, 200), GUI.skin.box);
            GUILayout.Label($"Session: {_session.Transport}");

            if (!_session.IsRunning)
            {
                if (GUILayout.Button("Host"))
                {
                    _session.Host();
                }

                if (_session.Transport == SessionTransport.Local && GUILayout.Button("Join (local)"))
                {
                    _session.JoinLocal();
                }
            }
            else
            {
                GUILayout.Label(_session.IsHost ? $"Hosting, {_session.ConnectedPlayers} player(s)" : "Connected");
                if (_session.CanInvite && GUILayout.Button("Invite (Steam)"))
                {
                    _session.OpenInviteOverlay();
                }

                if (GUILayout.Button("Leave"))
                {
                    _session.Leave();
                }
            }

            GUILayout.EndArea();
        }
    }
}
