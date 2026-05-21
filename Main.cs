using System;
using System.Collections.Generic;
using System.Data;
using UiPath.CodedWorkflows;
using UiPath.Core;

namespace RoboticEnterpriseFrameworkcoded
{
    public class Main : CodedWorkflow
    {
        [Workflow]
        public void Execute(string in_OrchestratorQueueName = null, string in_OrchestratorQueueFolder = null)
        {
            var state = FrameworkState.Initialization;
            Exception systemException = null;
            BusinessRuleException businessException = null;
            QueueItem transactionItem = null;
            Dictionary<string, object> config = null;
            var transactionNumber = 1;
            var retryNumber = 0;
            var consecutiveSystemExceptions = 0;
            var transactionField1 = string.Empty;
            var transactionField2 = string.Empty;
            var transactionID = string.Empty;
            DataTable dt_TransactionData = null;

            while (true)
            {
                if (state == FrameworkState.Initialization)
                {
                    systemException = null;
                    try
                    {
                        if (config == null)
                        {
                            config = LoadConfiguration("Data\\Config.xlsx", ["Settings", "Constants"]);
                            if (!string.IsNullOrWhiteSpace(in_OrchestratorQueueName))
                            {
                                config["OrchestratorQueueName"] = in_OrchestratorQueueName;
                            }

                            if (!string.IsNullOrWhiteSpace(in_OrchestratorQueueFolder))
                            {
                                config["OrchestratorQueueFolder"] = in_OrchestratorQueueFolder;
                            }

                            KillAllProcessesCore();
                            Log("The coded REFramework process has initialized.");
                        }

                        var maxConsecutive = GetConfigInt(config, "MaxConsecutiveSystemExceptions", 0);
                        if (maxConsecutive > 0 && consecutiveSystemExceptions >= maxConsecutive)
                        {
                            throw new Exception(GetConfigString(config, "ExceptionMessage_ConsecutiveErrors") + " Consecutive retry number: " + (consecutiveSystemExceptions + 1));
                        }

                        InitAllApplicationsCore(config);
                    }
                    catch (Exception exception)
                    {
                        systemException = exception;
                    }

                    if (systemException == null)
                    {
                        state = FrameworkState.GetTransactionData;
                    }
                    else
                    {
                        Log("System exception at initialization: " + systemException.Message + " at Source: " + systemException.Source);
                        state = FrameworkState.EndProcess;
                    }
                }
                else if (state == FrameworkState.GetTransactionData)
                {
                    try
                    {
                        var shouldStop = false;
                        if (shouldStop)
                        {
                            Log("Stop process requested.");
                            transactionItem = null;
                        }
                        else
                        {
                            var transactionData = GetTransactionDataCore(transactionNumber, config, dt_TransactionData);
                            transactionItem = transactionData.TransactionItem;
                            transactionField1 = transactionData.TransactionField1;
                            transactionField2 = transactionData.TransactionField2;
                            transactionID = transactionData.TransactionID;
                            dt_TransactionData = transactionData.TransactionData;
                        }
                    }
                    catch (Exception exception)
                    {
                        Log(GetConfigString(config, "LogMessage_GetTransactionDataError") + transactionNumber + ". " + exception.Message + " at Source: " + exception.Source);
                        transactionItem = null;
                    }

                    if (transactionItem != null)
                    {
                        Log(GetConfigString(config, "LogMessage_GetTransactionData") + transactionNumber);
                        state = FrameworkState.ProcessTransaction;
                    }
                    else
                    {
                        Log("Process finished due to no more transaction data");
                        state = FrameworkState.EndProcess;
                    }
                }
                else if (state == FrameworkState.ProcessTransaction)
                {
                    businessException = null;
                    systemException = null;

                    try
                    {
                        ProcessCore(transactionItem, config);
                        var status = SetTransactionStatusCore(null, config, transactionItem, retryNumber, transactionNumber, transactionField1, transactionField2, transactionID, null, consecutiveSystemExceptions);
                        retryNumber = status.RetryNumber;
                        transactionNumber = status.TransactionNumber;
                        consecutiveSystemExceptions = status.ConsecutiveSystemExceptions;
                    }
                    catch (BusinessRuleException exception)
                    {
                        businessException = exception;
                        var status = SetTransactionStatusCore(businessException, config, transactionItem, retryNumber, transactionNumber, transactionField1, transactionField2, transactionID, null, consecutiveSystemExceptions);
                        retryNumber = status.RetryNumber;
                        transactionNumber = status.TransactionNumber;
                        consecutiveSystemExceptions = status.ConsecutiveSystemExceptions;
                    }
                    catch (Exception exception)
                    {
                        systemException = exception;
                        var status = SetTransactionStatusCore(null, config, transactionItem, retryNumber, transactionNumber, transactionField1, transactionField2, transactionID, systemException, consecutiveSystemExceptions);
                        retryNumber = status.RetryNumber;
                        transactionNumber = status.TransactionNumber;
                        consecutiveSystemExceptions = status.ConsecutiveSystemExceptions;
                    }

                    state = systemException == null ? FrameworkState.GetTransactionData : FrameworkState.Initialization;
                }
                else
                {
                    try
                    {
                        CloseAllApplicationsCore();
                    }
                    catch (Exception exception)
                    {
                        Log("Applications failed to close gracefully. " + exception.Message + " at Source: " + exception.Source);
                        KillAllProcessesCore();
                    }

                    if (systemException != null && GetConfigBool(config, "ShouldMarkJobAsFaulted", false))
                    {
                        throw systemException;
                    }

                    return;
                }
            }
        }

        private enum FrameworkState
        {
            Initialization,
            GetTransactionData,
            ProcessTransaction,
            EndProcess
        }
    }
}
