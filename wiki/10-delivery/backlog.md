---
title: "백로그"
type: index
status: stable
tags: [delivery, backlog]
aliases: [Backlog]
created: 2026-09-27
updated: 2026-09-27
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
| BL-014 | BL-003 실측을 S03 Aspire 구현 작업 완료 조건에 넣기: 새 볼륨 → 시작 → 중지 → 재시작, 리소스 로그 오류 0, pg_database 소유자 employee_app, public 소유자 pg_database_owner | S01-T02 | 상 | planned:S03 | S03-T04. BL-003 병합. 재시작 오류 0 · 소유자 확인 문구는 S03 계획 리뷰에서 보강 (기존: S03) |
| BL-015 | 서비스 DB가 2개 이상이 되면 `REVOKE CONNECT, TEMPORARY ON DATABASE emergency_hub_<service> FROM PUBLIC` 적용 | S01-T02 | 하 | open | 서비스 DB 2개 이상일 때. 두 번째 서비스 토픽 |
| BL-016 | package-versions.md: WithInitBindMount Obsolete(9.5.2) → WithInitFiles 표기, 클라이언트 통합 미사용 시 Npgsql.OpenTelemetry 8.0.9 · HealthChecks.EntityFrameworkCore 8.0.31 행 추가 | S01-T02 | 하 | done | package-versions.md WithInitFiles, Npgsql.OpenTelemetry, HealthChecks.EFCore 행 (기존: S01-T02) |
| BL-017 | local-setup / troubleshooting: "MigrationService가 Waiting 상태로 멈춤" = 생성 스크립트 실패, 해결은 이름 있는 볼륨 삭제 | S01-T02 | 중 | planned:S04 | S04-T03 local-setup · troubleshooting (기존: S04) |
| BL-018 | raw-log(08-worklog/raw/*.md) frontmatter에 created · updated 없음: hook 템플릿에 추가할지, 필수 키 표에서 raw-log를 예외로 둘지 결정(BL-001 CI 편입 전제) | S01-T02 | 중 | open | check-docs 잔여 결함, BL-001 전제. hook 템플릿 수정은 토픽 밖 feature/* 작업 권장 |
| BL-019 | error-codes.md: 매핑 없는 23505(유니크 위반)용 공통 Conflict 코드 할당 | S01-T02 | 상 | planned:S02 | S02-T07 공통 코드 3003 Common.UniqueConstraintViolated(Conflict 409) 추가. S02 계획 리뷰에서 T06 → T07로 이동 |
| BL-020 | check-docs.js에 유형별 추가 필드(adr의 supersedes 등) 점검 추가 검토(기존 ADR 0005 등이 결함으로 잡힘) | S01-T02 | 하 | dropped | BL-001로 병합(CI 편입 전 check-docs 보완) |
| BL-021 | check-docs.js: 링크 대상에 공백이 있거나 `<...>` 안에 공백이 있는 형식은 점검에서 빠짐(현재 해당 링크 없음). CI 편입 전 보완 검토 | S01-T02 | 하 | dropped | BL-001로 병합(CI 편입 전 check-docs 보완) |
| BL-022 | database.md 트랜잭션 절 "기본값(Read Committed)을 쓴다"를 ADR-0014 "Read Committed 명시"에 맞춰 정리 | S01-T02 | 하 | done | database.md "Read Committed를 명시한다" (ADR-0014) (기존: S01-T04) |
| BL-023 | 23505가 UoW에서 Result로 바뀌어도 EF가 CommandError · SaveChangesFailed를 Error로 먼저 기록(정상 경합 경로에 Error 로그 2건). ConfigureWarnings로 수준을 낮출지 결정 | S01-T03 | 중 | open | S02 계획 리뷰: 편입하지 않음. S03-T05 실측 후 결정(ConfigureWarnings는 이벤트 ID 단위라 실제 오류도 가림) |
| BL-024 | Aspire 대시보드 추적에서 db.connection_string 태그에 비밀번호가 없는지, 로그 · 추적에 파라미터 값이 없는지 실측 | S01-T03 | 상 | open | NFR-06 관련. S03 계획 리뷰에서 S03-T04 완료 조건에 편입 권장 (기존: S03-T05) |
| BL-025 | coding-conventions CQRS 표의 Command 반환 `Result`를 `Result<Unit>`으로 수정(ADR-0015) | S01-T03 | 중 | done | coding-conventions Result<Unit> (기존: S01-T04) |
| BL-026 | package-versions에 Serilog.Sinks.Async · Serilog.Enrichers.Environment 버전 · 라이선스 행 추가 | S01-T03 | 하 | done | package-versions.md Serilog.Sinks.Async · Enrichers.Environment 행 (기존: S01-T04) |
| BL-027 | logging-observability에 반영: `Logging:LogLevel` 미사용, EF 로그 수준, 민감 데이터 규칙, 중복 방지 방식(ADR-0020) | S01-T03 | 중 | done | logging-observability ADR-0020 반영 (기존: S01-T04) |
| BL-028 | Mediator 로깅 데코레이터 로그 이벤트 ID를 공통 범위 1~999에서 할당하고 error-codes에 기록 | S01-T03 | 중 | planned:S02 | S02-T02 로깅 데코레이터(이벤트 ID 할당 포함) (기존: S02-T02) |
| BL-029 | 아키텍처 테스트 후보: Handler는 ISender 주입 금지, Validator는 IRepository · IReadRepository · IService 주입 금지 | S01-T03 | 중 | planned:S02 | BL-054 병합. S02-T05(위반 예시 타입으로 구조 확인, Employee 어셈블리 적용은 S03) |
| BL-030 | 헬스체크 경로 정리(logging-observability `/health/live` · `/health/ready` vs Aspire 템플릿 `/health` · `/alive`) | S01-T03 | 중 | planned:S03 | S03-T04 ServiceDefaults 헬스체크 (기존: S03-T04) |
| BL-031 | PRD-001 Q9 결론 문구가 ADR-0014 · 0015 구체화(데코레이터는 CommitAsync만, 실행 전략은 UoW 안)와 표현이 다름: "구체화: ADR-0014 · 0015" 주석 검토 | S01-T03 | 하 | done | PRD Q9 반영 열 "구체화: ADR-0014 · 0015" 주석 (기존: S01-T04) |
| BL-032 | clean-architecture.md의 `Endpoints/` · Minimal API 기본안을 `Controllers/`로 수정(ADR-0016) | S01-T03 | 중 | done | clean-architecture Controllers/ (기존: S01-T04) |
| BL-033 | 통합 테스트 fixture에서 employee_app 재현(슈퍼유저로 CREATE ROLE, CREATE DATABASE ... OWNER 한 문장씩), Respawn 이력 테이블 보존 · TRUNCATE 권한 · 읽기 연결 쓰기 거부(25006) 실측 | S01-T04 | 상 | planned:S03 | S03-T05(Respawn, 읽기 연결 쓰기 거부, employee_app 재현) (기존: S03-T05) |
| BL-034 | HasData 기준 데이터 도입 시 해당 테이블을 Respawn TablesToIgnore에 추가하거나 초기화 뒤 재시드(database.md 시드 절에 규칙 명시) | S01-T04 | 하 | open | HasData 도입 계획 없음 |
| BL-035 | 순환 FK 금지: Respawn의 DISABLE TRIGGER ALL은 슈퍼유저 필요 → employee_app에서 실패. 스키마 리뷰 항목 추가 검토 | S01-T04 | 하 | open | 스키마 리뷰 규칙 문서 작업. 현재 FK 없음 |
| BL-036 | 마이그레이션 SQL 검토 때 이력 테이블이 정확히 `public."__EFMigrationsHistory"`인지 확인(TablesToIgnore 일치) | S01-T04 | 중 | planned:S03 | S03-T02 idempotent SQL dba 검토 (기존: S03-T02) |
| BL-037 | CPM에 OpenTelemetry.Api 1.19.1 전이 고정(Npgsql.OpenTelemetry 8.0.9가 OpenTelemetry.API >= 1.6.0 의존, 미고정 시 NU1902) | S01-T04 | 상 | done | Directory.Packages.props OpenTelemetry.Api 1.19.1 전이 고정 (기존: S01-T05) |
| BL-038 | grep 대상 밖 문서(event-driven-architecture, service-communication, architecture-overview)의 보류 항목 🟡를 ADR-0023 링크 "보류"로 교체 | S01-T04 | 중 | open | BL-043 · 044 병합. S04 계획 리뷰에서 S04-T02 편입 검토 (기존: S04-T02) |
| BL-039 | coding-conventions Handler 예시의 `IIdGenerator.NewId()` 이름을 S02-T01 계약에 맞춤 | S01-T04 | 하 | planned:S04 | S04-T02 coding-conventions 코드 일치(S02-T01에서 이름이 바뀌면 그때 수정 가능) (기존: S02-T01) |
| BL-040 | `Result<T>` 값 → `Result<T>` 암시적 변환 여부 확인(coding-conventions 예시 `return result.Value.Id;`) | S01-T04 | 중 | done | T → Result<T> 암시적 변환 구현 · 테스트 (df00b97) (기존: S01-T06) |
| BL-041 | package-versions.md 테스트 표 용도 열 "v3, 추천" / "v2, 대안"을 "v3, 채택" / "v2, 비교용 · 미사용"으로 | S01-T04 | 하 | done | package-versions 용도 열 채택 / 비교용 · 미사용 (26b2ce4) |
| BL-042 | clean-architecture.md:91 서비스 카탈로그 🟡(도메인 항목)를 02-domain 토픽에서 정리할지(BL-038 범위 편입 여부) | S01-T04 | 하 | open | 서비스 카탈로그 확정은 02-domain 토픽 |
| BL-043 | service-catalog.md:24 "API Gateway 🟡 검토 중"을 BL-038 또는 BL-042 범위에 넣어 ADR-0023 "보류"로 정리 | S01-T04 | 중 | dropped | BL-038로 병합 |
| BL-044 | security.md:71 "마이그레이션 전용 계정 🟡"를 ADR-0012 · database.md 롤 모델과 대조해 표시 정리 | S01-T04 | 하 | dropped | BL-038로 병합. 설계 부채는 TD-001 |
| BL-045 | 첫 마이그레이션 생성 뒤 `[**/Persistence/Migrations/*.cs]` 섹션(generated_code + CS1591 none)이 실제 EF 생성 파일에서 빌드 경고 0 · format 통과하는지 재확인 | S01-T05 | 중 | planned:S03 | S03-T02 생성 코드 포함 빌드 경고 0(FR-02 재검증) (기존: S03-T02) |
| BL-046 | EF Design을 IDesignTimeDbContextFactory가 있는 Employee.Infrastructure에만 PrivateAssets=all로 참조하고 `dotnet ef --project/--startup-project` 통일 결정 | S01-T05 | 중 | planned:S03 | S03-T02 설계 시점 팩터리 · 초기 마이그레이션 (기존: S03) |
| BL-047 | coding-conventions `.editorconfig` 표 · database.md 마이그레이션 규칙에 "CS1591은 generated_code로 꺼지지 않으므로 severity none 병기" 추가 | S01-T05 | 중 | done | coding-conventions · database.md CS1591 none 병기 (기존: S01-T05 / S04-T02) |
| BL-048 | clean-architecture 저장소 구조 트리에 tests/BuildingBlocks/, Directory.Build.targets, .config/dotnet-tools.json, .gitattributes 추가, 공통 빌드 설정 표에 IsTestProject 이름 규칙 · NuGetAudit · IVT · PrivateAssets 일괄 적용 반영 | S01-T05 | 중 | planned:S04 | S04-T02 clean-architecture 저장소 구조 (기존: S04-T02) |
| BL-049 | local-setup / troubleshooting: Windows 깊은 경로 clone 시 MAX_PATH 초과로 MSB3101/MSB3030 실패 → 짧은 경로 또는 LongPathsEnabled 안내 | S01-T05 | 하 | planned:S04 | S04-T03 local-setup 사전 준비(S04 계획 리뷰에서 문구 명시) (기존: S04) |
| BL-050 | BuildingBlocks.Domain 첫 코드 도입 시 골격 확인 테스트(BuildingBlocksDomainAssemblyTests) 대체 또는 삭제 | S01-T05 | 하 | done | 골격 확인 테스트 삭제, 실제 테스트로 대체 (df00b97) (기존: S01-T06) |
| BL-051 | IDE0005(사용하지 않는 using) 빌드 강제 여부 결정(GenerateDocumentationFile 필요) | S01-T05 | 하 | open | GenerateDocumentationFile 추가 설정 필요, 급하지 않음 |
| BL-052 | 공통 에러 코드는 BuildingBlocks.Domain CommonErrors(11개)에 정의됨: S02-T06은 새로 할당하지 말고 ErrorType → HTTP 매핑과 ValidationError → ProblemDetails errors(camelCase) 변환만 구현 | S01-T06 | 상 | planned:S02 | S02-T06. 완료 조건을 "CommonErrors 재사용, 새 할당 없음"으로 수정(누락분 3003은 S02-T07) |
| BL-053 | 검증 데코레이터: CustomState에 Error가 없는 실패는 Error.Validation(1001, 메시지)로 감싸 FieldError.Create에 전달(FieldError는 검증 실패 유형만 받음) | S01-T06 | 중 | planned:S02 | S02-T02 검증 데코레이터 (기존: S02-T02) |
| BL-054 | 아키텍처 테스트 후보: 서비스 코드에서 Error / Result 파생 금지, Entity/AggregateRoot 파생 클래스는 sealed | S01-T06 | 중 | dropped | BL-029로 병합 (기존: S02-T05) |
| BL-055 | coding-conventions에 경고 억제 규칙 명문화([SuppressMessage] + Justification 필수, 전역 NoWarn 금지, 승인 목록) | S01-T06 | 중 | open | S02 계획 리뷰에서 reviewer 기준 합의(S02 계획 리뷰 절). 명문화는 S04-T02 |
| BL-056 | error-codes.md 또는 coding-conventions에 "ValidationError는 sealed"(non-sealed는 Error) 명시 — ADR 0018:22 문구 오해 방지 | S01-T06 | 하 | dropped | TD-016으로 병합(문서 문구 정리) |
| BL-057 | S03 CI 통합 테스트 대비: 통합 테스트 잡은 ubuntu 러너 고정 · services: 대신 Testcontainers, 이미지 pull 시간 단축(선 pull 또는 변형 검토, NFR-07), 컨테이너는 컬렉션 fixture로 공유(ADR-0022) | S01-T07 | 상 | planned:S03 | S03-T05 CI 통과 조건(NFR-07) (기존: S03) |
| BL-058 | Dependabot(github-actions 생태계)으로 액션 SHA · 버전 주석 자동 갱신 검토 | S01-T07 | 중 | open | 액션 SHA 고정 ADR 후보와 함께 결정(security.md:85) |
| BL-059 | CI 소요 시간이 NFR-07 10분에 가까워지면(S03 Testcontainers 뒤) NuGet 캐시 재판단(packages.lock.json 또는 actions/cache) | S01-T07 | 하 | open | S03-T05 · S04-T01 소요 시간 측정 뒤 재판단 (기존: S03) |
| BL-060 | ReportGenerator 무료판 MarkdownSummaryGithub의 메서드 커버리지 "sponsors only" 표시(라인 · 분기는 정상, 기록만) | S01-T07 | 하 | dropped | 기록 목적. NFR-03 판정 영향 없음, S04-T03 ci-cd에 한 줄 언급 |
| BL-061 | AggregateRoot<TId> protected 매개변수 없는 생성자 미실행: EF용이면 S03에서 커버리지 제외 여부 판단 | S01-T07 | 하 | dropped | NFR-03 영향 없음. S03 EF 매핑에서 자연 해소, 80% 미만 시 재등록 (기존: S03) |
| BL-062 | ci.yml Coverage report 단계는 테스트 실패 시 건너뜀: 의도라면 testing-strategy CI 절에 명시 | S01-T07 | 하 | planned:S04 | S04-T03 ci-cd 워크플로 단계 설명 |
| BL-063 | reportgenerator 입력 패턴 `TestResults/*/coverage.cobertura.xml`이 결과 폴더 구조 변화 시 0건이 될 수 있음: 테스트 프로젝트 증가 시 합산 대상 수 확인 | S01-T07 | 중 | planned:S04 | S04-T01 커버리지 기록(S03-T05 테스트 프로젝트 추가 때 먼저 확인 권장) (기존: S03) |
| BL-064 | 로컬(8.0.425)과 러너 setup-dotnet SDK 버전이 다를 수 있음: DoD에서 러너 `dotnet --info` 로그로 확인 | S01-T07 | 중 | done | PR #7 CI run 36303017840 러너 SDK 8.0.425(로컬과 같음) |

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-27 | - | `new` 상태 추가: 발견 즉시 기록, 스프린트 종료 때 정리 |
| 2026-09-27 | - | S01 종료 정리: BL-001~064 (done 22, 병합 7, planned 18, open 15, dropped 2) |
