using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class InputManager : InputManagerAPI
    {
        private float timeAtLastUseLeft;
        private float timeAtLastUseRight;

        private const float MaxInputUseAndClickTimeDifference = 0.05f;

        public override void InputUse(bool value, UdonInputEventArgs args)
        {
            if (!value)
                return;
            if (args.handType == HandType.LEFT)
                timeAtLastUseLeft = Time.time;
            else
                timeAtLastUseRight = Time.time;
            if (iuqCount == 0)
                return;
            DequeueAllFromInputUseQueue(args.handType == HandType.LEFT
                ? VRCPlayerApi.TrackingDataType.LeftHand
                : VRCPlayerApi.TrackingDataType.RightHand);
        }

        private object[][] inputUseQueue = new object[ArrQueue.MinCapacity][];
        private int iuqStartIndex = 0;
        private int iuqCount = 0;
        private int ignoreNextInputUseTimeOuts = 0;

        public override void DetermineHandUsedForClick(UdonSharpBehaviour callbackInst, string callbackEventName, object callbackCustomData)
        {
            float timeAtLastUse;
            if (timeAtLastUseLeft >= timeAtLastUseRight)
            {
                determinedHand = VRCPlayerApi.TrackingDataType.LeftHand;
                timeAtLastUse = timeAtLastUseLeft;
            }
            else
            {
                determinedHand = VRCPlayerApi.TrackingDataType.RightHand;
                timeAtLastUse = timeAtLastUseRight;
            }

            if (timeAtLastUse + MaxInputUseAndClickTimeDifference >= Time.time)
            {
                this.callbackCustomData = callbackCustomData;
                callbackInst.SendCustomEvent(callbackEventName);
                return;
            }

            ArrQueue.Enqueue(ref inputUseQueue, ref iuqStartIndex, ref iuqCount, new object[]
            {
                callbackInst,
                callbackEventName,
                callbackCustomData,
            });
            SendCustomEventDelayedSeconds(nameof(InputUseQueueTimedOut), MaxInputUseAndClickTimeDifference);
        }

        public void InputUseQueueTimedOut()
        {
            if (ignoreNextInputUseTimeOuts != 0 && (--ignoreNextInputUseTimeOuts) != 0)
                return;
            // Just use the last used hand as a fallback.
            determinedHand = timeAtLastUseLeft >= timeAtLastUseRight
                ? VRCPlayerApi.TrackingDataType.LeftHand
                : VRCPlayerApi.TrackingDataType.RightHand;
            object[] data = ArrQueue.Dequeue(ref inputUseQueue, ref iuqStartIndex, ref iuqCount);
            callbackCustomData = data[2];
            ((UdonSharpBehaviour)data[0]).SendCustomEvent((string)data[1]);
        }

        private void DequeueAllFromInputUseQueue(VRCPlayerApi.TrackingDataType determinedHand)
        {
            this.determinedHand = determinedHand;
            ignoreNextInputUseTimeOuts += iuqCount;
            int length = inputUseQueue.Length;
            for (int i = 0; i < iuqCount; i++)
            {
                object[] data = inputUseQueue[(iuqStartIndex + i) % length];
                callbackCustomData = data[2];
                ((UdonSharpBehaviour)data[0]).SendCustomEvent((string)data[1]);
            }
            ArrQueue.Clear(ref inputUseQueue, ref iuqStartIndex, ref iuqCount);
        }

        private VRCPlayerApi.TrackingDataType determinedHand;
        private object callbackCustomData;
        public override VRCPlayerApi.TrackingDataType DeterminedHand => determinedHand;
        public override object CallbackCustomData => callbackCustomData;
    }
}
