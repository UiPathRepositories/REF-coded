using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using UiPath.Core;
using UiPath.Core.Activities.API;

namespace RoboticEnterpriseFrameworkcoded
{
    public partial class CodedWorkflow
    {
        protected Dictionary<string, object> LoadConfiguration(string in_ConfigFile = "Data\\Config.xlsx", string[] in_ConfigSheets = null)
        {
            var configSheets = in_ConfigSheets ?? ["Settings", "Constants"];
            var configFile = ResolveProjectPath(string.IsNullOrWhiteSpace(in_ConfigFile) ? "Data\\Config.xlsx" : in_ConfigFile);
            var config = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var sheetName in configSheets)
            {
                var table = ConfigWorkbookReader.ReadSheet(configFile, sheetName);
                foreach (DataRow row in table.Rows)
                {
                    var name = GetRowText(row, "Name").Trim();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        config[name] = GetRowText(row, "Value");
                    }
                }
            }

            LoadAssets(configFile, config);
            return config;
        }

        protected TransactionDataResult GetTransactionDataCore(int in_TransactionNumber, Dictionary<string, object> in_Config, DataTable io_dt_TransactionData)
        {
            Log("Get the transaction item");

            var queueName = GetConfigString(in_Config, "OrchestratorQueueName");
            var folderPath = GetConfigString(in_Config, "OrchestratorQueueFolder");
            var retries = GetConfigInt(in_Config, "RetryNumberGetTransactionItem", 0);

            QueueItem transactionItem = null;
            if (!string.IsNullOrWhiteSpace(queueName))
            {
                transactionItem = Retry(
                    () => string.IsNullOrWhiteSpace(folderPath)
                        ? system.GetQueueItem(queueName)
                        : system.GetQueueItem(queueName, folderPath),
                    retries,
                    "Could not retrieve transaction item.");
            }

            return new TransactionDataResult
            {
                TransactionItem = transactionItem,
                TransactionID = transactionItem == null ? null : DateTime.Now.ToString(CultureInfo.CurrentCulture),
                TransactionField1 = transactionItem == null ? null : string.Empty,
                TransactionField2 = transactionItem == null ? null : string.Empty,
                TransactionData = io_dt_TransactionData
            };
        }

        protected void ProcessCore(QueueItem in_TransactionItem, Dictionary<string, object> in_Config)
        {
            Log("Started Process");
        }

        protected void InitAllApplicationsCore(Dictionary<string, object> in_Config)
        {
            Log("Opening applications...");
        }

        protected void CloseAllApplicationsCore()
        {
            Log("Closing applications...");
        }

        protected void KillAllProcessesCore()
        {
            Log("Killing processes...");
        }

        protected RetryTransactionResult RetryCurrentTransactionCore(
            Dictionary<string, object> in_Config,
            int io_RetryNumber,
            int io_TransactionNumber,
            Exception in_SystemException,
            bool in_QueueRetry)
        {
            var maxRetryNumber = GetConfigInt(in_Config, "MaxRetryNumber", 0);
            var applicationMessage = GetConfigString(in_Config, "LogMessage_ApplicationException");
            var exceptionText = FormatException(in_SystemException);

            if (maxRetryNumber > 0)
            {
                if (io_RetryNumber >= maxRetryNumber)
                {
                    Log(applicationMessage + " Max number of retries reached. " + exceptionText);
                    io_RetryNumber = 0;
                    io_TransactionNumber++;
                }
                else
                {
                    Log(applicationMessage + " Retry: " + io_RetryNumber.ToString(CultureInfo.InvariantCulture) + ". " + exceptionText);
                    if (in_QueueRetry)
                    {
                        io_TransactionNumber++;
                    }
                    else
                    {
                        io_RetryNumber++;
                    }
                }
            }
            else
            {
                Log(applicationMessage + exceptionText);
                io_TransactionNumber++;
            }

            return new RetryTransactionResult
            {
                RetryNumber = io_RetryNumber,
                TransactionNumber = io_TransactionNumber
            };
        }

        protected TransactionStatusResult SetTransactionStatusCore(
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
            if (in_BusinessException == null && in_SystemException == null)
            {
                SetQueueStatusWithRetry(in_Config, in_TransactionItem, ProcessingStatus.Successful);
                LogTransaction(in_Config, "LogMessage_Success", io_TransactionNumber, in_TransactionID, in_TransactionField1, in_TransactionField2, null);

                return new TransactionStatusResult
                {
                    RetryNumber = 0,
                    TransactionNumber = io_TransactionNumber + 1,
                    ConsecutiveSystemExceptions = 0
                };
            }

            if (in_BusinessException != null)
            {
                SetQueueStatusWithRetry(in_Config, in_TransactionItem, ProcessingStatus.Failed);
                LogTransaction(in_Config, "LogMessage_BusinessRuleException", io_TransactionNumber, in_TransactionID, in_TransactionField1, in_TransactionField2, in_BusinessException.Message);

                return new TransactionStatusResult
                {
                    RetryNumber = 0,
                    TransactionNumber = io_TransactionNumber + 1,
                    ConsecutiveSystemExceptions = 0
                };
            }

            Log("Consecutive system exception counter is " + io_ConsecutiveSystemExceptions.ToString(CultureInfo.InvariantCulture) + ".");
            var queueRetry = in_TransactionItem != null;
            string screenshotPath = null;

            try
            {
                screenshotPath = TakeScreenshotCore(GetConfigString(in_Config, "ExScreenshotsFolderPath"), null);
            }
            catch (Exception exception)
            {
                Log("Failed to take screenshot: " + exception.Message + " at Source: " + exception.Source);
            }

            if (queueRetry)
            {
                SetQueueStatusWithRetry(in_Config, in_TransactionItem, ProcessingStatus.Failed);
                io_RetryNumber = in_TransactionItem.RetryNo;
            }

            LogTransaction(in_Config, "LogMessage_ApplicationException", io_TransactionNumber, in_TransactionID, in_TransactionField1, in_TransactionField2, FormatException(in_SystemException));
            io_ConsecutiveSystemExceptions++;

            var retryResult = RetryCurrentTransactionCore(in_Config, io_RetryNumber, io_TransactionNumber, in_SystemException, queueRetry);

            try
            {
                CloseAllApplicationsCore();
            }
            catch (Exception closeException)
            {
                Log("CloseAllApplications failed. " + closeException.Message + " at Source: " + closeException.Source);
                try
                {
                    KillAllProcessesCore();
                }
                catch (Exception killException)
                {
                    Log("KillAllProcesses failed. " + killException.Message + " at Source: " + killException.Source);
                }
            }

            return new TransactionStatusResult
            {
                RetryNumber = retryResult.RetryNumber,
                TransactionNumber = retryResult.TransactionNumber,
                ConsecutiveSystemExceptions = io_ConsecutiveSystemExceptions
            };
        }

        protected string TakeScreenshotCore(string in_Folder, string io_FilePath)
        {
            var filePath = string.IsNullOrWhiteSpace(io_FilePath)
                ? Path.Combine(ResolveProjectPath(string.IsNullOrWhiteSpace(in_Folder) ? "Exceptions_Screenshots" : in_Folder), "ExceptionScreenshot_" + DateTime.Now.ToString("yyMMdd.hhmmss", CultureInfo.InvariantCulture) + ".png")
                : ResolveProjectPath(io_FilePath);

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var bitmap = new System.Drawing.Bitmap(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height))
            using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Location, System.Drawing.Point.Empty, bitmap.Size);
                bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
            }

            Log("Screenshot saved at: " + filePath);
            return filePath;
        }

        protected string GetConfigString(Dictionary<string, object> config, string key, string defaultValue = "")
        {
            if (config == null || !config.TryGetValue(key, out var value) || value == null)
            {
                return defaultValue;
            }

            return value.ToString();
        }

        protected int GetConfigInt(Dictionary<string, object> config, string key, int defaultValue = 0)
        {
            var value = GetConfigString(config, key);
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : defaultValue;
        }

        protected bool GetConfigBool(Dictionary<string, object> config, string key, bool defaultValue = false)
        {
            var value = GetConfigString(config, key);
            return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
        }

        private void LoadAssets(string configFile, Dictionary<string, object> config)
        {
            DataTable assets;
            try
            {
                assets = ConfigWorkbookReader.ReadSheet(configFile, "Assets");
            }
            catch
            {
                return;
            }

            foreach (DataRow row in assets.Rows)
            {
                var name = GetRowText(row, "Name").Trim();
                var assetName = GetRowText(row, "Asset").Trim();
                var folderPath = GetRowText(row, "OrchestratorAssetFolder").Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                try
                {
                    if (string.IsNullOrWhiteSpace(assetName))
                    {
                        throw new InvalidOperationException("Asset column is empty.");
                    }

                    config[name] = string.IsNullOrWhiteSpace(folderPath)
                        ? system.GetAsset(assetName)
                        : system.GetAsset(assetName, folderPath);
                }
                catch (Exception exception)
                {
                    throw new Exception("Loading asset " + assetName + " failed: " + exception.Message, exception);
                }
            }
        }

        private void SetQueueStatusWithRetry(
            Dictionary<string, object> config,
            QueueItem transactionItem,
            ProcessingStatus status)
        {
            if (transactionItem == null)
            {
                return;
            }

            var retries = GetConfigInt(config, "RetryNumberSetTransactionStatus", 0);
            Retry(
                () =>
                {
                    var folderPath = GetConfigString(config, "OrchestratorQueueFolder");
                    if (string.IsNullOrWhiteSpace(folderPath))
                    {
                        system.SetTransactionStatus(transactionItem, status);
                    }
                    else
                    {
                        system.SetTransactionStatus(transactionItem, status, folderPath);
                    }

                    return true;
                },
                retries,
                "Could not set the transaction status.");
        }

        private void LogTransaction(
            Dictionary<string, object> config,
            string messageKey,
            int transactionNumber,
            string transactionId,
            string transactionField1,
            string transactionField2,
            string suffix)
        {
            var fields = new Dictionary<string, object>
            {
                { GetConfigString(config, "logF_TransactionNumber", "TransactionNumber"), transactionNumber.ToString(CultureInfo.InvariantCulture) },
                { GetConfigString(config, "logF_TransactionID", "TransactionID"), transactionId ?? string.Empty },
                { GetConfigString(config, "logF_TransactionField1", "TransactionField1"), transactionField1 ?? string.Empty },
                { GetConfigString(config, "logF_TransactionField2", "TransactionField2"), transactionField2 ?? string.Empty }
            };

            Log(GetConfigString(config, messageKey) + (suffix ?? string.Empty), additionalLogFields: fields);
        }

        private T Retry<T>(Func<T> action, int retryCount, string warningPrefix)
        {
            Exception lastException = null;
            for (var attempt = 0; attempt <= retryCount; attempt++)
            {
                try
                {
                    return action();
                }
                catch (Exception exception)
                {
                    lastException = exception;
                    if (attempt >= retryCount)
                    {
                        break;
                    }

                    Log(warningPrefix + " Exception message: " + exception.Message);
                }
            }

            throw lastException ?? new InvalidOperationException(warningPrefix);
        }

        private static string GetRowText(DataRow row, string columnName)
        {
            if (row == null || row.Table == null || !row.Table.Columns.Contains(columnName) || row[columnName] == null)
            {
                return string.Empty;
            }

            return row[columnName].ToString();
        }

        private static string FormatException(Exception exception)
        {
            if (exception == null)
            {
                return string.Empty;
            }

            return exception.Message + " at Source: " + exception.Source;
        }

        private static string ResolveProjectPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return Directory.GetCurrentDirectory();
            }

            return Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
        }
    }
}
