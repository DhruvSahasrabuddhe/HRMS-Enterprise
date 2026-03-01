using System.ComponentModel.DataAnnotations;

namespace HRMS.Core.Entities.Base
{
    public abstract class BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}