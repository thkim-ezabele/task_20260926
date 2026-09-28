---
title: "S05: 재기준화, 결정 기록, 새 직원 모델과 스키마"
type: sprint
sprint: "S05"
status: planned
prd: [PRD-002]
started:
finished:
adrs: []
worklogs: []
aliases: [S05]
tags: [delivery, sprint]
created: 2026-09-27
updated: 2026-09-28
---

# S05: 재기준화, 결정 기록, 새 직원 모델과 스키마

- PRD: [PRD-002](../prd/PRD-002-employee-contacts.md)
- 토픽 브랜치: `feature/prd-002-employee-contacts` · 스프린트 종료 태그: `sprint/S05`
- 스프린트 번호는 **가번호**다. PRD-001 병합 뒤 T01에서 확정한다.

## 목표

> PRD-001 결과 위로 토픽을 다시 맞추고 ADR 4건을 `accepted`로 확정한다. 새 `employees` 스키마(`InitialCreate` 재생성)가 Aspire(MigrationService)와 Testcontainers에 적용되고, Domain Value Object 규칙과 DB 규칙(PRD-001 증빙 테스트 이전분)이 테스트로 통과한다. 이 스프린트 동안 HTTP API는 없다(샘플 API 제거).

## 작업

| ID | 작업 | 요구사항 | 완료 조건 | 의존 | 상태 | 커밋 |
|---|---|---|---|---|---|---|
| S05-T01 | 재기준화 (문서 작업) | NFR-05, NFR-06 | ① PRD-001이 `develop`에 병합(`v0.1.0`)되었고 `develop`을 토픽 브랜치에 병합했다 ② 스프린트 번호 확정(PRD `sprints` · 파일 이름 · ID 일치) ③ 범위 밖 항목을 `BL-NNN` / `TD-NNN`으로 등록(동명이인 전체 조회, CSV 헤더(BL + TD), CP949, `/` 포함 이름, 보존 기간 · 수정 · 삭제 · 조직) ④ ADR 번호 4개와 에러 코드 범위 예약 ⑤ `10-delivery/README` 목록 · roadmap에 PRD-002 행 추가 ⑥ ADR 0011~0023 · PRD-001 링크 점검(깨진 링크 0) ⑦ TD-010 · BL-019 · BL-023 · BL-024 처리 결과를 진행 기록에 적고, 미해결 항목은 "편입(planned:S06) 또는 위험 수용"을 사용자에게 확인 ⑧ S02-T06 ProblemDetails 계약(ErrorType → HTTP, `errors` 형식, CommonErrors)을 확인해 S06-T01 완료 조건 확정 ⑨ 병합 뒤 `dotnet build` 경고 0, `dotnet test` 통과, CI 동작 | PRD-001 병합 | todo | |
| S05-T02 | ADR 4건과 기준 문서 반영 (문서 작업, 사용자 확인) | FR-09, FR-11, NFR-05 | ① ADR 4건(API 규칙 예외, 일괄 가져오기 입력 처리, 이메일 대소문자 무시 유일, BuildingBlocks 오류 계약 확장)을 1차 초안 → 사용자 확인 → 2차 작성으로 `accepted` 커밋 ② ADR 커밋이 T03 이후 모든 구현 커밋보다 앞선다 ③ api-guidelines에 예외 절(적용 범위 3개 엔드포인트)과 링크 ④ error-codes에 공통 413 · 415, Employee 코드(파싱, 필드, 요청 안 중복, DB 중복, 대상 없음, 1,000행 초과, UTF-8 오류, JSON 문법 오류) 정수 할당 ⑤ ADR 목록 · 09-memory/design 갱신 대상 표시 | T01 | todo | |
| S05-T03 | Employee Domain 재설계, 매핑 변경, 샘플 API 제거 (마이그레이션 제외) | FR-01, FR-02, FR-10, NFR-06 | ① Value Object Name · Email(+NormalizedEmail) · PhoneNumber · JoinedOn의 `Create`가 Result를 반환하고 FR-01 규칙을 모두 구현 ② Aggregate: `display_name` → `name`, 등록 시 `employee_status` Active=1 고정 ③ 단위 테스트가 FR-01 엣지 목록을 모두 다룸 ④ EF 설정: 컬럼 · 타입 · NOT NULL · `ck_employees_employee_status` · `ux_employees_normalized_email` · `ix_employees_joined_on_id` · `ix_employees_name_joined_on_id` ⑤ 23505 매핑을 `ux_employees_normalized_email`로 교체 ⑥ 샘플 API(`api/v1/employees`)와 샘플 Command / Query / Validator 제거 ⑦ 증빙 테스트 **대응표** 초안(이전 테스트 → 새 테스트 또는 "이전 대기: 작업 ID") ⑧ 빌드 경고 0, 단위 · 아키텍처 테스트 통과. 통합 테스트는 T04 전까지 실패가 예상되며 진행 기록에 적는다 | T02 | todo | |
| S05-T04 | 스키마 리셋 전용 작업 (`InitialCreate` 재생성, ADR-0012) | FR-02, FR-11 | ① 커밋 R 하나에 `Persistence/Migrations/` 변경만 있다(폴더 삭제, `InitialCreate` 재생성), footer 없음 ② 기록 커밋 D(`Stage: dba`): `--idempotent` SQL 검토 기록(명명 · 타입 · ck · 인덱스 3개), 진행 기록(사유, 로컬 볼륨 삭제 필요), database.md 변경 이력 한 줄, local-setup 볼륨 삭제 안내 ③ 볼륨 삭제 → AppHost 재시작으로 MigrationService 성공, Api 기동 ④ Testcontainers `MigrateAsync` 적용, `dotnet test` 전체 통과(T05 이전 대상은 대응표에 표시) | T03 | todo | |
| S05-T05 | Repository 구현과 PRD-001 DB 증빙 테스트 이전 | FR-01, FR-06, FR-07, FR-08, FR-10, FR-11 | ① Write Repository: 정규화 이메일 목록 존재 조회(람다, `= ANY`, 쓰기 연결), 여러 건 추가(필요하면 BuildingBlocks `IRepository`에 추가) ② Read Repository: 목록(`joined_on`, `id` 정렬, Skip / Take), 개수, 이름 단건 프로젝션 ③ 새 스키마로 옮겨 통과: ck 위반 거부, 23505 → Conflict Result, xmin 충돌, 읽기 연결 쓰기 거부, UUID v7 DB 정렬, 감사 컬럼 UTC ④ HTTP 수준 항목(1001, 1002, 23505 → 409 HTTP, 500 형식)은 "이전 대기: S06-T06 / S07-T03"으로 표시 ⑤ database.md에 ERD · 인덱스 · 코드 표 반영(draft) | T04 | todo | |

상태: `todo` · `doing` · `done` · `blocked`(반려 3회) · `moved:BL-NNN`(백로그로 이관)

### 작업별 파이프라인

| 작업 | dba | developer | reviewer | tester |
|---|---|---|---|---|
| S05-T01 | database.md 변경 이력 · 마이그레이션 현황 확인(PRD-001 `InitialCreate`, 23505 매핑 목록). 없으면 해당 없음 | 문서 작업: 병합, 번호 확정, 공유 문서 반영, 백로그 등록, 계약 확인 기록 | 문서 규칙, 번호 충돌 없음, 기존 ADR 불변 | 명령 점검표: 병합 확인, 링크 점검, build / test 출력 |
| S05-T02 | ADR ③(정규화 컬럼) · ② 다중 Aggregate 트랜잭션 예외의 DB 내용 검토 | 1차 초안 반환 → 사용자 확인 → 2차 파일 작성 | ADR 템플릿, PRD FR-05 · 06 · 09 · Q14 · Q15와 일치, 코드 중복 없음 | 명령 점검표: ADR 커밋 순서, 코드 번호 중복 0, 링크 |
| S05-T03 | 스키마 명세(컬럼 · 인덱스 · 제약 이름 표)를 database.md 초안에 기록 | TDD: Value Object · Aggregate, EF 설정, 23505 매핑, 샘플 제거 | 표준 진입 점검, Domain 프레임워크 비의존, NFC · 소문자화 위치, 샘플 잔존 grep 0 | FR-01 인수 조건 대조, 통합 테스트 실패 목록과 대응표 일치 |
| S05-T04 | 주 작성자: 커밋 R(리셋만, footer 없음) → 커밋 D(기록 · 문서, `Stage: dba`) | 코드 변경 없음. 빌드 · 테스트 재실행 결과만 기록. 코드 수정이 필요하면 T03으로 반려 | `git show --stat R`로 Migrations 경로만 · footer 없음 · 명세 일치 확인 | 볼륨 삭제 후 AppHost 실행 기록, Testcontainers 적용, `\d employees` 출력 |
| S05-T05 | 쿼리 인덱스 사용 EXPLAIN 검토, 테스트 DB 구성(Respawn, read-only 연결), database.md 갱신 | TDD(Testcontainers), 람다 LINQ만 | 표준 진입 점검, 대응표가 PRD-001 증빙 목록과 1:1 | 대응표 대조, 통합 테스트 실행 |

## 완료 기준 (DoD)

- [ ] 모든 작업이 `done`이거나 백로그로 이관되었다
- [ ] 빌드와 모든 테스트(단위 · 통합 · 아키텍처)가 통과했다 (경고 0, 커버리지 보고 — NFR-06)
- [ ] 관련 위키 문서(API, 이벤트, DB)를 갱신했다
- [ ] 백로그 / 기술부채를 정리했다 (`new` 항목 없음)
- [ ] 토픽 브랜치를 push하고 `sprint/S05` 태그를 붙였다

## 진행 기록

> 단계 판정(PASS / REJECT / BLOCKED), 반려와 재작업, 계획 변경을 시간 순서로 적습니다.

| 날짜 | 작업 | 단계 | 판정 | 내용 (반려 시 되돌린 단계와 사유) |
|---|---|---|---|---|
| | | | | |

## 계획 리뷰

> `/sprint` 시작 시 에이전트별 계획 리뷰와 orchestrator 통합 결과, 사용자 승인 내용을 요약합니다.

분할 시 남긴 메모:

- **선행 조건**: T01은 PRD-001 병합(`v0.1.0`) 뒤에만 시작한다. 스프린트 · ADR · BL / TD · 에러 코드 번호는 T01에서 확정한다(이 계획의 ID는 가번호).
- **develop 병합 커밋**(T01)은 `Stage` footer가 없는 예외 커밋이다. 진행 기록에 해시를 남긴다.
- **리셋 전용 작업**(T04): ADR-0012 "리셋만 담은 커밋 1개"와 "단계마다 커밋"이 부딪치지 않도록 dba 단계에서 커밋 R(Migrations만, footer 없음)과 커밋 D(기록, `Stage: dba`)를 만든다. developer 단계는 코드를 바꾸지 않는다.
- **T03 → T04 공백**: 모델을 먼저 바꾸고 마이그레이션은 T04에서 다시 만들므로 T03 커밋 시점에는 통합 테스트가 실패한다. push는 스프린트 종료 때라 CI 영향은 없다. T03 developer 진입 점검의 "마이그레이션 적용" 항목은 T04로 넘긴다(계획 리뷰에서 확정).
- 스키마 변경을 T03에 모두 모아 이 토픽의 리셋은 T04 한 번으로 끝낸다. 이후 스키마를 바꾸면 추가 마이그레이션이 필요하다.
- T03은 ADR ③(이메일 정규화)이 `accepted`된 뒤 시작한다. ADR 사용자 확인 대기로 스프린트가 길어질 수 있다.
- 샘플 API 제거로 S05 동안 HTTP 엔드포인트가 없다. HTTP 수준 증빙은 대응표에 "이전 대기"로 남기고 S06 · S07에서 닫는다.
- 계획 리뷰에서 확인할 것: 샘플 API 제거 시점(T03), TD-010 편입 여부(T01). BL-024는 편입 확정(2026-09-28, S06-T06 · S07-T04), T01에서 backlog 상태를 `planned:S06`으로 바꾼다. BL-023은 PRD-001 S03에서 처리됨. .NET 10 전환(BL-002)은 진행하지 않음(사용자 결정)을 RETRO 결정 항목에 기록한다.

## 결과 리뷰

> 스프린트 종료 시 orchestrator의 결과 리뷰(계획 대비 실제, 완료 조건 · FR 충족, 반려 분석)를 요약합니다.

-

## 생긴 백로그 / 기술부채

| ID | 제목 | 발생 작업 | 정리 결과 |
|---|---|---|---|
| | | | open / planned:SNN / dropped |

## 회고

### 잘된 점

-

### 문제

-

### 다음에 바꿀 것

-

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 스프린트 계획 (`/prd` PRD-002 분할, 가번호) |
| 2026-09-28 | - | BL-024 편입 확정, .NET 10 미진행 메모 |
