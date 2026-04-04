using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Piramura.LookOrNotLook.UI
{
    public sealed class ComboPopupSpawner : MonoBehaviour, IComboPopupSpawner
    {
        [SerializeField] private ComboPopup popupPrefab;
        [SerializeField] private Vector3 offset = new(0f, 0.15f, 0f);

        public void Show(Vector3 worldPos, int combo, CancellationToken token)
        {
            if (popupPrefab == null) return;

            var pop = Instantiate(popupPrefab, worldPos + offset, Quaternion.identity);
            pop.Play(combo, token).Forget();
        }
    }
}
