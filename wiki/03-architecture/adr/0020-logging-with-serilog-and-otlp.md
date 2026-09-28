---
title: "ADR-0020: 로깅 · 관측 구현 (Serilog + OTLP)"
type: adr
adr: "0020"
status: accepted
date: 2026-09-27
deciders: []
supersedes:
superseded_by:
aliases: [ADR-0020]
tags: [adr, architecture, database, infrastructure]
created: 2026-09-27
updated: 2026-09-27
---

# ADR-0020: 로깅 · 관측 구현 (Serilog + OTLP)

## 배경 (Context)

- [로깅 & 관측성](../../04-development/logging-observability.md)은 애플리케이션 코드는 `ILogger<T>`만 쓰고 Serilog를 공급자로 연결하며, 콘솔은 텍스트 · 파일은 JSON(CLEF), 추적은 OpenTelemetry로 하고 개인정보를 남기지 않는다고 정했다.
- [PRD-001 질문과 답변](../../10-delivery/prd/PRD-001-foundation.md#질문과-답변) Q11에서 "로깅(Serilog + OTLP)"을 확정했고, 로그 수집기는 도입하지 않으며 로컬 관측은 Aspire 대시보드로 한다(Q5). FR-03은 ServiceDefaults에 OpenTelemetry(트레이스 · 메트릭)와 "Serilog → OTLP 로그 전송"을 요구한다.
- Aspire ServiceDefaults 템플릿은 `builder.Logging.AddOpenTelemetry(...)`로 OpenTelemetry SDK 로그 공급자를 붙이고 OTLP로 내보낸다. 여기에 Serilog의 OTLP 싱크를 더하면 **같은 로그가 대시보드에 두 번** 들어갈 수 있다(PRD developer 리뷰 지적).
- 템플릿(Aspire 9.5.2)의 net8.0 OpenTelemetry 버전은 1.9.0이고 취약점(GHSA-g94r-2vxg-569j, OpenTelemetry.Api `< 1.15.3`) 대상이다. S01-T01은 1.19.x를 권장했다(BL-007, [패키지 버전 · 라이선스 · Aspire](../package-versions.md#aspire)).
- EF Core는 `EnableSensitiveDataLogging`을 켜면 SQL 파라미터 값을 로그와 예외 메시지에 싣는다. 이 로그는 파일 JSON과 OTLP로 나가므로 개인정보(이메일 등)가 새어 나갈 수 있다(S01-T03 dba). 추적은 Npgsql.OpenTelemetry의 `AddNpgsql()`로 붙인다([ADR-0011](0011-use-aspire-local-orchestration.md)).

## 검토한 대안 (Options)

로그를 OTLP로 보내는 경로(중복 방지 방식):

1. **Serilog OTLP 싱크(Serilog.Sinks.OpenTelemetry)만 사용, OpenTelemetry SDK 로그 공급자는 두지 않음**: 장점: 로그 경로가 Serilog 하나라 수준 · 보강(enricher) · 필터 설정이 한 곳이고, PRD의 "Serilog → OTLP"와 같다. 단점: 템플릿에서 로그 부분을 빼야 하고, OTLP 설정 환경 변수를 싱크가 읽는지 확인이 필요하다(아래 결정에서 확인).
2. **OpenTelemetry SDK 로그 공급자로 OTLP 전송 + Serilog `writeToProviders: true`**: 장점: 템플릿 코드를 그대로 쓴다. 단점: Serilog 보강 속성이 공급자 경로로 넘어가지 않아 콘솔 · 파일과 대시보드의 필드가 달라지고, 한 이벤트가 두 파이프라인을 지난다.
3. **Serilog를 교체 방식이 아닌 일반 공급자(`Logging.AddSerilog`)로 추가하고 OTel 공급자와 나란히 사용**: 장점: 각자 역할이 분명하다. 단점: 수준 설정이 `Serilog:MinimumLevel`과 `Logging:LogLevel` 두 곳으로 나뉘어 혼란스럽다.

EF 민감 데이터 로깅: (가) 전면 금지 (나) **Development에서만 설정으로 켤 수 있게(opt-in), 기본 꺼짐**.

## 결정 (Decision)

**로그 경로는 대안 1, EF 민감 데이터 로깅은 (나)를 채택한다.**

Serilog 구성:

- 패키지: Serilog.AspNetCore 10.0.0(콘솔 · 파일 · CLEF · `Settings.Configuration` 포함), Serilog.Sinks.OpenTelemetry 4.2.0([패키지 버전 · 라이선스 · 애플리케이션](../package-versions.md#애플리케이션)).
- 등록: ServiceDefaults의 공통 확장(`AddServiceDefaults` 안)이 `builder.Services.AddSerilog((services, configuration) => ...)`로 등록한다. Api(WebApplication)와 MigrationService(Worker)가 같은 코드를 쓴다. 정적 `Log`와 부트스트랩 로거는 쓰지 않는다([로깅 & 관측성 · 구성](../../04-development/logging-observability.md#구성-serilog)).
- 콘솔(텍스트) · 파일(CLEF, `RenderedCompactJsonFormatter`) 싱크와 수준은 `appsettings*.json`의 `Serilog` 절(`ReadFrom.Configuration`)로 관리한다. 수준은 `Serilog:MinimumLevel`에서만 정하고 `Logging:LogLevel` 절은 두지 않는다(Serilog가 로거 팩터리를 교체하므로 쓰이지 않는다). 테스트 · CI 호스트는 설정으로 파일 싱크를 끈다.
- OTLP 싱크는 코드에서 `OTEL_EXPORTER_OTLP_ENDPOINT`가 있을 때만 붙인다(Aspire가 주입. 없으면 싱크 없음, 테스트 · CI 포함). 싱크는 `ignoreEnvironment: false`(기본)로 두어 `OTEL_EXPORTER_OTLP_ENDPOINT` · `OTEL_EXPORTER_OTLP_PROTOCOL` · `OTEL_EXPORTER_OTLP_HEADERS` · `OTEL_SERVICE_NAME` · `OTEL_RESOURCE_ATTRIBUTES`를 읽는다(4.2.0 XML 문서 · 어셈블리 문자열로 확인). 따라서 대시보드에서 로그가 트레이스와 같은 서비스 · 인스턴스로 묶인다.
- TraceId / SpanId는 `Activity.Current`에서 채워져 CLEF(`@tr` / `@sp`)와 OTLP 로그 레코드에 함께 실린다.

**Serilog / OpenTelemetry 로그 중복 방지**:

1. ServiceDefaults는 OpenTelemetry SDK 로그 공급자를 등록하지 않는다(템플릿의 `builder.Logging.AddOpenTelemetry(...)` 제거).
2. OTLP 내보내기는 모든 신호를 대상으로 하는 `UseOtlpExporter()` 대신 `WithTracing` · `WithMetrics` 안에서 `AddOtlpExporter()`로 트레이스 · 메트릭에만 붙인다.
3. Serilog는 `writeToProviders: false`(기본)로 등록한다. 다른 `ILoggerProvider`가 등록되더라도 Serilog가 팩터리를 교체하므로 이벤트가 전달되지 않는다.
4. 확인: S03-T04에서 대시보드 구조화 로그에 같은 이벤트가 한 번만 보이는지 실행 기록으로 남기고, 등록된 `ILoggerProvider`에 OpenTelemetry 로그 공급자가 없음을 테스트로 확인한다.

OpenTelemetry(트레이스 · 메트릭, ServiceDefaults):

- **템플릿 값(1.9.0) 대신 1.19.x로 고정한다(BL-007)**: OpenTelemetry.Extensions.Hosting · Exporter.OpenTelemetryProtocol 1.19.1, Instrumentation.AspNetCore · Http · Runtime 1.19.0. 1.15.3 미만은 취약점 대상이고, 취약점 없는 버전은 모두 net8.0에서 `System.Diagnostics.DiagnosticSource` 10.0.0을 요구한다(TD-008).
- 트레이스: ASP.NET Core · HttpClient 계측 + Npgsql.OpenTelemetry `AddNpgsql()`. **EF Core용 OTel 계측(OpenTelemetry.Instrumentation.EntityFrameworkCore)은 추가하지 않는다**(Npgsql span과 중복). 헬스체크 경로는 추적과 요청 로그에서 뺀다.
- 메트릭: ASP.NET Core · HttpClient · 런타임 기본 계측. 업무 메트릭은 이후 토픽.
- Npgsql 8.0.9 span 태그는 `db.statement`(`$1` 자리표시자 SQL), `db.name`, `db.user`, `db.connection_string`, `net.peer.*`이며 파라미터 값 태그는 없다. `db.connection_string`에 비밀번호가 빠진다는 것은 XML 문서 기준 추정이므로 S03-T05에서 대시보드 span으로 확인한다(BL-024).

요청 로그:

- `UseSerilogRequestLogging()`이 요청당 한 줄(경로, 상태 코드, 경과 시간)을 남긴다. 쿼리 문자열 · 본문은 남기지 않는다. 헬스체크 요청은 제외한다. ASP.NET Core 자체 요청 로그는 `Microsoft.AspNetCore` = `Warning`으로 낮춰 중복을 막는다.

EF Core · Npgsql 로그와 개인정보 (S01-T03 dba 확정):

- **`EnableSensitiveDataLogging`은 Development에서만 허용하고 기본은 꺼짐이다.** 조건은 `IHostEnvironment.IsDevelopment() && 설정 플래그`(예: `Database:EnableSensitiveDataLogging`, 기본 `false`). Testing · CI · Staging · Production에서는 설정과 무관하게 끈다. 결정은 쓰기 · 읽기 DbContext 공용 등록 확장 메서드 한 곳에서만 한다([ADR-0011](0011-use-aspire-local-orchestration.md)의 공용 등록). 근거: 켜면 `CommandExecuted` 로그에 파라미터 값, 예외 메시지에 키 값이 실려 파일 JSON 로그 · OTLP로 개인정보가 나간다. 로컬 디버깅 편의를 위해 전면 금지 대신 Development opt-in으로 한다.
- `EnableDetailedErrors`는 Development에서만 켠다(값 노출은 없고 성능 비용만 있다).
- **SQL 파라미터 값은 기록하지 않는다(Development opt-in 외)**: `EnableSensitiveDataLogging`을 끄고, `NpgsqlLoggingConfiguration.InitializeLogging(…, parameterLoggingEnabled: true)`와 `NpgsqlDataSourceBuilder.EnableParameterLogging`을 쓰지 않는다.
- EF Core 로그 수준: 기본 `Serilog:MinimumLevel:Override`에 `Microsoft.EntityFrameworkCore` = `Warning`, `Microsoft.EntityFrameworkCore.Database.Command` = `Warning`. `appsettings.Development.json`에서만 `Microsoft.EntityFrameworkCore.Database.Command` = `Information`(SQL 문장 · 소요 시간. 파라미터 값은 `?`).
- Npgsql 자체 로그: EFCore.PG 8.0.11 + 연결 문자열 방식에서는 전역 레거시 설정(`InitializeLogging`)을 거쳐야 나오고 기본은 꺼져 있다. **켜지 않는다.** SQL 로그는 EF Core 범주 하나로 남긴다(중복 방지).
- 제약 위반 예외 로그: UoW의 변환 로그에는 SqlState, ConstraintName, 엔티티 형식 이름만 남긴다([ADR-0014](0014-command-transaction-boundary-and-unit-of-work.md)). 연결 문자열에 `Include Error Detail=true`를 쓰지 않는다(꺼져 있으면 Npgsql이 `PostgresException.Detail`의 `Key (email)=(...)`를 가린다).
- 연결 문자열 · 비밀번호: `Persist Security Info=true`를 쓰지 않는다. `IConfiguration`, `ConnectionStrings` 절, 환경 변수(`ConnectionStrings__*`)를 통째로 로그에 쓰지 않는다. 시작 로그는 호스트 · DB 이름 · 사용자까지만 남긴다. Serilog `Settings.Configuration`에서도 연결 문자열을 enricher · 속성으로 붙이지 않는다.
- 정상 경합의 `23505`에서 EF Core가 `Error` 로그 2건(명령 실패, SaveChanges 실패)을 남기는 문제는 수준 조정 여부를 S02-T04에서 정한다(BL-023).

## 결과 (Consequences)

- 긍정: 로그가 Serilog 한 경로로만 나가 콘솔 · 파일 · 대시보드의 필드가 같고 중복이 없다. 로그와 트레이스가 TraceId와 서비스 리소스로 연결된다. SQL 파라미터 값과 연결 비밀이 Development opt-in 외에는 어느 출력에도 남지 않는다. OpenTelemetry 취약 버전을 쓰지 않는다.
- 부정: ServiceDefaults가 Aspire 템플릿과 달라진다(로그 공급자 제거, 신호별 내보내기). Aspire 버전을 올릴 때 이 차이를 다시 적용해야 한다. OpenTelemetry 1.19.x로 net8.0 앱에 Microsoft.Extensions.* · DiagnosticSource 10.0.0이 전이 유입된다(TD-008).
- 부정: [로깅 & 관측성](../../04-development/logging-observability.md)이 요구하는 파일 비동기 쓰기(`Serilog.Sinks.Async`)와 `Enrich.WithMachineName()`(Serilog.Enrichers.Environment)은 [패키지 버전 · 라이선스](../package-versions.md)에 아직 없다. 버전 · 라이선스를 기록한 뒤 도입한다(S01-T04).
- 검증: 중복 방지 · 대시보드 로그 · 트레이스는 S03-T04, 추적 태그의 비밀번호 · 파라미터 값 미노출은 S03-T05(BL-024), `EnableSensitiveDataLogging` 환경 조건은 공용 등록 확장 메서드 단위 테스트(S02-T04)로 확인한다.
- 후속: [로깅 & 관측성](../../04-development/logging-observability.md)의 Aspire 연동 · 🟡(로그 수집기, 추적 백엔드)는 S01-T04(도입 보류 ADR)와 FR-11에서 정리한다.
