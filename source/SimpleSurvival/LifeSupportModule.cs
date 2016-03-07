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
            // 10 unit(s) = 1 Kerbal for 1 day
            double RATE = 10.0 / 3600.0 / 6.0;
            double DEATH_CREDIT = -1.0;
            double ret_rs = part.RequestResource("LifeSupport", part.protoModuleCrew.Count * RATE * TimeWarp.fixedDeltaTime);

            List<ProtoCrewMember> part_crew = part.protoModuleCrew;

            if (ret_rs == 0f)
            {
                List<ProtoCrewMember> remove_kerbs = new List<ProtoCrewMember>();

                while (part_crew.Count > 0)
                {
                    ProtoCrewMember kerbal = part_crew[0];
                    bool respawn_flag = HighLogic.CurrentGame.Parameters.Difficulty.MissingCrewsRespawn;

                    // Kerbal must be removed from part BEFORE calling Die()
                    part.RemoveCrewmember(kerbal);

                    // ...for some reason
                    kerbal.Die();

                    // Put Kerbal in Missing queue
                    if (respawn_flag)
                        kerbal.StartRespawnPeriod();

                    // Credit part that lost Kerbal passed in
                    part.RequestResource("LifeSupport", DEATH_CREDIT);
                }
            }
        }
    }
}
