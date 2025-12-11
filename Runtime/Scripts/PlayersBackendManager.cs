using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [SingletonDependency(typeof(PermissionManagerAPI))]
    public class PlayersBackendManager : PlayersBackendManagerAPI
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;

        private void Start()
        {
            playerDataManager.RegisterCustomPlayerData<RPPlayerData>(nameof(RPPlayerData));
        }
    }
}
