"""
Seed script: Vietnamese public holidays 2026 + new payroll cycle settings.

Chạy với:
  .venv/Scripts/python.exe scripts/seed_public_holidays_2026.py

Tài liệu tham khảo:
  - Bộ luật Lao động 2019, Điều 112 (11 ngày lễ chính thức)
  - Quyết định nghỉ Tết của Thủ tướng hàng năm
  QUAN TRỌNG: Các ngày nghỉ Tết cụ thể (Âm lịch) cần cập nhật theo
  thông báo chính thức của Chính phủ mỗi năm.
"""

from pymongo import MongoClient
from datetime import datetime, timezone
from bson import ObjectId

# ── Config ────────────────────────────────────────────────────────────────────
MONGO_URI = "mongodb+srv://nguyenvanminh180220_db_user:Pass-180220@cluster0.kfpckor.mongodb.net/"
DB_NAME   = "EmployeeCleanDB"

client = MongoClient(MONGO_URI)
db = client[DB_NAME]

now = datetime.now(timezone.utc)

def make_utc(year, month, day):
    """Tạo datetime UTC (midnight) cho một ngày dương lịch."""
    return datetime(year, month, day, 0, 0, 0, tzinfo=timezone.utc)

# ── Danh sách ngày lễ 2026 ────────────────────────────────────────────────────
# IsRecurringYearly=True  → ngày này lặp lại hàng năm theo dương lịch (Điều 112).
# IsRecurringYearly=False → chỉ năm 2026, cần seed riêng cho năm khác.

HOLIDAYS_2026 = [
    # ── Tết Dương lịch (1/1 – cố định hàng năm) ─────────────────────────────
    {
        "Date": make_utc(2026, 1, 1),
        "Name": "Tết Dương lịch",
        "IsRecurringYearly": True,
        "Note": "Nghỉ 1 ngày (Điều 112 BLLĐ 2019)."
    },

    # ── Tết Nguyên Đán Bính Ngọ 2026 (Âm lịch – không cố định) ─────────────
    # Mùng 1 Tết = 17/02/2026 (Thứ Ba)
    # Chính phủ thường cho nghỉ 5 ngày làm việc: Thứ Hai 16/2 – Thứ Sáu 20/2
    # (29 Tháng Chạp = 14/02 Thứ Bảy → bù vào thứ Hai 16/02)
    {
        "Date": make_utc(2026, 2, 16),
        "Name": "Nghỉ Tết Nguyên Đán (29 Tháng Chạp – bù)",
        "IsRecurringYearly": False,
        "Note": "Bù ngày 29 Tháng Chạp rơi vào T7. Cần xác nhận theo Quyết định Thủ tướng."
    },
    {
        "Date": make_utc(2026, 2, 17),
        "Name": "Tết Nguyên Đán – Mùng 1",
        "IsRecurringYearly": False,
        "Note": "Mùng 1 Tháng Giêng Bính Ngọ."
    },
    {
        "Date": make_utc(2026, 2, 18),
        "Name": "Tết Nguyên Đán – Mùng 2",
        "IsRecurringYearly": False,
        "Note": "Mùng 2 Tháng Giêng Bính Ngọ."
    },
    {
        "Date": make_utc(2026, 2, 19),
        "Name": "Tết Nguyên Đán – Mùng 3",
        "IsRecurringYearly": False,
        "Note": "Mùng 3 Tháng Giêng Bính Ngọ."
    },
    {
        "Date": make_utc(2026, 2, 20),
        "Name": "Tết Nguyên Đán – Mùng 4",
        "IsRecurringYearly": False,
        "Note": "Mùng 4 Tháng Giêng Bính Ngọ. Cần xác nhận theo Quyết định Thủ tướng."
    },

    # ── Giỗ Tổ Hùng Vương (10/3 Âm lịch) ───────────────────────────────────
    # 10/3 ÂL 2026 = 26/04/2026 (Chủ nhật) → bù sang 27/04 (Thứ Hai)
    {
        "Date": make_utc(2026, 4, 27),
        "Name": "Giỗ Tổ Hùng Vương (bù)",
        "IsRecurringYearly": False,
        "Note": "10/3 ÂL 2026 = 26/04 (CN) → bù sang Thứ Hai 27/04. Cần xác nhận."
    },

    # ── 30/4 Ngày Giải phóng Miền Nam (cố định hàng năm) ────────────────────
    {
        "Date": make_utc(2026, 4, 30),
        "Name": "Ngày Giải phóng Miền Nam",
        "IsRecurringYearly": True,
        "Note": "Nghỉ 1 ngày (Điều 112 BLLĐ 2019)."
    },

    # ── 1/5 Quốc tế Lao động (cố định hàng năm) ─────────────────────────────
    {
        "Date": make_utc(2026, 5, 1),
        "Name": "Quốc tế Lao động",
        "IsRecurringYearly": True,
        "Note": "Nghỉ 1 ngày (Điều 112 BLLĐ 2019)."
    },

    # ── 2/9 Quốc khánh (cố định hàng năm) ───────────────────────────────────
    {
        "Date": make_utc(2026, 9, 2),
        "Name": "Quốc khánh",
        "IsRecurringYearly": True,
        "Note": "Nghỉ 1 ngày (Điều 112 BLLĐ 2019). Có thể được thêm 1 ngày liền kề."
    },
    # 2026: 02/09 là Thứ Tư → có thể thêm ngày 01/09 (Thứ Ba) hoặc 03/09 (Thứ Năm).
    # Tùy Quyết định Thủ tướng từng năm. Seed cả ngày 01/09 (không cố định).
    {
        "Date": make_utc(2026, 9, 1),
        "Name": "Nghỉ bù Quốc khánh",
        "IsRecurringYearly": False,
        "Note": "Ngày nghỉ thêm liền kề Quốc khánh 02/09/2026. Cần xác nhận theo Quyết định Thủ tướng."
    },
]


def seed_public_holidays():
    col = db["public_holidays"]

    # Xóa dữ liệu cũ của năm 2026 để tránh trùng lặp (chỉ xóa non-recurring năm 2026)
    col.delete_many({
        "Date": {"$gte": make_utc(2026, 1, 1), "$lte": make_utc(2026, 12, 31)},
        "IsRecurringYearly": False
    })
    # Xóa recurring đã seed để seed lại với dữ liệu mới nhất
    col.delete_many({"IsRecurringYearly": True})

    docs = []
    for h in HOLIDAYS_2026:
        doc = {
            "_id": ObjectId(),
            "IsDeleted": False,
            "CreatedAt": now,
            "CreatedBy": "System",
            "UpdatedAt": None,
            "UpdatedBy": None,
            "Version": 1,
            "Date": h["Date"],
            "Name": h["Name"],
            "IsRecurringYearly": h["IsRecurringYearly"],
            "Note": h.get("Note"),
        }
        docs.append(doc)

    result = col.insert_many(docs)
    print(f"✅ Đã seed {len(result.inserted_ids)} ngày lễ 2026 vào 'public_holidays'.")

    # Hiển thị tóm tắt
    for h in HOLIDAYS_2026:
        recurring = "🔁 recurring" if h["IsRecurringYearly"] else "📅 2026 only"
        print(f"   {h['Date'].strftime('%d/%m/%Y')} — {h['Name']}  [{recurring}]")


def seed_payroll_cycle_settings():
    col = db["system_settings"]

    new_settings = [
        {
            "Key": "PAYROLL_START_DAY",
            "Value": "1",
            "Description": "Ngày bắt đầu chu kỳ chấm công (1 = ngày 1 của tháng). "
                           "Nếu = 26 thì chu kỳ sẽ là từ 26 tháng trước đến PAYROLL_END_DAY tháng này.",
            "Group": "Payroll"
        },
        {
            "Key": "PAYROLL_END_DAY",
            "Value": "0",
            "Description": "Ngày kết thúc chu kỳ chấm công. 0 = ngày cuối tháng dương lịch. "
                           "1..28 = ngày cụ thể trong tháng.",
            "Group": "Payroll"
        },
        {
            "Key": "WEEKLY_DAYS_OFF",
            "Value": "6,0",
            "Description": "Các ngày nghỉ cố định trong tuần (DayOfWeek int: 0=CN, 1=T2, ..., 6=T7). "
                           "Mặc định '6,0' = Thứ Bảy và Chủ Nhật.",
            "Group": "Payroll"
        },
    ]

    inserted = 0
    for s in new_settings:
        existing = col.find_one({"Key": s["Key"], "IsDeleted": False})
        if existing:
            print(f"⚠️  Setting '{s['Key']}' đã tồn tại (value={existing['Value']}), bỏ qua.")
        else:
            col.insert_one({
                "_id": ObjectId(),
                "IsDeleted": False,
                "CreatedAt": now,
                "CreatedBy": "System",
                "UpdatedAt": None,
                "UpdatedBy": None,
                "Version": 1,
                **s
            })
            inserted += 1
            print(f"✅ Đã thêm setting '{s['Key']}' = '{s['Value']}'")

    # Cập nhật mô tả của STANDARD_WORKING_DAYS để ghi chú đã deprecated
    col.update_one(
        {"Key": "STANDARD_WORKING_DAYS", "IsDeleted": False},
        {"$set": {
            "Description": "[DEPRECATED - không còn sử dụng] Số ngày công chuẩn cố định. "
                           "Thay thế bởi PAYROLL_START_DAY + PAYROLL_END_DAY + WEEKLY_DAYS_OFF "
                           "(tính động theo chu kỳ lương và ngày lễ thực tế).",
            "UpdatedAt": now
        }}
    )
    print(f"\n✅ Đã thêm {inserted}/3 setting(s). STANDARD_WORKING_DAYS đã được đánh dấu [DEPRECATED].")


if __name__ == "__main__":
    print("=" * 60)
    print("SEED: Ngày lễ Việt Nam 2026 + Payroll Cycle Settings")
    print("=" * 60)
    seed_public_holidays()
    print()
    seed_payroll_cycle_settings()
    print("\nHoàn tất!")
    client.close()
