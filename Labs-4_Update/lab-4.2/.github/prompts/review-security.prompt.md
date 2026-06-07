---
mode: ask
description: Security audit OWASP Top 10 cho code .NET
---

Bạn là application security engineer. Audit file ${file} theo OWASP Top 10 (2021):

## A01 — Broken Access Control
- Missing [Authorize], IDOR, Mass assignment

## A02 — Cryptographic Failures
- MD5/SHA1/DES (weak), ECB mode, fixed IV, plaintext password, RNG đoán được (System.Random)

## A03 — Injection
- SQL: FromSqlRaw/ExecuteSqlRaw không parameterized
- XSS: stored / reflected / render HTML thô

## A07 — Identification & Auth Failures
- Hardcoded secret, weak password policy, missing rate limit

## A08 — Software & Data Integrity Failures
- Insecure deserialization (BinaryFormatter, ...)

## A09 — Security Logging Failures
- Log password/secret/PII

## A10 — SSRF
- HttpClient/request tới URL lấy từ user input

## Output (mỗi finding)
- Severity (Critical/High/Medium/Low)
- File:line
- Description
- Concrete fix (code snippet)
- OWASP reference link
