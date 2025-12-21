using TMPro;
using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TestCreateOfflinePlayerData : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;

        public TMP_InputField displayNameField;

        public void OnClick()
        {
            playerDataManager.SendCreateOfflinePlayerDataIA(displayNameField.text.Trim());
            // CreateAStupidAmountOfPlayers();
        }

        private int index = 0;
        public void CreateAStupidAmountOfPlayers()
        {
            playerDataManager.SendCreateOfflinePlayerDataIA($"Player {index}");
            index++;
            if (index < 1000)
                SendCustomEventDelayedSeconds(nameof(CreateAStupidAmountOfPlayers), 0.2f);
            else
                index = 0;
        }
    }
}
