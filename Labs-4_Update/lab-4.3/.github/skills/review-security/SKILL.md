---
name: review-security
description: |
  Use this skill when the user asks for an OWASP Top 10 security audit
  of a file, a diff, or a folder, especially auth / SQL / HTML rendering changes.
---

# Review Security Skill

## When to use
- The user names a file (`#file:VulnerableCode.cs`) and asks for a security audit
- Before merging a PR touching credential-handling, SQL, or HTML rendering

## Workflow
1. Read the target file (or `${selection}`)
2. Walk every OWASP category below
3. For each finding: severity, file:line, description, fix snippet, OWASP link

## OWASP categories
### A01 — Broken Access Control
- Missing `[Authorize]`, IDOR, mass assignment

### A02 — Cryptographic Failures
- MD5/SHA1/DES, plaintext password, weak RNG

### A03 — Injection
- `FromSqlRaw`/`ExecuteSqlRaw` not parameterized
- Stored/reflected XSS, raw HTML rendering

### A07 — Authentication Failures
- Hardcoded secret, weak password policy, predictable token

### A08 — Software & Data Integrity Failures
- Insecure deserialization (`BinaryFormatter`, unsafe serializers)

### A09 — Security Logging Failures
- Log password/secret/PII

### A10 — SSRF
- `HttpClient` or request to URL from user input

## Output format
```
### Critical
- file:line — description
  Why dangerous
  Fix
  https://owasp.org/...
```

## Verify pass criterion
After fix, re-run skill. Target: 0 Critical / 0 High.
