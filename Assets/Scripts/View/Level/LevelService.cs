using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using Assets.Scripts.Presentation;
using DG.Tweening;
using SharedData;
using UnityEngine;

namespace Helpers
{
    public class LevelService
    {
        public LevelData LevelData;

        public Vector2Int GridSize { get; private set; }

        private Sprite[] tileSprites;
        private GameObject tilePrefab;

        private Sprite[] entitySprites;
        private Entity entityPrefab;

        private Transform levelContainer;
        private Transform tilesContainer;
        private Transform entitiesContainer;

        private float quakeAnimationCooldown;
        private AudioComponent audio;

        private BattleManager battlemanager;
        private GridNavigator gridNavigator;

        private float stepDuration = .2f;

        public LevelService()
        {
            tileSprites = Resources.LoadAll<Sprite>("Sprites/Tileset");
            tilePrefab = Resources.Load<GameObject>("Prefabs/Tile");

            entitySprites = Resources.LoadAll<Sprite>("Sprites/Entities");
            entityPrefab = Resources.Load<Entity>("Prefabs/Entity");

            levelContainer = GameObject.Find("Level").transform;
            tilesContainer = levelContainer.transform.Find("Tiles");
            entitiesContainer = levelContainer.transform.Find("Entities");

            audio = GameObject.Find("Audio").GetComponent<AudioComponent>();
        }

        public List<Entity> GetCharacters(EntityFaction? filterFaction = null)
        {
            return LevelData.Entities
                .Where(e =>
                    {
                        bool factionCheck = true;
                        if (filterFaction.HasValue)
                        {
                            factionCheck = filterFaction.Value == e.Faction;
                        }
                        bool typeCheck = e.Type == EntityType.Character;
                        return factionCheck && typeCheck;
                    })
                .ToList();
        }

        public Entity GetClosestCharacter(Vector2Int targetPosition, EntityFaction faction)
        {
            return LevelData.Entities
                        .Where(p => p.Type == EntityType.Character && p.Faction == faction)
                        .OrderBy((entity) => Vector2Int.Distance(targetPosition, entity.GridPosition))
                        .FirstOrDefault();
        }

        public List<Entity> GetEntities()
        {
            return LevelData.Entities;
        }

        public bool IsPointOnLevelGrid(int x, int y)
        {
            bool outOfGridBounds = x >= GridSize.x || x < 0 || y >= GridSize.y || y < 0;
            return outOfGridBounds == false;
        }

        //Note: there could be only 1 entity at each tile at a time.
        public Entity GetEntityAtPosition(int x, int y)
        {
            if (IsPointOnLevelGrid(x, y))
            {
                return LevelData.TilesEntities[x, y];
            }
            else
            {
                return null;
            }
        }

        public List<Entity> GetEntitiesInRange(Entity attacker, EntityFaction targetFaction)
        {
            int range = attacker.AttackRange;
            Vector2Int position = attacker.GridPosition;

            List<Entity> entitiesList = new List<Entity>();

            //Check x axis
            for (int xOffset = -range; xOffset <= range; xOffset++)
            {
                Vector2Int offsetPosition = new Vector2Int(position.x + xOffset, position.y);
                Entity entity = GetEntityAtPosition(offsetPosition.x, offsetPosition.y);
                if (entity != null && entity != attacker && entity.Faction == targetFaction)
                {
                    entitiesList.Add(entity);
                }
            }

            //Check y axis
            for (int yOffset = -range; yOffset <= range; yOffset++)
            {
                Vector2Int offsetPosition = new Vector2Int(position.x, position.y + yOffset);
                Entity entity = GetEntityAtPosition(offsetPosition.x, offsetPosition.y);
                if (entity != null && entity != attacker && entity.Faction == targetFaction)
                {
                    entitiesList.Add(entity);
                }
            }

            return entitiesList;
        }

        public void Init(string levelName, BattleManager battlemanager, GridNavigator gridNavigator)
        {
            this.gridNavigator = gridNavigator;
            this.battlemanager = battlemanager;
            battlemanager.OnPlayerTurnEnded += OnPlayerTurnEnded;

            var levelText = Resources.Load<TextAsset>($"Levels/{levelName}").text;
            var rows = levelText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var width = int.Parse(rows[0]);
            var height = int.Parse(rows[1]);
            GridSize = new Vector2Int(width, height);

            LevelData = new LevelData
            {
                Width = width,
                Height = height,
                Tiles = new TileView[width, height],
                Entities = new List<Entity>(),
                TilesEntities = new Entity[width, height]
            };

            // Ground
            for (int y = 0; y < height; y++)
            {
                var row = rows[3 + y];

                for (int x = 0; x < width; x++)
                {
                    var tile = int.Parse(row[x].ToString()) - 1;
                    InstantiateTile(x, y, tile);
                }
            }

            // Entities
            Sprite entitySprite;
            for (int y = 0; y < height; y++)
            {
                var row = rows[4 + height + y];

                for (int x = 0; x < width; x++)
                {
                    Vector2Int gridPosition = new Vector2Int(x, y);
                    switch (row[x])
                    {
                        case 'e':
                            entitySprite = entitySprites[UnityEngine.Random.Range(0, 5)];
                            InstantiateEntity(gridPosition, entitySprite, EntityType.Character, EntityFaction.Enemy);
                            break;

                        case 'p':
                            entitySprite = entitySprites[UnityEngine.Random.Range(5, 10)];
                            InstantiateEntity(gridPosition, entitySprite, EntityType.Character, EntityFaction.Player);
                            break;

                        case '#':
                            entitySprite = tileSprites[49];
                            InstantiateEntity(gridPosition, entitySprite, EntityType.Obstacle, EntityFaction.Neutral);
                            break;
                    }
                }
            }

            gridNavigator.Init(this);
            CenterCamera(height);
        }

        private void CenterCamera(int levelHeight)
        {
            var numberOfRowsBeforeAdjustmentIsNeeded = 9;
            var difference = Mathf.Max(0, levelHeight - numberOfRowsBeforeAdjustmentIsNeeded);

            var adjustment = -(0.33f * difference);
            adjustment = Mathf.RoundToInt(adjustment);
            Camera.main.transform.position = new Vector3(adjustment, adjustment, Camera.main.transform.position.z);
        }

        private void InstantiateTile(int x, int y, int index)
        {
            var tile = GameObject.Instantiate(tilePrefab, Vector3.zero, Quaternion.identity, tilesContainer);
            var renderer = tile.GetComponent<SpriteRenderer>();
            renderer.sortingOrder = GridHelper.GetSortingOrder(x, y);
            renderer.sprite = tileSprites[index];
            renderer.color = (x ^ y) % 2 == 0 ? Color.white : new Color(0.95f, 0.95f, 0.95f);
            tile.name = $"Tile{x}_{y}";

            tile.transform.localPosition = GridHelper.ToWorldCoordinates(x, y);

            LevelData.Tiles[x, y] = tile.GetComponent<TileView>();
        }

        private void InstantiateEntity(Vector2Int gridPosition, Sprite sprite, EntityType type, EntityFaction faction)
        {
            Entity newEntity = GameObject.Instantiate(entityPrefab, Vector3.zero, Quaternion.identity, entitiesContainer);
            newEntity.name = type.ToString();
            newEntity.Init(gridPosition, gridNavigator, sprite, type, faction, this);
            if (type == EntityType.Character)
            {
                string pathToConfig = "Configs/" + "DefaultCharacterConfig";
                var config = Resources.Load<CharacterConfig>(pathToConfig);
                newEntity.AddCharacterParams(config, stepDuration);
                newEntity.OnMovementFinished += OnEntityMoved;
                newEntity.OnDestroyed += OnEntityDestroyed;
                newEntity.OnSelected += OnEntitySelected;
                newEntity.OnAttack += OnEntityAttack;
            }
            LevelData.Entities.Add(newEntity);
            LevelData.TilesEntities[gridPosition.x, gridPosition.y] = newEntity;
        }

        private void OnPlayerTurnEnded()
        {
            HideAllAttackTargetSelections();
            HideAllBreadCrumbs();
        }

        private void OnEntityAttack()
        {
            HideAllBreadCrumbs();
            HideAllAttackTargetSelections();
        }

        private void OnEntitySelected(Entity selectedEntity, bool isSelected)
        {
            foreach (var entity in GetEntities())
            {
                if (entity != selectedEntity)
                {
                    entity.EntityView.Deselect();
                }
            }
        }

        public void SetBreadCrumbVisible(int x, int y, bool isVisible, float delay = 0)
        {
            LevelData.Tiles[x, y].SetBreadCrumbVisible(isVisible, delay);
        }

        private void HideAllAttackTargetSelections()
        {
            foreach (var entity in GetEntities())
            {
                entity.EntityView.HideTargetVisuals();
            }
        }

        public void HideAllBreadCrumbs()
        {
            for (int y = 0; y < LevelData.Height; y++)
            {
                for (int x = 0; x < LevelData.Width; x++)
                {
                    LevelData.Tiles[x, y].SetBreadCrumbVisible(false);
                }
            }
        }

        public void PlayQuakeAnimation(int x, int y, int radius)
        {
            if (Time.realtimeSinceStartup < quakeAnimationCooldown)
            {
                return;
            }

            var calculatedTotalDurationOfAnimation = ((radius * 0.25f) * 0.5f) + 0.75f;
            quakeAnimationCooldown = Time.realtimeSinceStartup + calculatedTotalDurationOfAnimation;

            var center = new Vector2(x, y);
            var current = Vector2.zero;

            for (int y2 = y - radius; y2 <= y + radius; y2++)
            {
                for (int x2 = x - radius; x2 <= x + radius; x2++)
                {
                    if (x2 < 0 || x2 >= LevelData.Width ||
                        y2 < 0 || y2 >= LevelData.Height)
                    {
                        continue;
                    }

                    current.x = x2;
                    current.y = y2;

                    var distance = Vector2.Distance(current, center);

                    if (distance <= radius)
                    {
                        var tile = LevelData.Tiles[x2, y2].transform;
                        var originalY = tile.position.y;

                        var delay = (distance * 0.25f) * 0.5f;

                        var sequence = DOTween.Sequence();
                        sequence.PrependInterval(delay);
                        sequence.Append(tile.DOLocalMoveY(originalY + 0.1f, 0.25f).SetEase(Ease.OutBack));
                        sequence.Append(tile.DOLocalMoveY(originalY - 0.1f, 0.25f).SetEase(Ease.OutBack));
                        sequence.Append(tile.DOLocalMoveY(originalY, 0.25f));

                        for (int i = 0; i < LevelData.Entities.Count; i++)
                        {
                            var entity = LevelData.Entities[i];
                            if (entity.GridPosition == current)
                            {
                                var sequence2 = DOTween.Sequence();
                                sequence2.PrependInterval(delay);
                                var entityTransform = entity.gameObject.transform;
                                sequence2.Append(entityTransform.DOLocalMoveY(originalY + 0.1f, 0.25f).SetEase(Ease.OutBack));
                                sequence2.Append(entityTransform.DOLocalMoveY(originalY - 0.1f, 0.25f).SetEase(Ease.OutBack));
                                sequence2.Append(entityTransform.DOLocalMoveY(originalY, 0.25f));
                            }
                        }
                    }
                }
            }

            audio.PlayQuake();
        }

        private void OnEntityDestroyed(Entity entity)
        {
            LevelData.Entities.Remove(entity);
            LevelData.TilesEntities[entity.GridPosition.x, entity.GridPosition.y] = null;
        }

        private void OnEntityMoved(Entity entity, Vector2Int oldPosition, Vector2Int newPosition)
        {
            HideAllBreadCrumbs();
            LevelData.TilesEntities[oldPosition.x, oldPosition.y] = null;
            LevelData.TilesEntities[newPosition.x, newPosition.y] = entity;
        }
    }
}
