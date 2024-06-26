﻿using iSecureGateway_Suprema.Commons.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace iSecureGateway_Suprema.Models
{
    [Table("AccessLevel")]
    public class AccessLevel : BaseEntity
    {
        [Key]
        public required string Code { get; set; }

        public required string Name { get; set; }

        public uint Id { get; set; }

        public virtual AccessSchedule? AccessSchedule { get; set; }

        public virtual ICollection<AccessGroup>? AccessGroups { get; set; } = [];

        public virtual ICollection<AccessGroupAccessLevel>? AccessGroupAccessLevels { get; set; }
    }
}
