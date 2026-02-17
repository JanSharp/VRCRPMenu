using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class KeyboardKey : UdonSharpBehaviour
    {
        public Keyboard keyboard;
        public char character;
        public bool isBackspace;

        public void OnClick()
        {
            keyboard.OnClick(this);
        }
    }
}
