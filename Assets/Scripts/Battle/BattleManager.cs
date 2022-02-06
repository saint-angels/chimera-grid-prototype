using System;
using System.Collections.Generic;
using System.Linq;
using Tactics;
using SharedData;
using UnityEngine;
using Tactics.Helpers.Promises;
using Tactics.View.Level;
using Tactics.SharedData;

namespace Tactics.Battle
{
    public class BattleManager : MonoBehaviour
    {
        enum TurnState
        {
            UserIdle,
            UserCharSelected,
            ActionInProgress,
        }

        public Action<bool> OnBattleOver = (userWon) => { };
        public Action OnPlayerTurnEnded = () => { };
        public Action<Entity, Vector2Int, Vector2Int> OnCharacterMoved = (unit, oldPos, newPos) => { };
        public Action<Entity> OnEntitySelected = (entity) => { };
        public Action<List<Entity>, List<Entity>> OnUserCharacterActionsUpdate = (movable, attacking) => { };
        public Action<Entity, Entity, int> OnUnitAttack = (unit, actionType, damage) => { };
        public Action OnUnitMoveStarted = () => { };
        public Action OnUnitDeselected = () => { };

        [SerializeField] private Transform entityContainer = null;

        private Entity entityPrefab;
        private Entity selectedCharacter;

        private TurnState turnState;

        private LevelData LevelData;
        private List<Entity> MovableUserUnits = new List<Entity>();
        private List<Entity> AttackingUserUnits = new List<Entity>();

        public void Init(InputSystem inputSystem, GridNavigator gridNavigator, LevelView levelView)
        {
            MovableUserUnits = new List<Entity>();
            AttackingUserUnits = new List<Entity>();

            string levelText = Resources.Load<TextAsset>($"Levels/Level1").text;
            string[] rows = levelText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int width = int.Parse(rows[0]);
            int height = int.Parse(rows[1]);

            LevelData = new LevelData
            {
                Width = width,
                Height = height,
                Entities = new List<Entity>(),
                TilesEntities = new Entity[width, height]
            };

            levelView.Init(this, LevelData, rows);

            // Create Entities
            for (int y = 0; y < height; y++)
            {
                var row = rows[4 + height + y];

                for (int x = 0; x < width; x++)
                {
                    Vector2Int gridPosition = new Vector2Int(x, y);
                    switch (row[x])
                    {
                        case 'e':
                            InstantiateEntity(gridPosition, EntityType.Character, EntityFaction.Enemy, gridNavigator);
                            break;

                        case 'p':
                            InstantiateEntity(gridPosition, EntityType.Character, EntityFaction.Player, gridNavigator);
                            break;

                        case '#':
                            InstantiateEntity(gridPosition, EntityType.Obstacle, EntityFaction.Neutral, gridNavigator);
                            break;
                    }
                }
            }

            StartPlayerTurn();

            void InstantiateEntity(Vector2Int gridPosition, EntityType type, EntityFaction faction, GridNavigator gridNavigator)
            {

                Entity entityPrefab;
                switch (type)
                {
                    case EntityType.Character:
                        string unitName = faction == EntityFaction.Player ? "UnitUser" : "UnitEnemy";
                        entityPrefab = Resources.Load<Entity>($"Prefabs/{unitName}");
                        break;
                    case EntityType.Obstacle:
                        entityPrefab = Resources.Load<Entity>($"Prefabs/Obstacle");
                        break;
                    default:
                        Debug.LogError($"Creating entity of type {type} is not supported");
                        return;
                }

                Entity newEntity = GameObject.Instantiate(entityPrefab, Vector3.zero, Quaternion.identity, entityContainer);
                newEntity.name = $"{type}_{faction}";
                newEntity.Init(gridPosition, this, gridNavigator, type, faction, levelView);
                if (type == EntityType.Character)
                {
                    string pathToConfig = "Configs/" + "DefaultCharacterConfig";
                    var config = Resources.Load<CharacterConfig>(pathToConfig);
                    newEntity.AddCharacterParams(config);
                    newEntity.OnMoveStarted += () => OnUnitMoveStarted();
                    newEntity.OnMoved += (entity, oldPosition, newPosition) =>
                    {
                        LevelData.TilesEntities[oldPosition.x, oldPosition.y] = null;
                        LevelData.TilesEntities[newPosition.x, newPosition.y] = entity;
                        OnCharacterMoved(entity, oldPosition, newPosition);
                    };
                    newEntity.OnDestroyed += (entity) =>
                    {
                        LevelData.Entities.Remove(entity);
                        LevelData.TilesEntities[entity.GridPosition.x, entity.GridPosition.y] = null;
                    };
                    newEntity.OnSelected += (selectedEntity) =>
                    {
                        OnEntitySelected(selectedEntity);
                    };
                    newEntity.OnAttack += (entity, target, damage) => OnUnitAttack(entity, target, damage);
                }
                LevelData.Entities.Add(newEntity);
                LevelData.TilesEntities[gridPosition.x, gridPosition.y] = newEntity;
            }
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

        //There could be only 1 entity at each tile at a time.
        public Entity TryGetEntityAtPosition(int x, int y)
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
                Entity entity = TryGetEntityAtPosition(offsetPosition.x, offsetPosition.y);
                if (entity != null && entity != attacker && entity.Faction == targetFaction)
                {
                    entitiesList.Add(entity);
                }
            }

            //Check y axis
            for (int yOffset = -range; yOffset <= range; yOffset++)
            {
                Vector2Int offsetPosition = new Vector2Int(position.x, position.y + yOffset);
                Entity entity = TryGetEntityAtPosition(offsetPosition.x, offsetPosition.y);
                if (entity != null && entity != attacker && entity.Faction == targetFaction)
                {
                    entitiesList.Add(entity);
                }
            }

            return entitiesList;
        }

        public Entity GetClosestCharacter(Vector2Int targetPosition, EntityFaction faction)
        {
            return LevelData.Entities
                        .Where(p => p.Type == EntityType.Character && p.Faction == faction)
                        .OrderBy((entity) => Vector2Int.Distance(targetPosition, entity.GridPosition))
                        .FirstOrDefault();
        }

        public bool IsPointOnLevelGrid(int x, int y)
        {
            bool outOfGridBounds = x >= LevelData.Width || x < 0 || y >= LevelData.Height || y < 0;
            return outOfGridBounds == false;
        }

        public void EndTurn()
        {
            OnPlayerTurnEnded();
            turnState = TurnState.ActionInProgress;
            PlayEnemyAITurn().Done(() =>
            {
                bool isGameOver = CheckIsGameOver();
                if (isGameOver == false)
                {
                    StartPlayerTurn();
                }
            });

            IPromise PlayEnemyAITurn()
            {
                IPromise enemyTurnPromises = Deferred.GetFromPool().Resolve();
                var enemies = GetCharacters(EntityFaction.Enemy);

                foreach (var enemy in enemies)
                {
                    enemyTurnPromises = enemyTurnPromises.Then(() => enemy.MakeAITurn());
                }
                return enemyTurnPromises;
            }
        }

        public void HandleCharacterClick(Entity clickedCharacter)
        {
            switch (turnState)
            {
                case TurnState.UserIdle:
                    if (clickedCharacter.Faction == EntityFaction.Player)
                    {
                        SelectUserCharacter(clickedCharacter);
                    }
                    break;
                case TurnState.UserCharSelected:
                    switch (clickedCharacter.Faction)
                    {
                        case EntityFaction.Player:
                            SelectUserCharacter(clickedCharacter);
                            break;
                        case EntityFaction.Enemy:
                            bool hasAttackAction = AttackingUserUnits.Contains(selectedCharacter);
                            if (hasAttackAction && selectedCharacter.CanAttack(clickedCharacter))
                            {
                                AttackingUserUnits.Remove(selectedCharacter);
                                OnUserCharacterActionsUpdate(MovableUserUnits, AttackingUserUnits);
                                selectedCharacter.Attack(clickedCharacter);

                                bool isGameOver = CheckIsGameOver();
                                if (isGameOver == false)
                                {
                                    bool canMove = MovableUserUnits.Contains(selectedCharacter);
                                    if (canMove)
                                    {
                                        SelectUserCharacter(selectedCharacter);
                                    }
                                    else
                                    {
                                        //Select the next character that can still do an action
                                        if (MovableUserUnits.Count != 0)
                                        {
                                            SelectUserCharacter(MovableUserUnits[0]);
                                        }
                                        else if (AttackingUserUnits.Count != 0)
                                        {
                                            SelectUserCharacter(AttackingUserUnits[0]);
                                        }
                                    }
                                }
                            }
                            break;
                    }
                    break;
            }
        }

        public void HandleEmptyTileClick(Vector2Int gridPosition)
        {
            switch (turnState)
            {
                case TurnState.UserIdle:
                    break;
                case TurnState.UserCharSelected:
                    if (selectedCharacter.CanMove(gridPosition))
                    {
                        turnState = TurnState.ActionInProgress;
                        var movingCharacter = selectedCharacter;

                        movingCharacter.Move(gridPosition)
                            .Done(() =>
                            {
                                MovableUserUnits.Remove(movingCharacter);
                                OnUserCharacterActionsUpdate(MovableUserUnits, AttackingUserUnits);

                                bool isGameOver = CheckIsGameOver();
                                if (isGameOver == false)
                                {
                                    bool canAttack = AttackingUserUnits.Contains(movingCharacter);
                                    if (canAttack)
                                    {
                                        SelectUserCharacter(movingCharacter);
                                    }
                                    else
                                    {
                                        //Select the next character that can still do an action
                                        if (MovableUserUnits.Count != 0)
                                        {
                                            SelectUserCharacter(MovableUserUnits[0]);
                                        }
                                        else if (AttackingUserUnits.Count != 0)
                                        {
                                            SelectUserCharacter(AttackingUserUnits[0]);
                                        }
                                    }
                                }
                            });
                    }
                    break;
            }
        }

        private bool CheckIsGameOver()
        {
            var enemyCharacters = GetCharacters(EntityFaction.Enemy);
            var playerCharacters = GetCharacters(EntityFaction.Player);

            if (enemyCharacters.Count == 0)
            {
                OnBattleOver(true);
                return true;
            }
            else if (playerCharacters.Count == 0)
            {
                OnBattleOver(false);
                return true;
            }

            return false;
        }

        private void StartPlayerTurn()
        {
            MovableUserUnits.Clear();
            AttackingUserUnits.Clear();
            MovableUserUnits.AddRange(GetCharacters(EntityFaction.Player));
            AttackingUserUnits.AddRange(MovableUserUnits);
            OnUserCharacterActionsUpdate(MovableUserUnits, AttackingUserUnits);

            turnState = TurnState.UserIdle;
        }

        private void SelectUserCharacter(Entity selectedCharacter)
        {
            OnUnitDeselected();
            this.selectedCharacter = selectedCharacter;

            bool movementAllowed = MovableUserUnits.Contains(selectedCharacter);
            bool attackAllowed = AttackingUserUnits.Contains(selectedCharacter);
            selectedCharacter.Select(movementAllowed, attackAllowed);
            turnState = TurnState.UserCharSelected;
        }
    }
}
