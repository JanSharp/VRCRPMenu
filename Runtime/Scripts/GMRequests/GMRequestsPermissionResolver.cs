using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GMRequestsPermissionResolver : PermissionResolver
    {
        [HideInInspector][SerializeField][SingletonReference] private GMRequestsManagerAPI requestsManager;

        public GMRequestType requestType;
        [PermissionDefinitionReference(nameof(associatedRequestPermissionDef))]
        public string associatedRequestPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition associatedRequestPermissionDef;

        public override void InitializeInstantiated() { }

        public override void Resolve()
        {
            if (associatedRequestPermissionDef.valueForLocalPlayer)
                return;
            GMRequest[] requests = requestsManager.ActiveLocalRequestsRaw;
            int count = requestsManager.ActiveLocalRequestsCount;
            if (count == 0) // Mini optimization.
                return;
            // Find all the ones to delete first, because deleting raises events.
            GMRequest[] toDelete = new GMRequest[count];
            int toDeleteCount = 0;
            for (int i = 0; i < count; i++)
            {
                GMRequest request = requests[i];
                if (request.latencyRequestType == requestType)
                    toDelete[toDeleteCount++] = request;
            }
            for (int i = 0; i < toDeleteCount; i++)
                requestsManager.SendDeleteIA(toDelete[i]);
        }
    }
}
