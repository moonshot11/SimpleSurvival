using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleSurvival
{
    /// <summary>
    /// Abstract class for reporting life support remaining as text.
    /// </summary>
    public abstract class LifeSupportReportable : PartModule
    {
        public abstract string ReportLifeSupport();
    }
}
