using System;
using LastSeenWearing.Core.Capture;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Roles;
using LastSeenWearing.Core.Round;
using LastSeenWearing.Gameplay.Crowd;
using LastSeenWearing.Gameplay.Player;
using LastSeenWearing.Gameplay.Roles;
using LastSeenWearing.Gameplay.Round;
using Unity.Netcode;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Capture
{
    /// <summary>
    /// The patrol's cuffs (GDD §04.3, P1.24). The patrol holds Arrest on someone; the server checks it — the patrol,
    /// in play, close enough, someone real — and spends a cuff on anyone who is not the fugitive. The fugitive cuffed
    /// is the police's round; a wrong arrest gives the fugitive time; the last cuff wasted starts the last-cuff chase.
    /// <see cref="RoundDirector"/> ends the round, never this. Heat is not asked for yet (P2.05).
    /// </summary>
    public sealed class Arrests : NetworkBehaviour
    {
        [SerializeField] private CaptureConfig _config;
        [SerializeField] private RoundDirector _director;
        [SerializeField] private RoleRosterSync _roster;
        [SerializeField] private CrowdSpawner _crowd;

        private readonly NetworkVariable<int> _cuffsLeft = new();
        private Cuffs _cuffs;

        /// <summary>A wrong arrest happened (everyone hears it: an officer cuffing a stranger is seen).</summary>
        public event Action WrongArrest;

        public CaptureConfig Config => _config;
        public int CuffsLeft => _cuffsLeft.Value;
        public int CuffsPerRound => _config.CuffsPerRound;

        /// <summary>Server, when a round goes live: a full set of cuffs.</summary>
        public void BeginRound()
        {
            _cuffs = new Cuffs(_config.CuffsPerRound);
            _cuffsLeft.Value = _cuffs.Left;
        }

        /// <summary>The local patrol: arrest whoever this hitbox belongs to.</summary>
        public void Request(CharacterHitbox target)
        {
            var character = target.Character;
            if (character.TryGetComponent<FugitiveController>(out var fugitive))
            {
                ArrestRpc(fugitive.NetworkObject, -1);
            }
            else if (character.TryGetComponent<CrowdAgent>(out var agent))
            {
                ArrestRpc(default, agent.Index);
            }
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void ArrestRpc(NetworkObjectReference fugitiveBody, int npc, RpcParams rpcParams = default)
        {
            var sender = rpcParams.Receive.SenderClientId;
            if (_cuffs == null || _director.Phase != RoundPhase.Live || _roster.RoleOf(sender) != Role.Patrol
                || !NetworkManager.ConnectedClients.TryGetValue(sender, out var client) || client.PlayerObject == null)
            {
                return;
            }

            Vector3 at;
            var isFugitive = npc < 0;
            if (isFugitive)
            {
                if (!fugitiveBody.TryGet(out var body) || !body.TryGetComponent<FugitiveController>(out var fugitive) || fugitive.IsChanging)
                {
                    return; // gone, or inside a tent
                }

                at = body.transform.position;
            }
            else
            {
                var agents = _crowd.Agents;
                if (agents == null || npc >= agents.Length)
                {
                    return;
                }

                at = agents[npc].transform.position;
            }

            var reach = _config.ArrestRange + _config.ArrestRangeSlack;
            var offset = at - client.PlayerObject.transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > reach * reach)
            {
                return;
            }

            var result = _cuffs.Arrest(isFugitive);
            _cuffsLeft.Value = _cuffs.Left;
            Debug.Log($"[Arrests] Patrol arrested {(isFugitive ? "the fugitive" : $"Npc_{npc:000}")}: {result}, {_cuffs.Left} cuffs left.");
            switch (result)
            {
                case ArrestResult.Caught:
                    _director.ReportArrest();
                    break;
                case ArrestResult.Wrong:
                case ArrestResult.LastCuffSpent:
                    WrongArrestRpc();
                    _director.ReportWrongArrest(_config.WrongArrestBonusSeconds, result == ArrestResult.LastCuffSpent);
                    break;
            }
        }

        [Rpc(SendTo.Everyone)]
        private void WrongArrestRpc() => WrongArrest?.Invoke();
    }
}
