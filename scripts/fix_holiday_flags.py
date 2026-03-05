"""
Fix IsHoliday flags in attendance_buckets for Feb 2026 Tết holidays.
Run once: py scripts/fix_holiday_flags.py
"""
from pymongo import MongoClient
import datetime

CONN = "mongodb+srv://nguyenvanminh180220_db_user:Pass-180220@cluster0.kfpckor.mongodb.net/"
DB   = "EmployeeCleanDB"

HOLIDAYS_FEB_2026 = [
    (datetime.datetime(2026, 2, 16), "Nghi Tet Nguyen Dan (bu 29 Thang Chap)"),
    (datetime.datetime(2026, 2, 17), "Tet Nguyen Dan - Mung 1"),
    (datetime.datetime(2026, 2, 18), "Tet Nguyen Dan - Mung 2"),
    (datetime.datetime(2026, 2, 19), "Tet Nguyen Dan - Mung 3"),
    (datetime.datetime(2026, 2, 20), "Tet Nguyen Dan - Mung 4"),
]

def main():
    client = MongoClient(CONN)
    db = client[DB]
    col = db["attendance_buckets"]

    total_marked = 0
    total_set_holiday_status = 0

    for hdate, hname in HOLIDAYS_FEB_2026:
        # Step 1: Set IsHoliday=true + Note for every DailyLog on this date
        r1 = col.update_many(
            {"Month": "02-2026"},
            {"$set": {
                "DailyLogs.$[l].IsHoliday": True,
                "DailyLogs.$[l].Note": hname,
            }},
            array_filters=[{"l.Date": hdate}]
        )

        # Step 2: Absent employees (no CheckIn) -> Status=Holiday, clear deduction flags
        r2 = col.update_many(
            {"Month": "02-2026"},
            {"$set": {
                "DailyLogs.$[l].Status": "Holiday",
                "DailyLogs.$[l].IsLate": False,
                "DailyLogs.$[l].IsEarlyLeave": False,
                "DailyLogs.$[l].IsMissingPunch": False,
            }},
            array_filters=[{"l.Date": hdate, "l.CheckIn": None}]
        )

        total_marked          += r1.modified_count
        total_set_holiday_status += r2.modified_count
        print(f"{hdate.date()} [{hname}]")
        print(f"  IsHoliday flagged : {r1.modified_count} buckets")
        print(f"  Status -> Holiday  : {r2.modified_count} buckets (absent employees)")

    print()
    print(f"Summary:")
    print(f"  Total buckets with IsHoliday set : {total_marked}")
    print(f"  Total buckets with Status=Holiday: {total_set_holiday_status}")
    client.close()
    print("Done.")

if __name__ == "__main__":
    main()
