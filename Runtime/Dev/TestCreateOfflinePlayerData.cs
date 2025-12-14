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
        }
    }
}
