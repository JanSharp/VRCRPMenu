using Sylan.AudioManager;
using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [SingletonScript("db7677415ffdc97548340df437e1510a")] // Runtime/Prefabs/Managers/AudioSettingManagerWrapper.prefab
    public class AudioSettingManagerWrapper : UdonSharpBehaviour
    {
        public AudioSettingManager audioManager;
    }
}
