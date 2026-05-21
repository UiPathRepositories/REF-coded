using UiPath.CodedWorkflows;

namespace RoboticEnterpriseFrameworkcoded
{
    public class KillAllProcesses : CodedWorkflow
    {
        [Workflow]
        public void Execute()
        {
            KillAllProcessesCore();
        }
    }
}
