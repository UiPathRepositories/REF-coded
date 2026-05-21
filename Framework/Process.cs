using System.Collections.Generic;
using UiPath.CodedWorkflows;
using UiPath.Core;

namespace RoboticEnterpriseFrameworkcoded
{
    public class Process : CodedWorkflow
    {
        [Workflow]
        public void Execute(QueueItem in_TransactionItem, Dictionary<string, object> in_Config)
        {
            ProcessCore(in_TransactionItem, in_Config);
        }
    }
}
