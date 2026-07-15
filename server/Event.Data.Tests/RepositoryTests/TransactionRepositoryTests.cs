using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Event.Models;
using Event.Data.Repositories;

namespace Event.Data.Tests.RepositoryTests
{
    [TestFixture]
    public class TransactionRepositoryTests : RepositoryTestBase
    {
        private TransactionRepository? _repository;
        private Seed.DbSeeder? _seeder;
        private const string Repo = "TransactionRepository";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _repository = new TransactionRepository(Context);
            _seeder = new Seed.DbSeeder(Context);
        }

        private Transaction CreateMockTransaction(int userId)
        {
            return new Transaction
            {
                Sender_Id = $"Attendee_User_{userId}",
                Receiver_Id = "Platform_Escrow",
                Transaction_Type = "BookingPayment",
                Related_Id = 1,
                Amount = 200.00m,
                Currency = "USD",
                Status = "Success",
                Transaction_Reference = "tx_ref_" + new Random().Next(100000, 999999),
                Created_At = DateTime.UtcNow
            };
        }
        #endregion

        #region Create Tests
        [Test]
        public async Task Test_AddAsync()
        {
            var user = await _seeder.SeedUserAsync("Attendee");
            var transaction = CreateMockTransaction(user.User_Id);

            try
            {
                await _repository.AddAsync(transaction);
                Assert.That(transaction.Transaction_Id, Is.GreaterThan(0));
                LogTestDetail(Repo, "AddAsync", "Create new transaction", transaction, transaction, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "AddAsync", "Create new transaction", transaction, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Read Tests
        [Test]
        public async Task Test_GetByIdAsync()
        {
            var user = await _seeder.SeedUserAsync("Attendee");
            var transaction = CreateMockTransaction(user.User_Id);
            await _repository.AddAsync(transaction);

            try
            {
                var fetched = await _repository.GetByIdAsync(transaction.Transaction_Id);
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched.Amount, Is.EqualTo(200.00m));
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve transaction by ID", transaction.Transaction_Id, fetched, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve transaction by ID", transaction.Transaction_Id, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetTransactionsByUserIdAsync()
        {
            var user = await _seeder.SeedUserAsync("Attendee");
            var transaction = CreateMockTransaction(user.User_Id);
            await _repository.AddAsync(transaction);

            try
            {
                var list = await _repository.GetTransactionsByUserIdAsync(user.User_Id);
                Assert.That(list.Count(), Is.EqualTo(1));
                LogTestDetail(Repo, "GetTransactionsByUserIdAsync", "Retrieve transactions for User ID", user.User_Id, list.Count(), true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetTransactionsByUserIdAsync", "Retrieve transactions for User ID", user.User_Id, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetTransactionByReferenceAsync()
        {
            var user = await _seeder.SeedUserAsync("Attendee");
            var transaction = CreateMockTransaction(user.User_Id);
            await _repository.AddAsync(transaction);

            try
            {
                var fetched = await _repository.GetTransactionByReferenceAsync(transaction.Transaction_Reference!);
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched.Transaction_Id, Is.EqualTo(transaction.Transaction_Id));
                LogTestDetail(Repo, "GetTransactionByReferenceAsync", "Retrieve transaction by reference string", transaction.Transaction_Reference, fetched, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetTransactionByReferenceAsync", "Retrieve transaction by reference string", transaction.Transaction_Reference, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetPendingBookingTransactionAsync()
        {
            var user = await _seeder.SeedUserAsync("Attendee");
            var pendingTx = new Transaction
            {
                Sender_Id = $"Attendee_User_{user.User_Id}",
                Receiver_Id = "Platform_Escrow",
                Transaction_Type = "BookingPayment",
                Related_Id = 99,
                Amount = 100.00m,
                Currency = "USD",
                Status = "Pending",
                Created_At = DateTime.UtcNow
            };
            await _repository.AddAsync(pendingTx);

            try
            {
                var fetched = await _repository.GetPendingBookingTransactionAsync(99);
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched.Transaction_Id, Is.EqualTo(pendingTx.Transaction_Id));
                LogTestDetail(Repo, "GetPendingBookingTransactionAsync", "Retrieve pending booking transaction by Booking ID", 99, fetched, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetPendingBookingTransactionAsync", "Retrieve pending booking transaction by Booking ID", 99, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetSuccessBookingTransactionAsync()
        {
            var user = await _seeder.SeedUserAsync("Attendee");
            var successTx = new Transaction
            {
                Sender_Id = $"Attendee_User_{user.User_Id}",
                Receiver_Id = "Platform_Escrow",
                Transaction_Type = "BookingPayment",
                Related_Id = 99,
                Amount = 100.00m,
                Currency = "USD",
                Status = "Success",
                Created_At = DateTime.UtcNow
            };
            await _repository.AddAsync(successTx);

            try
            {
                var fetched = await _repository.GetSuccessBookingTransactionAsync(99);
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched.Transaction_Id, Is.EqualTo(successTx.Transaction_Id));
                LogTestDetail(Repo, "GetSuccessBookingTransactionAsync", "Retrieve successful booking transaction by Booking ID", 99, fetched, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetSuccessBookingTransactionAsync", "Retrieve successful booking transaction by Booking ID", 99, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Update Tests
        [Test]
        public async Task Test_UpdateAsync()
        {
            var user = await _seeder.SeedUserAsync("Attendee");
            var transaction = CreateMockTransaction(user.User_Id);
            await _repository.AddAsync(transaction);

            try
            {
                transaction.Status = "Refunded";
                transaction.Refunded_Amount = 200.00m;
                await _repository.UpdateAsync(transaction);
                var updated = await _repository.GetByIdAsync(transaction.Transaction_Id);
                Assert.That(updated.Status, Is.EqualTo("Refunded"));
                Assert.That(updated.Refunded_Amount, Is.EqualTo(200.00m));
                LogTestDetail(Repo, "UpdateAsync", "Update transaction status and refunded amount", transaction, updated, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "UpdateAsync", "Update transaction status and refunded amount", transaction, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Delete Tests
        [Test]
        public async Task Test_DeleteAsync()
        {
            var user = await _seeder.SeedUserAsync("Attendee");
            var transaction = CreateMockTransaction(user.User_Id);
            await _repository.AddAsync(transaction);

            try
            {
                await _repository.DeleteAsync(transaction);
                var deleted = await _repository.GetByIdAsync(transaction.Transaction_Id);
                Assert.That(deleted, Is.Null);
                LogTestDetail(Repo, "DeleteAsync", "Remove transaction from database", transaction, deleted, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "DeleteAsync", "Remove transaction from database", transaction, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Paged Query Tests
        [Test]
        public async Task Test_GetTransactionsPagedAsync()
        {
            var user = await _seeder.SeedUserAsync("Attendee");
            
            var tx1 = new Transaction
            {
                Sender_Id = $"Attendee_User_{user.User_Id}",
                Receiver_Id = "Platform_Escrow",
                Transaction_Type = "BookingPayment",
                Related_Id = 101,
                Amount = 150.00m,
                Currency = "USD",
                Status = "Success",
                Transaction_Reference = $"tx_ref_abc_{user.User_Id}",
                Created_At = DateTime.UtcNow.AddDays(-2),
                Remarks = $"Payment for concert ticket {user.User_Id}"
            };
            var tx2 = new Transaction
            {
                Sender_Id = $"Attendee_User_{user.User_Id}",
                Receiver_Id = "Platform_Escrow",
                Transaction_Type = "OrganizerUpfrontPayment",
                Related_Id = 102,
                Amount = 300.00m,
                Currency = "USD",
                Status = "Pending",
                Transaction_Reference = $"tx_ref_xyz_{user.User_Id}",
                Created_At = DateTime.UtcNow.AddDays(-1),
                Remarks = $"Upfront venue cost {user.User_Id}"
            };
            
            await _repository.AddAsync(tx1);
            await _repository.AddAsync(tx2);

            try
            {
                // 1. Keyword filter (searches remarks)
                var resultKeyword = await _repository.GetTransactionsPagedAsync($"concert ticket {user.User_Id}", null, null, null, null, null, 1, 10);
                Assert.That(resultKeyword.Items.Count, Is.EqualTo(1));
                Assert.That(resultKeyword.Items[0].Transaction_Reference, Is.EqualTo($"tx_ref_abc_{user.User_Id}"));

                // 2. Type filter
                var resultType = await _repository.GetTransactionsPagedAsync(null, "OrganizerUpfrontPayment", null, null, null, null, 1, 100);
                Assert.That(resultType.Items.All(t => t.Transaction_Type.Equals("OrganizerUpfrontPayment", StringComparison.OrdinalIgnoreCase)), Is.True);
                Assert.That(resultType.Items.Any(t => t.Transaction_Reference == $"tx_ref_xyz_{user.User_Id}"), Is.True);

                // 3. Status filter
                var resultStatus = await _repository.GetTransactionsPagedAsync(null, null, "Pending", null, null, null, 1, 100);
                Assert.That(resultStatus.Items.All(t => t.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase)), Is.True);
                Assert.That(resultStatus.Items.Any(t => t.Transaction_Reference == $"tx_ref_xyz_{user.User_Id}"), Is.True);

                // 4. Date range filter
                var resultDate = await _repository.GetTransactionsPagedAsync(null, null, null, DateTime.UtcNow.AddDays(-1.5), DateTime.UtcNow, null, 1, 100);
                Assert.That(resultDate.Items.All(t => t.Created_At >= DateTime.UtcNow.AddDays(-1.5) && t.Created_At <= DateTime.UtcNow), Is.True);
                Assert.That(resultDate.Items.Any(t => t.Transaction_Reference == $"tx_ref_xyz_{user.User_Id}"), Is.True);

                // 5. Sorting and pagination (amount_desc)
                var resultSort = await _repository.GetTransactionsPagedAsync(user.User_Id.ToString(), null, null, null, null, "amount_desc", 1, 100);
                for (int i = 0; i < resultSort.Items.Count - 1; i++)
                {
                    Assert.That(resultSort.Items[i].Amount, Is.GreaterThanOrEqualTo(resultSort.Items[i + 1].Amount));
                }
                Assert.That(resultSort.Items.Any(t => t.Transaction_Reference == $"tx_ref_abc_{user.User_Id}"), Is.True);
                Assert.That(resultSort.Items.Any(t => t.Transaction_Reference == $"tx_ref_xyz_{user.User_Id}"), Is.True);

                LogTestDetail(Repo, "GetTransactionsPagedAsync", "Query transactions with filters, pagination, and sorting", null, null, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetTransactionsPagedAsync", "Query transactions with filters, pagination, and sorting", null, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
