using UnityEngine;

namespace Piramura.LookOrNotLook.Audio
{
    [CreateAssetMenu(menuName = "LookOrNotLook/Audio/SfxConfig", fileName = "SfxConfig")]
    public sealed class SfxConfig : ScriptableObject
    {
        public AudioClip collect;
        public AudioClip penalty;
        public AudioClip reset;
        public AudioClip timeUp;
        public AudioClip result;
    }
}
