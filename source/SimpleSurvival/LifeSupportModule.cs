using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSurvival
{
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
                Util.StartupRequest(this, C.NAME_LS);
            base.OnStart(state);
        }

        public void FixedUpdate()
        {
            int crew_count = part.protoModuleCrew.Count;

            // Request resource based on rates defined by constants
            double ret_rs = part.RequestResource(C.NAME_LS, crew_count * C.LS_DRAIN_PER_SEC * TimeWarp.fixedDeltaTime);

            if (crew_count > 0 && ret_rs == 0.0)
            {
                Util.KillKerbals(this);

                // Credit part that lost Kerbal passed in
                part.RequestResource(C.NAME_LS, C.LS_DEATH_CREDIT);
            }
        }
    }
}
