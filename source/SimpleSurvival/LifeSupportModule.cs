using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSurvival
{
    [KSPModule("Life Support")]
    public class LifeSupportModule : PartModule
    {
        private float grace_timer = C.GRACE_PERIOD;

        public override void OnStart(StartState state)
        {
            StartState[] valid_states = new StartState[]
            {
                StartState.Flying,
                StartState.Landed,
                StartState.Orbital,
                StartState.Splashed,
                StartState.SubOrbital,
                StartState.Docked
            };

            grace_timer = C.GRACE_PERIOD;

            if (valid_states.Contains(state))
            {
                bool enough = Util.StartupRequest(this, C.NAME_LIFESUPPORT, C.LS_DRAIN_PER_SEC);

                if (!enough)
                    grace_timer = 0f;
            }
            else
                Util.Log("State = " + state.ToString() + ", ignoring startup LifeSupport request");

            base.OnStart(state);
        }

        public override string GetInfo()
        {
            // NOTE: This method is called ONCE during initial part loading,
            // so performance is not a concern

            string per_kerb = part.CrewCapacity > 1 ? "/Kerbal" : "";

            string info = "Active only when manned.\n\n" +
            "<b><color=#99ff00>Requires:</color></b>\n" +
            "- " + C.NAME_LIFESUPPORT + ": " + Util.FormatForGetInfo(C.LS_PER_DAY_PER_KERBAL) + per_kerb + "/day.\n";
            
            return info;
        }

        public void FixedUpdate()
        {
            // If part is unmanned, nothing to do
            if (part.protoModuleCrew.Count == 0)
            {
                grace_timer = C.GRACE_PERIOD;
                return;
            }

            // If vessel is below this altitude in an atmosphere with oxygen,
            // LifeSupport is irrelevant
            if (vessel.mainBody.atmosphereContainsOxygen && vessel.altitude < C.OXYGEN_CUTOFF_ALTITUDE)
                return;

            int crew_count = part.protoModuleCrew.Count;

            // How much lifesupport to request
            double ls_request = crew_count * C.LS_DRAIN_PER_SEC * TimeWarp.fixedDeltaTime;

            // Request resource based on rates defined by constants
            double ret_rs = part.RequestResource(C.NAME_LIFESUPPORT, ls_request);

            if (ret_rs > 0.0)
            {
                // If LifeSupport exists or is restored, reset grace period
                grace_timer = C.GRACE_PERIOD;
            }
            else
            {
                // If timer hasn't run out, tick then return
                if (grace_timer > 0f)
                {
                    // If this is the first tick, print warning
                    if (grace_timer == C.GRACE_PERIOD)
                    {
                        TimeWarp.SetRate(0, true);
                        string name = vessel.isActiveVessel ? part.partInfo.title : vessel.vesselName;

                        ScreenMessages.PostScreenMessage("<color=#ff8800>Crew in " + name + "\nhas " + C.GRACE_PERIOD + " seconds to live!</color>", 8f, ScreenMessageStyle.UPPER_CENTER);
                    }

                    grace_timer -= 1.0f * TimeWarp.fixedDeltaTime;
                    return;
                }

                Util.KillKerbals(this);
            }
        }
    }
}
