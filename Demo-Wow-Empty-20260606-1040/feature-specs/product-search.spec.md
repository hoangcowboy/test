# Feature: Product Search

> **Status:** Approved by PO
> **Priority:** High
> **Sprint:** 2026-05-19
> **Story Points:** 5

## Why

Khách hàng phàn nàn không tìm được sản phẩm theo từ khóa. Hiện tại chỉ có list + filter category. Cần search theo tên + description + sort theo relevance.

## What

Thêm endpoint `GET /api/products/search` với:
- Full-text search trên `Name` + `Description`
- Filter: `category`, `minPrice`, `maxPrice`
- Sort: `relevance` (default), `price`, `name`
- Pagination chuẩn (`page`, `pageSize`)
- Response trả về kèm relevance score

## Acceptance Criteria

- **AC1:** Search "iPhone" trả về sản phẩm có "iPhone" trong Name hoặc Description
- **AC2:** Filter `?category=<guid>` chỉ trả sản phẩm của category đó
- **AC3:** Filter `?minPrice=100000&maxPrice=500000` chỉ trả sản phẩm trong khoảng
- **AC4:** Sort `relevance`: Name match (×3) + Description match (×1)
- **AC5:** Sort `price` / `name` thay đổi thứ tự đúng
- **AC6:** Query rỗng (`?q=`) hoặc < 2 ký tự → 400 BadRequest
- **AC7:** Pagination meta đúng (total, page, pageSize, totalPages)
- **AC8:** Unicode (vd: "điện thoại") hoạt động
- **AC9:** SQL injection attempt không thành công
- **AC10:** Test coverage ≥ 80% cho SearchAsync

## Non-Functional

- p95 latency < 200ms với 10k products
- pageSize max = 100 (chống DoS)
- Cancellation: cancel HTTP request → cancel DB query

## Out of Scope (Phase 2)

- ❌ Full-Text Search engine (Elastic, Azure Cognitive Search)
- ❌ Search analytics (popular queries, no-result queries)
- ❌ Autocomplete / suggestions
- ❌ Synonym handling ("phone" = "smartphone")
- ❌ Multi-language (chỉ tiếng Việt + English)

## Sample request / response

```http
GET /api/products/search?q=phone&minPrice=10000000&sortBy=relevance&page=1&pageSize=20
```

```json
{
  "items": [
    {
      "id": "...",
      "name": "iPhone 16 Pro",
      "price": 30000000,
      "score": 3.0
    },
    {
      "id": "...",
      "name": "Samsung Galaxy",
      "description": "Premium phone with...",
      "price": 25000000,
      "score": 1.0
    }
  ],
  "meta": {
    "total": 47,
    "page": 1,
    "pageSize": 20,
    "totalPages": 3
  }
}
```

## Edge cases (PO ghi chú)

- Query `'); DROP TABLE Products; --` → 200 with empty results (parameterized OK), KHÔNG drop table
- Query có dấu tiếng Việt "điện thoại" → match "Điện Thoại iPhone"
- Query 1 ký tự → 400 "Query must be at least 2 characters"
- Page = -1 → coerce về 1
- PageSize = 10000 → cap at 100

## Hand-off

PO done. Engineer:
1. Run `@planner /plan-feature` to break down
2. Run `@architect /design-feature` to design
3. Run `@backend-dev /scaffold-feature all`
4. Run `@tester /generate-tests`
5. Run `@reviewer /review-feature`
