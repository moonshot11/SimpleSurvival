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

            if (valid_states.Contains(state))
                Util.StartupRequest(this, C.NAME_LIFESUPPORT, C.LS_DRAIN_PER_SEC);
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
                return;

            // If vessel is below this altitude in an atmosphere with oxygen,
            // LifeSupport is irrelevant
            if (vessel.mainBody.atmosphereContainsOxygen && vessel.altitude < C.OXYGEN_CUTOFF_ALTITUDE)
                return;

            int crew_count = part.protoModuleCrew.Count;

            // How much lifesupport to request
            double ls_request = crew_count * C.LS_DRAIN_PER_SEC * TimeWarp.fixedDeltaTime;

            // Request resource based on rates defined by constants
            double ret_rs = part.RequestResource(C.NAME_LIFESUPPORT, ls_request);

            if (crew_count > 0 && ret_rs == 0.0)
            {
                Util.KillKerbals(this);

                // Credit part that lost Kerbal passed in
                part.RequestResource(C.NAME_LIFESUPPORT, C.LS_DEATH_CREDIT);
            }
        }
    }
}
