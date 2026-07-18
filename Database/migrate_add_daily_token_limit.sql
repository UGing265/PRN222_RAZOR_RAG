-- Thêm cấu hình DailyTokenLimit vào bảng system_settings
INSERT INTO public.system_settings (key, value, description) 
VALUES 
('DailyTokenLimit_Student', '0', 'Giới hạn số token tối đa 1 Sinh viên được sử dụng trong 1 ngày (bao gồm cả Chat và Chia chương). 0 = Không giới hạn.'),
('DailyTokenLimit_Lecturer', '0', 'Giới hạn số token tối đa 1 Giảng viên được sử dụng trong 1 ngày (bao gồm cả Chat và Chia chương). 0 = Không giới hạn.')
ON CONFLICT (key) DO NOTHING;
