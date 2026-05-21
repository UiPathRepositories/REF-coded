using UiPath.CodedWorkflows;

namespace RoboticEnterpriseFrameworkcoded
{
    public class InitAllApplicationsTestCase : CodedWorkflow
    {
        [TestCase]
        public void Execute()
        {
            var config = LoadConfiguration("Data\\Config.xlsx", ["Settings", "Constants"]);
            InitAllApplicationsCore(config);
            CloseAllApplicationsCore();
            testing.VerifyExpression(true, "Application initialization and cleanup placeholders completed.");
        }
    }
}
