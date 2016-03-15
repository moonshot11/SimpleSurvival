using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSurvival
{
    public class EVALifeSupportModule : PartModule
    {
        public override void OnStart(StartState state)
        {
            // -- Check if resource is already added to part --
            bool found_resource = false;

            foreach (PartResource pr in part.Resources)
            {
                if (pr.resourceName == C.NAME_EVA_LIFESUPPORT)
                {
                    found_resource = true;
                    break;
                }
            }

            if (found_resource)
            {
                // If found, this EVA is already active - deduct LS.
                Util.StartupRequest(this, C.NAME_EVA_LIFESUPPORT, C.EVA_LS_DRAIN_PER_SEC);
            }
            else
            {
                // If not found, EVA has just been initialized.  Add LS to PartModule.
                float astro_lvl = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex);
                string eva_ls_max = "";

                // If Astronaut Complex is fully upgraded, EVA LS gets higher value
                if (astro_lvl == 1.0f)
                    eva_ls_max = C.EVA_LS_LVL_3;
                else
                    eva_ls_max = C.EVA_LS_LVL_2;

                Util.Log("Astronaut Complex Level " + astro_lvl + ", max EVA LS: " + eva_ls_max);

                ConfigNode resource_node = new ConfigNode("RESOURCE");
                resource_node.AddValue("name", C.NAME_EVA_LIFESUPPORT);
                resource_node.AddValue("amount", eva_ls_max);
                resource_node.AddValue("maxAmount", eva_ls_max);

                part.AddResource(resource_node);

                Util.Log("Adding " + C.NAME_EVA_LIFESUPPORT + " resource to " + part.name);
            }

            base.OnStart(state);
        }
        public void FixedUpdate()
        {
            // If Kerbal is below this altitude in an atmosphere with oxygen,
            // LifeSupport is irrelevant
            if (vessel.mainBody.atmosphereContainsOxygen && vessel.altitude < C.OXYGEN_CUTOFF_ALTITUDE)
                return;

            // -- Reduce resource --
            double retd = part.RequestResource(C.NAME_EVA_LIFESUPPORT, C.EVA_LS_DRAIN_PER_SEC * TimeWarp.fixedDeltaTime);

            // Necessary to check if crew count > 0?
            if (retd == 0.0)
            {
                Util.KillKerbals(this);
                part.explode();
                vessel.Die(); // Is this necessary?

                #region FlightResultsDialog
                // string kerbal_name = part.protoModuleCrew[0].name; // Move to top of code block

                // These values persist if user switches to another craft
                //
                // FlightResultsDialog.showExitControls = true;
                // FlightResultsDialog.allowClosingDialog = true;

                // Prints log with this message at the top,
                // where flight status is typically displayed
                //
                // If user leaves focus immediately, Kerbal is not registered as dead
                //
                // FlightResultsDialog.Display(kerbal_name + " ran out of EVA LifeSupport!");
                #endregion
            }
        }
    }
}
