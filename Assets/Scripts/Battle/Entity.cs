using DG.Tweening;
using SharedData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Tactics.View.Entities;
using Tactics.Helpers.Promises;
using Tactics.View.Level;

namespace Tactics.Battle
{
    [SelectionBase]
    public class Entity : MonoBehaviour
    {
        public event Action<Vector2Int, int, float> OnStep = (newPosition, stepIndex, stepDuration) => { };
        public event Action<Entity, Vector2Int, Vector2Int> OnMovementFinished = (entity, oldPosition, newPosition) => { };
        public event Action OnAttack = () => { };
        public event Action<float> OnDamaged = (currentHealthPercentage) => { };
        public event Action<Entity> OnSelected = (entity) => { };
        public event Action<bool> OnTargeted = (isTargeted) => { };
        public event Action<Entity> OnDestroyed = (entity) => { };

        public EntityType Type { get; private set; }
        public EntityFaction Faction { get; private set; }
        public Vector2Int GridPosition { get; private set; }
        public EntityView EntityView { get; private set; }

        public int MaxWalkDistance { get; private set; }
        public int AttackDamage { get; private set; }
        public int HealthPoints { get; private set; }
        public int AttackRange { get; private set; }

        public List<Vector2Int> possibleMoveTargets = new List<Vector2Int>();
        public List<Entity> possibleAttackTargets = new List<Entity>();

        private int maxHealth;
        private float stepDuration = .2f;
        private LevelView levelService;
        private GridNavigator gridNavigator;
        private BattleManager battleManager;

        public void Init(Vector2Int gridPosition, BattleManager battleManager, GridNavigator gridNavigator, Sprite sprite, EntityType type, EntityFaction faction, LevelView levelService)
        {
            this.levelService = levelService;
            this.gridNavigator = gridNavigator;
            this.battleManager = battleManager;
            GridPosition = gridPosition;
            Type = type;
            Faction = faction;
            EntityView = GetComponent<EntityView>() ?? gameObject.AddComponent<EntityView>();
            EntityView.Init(this, sprite, type, gridPosition, battleManager);
        }

        public void AddCharacterParams(CharacterConfig config)
        {
            this.AttackRange = config.attackRange;
            this.AttackDamage = config.attackDamage;
            this.maxHealth = config.maxHealth;
            this.HealthPoints = config.maxHealth;
            this.MaxWalkDistance = config.moveDistance;
        }

        public IPromise MakeAITurn()
        {
            EntityFaction opposingFaction = Faction == EntityFaction.Player ? EntityFaction.Enemy : EntityFaction.Player;
            bool attackSuccess = TryAttackFractionInRange(opposingFaction);
            if (attackSuccess == false)
            {
                Entity closestPlayerCharacter = battleManager.GetClosestCharacter(GridPosition, opposingFaction);
                if (closestPlayerCharacter != null)
                {
                    List<Vector2Int> path = gridNavigator.GetPath(this, closestPlayerCharacter.GridPosition, MaxWalkDistance, closestPlayerCharacter);
                    Vector2Int moveTarget = path.Last() == closestPlayerCharacter.GridPosition ? path[path.Count - 2] : path[path.Count - 1];
                    IPromise movePromise = Move(moveTarget);
                    movePromise.Done(() => TryAttackFractionInRange(opposingFaction));
                    return movePromise;
                }
            }
            return Deferred.GetFromPool().Resolve();
        }

        public void SetTargeted(bool isTargeted)
        {
            OnTargeted(isTargeted);
        }

        public IPromise Move(Vector2Int target)
        {
            List<Vector2Int> path = gridNavigator.GetPath(this, target, MaxWalkDistance);
            Deferred moveDeferred = Deferred.GetFromPool();
            if (path != null)
            {
                Vector2Int oldPosition = GridPosition;

                for (int stepIdx = 0; stepIdx < path.Count; stepIdx++)
                {
                    Vector2Int newPosition = path[stepIdx];
                    OnStep(newPosition, stepIdx, stepDuration);
                }

                Timers.Instance.Wait(path.Count * stepDuration)
                    .Done(() =>
                    {
                        GridPosition = path[path.Count - 1];
                        OnMovementFinished(this, oldPosition, GridPosition);
                        moveDeferred.Resolve();
                    });
                return moveDeferred;
            }
            else
            {
                return moveDeferred.Resolve();
            }
        }

        public void Select(bool movementAllowed, bool attackAllowed)
        {
            this.possibleMoveTargets.Clear();
            this.possibleAttackTargets.Clear();

            if (movementAllowed)
            {
                gridNavigator.DoActionOnNeighbours(GridPosition, MaxWalkDistance, true,
                    (depth, gridPosition) =>
                    {
                        possibleMoveTargets.Add(gridPosition);
                    });
            }

            if (attackAllowed)
            {
                EntityFaction opposingFaction = Faction == EntityFaction.Player ? EntityFaction.Enemy : EntityFaction.Player;
                List<Entity> entitiesInRange = battleManager.GetEntitiesInRange(this, opposingFaction);
                foreach (Entity entity in entitiesInRange)
                {
                    entity.SetTargeted(true);
                    possibleAttackTargets.Add(entity);
                }
            }
            OnSelected(this);
        }

        public bool CanAttack(Entity entity)
        {
            return possibleAttackTargets.Contains(entity);
        }

        public void Damage(int damage)
        {
            HealthPoints -= damage;
            if (HealthPoints <= 0)
            {
                OnDestroyed(this);
            }

            float currentHealthPercentage = (float)HealthPoints / (float)maxHealth;
            OnDamaged(currentHealthPercentage);
        }

        public void Attack(Entity target)
        {
            OnAttack();
            target.Damage(AttackDamage);
        }

        public bool CanMove(Vector2Int position)
        {
            return possibleMoveTargets.Contains(position);
        }

        private bool TryAttackFractionInRange(EntityFaction targetFaction)
        {
            List<Entity> entitiesInRange = battleManager.GetEntitiesInRange(this, targetFaction);
            if (entitiesInRange.Count > 0)
            {
                Attack(entitiesInRange[0]);
                return true;
            }
            return false;
        }
    }
}
