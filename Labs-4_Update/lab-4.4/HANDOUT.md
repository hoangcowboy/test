# Lab 4.4 — AI Review thành cổng CI (Hướng dẫn học viên)

## Bạn sẽ làm gì

Bắt mọi Pull Request phải được Copilot review **trước khi** người duyệt nhìn vào, theo 2 lớp:

- **Lớp 1 — Copilot review tự động:** bật bằng **ruleset** trong Settings repo (cơ chế thật của GitHub, KHÔNG cần viết YAML).
- **Lớp 2 — Branch protection:** bắt buộc có review + 1 người duyệt + CI xanh trước khi merge.
- **(Tùy chọn nâng cao)** Chạy *team prompt riêng* (`review-pr.prompt.md` từ Lab 4.1) trong CI bằng GitHub Copilot CLI.

> Quan trọng: cách Copilot review PR tự động hiện nay là **bật một rule trong Settings**, KHÔNG phải gõ `/review` hay viết workflow `gh copilot suggest` (lệnh đó chỉ gợi ý lệnh shell, không review code).

## Bước 0 — Chuẩn bị (prerequisite)

- Một repo GitHub mà bạn có quyền **Admin** (để vào Settings → Rules). Dùng repo cá nhân là được.
- Tài khoản đã bật GitHub Copilot (có Copilot code review).
- Repo đã có code + ít nhất 1 CI workflow chạy `build`/`test` (để require status check). Nếu chưa có, vẫn làm được Lớp 1, phần status check để optional.

## Bước 1 — Bật Copilot review tự động (Lớp 1, native)

Trên GitHub, mở repo của bạn:

1. **Settings** (tab trên cùng của repo) → menu trái **Rules** → **Rulesets**.
2. Bấm **New ruleset** → **New branch ruleset**.
3. **Ruleset Name:** `pr-quality`.
4. **Enforcement status:** chọn **Active**.
5. **Target branches:** bấm **Add target** → **Include default branch** (chính là `main`).
6. Trong danh sách **Branch rules**, tích:
   - **Require a pull request before merging** → đặt **Required approvals = 1**.
   - **Automatically request Copilot code review** (nếu UI ghi khác: "Request pull request review from Copilot").
7. Bấm **Create**.

Từ giờ, mỗi PR mở ra (hoặc đẩy commit mới) → Copilot tự review và để lại suggestions trong tab **Files changed**.

## Bước 2 — Branch protection (Lớp 2)

Vẫn trong ruleset `pr-quality` vừa tạo (hoặc Settings → Branches → Add rule), bật thêm:

- **Require status checks to pass before merging** → thêm các check: `build`, `test` (tên job CI của bạn).
- **Require conversation resolution before merging**.
- **Block force pushes** (Allow force pushes = off) và **không cho xóa nhánh** (Allow deletions = off).
- **Do not allow bypassing the above settings** (kể cả admin).

Lưu lại.

## Bước 3 — Smoke test (xác nhận chạy)

Trên máy, trong repo:

```powershell
cd <đường-dẫn-repo-của-bạn>
git checkout -b test-ai-review
Add-Content -Path README.md -Value "`n<!-- trigger ai review -->"
git add .
git commit -m "test: trigger AI review"
git push -u origin test-ai-review
gh pr create --fill
```

Mở PR trên GitHub (hoặc `gh pr view --web`). Đợi ~30-60s → trong tab **Files changed** / phần review phải xuất hiện **review của Copilot**. Nếu chưa thấy, đẩy thêm 1 commit hoặc Reopen PR để trigger lại.

Coi như đạt nếu: PR mới tự động có review của Copilot, và nút Merge bị chặn cho tới khi đủ 1 approval + check xanh.

## Bước 4 (TÙY CHỌN, nâng cao) — Team prompt trong CI bằng Copilot CLI

Lớp 1 dùng engine review chung của GitHub. Nếu muốn ép đúng **checklist team** (`review-pr.prompt.md`), chạy GitHub Copilot CLI trong Actions:

### 4a. Tạo token

1. GitHub → **Settings (account)** → **Developer settings** → **Personal access tokens** → **Fine-grained tokens** → **Generate new token**.
2. Cấp quyền **Copilot Requests** (read/write theo yêu cầu của trang).
3. Copy token. Vào **repo → Settings → Secrets and variables → Actions → New repository secret**: tên `PERSONAL_ACCESS_TOKEN`, value = token.

### 4b. Thêm workflow review (copy-paste)

Tạo file `.github/workflows/copilot-review.yml` trong repo của bạn, dán nguyên nội dung sau (prerequisite: repo có `.github/prompts/review-pr.prompt.md` từ Lab 4.1):

```yaml
name: AI Review (team prompt)

on:
  pull_request:
    types: [opened, synchronize, reopened]

permissions:
  contents: read
  pull-requests: write

concurrency:
  group: ai-review-${{ github.event.pull_request.number }}
  cancel-in-progress: true

jobs:
  ai-review:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0
      - uses: actions/setup-node@v4
        with:
          node-version: 20
      - name: Install GitHub Copilot CLI
        run: npm install -g @github/copilot
      - name: Get PR diff
        id: diff
        run: |
          git diff origin/${{ github.base_ref }}...HEAD > diff.patch
          echo "diff_size=$(wc -l < diff.patch)" >> "$GITHUB_OUTPUT"
      - name: Run Copilot review with team prompt
        if: ${{ fromJSON(steps.diff.outputs.diff_size) <= 2000 }}
        env:
          COPILOT_GITHUB_TOKEN: ${{ secrets.PERSONAL_ACCESS_TOKEN }}
        run: |
          PROMPT="$(cat .github/prompts/review-pr.prompt.md)"
          copilot -p "${PROMPT}

          ## Diff to review (output Markdown to stdout):

          $(cat diff.patch)" --no-ask-user > review-output.md
      - name: Post review comment
        if: ${{ fromJSON(steps.diff.outputs.diff_size) <= 2000 }}
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: gh pr comment ${{ github.event.pull_request.number }} --body-file review-output.md
```

### 4c. (Tùy chọn) PR-summary tự động (copy-paste)

Tạo `.github/prompts/explain-pr.prompt.md` (nếu chưa có), dán:

```markdown
---
mode: ask
description: Sinh PR description chuẩn từ diff
---

Phân tích diff được cung cấp và viết PR description Markdown gồm:

## Summary
1-2 câu mô tả thay đổi chính (why, không phải what).

## Changes
- Bullet ngắn, group theo loại (feature / fix / refactor)

## Testing
- [ ] Unit test added/updated
- [ ] Manual smoke test

## Risk
- Breaking change: ___
- Rollback plan: ___
```

Rồi tạo `.github/workflows/pr-summary.yml`, dán:

```yaml
name: PR Summary

on:
  pull_request:
    types: [opened]

permissions:
  contents: read
  pull-requests: write

jobs:
  summary:
    runs-on: ubuntu-latest
    if: ${{ github.event.pull_request.body == '' || github.event.pull_request.body == null }}
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0
      - uses: actions/setup-node@v4
        with:
          node-version: 20
      - name: Install GitHub Copilot CLI
        run: npm install -g @github/copilot
      - name: Generate summary
        env:
          COPILOT_GITHUB_TOKEN: ${{ secrets.PERSONAL_ACCESS_TOKEN }}
        run: |
          git diff origin/${{ github.base_ref }}...HEAD > diff.patch
          PROMPT="$(cat .github/prompts/explain-pr.prompt.md)"
          copilot -p "${PROMPT}

          ## Diff:

          $(cat diff.patch)" --no-ask-user > summary.md
      - name: Update PR body
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: gh pr edit ${{ github.event.pull_request.number }} --body-file summary.md
```

## Coi như xong khi

- [ ] Ruleset bật "Automatically request Copilot code review" → PR mới tự có review của Copilot (Lớp 1)
- [ ] Branch protection: require 1 approval + status checks + conversation resolution + chặn force-push
- [ ] Smoke test: PR mới có review tự động trong < 60s và bị chặn merge tới khi đủ điều kiện
- [ ] (Tùy chọn) `copilot-review.yml` chạy team prompt và post comment trên PR

## Cảnh báo thường gặp

- Không thấy mục "Automatically request Copilot code review": tài khoản/org chưa bật Copilot code review, hoặc UI ở chỗ khác (thử Settings → Code review). Bật ở org settings nếu repo thuộc org.
- Status check `ai-review` không hiện trong danh sách required: phải để workflow chạy 1 lần (mở 1 PR) thì tên check mới xuất hiện để add.
- Workflow CLI báo lỗi auth: kiểm tra secret `PERSONAL_ACCESS_TOKEN` có quyền "Copilot Requests" và đã map vào env `COPILOT_GITHUB_TOKEN`.
