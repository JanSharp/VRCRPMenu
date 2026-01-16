using UdonSharp;
using VRC.SDK3.Data;

namespace JanSharp
{
    public enum GMRequestType : byte
    {
        Regular,
        Urgent,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GMRequest : WannaBeClass
    {
        [System.NonSerialized] public DataDictionary latencyHiddenUniqueIds = new DataDictionary();

        #region LatencyState
        [System.NonSerialized] public bool isLatency;
        [System.NonSerialized] public GMRequestType latencyRequestType;
        [System.NonSerialized] public bool latencyIsRead;
        [System.NonSerialized] public RPPlayerData latencyRespondingPlayer;
        [System.NonSerialized] public bool latencyIsDeleted;
        #endregion

        // The game state is unusable while isLatency is true, with some exceptions as noted below.
        #region GameState
        [System.NonSerialized] public int index;
        /// <summary>
        /// <para>Can be read while <see cref="isLatency"/> is <see langword="true"/>.</para>
        /// <para>Readonly.</para>
        /// </summary>
        [System.NonSerialized] public ulong uniqueId;
        /// <summary>
        /// <para>Readonly.</para>
        /// </summary>
        [System.NonSerialized] public uint id;
        [System.NonSerialized] public GMRequestType requestType;
        [System.NonSerialized] public bool isRead;
        /// <summary>
        /// <para>Readonly.</para>
        /// </summary>
        [System.NonSerialized] public uint requestedAtTick;
        /// <summary>
        /// <para><c>0u</c> and meaningless while <see cref="isRead"/> is <see langword="false"/>.</para>
        /// </summary>
        [System.NonSerialized] public uint autoDeleteAtTick;
        /// <summary>
        /// <para>Can be read while <see cref="isLatency"/> is <see langword="true"/>.</para>
        /// <para>Can be <see langword="null"/>.</para>
        /// <para>Readonly.</para>
        /// </summary>
        [System.NonSerialized] public CorePlayerData requestingCorePlayer;
        /// <summary>
        /// <para>Can be read while <see cref="isLatency"/> is <see langword="true"/>.</para>
        /// <para>Can be <see langword="null"/>.</para>
        /// <para>Readonly.</para>
        /// </summary>
        [System.NonSerialized] public RPPlayerData requestingPlayer;
        /// <summary>
        /// <para>Is <see langword="null"/> while <see cref="isRead"/> is <see langword="false"/>, but can be
        /// <see langword="null"/> even when <see langword="true"/>.</para>
        /// </summary>
        [System.NonSerialized] public RPPlayerData respondingPlayer;
        [System.NonSerialized] public bool isDeleted;
        #endregion
    }
}
