using TMPro;
using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GMProxySpawnButton : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private GMProxiesManagerAPI gmProxiesManager;
        [HideInInspector][SerializeField][SingletonReference] private ItemSpawnLocationHelperAPI itemSpawnLocationHelper;

        public TMP_InputField displayNameField;
        public ToggleGroupWithFloatValues scaleToggles;

        public void OnClick()
        {
            itemSpawnLocationHelper.DetermineItemSpawnLocation(this, nameof(OnLocationDetermined), new object[]
            {
                scaleToggles.GetValue(),
                displayNameField.text,
            });
        }

        public void OnLocationDetermined()
        {
            object[] callbackData = (object[])itemSpawnLocationHelper.CallbackCustomData;
            gmProxiesManager.CreateGMProxy(
                itemSpawnLocationHelper.DeterminedPosition,
                itemSpawnLocationHelper.DeterminedRotation,
                (float)callbackData[0],
                (string)callbackData[1]);
        }
    }
}
