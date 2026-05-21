using System.Collections.Generic;
using System.Data;
using UiPath.CodedWorkflows;
using UiPath.Core;

namespace RoboticEnterpriseFrameworkcoded
{
    public class GetTransactionData : CodedWorkflow
    {
        [Workflow]
        public (QueueItem out_TransactionItem, string out_TransactionField1, string out_TransactionField2, string out_TransactionID, DataTable io_dt_TransactionData) Execute(
            int in_TransactionNumber,
            Dictionary<string, object> in_Config,
            DataTable io_dt_TransactionData)
        {
            var result = GetTransactionDataCore(in_TransactionNumber, in_Config, io_dt_TransactionData);
            return (result.TransactionItem, result.TransactionField1, result.TransactionField2, result.TransactionID, result.TransactionData);
        }
    }
}
