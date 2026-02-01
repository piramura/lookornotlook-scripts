using UnityEngine;

namespace Piramura.LookOrNotLook.Audio
{
    public sealed class SfxSource : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private SfxConfig config;

        public AudioSource AudioSource => audioSource;
        public SfxConfig Config => config;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }
    }
}
