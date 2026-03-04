using Employee.Domain.Entities.Common;

namespace Employee.Domain.Entities.Payroll
{
  /// <summary>
  /// Đại diện một ngày nghỉ lễ/tết hưởng lương.
  /// Được dùng để loại trừ khỏi tổng ngày công chuẩn khi tính lương.
  /// </summary>
  public class PublicHoliday : BaseEntity
  {
    /// <summary>Ngày nghỉ lễ (UTC, chỉ dùng phần Date).</summary>
    public DateTime Date { get; private set; }

    /// <summary>Tên ngày lễ, VD: "Giỗ Tổ Hùng Vương", "Quốc khánh".</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Nếu true thì ngày lễ này lặp lại hàng năm theo tháng/ngày (Âm lịch cần tính riêng).
    /// Nếu false thì chỉ áp dụng đúng năm dương lịch của <see cref="Date"/>.
    /// </summary>
    public bool IsRecurringYearly { get; private set; }

    /// <summary>Ghi chú tùy chọn, VD: "Bù sang thứ 2 do trùng cuối tuần".</summary>
    public string? Note { get; private set; }

    // Parameterless constructor for MongoDB deserialization
    private PublicHoliday() { }

    public PublicHoliday(DateTime date, string name, bool isRecurringYearly = false, string? note = null)
    {
      if (string.IsNullOrWhiteSpace(name))
        throw new ArgumentException("Holiday name is required.", nameof(name));

      Date = date.Date; // Normalize to date-only (midnight UTC)
      Name = name.Trim();
      IsRecurringYearly = isRecurringYearly;
      Note = note?.Trim();
      CreatedAt = DateTime.UtcNow;
    }

    public void Update(DateTime date, string name, bool isRecurringYearly, string? note)
    {
      if (string.IsNullOrWhiteSpace(name))
        throw new ArgumentException("Holiday name is required.", nameof(name));

      Date = date.Date;
      Name = name.Trim();
      IsRecurringYearly = isRecurringYearly;
      Note = note?.Trim();
    }
  }
}
