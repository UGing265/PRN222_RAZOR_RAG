using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BLL.DTOs.Tokens;
using BLL.Interfaces.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace GUI.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class TokenUsageModel : PageModel
    {
        private readonly ITokenUsageService _tokenUsageService;
        private readonly ILogger<TokenUsageModel> _logger;

        public TokenUsageModel(ITokenUsageService tokenUsageService, ILogger<TokenUsageModel> logger)
        {
            _tokenUsageService = tokenUsageService;
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

        public async Task OnGetAsync()
        {
            _logger.LogInformation("Admin loading TokenUsage page with FilterRole={FilterRole}, SearchTerm={SearchTerm}, SortBy={SortBy}", FilterRole, SearchTerm, SortBy);

            // Kết nối dữ liệu thật từ BLL / Repository
            var (users, stats) = await _tokenUsageService.GetTokenUsageReportAsync(200000, HttpContext.RequestAborted);
            UserTokensList = users;
            HeroStats = stats;

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
    }
}
