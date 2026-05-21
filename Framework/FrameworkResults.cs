using System.Data;
using UiPath.Core;

namespace RoboticEnterpriseFrameworkcoded
{
    public class TransactionDataResult
    {
        public QueueItem TransactionItem { get; set; }
        public string TransactionField1 { get; set; }
        public string TransactionField2 { get; set; }
        public string TransactionID { get; set; }
        public DataTable TransactionData { get; set; }
    }

    public class RetryTransactionResult
    {
        public int RetryNumber { get; set; }
        public int TransactionNumber { get; set; }
    }

    public class TransactionStatusResult
    {
        public int RetryNumber { get; set; }
        public int TransactionNumber { get; set; }
        public int ConsecutiveSystemExceptions { get; set; }
    }
}
