---
title: "백로그"
type: index
status: stable
tags: [delivery, backlog]
aliases: [Backlog]
created: 2026-09-27
updated: 2026-09-28
---

# 백로그

> 개발하면서 생긴 추가 작업, 범위 밖으로 미룬 요구사항, 스프린트에서 끝내지 못한 작업을 기록합니다.
> 기술적으로 빚진 것(임시 구현, 누락된 테스트 등)은 [기술부채](tech-debt.md)에 적습니다.
>
> [개발 관리](README.md)

## 작성 규칙

- 발견한 즉시 `new` 상태로 행을 추가하고 그 단계의 커밋에 포함한다. 우선순위는 비워 두고, 스프린트 종료 때 orchestrator가 정리한다. ID는 `BL-NNN`으로 순서대로 붙이고 재사용하지 않는다.
- 행은 지우지 않는다. 끝나거나 버린 항목은 상태만 바꾼다.
- **출처**: 어디서 생겼는지 (`PRD-001/FR-05`, `S01-T02`, PR `#12`)
- **우선순위**: `상` / `중` / `하`
- **상태**: `new`(미정리) · `open` · `planned:SNN`(스프린트에 편입) · `done` · `dropped`(비고에 이유)

## 목록

| ID | 제목 | 출처 | 우선순위 | 상태 | 비고 |
|---|---|---|---|---|---|
| BL-001 | 문서 점검 Node 스크립트(S01-T02)를 CI에 편입할지 검토 | S01 계획 리뷰 | 중 | open | S04 계획 리뷰에서 S04-T03(ci-cd) 편입 검토. 전제: BL-018 해결, BL-020/021 보완 (기존: S04-T02 또는 이후) |
| BL-002 | Aspire 9.x 지원 종료(NuGet 폐기) 대응: .NET 8 지원 종료(2026-11-10)와 함께 .NET 10 · Aspire 13 전환 여부 재검토 | S01-T01 | 상 | open | .NET 8 지원 종료(2026-11-10) 대응. PRD-001 범위 밖 결정(ADR-0001 대체 여부), 다음 토픽 후보 |
| BL-003 | 롤 생성은 초기화 스크립트로 분리하고 `WithCreationScript`는 `CREATE DATABASE ... OWNER` 한 문장만. Docker로 두 번 시작해 오류 로그 없음 검증 | S01-T01 | 상 | dropped | BL-014로 병합: 결정은 ADR-0011에 반영, 남은 실측만 BL-014 (기존: S01-T02 / S03) |
| BL-004 | 클라이언트 통합 vs `AddDbContext`+`UseNpgsql` 판단 근거에 NU1902(OpenTelemetry.Api 1.9.0 전이) 추가 | S01-T01 | 중 | done | ADR-0011 판단 근거에 NU1902 포함, 클라이언트 통합 미사용 결정 (기존: S01-T02) |
| BL-005 | 로컬 SDK 8.0.425 설치(현재 8.0.202 · 런타임 8.0.3, 패치 끊긴 밴드). S01-T05 빌드와 xunit.v3의 전제 조건(사용자 작업) | S01-T01 | 상 | done | SDK 8.0.425 설치 확인(S01-T05 새 clone에서 선택) (기존: T05 전) |
| BL-006 | Directory.Build.props에 `NuGetAuditMode=all`, Directory.Packages.props에 MessagePack 2.5.305 전이 고정, global.json 8.0.400 + latestFeature | S01-T01 | 상 | done | NuGetAuditMode=all, MessagePack 2.5.305, global.json 8.0.400 latestFeature (0f0a54a) (기존: S01-T05) |
| BL-007 | 로깅 ADR: ServiceDefaults OpenTelemetry는 템플릿 값(1.9.0) 대신 1.19.x 고정 | S01-T01 | 중 | done | ADR-0020에 OTel 1.19.x 고정 명시 (기존: S01-T03) |
| BL-008 | BL-004 판단 자료 추가: OpenTelemetry 1.19.x와 함께 쓰면 클라이언트 통합 NU1902 해소, TD-006도 해소 가능 | S01-T01 | 하 | done | 클라이언트 통합 미사용으로 불필요. BL-004와 함께 종결 (기존: S01-T02) |
| BL-009 | ServiceDiscovery · Http.Resilience 사용 여부 결정(서비스 1개) | S01-T01 | 중 | done | ADR-0011에 ServiceDiscovery · Http.Resilience 포함 결정 (기존: S01-T02 / T03) |
| BL-010 | package-versions.md 클라이언트 통합 행에 net8.0 대상 EF Core 의존 버전과 하한 명시(사용 여부 결정 시) | S01-T01 | 하 | done | package-versions.md 클라이언트 통합 미사용 · 의존 버전 기재 (5b94975) (기존: S01-T02) |
| BL-011 | NFR-05 증빙: PostgreSQL 이미지(PostgreSQL License) · .NET SDK(MIT) 라이선스 행 추가 검토 | S01-T01 | 중 | done | package-versions.md .NET SDK · PostgreSQL License 행 추가 |
| BL-012 | Aspire 리소스 이름에 밑줄 불가(ASPIRE006): `AddDatabase("<하이픈 이름>", databaseName: "emergency_hub_employee")`로 분리하고 ADR 0009 Write / Read 연결 키와 맞춤 | S01-T01 | 중 | done | ADR-0011 리소스 이름 · DB 이름 분리 결정. 구현은 S03-T04 완료 조건 (기존: S01-T02 / T05) |
| BL-013 | package-versions.md: Aspire.Npgsql.EntityFrameworkCore.PostgreSQL 9.5.2 및 암묵 추가 Aspire.Dashboard.Sdk · Orchestration 패키지의 폐기(Legacy) 표시 추가 | S01-T01 | 하 | done | package-versions.md 폐기(Legacy) 표시 (기존: S01-T02) |
| BL-014 | BL-003 실측을 S03 Aspire 구현 작업 완료 조건에 넣기: 새 볼륨 → 시작 → 중지 → 재시작, 리소스 로그 오류 0, pg_database 소유자 employee_app, public 소유자 pg_database_owner | S01-T02 | 상 | done | S03-T05 1530f60: 재시작 2회, 42P04 쌍 2개 · 3D000 1건만(판정에서 제외, 개수 기록), 42710 · 그 밖 ERROR/FATAL 0, 소유자 employee_app · public pg_database_owner · 이력 1행. 판정 기준 고정은 BL-096 |
| BL-015 | 서비스 DB가 2개 이상이 되면 `REVOKE CONNECT, TEMPORARY ON DATABASE emergency_hub_<service> FROM PUBLIC` 적용 | S01-T02 | 하 | open | 서비스 DB 2개 이상일 때. 두 번째 서비스 토픽 |
| BL-016 | package-versions.md: WithInitBindMount Obsolete(9.5.2) → WithInitFiles 표기, 클라이언트 통합 미사용 시 Npgsql.OpenTelemetry 8.0.9 · HealthChecks.EntityFrameworkCore 8.0.31 행 추가 | S01-T02 | 하 | done | package-versions.md WithInitFiles, Npgsql.OpenTelemetry, HealthChecks.EFCore 행 (기존: S01-T02) |
| BL-017 | local-setup / troubleshooting: "MigrationService가 Waiting 상태로 멈춤" = 생성 스크립트 실패, 해결은 이름 있는 볼륨 삭제 | S01-T02 | 중 | done | S04 종료 정리: S04 반영 완료(스프린트 문서 결과 리뷰). S04-T03 local-setup · troubleshooting (기존: S04) |
| BL-018 | raw-log(08-worklog/raw/*.md) frontmatter에 created · updated 없음: hook 템플릿에 추가할지, 필수 키 표에서 raw-log를 예외로 둘지 결정(BL-001 CI 편입 전제) | S01-T02 | 중 | open | check-docs 잔여 결함, BL-001 전제. hook 템플릿 수정은 토픽 밖 feature/* 작업 권장 |
| BL-019 | error-codes.md: 매핑 없는 23505(유니크 위반)용 공통 Conflict 코드 할당 | S01-T02 | 상 | done | S02-T07(64044bd): 3003 Common.UniqueConstraintViolated 추가, CommonErrorsTests · error-codes 같은 커밋 |
| BL-020 | check-docs.js에 유형별 추가 필드(adr의 supersedes 등) 점검 추가 검토(기존 ADR 0005 등이 결함으로 잡힘) | S01-T02 | 하 | dropped | BL-001로 병합(CI 편입 전 check-docs 보완) |
| BL-021 | check-docs.js: 링크 대상에 공백이 있거나 `<...>` 안에 공백이 있는 형식은 점검에서 빠짐(현재 해당 링크 없음). CI 편입 전 보완 검토 | S01-T02 | 하 | dropped | BL-001로 병합(CI 편입 전 check-docs 보완) |
| BL-022 | database.md 트랜잭션 절 "기본값(Read Committed)을 쓴다"를 ADR-0014 "Read Committed 명시"에 맞춰 정리 | S01-T02 | 하 | done | database.md "Read Committed를 명시한다" (ADR-0014) (기존: S01-T04) |
| BL-023 | 23505가 UoW에서 Result로 바뀌어도 EF가 CommandError · SaveChangesFailed를 Error로 먼저 기록(정상 경합 경로에 Error 로그 2건). ConfigureWarnings로 수준을 낮출지 결정 | S01-T03 | 중 | done | S03-T06 d2263d6: EF 실패 로그 3종 Debug, 23505 경로 EF Error 2 → 0, 변환되지 않는 예외는 전역 처리기 Error 1회 유지. ADR 대체 후보(ADR-0020 75행) |
| BL-024 | Aspire 대시보드 추적에서 db.connection_string 태그에 비밀번호가 없는지, 로그 · 추적에 파라미터 값이 없는지 실측 | S01-T03 | 상 | planned:S06 | S05-T01: PRD-002 편입(Q17 · NFR-04, 2026-09-28 사용자 결정). 자동 테스트(`ActivityListener`) S06-T06, 대시보드 수동 확인 S07-T04. NFR-06 관련. S03 계획 리뷰에서 S03-T04 완료 조건에 편입 권장 (기존: S03-T05) |
| BL-025 | coding-conventions CQRS 표의 Command 반환 `Result`를 `Result<Unit>`으로 수정(ADR-0015) | S01-T03 | 중 | done | coding-conventions Result<Unit> (기존: S01-T04) |
| BL-026 | package-versions에 Serilog.Sinks.Async · Serilog.Enrichers.Environment 버전 · 라이선스 행 추가 | S01-T03 | 하 | done | package-versions.md Serilog.Sinks.Async · Enrichers.Environment 행 (기존: S01-T04) |
| BL-027 | logging-observability에 반영: `Logging:LogLevel` 미사용, EF 로그 수준, 민감 데이터 규칙, 중복 방지 방식(ADR-0020) | S01-T03 | 중 | done | logging-observability ADR-0020 반영 (기존: S01-T04) |
| BL-028 | Mediator 로깅 데코레이터 로그 이벤트 ID를 공통 범위 1~999에서 할당하고 error-codes에 기록 | S01-T03 | 중 | done | S02-T02(b3a0039): 이벤트 ID 하위 범위(1 · 101~199 · 201~299 · 301~399) error-codes 기록 |
| BL-029 | 아키텍처 테스트 후보: Handler는 ISender 주입 금지, Validator는 IRepository · IReadRepository · IService 주입 금지 | S01-T03 | 중 | done | S02-T05(c2407f9): Handler ↛ ISender, Validator ↛ Repository · Service 규칙 구현. Employee 적용은 BL-085 |
| BL-030 | 헬스체크 경로 정리(logging-observability `/health/live` · `/health/ready` vs Aspire 템플릿 `/health` · `/alive`) | S01-T03 | 중 | done | S03-T03 19954f4: /health/live · /health/ready 모든 환경, 본문 상태만 |
| BL-031 | PRD-001 Q9 결론 문구가 ADR-0014 · 0015 구체화(데코레이터는 CommitAsync만, 실행 전략은 UoW 안)와 표현이 다름: "구체화: ADR-0014 · 0015" 주석 검토 | S01-T03 | 하 | done | PRD Q9 반영 열 "구체화: ADR-0014 · 0015" 주석 (기존: S01-T04) |
| BL-032 | clean-architecture.md의 `Endpoints/` · Minimal API 기본안을 `Controllers/`로 수정(ADR-0016) | S01-T03 | 중 | done | clean-architecture Controllers/ (기존: S01-T04) |
| BL-033 | 통합 테스트 fixture에서 employee_app 재현(슈퍼유저로 CREATE ROLE, CREATE DATABASE ... OWNER 한 문장씩), Respawn 이력 테이블 보존 · TRUNCATE 권한 · 읽기 연결 쓰기 거부(25006) 실측 | S01-T04 | 상 | done | S03-T06 d2263d6 · 88c470a: employee_app 재현(AppHost 초기화 스크립트 공유), Respawn 이력 보존 · TRUNCATE 권한, 25006 |
| BL-034 | HasData 기준 데이터 도입 시 해당 테이블을 Respawn TablesToIgnore에 추가하거나 초기화 뒤 재시드(database.md 시드 절에 규칙 명시) | S01-T04 | 하 | open | HasData 도입 계획 없음 |
| BL-035 | 순환 FK 금지: Respawn의 DISABLE TRIGGER ALL은 슈퍼유저 필요 → employee_app에서 실패. 스키마 리뷰 항목 추가 검토 | S01-T04 | 하 | open | 스키마 리뷰 규칙 문서 작업. 현재 FK 없음 |
| BL-036 | 마이그레이션 SQL 검토 때 이력 테이블이 정확히 `public."__EFMigrationsHistory"`인지 확인(TablesToIgnore 일치) | S01-T04 | 중 | done | S03-T02 a11883e: 이력 테이블 public."__EFMigrationsHistory", 스키마 한정자 없음 |
| BL-037 | CPM에 OpenTelemetry.Api 1.19.1 전이 고정(Npgsql.OpenTelemetry 8.0.9가 OpenTelemetry.API >= 1.6.0 의존, 미고정 시 NU1902) | S01-T04 | 상 | done | Directory.Packages.props OpenTelemetry.Api 1.19.1 전이 고정 (기존: S01-T05) |
| BL-038 | grep 대상 밖 문서(event-driven-architecture, service-communication, architecture-overview)의 보류 항목 🟡를 ADR-0023 링크 "보류"로 교체 | S01-T04 | 중 | done | S04 종료 정리: S04 반영 완료(스프린트 문서 결과 리뷰). S04 계획 확정: S04-T02 편입. BL-043 · 044 병합. S04 계획 리뷰에서 S04-T02 편입 검토 (기존: S04-T02) |
| BL-039 | coding-conventions Handler 예시의 `IIdGenerator.NewId()` 이름을 S02-T01 계약에 맞춤 | S01-T04 | 하 | done | S04 종료 정리: S04 반영 완료(스프린트 문서 결과 리뷰). S04-T02 coding-conventions 코드 일치(S02-T01에서 이름이 바뀌면 그때 수정 가능) (기존: S02-T01) |
| BL-040 | `Result<T>` 값 → `Result<T>` 암시적 변환 여부 확인(coding-conventions 예시 `return result.Value.Id;`) | S01-T04 | 중 | done | T → Result<T> 암시적 변환 구현 · 테스트 (df00b97) (기존: S01-T06) |
| BL-041 | package-versions.md 테스트 표 용도 열 "v3, 추천" / "v2, 대안"을 "v3, 채택" / "v2, 비교용 · 미사용"으로 | S01-T04 | 하 | done | package-versions 용도 열 채택 / 비교용 · 미사용 (26b2ce4) |
| BL-042 | clean-architecture.md:91 서비스 카탈로그 🟡(도메인 항목)를 02-domain 토픽에서 정리할지(BL-038 범위 편입 여부) | S01-T04 | 하 | open | 서비스 카탈로그 확정은 02-domain 토픽 |
| BL-043 | service-catalog.md:24 "API Gateway 🟡 검토 중"을 BL-038 또는 BL-042 범위에 넣어 ADR-0023 "보류"로 정리 | S01-T04 | 중 | dropped | BL-038로 병합 |
| BL-044 | security.md:71 "마이그레이션 전용 계정 🟡"를 ADR-0012 · database.md 롤 모델과 대조해 표시 정리 | S01-T04 | 하 | dropped | BL-038로 병합. 설계 부채는 TD-001 |
| BL-045 | 첫 마이그레이션 생성 뒤 `[**/Persistence/Migrations/*.cs]` 섹션(generated_code + CS1591 none)이 실제 EF 생성 파일에서 빌드 경고 0 · format 통과하는지 재확인 | S01-T05 | 중 | done | S03-T02: 생성 코드 포함 경고 0 · format 통과 |
| BL-046 | EF Design을 IDesignTimeDbContextFactory가 있는 Employee.Infrastructure에만 PrivateAssets=all로 참조하고 `dotnet ef --project/--startup-project` 통일 결정 | S01-T05 | 중 | done | S03-T02 17832a2: EF Design은 Infrastructure에만 PrivateAssets=all, --project/--startup-project 통일 |
| BL-047 | coding-conventions `.editorconfig` 표 · database.md 마이그레이션 규칙에 "CS1591은 generated_code로 꺼지지 않으므로 severity none 병기" 추가 | S01-T05 | 중 | done | coding-conventions · database.md CS1591 none 병기 (기존: S01-T05 / S04-T02) |
| BL-048 | clean-architecture 저장소 구조 트리에 tests/BuildingBlocks/, Directory.Build.targets, .config/dotnet-tools.json, .gitattributes 추가, 공통 빌드 설정 표에 IsTestProject 이름 규칙 · NuGetAudit · IVT · PrivateAssets 일괄 적용 반영 | S01-T05 | 중 | done | S04 종료 정리: S04 반영 완료(스프린트 문서 결과 리뷰). S04-T02 clean-architecture 저장소 구조 (기존: S04-T02) |
| BL-049 | local-setup / troubleshooting: Windows 깊은 경로 clone 시 MAX_PATH 초과로 MSB3101/MSB3030 실패 → 짧은 경로 또는 LongPathsEnabled 안내 | S01-T05 | 하 | done | S04 종료 정리: S04 반영 완료(스프린트 문서 결과 리뷰). S04-T03 local-setup 사전 준비(S04 계획 리뷰에서 문구 명시) (기존: S04) |
| BL-050 | BuildingBlocks.Domain 첫 코드 도입 시 골격 확인 테스트(BuildingBlocksDomainAssemblyTests) 대체 또는 삭제 | S01-T05 | 하 | done | 골격 확인 테스트 삭제, 실제 테스트로 대체 (df00b97) (기존: S01-T06) |
| BL-051 | IDE0005(사용하지 않는 using) 빌드 강제 여부 결정(GenerateDocumentationFile 필요) | S01-T05 | 하 | open | GenerateDocumentationFile 추가 설정 필요, 급하지 않음 |
| BL-052 | 공통 에러 코드는 BuildingBlocks.Domain CommonErrors(11개)에 정의됨: S02-T06은 새로 할당하지 말고 ErrorType → HTTP 매핑과 ValidationError → ProblemDetails errors(camelCase) 변환만 구현 | S01-T06 | 상 | done | S02-T06(6bee2d6): CommonErrors 재사용, 새 코드 할당 0 |
| BL-053 | 검증 데코레이터: CustomState에 Error가 없는 실패는 Error.Validation(1001, 메시지)로 감싸 FieldError.Create에 전달(FieldError는 검증 실패 유형만 받음) | S01-T06 | 중 | done | S02-T02(b3a0039): CustomState 없는 실패를 1001로 감쌈 |
| BL-054 | 아키텍처 테스트 후보: 서비스 코드에서 Error / Result 파생 금지, Entity/AggregateRoot 파생 클래스는 sealed | S01-T06 | 중 | dropped | BL-029로 병합 (기존: S02-T05) |
| BL-055 | coding-conventions에 경고 억제 규칙 명문화([SuppressMessage] + Justification 필수, 전역 NoWarn 금지, 승인 목록) | S01-T06 | 중 | done | S04 종료 정리: S04 반영 완료(스프린트 문서 결과 리뷰). S02 동안 reviewer 기준으로 운영(억제 승인 목록 기록). S04-T02에서 coding-conventions 명문화 |
| BL-056 | error-codes.md 또는 coding-conventions에 "ValidationError는 sealed"(non-sealed는 Error) 명시 — ADR 0018:22 문구 오해 방지 | S01-T06 | 하 | dropped | TD-016으로 병합(문서 문구 정리) |
| BL-057 | S03 CI 통합 테스트 대비: 통합 테스트 잡은 ubuntu 러너 고정 · services: 대신 Testcontainers, 이미지 pull 시간 단축(선 pull 또는 변형 검토, NFR-07), 컨테이너는 컬렉션 fixture로 공유(ADR-0022) | S01-T07 | 상 | done | S03-T06 d2263d6 CI 변경, S03 종료 PR CI 통과(run 36340112141, 약 4분 12초, 통합 129건 25초) |
| BL-058 | Dependabot(github-actions 생태계)으로 액션 SHA · 버전 주석 자동 갱신 검토 | S01-T07 | 중 | open | 액션 SHA 고정 ADR 후보와 함께 결정(security.md:85) |
| BL-059 | CI 소요 시간이 NFR-07 10분에 가까워지면(S03 Testcontainers 뒤) NuGet 캐시 재판단(packages.lock.json 또는 actions/cache) | S01-T07 | 하 | open | S04 DoD 판단: S04 HEAD CI 4분 7초로 7분 이하라 open 유지, 트리거는 CI 7분 초과. S03-T05 · S04-T01 소요 시간 측정 뒤 재판단 (기존: S03) |
| BL-060 | ReportGenerator 무료판 MarkdownSummaryGithub의 메서드 커버리지 "sponsors only" 표시(라인 · 분기는 정상, 기록만) | S01-T07 | 하 | dropped | 기록 목적. NFR-03 판정 영향 없음, S04-T03 ci-cd에 한 줄 언급 |
| BL-061 | AggregateRoot<TId> protected 매개변수 없는 생성자 미실행: EF용이면 S03에서 커버리지 제외 여부 판단 | S01-T07 | 하 | dropped | NFR-03 영향 없음. S03 EF 매핑에서 자연 해소, 80% 미만 시 재등록 (기존: S03) |
| BL-062 | ci.yml Coverage report 단계는 테스트 실패 시 건너뜀: 의도라면 testing-strategy CI 절에 명시 | S01-T07 | 하 | done | S04 종료 정리: S04 반영 완료(스프린트 문서 결과 리뷰). S04-T03 ci-cd 워크플로 단계 설명 |
| BL-063 | reportgenerator 입력 패턴 `TestResults/*/coverage.cobertura.xml`이 결과 폴더 구조 변화 시 0건이 될 수 있음: 테스트 프로젝트 증가 시 합산 대상 수 확인 | S01-T07 | 중 | done | S04 종료 정리: S04 반영 완료(스프린트 문서 결과 리뷰). S04-T01 커버리지 기록(S03-T05 테스트 프로젝트 추가 때 먼저 확인 권장) (기존: S03) |
| BL-064 | 로컬(8.0.425)과 러너 setup-dotnet SDK 버전이 다를 수 있음: DoD에서 러너 `dotnet --info` 로그로 확인 | S01-T07 | 중 | done | PR #7 CI run 36303017840 러너 SDK 8.0.425(로컬과 같음) |
| BL-065 | IService 마커가 BuildingBlocks.Application에 있어 Domain의 도메인 서비스 인터페이스가 상속할 수 없음(coding-conventions DI 표와 불일치): Domain용 마커를 둘지 문서를 고칠지 결정 | S02-T01 | 중 | open | S03 계획 리뷰: S03에 도메인 서비스가 없어 연기. 재검토 트리거 첫 도메인 서비스, 우선안은 IService를 BuildingBlocks.Domain으로 이동 (기존: planned:S03) |
| BL-066 | ADR-0018 본문의 Validator 형태(AbstractValidator<TRequest>)와 S02-T02에서 정한 공통 기반 RequestValidator<TRequest>(RuleLevelCascadeMode = Stop) 불일치: clean-architecture · testing-strategy 문구 정리, ADR 보충 여부 판단 | S02-T02 | 중 | done | S04 종료 정리: S04 반영 완료(스프린트 문서 결과 리뷰). S04-T02 기준 문서 정리 + ADR-0018 보충 ADR 여부 판단. BL-067 · 077 병합 |
| BL-067 | ADR-0013 '자동 등록' 문구와 ADR-0017 '명시 등록' 불일치: S02-T03에서 ADR-0017(명시, Scoped)로 해석. 해석 기록 위치(결과 리뷰 / S04-T02 명문화) 판단 | S02-T03 | 하 | dropped | BL-066에 병합. 해석(ADR-0017 명시 등록, Scoped)은 S02 결과 리뷰에 기록 |
| BL-068 | coverlet.runsettings Include에 BuildingBlocks.Infrastructure(및 Api)가 없음: 80% 보고 대상 포함 여부 결정 | S02-T03 | 중 | done | S04 종료 정리: S04 반영 완료(스프린트 문서 결과 리뷰). S04-T01에서 NFR-03 대상(BuildingBlocks에 Infrastructure · Api 포함 여부) 확정 후 coverlet Include 조정 |
| BL-069 | 테스트 Samples 형식의 파일 규칙: 한 파일 한 형식을 테스트에도 적용한다고 명문화할지, 예외(시나리오 묶음 파일)를 둘지 결정(결정 전에는 현행 규칙 적용) | S02-T03 | 하 | done | S04 종료 정리: S04 반영 완료(스프린트 문서 결과 리뷰). S02 반려 원인. S04-T02 coding-conventions에 테스트 코드 적용 명문화, 그 전까지 현행 규칙 적용 |
| BL-070 | owned가 아닌 하위 엔티티(같은 Aggregate, 별도 테이블)만 바뀌면 루트 행이 UPDATE되지 않아 루트 xmin 동시성 검사가 걸리지 않음: 루트 갱신 여부 · 소유 관계 판별 방법 결정 | S02-T04 | 중 | open | 재검토 조건: 별도 테이블 하위 엔티티를 가진 첫 Aggregate 설계 |
| BL-071 | 테이블 · 컬럼 · pk_ · fk_ · ix_ 이름의 63바이트 한도를 모델 생성 시 검사하는 공통 검증(현재 ux_ · ck_만 검사, EF 자동 잘림으로 23505 매핑과 어긋날 수 있음) | S02-T04 | 중 | open | PRD-001에서는 위험 낮음. S03-T02 dba 검토(BL-084)에서 63바이트 수동 확인, 공통 검증은 이후 토픽 |
| BL-072 | IStronglyTypedId<Other>처럼 형식 인자가 자기 자신이 아닌 구현은 변환기 등록에서 조용히 빠짐: 아키텍처 테스트나 분석기 규칙으로 막을지 검토 | S02-T04 | 하 | dropped | S02-T05에서 구현(IStronglyTypedId<TSelf> 자기 형식 규칙). 남은 enum 부분은 BL-079 |
| BL-073 | EnableRetryOnFailure 기본값(6회, 최대 지연 30초)이면 일시 장애 때 요청 하나가 수십 초 걸릴 수 있음: maxRetryCount · maxRetryDelay 조정 여부 | S02-T07 | 중 | done | S03-T06 d2263d6: Api 3회 · 5초(즉시 오류 약 4.3초, 기본값 약 57초) 확정, MigrationService 기본값, Api 요청 제한 시간 없음 기록 |
| BL-074 | IExceptionClassifier가 RetryLimitExceeded만 9003으로 분류: 실행 전략을 거치지 않은 일시 오류(NpgsqlException.IsTransient)는 9001이 됨. 분류 대상 확대 검토 | S02-T07 | 하 | open | PRD-001 범위에 해당 경로 없음. 나가는 호출 · 브로커가 생길 때 재검토 |
| BL-075 | ServiceDefaults Serilog 설정에서 Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware 범주를 MinimumLevel.Override로 끄기: Serilog는 Microsoft.Extensions.Logging 필터를 무시하므로 없으면 원본 예외 메시지(제약 이름 · SQL)가 Error로 남음 | S02-T06 | 상 | done | S03-T03 19954f4(Override Off, 설정으로 재활성 불가), S03-T07 H4 원본 메시지 0건. TD-022 open 유지 |
| BL-076 | 메시지를 남겨도 되는 예외 형식(예: ArgumentException) 허용 목록 정책: 현재 전역 예외 로그는 모든 예외 메시지를 뺀 사본(RedactedException)으로 기록 | S02-T06 | 하 | open | 현재 모든 예외 메시지를 빼 안전. 운영 진단 요구가 생길 때 결정 |
| BL-077 | ADR-0018은 [Flags] 조합 검사를 Phase 2로 적었으나 S02-T06(스프린트 완료 조건)에서 MustBeDefinedEnum으로 구현: 차이를 기록할 위치(후속 ADR / 결과 리뷰) 판단 | S02-T06 | 하 | dropped | BL-066에 병합 |
| BL-078 | 서비스끼리 프로젝트 참조 금지 아키텍처 규칙: 서비스가 2개 이상일 때만 대상이 생김(1개면 공허 통과) | S02-T05 | 하 | dropped | S02-T05 재작업(933aabb)에서 구현: 서비스 간 형식 규칙(서비스 2개 미만 건너뜀) + 선언 참조 격리 |
| BL-079 | [Flags] enum의 `: int` 명시 여부는 메타데이터로 구별 불가: Roslyn 분석기(구문 기준)로 강제할지 검토 | S02-T05 | 하 | open | Roslyn 분석기는 비용이 큼. developer 자체 점검 항목으로 먼저 완화(S02 회고) |
| BL-080 | 테스트 프로젝트 이름 규칙(TD-014)을 아키텍처 테스트로 볼 수 없음: sln 파싱 CI 점검 스크립트로 보완 검토 | S02-T05 | 하 | dropped | TD-014 상환 계획에 병합 |
| BL-081 | S03-T05 이관 영속성 · 트랜잭션 실측 묶음(S02 결과 리뷰 P1~P9): 실행 전략 + Read Committed, accept false 실효, 실패 뒤 롤백, 23505 ConstraintName · 201/202, xmin 충돌 3001 · owned 소유자 UPDATE, 재시도 한도 → 9003, Deleted 이벤트 비움, EF Error 로그 중복(BL-023), 커밋 응답 끊김 오보고(TD-010) | S02 결과 리뷰 | 상 | done | S03-T06 88c470a: P1~P8 통과. P9 → TD-010(open), P5 owned → BL-088(open) |
| BL-082 | S03-T05 이관 스키마 · 연결 실측 묶음(S1~S6): 감사 UTC 저장(+09:00), 읽기 연결 25006 → 9001 미노출, UUID v7 DB 정렬 · uuid · 기본값 없음, ck_ 위반 23514 · Flags 미정의 비트, 샘플 owned ck_ · bigint 마스크(BuildingBlocks 샘플로 할지 S03 계획 리뷰에서 결정), Respawn fixture employee_app · read-only 재현 | S02 결과 리뷰 | 상 | done | S03-T06: S1 · S2(DB) · S3 · S4(smallint) · S6 · 재적용 멱등, S03-T07: S2 응답 500 · 9001. S5 · S4 Flags → BL-088 |
| BL-083 | S03-T05 이관 HTTP · 로그 경로 실측 묶음(H1~H4): Kestrel 경로 바인딩 오류 1001 키, BadHttpRequest 본문 초과 400(TD-021), 응답 traceId = traceparent, Serilog OTLP 경로 ExceptionHandlerMiddleware 원본 메시지 미기록(BL-075) | S02 결과 리뷰 | 중 | done | H1 Kestrel · H2(S03-T05 curl, 413은 로그 302에만 → TD-021 현행), H1 TestServer · H3 · H4(S03-T07). 요청 줄 형식 오류는 Kestrel 본문 없는 400(기록만) |
| BL-084 | S03-T02 dba 마이그레이션 SQL 검토 항목: xmin 컬럼 생성 없음, ux_ · ck_ 이름 = 상수, ck_ Flags 마스크 괄호, created_at · updated_at timestamptz NOT NULL, id uuid 기본값 없음, 식별자 63바이트 이하(BL-071) | S02-T04 dba | 중 | done | S03-T02 a11883e: dba 재확인 (a)~(g) 판정 |
| BL-085 | 아키텍처 테스트 Employee 편입: ArchitectureAssemblies.All에 Employee Domain · Application · Infrastructure · Api · MigrationService + csproj 참조, 서비스 전용 규칙 10개 활성화, 제품 코드 위반 3종(Controller Repository 주입, MigrationService → Api, Handler ISender 주입) 재현 기록. MigrationService가 S03-T04에서 생기므로 편입 순서 결정 필요 | S02-T05 | 상 | done | S03-T04 bcba791: 5개 레이어 편입, 서비스 전용 규칙 건너뜀 0, 안전장치, 위반 3종 재현 기록 |
| BL-086 | ProductNames.ServiceOf가 EmergencyHub.AppHost 등 비서비스 프로젝트도 서비스로 판정: 제외 목록 추가 | S02-T05 reviewer | 하 | done | S03-T04: ProductNames AppHost 비서비스 판정 |
| BL-087 | S04-T02 문서 반영 묶음: clean-architecture 의존성 규칙 표 Api · MigrationService 행 + testing-strategy 아키텍처 테스트 절 동기화 + 규칙 Source 문자열 변경, database.md xmin 설명과 구현(IsConcurrencyToken + OnAddOrUpdate)이 같은 구성이라는 한 줄 | S02 결과 리뷰 | 중 | done | S04 종료 정리: S04 반영 완료(스프린트 문서 결과 리뷰). S04-T02 입력 |
| BL-088 | Flags · owned 실측(S03 계획 리뷰에서 이관): [Flags] 미정의 비트 ck_ 거부(BL-082 S4 Flags), owned ck_ · bigint 마스크(S5), owned 하위 값만 바뀔 때 소유자 UPDATE · xmin 충돌(BL-081 P5 owned) | S03 계획 리뷰 | 중 | open | 재검토 트리거: 제품 코드의 첫 [Flags] 코드 또는 owned 타입(예: Contact Network 알림 채널). 생성 SQL은 S02 설계 시점 단위 테스트로 확인됨 |
| BL-089 | 기준 문서가 S03 결정과 어긋남: coding-conventions DDD 구현 규칙(불변식 위반 시 Result 실패, Email.Create → Result 값 객체, CQRS 예시 Handler)과 S03 결정(Aggregate 불변식 위반은 예외, Email은 정규화한 string), logging-observability 20001 예시 템플릿과 실제 정의(Employee {EmployeeId} registered) | S03-T01 | 중 | done | S04 종료 정리: S04 반영 완료(스프린트 문서 결과 리뷰). S03 종료 정리: S04-T02 coding-conventions(DDD 규칙 · Email · CQRS 예시) · logging-observability(20001 템플릿)를 코드와 맞춤 |
| BL-090 | 아키텍처 규칙 ClassesAreSealed가 internal 생성 형식(EmployeeDbContextModelSnapshot, partial, sealed 아님)은 잡지 않고 public InitialCreate만 잡음: 규칙 범위(가시성)와 EF 생성 형식 처리 기준을 아키텍처 테스트 문서에 적을지 결정 | S03-T02 reviewer | 중 | open | S03 종료 정리: 공통 규칙(Migration · ModelSnapshot 파생 sealed)은 코드 작업이라 S04 밖. 트리거: 두 번째 마이그레이션 · ADR-0012 리셋 · 두 번째 서비스. 현재 규칙 범위(public만 잡음) 기록은 BL-087(S04-T02)에 함께 S04-T02 실측 정정: ClassesAreSealed는 가시성과 무관하게 internal 생성 형식(ModelSnapshot)도 잡음(sealed 제거 시 실패 재현). 남은 쟁점은 EF 생성 형식을 규칙 예외로 할지 |
| BL-091 | coding-conventions 코드값 규칙 '0은 None/Unknown 예약'의 적용 범위 명시: 프로세스 종료 코드처럼 외부 규약상 0이 의미 있는 internal enum(MigrationExitCode Succeeded=0)은 예외 | S03-T03 reviewer | 하 | done | S04 종료 정리: S04 반영 완료(스프린트 문서 결과 리뷰). S03 종료 정리: S04-T02 coding-conventions 코드값 규칙에 외부 규약상 0이 의미 있는 internal enum 예외 추가 |
| BL-092 | coding-conventions DI 규칙에 IHostedService(AddHostedService)를 Program에서 명시 등록해도 되는지 명시(현재 표는 서비스 · Repository · Handler만) | S03-T03 reviewer | 하 | done | S04 종료 정리: S04 반영 완료(스프린트 문서 결과 리뷰). S03 종료 정리: S04-T02 coding-conventions DI 표에 AddHostedService 명시 등록 허용 행 추가 |
| BL-093 | ADR-0006 결과 36행(developer는 단위 테스트 먼저)과 결정 41행(도메인 / 애플리케이션 로직)의 적용 범위 차이를 tdd-guide 레이어 표에 호스트 구성(ServiceDefaults 등) 행으로 명시 | S03-T03 reviewer | 하 | open | S03 종료 정리: tdd-guide는 S04-T02 대상 아님. PRD-001 /retro 기준 문서 개선안(레이어 표에 호스트 구성 행) |
| BL-094 | ADR-0020 EnableSensitiveDataLogging Development opt-in 경로 미구현: 공용 등록 확장 한 곳의 IsDevelopment() && Database:EnableSensitiveDataLogging 판단과 환경별 단위 테스트(ADR-0020 82행은 S02-T04 검증으로 적었으나 누락). 현재 모든 환경에서 꺼져 있어 안전 쪽 | S03-T04 dba | 중 | open | S03 종료 정리: 구현은 코드 작업이라 다음 토픽. 그 전까지 S04-T02 logging-observability에 미구현 · 꺼짐을 기록. 트리거: 로컬 SQL 파라미터 디버깅 필요 시 |
| BL-095 | 요청 로그 구성(UseSerilogRequestLogging + 헬스 경로 제외, Employee.Api RequestLogLevels)을 ServiceDefaults 공용 확장으로 이동 | S03-T04 developer | 하 | open | S03 종료 정리: 트리거 두 번째 서비스 Api |
| BL-096 | ADR-0011 롤 생성 조항 보충 후보: 재시작 때 생성 스크립트 42P04가 서버 로그에 ERROR로 남는다는 사실과 BL-014 리소스 로그 오류 0의 판정 기준(42P04 한 쌍 제외) | S03-T05 dba | 중 | open | S03 종료 정리: ADR 후보 ADR-0011 보충(PRD-001 /retro에서 목록 판단, 사용자 확인 뒤 작성 때 done). 42P04 · 3D000 잡음 troubleshooting 기록은 BL-017과 함께 S04-T03 |
| BL-097 | Aspire 9.5.2 AppHost가 실행할 때마다 버전 확인으로 네트워크에 접속하고 user-secrets에 Aspire:VersionCheck:* 키를 씀: ASPIRE_VERSION_CHECK_DISABLED 사용 여부 결정 | S03-T05 developer | 하 | done | S04 종료 정리: S04 반영 완료(스프린트 문서 결과 리뷰). S03 종료 정리: S04-T03 local-setup에 Aspire 버전 확인 · VersionCheck 키 기록, ASPIRE_VERSION_CHECK_DISABLED 결정 |
| BL-098 | 초기화 스크립트가 employee_app 비밀번호를 psql -v 명령줄 인자로 넘겨 실행 중 짧게 컨테이너 안 프로세스 목록에 보일 수 있음(로컬 전용): PGOPTIONS · 표준 입력 방식 전환 여부 | S03-T05 reviewer | 하 | open | S03 종료 정리: 로컬 전용 · 짧은 노출. 트리거: 공유 · 배포 환경에서 초기화 스크립트 사용(Phase 4, TD-001과 함께) |
| BL-099 | local-setup: https 기본 프로필은 dotnet dev-certs https --trust가 있어야 대시보드 로그 · 추적이 보임(없으면 OTLP TLS 실패로 0건). http 프로필 대안 · 확인 방법(dotnet dev-certs https --check --trust) 기록, 기본 프로필 순서 변경 여부 판단 | S03-T05 tester | 중 | done | S04 종료 정리: S04 반영 완료(스프린트 문서 결과 리뷰). S03 종료 정리: S04-T03 local-setup · troubleshooting. S04-T04 새 환경 재현(NFR-04) 실패 원인이 될 수 있음 |
| BL-100 | database.md · local-setup: 첫 실행 때 user-secrets에 AppHost:OtlpApiKey도 저장됨을 기록하고 초기화 절차 clear 대상에 포함 | S03-T05 tester | 하 | done | S04 종료 정리: S04 반영 완료(스프린트 문서 결과 리뷰). S03 종료 정리: S04-T03 local-setup 초기화 clear 대상에 AppHost:OtlpApiKey, database.md 복구 절차 한 줄 |
| BL-101 | 운영 PostgreSQL 서버 로그 DETAIL 노출: 기본 설정에서 23505 · 23514 때 Key (email)=(...)와 위반 행 전체가 서버 로그에 남음. log_error_verbosity=terse 등 서버 로그 설정 검토 | S03-T06 dba | 중 | open | S03 종료 정리: 트리거 배포 토픽(Phase 4) 또는 ADR-0023 로그 수집기 도입 |
| BL-102 | 로컬 Docker Engine API 1.44 미만(Docker Desktop 4.26 / Engine 24.0.7)이면 Testcontainers 4.15.0 연결 실패: local-setup에 최소 Docker 버전과 DOCKER_API_VERSION=1.43 대처 기록 또는 Docker Desktop 업그레이드 검토 | S03-T06 developer | 중 | done | S04 종료 정리: S04 반영 완료(스프린트 문서 결과 리뷰). S03 종료 정리: S04-T03 local-setup 사전 준비에 최소 Docker Engine API 1.44 / DOCKER_API_VERSION 대처. NFR-04 재현 전제 |
| BL-103 | MigrationService 실제 호스트(Program.Configure + MigrationWorker)를 컨테이너로 실행하는 통합 시나리오: Program · Configure가 internal이고 InternalsVisibleTo가 UnitTests만 허용해 참조 · 가시성 확장 필요 | S03-T03 tester · S03-T06 developer | 중 | open | S03 종료 정리: fixture가 같은 경로를 검증해 급하지 않음. 트리거: MigrationWorker 적용 코드 변경 또는 두 번째 서비스. 해결 때 TD-027 함께 상환 |
| BL-104 | 로그 수집기 · 알림 도입 때 알림 조건에 301 · ErrorCode 9003(재시도 한도 초과) 포함: BL-023 뒤 DB 장애는 Warning으로만 남아 Error 급증 규칙으로는 안 잡힘(logging-observability 196행 TODO) | S03-T06 reviewer | 중 | open | S03 종료 정리: 트리거 ADR-0023 로그 수집기 도입 |
| BL-105 | logging-observability 수준 표에 재시도 한도 초과(9003) = Warning(301) 예시를 적어 Outbox 발행 최종 실패 = Error 예시와의 차이 명시 | S03-T06 reviewer | 하 | done | S04 종료 정리: S04 반영 완료(스프린트 문서 결과 리뷰). S03 종료 정리: S04-T02 logging-observability 수준 표에 9003 = Warning(301) 예시 |
| BL-106 | 500 응답(GlobalExceptionHandler WriteAsJsonAsync)의 Content-Type에 charset=utf-8이 없어 MVC 결과(400 · 404 · 409)와 표기가 다름(동작 문제 아님, 일관성) | S03-T07 tester | 하 | open | S03 종료 정리: 트리거 api-guidelines 정비(TD-021 · BL-107과 함께) |
| BL-107 | swagger.json이 employee-api.md 실패 표의 500(9001) · 503(9003)을 선언하지 않음: 문서와 OpenAPI 범위 맞출지 판단 | S03-T07 tester | 하 | open | S03 종료 정리: 트리거 api-guidelines 정비. swagger 선언 또는 공통 응답 참조를 TD-021 · BL-106과 함께 결정 |
| BL-108 | DB 정지 상태 /health/ready가 연결 제한 시간만큼(약 15초) 걸려 503: 헬스 검사 제한 시간 별도 설정 여부(프로브 제한 시간) | S03-T07 tester | 중 | open | S03 종료 정리: 제한 시간 설정은 배포 토픽에서 프로브 주기와 함께. 실측값(DB 정지 약 15.0초 → 503, live 200)은 S04-T02 logging-observability 헬스체크 절에 기록 |
| BL-109 | 비밀 점검 명령 6개를 scripts/check-secrets.js로 스크립트화하고, 알려진 가짜 값은 고유 표식(do-not-leak 계열)과 경로 기준으로 제외: 문서 인용 때문에 제외 행이 늘어나는 문제 해소 | S03 결과 리뷰 | 중 | open | S04 계획 리뷰에서 S04-T03(ci-cd) 편입 판단(BL-001과 같은 CI 편입 축) |
| BL-110 | MigrationService가 Aspire에서 Environment=Production으로 뜸(launchSettings 없음): Api와 맞춰 Development로(AppHost DOTNET_ENVIRONMENT 주입 또는 launchSettings 추가) | S03 결과 리뷰 | 하 | done | S04 종료 정리: S04 반영 완료(스프린트 문서 결과 리뷰). S04-T03 environments 문서에서 결정 · 기록(오케스트레이션 세션 승인: Development로). 동작 영향 없음 |
| BL-111 | Aspire 스모크 테스트(Aspire.Hosting.Testing으로 AppHost 기동 → Api /health/ready · 마이그레이션 완료 확인) 도입. 재도입 조건: AppHost 테스트 모드(데이터 볼륨 · 매개변수 persist 끔) 마련, CI 3회 연속 통과, CI 추가 시간 최대 7분 | S04-T01 | 중 | open | S04 종료 정리: 재도입 조건 충족 때(BL-059와 같은 측정 축). S04 미도입 근거: 스프린트 안에 CI 러너에서 AppHost 기동 시간을 측정할 경로가 없음, 현재 AppHost는 개발용 데이터 볼륨과 user-secrets persist를 테스트와 공유함, S03 CI 기준선 약 4분 12초(NFR-07 10분 안 여유 판단 근거 없음). PRD-001 FR-03 선택 스모크 해석은 S04 계획 확정(인수 조건 해석) |
| BL-112 | 아키텍처 문서 3개(event-driven-architecture · service-communication · architecture-overview)에 남은 보류 아닌 🟡(Polly · Kubernetes = 이후 토픽, Identity · 사업자 · 서비스 후보 · 전파 속도 수치 · gRPC = PRD 결정) 표기를 tech-stack 처리 방식 표 기준으로 통일 | S04-T02 developer | 하 | open | S04 종료 정리: 해당 PRD 결정이 나오는 토픽에서. BL-038(보류 항목)과 범위 분리 |
| BL-113 | 설정 · 구성 코드(DI 등록, 호스트 환경 주입, 빌드 설정)의 "실패" 테스트 해석 기준(예: 부정 범위 확인)을 정할지 결정. 정하면 사용자 결정 후 agents.md 테스트 범위 절에 먼저 반영하고 testing-strategy는 요약 · 링크만 | S04-T02 reviewer | 하 | open | S04 종료 정리: PRD-001 /retro 개선안에서 사용자 결정, 그 전에는 현행 agents.md 테스트 범위 절 적용. S04-T02에서 완료 조건 밖으로 추가된 해석 문단은 반려로 제거 |
| BL-114 | local-setup 동작 확인: PowerShell 5.1 Invoke-RestMethod는 409 응답 본문(code 23001)을 예외로 바꿔 바로 보여 주지 않음. 재등록 결과 확인 방법(try/catch 또는 Invoke-WebRequest) 안내 검토 | S04-T03 tester | 하 | open | S04 종료 정리: 다음 local-setup 수정 때. |
| BL-115 | database.md · troubleshooting 42P04 판정 문구 "(실행 횟수 − 1)"에 풀이 추가: 컨테이너가 세션 수명이라 서버 로그는 실행별, 첫 실행 0 · 이후 실행마다 한 쌍 1개 | S04-T04 dba | 하 | open | S04 종료 정리: BL-116 병합, BL-096 결정 때 함께. |
| BL-116 | database.md psql 확인 절에 Git Bash heredoc 명령 틀(postgres · employee_app 접속, 11번 PGOPTIONS 읽기 전용 세션)과 PowerShell 5.1 인용 주의 추가 | S04-T04 dba | 하 | dropped | S04 종료 정리: BL-115에 병합. |
| BL-117 | 새 볼륨 첫 실행에서 employee-api의 첫 /health/ready가 Unhealthy(Error 2건, EventId 103 DefaultHealthCheckService, CanConnect=false · 예외 메시지 없음, 같은 시각 서버 로그 오류 없음)로 나오는 원인 조사. 3D000처럼 첫 실행 잡음으로 판정에서 뺄지, 로깅 · 헬스 검사를 손볼지 결정 | S04-T04 tester | 중 | open | S04 종료 정리: 트리거는 배포 토픽 프로브 설계(BL-108) 또는 AppHost 테스트 모드(BL-111). 원인 조사 → 잡음 제외 · 헬스 검사 수정 중 결정. 재현 조건: 볼륨 삭제 + user-secrets clear 뒤 1회차. 2 · 3회차와 이전 11회 시작에서는 0건 |
| BL-118 | 대시보드 DOM 증빙 방법 문서화: Edge --dump-dom은 Blazor 대화형 렌더링 전에 끝나 본문이 비므로 원격 디버깅(CDP)으로 렌더링 뒤 DOM을 덤프해야 함 | S04-T04 tester | 하 | open | S04 종료 정리: 범위 확장 — CDP DOM 덤프, DCP API(/apis/usvc-dev.developer.microsoft.com/v1/executables) MigrationService exitCode 조회 문서화, 스크립트화 여부 판단. |
| BL-119 | 아키텍처 테스트로 기계화: 한 파일 한 최상위 형식, Repository 본문 제약(람다 LINQ만), 테스트 enum 기반 형식 명시, Controller 서비스 로케이터 검출(BL-079 · TD-025와 함께 판단) | RETRO-PRD-001 | 중 | open | 회고 개선안 18 보류. 다음 토픽 계획 리뷰에서 편입 판단 |
| BL-120 | check-docs.js에 완료 조건 FR ↔ 증빙 표 행 교차 점검 옵션 추가 | RETRO-PRD-001 | 하 | open | 회고 개선안 19 보류. BL-001 · 018과 묶어 판단 |
| BL-121 | 동명이인 전체 조회 · 식별: 같은 이름의 직원 목록 반환, ID 조회 엔드포인트 또는 v2 | PRD-002 범위 밖 (Q3 · FR-08) | | new | PRD-002 FR-08의 `GET /api/employee/{name}`은 동명이인 중 입사일이 가장 빠른 1명만 반환 |
| BL-122 | CSV 헤더 행 지원 | PRD-002 범위 밖 (Q7 · FR-03) | | new | PRD-002 FR-03은 헤더 없음만 지원(헤더를 넣으면 날짜 형식 오류로 거부). 기술부채 TD-028과 짝 |
| BL-123 | CP949(Excel 기본 저장) CSV 지원 | PRD-002 범위 밖 (FR-03) | | new | PRD-002 FR-03은 엄격 UTF-8(BOM 허용)만 받고 CP949 바이트는 400 |
| BL-124 | `/`가 들어간 이름(`%2F`)의 경로 조회 | PRD-002 범위 밖 (FR-08) | | new | PRD-002 FR-08에서 보장하지 않음 |
| BL-125 | 직원 개인정보 보존 기간 · 삭제 정책 | PRD-002 범위 밖 | | new | |
| BL-126 | 직원 정보 수정 · 삭제 API | PRD-002 범위 밖 | | new | PRD-002는 등록 · 조회만 |
| BL-127 | 조직 · 부서 정보 | PRD-002 범위 밖 | | new | 로드맵 Phase 2 후보 토픽(인증 / 직원 · 조직 관리)과 함께 판단 |
| BL-128 | roadmap.md Phase 1 표와 일정표의 PRD-001 행이 '진행 중 / v0.1.0 (예정)'으로 남아 있음(v0.1.0 태그 2987cdc 존재). `/retro` 종료 때 갱신 누락 | S05-T01 developer | | new | 완료 조건 밖이라 S05-T01에서 고치지 않음 |
| BL-129 | Email local 부분의 제어 문자(예: U+0001)를 거부할지: 현재 규칙(`@` 하나 · 공백 없음 · domain `.`)으로 거르지 않음 | S05-T03 developer | | new | PRD-002 FR-01 규칙 밖이라 구현하지 않음. PRD 개정 여부 판단. S05-T03 tester: NUL(U+0000) · 짝 없는 서로게이트도 통과하며, PostgreSQL text는 NUL을 저장할 수 없어 일괄 등록 저장에서 500이 날 가능성(미실측) |
| BL-130 | Value Object record 기본 ToString이 값을 출력(`Name { Value = 홍길동 }`): 로그 템플릿 인자로 넘기면 개인정보가 남음. ToString 재정의 또는 로그 인자 금지 규칙 검토 | S05-T03 developer | | new | NFR-04 · FR-10 개인정보 테스트(S06-T06)와 관련 |
| BL-131 | testing-strategy 규칙 후보: xUnit v3 MemberData / InlineData는 발견 단계 직렬화에서 짝 없는 서로게이트 문자열을 바꿀 수 있으므로 `DisableDiscoveryEnumeration = true`와 입력 보존 단언을 둔다 | S05-T03 developer | | new | S05-T03 실측(21009 사례 4건 실패 후 수정) |
| BL-132 | logging-observability.md(87 · 93 · 152행) · tdd-guide.md(51 · 71행) 예시가 PRD-001 샘플 이름(RegisterEmployeeCommandHandler 등)을 씀 | S05-T04 developer | | new | 형식 설명이라 S05에서는 두었음. S06-T04 Handler 예시 교체 때 함께 |
| BL-133 | IntegrationTests `Http/HttpProblem.cs` · `LogEventText.cs`가 HTTP 테스트 삭제로 호출처 0 | S05-T04 developer | | new | S06-T06 HTTP 테스트에서 다시 쓰거나 삭제 |
| BL-134 | employee-api.md 83행 409 설명이 옛 `ux_employees_email`로 남아 있음(샘플 API 제거된 PRD-001 문서) | S05-T05 reviewer | | new | S06 API 문서 작업에서 다시 쓸 대상 |
| BL-135 | database.md '알려진 잡음 로그' 표가 BL-117(첫 /health/ready Unhealthy Error 2건)을 '잡음 아님 · 제외하지 않음'으로 두어, RETRO-PRD-001 유지 판단(BL-117 잡음 제외) · S05 계획 인계 메모와 다름. 한쪽으로 맞춤 | S05-T05 tester | | new | S05-T05에서는 승인된 인계 메모 기준으로 개수 기록 후 제외. S05-T05 관찰: Api 시작 약 2.6초 뒤 첫 요청, EmployeeDbContext 924 ms · ReadDbContext 108 ms, 두 번째 요청부터 200 |

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-27 | - | `new` 상태 추가: 발견 즉시 기록, 스프린트 종료 때 정리 |
| 2026-09-27 | - | S01 종료 정리: BL-001~064 (done 22, 병합 7, planned 18, open 15, dropped 2) |
| 2026-09-27 | orchestrator | S02 종료 정리: BL-065~080 정리(done 5 · dropped 6 · planned 9 · open 6), 인계 메모로 BL-081~087 추가(S03-T05 실측 묶음 3, S03-T02 · 아키텍처 편입 · AppHost 제외 · S04 문서 묶음), `new` 0 |
| 2026-09-28 | orchestrator | S03 종료 정리: BL-089~108 정리(planned:S04 8 · open 12), S03 편입분 done 15 · BL-057은 CI 판정 대기, 결과 리뷰에서 BL-109 · 110 추가, `new` 0 |
| 2026-09-28 | orchestrator | S04 계획 확정: BL-038 open → planned:S04(S04-T02), BL-110 배정을 S04-T01(코드) · S04-T03(기록)으로 나눔 |
| 2026-09-28 | developer | S04-T01: Aspire 스모크 미도입 근거 · 재도입 조건으로 BL-111 추가(`new`) |
| 2026-09-28 | orchestrator | S04 종료 정리: BL-111~118 정리(open 7 · dropped 1(BL-116 → BL-115)), planned:S04 21건 done, `new` 0 |
| 2026-09-28 | orchestrator | RETRO-PRD-001: 보류 개선안 BL-119 · 120 추가(open) |
| 2026-09-28 | developer | S05-T01: PRD-002 범위 밖 백로그 후보 BL-121~127 추가(`new`), BL-024 open → planned:S06 |
