using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BS_Utils.Utilities;
using System.IO;
namespace ReLoader
{


    public class ReLoader : MonoBehaviour
    {
        private AudioTimeSyncController _timeSync;
        private AudioSource _songAudio;
        private PauseController _pauseController;
        private BeatmapObjectManager _beatmapObjectManager;
        private BeatmapObjectSpawnController _spawnController;
        private BeatmapObjectCallbackController _callbackController;
        private BeatmapObjectSpawnMovementData _originalSpawnMovementData;
        private NoteCutSoundEffectManager _seManager;
        private BeatmapDataLoader _dataLoader = new BeatmapDataLoader();
        private StandardLevelInfoSaveData.DifficultyBeatmap _currentDiffBeatmap;
        private CustomPreviewBeatmapLevel _currentLevel;

        private AudioTimeSyncController.InitData _originalInitData;
        private float _songStartTime;

        private bool _init;
        private bool _queuedLoad = false;
        private void Awake()
        {
            StartCoroutine(DelayedSetup());

        }

        private IEnumerator DelayedSetup()
        {
            //Slight delay before grabbing needed objects
            yield return new WaitForSeconds(0.1f);
            _timeSync = Resources.FindObjectsOfTypeAll<AudioTimeSyncController>().First();
            _songAudio = _timeSync.GetField<AudioSource>("_audioSource");
            _beatmapObjectManager = Resources.FindObjectsOfTypeAll<BeatmapObjectManager>().First();
            _spawnController = Resources.FindObjectsOfTypeAll<BeatmapObjectSpawnController>().First();
            _originalSpawnMovementData = _spawnController.GetField<BeatmapObjectSpawnMovementData>("_beatmapObjectSpawnMovementData");
            _pauseController = Resources.FindObjectsOfTypeAll<PauseController>().First();
            _callbackController = Resources.FindObjectsOfTypeAll<BeatmapObjectCallbackController>().First();
            _seManager = Resources.FindObjectsOfTypeAll<NoteCutSoundEffectManager>().First();

            var level = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.difficultyBeatmap.level;
            if (!(level is CustomPreviewBeatmapLevel)) yield break;
            _currentLevel = level as CustomPreviewBeatmapLevel;

            //Get DifficultyBeatmap
            BeatmapDifficulty levelDiff = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.difficultyBeatmap.difficulty;
            BeatmapCharacteristicSO levelCharacteristic = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic;
            _currentDiffBeatmap = _currentLevel.standardLevelInfoSaveData.difficultyBeatmapSets.First(
               x => x.beatmapCharacteristicName == levelCharacteristic.serializedName).difficultyBeatmaps.First(
               x => x.difficulty == levelDiff.ToString());

            _originalInitData = _timeSync.GetField<AudioTimeSyncController.InitData>("_initData");
            _songStartTime = _originalInitData.startSongTime;
            //Initialize if everything successfully grabbed
            _init = true;
        }

        private void Start()
        {


        }

        private void Update()
        {
            //Do nothing if not initialized
            if (!_init || _queuedLoad) return;

            //Reload
            if (Input.GetKeyDown(KeyCode.Space))
            {
                //Set new start time
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    _songStartTime = _songAudio.time;
                }
                _queuedLoad = true;
                ReloadBeatmap();
            }
        }

        private async void ReloadBeatmap()
        {

            string filePath = Path.Combine(_currentLevel.customLevelPath, _currentDiffBeatmap.beatmapFilename);
            string leveljson = File.ReadAllText(filePath);
            //Load new beatmapdata asynchronously
            BeatmapData newBeatmap = await Task.Run(() => _dataLoader.GetBeatmapDataFromJson(leveljson, _currentLevel.beatsPerMinute, _currentLevel.shuffle, _currentLevel.shufflePeriod));

            //Hotswap Beatmap

            ResetTimeSync();
            DestroyObjects();
            ResetNoteCutSoundEffects();
            _callbackController.SetField("_spawningStartTime", _songStartTime);
            _callbackController.SetNewBeatmapData(newBeatmap);
            //Unpause
            if (_pauseController.GetField<bool>("_paused"))
            {
                CheckPracticePlugin();
                _pauseController.HandlePauseMenuManagerDidPressContinueButton();
            }
            _queuedLoad = false;

        }

        public void ResetNoteCutSoundEffects()
        {
            _seManager.SetField("_prevNoteATime", -1f);
            _seManager.SetField("_prevNoteBTime", -1f);

        }
        public void ResetTimeSync()
        {
            AudioTimeSyncController.InitData newInitData = new AudioTimeSyncController.InitData(_originalInitData.audioClip,
                            _songStartTime, _originalInitData.songTimeOffset, _originalInitData.timeScale);
            _timeSync.SetPrivateField("_initData", newInitData);
            _timeSync.StartSong();
        }

        public void CheckPracticePlugin()
        {
            if (GameObject.Find("Song Seeker") != null)
            {
                ForcePracticePlugin();
            }
        }
        public void ForcePracticePlugin()
        {
            PracticePlugin.SongSeeker seeker = Resources.FindObjectsOfTypeAll<PracticePlugin.SongSeeker>().FirstOrDefault();
            if (seeker)
            {
                seeker.SetProperty("PlaybackPosition", (_songStartTime / _songAudio.clip.length));
            }
        }

        public void DestroyObjects()
        {
            _beatmapObjectManager.DissolveAllObjects();
        }


    }
}
