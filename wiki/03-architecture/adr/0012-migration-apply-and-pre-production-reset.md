---
title: "ADR-0012: 마이그레이션 적용 방식과 운영 전 리셋 정책"
type: adr
adr: "0012"
status: accepted
date: 2026-09-27
deciders: []
supersedes:
superseded_by:
aliases: [ADR-0012]
tags: [adr, architecture, database]
created: 2026-09-27
updated: 2026-09-27
---

# ADR-0012: 마이그레이션 적용 방식과 운영 전 리셋 정책

## 배경 (Context)

- 로컬에서는 명령 하나로 DB · 마이그레이션 · Api가 떠야 한다(FR-03, [ADR-0011](0011-use-aspire-local-orchestration.md)). [데이터베이스 · 마이그레이션 규칙](../../04-development/database.md#마이그레이션-규칙)은 로컬에서만 시작 시 적용을 허용하고, 운영에서는 번들 / 스크립트로 적용한다.
- Api가 시작할 때마다 `Migrate`를 부르면 인스턴스끼리 경합하고 Api가 DDL 책임을 갖는다. EF Core 8의 `MigrateAsync`에는 마이그레이션 잠금이 없다(EF Core 9에서 추가, TD-011). 따라서 적용 주체는 하나여야 한다([PRD-001 질문과 답변](../../10-delivery/prd/PRD-001-foundation.md#질문과-답변) Q10).
- 기반 구축 동안에는 스키마가 자주 바뀐다. 지금 지속되는 DB는 로컬 볼륨과 테스트 컨테이너뿐이라, 마이그레이션을 처음부터 다시 만드는 비용이 볼륨 삭제 정도다(PRD Q13). 반면 "적용(push)된 마이그레이션은 고치지 않는다"는 불변 규칙과의 관계를 정해 두지 않으면 리셋이 규칙 위반처럼 보인다.

## 검토한 대안 (Options)

적용 방식:

1. **Api 시작 시 `Migrate`**: 장점: 가장 단순하다. 단점: 인스턴스 간 경합, Api에 DDL 책임이 섞인다.
2. **수동 `dotnet ef database update`**: 장점: 적용 시점을 사람이 통제한다. 단점: FR-03 "명령 하나로 실행"을 충족하지 못한다.
3. **별도 MigrationService(Worker) 1개가 적용 후 종료, Api는 완료 대기**: 장점: 적용 주체가 하나이고 Api는 DDL을 하지 않는다. 단점: 실행 프로젝트가 하나 늘어난다.

운영 전 이력 관리:

- (a) **리셋 금지(누적)**: 규칙이 단순하다. 단점: 초기 설계 시행착오가 마이그레이션 이력으로 쌓인다.
- (b) **운영 배포 전까지 전체 리셋 허용**: 단점: 리셋 때 모든 개발자가 로컬 볼륨을 지워야 한다.

## 결정 (Decision)

**적용 방식은 대안 3, 이력 관리는 (b)를 채택한다.**

적용 방식:

- **로컬**: MigrationService(`employee-migrations`) 하나가 Write 연결로 `strategy.ExecuteAsync(() => db.Database.MigrateAsync())`만 실행하고 종료한다. 성공이면 종료 코드 0, 실패면 0이 아닌 값(`Environment.ExitCode`를 명시적으로 설정)으로 끝나야 Api가 뜨지 않는다(`WaitForCompletion`).
- **DB 생성은 AppHost**(`AddDatabase`, ADR-0011)가 맡는다. MigrationService · Api · 테스트는 `EnsureCreated`나 `CREATE DATABASE`를 쓰지 않는다(`employee_app`에는 `CREATEDB`가 없다). Api는 시작할 때 마이그레이션하지 않는다.
- **통합 테스트**: Testcontainers(`postgres:17`)에 실제 `MigrateAsync`를 적용한다. `EnsureCreated`는 금지한다(FR-09).
- **운영(Phase 4)**: 시작 시 적용하지 않고 마이그레이션 번들 또는 `--idempotent` 스크립트로 적용한다(데이터베이스 규칙 유지).
- **마이그레이션 대상**: 쓰기 DbContext만 마이그레이션을 가진다([ADR-0009](0009-separate-read-write-db-context.md)). `IDesignTimeDbContextFactory`도 쓰기 DbContext만 만들고, 연결 문자열은 환경 변수 또는 더미 값을 쓴다(비밀 없음). 한 어셈블리에 DbContext가 2개라 `dotnet ef`에는 `--context EmployeeDbContext`가 필수다. 위치는 Infrastructure의 `Persistence/Migrations/`이고 생성 코드(`generated_code`)로 분석에서 뺀다.
- **`__EFMigrationsHistory`는 snake_case 규칙의 예외**로 EF 기본 이름을 유지한다(S01 계획 리뷰 기본값). 컬럼 `MigrationId` · `ProductVersion`은 SQL에서 따옴표가 필요하다. Respawn 초기화 대상에서 제외한다(S01-T04).

운영 전 리셋 정책:

- **허용 기간**: 운영 배포(Phase 4) 전까지. 그보다 먼저 로컬 밖에 지속되는 공유 DB(스테이징 등)가 생기면 그 시점에 끝난다. 종료 조건을 이 ADR에 적어 두므로, 종료할 때 새 ADR은 필요 없다.
- **절차**: ① `Persistence/Migrations/` 폴더를 스냅숏까지 삭제 → ② `dotnet ef migrations add InitialCreate --context EmployeeDbContext` → ③ `dotnet ef migrations script --idempotent --context EmployeeDbContext` 결과를 dba가 검토 → ④ 이름 있는 데이터 볼륨 삭제(또는 `DROP DATABASE`) → ⑤ AppHost 재시작.
- **기록**: 리셋만 담은 커밋 1개(다른 변경과 섞지 않음), 스프린트 진행 기록에 사유와 영향(로컬 볼륨 삭제 필요), [데이터베이스](../../04-development/database.md) 변경 이력에 한 줄. 커밋 footer는 따로 두지 않는다.
- **불변 규칙과의 관계**: "적용(push)된 마이그레이션은 고치지 않는다"는 그대로 유지한다. 리셋은 **전체를 `InitialCreate` 하나로 다시 만드는** 예외이며, 리셋 기간에도 개별 마이그레이션을 부분 수정하는 것은 금지한다. push 전 토픽 브랜치 안에만 있는 공유되지 않은 마이그레이션을 다시 만드는 것은 불변 규칙 대상이 아니다.

## 결과 (Consequences)

- 긍정: 적용 주체가 하나라 경합이 없고, Api는 DDL을 하지 않는다. 마이그레이션이 실패하면 Api가 뜨지 않아 스키마 불일치 상태로 요청을 받지 않는다. 통합 테스트가 운영과 같은 마이그레이션 경로를 검증한다. 기반 구축 동안 초기 설계 변경 이력이 쌓이지 않는다.
- 부정: 실행 프로젝트(MigrationService)가 하나 늘어난다. 리셋 뒤에는 모든 개발자가 로컬 볼륨을 지워야 한다(로컬 개발 환경 구성 문서에 안내). 지금 MigrationService도 `employee_app`으로 접속하므로 마이그레이션 전용 롤 분리는 TD-001로 남는다.
- 위험: EF Core 8에는 마이그레이션 잠금이 없으므로 MigrationService를 여러 개 띄우거나 Api에서 `Migrate`를 부르지 않는다(TD-011). 운영 적용 방식은 Phase 4에서 번들 / 스크립트로 구체화한다.
- 후속: 리셋 정책 · `__EFMigrationsHistory` 예외 · 설계 시점 팩터리 규칙을 [데이터베이스](../../04-development/database.md#마이그레이션-규칙)에 반영한다(S01-T04 · FR-11). 리셋 절차와 볼륨 삭제 방법은 [로컬 개발 환경 구성](../../01-getting-started/local-setup.md)(S04)에 쓴다. Respawn 제외 테이블은 S01-T04 ADR에서 정한다.
