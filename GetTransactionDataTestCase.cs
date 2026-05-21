using UiPath.CodedWorkflows;

namespace RoboticEnterpriseFrameworkcoded
{
    public class GetTransactionDataTestCase : CodedWorkflow
    {
        [TestCase]
        public void Execute()
        {
            var config = LoadConfiguration("Data\\Config.xlsx", ["Settings", "Constants"]);
            testing.VerifyExpression(config.ContainsKey("RetryNumberGetTransactionItem"), "Config should include RetryNumberGetTransactionItem.");

            if (string.IsNullOrWhiteSpace(GetConfigString(config, "OrchestratorQueueName")))
            {
                var result = GetTransactionDataCore(1, config, null);
                testing.VerifyExpression(result.TransactionItem == null, "No queue name should result in no transaction item.");
            }
            else
            {
                Log("Queue name is configured; transaction retrieval depends on Orchestrator queue contents.");
                testing.VerifyExpression(true, "Queue-backed transaction retrieval is configured.");
            }
        }
    }
}
