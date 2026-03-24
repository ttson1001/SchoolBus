using BE_API.Configuration;
using BE_API.Dto.Wallet;
using BE_API.Entites;
using BE_API.Entites.Enums;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using PayOS;
using PayOS.Models;
using PayOS.Models.Webhooks;
using PayOS.Models.V2.PaymentRequests;

namespace BE_API.Service
{
    public class WalletService : IWalletService
    {
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Wallet> _walletRepo;
        private readonly IRepository<TransactionLog> _transactionLogRepo;
        private readonly PayOSClient _payOsClient;
        private readonly PayOsSettings _payOsSettings;

        public WalletService(
            IRepository<User> userRepo,
            IRepository<Wallet> walletRepo,
            IRepository<TransactionLog> transactionLogRepo,
            PayOSClient payOsClient,
            IOptions<PayOsSettings> payOsOptions)
        {
            _userRepo = userRepo;
            _walletRepo = walletRepo;
            _transactionLogRepo = transactionLogRepo;
            _payOsClient = payOsClient;
            _payOsSettings = payOsOptions.Value;
        }

        public async Task<WalletDto> TopUpAsync(WalletTopUpDto dto)
        {
            if (dto.UserId <= 0)
                throw new Exception("UserId phai lon hon 0");

            if (dto.Amount <= 0)
                throw new Exception("So tien nap phai lon hon 0");

            var guardian = await ValidateGuardianAsync(dto.UserId);
            var wallet = await FindOrCreateWalletAsync(guardian.Id);

            wallet.Balance += dto.Amount;
            await _walletRepo.SaveChangesAsync();

            return MapToDto(wallet, guardian);
        }

        public async Task<WalletDto> GetWalletByUserIdAsync(long userId)
        {
            if (userId <= 0)
                throw new Exception("UserId phai lon hon 0");

            var guardian = await ValidateGuardianAsync(userId);
            var wallet = await FindOrCreateWalletAsync(guardian.Id);

            await _walletRepo.SaveChangesAsync();
            return MapToDto(wallet, guardian);
        }

        public async Task<WalletPayOsLinkDto> CreatePayOsTopUpLinkAsync(WalletPayOsCreateDto dto)
        {
            if (dto.UserId <= 0)
                throw new Exception("UserId phai lon hon 0");

            if (dto.Amount <= 0)
                throw new Exception("So tien nap phai lon hon 0");

            if (decimal.Truncate(dto.Amount) != dto.Amount)
                throw new Exception("So tien nap qua payOS phai la so nguyen VND");

            var guardian = await ValidateGuardianAsync(dto.UserId);
            var returnUrl = ResolveUrl(dto.ReturnUrl, _payOsSettings.ReturnUrl, "ReturnUrl");
            var cancelUrl = ResolveUrl(dto.CancelUrl, _payOsSettings.CancelUrl, "CancelUrl");
            var orderCode = await GenerateOrderCodeAsync();
            var description = BuildDescription(guardian.Id);
            var amount = decimal.ToInt64(dto.Amount);
            var wallet = await FindOrCreateWalletAsync(guardian.Id);
            var createdAt = DateTime.UtcNow;

            var paymentRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = amount,
                Description = description,
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl
            };

            var paymentLink = await _payOsClient.PaymentRequests.CreateAsync(paymentRequest);

            var transactionLog = new TransactionLog
            {
                Amount = dto.Amount,
                Method = "PAYOS",
                Status = WalletTopUpStatus.PENDING.ToString(),
                OldBalance = wallet.Balance,
                NewBalance = wallet.Balance,
                Sender = guardian.Email,
                Receiver = "WALLET",
                Description = description,
                Code = orderCode.ToString()
            };

            await _transactionLogRepo.AddAsync(transactionLog);
            await _transactionLogRepo.SaveChangesAsync();

            return new WalletPayOsLinkDto
            {
                UserId = guardian.Id,
                OrderCode = orderCode,
                Amount = dto.Amount,
                Description = description,
                CheckoutUrl = paymentLink.CheckoutUrl,
                Status = WalletTopUpStatus.PENDING.ToString(),
                CreatedAt = createdAt
            };
        }

        public async Task<WalletPayOsWebhookResultDto> HandlePayOsWebhookAsync(Webhook webhook)
        {
            var verifiedData = await _payOsClient.Webhooks.VerifyAsync(webhook);

            var transactionLog = await _transactionLogRepo.Get()
                .FirstOrDefaultAsync(x => x.Method == "PAYOS" && x.Code == verifiedData.OrderCode.ToString())
                ?? throw new Exception("Khong tim thay giao dich nap tien payOS");

            var userId = await GetUserIdFromPayOsTransactionAsync(transactionLog);
            var wallet = await FindOrCreateWalletAsync(userId);

            if (string.Equals(transactionLog.Status, WalletTopUpStatus.PAID.ToString(), StringComparison.OrdinalIgnoreCase))
                return MapToWebhookResultDto(userId, verifiedData.OrderCode, transactionLog, wallet.Balance);

            if (!string.Equals(verifiedData.Code, "00", StringComparison.OrdinalIgnoreCase))
            {
                transactionLog.Status = WalletTopUpStatus.FAILED.ToString();
                transactionLog.PaidAt = ParseTransactionDateTime(verifiedData.TransactionDateTime);
                _transactionLogRepo.Update(transactionLog);
                await _transactionLogRepo.SaveChangesAsync();
                return MapToWebhookResultDto(userId, verifiedData.OrderCode, transactionLog, wallet.Balance);
            }

            if (verifiedData.Amount != decimal.ToInt64(transactionLog.Amount))
                throw new Exception("So tien thanh toan khong khop voi giao dich nap tien");

            var oldBalance = wallet.Balance;
            wallet.Balance += transactionLog.Amount;
            transactionLog.Status = WalletTopUpStatus.PAID.ToString();
            transactionLog.PaidAt = ParseTransactionDateTime(verifiedData.TransactionDateTime);
            transactionLog.OldBalance = oldBalance;
            transactionLog.NewBalance = wallet.Balance;

            _walletRepo.Update(wallet);
            _transactionLogRepo.Update(transactionLog);
            await _transactionLogRepo.SaveChangesAsync();

            return MapToWebhookResultDto(userId, verifiedData.OrderCode, transactionLog, wallet.Balance);
        }

        public async Task<WalletPayOsWebhookResultDto> GetPayOsTopUpStatusAsync(long orderCode)
        {
            if (orderCode <= 0)
                throw new Exception("OrderCode phai lon hon 0");

            var transactionLog = await _transactionLogRepo.Get()
                .FirstOrDefaultAsync(x => x.Method == "PAYOS" && x.Code == orderCode.ToString())
                ?? throw new Exception("Khong tim thay giao dich nap tien payOS");

            var userId = await GetUserIdFromPayOsTransactionAsync(transactionLog);
            var wallet = await FindOrCreateWalletAsync(userId);
            await _walletRepo.SaveChangesAsync();

            return MapToWebhookResultDto(userId, orderCode, transactionLog, wallet.Balance);
        }

        private async Task<User> ValidateGuardianAsync(long userId)
        {
            var user = await _userRepo.Get()
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == userId)
                ?? throw new Exception("Guardian khong ton tai");

            if (!string.Equals(user.Role.Name, "guardian", StringComparison.OrdinalIgnoreCase))
                throw new Exception("User duoc chon khong phai guardian");

            if (user.Status != AccountStatus.ACTIVE)
                throw new Exception("Guardian dang khong hoat dong");

            return user;
        }

        private async Task<Wallet> FindOrCreateWalletAsync(long userId)
        {
            var wallet = await _walletRepo.Get()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (wallet != null)
                return wallet;

            wallet = new Wallet
            {
                UserId = userId,
                Balance = 0
            };

            await _walletRepo.AddAsync(wallet);
            return wallet;
        }

        private async Task<long> GenerateOrderCodeAsync()
        {
            var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            while (await _transactionLogRepo.Get().AnyAsync(x => x.Method == "PAYOS" && x.Code == orderCode.ToString()))
            {
                orderCode++;
            }

            return orderCode;
        }

        private static string ResolveUrl(string? inputUrl, string configuredUrl, string fieldName)
        {
            var url = string.IsNullOrWhiteSpace(inputUrl) ? configuredUrl : inputUrl.Trim();

            if (string.IsNullOrWhiteSpace(url))
                throw new Exception($"{fieldName} cua payOS chua duoc cau hinh");

            return url;
        }

        private static string BuildDescription(long userId)
        {
            return $"Nap vi GD{userId}";
        }

        private async Task<long> GetUserIdFromPayOsTransactionAsync(TransactionLog transactionLog)
        {
            if (!string.IsNullOrWhiteSpace(transactionLog.Description))
            {
                const string prefix = "Nap vi GD";

                if (transactionLog.Description.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    long.TryParse(transactionLog.Description[prefix.Length..], out var parsedUserId))
                {
                    return parsedUserId;
                }
            }

            if (!string.IsNullOrWhiteSpace(transactionLog.Sender))
            {
                var userId = await _userRepo.Get()
                    .Where(x => x.Email == transactionLog.Sender)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync();

                if (userId > 0)
                    return userId;
            }

            throw new Exception("Khong xac dinh duoc guardian cua giao dich payOS");
        }

        private static DateTime ParseTransactionDateTime(string? transactionDateTime)
        {
            if (DateTime.TryParse(transactionDateTime, out var parsedDateTime))
                return parsedDateTime;

            return DateTime.UtcNow;
        }

        private static WalletDto MapToDto(Wallet wallet, User user)
        {
            return new WalletDto
            {
                Id = wallet.Id,
                UserId = user.Id,
                UserName = user.FullName ?? string.Empty,
                Email = user.Email,
                Balance = wallet.Balance
            };
        }

        private static WalletPayOsWebhookResultDto MapToWebhookResultDto(
            long userId,
            long orderCode,
            TransactionLog transactionLog,
            decimal walletBalance)
        {
            return new WalletPayOsWebhookResultDto
            {
                UserId = userId,
                OrderCode = orderCode,
                Amount = transactionLog.Amount,
                Status = transactionLog.Status,
                PaidAt = transactionLog.PaidAt,
                WalletBalance = walletBalance
            };
        }
    }
}
