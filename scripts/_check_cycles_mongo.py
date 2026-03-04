"""Kiểm tra raw values của payroll_cycles trong MongoDB."""
from pymongo import MongoClient

db = MongoClient("mongodb+srv://nguyenvanminh180220_db_user:Pass-180220@cluster0.kfpckor.mongodb.net/")["EmployeeCleanDB"]
cycles = list(db["payroll_cycles"].find({}, {"monthKey": 1, "startDate": 1, "endDate": 1, "standardWorkingDays": 1}).sort("monthKey", 1))

print(f"Total cycles in DB: {len(cycles)}")
for c in cycles:
    mk = c.get("monthKey") or c.get("MonthKey") or "?"
    sd = c.get("startDate") or c.get("StartDate") or "?"
    ed = c.get("endDate") or c.get("EndDate") or "?"
    wdays = c.get("standardWorkingDays") or c.get("StandardWorkingDays") or "?"
    print(f"  {mk:10} startDate={sd}  endDate={ed}  workDays={wdays}")
