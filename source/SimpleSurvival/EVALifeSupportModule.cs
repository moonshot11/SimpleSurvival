using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleSurvival
{
    /// <summary>
    /// EVA LifeSupport PartModule, lives in EVA Kerbals.
    /// </summary>
    public class EVALifeSupportModule : LifeSupportReportable
    {
        public override void OnStart(StartState state)
        {
            Util.Log("EVALifeSupportModule OnStart()");

            // Check if EVA Kerbal has exactly one ProtoCrewMember
            if (part.protoModuleCrew.Count == 0)
            {
                string msg = "0 PMCs found in EVA Kerbal: " + part.name;
                Util.Log(msg);
                throw new IndexOutOfRangeException(msg);
            }
            else if (part.protoModuleCrew.Count > 1)
            {
                Util.Log("Weird...multiple PMCs found in EVA Kerbal: " + part.name);
            }

            // To avoid conflicts with Unity variable "name"
            string kerbal_name = part.protoModuleCrew[0].name;

            PartResource ls_resource = null;

            foreach (PartResource pr in part.Resources)
            {
                if (pr.resourceName == C.NAME_EVA_LIFESUPPORT)
                {
                    ls_resource = pr;
                    break;
                }
            }

            // Kerbals assigned after this mod's installation should already be tracked,
            // but for Kerbals already in flight, add EVA LS according to current state
            // of astronaut complex
            EVALifeSupportTracker.AddKerbalToTracking(kerbal_name);

            if (ls_resource == null)
            {
                // If not found, add EVA LS resource to this PartModule.
                Util.Log("Adding " + C.NAME_EVA_LIFESUPPORT + " resource to " + part.name);

                var info = EVALifeSupportTracker.GetEVALSInfo(kerbal_name);

                ConfigNode resource_node = new ConfigNode("RESOURCE");
                resource_node.AddValue("name", C.NAME_EVA_LIFESUPPORT);
                resource_node.AddValue("amount", info.ls_current.ToString());
                resource_node.AddValue("maxAmount", info.ls_max.ToString());

                ls_resource = part.AddResource(resource_node);

                Util.Log("Added EVA LS resource to " + part.name);
            }
            else
            {
                // If found, this EVA is already active - deduct LS.
                Util.StartupRequest(this, C.NAME_EVA_LIFESUPPORT, C.EVA_LS_DRAIN_PER_SEC);

                if (ls_resource.amount < C.KILL_BUFFER)
                    ls_resource.amount = C.KILL_BUFFER;
                else if (ls_resource.amount < C.EVA_LS_30_SECONDS)
                    Util.PostUpperMessage(kerbal_name + " has " + (int)(ls_resource.amount / C.EVA_LS_DRAIN_PER_SEC) + " seconds to live!", 1);
            }

            // Necessary to override game's default behavior, which refills
            // EVA Propellant automatically every time EVA Kerbal is reset
            var eva_info = EVALifeSupportTracker.GetEVALSInfo(kerbal_name);

            // If difficulty option "Immediate Level Up" is selected,
            // immediately set this Kerbal's EVA to new max
            if (this.vessel.CanUpdateEVAStat(Config.EVA_MAX_UPDATE))
                ls_resource.maxAmount = Util.MaxAllowedEvaLS();

            base.OnStart(state);
        }

        public void FixedUpdate()
        {
            PartResource resource = part.Resources[C.NAME_EVA_LIFESUPPORT];
            double initial_value = resource.amount;
            string kerbal_name = part.protoModuleCrew[0].name;

            // Update tracking info.
            // While Kerbal is in EVA, PartResource contains the "primary" value,
            // and tracking is only updated as a consequence.
            // It will be a frame behind, but that should be okay.
            EVALifeSupportTracker.SetCurrentAmount(kerbal_name, initial_value);

            // If Kerbal is below this altitude in an atmosphere with oxygen,
            // LifeSupport is irrelevant
            if (Util.BreathableAir(vessel))
                return;

            // -- Reduce resource --
            double retd = part.RequestResource(C.NAME_EVA_LIFESUPPORT, C.EVA_LS_DRAIN_PER_SEC * TimeWarp.fixedDeltaTime);

            if (initial_value > C.EVA_LS_30_SECONDS &&
                resource.amount <= C.EVA_LS_30_SECONDS)
            {
                TimeWarp.SetRate(0, true);
                Util.PostUpperMessage(kerbal_name + " has 30 seconds to live!", 1);
                resource.amount = C.EVA_LS_30_SECONDS;
            }

            // Necessary to check if crew count > 0?
            if (retd == 0.0)
            {
                Util.KillKerbals(this);
                part.explode();
            }
        }

        public override string ReportLifeSupport(out bool isEmpty)
        {
            isEmpty = false;
            return "--";
        }
    }
}
