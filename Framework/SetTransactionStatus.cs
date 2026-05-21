using System;
using System.Collections.Generic;
using UiPath.CodedWorkflows;
using UiPath.Core;

namespace RoboticEnterpriseFrameworkcoded
{
    public class SetTransactionStatus : CodedWorkflow
    {
        [Workflow]
        public (int io_RetryNumber, int io_TransactionNumber, int io_ConsecutiveSystemExceptions) Execute(
            BusinessRuleException in_BusinessException,
            Dictionary<string, object> in_Config,
            QueueItem in_TransactionItem,
            int io_RetryNumber,
            int io_TransactionNumber,
            string in_TransactionField1,
            string in_TransactionField2,
            string in_TransactionID,
            Exception in_SystemException,
            int io_ConsecutiveSystemExceptions)
        {
            var result = SetTransactionStatusCore(
                in_BusinessException,
                in_Config,
                in_TransactionItem,
                io_RetryNumber,
                io_TransactionNumber,
                in_TransactionField1,
                in_TransactionField2,
                in_TransactionID,
                in_SystemException,
                io_ConsecutiveSystemExceptions);

            return (result.RetryNumber, result.TransactionNumber, result.ConsecutiveSystemExceptions);
        }
    }
}
