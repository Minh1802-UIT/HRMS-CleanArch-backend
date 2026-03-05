"""
Fix attendance_buckets data for 02-2026:

Problem 1 – Holiday days (Feb 16-20, Tết Nguyên Đán):
  - Seeded with random punch data (Present/Late/EarlyLeave) — should be:
    CheckIn=null, CheckOut=null, WorkingHours=shift_std, OT=0, Status=Holiday

Problem 2 – TotalOvertime stored as COUNT of days with OT (seed bug)
  - C# RecalculateTotals() uses SUM of OvertimeHours, not count.
    Both need to be fixed for all 128 buckets.

Run: py scripts/fix_attendance_feb2026.py
"""

from pymongo import MongoClient
import datetime

CONN = "mongodb+srv://nguyenvanminh180220_db_user:Pass-180220@cluster0.kfpckor.mongodb.net/"
DB   = "EmployeeCleanDB"

# Standard working hours per shift code
SHIFT_STD_HOURS = {
    "S01": 8.0,
    "S02": 7.5,
    "S00": 8.0,
}

# Tết holidays Feb 2026
HOLIDAYS = {
    datetime.datetime(2026, 2, 16, tzinfo=datetime.timezone.utc): "Nghi Tet Nguyen Dan (bu 29 Thang Chap)",
    datetime.datetime(2026, 2, 17, tzinfo=datetime.timezone.utc): "Tet Nguyen Dan - Mung 1",
    datetime.datetime(2026, 2, 18, tzinfo=datetime.timezone.utc): "Tet Nguyen Dan - Mung 2",
    datetime.datetime(2026, 2, 19, tzinfo=datetime.timezone.utc): "Tet Nguyen Dan - Mung 3",
    datetime.datetime(2026, 2, 20, tzinfo=datetime.timezone.utc): "Tet Nguyen Dan - Mung 4",
}

# Status values that count as "Present" (mirrors C# IsPresent)
PRESENT_STATUSES = {"Present", "Late", "EarlyLeave"}


def fix_bucket(bucket: dict) -> dict:
    """
    Return updated DailyLogs + recalculated totals for a bucket.
    Returns (new_daily_logs, total_present, total_late, total_overtime).
    """
    logs = bucket.get("DailyLogs", [])

    for log in logs:
        log_date = log["Date"]
        # normalize to UTC-aware datetime for comparison
        if log_date.tzinfo is None:
            log_date = log_date.replace(tzinfo=datetime.timezone.utc)
        log_date_utc = log_date.replace(hour=0, minute=0, second=0, microsecond=0,
                                        tzinfo=datetime.timezone.utc)

        if log_date_utc in HOLIDAYS:
            holiday_name = HOLIDAYS[log_date_utc]

            log["CheckIn"]           = None
            log["CheckOut"]          = None
            log["WorkingHours"]      = 8.0  # Full day = 8h for everyone on holidays
            log["OvertimeHours"]     = 0.0
            log["LateMinutes"]       = 0
            log["EarlyLeaveMinutes"] = 0
            log["IsHoliday"]         = True
            log["IsWeekend"]         = False
            log["IsLate"]            = False
            log["IsEarlyLeave"]      = False
            log["IsMissingPunch"]    = False
            log["Status"]            = "Holiday"
            log["Note"]              = holiday_name

    # Recalculate totals (mirrors C# AttendanceBucket.RecalculateTotals)
    total_present  = sum(1 for l in logs if l.get("Status") in PRESENT_STATUSES)
    total_late     = sum(1 for l in logs if l.get("IsLate") or l.get("Status") == "Late")
    total_overtime = sum(l.get("OvertimeHours", 0.0) for l in logs)

    return logs, total_present, total_late, total_overtime


def main():
    client = MongoClient(CONN)
    db     = client[DB]
    col    = db["attendance_buckets"]

    buckets = list(col.find({"Month": "02-2026"}))
    print(f"Found {len(buckets)} buckets for 02-2026")

    updated = 0
    for bucket in buckets:
        new_logs, total_present, total_late, total_overtime = fix_bucket(bucket)

        col.update_one(
            {"_id": bucket["_id"]},
            {"$set": {
                "DailyLogs":      new_logs,
                "TotalPresent":   total_present,
                "TotalLate":      total_late,
                "TotalOvertime":  round(total_overtime, 4),
            }}
        )
        updated += 1
        if updated % 20 == 0:
            print(f"  Updated {updated}/{len(buckets)}...")

    print(f"\nDone. Updated {updated} buckets.")

    # ── Sanity check ──────────────────────────────────────────────────
    print("\n── Sanity check: holiday days ──")
    sample = list(col.aggregate([
        {"$match": {"Month": "02-2026"}},
        {"$unwind": "$DailyLogs"},
        {"$match": {"DailyLogs.Date": {
            "$gte": datetime.datetime(2026, 2, 16, tzinfo=datetime.timezone.utc),
            "$lte": datetime.datetime(2026, 2, 20, 23, 59, 59, tzinfo=datetime.timezone.utc),
        }}},
        {"$group": {
            "_id": {
                "date": "$DailyLogs.Date",
                "status": "$DailyLogs.Status",
                "isHoliday": "$DailyLogs.IsHoliday",
            },
            "count": {"$sum": 1},
            "avgWorked": {"$avg": "$DailyLogs.WorkingHours"},
            "avgOT": {"$avg": "$DailyLogs.OvertimeHours"},
        }},
        {"$sort": {"_id.date": 1, "_id.status": 1}},
    ]))

    for row in sample:
        d   = row["_id"]["date"].strftime("%d/%m")
        st  = row["_id"]["status"]
        ih  = row["_id"]["isHoliday"]
        cnt = row["count"]
        wh  = round(row["avgWorked"], 2)
        ot  = round(row["avgOT"], 3)
        print(f"  {d} | {st:10s} | IsHoliday={ih} | count={cnt:3d} | avgWorked={wh}h | avgOT={ot}h")

    print("\n── Sanity check: TotalOvertime sample (first 5 buckets) ──")
    for b in col.find({"Month": "02-2026"}).limit(5):
        emp = b["EmployeeId"][:8]
        print(f"  EmployeeId=...{emp} | TotalPresent={b['TotalPresent']} | TotalLate={b['TotalLate']} | TotalOvertime={b['TotalOvertime']:.4f}h")

    client.close()


if __name__ == "__main__":
    main()
