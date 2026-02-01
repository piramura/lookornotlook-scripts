using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Piramura.Common
{
    public abstract class AsyncMB : MonoBehaviour
    {
        protected CancellationToken destroyToken;

        protected virtual void Awake()
        {
            destroyToken = this.GetCancellationTokenOnDestroy();
        }
    }
}
