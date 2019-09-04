using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SimpleSurvival
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER)]
    public class VariantSetter : ScenarioModule
    {
        public static Dictionary<AvailablePart, PartVariant> NewVariants =
            new Dictionary<AvailablePart, PartVariant>();

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            Util.Log("VariantSetter OnLoad");
            foreach (var pair in NewVariants)
            {
                Util.Log($"  Setting {pair.Key.name} -> {pair.Value.Name}");
                var loadedPart = PartLoader.LoadedPartsList.Find(a => a.name == pair.Key.name);
                Util.Log($"  Equal? {pair.Key == loadedPart}");
                loadedPart.variant = pair.Value;

            }
            ResearchAndDevelopment.RefreshTechTreeUI();
        }
    }
}
