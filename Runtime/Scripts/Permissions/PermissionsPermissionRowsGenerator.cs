using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    public class PermissionsPermissionRowsGenerator : MonoBehaviour, VRC.SDKBase.IEditorOnly
    {
        public VerticalLayoutGroup layoutGroupSpecification;
        public GameObject permissionRowPrefab;
        public GameObject permissionRowTagToUse;
        public RectTransform content;
    }
}
