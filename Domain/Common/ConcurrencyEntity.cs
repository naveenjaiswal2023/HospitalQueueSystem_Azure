using System.ComponentModel.DataAnnotations;

namespace HospitalQueueSystem.Domain.Common
{
    public abstract class ConcurrencyEntity
    {
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}
