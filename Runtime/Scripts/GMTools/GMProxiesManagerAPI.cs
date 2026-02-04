using UnityEngine;

namespace JanSharp
{
    [SingletonScript("a5d639fb553104b0595a01f3266d70f7")] // Runtime/Prefabs/Managers/GMProxiesManager.prefab
    public abstract class GMProxiesManagerAPI : PermissionResolver
    {
        public abstract GMProxyDefinition GetGMProxyDefinition(string internalName);
        public abstract void CreateGMProxy(string entityPrototypeName, Vector3 position, Quaternion rotation, float scale, string displayName);
    }
}
