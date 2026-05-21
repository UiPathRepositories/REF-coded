using UiPath.CodedWorkflows;

namespace RoboticEnterpriseFrameworkcoded
{
    public class InitAllSettingsTestCase : CodedWorkflow
    {
        [TestCase]
        public void Execute()
        {
            var config = LoadConfiguration("Data\\Config.xlsx", ["Settings", "Constants"]);
            testing.VerifyExpression(config.ContainsKey("OrchestratorQueueName"), "Config should include OrchestratorQueueName.");
            testing.VerifyExpression(config.ContainsKey("MaxRetryNumber"), "Config should include MaxRetryNumber.");
        }
    }
}
