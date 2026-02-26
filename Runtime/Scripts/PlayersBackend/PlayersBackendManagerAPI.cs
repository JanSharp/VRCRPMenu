using UdonSharp;

namespace JanSharp
{
    public enum PlayersBackendEventType
    {
        /// <summary>
        /// <para>Use <see cref="PlayersBackendManagerAPI.RPPlayerDataForEvent"/> to get the player data for
        /// which the <see cref="RPPlayerData.overriddenDisplayName"/> has been changed.</para>
        /// <para>Use <see cref="PlayersBackendManagerAPI.PreviousOverriddenDisplayName"/> to get the previous
        /// value of <see cref="RPPlayerData.overriddenDisplayName"/> before the change.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnRPPlayerDataOverriddenDisplayNameChanged,
        /// <summary>
        /// <para>Use <see cref="PlayersBackendManagerAPI.RPPlayerDataForEvent"/> to get the player data for
        /// which the <see cref="RPPlayerData.characterName"/> has been changed.</para>
        /// <para>Use <see cref="PlayersBackendManagerAPI.PreviousCharacterName"/> to get the previous value
        /// of <see cref="RPPlayerData.characterName"/> before the change.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnRPPlayerDataCharacterNameChanged,
        /// <summary>
        /// <para>Use <see cref="PlayersBackendManagerAPI.RPPlayerDataForEvent"/> to get the player data for
        /// which the <see cref="RPPlayerData.overriddenDisplayName"/> was attempted to be changed.</para>
        /// <para>Runs inside of an input action, making API properties for input actions from lockstep or
        /// other systems available.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnRPPlayerDataOverriddenDisplayNameChangeDenied,
        /// <summary>
        /// <para>Use <see cref="PlayersBackendManagerAPI.RPPlayerDataForEvent"/> to get the player data for
        /// which the <see cref="RPPlayerData.characterName"/> was attempted to be changed.</para>
        /// <para>Runs inside of an input action, making API properties for input actions from lockstep or
        /// other systems available.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnRPPlayerDataCharacterNameChangeDenied,
        /// <summary>
        /// <para>Use <see cref="PlayersBackendManagerAPI.PersistentIdAttemptedToBeAffected"/> to get the
        /// player which was attempted to be deleted.</para>
        /// <para>Use <see cref="PlayersBackendManagerAPI.IsLastPlayerWhoCanEditPermissions"/> to check if it
        /// was denied due to that being the last player who has have the permission to edit
        /// permissions.</para>
        /// <para>Runs inside of an input action, making API properties for input actions from lockstep or
        /// other systems available.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnDeleteOfflinePlayerDataDenied,
    }

    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class PlayersBackendEventAttribute : CustomRaisedEventBaseAttribute
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
        public PlayersBackendEventAttribute(PlayersBackendEventType eventType)
            : base((int)eventType)
        { }
    }

    [SingletonScript("000377df80d8968ff9358be09d3fa9e3")] // Runtime/Prefabs/PlayersBackendManager.prefab
    public abstract class PlayersBackendManagerAPI : LockstepGameState
    {
        public abstract void SendSetOverriddenDisplayNameIA(RPPlayerData rpPlayerData, string overriddenDisplayName);
        public abstract void SetOverriddenDisplayNameInGS(RPPlayerData rpPlayerData, string overriddenDisplayName);
        public abstract void SendSetCharacterNameIA(RPPlayerData rpPlayerData, string characterName);
        public abstract void SetCharacterNameInGS(RPPlayerData rpPlayerData, string characterName);

        /// <summary>
        /// <para>Effectively the same as
        /// <see cref="PlayerDataManagerAPI.SendDeleteOfflinePlayerDataIA(CorePlayerData)"/> except that it
        /// raises <see cref="PlayersBackendEventType.OnDeleteOfflinePlayerDataDenied"/> instead if the
        /// sending player lacks permission to do so.</para>
        /// </summary>
        /// <param name="corePlayerData"></param>
        public abstract void SendDeleteOfflinePlayerDataIA(CorePlayerData corePlayerData);

        public abstract RPPlayerData SendingRPPlayerData { get; }
        public abstract RPPlayerData GetRPPlayerData(CorePlayerData core);

        public abstract void WriteRPPlayerDataRef(RPPlayerData rpPlayerData);
        public abstract RPPlayerData ReadRPPlayerDataRef();
        public abstract RPPlayerData ReadRPPlayerDataRef(bool isImport);

        /// <summary>
        /// <para>Used in exports.</para>
        /// </summary>
        public abstract PlayersBackendImportExportOptions ExportOptions { get; }
        /// <summary>
        /// <para>Used in imports.</para>
        /// </summary>
        public abstract PlayersBackendImportExportOptions ImportOptions { get; }
        /// <summary>
        /// <para>Used in imports.</para>
        /// </summary>
        public abstract PlayersBackendImportExportOptions OptionsFromExport { get; }

        /// <summary>
        /// <para>Usable inside of
        /// <see cref="PlayersBackendEventType.OnRPPlayerDataOverriddenDisplayNameChanged"/> and
        /// <see cref="PlayersBackendEventType.OnRPPlayerDataCharacterNameChanged"/> and their denied
        /// variants.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        public abstract RPPlayerData RPPlayerDataForEvent { get; }
        /// <summary>
        /// <para>Usable inside of
        /// <see cref="PlayersBackendEventType.OnRPPlayerDataOverriddenDisplayNameChanged"/>.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        public abstract string PreviousOverriddenDisplayName { get; }
        /// <summary>
        /// <para>Usable inside of
        /// <see cref="PlayersBackendEventType.OnRPPlayerDataCharacterNameChanged"/>.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        public abstract string PreviousCharacterName { get; }

        /// <summary>
        /// <para>Usable inside of
        /// <see cref="PlayersBackendEventType.OnDeleteOfflinePlayerDataDenied"/>.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        public abstract uint PersistentIdAttemptedToBeAffected { get; }
        /// <summary>
        /// <para>Usable inside of
        /// <see cref="PlayersBackendEventType.OnDeleteOfflinePlayerDataDenied"/>.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        public abstract bool IsLastPlayerWhoCanEditPermissions { get; }
    }
}
