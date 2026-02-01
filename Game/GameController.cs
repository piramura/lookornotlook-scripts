using Piramura.LookOrNotLook.Item;
using Piramura.LookOrNotLook.Logic;
using UnityEngine;

namespace Piramura.LookOrNotLook.Game
{
    /// <summary>
    /// まずはスコア加算だけ。後で称号や図鑑はここに集約
    /// </summary>
    public sealed class GameController : IGameActions
    {
        private readonly IScoreService score;

        public GameController(IScoreService score)
        {
            this.score = score;
        }

        public void Collect(ItemDefinition def)
        {
            if (def == null) return;
            score.Add(def.Value);
            Debug.Log($"[Game] Collect: {def.ItemId} +{def.Value} => score={score.Score}");
        }

        public void Penalty(ItemDefinition def)
        {
            if (def == null) return;

            // ひとまず「Valueを減点」にしておく（後でpenaltyValueをItemDefinitionに足すのが綺麗）
            score.Add(-def.Value);
            Debug.Log($"[Game] Penalty: {def.ItemId} -{def.Value} => score={score.Score}");
        }
    }
}
