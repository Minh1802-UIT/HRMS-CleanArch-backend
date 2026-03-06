"""
Migration: Backfill CreatedAt from ObjectId timestamp for all collections.

Vấn đề: Tất cả document cũ có CreatedAt = 0001-01-01T00:00:00Z (DateTime.MinValue)
vì các entity constructor chưa set CreatedAt (đã fix trong code, nhưng data cũ không
tự update).

Giải pháp: Dùng timestamp nhúng trong ObjectId (_id) để khôi phục thời điểm tạo
document thực tế. MongoDB ObjectId = 4 bytes Unix timestamp + 5 bytes random + 3 bytes counter.

Chạy với:
  .venv/Scripts/python.exe scripts/migrate_created_at.py

An toàn: Chỉ update các document có CreatedAt == 0001-01-01. Document đã có
CreatedAt đúng (ví dụ payroll_cycles, public_holidays, raw_attendance_logs mới)
sẽ không bị động tới.
"""

from pymongo import MongoClient, UpdateOne
from datetime import datetime, timezone
from bson import ObjectId

# ── Config ────────────────────────────────────────────────────────────────────
MONGO_URI = "mongodb+srv://nguyenvanminh180220_db_user:Pass-180220@cluster0.kfpckor.mongodb.net/"
DB_NAME   = "EmployeeCleanDB"

# Collections cần migrate (tất cả collection có entity kế thừa BaseEntity mà
# constructor chưa set CreatedAt). Bỏ qua payroll_cycles, public_holidays vì
# chúng đã set CreatedAt đúng từ đầu.
COLLECTIONS = [
    "attendance_buckets",
    "audit_logs",
    "candidates",
    "contracts",
    "departments",
    "employees",
    "interviews",
    "job_vacancies",
    "leave_allocations",
    "leave_requests",
    "leave_types",
    "notifications",
    "payrolls",
    "performance_goals",
    "performance_reviews",
    "positions",
    "raw_attendance_logs",
    "shifts",
    "system_settings",
]

# DateTime.MinValue được .NET MongoDB driver lưu như 0001-01-01T00:00:00Z
DOTNET_MIN_VALUE = datetime(1, 1, 1, 0, 0, 0, tzinfo=timezone.utc)


def migrate_collection(collection, dry_run: bool = False) -> dict:
    """
    Tìm tất cả document có CreatedAt == DateTime.MinValue, rồi set lại
    CreatedAt = thời điểm tạo ObjectId (_id.generation_time).
    """
    # Filter: chỉ document có CreatedAt là MinValue
    query = {"CreatedAt": DOTNET_MIN_VALUE}
    docs  = list(collection.find(query, {"_id": 1}))

    if not docs:
        return {"found": 0, "updated": 0}

    ops = []
    for doc in docs:
        oid = doc["_id"]
        # ObjectId.generation_time là datetime UTC (aware)
        created_at = oid.generation_time   # đã là timezone-aware UTC

        ops.append(UpdateOne(
            {"_id": oid},
            {"$set": {"CreatedAt": created_at}}
        ))

    if dry_run:
        return {"found": len(docs), "updated": 0, "dry_run": True}

    result = collection.bulk_write(ops, ordered=False)
    return {"found": len(docs), "updated": result.modified_count}


def main():
    client = MongoClient(MONGO_URI)
    db = client[DB_NAME]

    print(f"Connected to {DB_NAME}")
    print(f"Migration target: CreatedAt == {DOTNET_MIN_VALUE.isoformat()}\n")
    print(f"{'Collection':<30} {'Found':>8} {'Updated':>10}")
    print("-" * 52)

    total_found   = 0
    total_updated = 0

    for name in COLLECTIONS:
        col    = db[name]
        result = migrate_collection(col, dry_run=False)
        found   = result["found"]
        updated = result.get("updated", 0)
        total_found   += found
        total_updated += updated

        status = "✓" if updated == found else ("(skipped)" if found == 0 else "!")
        print(f"{name:<30} {found:>8} {updated:>10}  {status}")

    print("-" * 52)
    print(f"{'TOTAL':<30} {total_found:>8} {total_updated:>10}")
    print("\nDone.")
    client.close()


if __name__ == "__main__":
    main()
