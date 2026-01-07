using UdonSharp;

namespace JanSharp
{
    public enum PermissionsPagesEventType
    {
        // /// <summary>
        // /// <para>Use <see cref="PermissionManagerAPI.CreatedPermissionGroup"/> to get the newly created
        // /// permission group.</para>
        // /// <para>Use <see cref="PermissionManagerAPI.CreatedPermissionGroupDuplicationSource"/> to get the
        // /// permission group which was duplicated to create the new group.</para>
        // /// <para>Runs inside of an input action, making API properties for input actions from lockstep or
        // /// other systems available.</para>
        // /// <para>Game state safe.</para>
        // /// </summary>
        // OnPermissionGroupDuplicatedDenied,
        // /// <summary>
        // /// <para>Use <see cref="PermissionManagerAPI.DeletedPermissionGroup"/> to get the permission group
        // /// which has been deleted.</para>
        // /// <para>Runs inside of an input action, making API properties for input actions from lockstep or
        // /// other systems available.</para>
        // /// <para>Game state safe.</para>
        // /// </summary>
        // OnPermissionGroupDeletedDenied,
        /// <summary>
        /// <para>Use <see cref="PermissionsPagesManagerAPI.PermissionGroupAttemptedToBeAffected"/> to get the
        /// permission group which was attempted to be renamed.</para>
        /// <para>Runs inside of an input action, making API properties for input actions from lockstep or
        /// other systems available.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnPermissionGroupRenameDenied,
        /// <summary>
        /// <para>Use <see cref="PermissionsPagesManagerAPI.PersistentIdAttemptedToBeAffected"/> to get the
        /// player who's <see cref="PermissionsPlayerData.permissionGroup"/> was attempted to be
        /// changed.</para>
        /// <para>Use <see cref="PermissionsPagesManagerAPI.WouldLoseEditPermissions"/> check if it was denied
        /// due to the sending player also being the affected player and the target permission group not
        /// having the edit permissions permission.</para>
        /// <para>Runs inside of an input action, making API properties for input actions from lockstep or
        /// other systems available.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnPlayerPermissionGroupChangeDenied,
        /// <summary>
        /// <para>Use <see cref="PermissionsPagesManagerAPI.PermissionGroupAttemptedToBeAffected"/> to get the
        /// permission group for which a <see cref="PermissionGroup.permissionValues"/> was attempted to be
        /// changed.</para>
        /// <para>Use <see cref="PermissionsPagesManagerAPI.PermissionAttemptedToBeAffected"/> to get the
        /// permission which has was attempted to be changed.</para>
        /// <para>Only gets raised if the value actually changed, and since it is either
        /// <see langword="true"/> or <see langword="false"/> the previous value can be determined quite
        /// easily. It is the inverse of the current value.</para>
        /// <para>Runs inside of an input action, making API properties for input actions from lockstep or
        /// other systems available.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnPermissionValueChangeDenied,
    }

    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class PermissionsPagesEventAttribute : CustomRaisedEventBaseAttribute
    {
        /// <summary>
        /// <para>The method this attribute gets applied to must be public.</para>
        /// <para>The name of the function this attribute is applied to must have the exact same name as the
        /// name of the <paramref name="eventType"/>.</para>
        /// <para>Event registration is performed at OnBuild, which is to say that scripts with these kinds of
        /// event handlers must exist in the scene at build time, any runtime instantiated objects with these
        /// scripts on them will not receive these events.</para>
        /// <para>Disabled scripts still receive events.</para>
        /// </summary>
        /// <param name="eventType">The event to register this function as a listener to.</param>
        public PermissionsPagesEventAttribute(PermissionsPagesEventType eventType)
            : base((int)eventType)
        { }
    }

    [SingletonScript("bec13509efe353dd3bc14b2853c828cf")] // Runtime/Prefabs/Managers/PermissionsPagesManager.prefab
    public abstract class PermissionsPagesManagerAPI : UdonSharpBehaviour
    {
        /// <summary>
        /// <para>Effectively the same as
        /// <see cref="PermissionManagerAPI.SendDuplicatePermissionGroupIA(string, PermissionGroup)"/>
        /// except checks if the sending player has permission to do so.</para>
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="toDuplicate"></param>
        public abstract void SendDuplicatePermissionGroupIA(string groupName, PermissionGroup toDuplicate);
        /// <summary>
        /// <para>Effectively the same as
        /// <see cref="PermissionManagerAPI.SendDeletePermissionGroupIA(PermissionGroup, PermissionGroup)"/>
        /// except checks if the sending player has permission to do so.</para>
        /// </summary>
        /// <param name="group"></param>
        /// <param name="groupToMovePlayersTo"></param>
        public abstract void SendDeletePermissionGroupIA(PermissionGroup group, PermissionGroup groupToMovePlayersTo);
        /// <summary>
        /// <para>Effectively the same as
        /// <see cref="PermissionManagerAPI.SendRenamePermissionGroupIA(PermissionGroup, string)"/>
        /// except that it raises <see cref="PermissionsPagesEventType.OnPermissionGroupRenameDenied"/>
        /// instead if the sending player lacks permission to do so.</para>
        /// </summary>
        /// <param name="group"></param>
        /// <param name="newGroupName"></param>
        public abstract void SendRenamePermissionGroupIA(PermissionGroup group, string newGroupName);
        /// <summary>
        /// <para>Effectively the same as
        /// <see cref="PermissionManagerAPI.SendSetPlayerPermissionGroupIA(CorePlayerData, PermissionGroup)"/>
        /// except that it raises <see cref="PermissionsPagesEventType.OnPlayerPermissionGroupChangeDenied"/>
        /// instead if the sending player lacks permission to do so.</para>
        /// </summary>
        /// <param name="corePlayerData"></param>
        /// <param name="group"></param>
        public abstract void SendSetPlayerPermissionGroupIA(CorePlayerData corePlayerData, PermissionGroup group);
        /// <summary>
        /// <para>Effectively the same as
        /// <see cref="PermissionManagerAPI.SendSetPermissionValueIA(PermissionGroup, PermissionDefinition, bool)"/>
        /// except that it raises <see cref="PermissionsPagesEventType.OnPermissionValueChangeDenied"/>
        /// instead if the sending player lacks permission to do so.</para>
        /// </summary>
        /// <param name="group"></param>
        /// <param name="permissionDef"></param>
        /// <param name="value"></param>
        public abstract void SendSetPermissionValueIA(PermissionGroup group, PermissionDefinition permissionDef, bool value);

        /// <summary>
        /// <para>Usable inside of <see cref="PermissionsPagesEventType.OnPermissionGroupRenameDenied"/>,
        /// and <see cref="PermissionsPagesEventType.OnPermissionValueChangeDenied"/>.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        public abstract PermissionGroup PermissionGroupAttemptedToBeAffected { get; }
        /// <summary>
        /// <para>Usable inside of
        /// <see cref="PermissionsPagesEventType.OnPlayerPermissionGroupChangeDenied"/>.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        public abstract uint PersistentIdAttemptedToBeAffected { get; }
        /// <summary>
        /// <para>Usable inside of
        /// <see cref="PermissionsPagesEventType.OnPlayerPermissionGroupChangeDenied"/>.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        public abstract bool WouldLoseEditPermissions { get; }
        /// <summary>
        /// <para>Usable inside of
        /// <see cref="PermissionsPagesEventType.OnPermissionValueChangeDenied"/>.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        public abstract PermissionDefinition PermissionAttemptedToBeAffected { get; }
    }
}
