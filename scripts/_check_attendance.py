"""Check attendance data availability for payroll test."""
from pymongo import MongoClient

db = MongoClient("mongodb+srv://nguyenvanminh180220_db_user:Pass-180220@cluster0.kfpckor.mongodb.net/")["EmployeeCleanDB"]

print(f"01-2026 attendance buckets: {db['attendance_buckets'].count_documents({'Month': '01-2026'})}")
print(f"02-2026 attendance buckets: {db['attendance_buckets'].count_documents({'Month': '02-2026'})}")

# Check a sample 02-2026 bucket
sample = db["attendance_buckets"].find_one({"Month": "02-2026"}, {"EmployeeId": 1, "DailyLogs": 1, "TotalPresent": 1})
if sample:
    logs = sample.get("DailyLogs", [])
    dates = sorted([l["Date"] for l in logs if "Date" in l])
    print(f"  Sample 02-2026 bucket: {len(logs)} daily logs")
    print(f"  Date range: {dates[0] if dates else None} → {dates[-1] if dates else None}")
    total_present = sample.get("TotalPresent", 0)
    print(f"  TotalPresent: {total_present}")

    # Count logs in cycle window [2026-01-26, 2026-02-25]
    from datetime import datetime
    cycle_start = datetime(2026, 1, 26)
    cycle_end = datetime(2026, 2, 25)
    in_window = sum(1 for l in logs if "Date" in l and cycle_start <= l["Date"].replace(tzinfo=None) <= cycle_end)
    print(f"  Logs in cycle window [26/01–25/02]: {in_window}")
