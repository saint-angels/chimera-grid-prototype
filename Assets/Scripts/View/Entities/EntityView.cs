using System;
using System.Collections;
using System.Collections.Generic;
using Helpers;
using Helpers.Promises;
using DG.Tweening;
using SharedData;
using UnityEngine;

namespace Assets.Scripts.Presentation.Entities
{
    public class EntityView : MonoBehaviour
    {
        public SpriteRenderer Renderer;
        public GameObject SlashPrefab;
        public GameObject Selection;
        public GameObject AttackTargetSelection;
        public GameObject HealthBarContainer;
        public GameObject HealthBar;

        private AudioComponent audio;
        private Entity entityOwner;
        private LevelService levelService;

        private void Awake()
        {
            audio = GameObject.Find("Audio").GetComponent<AudioComponent>();
        }

        public void Init(Entity entityOwner, Sprite sprite, EntityType type, Vector2Int gridPosition, LevelService levelService)
        {
            this.levelService = levelService;
            this.entityOwner = entityOwner;

            entityOwner.OnDamaged += OnEntityDamaged;
            entityOwner.OnDestroyed += OnEntityDestroyed;
            entityOwner.OnSelected += OnEntitySelected;
            entityOwner.OnTargeted += OnEntityTargeted;
            entityOwner.OnStep += OnEntityStep;

            Renderer.sortingOrder = GridHelper.GetSortingOrder(gridPosition.x, gridPosition.y);
            Renderer.sprite = sprite;
            if (type == EntityType.Character)
            {
                HealthBarContainer.SetActive(true);
                HealthBar.SetActive(true);
                HealthBar.transform.localScale = Vector3.one;
            }
            transform.position = GridHelper.ToWorldCoordinates(gridPosition);
        }

        public void Deselect()
        {
            OnEntitySelected(entityOwner, false);
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
                        audio.PlayMove();
                    });
        }

        private void OnEntityDamaged(float currentHealthPercentage)
        {
            transform.DOPunchRotation(new Vector3(0, 0, 20), 0.5f)
                .OnStart(() =>
                {
                    InstantiateSlash(entityOwner.GridPosition.x, entityOwner.GridPosition.y);
                    audio.PlayTakeDamage();
                })
                .OnComplete(() =>
                {
                    transform.rotation = Quaternion.identity;
                });

            float clampedHealthPercentage = Mathf.Clamp01(currentHealthPercentage);
            HealthBarContainer.transform.DOShakePosition(0.5f, new Vector3(0.1f, 0.1f, 0));
            HealthBar.transform.DOScaleX(clampedHealthPercentage, 0.25f);
        }

        private void OnEntitySelected(Entity selectedEntity, bool isSelected)
        {
            Selection.gameObject.SetActive(isSelected);

            if (isSelected)
            {
                Selection.gameObject.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.5f);
                audio.PlaySelect();

                levelService.HideAllBreadCrumbs();
                foreach (var moveTargetPosition in selectedEntity.possibleMoveTargets)
                {
                    levelService.SetBreadCrumbVisible(moveTargetPosition.x, moveTargetPosition.y, true);
                }
            }
        }

        private void OnEntityTargeted(bool state)
        {
            AttackTargetSelection.gameObject.SetActive(state);

            if (state)
            {
                AttackTargetSelection.gameObject.transform.localScale = Vector3.one;
                AttackTargetSelection.gameObject.transform.DOScale(new Vector3(1.2f, 1.2f, 1f), 0.5f).SetEase(Ease.OutQuint).SetLoops(int.MaxValue, LoopType.Yoyo);
                audio.PlaySelectTarget();
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

            audio.PlayDeath();
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
