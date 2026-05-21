using UiPath.CodedWorkflows;

namespace RoboticEnterpriseFrameworkcoded
{
    public class ProcessTestCase : CodedWorkflow
    {
        [TestCase]
        public void Execute()
        {
            var config = LoadConfiguration("Data\\Config.xlsx", ["Settings", "Constants"]);
            ProcessCore(null, config);
            testing.VerifyExpression(true, "Process placeholder completed.");
        }
    }
}
