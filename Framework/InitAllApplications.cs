using System.Collections.Generic;
using UiPath.CodedWorkflows;

namespace RoboticEnterpriseFrameworkcoded
{
    public class InitAllApplications : CodedWorkflow
    {
        [Workflow]
        public void Execute(Dictionary<string, object> in_Config)
        {
            InitAllApplicationsCore(in_Config);
        }
    }
}
