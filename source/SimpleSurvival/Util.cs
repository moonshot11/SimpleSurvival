using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSurvival
{
    public static class Util
    {
        /// <summary>
        /// Checks whether resource is available for request from given part
        /// </summary>
        /// <param name="part">The part making this request</param>
        /// <param name="resource">The name of the resource</param>
        /// <param name="request">The amount of resource requested</param>
        /// <param name="flowmode">The FlowMode</param>
        /// <returns></returns>
        public static bool ResourceAvailable(Part part, string resource, double request,
            ResourceFlowMode flowmode = ResourceFlowMode.NULL)
        {
            // Will hold the value of the resource obtained
            double obtained = 0.0;
            // The margin for declaring double equality
            double margin = 0.000001;
            
            // Check resource availability by 
            if (flowmode == ResourceFlowMode.NULL)
                obtained = part.RequestResource(resource, request);
            else
                obtained = part.RequestResource(resource, request, flowmode);

            // Store result if amount obtained is "close enough" to original request
            bool result = (Math.Abs(request - obtained) < margin);

            // Restore resource that was obtained
            if (flowmode == ResourceFlowMode.NULL)
                part.RequestResource(resource, -obtained);
            else
                part.RequestResource(resource, -obtained, flowmode);

            return result;
        }
    }
}
