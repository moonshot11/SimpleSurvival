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
            // Universal Time in seconds
            int lastUT = (int)vessel.lastUT;
            int currUT = (int)HighLogic.CurrentGame.UniversalTime;

            // Integer logic could overflow after 233 Kerbin years
            int delta = lastUT - currUT;

            Util.Log("LastUT = " + lastUT + " (" + KSPUtil.PrintDate(lastUT, true, true) + ")");
            Util.Log("CurrUT = " + currUT + " (" + KSPUtil.PrintDate(currUT, true, true) + ")");
            Util.Log("Time elapsed: " + delta + " (" + KSPUtil.PrintDateDelta(delta, true, true) + ")");

            base.OnStart(state);
        }
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
