using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static CashDesk.Model;
namespace CashDesk
{
    /// <inheritdoc />
    public class DataAccess : IDataAccess
    {
        private CashDeskDataContext dataContext;
        private void ThrowIfNotInitialized()
        {
            // A user has to call InitializeDatabaseAsync before calling any other method of the class.
            if (dataContext == null)
            {
                throw new InvalidOperationException("Not initialized");
            }
        }
        /// <inheritdoc />
        public Task InitializeDatabaseAsync()
        {
            if (dataContext != null)
            {
                // InitializeDatabaseAsync has already been called
                throw new InvalidOperationException("Already initialized");
            }
            dataContext = new CashDeskDataContext();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<int> AddMemberAsync(string firstName, string lastName, DateTime birthday)
        {
            ThrowIfNotInitialized();
            if (string.IsNullOrEmpty(firstName))
            {
                throw new ArgumentException("Must not be null or empty", nameof(firstName));
            }

            if (string.IsNullOrEmpty(lastName))
            {
                throw new ArgumentException("Must not be null or empty", nameof(lastName));
            }
            if (await dataContext.Members.AnyAsync(m => m.LastName == lastName))
            {
                throw new DuplicateNameException("Lastname already exists in database");
            }
            var newMember = new Member
            {
                FirstName = firstName,
                LastName = lastName,
                Birthday = birthday
            };
            dataContext.Members.Add(newMember);
            await dataContext.SaveChangesAsync();
            return newMember.MemberNumber;
        }

        /// <inheritdoc />
        public async Task DeleteMemberAsync(int memberNumber)
        {
            ThrowIfNotInitialized();
            Member member;
            try
            {
                member = await dataContext.Members.FirstAsync(m => m.MemberNumber == memberNumber);
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException();
            }
            dataContext.Members.Remove(member);
            await dataContext.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<IMembership> JoinMemberAsync(int memberNumber)
        {
            ThrowIfNotInitialized();
            Membership newMembership;
            if (await dataContext.Memberships.AnyAsync(m => m.Member.MemberNumber == memberNumber
                   && DateTime.Now >= m.Begin && DateTime.Now <= m.End))
            {
                throw new AlreadyMemberException();
            }
            newMembership = new Membership
            {
                Member = await dataContext.Members.FirstAsync(m => m.MemberNumber == memberNumber),
                Begin = DateTime.Now,
                End = DateTime.MaxValue
            };
            dataContext.Memberships.Add(newMembership);
            await dataContext.SaveChangesAsync();

            return newMembership;
        }

        /// <inheritdoc />
        public async Task<IMembership> CancelMembershipAsync(int memberNumber)
        {
            ThrowIfNotInitialized();
            Membership membership;
            try
            {
                membership = await dataContext.Memberships.FirstAsync(m => m.Member.MemberNumber == memberNumber
                   && m.Member.Memberships.Count != 0 && m.End == DateTime.MaxValue);
            }
            catch (InvalidOperationException)
            {
                throw new NoMemberException();
            };
            membership.End = DateTime.Now;
            await dataContext.SaveChangesAsync();
            return membership;
        }

        /// <inheritdoc />
        public async Task DepositAsync(int memberNumber, decimal amount)
        {
            ThrowIfNotInitialized();
            Member member;
            try
            {
                member = await dataContext.Members.FirstAsync(m => m.MemberNumber == memberNumber);
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException();
            }
            Membership membership;
            try
            {
                membership = await dataContext.Memberships.FirstAsync(m => m.Member.MemberNumber == memberNumber
                   && m.Member.Memberships.Count != 0 && m.End == DateTime.MaxValue);
            }
            catch (InvalidOperationException)
            {
                throw new NoMemberException();
            };
            var newDeposit = new Deposit
            {
                Amount = amount,
                Membership = membership
            };
            dataContext.Deposits.Add(newDeposit);
            await dataContext.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<IDepositStatistics>> GetDepositStatisticsAsync()
        {
            ThrowIfNotInitialized();
            return (await dataContext.Deposits.Include("Membership.Member").ToArrayAsync())
                .GroupBy(d => new { d.Membership.Begin.Year, d.Membership.Member })
                .Select(i => new DepositStatistics
                {
                    Year = i.Key.Year,
                    Member = i.Key.Member,
                    TotalAmount = i.Sum(d => d.Amount)
                });
        }

        /// <inheritdoc />
        public void Dispose()
        {
            ThrowIfNotInitialized();
            if (dataContext != null)
            {
                dataContext.Dispose();
                dataContext = null;
            }
        }
    }
}