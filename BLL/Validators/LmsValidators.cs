using FluentValidation;
using Models;

namespace BLL.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.TenDangNhap)
                .NotEmpty().WithMessage("Tên đăng nhập không được để trống.")
                .MaximumLength(50).WithMessage("Tên đăng nhập không được vượt quá 50 ký tự.");

            RuleFor(x => x.MatKhau)
                .NotEmpty().WithMessage("Mật khẩu không được để trống.");
        }
    }

    public class RegisterStudentRequestValidator : AbstractValidator<RegisterStudentRequest>
    {
        public RegisterStudentRequestValidator()
        {
            RuleFor(x => x.TenDangNhap)
                .NotEmpty().WithMessage("Tên đăng nhập không được để trống.")
                .Length(3, 50).WithMessage("Tên đăng nhập phải từ 3 đến 50 ký tự.");

            RuleFor(x => x.MatKhau)
                .NotEmpty().WithMessage("Mật khẩu không được để trống.")
                .MinimumLength(6).WithMessage("Mật khẩu phải có ít nhất 6 ký tự.");

            RuleFor(x => x.HoTen)
                .NotEmpty().WithMessage("Họ tên không được để trống.")
                .MaximumLength(100).WithMessage("Họ tên không được vượt quá 100 ký tự.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email không được để trống.")
                .EmailAddress().WithMessage("Email không hợp lệ.")
                .MaximumLength(100).WithMessage("Email không được vượt quá 100 ký tự.");
        }
    }

    public class KhoaHocModelValidator : AbstractValidator<KhoaHocModel>
    {
        public KhoaHocModelValidator()
        {
            RuleFor(x => x.TenKhoaHoc)
                .NotEmpty().WithMessage("Tên khóa học không được để trống.")
                .MaximumLength(200).WithMessage("Tên khóa học không được vượt quá 200 ký tự.");

            RuleFor(x => x.MaDanhMuc)
                .GreaterThan(0).WithMessage("Danh mục không hợp lệ.");

            RuleFor(x => x.MaGiangVien)
                .GreaterThan(0).WithMessage("Giảng viên không hợp lệ.");

            RuleFor(x => x.Gia)
                .GreaterThanOrEqualTo(0).WithMessage("Giá khóa học không thể âm.");
        }
    }
}
