using System.Threading;
using UnityEngine;

namespace Piramura.LookOrNotLook.UI
{
    public interface IComboPopupSpawner
    {
        void Show(Vector3 worldPos, int combo, CancellationToken token);
    }
}
