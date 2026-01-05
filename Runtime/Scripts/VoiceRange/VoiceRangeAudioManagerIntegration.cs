using Sylan.AudioManager;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceRangeAudioManagerIntegration : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private VoiceRangeManagerAPI voiceRangeManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][SingletonReference] private AudioSettingManagerWrapper audioManagerWrapper;
        private AudioSettingManager audioManager;

        private const string VoiceRangeSettingId = "rp-menu.voice-range";

        private bool isInitialized = false;
        private int[] priorities;
        private DataList[] audioSettings;

        private void Start()
        {
            audioManager = audioManagerWrapper.audioManager;
        }

        private void Initialize()
        {
            isInitialized = true;

            int count = voiceRangeManager.VoiceRangeDefinitionCount;
            priorities = new int[count];
            audioSettings = new DataList[count];
            for (int i = 0; i < count; i++)
            {
                VoiceRangeDefinition def = voiceRangeManager.GetVoiceRangeDefinition(i);
                priorities[i] = def.audioSettingPriority;
                DataList audioSetting = new DataList();
                audioSetting.Add(new DataToken());
                audioSetting.Add(new DataToken());
                audioSetting.Add(new DataToken());
                audioSetting.Add(new DataToken());
                audioSetting.Add(new DataToken());
                audioSetting[AudioSettingManager.VOICE_GAIN_INDEX] = def.gain;
                audioSetting[AudioSettingManager.RANGE_NEAR_INDEX] = def.nearRange;
                audioSetting[AudioSettingManager.RANGE_FAR_INDEX] = def.farRange;
                audioSetting[AudioSettingManager.VOLUMETRIC_RADIUS_INDEX] = def.volumetricRange;
                audioSetting[AudioSettingManager.VOICE_LOWPASS_INDEX] = def.lowPass;
                audioSettings[i] = audioSetting;
            }

            count = playerDataManager.AllCorePlayerDataCount;
            CorePlayerData[] players = playerDataManager.AllCorePlayerDataRaw;
            for (int i = 0; i < count; i++)
                UpdateAudioSettingForPlayer(voiceRangeManager.GetVoiceRangePlayerData(players[i]));
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit() => Initialize();

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp() => Initialize();

        [VoiceRangeEvent(VoiceRangeEventType.OnVoiceRangeIndexChangedInLatency)]
        public void OnVoiceRangeIndexChangedInLatency()
        {
            UpdateAudioSettingForPlayer(voiceRangeManager.PlayerDataForEvent);
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataCreated)]
        public void OnPlayerDataCreated()
        {
            UpdateAudioSettingForPlayer(voiceRangeManager.GetVoiceRangePlayerData(playerDataManager.PlayerDataForEvent));
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataWentOnline)]
        public void OnPlayerDataWentOnline()
        {
            UpdateAudioSettingForPlayer(voiceRangeManager.GetVoiceRangePlayerData(playerDataManager.PlayerDataForEvent));
        }

        private void UpdateAudioSettingForPlayer(VoiceRangePlayerData player)
        {
            if (!isInitialized || player.core.isLocal)
                return;

            VRCPlayerApi playerApi = player.core.playerApi;
            if (!Utilities.IsValid(playerApi))
                return;

            int index = player.latencyVoiceRangeIndex;
            audioManager.RemoveAudioSetting(playerApi, VoiceRangeSettingId);
            audioManager.AddAudioSetting(playerApi, VoiceRangeSettingId, priorities[index], audioSettings[index]);
        }
    }
}
