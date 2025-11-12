using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutboxBroker.Models
{
    public class OutboxMessage
    {
        public Guid Id { get; set; }
        public Guid TransactionId { get; set; }
        public Guid UserId { get; set; }
        public Guid AggregateId { get; set; }

        public string AggregateType { get; set; } = string.Empty;
        public long Sequence { get; set; }
        public string Type { get; set; } = string.Empty;
        public StatusCode Status { get; set; } = StatusCode.New;
    }

    public enum StatusCode
    {
        New = 0,
        Publishing = 1,
        Dead = 2
    }
}
