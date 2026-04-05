using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Piramura.LookOrNotLook.Game
{
    public interface IItemCollectFlow
    {
        UniTask ExecuteAsync(GameObject item, Func<bool> isFinished);
    }
}
