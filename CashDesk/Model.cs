using Foolproof;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace CashDesk
{
    public class Model
    {
        public class Member : IMember
        {
            [Key]
            public int MemberNumber { get; set; }
            [Required, StringLength(100)]
            public string FirstName { get; set; }
            [Required, StringLength(100), Index(IsUnique = true)]
            public string LastName { get; set; }
            [Required]
            public DateTime Birthday { get; set; }
            public List<Membership> Memberships { get; set; }

        }
        public class Membership : IMembership
        {
            public int MembershipId { get; set; }
            [Required]
            public Member Member { get; set; }
            [Required]
            public DateTime Begin { get; set; }
            [Required, GreaterThan("Begin")]
            public DateTime End { get; set; }
            public List<Deposit> Deposits { get; set; }
            IMember IMembership.Member => Member;
        }
        public class Deposit : IDeposit
        {
            public int DepositId { get; set; }
            [Required]
            public Membership Membership { get; set; }
            [GreaterThan("0")]
            public decimal Amount { get; set; }
            IMembership IDeposit.Membership => Membership;

        }
        public class DepositStatistics : IDepositStatistics
        {
            public IMember Member { get; set; }
            public int Year { get; set; }
            public decimal TotalAmount { get; set; }
        }
    }

}