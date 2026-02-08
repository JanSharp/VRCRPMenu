using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace JanSharp
{
    public abstract class RPMenuUndoRedoTPButton : UdonSharpBehaviour
    {
        public TextMeshProUGUI label;
        public bool singleLine;
        protected string baseLabelText;

        protected VRCPlayerApi localPlayer;

        protected Vector3 positionForTooltip;
        protected float undoAbleActionTakenAtTime;
        protected uint elapsedSeconds;
        protected string formattedElapsedSeconds;

        protected void UpdateTimeAndDistanceTooltip()
        {
            uint seconds = (uint)(Time.time - undoAbleActionTakenAtTime);
            if (seconds != elapsedSeconds)
            {
                elapsedSeconds = seconds;
                formattedElapsedSeconds = FormatTime(seconds);
            }
            float distance = Vector3.Distance(localPlayer.GetPosition(), positionForTooltip);
            label.text = $"{baseLabelText}{(singleLine ? " - " : "\n")}{formattedElapsedSeconds} ago, {distance:0.0} m";
        }

        protected string FormatTime(uint seconds)
        {
            uint minutes = seconds / 60u;
            seconds -= minutes * 60u;
            if (minutes < 60u)
                return $"{minutes}:{seconds:d2}";
            uint hours = minutes / 60u;
            minutes -= hours * 60u;
            return $"{hours}:{minutes:d2}:{seconds:d2}";
        }
    }
}
