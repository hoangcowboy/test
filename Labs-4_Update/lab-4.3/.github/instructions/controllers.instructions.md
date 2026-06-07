---
applyTo: "**/Controllers/**/*.cs"
description: Conventions for ASP.NET Core controllers
---

# Controller Conventions

## Shape
- Controller mỏng: chỉ orchestrate, KHÔNG chứa business logic.
- DI style: dùng primary constructor với readonly private fields.
- Return type: `IActionResult` hoặc `ActionResult<T>`, không trả entity thô.

## HTTP semantics
- `POST` trả `201 Created` kèm `Location` khi tạo resource.
- `PUT` trả `200 OK` hoặc `204 NoContent` khi cập nhật thành công.
- `DELETE` trả `204 NoContent` khi xóa thành công.
- Validation và model binding error sử dụng `ProblemDetails`.
- `404 NotFound` cho tài nguyên không tồn tại, `409 Conflict` cho xung đột business.

## Forbidden in controllers
- KHÔNG gọi thẳng `DbContext` trong controller — đi qua service.
- KHÔNG chứa business logic nặng hoặc query logic.
- KHÔNG bắt `Exception` chung rồi nuốt, để middleware xử lý.
