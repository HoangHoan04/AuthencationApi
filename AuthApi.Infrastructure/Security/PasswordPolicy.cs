using System.Text.RegularExpressions;
using AuthApi.Application.Common.Interfaces;

namespace AuthApi.Infrastructure.Security;

public class PasswordPolicy : IPasswordPolicy
{
    public const int MinLength = 12;
    public const int HistoryCount = 5;

    public void Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinLength)
        {
            throw new InvalidOperationException($"Mật khẩu phải có ít nhất {MinLength} ký tự.");
        }

        if (!Regex.IsMatch(password, "[A-Z]"))
        {
            throw new InvalidOperationException("Mật khẩu phải có ít nhất một chữ hoa.");
        }

        if (!Regex.IsMatch(password, "[a-z]"))
        {
            throw new InvalidOperationException("Mật khẩu phải có ít nhất một chữ thường.");
        }

        if (!Regex.IsMatch(password, "[0-9]"))
        {
            throw new InvalidOperationException("Mật khẩu phải có ít nhất một chữ số.");
        }

        if (!Regex.IsMatch(password, @"[^A-Za-z0-9]"))
        {
            throw new InvalidOperationException("Mật khẩu phải có ít nhất một ký tự đặc biệt.");
        }
    }
}
