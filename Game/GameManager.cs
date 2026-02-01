using UnityEngine;
using Piramura.LookOrNotLook.Logic;
using Piramura.LookOrNotLook.Item;
using System.Collections.Generic;
using Piramura.LookOrNotLook.Reaction;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using VContainer;

namespace Piramura.LookOrNotLook.Game
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private SeeingLogic seeingLogic;

        [Header("Spawn")]
        [SerializeField] private ItemSpawner itemSpawner;
        [SerializeField] private ItemLayout layout;
        
        [SerializeField] private ItemDefinition[] itemPool;
        
        private readonly List<int> freeIndices = new();
        
        private ItemProgress currentProgress;
        private CollectableItem currentCollectable;
        private ItemReaction currentReaction;
        [Header("Start Spawn")]
        [SerializeField] private bool spawnAllOnStart = true;

        [Header("Refresh On Collect")]
        [SerializeField] private int refreshRadius = 1;       // 1 or 2
        private readonly List<int> aroundBuffer = new();
        private GameObject[] slotObjects; // index -> spawned item
        public ItemDefinition[] ItemPool => itemPool;
        public bool SpawnAllOnStart => spawnAllOnStart;
        public int RefreshRadius => refreshRadius;

        [Inject]
        public void Construct(SeeingLogic seeing, ItemSpawner spawner, ItemLayout layout)
        {
            if (seeingLogic == null) seeingLogic = seeing;
            if (itemSpawner == null) itemSpawner = spawner;
            if (this.layout == null) this.layout = layout;
        }
    }

    public enum GameFlowState
    {
        Idle,
        Progress,
        Stopped
    }
}
