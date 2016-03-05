using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSurvival
{
    public class SimpleSurvivalModule : PartModule
    {
        public void FixedUpdate()
        {
            float RATE = 10f / 3600f / 6f;
            float ret_rs = part.RequestResource("LifeSupport", part.protoModuleCrew.Count * RATE * TimeWarp.fixedDeltaTime);

            if (ret_rs == 0f)
            {
                List<ProtoCrewMember> remove_kerbs = new List<ProtoCrewMember>();

                foreach (ProtoCrewMember kerb in part.protoModuleCrew)
                {
                    // Move so list isn't being modified in place
                    // Necessary - otherwise Kerbals are "K.I.A."
                    // for future assignment
                    remove_kerbs.Add(kerb);
                    kerb.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                    kerb.Die();
                }

                // Remove Kerbals from part
                foreach (ProtoCrewMember kerb in remove_kerbs)
                    part.RemoveCrewmember(kerb);
            }
        }
    }
}
