using BE_API.Dto.Common;
using BE_API.Dto.TransactionLog;
using BE_API.Entites;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Service
{
    public class TransactionLogService : ITransactionLogService
    {
        private readonly IRepository<TransactionLog> _transactionLogRepo;
        private readonly IRepository<Order> _orderRepo;

        public TransactionLogService(
            IRepository<TransactionLog> transactionLogRepo,
            IRepository<Order> orderRepo)
        {
            _transactionLogRepo = transactionLogRepo;
            _orderRepo = orderRepo;
        }

        public async Task<PagedResult<TransactionLogDto>> SearchAsync(
            string? keyword,
            string? method,
            string? status,
            long? orderId,
            string? code,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize)
        {
            var query = GetQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(x =>
                    (x.Sender != null && x.Sender.ToLower().Contains(keyword)) ||
                    (x.Receiver != null && x.Receiver.ToLower().Contains(keyword)) ||
                    (x.Description != null && x.Description.ToLower().Contains(keyword)) ||
                    (x.Code != null && x.Code.ToLower().Contains(keyword)) ||
                    x.Method.ToLower().Contains(keyword) ||
                    x.Status.ToLower().Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(method))
            {
                method = method.Trim().ToUpper();
                query = query.Where(x => x.Method.ToUpper() == method);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                status = status.Trim().ToUpper();
                query = query.Where(x => x.Status.ToUpper() == status);
            }

            if (orderId.HasValue)
                query = query.Where(x => x.OrderId == orderId.Value);

            if (!string.IsNullOrWhiteSpace(code))
            {
                code = code.Trim().ToLower();
                query = query.Where(x => x.Code != null && x.Code.ToLower().Contains(code));
            }

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(x => x.PaidAt.HasValue && x.PaidAt.Value.Date >= from);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date;
                query = query.Where(x => x.PaidAt.HasValue && x.PaidAt.Value.Date <= to);
            }

            var totalItems = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.PaidAt ?? DateTime.MinValue)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<TransactionLogDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<TransactionLogDto> GetByIdAsync(long id)
        {
            var transactionLog = await GetQueryable()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("TransactionLog khAng tan tai");

            return MapToDto(transactionLog);
        }

        public async Task<TransactionLogDto> CreateAsync(TransactionLogCreateDto dto)
        {
            if (dto.OrderId.HasValue)
                await ValidateOrderAsync(dto.OrderId.Value);

            var transactionLog = new TransactionLog
            {
                OrderId = dto.OrderId,
                Method = NormalizeRequired(dto.Method, "Method"),
                Amount = dto.Amount,
                Status = NormalizeRequired(dto.Status, "Status"),
                PaidAt = dto.PaidAt,
                OldBalance = dto.OldBalance,
                NewBalance = dto.NewBalance,
                Sender = NormalizeOptional(dto.Sender),
                Receiver = NormalizeOptional(dto.Receiver),
                Description = NormalizeOptional(dto.Description),
                Code = NormalizeOptional(dto.Code)
            };

            ValidateTransactionLog(transactionLog);

            await EnsureCodeNotDuplicatedAsync(transactionLog.Code, null);

            await _transactionLogRepo.AddAsync(transactionLog);
            await _transactionLogRepo.SaveChangesAsync();

            return await GetByIdAsync(transactionLog.Id);
        }

        public async Task<TransactionLogDto> UpdateAsync(long id, TransactionLogUpdateDto dto)
        {
            var transactionLog = await _transactionLogRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("TransactionLog khAng tan tai");

            if (dto.OrderId.HasValue)
            {
                await ValidateOrderAsync(dto.OrderId.Value);
                transactionLog.OrderId = dto.OrderId.Value;
            }

            if (dto.Method != null)
                transactionLog.Method = NormalizeRequired(dto.Method, "Method");

            if (dto.Amount.HasValue)
                transactionLog.Amount = dto.Amount.Value;

            if (dto.Status != null)
                transactionLog.Status = NormalizeRequired(dto.Status, "Status");

            if (dto.PaidAt.HasValue)
                transactionLog.PaidAt = dto.PaidAt.Value;

            if (dto.OldBalance.HasValue)
                transactionLog.OldBalance = dto.OldBalance.Value;

            if (dto.NewBalance.HasValue)
                transactionLog.NewBalance = dto.NewBalance.Value;

            if (dto.Sender != null)
                transactionLog.Sender = NormalizeOptional(dto.Sender);

            if (dto.Receiver != null)
                transactionLog.Receiver = NormalizeOptional(dto.Receiver);

            if (dto.Description != null)
                transactionLog.Description = NormalizeOptional(dto.Description);

            if (dto.Code != null)
                transactionLog.Code = NormalizeOptional(dto.Code);

            ValidateTransactionLog(transactionLog);
            await EnsureCodeNotDuplicatedAsync(transactionLog.Code, id);

            _transactionLogRepo.Update(transactionLog);
            await _transactionLogRepo.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        public async Task DeleteAsync(long id)
        {
            var transactionLog = await _transactionLogRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("TransactionLog khAng tan tai");

            _transactionLogRepo.Delete(transactionLog);
            await _transactionLogRepo.SaveChangesAsync();
        }

        private IQueryable<TransactionLog> GetQueryable()
        {
            return _transactionLogRepo.Get()
                .Include(x => x.Order);
        }

        private async Task ValidateOrderAsync(long orderId)
        {
            if (orderId <= 0)
                throw new Exception("OrderId phai lan hn 0");

            var exists = await _orderRepo.Get().AnyAsync(x => x.Id == orderId);

            if (!exists)
                throw new Exception("Order khAng tan tai");
        }

        private async Task EnsureCodeNotDuplicatedAsync(string? code, long? excludedId)
        {
            if (string.IsNullOrWhiteSpace(code))
                return;

            var exists = await _transactionLogRepo.Get()
                .AnyAsync(x =>
                    x.Code != null &&
                    x.Code.ToLower() == code.ToLower() &&
                    (!excludedId.HasValue || x.Id != excludedId.Value));

            if (exists)
                throw new Exception("Code giao dach AA tan tai");
        }

        private static void ValidateTransactionLog(TransactionLog transactionLog)
        {
            if (transactionLog.Amount <= 0)
                throw new Exception("Amount phai lan hn 0");

            if (string.IsNullOrWhiteSpace(transactionLog.Method))
                throw new Exception("Method khAng Aac Aa trang");

            if (string.IsNullOrWhiteSpace(transactionLog.Status))
                throw new Exception("Status khAng Aac Aa trang");
        }

        private static string NormalizeRequired(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new Exception($"{fieldName} khAng Aac Aa trang");

            return value.Trim();
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static TransactionLogDto MapToDto(TransactionLog transactionLog)
        {
            return new TransactionLogDto
            {
                Id = transactionLog.Id,
                OrderId = transactionLog.OrderId,
                OrderCode = transactionLog.OrderId?.ToString(),
                Method = transactionLog.Method,
                Amount = transactionLog.Amount,
                Status = transactionLog.Status,
                PaidAt = transactionLog.PaidAt,
                OldBalance = transactionLog.OldBalance,
                NewBalance = transactionLog.NewBalance,
                Sender = transactionLog.Sender,
                Receiver = transactionLog.Receiver,
                Description = transactionLog.Description,
                Code = transactionLog.Code
            };
        }
    }
}
