"""
Seed attendance_buckets for 01-2026 and 02-2026 to allow payroll calculation.
"""

from pymongo import MongoClient
from datetime import datetime, timedelta, timezone
from bson import ObjectId
import calendar
from typing import Any

# ── Config ─────────────────────────────────────────────────────────────────
MONGO_URI = "mongodb+srv://nguyenvanminh180220_db_user:Pass-180220@cluster0.kfpckor.mongodb.net/"
DB_NAME   = "EmployeeCleanDB"

client = MongoClient(MONGO_URI)
db = client[DB_NAME]
now = datetime.now(timezone.utc)

WEEKLY_DAYS_OFF_INT = {5, 6}   # Saturday=5, Sunday=6 (Python weekday: Mon=0 … Sun=6)

def get_holiday_dates_utc(db, start: datetime, end: datetime) -> set:
    all_holidays = list(db["public_holidays"].find({"IsDeleted": False}))
    result = set()
    for h in all_holidays:
        date_utc = h["Date"]
        is_recurring = h.get("IsRecurringYearly", False)
        if is_recurring:
            for year in range(start.year, end.year + 1):
                try:
                    candidate = datetime(year, date_utc.month, date_utc.day, tzinfo=timezone.utc)
                    if start.date() <= candidate.date() <= end.date():
                        result.add(candidate.date())
                except ValueError:
                    pass
        else:
            if start.date() <= date_utc.date() <= end.date():
                result.add(date_utc.date())
    return result

def calculate_cycle(month: int, year: int, start_day: int = 26, end_day: int = 25):
    if month == 1:
        prev_month, prev_year = 12, year - 1
    else:
        prev_month, prev_year = month - 1, year
    days_in_prev = calendar.monthrange(prev_year, prev_month)[1]
    actual_start_day = min(start_day, days_in_prev)
    start_date = datetime(prev_year, prev_month, actual_start_day, tzinfo=timezone.utc)

    days_in_current = calendar.monthrange(year, month)[1]
    actual_end_day = min(end_day, days_in_current)
    end_date = datetime(year, month, actual_end_day, tzinfo=timezone.utc)

    return start_date, end_date

def seed_attendance():
    employees = list(db["employees"].find({"IsDeleted": False}))
    buckets_col = db["attendance_buckets"]

    cycles = [
        (1, 2026),
        (2, 2026)
    ]

    total_inserted = 0

    for month_num, year_num in cycles:
        month_key = f"{month_num:02d}-{year_num}"
        start_date, end_date = calculate_cycle(month_num, year_num)
        holiday_dates = get_holiday_dates_utc(db, start_date, end_date)

        print(f"\\nProcessing cycle {month_key} ({start_date.date()} to {end_date.date()})")

        # Cleanup existing buckets for this month to avoid duplicates
        deleted = buckets_col.delete_many({"Month": month_key})
        if deleted.deleted_count > 0:
            print(f"  -> Deleted {deleted.deleted_count} existing buckets for {month_key}")

        docs_to_insert = []

        for emp in employees:
            emp_id_str = str(emp["_id"])
            
            daily_logs: list[dict[str, Any]] = []
            # Loop through tracking period
            current = start_date.date()
            while current <= end_date.date():
                is_weekend = current.weekday() in WEEKLY_DAYS_OFF_INT
                is_holiday = current in holiday_dates
                
                dt_utc = datetime(current.year, current.month, current.day, tzinfo=timezone.utc)
                
                if not is_weekend and not is_holiday:
                    # Normal working day
                    check_in = datetime(current.year, current.month, current.day, 8, 0, 0, tzinfo=timezone.utc)
                    check_out = datetime(current.year, current.month, current.day, 17, 0, 0, tzinfo=timezone.utc)
                    
                    daily_logs.append({
                        "Date": dt_utc,
                        "CheckIn": check_in,
                        "CheckOut": check_out,
                        "ShiftCode": "S01",  # Assuming S01 is typical shift
                        "WorkingHours": 8.0,
                        "LateMinutes": 0,
                        "EarlyLeaveMinutes": 0,
                        "OvertimeHours": 0.0,
                        "Status": "Present",
                        "IsLate": False,
                        "IsEarlyLeave": False,
                        "IsMissingPunch": False,
                        "IsMissingCheckIn": False,
                        "Note": "Mock data",
                        "IsHoliday": False,
                        "IsWeekend": False
                    })
                else:
                    daily_logs.append({
                        "Date": dt_utc,
                        "CheckIn": None,
                        "CheckOut": None,
                        "ShiftCode": "S01",
                        "WorkingHours": 0.0,
                        "LateMinutes": 0,
                        "EarlyLeaveMinutes": 0,
                        "OvertimeHours": 0.0,
                        "Status": "Holiday" if is_holiday else "Absent",
                        "IsLate": False,
                        "IsEarlyLeave": False,
                        "IsMissingPunch": False,
                        "IsMissingCheckIn": False,
                        "Note": "Mock data",
                        "IsHoliday": is_holiday,
                        "IsWeekend": is_weekend
                    })

                current += timedelta(days=1)

            bucket_doc = {
                "_id": ObjectId(),
                "IsDeleted": False,
                "CreatedAt": now,
                "CreatedBy": "SeedScript",
                "UpdatedAt": None,
                "UpdatedBy": None,
                "Version": 1,
                "EmployeeId": emp_id_str,
                "Month": month_key,
                "DailyLogs": daily_logs,
                "TotalPresent": sum(1 for log in daily_logs if log["Status"] == "Present"),
                "TotalLate": 0,
                "TotalOvertime": 0.0
            }
            docs_to_insert.append(bucket_doc)

        if docs_to_insert:
            buckets_col.insert_many(docs_to_insert)
            total_inserted += len(docs_to_insert)
            print(f"  -> Inserted {len(docs_to_insert)} attendance buckets for {month_key}")
            
    print(f"\\nTotal {total_inserted} documents inserted.")

if __name__ == '__main__':
    seed_attendance()
    client.close()
