using Assets.Scripts.Match.Actors;
using Assets.Scripts.Match.Multiplayer.Protocol;
using UnityEngine;

namespace Assets.Scripts.Match.Multiplayer
{
    public sealed class MultiplayerGameModeManager : GameModeManager
    {
        private readonly ActorPointer[] actors = new ActorPointer[ProtocolConfig.MaxPlayers];

        public MultiplayerGameModeManager(MatchManager matchManager) : base(matchManager)
        {
        }

        public override void Update()
        {
            var multiplayerMatchManager = (MultiplayerMatchManager) matchManager;

            if (multiplayerMatchManager.isHost)
            {
                for (var i = 0; i < actors.Length; i++)
                {
                    var player = multiplayerMatchManager.players[i];
                    if (player == null) continue;

                    //Respawn player
                    var playerActor = matchManager.actorManager.TryGetActor(actors[i]);
                    if (playerActor == null)
                    {
                        if (Time.time - player.lastActorLifeTime < 0.7f && player.lastActorLifeTime > 0) continue;

                        var createdActor = matchManager.actorManager.CreateActor(ActorType.Character);
                        actors[i] = createdActor.actorPointer;

                        createdActor.Pos = matchManager.mapManager.GetRandomSpawnPos();

                        if (i == multiplayerMatchManager.LocalPlayerSlot) LocalPlayerControlledActorPointer = createdActor.actorPointer;
                    }
                    else player.lastActorLifeTime = Time.time;
                }
            }

            base.Update();
        }

        public override void ProcessActorDeath(CharacterActor actor)
        {
            var multiplayerMatchManager = (MultiplayerMatchManager) matchManager;

            if (!multiplayerMatchManager.isHost) return;

            for (var i = 0; i < actors.Length; i++)
            {
                if(actors[i] == actor.actorPointer)
                {
                    var player = multiplayerMatchManager.players[i];
                    if (player == null) return;

                    player.deaths++;

                    break;
                }
            }

            base.ProcessActorDeath(actor);
        }

        protected override void ProcessBlockTouchDamage(CharacterActor characterActor)
        {
            var multiplayerMatchManager = (MultiplayerMatchManager) matchManager;

            if (!multiplayerMatchManager.isHost)
            {
                if(characterActor.actorPointer == LocalPlayerControlledActorPointer)
                    multiplayerMatchManager.SendBlockTouchPacket(LocalPlayerControlledActorPointer);

                return;
            }

            base.ProcessBlockTouchDamage(characterActor);
        }

        public void OnPlayerConnect(Player player)
        {
        }

        public void OnPlayerDisconnect(Player player)
        {
            var playerActor = matchManager.actorManager.TryGetActor(actors[player.slotId]);

            if (playerActor != null)
            {
                actors[player.slotId] = ActorPointer.Null;

                matchManager.actorManager.DestroyActor(playerActor.actorPointer);
            }
        }

        public void Pack(PacketWriter packetWriter)
        {
            foreach (var actorPointer in actors)
            {
                actorPointer.Save(packetWriter);
            }
        }

        public void Unpack(PacketReader packetReader)
        {
            for (var i = 0; i < actors.Length; i++)
            {
                actors[i] = ActorPointer.Load(packetReader);
            }

            var multiplayerMatchManager = (MultiplayerMatchManager)matchManager;
            LocalPlayerControlledActorPointer = actors[multiplayerMatchManager.LocalPlayerSlot];
        }

        public ActorPointer GetPlayerActorPointer(Player player)
        {
            return actors[player.slotId];
        }

        public override void ProcessLifeTimeDestroy(Actor actor)
        {
            var multiplayerMatchManager = (MultiplayerMatchManager) matchManager;

            if (!multiplayerMatchManager.isHost) return;

            base.ProcessLifeTimeDestroy(actor);
        }

        public override void ProcessBulletHit(BulletActor actor, Collider collider)
        {
            var multiplayerMatchManager = (MultiplayerMatchManager) matchManager;

            if (!multiplayerMatchManager.isHost) return;

            base.ProcessBulletHit(actor, collider);
        }

        public override void ProcessKickAction(CharacterActor characterActor)
        {
            var multiplayerMatchManager = (MultiplayerMatchManager)matchManager;

            if (!multiplayerMatchManager.isHost)
            {
                multiplayerMatchManager.SendClientState();

                return;
            }

            base.ProcessKickAction(characterActor);
        }

        protected override void ProcessCreatedBullet(CharacterActor creator, BulletActor bulletActor)
        {
            for (var i = 0; i < actors.Length; i++)
            {
                if (actors[i] == creator.actorPointer)
                {
                    bulletActor.owner = i;

                    return;
                }
            }
        }

        protected override void ProcessKillScore(int owner)
        {
            var multiplayerMatchManager = (MultiplayerMatchManager) matchManager;

            var player = multiplayerMatchManager.players[owner];
            if (player != null) player.kills++;
        }
    }
}