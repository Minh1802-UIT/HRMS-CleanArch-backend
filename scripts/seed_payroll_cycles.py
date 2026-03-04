"""
Seed script:
  1. Cập nhật system_settings: PAYROLL_START_DAY=26, PAYROLL_END_DAY=25
  2. Pre-seed PayrollCycle cho 01/2026, 02/2026, 03/2026

Chu kỳ lương lệch (Shifted Payroll Cycle):
  Tháng T/YYYY → StartDate = ngày 26 tháng (T-1)/(YYYY)
               → EndDate   = ngày 25 tháng T/YYYY

VD:
  02/2026 → 26/01/2026 – 25/02/2026  (17 ngày công — có 5 ngày Tết 16-20/02)
  03/2026 → 26/02/2026 – 25/03/2026  (19 ngày công — không có ngày lễ)
"""

from pymongo import MongoClient
from datetime import datetime, timedelta, timezone
from bson import ObjectId

# ── Config ─────────────────────────────────────────────────────────────────
MONGO_URI = "mongodb+srv://nguyenvanminh180220_db_user:Pass-180220@cluster0.kfpckor.mongodb.net/"
DB_NAME   = "EmployeeCleanDB"

client = MongoClient(MONGO_URI)
db = client[DB_NAME]
now = datetime.now(timezone.utc)

WEEKLY_DAYS_OFF_INT = {5, 6}   # Saturday=5, Sunday=6 (Python weekday: Mon=0 … Sun=6)
WEEKLY_DAYS_OFF_SNAPSHOT = "6,0"  # C# DayOfWeek format stored in DB


def get_holiday_dates_utc(db, start: datetime, end: datetime) -> set:
    """
    Lấy tập hợp ngày lễ (date objects) từ collection public_holidays
    trong khoảng [start, end].
    Xử lý cả IsRecurringYearly=True.
    """
    all_holidays = list(db["public_holidays"].find({
        "IsDeleted": False
    }))

    result = set()
    for h in all_holidays:
        date_utc: datetime = h["Date"]
        is_recurring = h.get("IsRecurringYearly", False)

        if is_recurring:
            # Thử tất cả các năm trong khoảng
            for year in range(start.year, end.year + 1):
                try:
                    candidate = datetime(year, date_utc.month, date_utc.day, tzinfo=timezone.utc)
                    if start.date() <= candidate.date() <= end.date():
                        result.add(candidate.date())
                except ValueError:
                    pass  # invalid date (e.g. Feb 29 non-leap year)
        else:
            if start.date() <= date_utc.date() <= end.date():
                result.add(date_utc.date())

    return result


def count_working_days(start: datetime, end: datetime, holiday_dates: set) -> int:
    """Đếm số ngày làm việc trong [start, end], loại cuối tuần và ngày lễ."""
    count = 0
    current = start.date()
    end_date = end.date()
    while current <= end_date:
        if current.weekday() not in WEEKLY_DAYS_OFF_INT and current not in holiday_dates:
            count += 1
        current += timedelta(days=1)
    return count


def calculate_cycle(month: int, year: int, start_day: int = 26, end_day: int = 25):
    """Tính StartDate và EndDate của chu kỳ lệch."""
    from datetime import date
    import calendar

    # StartDate: ngày start_day của tháng trước
    if month == 1:
        prev_month, prev_year = 12, year - 1
    else:
        prev_month, prev_year = month - 1, year

    days_in_prev = calendar.monthrange(prev_year, prev_month)[1]
    actual_start_day = min(start_day, days_in_prev)
    start_date = datetime(prev_year, prev_month, actual_start_day, tzinfo=timezone.utc)

    # EndDate: ngày end_day của tháng hiện tại
    days_in_current = calendar.monthrange(year, month)[1]
    actual_end_day = min(end_day, days_in_current)
    end_date = datetime(year, month, actual_end_day, tzinfo=timezone.utc)

    return start_date, end_date


def seed_payroll_cycles():
    col = db["payroll_cycles"]

    cycles_to_seed = [
        (1, 2026),   # Tháng 01/2026: 26/12/2025 – 25/01/2026
        (2, 2026),   # Tháng 02/2026: 26/01/2026 – 25/02/2026 (có Tết)
        (3, 2026),   # Tháng 03/2026: 26/02/2026 – 25/03/2026
    ]

    for month, year in cycles_to_seed:
        month_key = f"{month:02d}-{year}"

        # Kiểm tra đã tồn tại chưa
        existing = col.find_one({"MonthKey": month_key, "IsDeleted": False})
        if existing:
            print(f"⚠️  PayrollCycle {month_key} đã tồn tại, bỏ qua.")
            continue

        start_date, end_date = calculate_cycle(month, year)
        holiday_dates = get_holiday_dates_utc(db, start_date, end_date)
        std_days = count_working_days(start_date, end_date, holiday_dates)

        doc = {
            "_id": ObjectId(),
            "IsDeleted": False,
            "CreatedAt": now,
            "CreatedBy": "System-Seed",
            "UpdatedAt": None,
            "UpdatedBy": None,
            "Version": 1,
            "Month": month,
            "Year": year,
            "MonthKey": month_key,
            "StartDate": start_date,
            "EndDate": end_date,
            "StandardWorkingDays": std_days,
            "WeeklyDaysOffSnapshot": WEEKLY_DAYS_OFF_SNAPSHOT,
            "PublicHolidaysExcluded": len(holiday_dates),
            "Status": "Open"
        }
        col.insert_one(doc)
        print(
            f"✅ PayrollCycle {month_key}: "
            f"{start_date.strftime('%d/%m/%Y')} – {end_date.strftime('%d/%m/%Y')}  "
            f"| Ngày công chuẩn = {std_days}  "
            f"| Ngày lễ loại trừ = {len(holiday_dates)}"
        )
        if holiday_dates:
            for d in sorted(holiday_dates):
                print(f"      🎌 {d.strftime('%d/%m/%Y')} (Lễ)")


def update_payroll_cycle_settings():
    col = db["system_settings"]

    updates = [
        ("PAYROLL_START_DAY", "26", "Ngày bắt đầu chu kỳ chấm công — 26 = chu kỳ lệch (cutoff ngày 25)."),
        ("PAYROLL_END_DAY",   "25", "Ngày kết thúc chu kỳ — 25 = chốt ngày 25 hàng tháng."),
    ]

    print("\n── Cập nhật system_settings ──")
    for key, value, desc in updates:
        result = col.update_one(
            {"Key": key, "IsDeleted": False},
            {"$set": {"Value": value, "Description": desc, "UpdatedAt": now}}
        )
        if result.modified_count:
            print(f"✅ Updated {key} → '{value}'")
        else:
            print(f"⚠️  {key} không tìm thấy để cập nhật.")


if __name__ == "__main__":
    print("=" * 65)
    print("SEED: Payroll Cycles 2026 (Shifted Cycle: ngày 26 → ngày 25)")
    print("=" * 65)
    seed_payroll_cycles()
    update_payroll_cycle_settings()
    print("\nHoàn tất!")
    client.close()
