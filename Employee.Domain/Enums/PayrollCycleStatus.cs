namespace Employee.Domain.Enums
{
  public enum PayrollCycleStatus
  {
    /// <summary>Chu kỳ đã được tạo, chưa có bảng lương nào được tính.</summary>
    Open = 0,

    /// <summary>Bảng lương đang được tính (hoặc đã tính xong nhưng chưa duyệt hết).</summary>
    Processing = 1,

    /// <summary>Tất cả bảng lương trong chu kỳ đã được duyệt và chốt.</summary>
    Closed = 2,

    /// <summary>Chu kỳ đã bị hủy (dùng trong trường hợp nhập sai tháng/năm).</summary>
    Cancelled = 3
  }
}
