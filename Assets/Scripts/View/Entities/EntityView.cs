﻿using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using SharedData;
using UnityEngine;
using Tactics.Battle;
using Tactics.Helpers;
using Tactics.View.Level;

namespace Tactics.View.Entities
{
    public class EntityView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer Renderer;
        [SerializeField] private GameObject SlashPrefab;
        [SerializeField] private GameObject Selection;
        [SerializeField] private GameObject AttackTargetSelection;
        [SerializeField] private GameObject HealthBarContainer;
        [SerializeField] private GameObject HealthBar;
        [SerializeField] private GameObject attackAvailableIndicator;
        [SerializeField] private GameObject moveAvailableIndicator;

        private Entity entityOwner;
        private LevelView levelService;

        public void Init(Entity entityOwner,
                         Sprite sprite,
                         EntityType type,
                         Vector2Int gridPosition,
                         BattleManager battleManager)
        {
            this.entityOwner = entityOwner;

            entityOwner.OnDamaged += OnEntityDamaged;
            entityOwner.OnDestroyed += OnEntityDestroyed;
            entityOwner.OnTargeted += OnEntityTargeted;
            entityOwner.OnStep += OnEntityStep;

            battleManager.OnCharacterAttack += () =>
            {
                HideTargetVisuals();
            };
            battleManager.OnUserCharacterActionsUpdate += (movableChars, attackingChars) =>
            {
                bool canMove = movableChars.Contains(entityOwner);
                bool canAttack = attackingChars.Contains(entityOwner);
                attackAvailableIndicator.SetActive(canAttack);
                moveAvailableIndicator.SetActive(canMove);
            };

            entityOwner.OnSelected += (entity) =>
            {
                bool ownerSelected = entity == entityOwner;
                Selection.gameObject.SetActive(ownerSelected);
                if (ownerSelected)
                {
                    Selection.gameObject.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.5f);
                    Root.Audio.PlaySelect();
                }
            };

            Renderer.sortingOrder = GridHelper.GetSortingOrder(gridPosition.x, gridPosition.y);
            Renderer.sprite = sprite;

            //Turn actions indicators are turned off for everyone
            //including user's characters
            attackAvailableIndicator.SetActive(false);
            moveAvailableIndicator.SetActive(false);

            HealthBarContainer.SetActive(false);
            HealthBar.SetActive(false);
            switch (type)
            {
                case EntityType.Character:
                    HealthBarContainer.SetActive(true);
                    HealthBar.SetActive(true);
                    HealthBar.transform.localScale = Vector3.one;
                    break;
                case EntityType.Obstacle:
                    break;
                default:
                    Debug.LogError($"EntityView doesn't support entity of type{type}");
                    break;
            }

            transform.position = GridHelper.ToWorldCoordinates(gridPosition);
        }

        public void HideTargetVisuals()
        {
            OnEntityTargeted(false);
        }

        private void OnEntityStep(Vector2Int to, int stepIndex, float stepDuration)
        {
            Vector2 toPositionWorld = GridHelper.ToWorldCoordinates(to.x, to.y);

            transform.DOJump(toPositionWorld, 0.25f, 1, stepDuration).SetEase(Ease.InQuint)
                    .SetDelay(stepDuration * stepIndex)
                    .OnComplete(() =>
                    {
                        Renderer.sortingOrder = GridHelper.GetSortingOrder(to.x, to.y);
                        Root.Audio.PlayMove();
                    });
        }

        private void OnEntityDamaged(float currentHealthPercentage)
        {
            transform.DOPunchRotation(new Vector3(0, 0, 20), 0.5f)
                .OnStart(() =>
                {
                    InstantiateSlash(entityOwner.GridPosition.x, entityOwner.GridPosition.y);
                    Root.Audio.PlayTakeDamage();
                })
                .OnComplete(() =>
                {
                    transform.rotation = Quaternion.identity;
                });

            float clampedHealthPercentage = Mathf.Clamp01(currentHealthPercentage);
            HealthBarContainer.transform.DOShakePosition(0.5f, new Vector3(0.1f, 0.1f, 0));
            HealthBar.transform.DOScaleX(clampedHealthPercentage, 0.25f);
        }

        private void OnEntityTargeted(bool state)
        {
            AttackTargetSelection.gameObject.SetActive(state);

            if (state)
            {
                AttackTargetSelection.gameObject.transform.localScale = Vector3.one;
                AttackTargetSelection.gameObject.transform.DOScale(new Vector3(1.2f, 1.2f, 1f), 0.5f).SetEase(Ease.OutQuint).SetLoops(int.MaxValue, LoopType.Yoyo);
                Root.Audio.PlaySelectTarget();
            }
            else
            {
                DOTween.Kill(AttackTargetSelection.gameObject.transform);
            }
        }

        private void OnEntityDestroyed(Entity entity)
        {
            transform.DOMoveX(UnityEngine.Random.Range(-3f, 3f), 4f).SetEase(Ease.OutQuart);
            transform.GetChild(0).DOLocalRotate(new Vector3(0, 0, 180f), 2f);

            transform.DOMoveY(transform.position.y + 1f, 0.5f)
                .SetEase(Ease.OutQuint)
                .OnComplete(() =>
                {
                    transform.DOMoveY(-10, 5f).SetEase(Ease.OutQuint).OnComplete(() =>
                    {
                        gameObject.SetActive(false);
                    });
                });

            Root.Audio.PlayDeath();
        }

        private void InstantiateSlash(int x, int y)
        {
            var slash = GameObject.Instantiate(SlashPrefab, Vector3.zero, Quaternion.identity);
            slash.name = SlashPrefab.name;
            slash.transform.position = GridHelper.ToWorldCoordinates(x, y);
            slash.transform.GetChild(0).rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0, 360));
            GameObject.Destroy(slash, 1f);
        }
    }
}
