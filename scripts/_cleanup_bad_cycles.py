"""Xóa các payroll_cycles bị lệch ngày (giữ lại 3 cycle seeded đúng)."""
from pymongo import MongoClient

MONGO_URI = "mongodb+srv://nguyenvanminh180220_db_user:Pass-180220@cluster0.kfpckor.mongodb.net/"
db = MongoClient(MONGO_URI)["EmployeeCleanDB"]

keep = {"01-2026", "02-2026", "03-2026"}
all_cycles = list(db["payroll_cycles"].find({}, {"_id": 1, "monthKey": 1}))
to_delete  = [c["_id"] for c in all_cycles if c.get("monthKey") not in keep]

if to_delete:
    res = db["payroll_cycles"].delete_many({"_id": {"$in": to_delete}})
    print(f"Deleted {res.deleted_count} bad cycle(s): {[c.get('monthKey') for c in all_cycles if c['_id'] in to_delete]}")
else:
    print("Nothing to delete — only seeded cycles present.")

remaining = [c["monthKey"] for c in db["payroll_cycles"].find({}, {"monthKey": 1})]
print(f"Remaining cycles: {remaining}")
