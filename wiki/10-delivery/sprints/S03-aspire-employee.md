---
title: "S03: Aspire와 Employee 샘플 서비스 전 구간"
type: sprint
sprint: "S03"
status: planned
prd: [PRD-001]
started:
finished:
adrs: []
worklogs: []
aliases: [S03]
tags: [delivery, sprint]
created: 2026-09-27
updated: 2026-09-27
---

# S03: Aspire와 Employee 샘플 서비스 전 구간

- PRD: [PRD-001](../prd/PRD-001-foundation.md)
- 토픽 브랜치: `feature/prd-001-foundation` · 스프린트 종료 태그: `sprint/S03`

## 목표

> `dotnet run --project <AppHost>` 한 번으로 PostgreSQL → MigrationService → Employee Api가 순서대로 뜨고, HTTP로 직원을 등록 · 조회할 수 있으며, 통합 테스트가 DB 규칙을 증명한다.

## 작업

| ID | 작업 | 요구사항 | 완료 조건 | 의존 | 상태 | 커밋 |
|---|---|---|---|---|---|---|
| S03-T01 | Employee Domain · Application: 직원 등록 Command, 단건 조회 Query | FR-08, FR-09 | Employee Aggregate(팩토리는 ID 인자), `EmployeeStatus` enum(smallint 대상), 등록 Command + Validator(이메일 형식, enum 1002) + Handler(`IIdGenerator`로 ID 생성, 이메일 중복이면 충돌 Result, SaveChanges 미호출), 단건 조회 Query + Read Repository 인터페이스 + Response record. Employee 에러 코드 할당. 단위 테스트가 성공 / 실패 / 엣지를 다룸 | S02-T02, S02-T06 | todo | |
| S03-T02 | Employee Infrastructure: 쓰기 / 읽기 DbContext, 매핑, Repository, 설계 시점 팩터리, 초기 마이그레이션 | FR-08, FR-06, FR-02, NFR-01 | `employees` 테이블(id uuid, display_name, email `ux_`, employee_status smallint + `ck_`, created_at / updated_at timestamptz, xmin) 매핑, 쓰기 DbContext용 `IDesignTimeDbContextFactory`(읽기는 제외), Repository / ReadRepository 구현. 초기 마이그레이션 1건 생성, `--idempotent` SQL을 dba가 검토 · 기록. 생성 코드 포함 `dotnet build` 경고 0 | T01, S02-T04 | todo | |
| S03-T03 | Employee Api: Controller, 공통 API 처리 연결, 아키텍처 테스트 확장 | FR-08, FR-07, FR-09, NFR-02 | 등록 POST · 단건 조회 GET Controller가 디스패처를 호출하고 Result를 ProblemDetails로 변환. `ConnectionStrings` Write / Read가 각각 쓰기 / 읽기 DbContext에 바인딩. 개발 환경 Swagger UI. 아키텍처 테스트 대상에 Employee 4개 레이어 추가 · 통과 | T02 | todo | |
| S03-T04 | Aspire AppHost · ServiceDefaults · MigrationService | FR-03, FR-08, NFR-04, NFR-06 | AppHost: PostgreSQL(user-secrets 비밀번호, 데이터 볼륨, S01-T01 이미지 태그), Database `emergency_hub_employee`(리소스 `employee-db`), `employee_app` 롤 · 권한 스크립트, MigrationService(적용 후 종료), Api에 Write / Read 주입(Read에 `default_transaction_read_only=on`), `WaitFor(db)` · `WaitForCompletion(migration)`. ServiceDefaults: 헬스체크, OTel 트레이스 · 메트릭, 서비스 디스커버리, 복원력, Serilog → OTLP. 명령 한 번으로 순서대로 뜨고 대시보드에서 Healthy · 로그 · 트레이스, HTTP 등록 → 조회(실행 기록 · 스크린샷). 저장소에 비밀 값 없음 | T02, T03 | todo | |
| S03-T05 | 통합 테스트: Testcontainers + WebApplicationFactory + Respawn | FR-09, FR-08, FR-05, FR-06, FR-07 | Testcontainers PostgreSQL(Aspire와 같은 메이저 태그), 실제 마이그레이션 적용(`EnsureCreated` 금지), Respawn, Aspire와 같은 실행 전략(재시도). 통과 항목: 등록 → 조회, 이메일 중복 → 충돌 ProblemDetails, 체크 제약 위반 거부, 동시성 충돌(인위 재현) → 충돌 Result, 읽기 연결 쓰기 시도 DB 거부, UUID v7 DB 정렬, 감사 컬럼 UTC, 바인딩 오류 1001 · enum 1002 · 처리되지 않은 예외 응답 형식. CI에서도 통과 | T03, T04 | todo | |

상태: `todo` · `doing` · `done` · `blocked`(반려 3회) · `moved:BL-NNN`(백로그로 이관)

### 작업별 파이프라인

| 작업 | dba | developer | reviewer | tester |
|---|---|---|---|---|
| S03-T01 | 해당 없음(테이블 세부는 계획 리뷰에서 확정한 구성) | TDD | 표준 진입 점검 | 인수 조건 대조 |
| S03-T02 | 스키마 · 매핑 · 마이그레이션 작성, idempotent SQL 검토 | Repository 구현, 테스트 | 표준 진입 점검 | 빌드 경고 0(FR-02 생성 코드 조건 재검증). 적용 확인은 T04 · T05 |
| S03-T03 | 연결 문자열 키가 S01-T02 ADR 매핑과 같은지 확인 | TDD, Controller는 얇게 | 표준 진입 점검 | 아키텍처 테스트, 인수 조건 대조(HTTP 검증은 T05) |
| S03-T04 | DB 리소스, 롤 · 권한 스크립트, 마이그레이션 적용, 연결 매핑 작성 / 검토 | AppHost · ServiceDefaults · MigrationService 구현 | 비밀 값 커밋 grep, 설정 키 일치 | 볼륨 초기화 후 AppHost 실행 기록(순서, Healthy, 등록 → 조회), 스크린샷 증빙 |
| S03-T05 | 테스트 DB 구성(롤, read-only 연결, Respawn 제외 테이블) 검토 | 테스트 fixture 준비 | 표준 진입 점검 | 주 작성자: 인수 시나리오 테스트 작성 · 실행, 실패 시 원인별 반려 |

## 완료 기준 (DoD)

- [ ] 모든 작업이 `done`이거나 백로그로 이관되었다
- [ ] 빌드와 모든 테스트(단위 · 통합 · 아키텍처)가 통과했다
- [ ] 관련 위키 문서(API, 이벤트, DB)를 갱신했다
- [ ] 백로그 / 기술부채를 정리했다 (`new` 항목 없음)
- [ ] 토픽 브랜치를 push하고 `sprint/S03` 태그를 붙였다

## 진행 기록

> 단계 판정(PASS / REJECT / BLOCKED), 반려와 재작업, 계획 변경을 시간 순서로 적습니다.

| 날짜 | 작업 | 단계 | 판정 | 내용 (반려 시 되돌린 단계와 사유) |
|---|---|---|---|---|
| | | | | |

## 계획 리뷰

> `/sprint` 시작 시 에이전트별 계획 리뷰와 orchestrator 통합 결과, 사용자 승인 내용을 요약합니다.

분할 시 남긴 메모:

- 샘플 `employees` 테이블 세부 구성(PRD Q16)은 이 스프린트 계획 리뷰에서 dba와 developer가 합의해 확정한다.
- S02에서 이관한 DB 동작 검증 항목은 T05에서 모두 닫는다.

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
| 2026-09-27 | - | 스프린트 계획 (`/prd` PRD-001 분할) |
