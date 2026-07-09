using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GUI.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class TokenUsageModel : PageModel
    {
        private readonly ILogger<TokenUsageModel> _logger;

        public TokenUsageModel(ILogger<TokenUsageModel> logger)
        {
            _logger = logger;
        }

        public HeroStatsDto HeroStats { get; set; } = new();
        public List<UserTokenUsageDto> UserTokensList { get; set; } = new();
        public List<UserTokenUsageDto> FilteredList { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string FilterRole { get; set; } = "all"; // all, lecturer, student

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "highest"; // highest, lowest, name

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 6;
        public int TotalPages { get; set; } = 1;
        public int TotalUsersCount { get; set; } = 0;

        public void OnGet()
        {
            _logger.LogInformation("Admin loading TokenUsage page with FilterRole={FilterRole}, SearchTerm={SearchTerm}, SortBy={SortBy}", FilterRole, SearchTerm, SortBy);

            // Generate realistic mock data for UI verification
            GenerateMockData();

            // Filter by Role
            var query = UserTokensList.AsEnumerable();
            if (string.Equals(FilterRole, "lecturer", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(u => u.RoleId == 2);
            }
            else if (string.Equals(FilterRole, "student", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(u => u.RoleId == 3);
            }

            // Filter by SearchTerm
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
            }

            // Sort
            query = SortBy.ToLower() switch
            {
                "lowest" => query.OrderBy(u => u.TotalTokens),
                "name" => query.OrderBy(u => u.FullName),
                _ => query.OrderByDescending(u => u.TotalTokens)
            };

            var allFiltered = query.ToList();
            TotalUsersCount = allFiltered.Count;
            TotalPages = (int)Math.Ceiling(TotalUsersCount / (double)PageSize);
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

            FilteredList = allFiltered.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();
        }

        private void GenerateMockData()
        {
            var dateLabels = new List<string> { "03/07", "04/07", "05/07", "06/07", "07/07", "08/07", "09/07" };

            var users = new List<UserTokenUsageDto>
            {
                new()
                {
                    UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    FullName = "TS. Nguyễn Trần Gia Bảo",
                    Email = "baontg@fe.edu.vn",
                    RoleId = 2,
                    RoleName = "Giảng Viên",
                    ChatTokens = 28400,
                    DocTokens = 16800,
                    SparklineData = new List<int> { 4200, 5800, 6100, 7200, 6500, 8100, 7300 },
                    ChatHistoryData = new List<int> { 2500, 3500, 3800, 4500, 4000, 5200, 4900 },
                    DocHistoryData = new List<int> { 1700, 2300, 2300, 2700, 2500, 2900, 2400 },
                    DateLabels = dateLabels
                },
                new()
                {
                    UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    FullName = "PGS.TS. Lê Quốc Cota",
                    Email = "cotalq@fe.edu.vn",
                    RoleId = 2,
                    RoleName = "Giảng Viên",
                    ChatTokens = 21500,
                    DocTokens = 14200,
                    SparklineData = new List<int> { 3100, 4200, 4900, 5800, 5100, 6400, 6200 },
                    ChatHistoryData = new List<int> { 1900, 2600, 3000, 3600, 3100, 3900, 3400 },
                    DocHistoryData = new List<int> { 1200, 1600, 1900, 2200, 2000, 2500, 2800 },
                    DateLabels = dateLabels
                },
                new()
                {
                    UserId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    FullName = "Trần Minh Khang (SE170123)",
                    Email = "khangtmse170123@fpt.edu.vn",
                    RoleId = 3,
                    RoleName = "Sinh Viên",
                    ChatTokens = 12400,
                    DocTokens = 6800,
                    SparklineData = new List<int> { 1800, 2200, 2900, 3400, 2800, 3200, 2900 },
                    ChatHistoryData = new List<int> { 1200, 1400, 1900, 2200, 1800, 2000, 1900 },
                    DocHistoryData = new List<int> { 600, 800, 1000, 1200, 1000, 1200, 1000 },
                    DateLabels = dateLabels
                },
                new()
                {
                    UserId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    FullName = "ThS. Phạm Hoàng Thu Huyền",
                    Email = "huyenpht@fe.edu.vn",
                    RoleId = 2,
                    RoleName = "Giảng Viên",
                    ChatTokens = 8900,
                    DocTokens = 5400,
                    SparklineData = new List<int> { 1500, 1900, 2100, 2300, 2100, 2200, 2200 },
                    ChatHistoryData = new List<int> { 900, 1200, 1300, 1400, 1300, 1400, 1400 },
                    DocHistoryData = new List<int> { 600, 700, 800, 900, 800, 800, 800 },
                    DateLabels = dateLabels
                },
                new()
                {
                    UserId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    FullName = "Lý Hữu Phúc (SE170456)",
                    Email = "phuclhse170456@fpt.edu.vn",
                    RoleId = 3,
                    RoleName = "Sinh Viên",
                    ChatTokens = 5200,
                    DocTokens = 2100,
                    SparklineData = new List<int> { 800, 950, 1100, 1250, 1050, 1150, 1000 },
                    ChatHistoryData = new List<int> { 600, 700, 800, 900, 750, 800, 650 },
                    DocHistoryData = new List<int> { 200, 250, 300, 350, 300, 350, 350 },
                    DateLabels = dateLabels
                },
                new()
                {
                    UserId = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    FullName = "Đặng Thảo Vy (SE170789)",
                    Email = "vydtse170789@fpt.edu.vn",
                    RoleId = 3,
                    RoleName = "Sinh Viên",
                    ChatTokens = 4100,
                    DocTokens = 1800,
                    SparklineData = new List<int> { 600, 750, 850, 950, 900, 950, 900 },
                    ChatHistoryData = new List<int> { 400, 500, 600, 650, 650, 650, 650 },
                    DocHistoryData = new List<int> { 200, 250, 250, 300, 250, 300, 250 },
                    DateLabels = dateLabels
                },
                new()
                {
                    UserId = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                    FullName = "Hoàng Tuấn Anh (SE170999)",
                    Email = "anhhtse170999@fpt.edu.vn",
                    RoleId = 3,
                    RoleName = "Sinh Viên",
                    ChatTokens = 3800,
                    DocTokens = 1200,
                    SparklineData = new List<int> { 500, 600, 750, 800, 750, 800, 800 },
                    ChatHistoryData = new List<int> { 350, 450, 550, 600, 550, 650, 650 },
                    DocHistoryData = new List<int> { 150, 150, 200, 200, 200, 150, 150 },
                    DateLabels = dateLabels
                },
                new()
                {
                    UserId = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                    FullName = "TS. Bùi Anh Tuấn",
                    Email = "tuanba@fe.edu.vn",
                    RoleId = 2,
                    RoleName = "Giảng Viên",
                    ChatTokens = 7100,
                    DocTokens = 4800,
                    SparklineData = new List<int> { 1200, 1500, 1700, 1900, 1800, 1900, 1900 },
                    ChatHistoryData = new List<int> { 700, 900, 1000, 1100, 1100, 1150, 1150 },
                    DocHistoryData = new List<int> { 500, 600, 700, 800, 700, 750, 750 },
                    DateLabels = dateLabels
                }
            };

            // Calculate percentage of quota for each user (against 200,000)
            foreach (var u in users)
            {
                u.PercentOfQuota = Math.Round((double)u.TotalTokens / 200000 * 100, 1);
            }

            UserTokensList = users;

            HeroStats = new HeroStatsDto
            {
                TotalUsedTokens = users.Sum(u => u.TotalTokens),
                TotalQuotaTokens = 200000,
                TotalChatTokens = users.Sum(u => u.ChatTokens),
                TotalDocTokens = users.Sum(u => u.DocTokens),
                TopConsumer = users.OrderByDescending(u => u.TotalTokens).First(),
                DailyAvgTokens = (int)Math.Round(users.Sum(u => u.TotalTokens) / 7.0),
                WeeklyApiRequests = 1420
            };
        }
    }

    public class UserTokenUsageDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public short RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public long ChatTokens { get; set; }
        public long DocTokens { get; set; }
        public long TotalTokens => ChatTokens + DocTokens;
        public double PercentOfQuota { get; set; }
        public List<int> SparklineData { get; set; } = new();
        public List<int> ChatHistoryData { get; set; } = new();
        public List<int> DocHistoryData { get; set; } = new();
        public List<string> DateLabels { get; set; } = new();

        public string Initial => string.IsNullOrWhiteSpace(FullName) ? "U" : FullName.Trim().Substring(0, 1).ToUpper();
    }

    public class HeroStatsDto
    {
        public long TotalUsedTokens { get; set; }
        public long TotalQuotaTokens { get; set; }
        public double OverallPercent => Math.Round((double)TotalUsedTokens / TotalQuotaTokens * 100, 1);
        public long TotalChatTokens { get; set; }
        public long TotalDocTokens { get; set; }
        public UserTokenUsageDto? TopConsumer { get; set; }
        public int DailyAvgTokens { get; set; }
        public int WeeklyApiRequests { get; set; }
    }
}
