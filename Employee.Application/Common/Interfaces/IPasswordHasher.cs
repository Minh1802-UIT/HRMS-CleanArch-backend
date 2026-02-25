namespace Employee.Application.Common.Interfaces
{
    public interface IPasswordHasher
    {
        // Hàm băm mật khẩu (dùng khi Đăng ký)
        string Hash(string password);

        bool Verify(string password, string hashedPassword);
    }
}