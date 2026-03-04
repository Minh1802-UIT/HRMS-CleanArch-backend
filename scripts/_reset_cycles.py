"""Xóa toàn bộ payroll_cycles để cho C# bulk-generate tạo lại từ đầu với UTC-correct dates."""
from pymongo import MongoClient

db = MongoClient("mongodb+srv://nguyenvanminh180220_db_user:Pass-180220@cluster0.kfpckor.mongodb.net/")["EmployeeCleanDB"]
res = db["payroll_cycles"].delete_many({})
print(f"Deleted {res.deleted_count} cycles. Collection is now empty.")

remaining = db["payroll_cycles"].count_documents({})
print(f"Remaining: {remaining}")
