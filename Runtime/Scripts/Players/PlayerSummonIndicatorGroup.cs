using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerSummonIndicatorGroup : WannaBeClass
    {
        [HideInInspector][SerializeField][SingletonReference] private PlayerSummonManagerAPI summonManager;

        [System.NonSerialized] public Vector3 centerPosition;
        [System.NonSerialized] public Quaternion centerRotation;
        [System.NonSerialized] public GameObject[] indicators;
        public bool IsActive => indicators != null;

        public void Show(Vector3 centerPosition, Quaternion centerRotation, GameObject[] indicators)
        {
            this.centerPosition = centerPosition;
            this.centerRotation = centerRotation;
            this.indicators = indicators;
        }

        public void Hide()
        {
            if (!IsActive)
                return;
            ((Internal.PlayerSummonManager)summonManager).ReturnIndicatorGroup(this);
            indicators = null;
        }

        public void HideDelayed(float seconds)
        {
            SendCustomEventDelayedSeconds(nameof(Hide), seconds);
        }
    }
}
