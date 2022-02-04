using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Tactics.Battle;
using Tactics.Helpers;
using Tactics.View;

namespace Tactics
{
    public class Root : MonoBehaviour
    {
        public static AudioComponent Audio => _instance.audio;

        [SerializeField] private Tactics.BattleHUD hud;
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private InputSystem input;
        [SerializeField] private new AudioComponent audio;
        [SerializeField] private GridNavigator gridNavigator = null;

        private static Root _instance;

        void Awake()
        {
            _instance = this;
            DG.Tweening.DOTween.SetTweensCapacity(500, 100);
        }

        void Start()
        {
            hud.Init(battleManager);
            battleManager.Init(input, gridNavigator);
            gridNavigator.Init(battleManager);
            input.Init(battleManager);
        }
    }
}
