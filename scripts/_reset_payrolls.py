"""Reset payrolls và attendance cho tháng 02-2026 để test tính lương sạch."""
from pymongo import MongoClient

db = MongoClient("mongodb+srv://nguyenvanminh180220_db_user:Pass-180220@cluster0.kfpckor.mongodb.net/")["EmployeeCleanDB"]

# Xóa payrolls 02-2026 (để tính lại từ đầu)
res = db["payrolls"].delete_many({"Month": "02-2026"})
print(f"Deleted {res.deleted_count} payroll records for 02-2026")

# Xóa cycle 02-2026 cũ (nếu cần regenerate)
# (Đã có 1 cycle đúng trong Atlas từ single generate call)

# Verify attendance
attn = db["attendance_buckets"].count_documents({"Month": "02-2026"})
print(f"Attendance buckets for 02-2026: {attn} (should be 128)")

# Verify public holidays
ph = db["public_holidays"].count_documents({})
print(f"Public holidays in Atlas: {ph}")

# Verify cycle
cycle = db["payroll_cycles"].find_one({"MonthKey": "02-2026"})
if cycle:
    print(f"Cycle 02-2026: {cycle.get('StartDate')} – {cycle.get('EndDate')}, WorkDays={cycle.get('StandardWorkingDays')}, Holidays={cycle.get('PublicHolidaysExcluded')}")
else:
    print("No cycle 02-2026 found")
