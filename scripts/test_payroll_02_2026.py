"""
Test script: Kiểm tra tính lương tháng 02/2026 với shifted payroll cycle.

Các bước:
  1. Đăng nhập lấy JWT token
  2. Bulk-generate chu kỳ lương năm 2026 (idempotent)
  3. Xem chi tiết chu kỳ 02-2026 (StartDate, EndDate, StandardWorkingDays)
  4. Chạy tính lương 02-2026
  5. Lấy kết quả bảng lương và in summary

Chạy: python scripts/test_payroll_02_2026.py
"""

import requests
import json

BASE_URL = "http://localhost:5055"
HEADERS  = {"Content-Type": "application/json"}

# ─── 1. LOGIN ────────────────────────────────────────────────────────────────
print("=" * 60)
print("BƯỚC 1: Đăng nhập")
print("=" * 60)

login_resp = requests.post(
    f"{BASE_URL}/api/auth/login",
    json={"username": "admin", "password": "User@12345"},
    headers=HEADERS,
    timeout=15,
)
print(f"Status: {login_resp.status_code}")
login_data = login_resp.json()

if login_resp.status_code != 200 or "data" not in login_data:
    print("❌ Đăng nhập thất bại!")
    print(json.dumps(login_data, indent=2, ensure_ascii=False))
    exit(1)

token = login_data["data"].get("accessToken") or login_data["data"].get("token")
if not token:
    # cố gắng tìm token trong nested data
    for key in ("accessToken", "token", "access_token", "jwt"):
        if key in login_data["data"]:
            token = login_data["data"][key]
            break
    if not token:
        print("❌ Không tìm thấy token trong response!")
        print(json.dumps(login_data["data"], indent=2, ensure_ascii=False))
        exit(1)

AUTH_HEADERS = {**HEADERS, "Authorization": f"Bearer {token}"}
print(f"✅ Đăng nhập thành công. Token: {token[:30]}...")


# ─── 2. BULK-GENERATE CYCLES 2026 ────────────────────────────────────────────
print("\n" + "=" * 60)
print("BƯỚC 2: Bulk-generate chu kỳ lương năm 2026")
print("=" * 60)

bulk_resp = requests.post(
    f"{BASE_URL}/api/payroll-cycles/bulk-generate",
    json={"year": 2026},
    headers=AUTH_HEADERS,
    timeout=120,
)
print(f"Status: {bulk_resp.status_code}")
bulk_data = bulk_resp.json()

if bulk_resp.status_code == 200:
    print(f"✅ {bulk_data.get('message', '')}")
    cycles = bulk_data.get("data", [])
    print(f"\n{'MonthKey':<10} {'StartDate':<14} {'EndDate':<14} {'WorkDays':>8} {'Holidays':>9} {'Status'}")
    print("-" * 70)
    for c in cycles:
        print(f"{c['monthKey']:<10} {c['startDate']:<14} {c['endDate']:<14} "
              f"{c['standardWorkingDays']:>8} {c['publicHolidaysExcluded']:>9} {c['status']}")
else:
    print("⚠️  Bulk-generate response:")
    print(json.dumps(bulk_data, indent=2, ensure_ascii=False))


# ─── 3. CHI TIẾT CU KỲ 02-2026 ───────────────────────────────────────────────
print("\n" + "=" * 60)
print("BƯỚC 3: Chi tiết chu kỳ 02-2026")
print("=" * 60)

cycle_resp = requests.get(
    f"{BASE_URL}/api/payroll-cycles/02-2026",
    headers=AUTH_HEADERS,
    timeout=10,
)
print(f"Status: {cycle_resp.status_code}")
cycle_data = cycle_resp.json()

if cycle_resp.status_code == 200:
    c = cycle_data["data"]
    print(f"  MonthKey            : {c['monthKey']}")
    print(f"  StartDate           : {c['startDate']}")
    print(f"  EndDate             : {c['endDate']}")
    print(f"  StandardWorkingDays : {c['standardWorkingDays']}")
    print(f"  PublicHolidays      : {c['publicHolidaysExcluded']}")
    print(f"  Status              : {c['status']}")
else:
    print(json.dumps(cycle_data, indent=2, ensure_ascii=False))


# ─── 4. TÍNH LƯƠNG 02-2026 ─────────────────────────────────────────────────
print("\n" + "=" * 60)
print("BƯỚC 4: Tính lương tháng 02-2026")
print("=" * 60)

gen_resp = requests.post(
    f"{BASE_URL}/api/payrolls/generate",
    json={"month": "02-2026"},
    headers=AUTH_HEADERS,
    timeout=120,
)
print(f"Status: {gen_resp.status_code}")
gen_data = gen_resp.json()

if gen_resp.status_code == 200:
    print(f"✅ {gen_data.get('message', '')}")
    print(f"   Data: {json.dumps(gen_data.get('data'), ensure_ascii=False)}")
else:
    print("❌ Tính lương thất bại!")
    print(json.dumps(gen_data, indent=2, ensure_ascii=False))


# ─── 5. XEM KẾT QUẢ BẢNG LƯƠNG ───────────────────────────────────────────────
print("\n" + "=" * 60)
print("BƯỚC 5: Kết quả bảng lương 02-2026 (10 dòng đầu)")
print("=" * 60)

list_resp = requests.get(
    f"{BASE_URL}/api/payrolls?month=02-2026",
    headers=AUTH_HEADERS,
    timeout=15,
)
print(f"Status: {list_resp.status_code}")
list_data = list_resp.json()

if list_resp.status_code == 200:
    payrolls = list_data.get("data", {}).get("items", list_data.get("data", []))
    if isinstance(payrolls, dict):
        payrolls = payrolls.get("items", [])

    if not payrolls:
        print("⚠️  Không có bảng lương nào được trả về.")
        print(json.dumps(list_data, indent=2, ensure_ascii=False)[:800])
    else:
        print(f"\nTổng cộng (trang đầu): {len(payrolls)} bảng lương")
        month_key = "02-2026"
        print(f"Chu kỳ {month_key}: 26/01/2026 – 25/02/2026  (mẫu số = 18 ngày)")
        print(f"\n{'Mã NV':<10} {'Họ tên':<30} {'NgàyThực':>9} {'Base':>14} {'Gross':>14} {'Net':>14}")
        print("-" * 94)

        total_gross = 0
        total_net   = 0
        for p in payrolls[:10]:
            code  = p.get('employeeCode', '---')
            name  = p.get('employeeName', '---')
            act   = p.get('actualWorkingDays', '-')
            base  = p.get('baseSalary', 0)
            gross = p.get('grossIncome', 0)
            net   = p.get('finalNetSalary', 0)

            total_gross += float(gross) if gross else 0
            total_net   += float(net)   if net   else 0

            print(f"{str(code):<10} {str(name)[:29]:<30} {str(act):>9} "
                  f"{float(base):>14,.0f} {float(gross):>14,.0f} {float(net):>14,.0f}")

        print("-" * 94)
        print(f"{'TỔNG (trang đầu)':<52} {total_gross:>14,.0f} {total_net:>14,.0f} VNĐ")
else:
    print(json.dumps(list_data, indent=2, ensure_ascii=False)[:1000])

print("\n" + "=" * 60)
print("✅ HOÀN THÀNH TEST")
print("=" * 60)
