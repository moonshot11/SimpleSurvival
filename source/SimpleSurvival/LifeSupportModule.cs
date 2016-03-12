using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSurvival
{
    public class LifeSupportModule : PartModule
    {
        public void FixedUpdate()
        {
            int crew_count = part.protoModuleCrew.Count;

            // 10 unit(s) = 1 Kerbal for 1 day
            double RATE = 10.0 / 3600.0 / 6.0;
            double DEATH_CREDIT = -1.0;
            double ret_rs = part.RequestResource("LifeSupport", crew_count * RATE * TimeWarp.fixedDeltaTime);

            if (crew_count > 0 && ret_rs == 0.0)
            {
                Util.KillKerbals(this);

                // Credit part that lost Kerbal passed in
                part.RequestResource("LifeSupport", DEATH_CREDIT);
            }
        }
    }
}
