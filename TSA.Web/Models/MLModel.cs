using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TSA.Web.Models
{
    public class MLModel
    {
        public enum CurrentStep
        {
            SelectSource,
            SelectModel,
            Train,
            Topics,
            Results
        }

        public CurrentStep Step { get; set; }

        public MLModel()
        {
            Step = CurrentStep.SelectSource;
        }
    }
}
