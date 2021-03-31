using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IPA;
using IPA.Config;
using IPA.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine;
using IPALogger = IPA.Logging.Logger;

namespace ReLoader
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static string Name => "ReLoader";
        [Init]
        public void Init(IPALogger logger)
        {
            Logger.log = logger;
        }

        [OnStart]
        public void OnApplicationStart()
        {
            BS_Utils.Utilities.BSEvents.gameSceneLoaded += BSEvents_gameSceneLoaded;
        }

        private void BSEvents_gameSceneLoaded()
        {
            if (!BS_Utils.Plugin.LevelData.IsSet || BS_Utils.Plugin.LevelData.Mode != BS_Utils.Gameplay.Mode.Standard) return;
           bool  PracticeMode = (BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.practiceSettings != null && !BS_Utils.Gameplay.Gamemode.IsIsolatedLevel
                   && Resources.FindObjectsOfTypeAll<MissionLevelGameplayManager>().FirstOrDefault() == null);

            if (PracticeMode)
                new GameObject("ReLoader").AddComponent<ReLoader>();


        }

        [OnExit]
        public void OnApplicationQuit()
        {

        }

    }
}
