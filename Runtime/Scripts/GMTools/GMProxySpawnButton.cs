using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GMProxySpawnButton : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private GMProxiesManagerAPI gmProxiesManager;
        [HideInInspector][SerializeField][SingletonReference] private ItemSpawnLocationHelperAPI itemSpawnLocationHelper;

        public TMP_InputField displayNameField;
        public ToggleGroupWithFloatValues scaleToggles;
        public Image accentColorImage;
        public string gmProxyDefInternalName;
        private GMProxyDefinition gmProxyDefinition;

        private void Initialize()
        {
            gmProxyDefinition = gmProxiesManager.GetGMProxyDefinition(gmProxyDefInternalName);
            accentColorImage.color *= gmProxyDefinition.color;
        }

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
                gmProxyDefinition.entityPrototypeName,
                itemSpawnLocationHelper.DeterminedPosition,
                itemSpawnLocationHelper.DeterminedRotation,
                (float)callbackData[0],
                (string)callbackData[1]);
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit() => Initialize();

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp() => Initialize();
    }
}
