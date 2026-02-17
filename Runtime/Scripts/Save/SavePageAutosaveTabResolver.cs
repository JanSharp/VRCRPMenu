using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SavePageAutosaveTabResolver : PermissionResolver
    {
        [SerializeField][HideInInspector][SingletonReference] private LockstepAPI lockstep;

        private bool isPaused;

        [PermissionDefinitionReference(nameof(exportGameStatesPDef))]
        public string exportGameStatesPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition exportGameStatesPDef;

        public override void InitializeInstantiated() { }

        public override void Resolve()
        {
            bool shouldPause = !exportGameStatesPDef.valueForLocalPlayer;
            if (isPaused == shouldPause)
                return;
            isPaused = shouldPause;
            if (shouldPause)
                lockstep.StartScopedAutosavePause();
            else
                lockstep.StopScopedAutosavePause();
        }
    }
}
