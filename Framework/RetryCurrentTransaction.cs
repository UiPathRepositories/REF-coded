using System;
using System.Collections.Generic;
using UiPath.CodedWorkflows;

namespace RoboticEnterpriseFrameworkcoded
{
    public class RetryCurrentTransaction : CodedWorkflow
    {
        [Workflow]
        public (int io_RetryNumber, int io_TransactionNumber) Execute(
            Dictionary<string, object> in_Config,
            int io_RetryNumber,
            int io_TransactionNumber,
            Exception in_SystemException,
            bool in_QueueRetry)
        {
            var result = RetryCurrentTransactionCore(in_Config, io_RetryNumber, io_TransactionNumber, in_SystemException, in_QueueRetry);
            return (result.RetryNumber, result.TransactionNumber);
        }
    }
}
