using DG.Tweening;
using System;
using Tactics.Battle;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tactics
{
    public class BattleHUD : MonoBehaviour
    {
        [SerializeField] private Button buttonEndTurn;
        [SerializeField] private RawImage Overlay;
        [SerializeField] private GameObject Banner;
        [SerializeField] private Text BannerText;
        [SerializeField] private Text BannerTextShadow;
        [SerializeField] private Text battleLogLabel;

        private void Awake()
        {
            Overlay.color = new Color(0, 0, 0, 0);
            Banner.gameObject.SetActive(false);
        }

        public void Init(BattleManager battleManager)
        {
            battleManager.OnBattleOver += (userWon) =>
            {
                if (userWon)
                {
                    ShowAndHideBanner("Player wins!");
                }
                else
                {
                    ShowAndHideBanner("Enemy wins!");
                }
            };

            battleManager.OnUserCharacterActionsUpdate += (movableChars, attackingChars) =>
            {
                bool canFinishTurn = movableChars.Count == 0;
                buttonEndTurn.interactable = canFinishTurn;
            };

            battleManager.OnUnitAttack += (unit, target, damage) =>
            {
                battleLogLabel.text += $"\n{unit.gameObject.name} dealt {damage} dmg to {target.gameObject.name}!";
            };

            battleManager.OnCharacterMoved += (unit, oldPos, newPos) =>
            {
                battleLogLabel.text += $"\n{unit.gameObject.name} {oldPos.x}:{oldPos.y}->{newPos.x}:{newPos.y}";
            };


            buttonEndTurn.onClick.AddListener(() => battleManager.EndTurn());
        }

        private void ShowAndHideBanner(string text, float showDelay = 0, float hideDelay = 2)
        {
            Overlay.DOColor(new Color(0, 0, 0, 0.5f), 0.25f)
                .SetDelay(showDelay)
                .OnComplete(() =>
                {
                    Banner.gameObject.transform.DOPunchPosition(new Vector3(2f, 2f, 2f), 1f);
                    BannerText.text = text;
                    BannerTextShadow.text = text;
                    Banner.gameObject.SetActive(true);
                });

            Overlay.DOColor(new Color(0, 0, 0, 0), 0.25f)
                .SetDelay(showDelay + hideDelay)
                .OnStart(() =>
                {
                    Banner.gameObject.SetActive(false);
                });
        }
    }
}
