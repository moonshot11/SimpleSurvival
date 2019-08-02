using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SimpleSurvival
{
    /// <summary>
    /// Loads the EVALifeSupportModule into the EVA parts on startup.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class EVALifeSupportLoader : MonoBehaviour
    {
        public void Awake()
        {
            Util.Log("Beginning setup of EVA LifeSupport");

            // -- Attach PartModule to EVA part(s) --

            Util.Log("Scanning loaded parts for EVA");

            List<AvailablePart> part_list = PartLoader.LoadedPartsList;
            part_list.Sort(
                delegate (AvailablePart p1, AvailablePart p2)
                {
                    return p1.name.CompareTo(p2.name);
                }
            );

            ConfigNode mod_node = new ConfigNode("MODULE");
            mod_node.AddValue("name", "EVALifeSupportModule");

            // Search each loaded part. If EVA part is found,
            // add EVALifeSupportModule.
            foreach (AvailablePart part in part_list)
            {
                string name = part.name;

                if (name.StartsWith("kerbalEVA"))
                {
                    Util.Log("Adding EVALifeSupportModule to " + name);

                    // Necessary to guarantee that this code block operates
                    // on kerbalEVA and kerbalEVAfemale
                    try
                    {
                        part.partPrefab.AddModule(mod_node);
                    }
                    catch (NullReferenceException e)
                    {
                        Util.Log("Catching exception, doesn't seem to affect game:");
                        Util.Log("  " + e.ToString());
                    }
                }
                else if (name == "fuelTank")
                {
                    Util.Log("Adding texture to, NOT " + name);
                }
            }

            TextureUtil.ApplyDecals();
        }

        
    }
}
