using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSurvival
{
    public class EVALifeSupportModule : PartModule
    {
        public void FixedUpdate()
        {
            // -- First, check if resource is already added to part --
            // This check should be done once on part load, not each frame
            bool found_resource = false;

            foreach (PartResource pr in part.Resources)
            {
                if (pr.resourceName == "EVA LifeSupport")
                {
                    found_resource = true;
                    break;
                }
            }

            if (!found_resource)
            {
                ConfigNode resource_node = new ConfigNode("RESOURCE");
                resource_node.AddValue("name", "EVA LifeSupport");
                resource_node.AddValue("amount", "5");
                resource_node.AddValue("maxAmount", "5");

                part.AddResource(resource_node);

                Util.Log("Adding resource to " + part.name);
            }

            // -- Reduce resource, game logic --
            double retd = part.RequestResource("EVA LifeSupport", 0.5 * TimeWarp.fixedDeltaTime);

            // Necessary to check if crew count > 0?
            if (retd == 0.0)
            {
                Util.KillKerbals(this);
                vessel.Die();
            }
        }
    }
}
