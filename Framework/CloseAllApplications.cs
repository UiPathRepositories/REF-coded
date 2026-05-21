using UiPath.CodedWorkflows;

namespace RoboticEnterpriseFrameworkcoded
{
    public class CloseAllApplications : CodedWorkflow
    {
        [Workflow]
        public void Execute()
        {
            CloseAllApplicationsCore();
        }
    }
}
