using UiPath.CodedWorkflows;

namespace RoboticEnterpriseFrameworkcoded
{
    public class WorkflowTestCaseTemplate : CodedWorkflow
    {
        [TestCase]
        public void Execute()
        {
            var config = LoadConfiguration("Data\\Config.xlsx", ["Settings", "Constants"]);
            testing.VerifyExpression(config != null, "Template setup should load configuration.");
        }
    }
}
