using BLL.DTOs.Auth;
using BLL.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GUI.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class UsersModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly ILogger<UsersModel> _logger;

        public UsersModel(IAuthService authService, ILogger<UsersModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public List<AuthUserDto> UsersList { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            UsersList = await _authService.GetAllUsersAsync(cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnPostCreateUserAsync(string fullName, string email, short roleId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin để tạo tài khoản.";
                return RedirectToPage();
            }

            if (roleId == 1)
            {
                TempData["ErrorMessage"] = "Không được phép tạo tài khoản Admin qua giao diện này.";
                return RedirectToPage();
            }

            try
            {
                var created = await _authService.RegisterAsync(fullName, email, roleId, cancellationToken);
                TempData["SuccessMessage"] =
                    $"Đã tạo user {created.Email}. Email xác nhận đang được gửi tới hộp thư người dùng.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin failed to create user {Email}", email);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo tài khoản. Vui lòng thử lại.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostApproveAsync(Guid id, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier), out var adminUserId)) return Unauthorized();
            var success = await _authService.ApproveUserAsync(adminUserId, id, cancellationToken);
            if (success)
            {
                TempData["SuccessMessage"] = "Đã phê duyệt người dùng thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể tìm thấy người dùng.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectOrBlockAsync(Guid id, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier), out var adminUserId)) return Unauthorized();
            var success = await _authService.RejectOrBlockUserAsync(adminUserId, id, cancellationToken);
            if (success)
            {
                TempData["SuccessMessage"] = "Đã thực hiện thao tác thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể xử lý yêu cầu.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUnblockAsync(Guid id, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier), out var adminUserId)) return Unauthorized();
            var success = await _authService.UnblockUserAsync(adminUserId, id, cancellationToken);
            if (success)
            {
                TempData["SuccessMessage"] = "Đã mở khóa tài khoản người dùng thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể mở khóa tài khoản.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostImportExcelAsync(IFormFile excelFile, short roleId, CancellationToken cancellationToken)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn tệp Excel hợp lệ.";
                return RedirectToPage();
            }

            if (!excelFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Chỉ hỗ trợ tệp định dạng .xlsx.";
                return RedirectToPage();
            }

            try
            {
                using var stream = excelFile.OpenReadStream();
                var result = await _authService.BulkRegisterFromExcelAsync(stream, roleId, cancellationToken);
                
                if (result.Errors.Any())
                {
                    string errSummary = result.Errors.First();
                    if (result.Errors.Count > 1) 
                    {
                        errSummary += $" (và {result.Errors.Count - 1} lỗi khác)";
                    }
                    TempData["ErrorMessage"] = $"Đã tạo thành công {result.SuccessCount} tài khoản. Có lỗi: {errSummary}";
                }
                else
                {
                    TempData["SuccessMessage"] = $"Đã tạo thành công {result.SuccessCount} tài khoản.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin failed to import excel");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý tệp Excel.";
            }

            return RedirectToPage();
        }

        public IActionResult OnGetDownloadTemplate(short roleId)
        {
            string roleName = roleId == 3 ? "Sinh Viên" : "Giảng Viên";
            string idColumnName = roleId == 3 ? "Mã Số Sinh Viên (MSSV)" : "Mã Giảng Viên (MGV)";

            using var memoryStream = new MemoryStream();
            var workbook = new NPOI.XSSF.UserModel.XSSFWorkbook();
            var sheet = workbook.CreateSheet($"Danh sach {roleName}");

            var headerRow = sheet.CreateRow(0);
            headerRow.CreateCell(0).SetCellValue(idColumnName);
            headerRow.CreateCell(1).SetCellValue("Họ và Tên (*)");
            headerRow.CreateCell(2).SetCellValue("Email (*)");

            var sampleRow = sheet.CreateRow(1);
            sampleRow.CreateCell(0).SetCellValue(roleId == 3 ? "SE123456" : "GV12345");
            sampleRow.CreateCell(1).SetCellValue("Nguyễn Trần Gia Bảo");
            sampleRow.CreateCell(2).SetCellValue(roleId == 3 ? "baontgse123456@fpt.edu.vn" : "baontg@fe.edu.vn");

            sheet.SetColumnWidth(0, 6500); 
            sheet.SetColumnWidth(1, 8000); 
            sheet.SetColumnWidth(2, 8000); 

            workbook.Write(memoryStream);
            var content = memoryStream.ToArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Template_Import_{roleName}.xlsx");
        }

        public async Task<IActionResult> OnGetExportExcelAsync(short roleId, CancellationToken cancellationToken)
        {
            var users = await _authService.GetAllUsersAsync(cancellationToken);
            var filteredUsers = users.Where(u => u.RoleId == roleId && u.IsActive && !u.IsBlocked).ToList();

            string roleName = roleId == 3 ? "Sinh Viên" : "Giảng Viên";
            
            using var memoryStream = new MemoryStream();
            var workbook = new NPOI.XSSF.UserModel.XSSFWorkbook();
            var sheet = workbook.CreateSheet($"Danh Sach {roleName}");

            var headerRow = sheet.CreateRow(0);
            headerRow.CreateCell(0).SetCellValue("STT");
            headerRow.CreateCell(1).SetCellValue("Họ và Tên");
            headerRow.CreateCell(2).SetCellValue("Email");
            headerRow.CreateCell(3).SetCellValue("Trạng Thái");

            int rowIndex = 1;
            foreach (var user in filteredUsers)
            {
                var row = sheet.CreateRow(rowIndex);
                row.CreateCell(0).SetCellValue(rowIndex);
                row.CreateCell(1).SetCellValue(user.FullName);
                row.CreateCell(2).SetCellValue(user.Email);
                row.CreateCell(3).SetCellValue("Đang hoạt động");
                rowIndex++;
            }

            sheet.SetColumnWidth(0, 2000);
            sheet.SetColumnWidth(1, 8000);
            sheet.SetColumnWidth(2, 10000);
            sheet.SetColumnWidth(3, 5000);

            workbook.Write(memoryStream);
            var content = memoryStream.ToArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Danh_Sach_{roleName}.xlsx");
        }

        public async Task<IActionResult> OnGetExportPdfAsync(short roleId, CancellationToken cancellationToken)
        {
            var users = await _authService.GetAllUsersAsync(cancellationToken);
            var filteredUsers = users.Where(u => u.RoleId == roleId && u.IsActive && !u.IsBlocked).ToList();

            string roleName = roleId == 3 ? "Sinh Viên" : "Giảng Viên";

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            
            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4);
                    page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                    page.PageColor(QuestPDF.Helpers.Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(compose => 
                    {
                        compose.Text($"Danh Sách {roleName}")
                            .SemiBold().FontSize(20).FontColor(QuestPDF.Helpers.Colors.Blue.Darken2);
                    });

                    page.Content().PaddingVertical(1, QuestPDF.Infrastructure.Unit.Centimetre).Column(col => 
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(40);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(3);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("STT").SemiBold();
                                header.Cell().Text("Họ và Tên").SemiBold();
                                header.Cell().Text("Email").SemiBold();
                            });

                            int index = 1;
                            foreach (var user in filteredUsers)
                            {
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).PaddingVertical(5).Text(index.ToString());
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).PaddingVertical(5).Text(user.FullName);
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).PaddingVertical(5).Text(user.Email);
                                index++;
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Trang ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            using var ms = new MemoryStream();
            document.GeneratePdf(ms);
            return File(ms.ToArray(), "application/pdf", $"Danh_Sach_{roleName}.pdf");
        }
    }
}
