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

        private static Root _instance;

        void Awake()
        {
            _instance = this;
        }

        void Start()
        {
            hud.Init(battleManager);
            battleManager.Init(hud, input);
        }

    }
}
