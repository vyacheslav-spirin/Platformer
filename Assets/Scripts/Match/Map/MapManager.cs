using System;
using System.Collections.Generic;
using Assets.Scripts.Match.Actors;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Match.Map
{
    public sealed class MapManager
    {
        private sealed class RuntimeBlockDescription
        {
            public BlockDescription description;

            public GameObject prefab;
        }

        private readonly MatchManager matchManager;

        private readonly MapDescription mapDescription;

        private readonly RuntimeBlockDescription[] blockDescriptions;

        private readonly Vector2[] spawns;

        public readonly int mapWidth, mapHeight;

        private readonly Block[] blocks;

        private bool isCreatedBlockActors;

        public MapManager(MatchManager matchManager, string mapName)
        {
            this.matchManager = matchManager;

            var blockDescriptionsList = new List<RuntimeBlockDescription>(10);

            var i = 0;
            while (true)
            {
                var typeId = i++;

                var blockResourcePath = "Blocks/" + typeId;

                var blockDescription = Resources.Load<BlockDescription>(blockResourcePath);

                if(blockDescription == null) break;

                var blockPrefab = Resources.Load<GameObject>(blockResourcePath);

                blockDescriptionsList.Add(new RuntimeBlockDescription
                {
                    description = blockDescription,
                    prefab = blockPrefab
                });
            }

            blockDescriptions = blockDescriptionsList.ToArray();

            //load map

            mapDescription = Resources.Load<MapDescription>("Maps/" + mapName + "/MapDescription");
            if (mapDescription == null) throw new Exception($"Could not find {nameof(MapDescription)} by map name: {mapName}!");

            //calc map bounds

            foreach (var block in mapDescription.blocks)
            {
                if(block.posX < 0 || block.posY < 0) throw new Exception("Invalid block pos!");

                if (block.posX >= mapWidth) mapWidth = block.posX + 1;
                if (block.posY >= mapHeight) mapHeight = block.posY + 1;
            }

            blocks = new Block[mapWidth * mapHeight];

            //create static only

            foreach (var block in mapDescription.blocks)
            {
                var blockDescription = GetBlockDescriptionByTypeId(block.typeId);

                if (blockDescription.blockBehaviour != BlockDescription.BlockBehaviour.Static) continue;

                CreateBlock(block.typeId, block.posX, block.posY);
            }

            spawns = mapDescription.spawns;
        }

        public void CreateBlockActors()
        {
            if(isCreatedBlockActors) throw new Exception("Block actors already is created!");

            isCreatedBlockActors = true;

            var actorManager = matchManager.actorManager;

            foreach (var block in mapDescription.blocks)
            {
                var blockDescription = GetBlockDescriptionByTypeId(block.typeId);

                if (blockDescription.blockBehaviour != BlockDescription.BlockBehaviour.Destructible) continue;

                var blockActor = (BlockActor) actorManager.CreateActor(ActorType.Block);
                blockActor.InitBlock(block.typeId, block.posX, block.posY);
            }
        }

        private RuntimeBlockDescription GetRuntimeBlockDescriptionByTypeId(int typeId)
        {
            if (typeId < 0 || typeId >= blockDescriptions.Length) throw new Exception($"Could not find {nameof(BlockDescription)} by type id: {typeId}");

            return blockDescriptions[typeId];
        }

        private void CreateBlock(int typeId, int posX, int posY)
        {
            if(posX < 0 || posY < 0 || posX >= mapWidth || posY >= mapHeight) throw new Exception("Could not create block! Invalid block pos!");

            if (blocks[posY * mapWidth + posX] != null) throw new Exception("Could not create block! Block with same position already exists!");

            var instance = CreateBlockGameObject(typeId, posX, posY);

            blocks[posY * mapWidth + posX] = new Block(typeId, instance);
        }

        public GameObject CreateBlockGameObject(int typeId, int posX, int posY)
        {
            var prefab = GetRuntimeBlockDescriptionByTypeId(typeId).prefab;

            return Object.Instantiate(prefab, new Vector3(posX, posY, 0), Quaternion.identity);
        }

        public void AddBlockActor(BlockActor blockActor)
        {
            var pos = blockActor.Pos;

            if (pos.x < 0 || pos.y < 0 || pos.x >= mapWidth || pos.y >= mapHeight) throw new Exception("Could not create block! Invalid block pos!");

            var arrIndex = (int) pos.y * mapWidth + (int) pos.x;

            blocks[arrIndex] = new Block(blockActor.BlockTypeId, blockActor.GameObject);
        }

        public void RemoveBlockActor(BlockActor blockActor)
        {
            var pos = blockActor.Pos;

            var arrIndex = (int) pos.y * mapWidth + (int) pos.x;

            var block = blocks[arrIndex];

            if (block == null || block.gameObject != blockActor.GameObject) return;

            blocks[arrIndex] = null;
        }

        public BlockDescription GetBlockDescriptionByTypeId(int typeId)
        {
            return GetRuntimeBlockDescriptionByTypeId(typeId).description;
        }

        public Vector2 GetRandomSpawnPos()
        {
            return spawns[Random.Range(0, spawns.Length)];
        }

        public Block GetBlock(int x, int y)
        {
            if (x < 0 || y < 0 || x >= mapWidth || y >= mapHeight) return null;

            return blocks[y * mapWidth + x];
        }
    }
}