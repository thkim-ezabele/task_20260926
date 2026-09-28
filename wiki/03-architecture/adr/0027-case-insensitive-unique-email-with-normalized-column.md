---
title: "ADR-0027: 이메일 대소문자 무시 유일 (정규화 컬럼 + 일반 유니크 인덱스)"
type: adr
adr: "0027"
status: accepted
date: 2026-09-28
deciders: []
supersedes:
superseded_by:
aliases: [ADR-0027]
tags: [adr, architecture, database]
created: 2026-09-28
updated: 2026-09-28
---

# ADR-0027: 이메일 대소문자 무시 유일 (정규화 컬럼 + 일반 유니크 인덱스)

## 배경 (Context)

- 이메일은 유일해야 하고 **대소문자를 무시**한다. 같은 요청 안과 DB 모두에 적용한다(PRD-002 인터뷰 · Q8, FR-06). 등록한 표기는 **입력 그대로 보존**한다(FR-01: `email` varchar(254) 입력 표기, `normalized_email` varchar(254) Domain 정규화 값).
- 중복은 세 곳에서 판정한다: 요청 안 중복(Handler, `400`), DB 중복 사전 조회(Handler, `409` + 행 번호), 동시 요청 경합(유니크 인덱스 `23505` → 제약 이름 매핑, `409` 행 번호 없음)([ADR-0026](0026-employee-bulk-import-input-processing.md), [ADR-0014](0014-command-transaction-boundary-and-unit-of-work.md)). 세 곳이 같은 판정을 내려야 한다. 다르면 Domain이 다르다고 본 두 이메일이 DB에서 `23505`가 되어 정상 요청이 `409`(행 번호 없음)로 보인다.
- PRD-001(S03)은 `email` 한 컬럼에 Trim + 소문자(Invariant) 정규화 값만 저장하고 `ux_employees_email`로 막았다. 입력 표기는 남지 않았다. `CHECK (email = lower(email))`는 DB collation과 .NET Invariant 소문자 변환 결과가 다를 수 있어 두지 않았다([데이터베이스](../../04-development/database.md)).
- DB는 PostgreSQL 17이고 DB collation은 libc `en_US.utf8`이다(S05-T02 실측, 아래 표 #1). Repository는 람다 LINQ만 쓴다([ADR-0009](0009-separate-read-write-db-context.md)).

## 검토한 대안 (Options)

1. **`email` 한 컬럼에 정규화 값만 저장**(PRD-001 방식): 장점: 컬럼 하나. 단점: 입력 표기를 보존하라는 FR-01을 채우지 못한다.
2. **citext 컬럼 + 유니크 인덱스**: 비교가 DB `lower()`(DB collation, 현재 libc `en_US.utf8`)를 따르고 Domain `ToLowerInvariant`와 다르다(실측 #3 · #5: `'İ@x.com'::citext = 'i@x.com'::citext`가 참, .NET은 U+0130을 바꾸지 않음). 요청 안 중복(Domain)과 DB 유일 판정이 갈려 Domain이 다르다고 본 두 이메일이 `23505` → 정상 요청이 `409`(행 번호 없음)로 보인다. 확장 설치(`CREATE EXTENSION`)가 마이그레이션 의존성으로 추가된다. (권한은 근거가 아니다: `employee_app`으로 설치할 수 있음을 실측 #2.)
3. **`lower(email)` 식 유니크 인덱스 또는 `CHECK (normalized_email = lower(email))`**: `lower()` 결과가 DB collation에 따라 다르고(실측 #4: U+0130이 libc에서는 `i`, ICU에서는 `i` + U+0307) .NET과도 다르다. 식 인덱스는 2와 같은 불일치가 생긴다(실측 #7: `'İ@x.com'` 뒤 `'i@x.com'`이 `23505`). CHECK는 Domain이 정상 처리한 값을 `23514`로 거부하고(실측 #6) ADR-0014는 `23514`를 변환하지 않아 `500`이다. 사전 조회도 `lower(email) = ANY`가 되어 Repository 쿼리에 DB 함수 의미가 들어간다.
4. **ICU nondeterministic collation**(대소문자 무시, level2): 유니크와 `= ANY`는 동작한다(실측 #9). PostgreSQL 17에서 이 collation 컬럼에 LIKE를 쓸 수 없다(실측 #9: `nondeterministic collations are not supported for LIKE`). 비교 규칙이 ICU라 Domain과 같은 판정이 보장되지 않는다(실측 #9: `ﬀ`와 `ff`를 같다고 판정. .NET의 `ﬀ` 변환은 실측하지 않았으므로 "보장되지 않는다"까지만 쓴다). collation 생성이 마이그레이션에 들어가고 판정이 ICU 버전에 묶인다.
5. **입력 표기 컬럼 + Domain 정규화 컬럼 + 일반 유니크 인덱스**: 장점: 판정 규칙이 Domain 한 곳에 있고 DB · 서버 설정과 무관하다. 단점: 컬럼 하나를 더 저장하고, DB가 정규화를 강제하지 않는다.

## 결정 (Decision)

**대안 5: 입력 표기(`email`)와 Domain이 정규화한 값(`normalized_email`)을 따로 저장하고, 유일성은 `normalized_email`의 일반 유니크 인덱스 하나로 강제한다.**

### Domain

- Email Value Object는 `Create(string?)`에서 앞뒤 공백을 뺀 입력 표기(`Value`)와 **`NormalizedEmail = Value.ToLowerInvariant()`**(NFC 정규화 없음)를 만든다. 형식 규칙과 오류 코드는 Value Object가 갖는다([ADR-0026](0026-employee-bulk-import-input-processing.md) 8절).
- Employee Aggregate는 Email Value Object(→ `email`)와 `NormalizedEmail` 문자열 속성(→ `normalized_email`)을 가진다. 유니크 인덱스와 사전 조회 쿼리는 이 속성에 건다.
- 요청 안 중복은 `NormalizedEmail`을 서수(ordinal) 비교한다.

### DB (dba 조항)

- `employees`에 `email varchar(254) NOT NULL`(입력 앞뒤 공백만 뺀 표기)과 `normalized_email varchar(254) NOT NULL`(Domain Email Value Object가 `email.ToLowerInvariant()`로 만든 값, NFC 정규화 없음)을 둔다.
- 유일성은 일반 유니크 인덱스 `ux_employees_normalized_email (normalized_email)` 하나로 강제한다. `email` 컬럼에는 인덱스를 두지 않는다.
- `23505` 제약 이름 매핑은 `ux_employees_normalized_email` → `23001`. DB 중복 사전 조회(`normalized_email = ANY(@p)`)와 경합 `23505`가 같은 코드를 쓴다.
- 판정 원본은 Domain 한 곳이다. DB는 Domain이 계산한 값을 바이트 그대로 비교만 한다. `CHECK (normalized_email = lower(email))`, 식 인덱스, 확장, 사용자 정의 collation은 두지 않는다.

### 실측 근거 (S05-T02 dba)

임시 컨테이너 `postgres:17`(17.11, tmpfs만, 볼륨 없음), 롤 `employee_app`(LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS, DB 소유자), `datcollate en_US.utf8`.

| # | 명령 | 출력 |
|---|---|---|
| 1 | `pg_roles` / `pg_database` | `rolsuper f`, owner `employee_app`, `datcollate en_US.utf8`, `datlocprovider c` |
| 2 | `CREATE EXTENSION citext;` | `CREATE EXTENSION`, `extversion 1.6`, `extowner employee_app` |
| 3 | `SELECT E'İ@x.com'::citext = 'i@x.com'::citext` | `t` |
| 4 | `lower(E'İ')` libc / `COLLATE "und-x-icu"` | `i`(1자) / `i̇`(2자) |
| 5 | .NET 8.0.31 `"İ".ToLowerInvariant()` | U+0130(변화 없음) |
| 6 | `CHECK (normalized_email = lower(email))`에 `İ@x.com` | `23514` |
| 7 | `UNIQUE INDEX (lower(email))`에 `İ@x.com` 뒤 `i@x.com` | `23505` |
| 8 | 일반 유니크(`normalized_email`)에 `İ@x.com`, `i@x.com` | 2행 저장 |
| 9 | ICU `und-u-ks-level2` nondeterministic: 유니크 · `= ANY` · LIKE · `'ﬀ@x.com' = 'ff@x.com'` | `23505` · 1건 · LIKE 오류 · `t` |

## 결과 (Consequences)

- 좋은 점: 판정 규칙이 Domain 한 곳이고 DB · 서버 설정(collation, 확장, ICU 버전)과 무관하다. 요청 안 중복 · DB 사전 조회 · 경합 `23505`가 같은 값을 비교한다. 인덱스는 일반 B-tree 하나다. 입력 표기가 보존된다.
- 비용 · 한계: 컬럼 하나 추가(최대 254자 중복 저장). `ToLowerInvariant`는 단순 대소문자 변환이고 NFC를 하지 않으므로 비ASCII 이메일은 사람이 같다고 볼 주소를 다르게 볼 수 있다(예: `STRASSE` / `straße`, NFC / NFD, U+0130 `İ` / `i`).
- 비용 · 한계: DB가 정규화를 강제하지 않으므로 앱을 거치지 않은 INSERT(시드, 원시 SQL, 수동 보정, 테스트 시드)는 같은 규칙(`ToLowerInvariant`)으로 `normalized_email`을 채워야 한다.
- 정규화 규칙을 바꾸면 기존 행을 다시 계산하는 데이터 마이그레이션이 필요하고, 그 결정은 새 ADR로 한다.
- 참고: [ADR-0014](0014-command-transaction-boundary-and-unit-of-work.md) 본문의 `ux_employees_email`은 매핑 방식의 예시이고, 이 ADR 뒤 실제 이름은 `ux_employees_normalized_email`이다(ADR-0014는 고치지 않는다).
- 전제와 실측 작업:
  - 새 스키마의 `\d employees`(컬럼 · 타입 · NOT NULL · 인덱스 이름)와 `--idempotent` SQL의 `CREATE UNIQUE INDEX ux_employees_normalized_email ON employees (normalized_email);`(S05-T04 명세, S05-T05 리셋 · 검토).
  - 대소문자만 다른 이메일의 `23505` → `23001`과 입력 표기 보존(S05-T06 통합 테스트).
  - 람다 `Contains` → `= ANY` 번역과 인덱스 사용(S05-T06 EXPLAIN).
- 후속: Email Value Object S05-T03, Aggregate · EF 매핑 · 제약 이름 매핑 교체 S05-T04, `InitialCreate` 재생성 S05-T05, Repository · DB 증빙 S05-T06. [데이터베이스](../../04-development/database.md)의 CHECK 문구는 S05-T02에서 고쳤고, ERD · 매핑 설명은 S05-T05(커밋 D)에서 실측값으로 고친다.
- 확인: 2026-09-28 오케스트레이션 세션 대리 확인(원안, 대안 5). `/retro` ④ 추인 대상이다([S05 대리 승인](../../10-delivery/sprints/S05-rebase-decisions-schema.md#대리-승인)).
