# Lab 4.1 — AI Code Review trên PR (Hướng dẫn học viên)

## Bạn sẽ làm gì

`sample-repo/` (.NET 8) có **7 bug cố ý** giấu trong 5 file. Bạn review nó bằng 2 cách của Copilot rồi đếm mỗi cách bắt được mấy / 7:

- **Vòng 1 — Built-in Copilot Code Review:** review "có sẵn", bấm 1 nút, không cấu hình.
- **Vòng 2 — Custom prompt `/review-pr`:** bạn tự viết 1 file checklist của team → Copilot review theo checklist đó.

Kết luận sẽ thấy: Vòng 1 bắt ~3-4/7, Vòng 2 bắt ~6-7/7.

> ĐỌC KỸ: Copilot **không có** lệnh gõ `/review` trong khung chat. Đừng đi tìm nó. "Review có sẵn" của Copilot bấm bằng **nút trên giao diện** (Bước 3). Riêng `/review-pr` ở Vòng 2 gõ được vì **bạn tự tạo file** cho nó (Bước 5).

---

## Bước 1 — Mở thư mục lab trong VS Code

Mở PowerShell, chạy:

```powershell
cd C:\Documents\Training\GithubCopilot\Slides\Assets\labs\lab-4.1
code .
```

VS Code mở ra. Ở thanh bên trái (Explorer) bạn thấy thư mục `sample-repo`.

---

## Bước 2 — Build cho chắc code chạy được

Mở Terminal trong VS Code: menu **Terminal > New Terminal** (hoặc `` Ctrl+` ``). Gõ:

```powershell
cd C:\Documents\Training\GithubCopilot\Slides\Assets\labs\lab-4.1\sample-repo
dotnet restore
dotnet build
```

Kết quả mong đợi: dòng cuối ghi **Build succeeded. 0 Error(s)**. Nếu lỗi, báo giảng viên.

---

## Bước 3 — Khởi tạo git để Copilot có "thay đổi" mà review

Built-in Copilot Code Review chỉ soi vào **thay đổi git chưa commit**. `sample-repo` chưa có git (bình thường). Trong cùng Terminal đó, gõ:

```powershell
git init
git add .
```

KHÔNG gõ `git commit`. KHÔNG cần push lên GitHub, KHÔNG cần tạo PR thật — mọi thứ chạy ngay trên máy bạn.

Kiểm tra: bấm icon **Source Control** ở thanh dọc bên trái (icon trông như 3 chấm nối nhánh, hoặc nhấn `Ctrl+Shift+G`). Bạn phải thấy danh sách file đang ở mục **Staged Changes** (UserService.cs, OrderController.cs, ...). Nếu trống → bạn quên `git add .`, chạy lại.

---

## Bước 4 — Vòng 1: chạy Built-in Copilot Code Review

Vẫn ở panel **Source Control** (`Ctrl+Shift+G`):

1. Nhìn lên **đầu panel**, ngay trên/cạnh ô nhập "Message" (ô để gõ commit message).
2. Tìm **icon Copilot Code Review** — hình ngôi sao lấp lánh (sparkle). Rê chuột vào sẽ hiện tooltip **"Copilot Code Review - Uncommitted Changes"**.
3. **Bấm vào icon đó.** Copilot bắt đầu review, chờ vài giây.
4. Kết quả: Copilot chèn **comment trực tiếp trong code** (chỗ dòng có vấn đề hiện 1 bong bóng/ghi chú). Mở từng file để đọc, hoặc xem danh sách tổng hợp trong tab review.

**Nếu KHÔNG thấy icon ngôi sao** (bản VS Code cũ hơn) — dùng cách thay thế, làm cho từng file:
1. Mở file (vd `Services/UserService.cs`).
2. Nhấn `Ctrl+A` để chọn toàn bộ.
3. **Chuột phải** vào vùng đã chọn → chọn **Copilot** → **Review and Comment** (một số bản: **Generate Code > Review**).
4. Lặp lại cho cả 5 file có code.

**Đếm và ghi lại:**

```
Vòng 1 (built-in) bắt được: ____ / 7 bug
```

Thường là **3-4 / 7**. Để ý 2 bug nó hay bỏ sót: **race condition** (CounterService) và **N+1 query** (OrderController).

> Vì sao chỉ 3-4? Vì review này chung chung, không biết quy tắc riêng của team bạn.

---

## Bước 5 — Vòng 2: tạo custom prompt theo checklist team

### 5a. Tạo file prompt

Trong Explorer (`Ctrl+Shift+E`), bên trong `sample-repo`, tạo cây thư mục và file:

`.github/prompts/review-pr.prompt.md`

Cách tạo nhanh: chuột phải vào `sample-repo` → **New File...** → gõ nguyên đường dẫn `.github/prompts/review-pr.prompt.md` rồi Enter (VS Code tự tạo các thư mục con).

### 5b. Dán nội dung này vào file, rồi **Lưu** (`Ctrl+S`)

```markdown
---
mode: ask
description: Review PR theo team checklist
---

Bạn là senior .NET reviewer.

Review file ${file} hoặc ${selection} theo checklist, đánh dấu severity Block / Suggest / Nit.

## Security (Block)
- SQL injection (FromSqlRaw with string concat)
- Hardcoded secret / connection string / API key
- Missing [Authorize] / authorization check
- XSS in HTML rendering

## Performance (Suggest)
- N+1 query (loop with DB call)
- Missing AsNoTracking() cho read-only
- Synchronous block trong async (.Result, .Wait)
- Missing pagination cho list endpoint
- new HttpClient() thay vì IHttpClientFactory

## Correctness (Block/Suggest)
- Null reference (truy cập property của possibly-null object)
- Race condition (shared mutable state, async)
- DateTime: UTC vs Local mix
- Missing CancellationToken propagation
- Lost exceptions (catch rỗng)

## Style (Nit)
- Magic number không có constant
- Naming không theo team convention
- TODO/FIXME không có ticket reference

## Output format
Group theo severity. Mỗi issue:
- File:line
- Vấn đề
- Fix gợi ý (code snippet)
```

### 5c. Chạy prompt vừa tạo

1. Mở **Copilot Chat**: bấm icon Copilot trên thanh trên cùng, hoặc nhấn `Ctrl+Alt+I`.
2. Trong khung chat, gõ dấu `/`. Một danh sách hiện ra — bạn sẽ thấy **`/review-pr`** (chính là file bạn vừa tạo). Chọn nó.
3. Đính kèm cả 7 file bằng cách dán nguyên dòng dưới (gõ `#file:` rồi VS Code gợi ý chọn file, hoặc copy nguyên):

```text
/review-pr #file:Services/UserService.cs #file:Controllers/OrderController.cs #file:Services/ShippingService.cs #file:Infrastructure/EmailSender.cs #file:Services/CounterService.cs #file:Mappers/ProductMapper.cs #file:appsettings.json
```

4. Nhấn Enter, chờ Copilot trả về danh sách issue gom theo Security / Performance / Correctness / Style.

**Đếm và ghi lại:**

```
Vòng 2 (custom) bắt được: ____ / 7 bug
```

Thường là **6-7 / 7**.

---

## Bước 6 — Đối chiếu đáp án & tinh chỉnh (refine)

1. Mở `sample-pr.md` (file cùng thư mục lab) — đây là đáp án đầy đủ 7 bug + chỗ nằm.
2. So với output của Vòng 2: bug nào Copilot vẫn bỏ sót?
3. Mở lại `review-pr.prompt.md`, thêm 1 rule cụ thể cho bug bị sót, **Lưu**, rồi chạy lại Bước 5c.

Gợi ý rule khi còn sót:

| Bug còn sót | Thêm dòng này vào prompt |
|---|---|
| Race condition | `- Race condition: Singleton service có field/property mutable bị ghi đồng thời` |
| N+1 query | `- N+1: gọi DB (Find/First/Where...ToList) bên trong vòng lặp foreach/for` |
| Null reference | `- Navigation property (vd p.Category) dùng mà không Include hoặc không null-check` |

Chạy lại → xác nhận đã bắt đủ.

---

## Bảng tổng kết (điền vào để nộp)

| Cách review | Bug caught | Thời gian |
|---|---|---|
| Vòng 1 — Built-in Copilot Code Review | ____ / 7 | ~30s |
| Vòng 2 — Custom `/review-pr` | ____ / 7 | ~1 phút |
| (Tham khảo) Human review kỹ | 7 / 7 | 30+ phút |

---

## Coi như xong khi

- [ ] Đã chạy Vòng 1 (built-in) và ghi số bug bắt được
- [ ] Đã tạo file `.github/prompts/review-pr.prompt.md` và chạy `/review-pr`
- [ ] Đã refine prompt ít nhất 1 lần cho bug bị sót
- [ ] Điền xong bảng tổng kết

## Rút ra

AI review **không thay được** người review, nhưng custom prompt cắt khoảng **70% công** của senior. Built-in = first-pass chung chung; custom prompt = chuẩn riêng của team. Kết quả AI không cố định mỗi lần chạy, nên chạy 2 lần và lấy khoảng.

> Lab 4.3 sẽ "nâng cấp" file `review-pr.prompt.md` này thành một skill dùng chung cho cả team, và Lab 4.4 cho CI tự chạy nó trên mọi PR.
