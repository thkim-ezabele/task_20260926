---
title: "패키지 버전 · 라이선스"
type: doc
status: draft
tags: [architecture, packages, license]
created: 2026-09-27
updated: 2026-09-28
---

# 패키지 버전 · 라이선스

> PRD-001 기반 구축에서 도입하는 SDK · 패키지 · 컨테이너 이미지 · GitHub Actions의 고정 버전, 라이선스, 출처를 기록합니다(S01-T01 사전 확인, [PRD-001](../10-delivery/prd/PRD-001-foundation.md) FR-01①, NFR-05, Q19).
> 모든 값은 2026-09-27에 NuGet API · GitHub 원본 · 공식 페이지에서 직접 확인했습니다. 기술 선택의 결정 상태는 [기술 스택](tech-stack.md), 결정 배경은 [ADR](adr/README.md)이 원본입니다.
>
> [위키 홈](../README.md)

## 요약

| 항목 | 결론 | 근거 절 |
|---|---|---|
| Aspire 버전 | **9.5.2** 고정(사용자 결정). 9.x 전 버전이 NuGet에서 폐기(out of support) 상태 | [Aspire](#aspire) |
| SDK | `global.json` `8.0.400` + `rollForward: latestFeature` 권장. 로컬 SDK 8.0.202는 **8.0.4xx로 업데이트 필요** | [SDK · global.json](#sdk--globaljson) |
| EF Core 계열 | EF Core · Relational · Design · dotnet-ef **8.0.31**, Npgsql.EFCore **8.0.11**, Npgsql **8.0.9**, EFCore.NamingConventions **8.0.3** | [데이터](#데이터) |
| PostgreSQL 이미지 | 메이저 태그 **`17`**, AppHost와 Testcontainers 양쪽에 명시하고 한 곳에서 관리 | [PostgreSQL 이미지](#postgresql-이미지) |
| 테스트 프레임워크 | **xUnit v3(`xunit.v3` 4.0.1) 확정**([ADR-0021](adr/0021-test-tooling-xunit-v3-and-awesomeassertions.md)). v2(2.9.3)는 NuGet `Legacy` 폐기 표시 | [테스트](#테스트) |
| 취약점 | 고정 버전 조합에서 취약점 0건. 단, **MessagePack 2.5.305 전이 고정**(AppHost)과 **`NuGetAuditMode=all` 명시**가 필요 | [스크래치 검증](#스크래치-restore-검증) |
| 라이선스 | 상용 라이선스 **0건**(MIT, Apache-2.0, BSD-3-Clause, PostgreSQL, 0BSD, 무료 Microsoft 사용 조건 2종) | [라이선스 점검](#라이선스-점검) |

## SDK · global.json

| 항목 | 값 | 출처 |
|---|---|---|
| .NET 8 최신 런타임 | 8.0.31 (2026-09-08) | [releases.json](https://raw.githubusercontent.com/dotnet/core/main/release-notes/8.0/releases.json) |
| .NET 8 최신 SDK | 8.0.425 (4xx 밴드), 8.0.131 (1xx 밴드). 2xx · 3xx 밴드는 더 이상 패치가 나오지 않음 | 같은 파일 `releases[0].sdks` |
| .NET 8 지원 종료 | 2026-11-10 (`support-phase: maintenance`) | 같은 파일 `eol-date` |
| 로컬 환경 | SDK 8.0.202 하나, 런타임 8.0.3 (`dotnet --list-sdks`, `--list-runtimes`) | 로컬 확인 |
| .NET SDK 라이선스 | 소스 MIT. 배포물은 Linux · macOS MIT, Windows는 .NET Library License(무료) | [license-information.md](https://github.com/dotnet/core/blob/main/license-information.md) |

**최소 SDK 기능 밴드: 8.0.400.**

- Aspire 9.x 공식 전제 조건은 ".NET 8.0 또는 9.0"뿐이고 기능 밴드는 따로 없습니다([aspire-prereqs.md, 2025-10-01 판](https://raw.githubusercontent.com/dotnet/docs-aspire/1f1a76339b3f22c99ea2060f42ba72361d9728f9/docs/includes/aspire-prereqs.md)). 실제로 AppHost(Aspire.AppHost.Sdk 9.5.2)는 SDK 8.0.202에서도 빌드 오류가 없었습니다.
- 하한을 정하는 쪽은 xUnit v3입니다. `xunit.v3` 4.0.1이 끌어오는 `xunit.analyzers` 2.1.0은 Roslyn 4.11을 요구하는데, SDK 8.0.202(Roslyn 4.9)에서는 `CS9057`로 빌드가 실패했고 SDK 8.0.425에서는 성공했습니다.
- 8.0.2xx 밴드는 패치가 끊겼고, 로컬 런타임 8.0.3은 보안 패치가 1년 넘게 밀려 있습니다.

권장 `global.json`(S01-T05에서 작성):

```json
{
  "sdk": {
    "version": "8.0.400",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  }
}
```

- `latestFeature`: 8.0.400 이상 중 설치된 가장 높은 8.0.x 기능 밴드 · 패치를 씁니다. 9.x SDK로는 올라가지 않습니다.
- CI: `actions/setup-dotnet` v6은 `global-json-file`에 `rollForward: latestFeature`가 있으면 `8.0` 채널의 최신 SDK를 설치합니다([setup-dotnet.ts v6.0.0](https://github.com/actions/setup-dotnet/blob/v6.0.0/src/setup-dotnet.ts)의 `case 'latestFeature'`).
- 스크래치 확인: 위 파일을 두고 SDK 8.0.425에서는 `dotnet --version`이 `8.0.425`로 나왔고, SDK 8.0.202만 있을 때는 SDK를 찾지 못해 실패했습니다. **로컬에 SDK 8.0.425를 설치해야 S01-T05 빌드가 가능합니다.**

## Aspire

### 지원 기간

| 항목 | 내용 | 출처 |
|---|---|---|
| 지원 정책 | Microsoft Modern Lifecycle. **최신 릴리스 하나만 지원**하고, 다음 메이저 · 마이너가 나오면 이전 버전 지원이 끝남 | [Aspire support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/aspire) |
| Aspire 9.5 | 출시 2025-09-25, 마지막 패치 9.5.2 (2025-10-23), **지원 종료 2025-11-11** (Aspire 13.0 출시일) | 같은 페이지 "Out of support versions" |
| 현재 지원 버전 | Aspire 13.5 (13.5.4, 2026-09-15) | 같은 페이지 |
| NuGet 표시 | `Aspire.Hosting.AppHost` · `Aspire.AppHost.Sdk` · `Aspire.Hosting` · `Aspire.Hosting.PostgreSQL` 9.5.2 모두 폐기(`Other`, `Legacy`): "This version is out of support and is no longer maintained" | NuGet registration `deprecation` |
| .NET 8 지원 종료와 관계 | Aspire 9.5는 .NET 8 지원 종료(2026-11-10)보다 **약 1년 먼저** 지원이 끝남. 이미 지원 밖이므로 .NET 8 종료로 추가되는 위험은 런타임 쪽뿐 | 위 두 출처 |
| Aspire 13 전환 조건 | `Aspire.Hosting.AppHost` 13.5.4도 `net8.0` 대상 그룹은 있지만, **C# AppHost는 .NET 10 SDK가 필수** | [aspire.dev prerequisites](https://aspire.dev/get-started/prerequisites/), [13.5.4 nuspec](https://api.nuget.org/v3-flatcontainer/aspire.hosting.apphost/13.5.4/aspire.hosting.apphost.nuspec) |

결정: 9.5.2를 유지합니다(사용자 결정, PRD Q3 · Q19). 지원 종료 사실은 Aspire ADR(S01-T02)에 적고, TD-005 · BL-002로 관리합니다.

### 버전별 기능

| 기능 | 도입 버전 | 확인 방법 |
|---|---|---|
| `Aspire.AppHost.Sdk`(워크로드 없이 MSBuild SDK로 AppHost 구성) | 9.0.0 | NuGet `aspire.apphost.sdk` 버전 목록 첫 정식판 9.0.0 |
| `WaitFor` · `WaitForCompletion` | 9.0.0 | [v9.0.0 ResourceBuilderExtensions.cs](https://github.com/dotnet/aspire/blob/v9.0.0/src/Aspire.Hosting/ResourceBuilderExtensions.cs) |
| `WaitForStart` | 9.5.2에 있음(9.0.0에는 없음) | [v9.5.2 ResourceBuilderExtensions.cs](https://github.com/dotnet/aspire/blob/v9.5.2/src/Aspire.Hosting/ResourceBuilderExtensions.cs) |
| PostgreSQL `WithInitBindMount` | 9.0.0 | [v9.0.0 PostgresBuilderExtensions.cs](https://github.com/dotnet/aspire/blob/v9.0.0/src/Aspire.Hosting.PostgreSQL/PostgresBuilderExtensions.cs) |
| PostgreSQL `AddDatabase`가 실제 DB 생성, `WithCreationScript` | 9.2.0 (dba 확인) | [v9.5.2 PostgresBuilderExtensions.cs](https://github.com/dotnet/aspire/blob/v9.5.2/src/Aspire.Hosting.PostgreSQL/PostgresBuilderExtensions.cs) |
| PostgreSQL `WithInitFiles`, `WithPassword` | 9.5.x에 있음. 롤 생성 초기화 스크립트는 `WithInitFiles`로 넣는다([ADR-0011](adr/0011-use-aspire-local-orchestration.md)). 9.5.2의 `WithInitBindMount`는 `Obsolete`라 쓰지 않는다(경고 = 오류) | 같은 파일 |
| PostgreSQL 기본 이미지 | 9.2.0: 17.2, 9.5.2: **17.6** | [v9.5.2 PostgresContainerImageTags.cs](https://github.com/dotnet/aspire/blob/v9.5.2/src/Aspire.Hosting.PostgreSQL/PostgresContainerImageTags.cs) |
| ServiceDefaults 템플릿의 net8.0 OpenTelemetry 버전 | 9.5.2: **1.9.0**(취약, 아래 참고) | [v9.5.2 eng/Versions.props](https://github.com/dotnet/aspire/blob/v9.5.2/eng/Versions.props) `OpenTelemetryNet8Version` |

- 9.5.2 `AddDatabase`의 생성 동작: 서버 `ResourceReadyEvent` 때 `postgres` DB에 접속해 스크립트 전체를 명령 1개로 실행하고, `42P04`(이미 존재)만 무시하며 다른 오류는 로그만 남기고 계속합니다(dba 확인). 실측은 BL-003.
- dba 인계 자료의 "`WithInitBindMount` 9.2+"는 v9.0.0 소스에 이미 있어 9.0.0으로 정정합니다(9.5.2 사용에는 영향 없음).

### AppHost · ServiceDefaults 패키지

| 패키지 | 버전 | 용도 | 라이선스 | 대상 프레임워크 | 출처 | 비고 |
|---|---|---|---|---|---|---|
| Aspire.AppHost.Sdk | 9.5.2 | AppHost MSBuild SDK (`<Sdk Name="Aspire.AppHost.Sdk" Version="9.5.2" />`) | MIT | MSBuild SDK | [nuspec](https://api.nuget.org/v3-flatcontainer/aspire.apphost.sdk/9.5.2/aspire.apphost.sdk.nuspec), [Sdk.in.targets](https://github.com/dotnet/aspire/blob/v9.5.2/src/Aspire.AppHost.Sdk/SDK/Sdk.in.targets) | 폐기(out of support). CPM의 `Aspire.Hosting.AppHost` 버전을 읽어 Dashboard · DCP 패키지를 같은 버전으로 암묵 추가 |
| Aspire.Hosting.AppHost | 9.5.2 | AppHost 런타임 | MIT | net8.0, net9.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/aspire.hosting.apphost/9.5.2/aspire.hosting.apphost.nuspec) | 폐기. net8.0 그룹 의존은 Microsoft.Extensions.* 8.0.x |
| Aspire.Hosting | 9.5.2 | AppHost 전이 의존 | MIT | net8.0, net9.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/aspire.hosting/9.5.2/aspire.hosting.nuspec) | 폐기. StreamJsonRpc 2.22.11 → **MessagePack 2.5.192(취약)** 전이 |
| Aspire.Hosting.PostgreSQL | 9.5.2 | PostgreSQL 컨테이너 리소스 | MIT | net8.0, net9.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/aspire.hosting.postgresql/9.5.2/aspire.hosting.postgresql.nuspec) | 폐기. dba 확인 |
| Aspire.Npgsql.EntityFrameworkCore.PostgreSQL | 9.5.2 — **사용하지 않음**([ADR-0011](adr/0011-use-aspire-local-orchestration.md)) | 클라이언트 통합 | MIT | net8.0, net9.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/aspire.npgsql.entityframeworkcore.postgresql/9.5.2/aspire.npgsql.entityframeworkcore.postgresql.nuspec) | 폐기(`Other`, `Legacy`). net8.0 그룹 의존: Npgsql.EntityFrameworkCore.PostgreSQL **8.0.11**(하한, EF Core 8.0.11 이상을 끌어옴 · EF Core 9 유입 없음), Npgsql.DependencyInjection 8.0.6, Npgsql.OpenTelemetry 8.0.6. 미사용 이유: 항상 `AddDbContextPool`, DI `SaveChangesInterceptor` 불가, 단독 사용 시 OpenTelemetry.Api 1.9.0 → NU1902. 대신 `AddDbContext` + `UseNpgsql` 직접 등록 |
| Aspire.Dashboard.Sdk.&lt;rid&gt; | 9.5.2 (암묵 추가) | Aspire 대시보드 실행 파일 | Microsoft 소프트웨어 사용 조건(패키지 내 `EULA.md`, 무료 · 개발 · 테스트 사용 허용, 오픈 소스 아님) | 런타임별 도구 패키지 | [win-x64 nuspec](https://api.nuget.org/v3-flatcontainer/aspire.dashboard.sdk.win-x64/9.5.2/aspire.dashboard.sdk.win-x64.nuspec) | 폐기(`Other`, `Legacy`). `Aspire.AppHost.Sdk`가 OS에 맞는 패키지를 자동 추가하며 CPM에 적지 않는다 |
| Aspire.Hosting.Orchestration.&lt;rid&gt; | 9.5.2 (암묵 추가) | DCP(로컬 오케스트레이터) | MIT | 런타임별 도구 패키지 | [win-x64 nuspec](https://api.nuget.org/v3-flatcontainer/aspire.hosting.orchestration.win-x64/9.5.2/aspire.hosting.orchestration.win-x64.nuspec) | 폐기(`Other`, `Legacy`). 위와 같이 자동 추가 |
| MessagePack | **2.5.305** (전이 고정) | AppHost 전이 의존 취약점 해소 | MIT | netstandard2.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/messagepack/2.5.305/messagepack.nuspec) | 2.5.192는 GHSA 11건(high 2). 모두 `< 2.5.301`에서 수정. 같은 2.x 줄의 최신 패치로 고정 |
| Microsoft.Extensions.ServiceDiscovery | 9.5.2 | ServiceDefaults 서비스 검색 | MIT | net462, netstandard2.0, net8.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/microsoft.extensions.servicediscovery/9.5.2/microsoft.extensions.servicediscovery.nuspec) | 폐기 표시 없음. 최신 10.10.0도 net8.0 지원. 템플릿대로 포함(ADR-0011, BL-009 해소) |
| Microsoft.Extensions.Http.Resilience | 9.9.0 | ServiceDefaults `AddStandardResilienceHandler` | MIT | net462, netstandard2.0, net8.0, net9.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/microsoft.extensions.http.resilience/9.9.0/microsoft.extensions.http.resilience.nuspec) | Aspire 9.5.2 템플릿 값(`MicrosoftExtensionsHttpResilienceVersion`). 최신 10.10.0도 net8.0 지원 |
| OpenTelemetry.Extensions.Hosting | 1.19.1 | OTel 호스팅 | Apache-2.0 | net8.0 외 | [nuspec](https://api.nuget.org/v3-flatcontainer/opentelemetry.extensions.hosting/1.19.1/opentelemetry.extensions.hosting.nuspec) | 템플릿 값 1.9.0 대신 사용. GHSA-g94r-2vxg-569j는 OpenTelemetry.Api `< 1.15.3` |
| OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.19.1 | OTLP 내보내기(Aspire 대시보드) | Apache-2.0 | net8.0 외 | [nuspec](https://api.nuget.org/v3-flatcontainer/opentelemetry.exporter.opentelemetryprotocol/1.19.1/opentelemetry.exporter.opentelemetryprotocol.nuspec) | |
| OpenTelemetry.Instrumentation.AspNetCore | 1.19.0 | ASP.NET Core 추적 · 메트릭 | Apache-2.0 | net8.0, net10.0, netstandard2.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/opentelemetry.instrumentation.aspnetcore/1.19.0/opentelemetry.instrumentation.aspnetcore.nuspec) | |
| OpenTelemetry.Instrumentation.Http | 1.19.0 | HttpClient 추적 | Apache-2.0 | net8.0 외 | [nuspec](https://api.nuget.org/v3-flatcontainer/opentelemetry.instrumentation.http/1.19.0/opentelemetry.instrumentation.http.nuspec) | |
| OpenTelemetry.Instrumentation.Runtime | 1.19.0 | 런타임 메트릭 | Apache-2.0 | net8.0 외 | [nuspec](https://api.nuget.org/v3-flatcontainer/opentelemetry.instrumentation.runtime/1.19.0/opentelemetry.instrumentation.runtime.nuspec) | |

- OpenTelemetry는 취약점이 없는 버전(1.15.3 이상)이 모두 net8.0에서 `System.Diagnostics.DiagnosticSource` 10.0.0을 요구합니다([1.15.3 nuspec](https://api.nuget.org/v3-flatcontainer/opentelemetry.api/1.15.3/opentelemetry.api.nuspec)). 1.12 ~ 1.15.0은 GHSA-g94r-2vxg-569j 대상입니다.
- ServiceDefaults가 OTel 1.19.x를 참조하면, 클라이언트 통합(9.5.2)을 함께 넣어도 OpenTelemetry.Api가 1.19.1로 올라가 NU1902가 사라지고 OTel 버전이 한 줄로 맞춰집니다(스크래치 확인, 빌드 경고 0). BL-004 · TD-006 판단 자료입니다.

## PostgreSQL 이미지

| 항목 | 값 | 출처 |
|---|---|---|
| 결론 | **`postgres:17`** 메이저 태그. AppHost `.WithImageTag("17")`와 Testcontainers `new PostgreSqlBuilder("postgres:17")` 양쪽에 명시하고, 값은 한 곳(예: `Directory.Build.props` 속성 또는 공용 상수)에서 관리 | dba 결론 |
| Docker Hub `17` | 존재, 2026-09-24 갱신. 최신 마이너 태그 17.11 | [Docker Hub tags API](https://hub.docker.com/v2/repositories/library/postgres/tags/17) |
| 라이선스 | PostgreSQL 본체 PostgreSQL License, 이미지 빌드 파일(docker-library/postgres) MIT. 이미지 안의 다른 패키지는 각 배포판 라이선스 | [PostgreSQL License](https://www.postgresql.org/about/licence/), [docker-library/postgres LICENSE](https://github.com/docker-library/postgres/blob/master/LICENSE) |
| PostgreSQL 17 지원 | 최신 17.11, 지원 종료 2029-11-08 | [PostgreSQL versioning policy](https://www.postgresql.org/support/versioning/) |
| Aspire 9.5.2 기본값 | `library/postgres:17.6` | [PostgresContainerImageTags.cs](https://github.com/dotnet/aspire/blob/v9.5.2/src/Aspire.Hosting.PostgreSQL/PostgresContainerImageTags.cs) |
| Testcontainers 4.15.0 기본값 | `postgres:15.1`, 매개변수 없는 생성자는 `Obsolete`(CS0618) → 이미지 인자 필수(TD-004) | [PostgreSqlBuilder.cs](https://github.com/testcontainers/testcontainers-dotnet/blob/4.15.0/src/Testcontainers.PostgreSql/PostgreSqlBuilder.cs) |

## 데이터

dba 확인 결과(S01-T01 dba 단계)를 옮깁니다.

| 패키지 | 버전 | 용도 | 라이선스 | 대상 프레임워크 | 출처 | 비고 |
|---|---|---|---|---|---|---|
| Microsoft.EntityFrameworkCore | 8.0.31 | ORM | MIT | net8.0 | [index](https://api.nuget.org/v3-flatcontainer/microsoft.entityframeworkcore/index.json) | 8.0.x 최신(2026-09-08) |
| Microsoft.EntityFrameworkCore.Relational | 8.0.31 | 관계형 공급자 기반 | MIT | net8.0 | [index](https://api.nuget.org/v3-flatcontainer/microsoft.entityframeworkcore.relational/index.json) | Npgsql.EFCore 하한(8.0.11)을 올리기 위해 CPM에 명시 |
| Microsoft.EntityFrameworkCore.Design | 8.0.31 | 마이그레이션 설계 시점 | MIT | net8.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/microsoft.entityframeworkcore.design/8.0.31/microsoft.entityframeworkcore.design.nuspec) | `PrivateAssets=all` |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.11 | EF Core PostgreSQL 공급자 | PostgreSQL | net8.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/npgsql.entityframeworkcore.postgresql/8.0.11/npgsql.entityframeworkcore.postgresql.nuspec) | EF Core 의존 상한 없음 → 전이 고정 필수(TD-003) |
| Npgsql | 8.0.9 | ADO.NET 드라이버 | PostgreSQL | net8.0 외 | [nuspec](https://api.nuget.org/v3-flatcontainer/npgsql/8.0.9/npgsql.nuspec) | 8.0.x 최신(2026-03-12), 취약점 0 |
| Npgsql.OpenTelemetry | 8.0.9 | Npgsql 추적(`AddNpgsql()`) | PostgreSQL | netstandard2.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/npgsql.opentelemetry/8.0.9/npgsql.opentelemetry.nuspec) | 2026-03-12, 폐기 표시 없음. 의존 Npgsql 8.0.9, OpenTelemetry.API **1.6.0**(하한, GHSA-g94r-2vxg-569j 범위). ServiceDefaults를 참조하지 않는 프로젝트(예: BuildingBlocks.Infrastructure)에서는 1.6.0이 그대로 풀려 NU1902가 날 수 있으므로, CPM에 `OpenTelemetry.Api` 1.19.1을 명시해 전이 고정한다(S01-T05, [ADR-0011](adr/0011-use-aspire-local-orchestration.md), [ADR-0020](adr/0020-logging-with-serilog-and-otlp.md)) |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 8.0.31 | DbContext 헬스체크(`AddDbContextCheck<T>()`) | MIT | net8.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/microsoft.extensions.diagnostics.healthchecks.entityframeworkcore/8.0.31/microsoft.extensions.diagnostics.healthchecks.entityframeworkcore.nuspec) | 2026-09-08, EF 런타임과 같은 패치. 의존 EF Core Relational 8.0.31, HealthChecks 8.0.31 |
| EFCore.NamingConventions | 8.0.3 | snake_case 변환 | Apache-2.0 | net8.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/efcore.namingconventions/8.0.3/efcore.namingconventions.nuspec) | EF `[8.0.0, 9.0.0)` |
| UUIDNext | 4.2.4 | UUID v7 생성 | 0BSD | net8.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/uuidnext/4.2.4/uuidnext.nuspec) | 의존 없음. `Uuid.NewDatabaseFriendly(Database.PostgreSql)` 컴파일 확인 |

## 애플리케이션

| 패키지 | 버전 | 용도 | 라이선스 | 대상 프레임워크 | 출처 | 비고 |
|---|---|---|---|---|---|---|
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.0 | BuildingBlocks.Application 추상화(`GetService<T>`, ADR-0015) | MIT | net462, netstandard2.0, netstandard2.1, net8.0, net9.0, net10.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/microsoft.extensions.dependencyinjection.abstractions/10.0.0/microsoft.extensions.dependencyinjection.abstractions.nuspec) | 2025-11-11. net8.0 그룹 의존 없음. **8.0.x가 아니라 10.0.0**: Scrutor 7이 `>= 10.0.0`을 요구해 8.0.2로 고정하면 전이 고정 하향 NU1109(스크래치 restore 실측, S02-T01). 최신 패치 10.0.12 대신 전이 요구 하한과 같은 10.0.0으로 두어 다른 전이 버전을 올리지 않음(TD-008) |
| Microsoft.Extensions.Logging.Abstractions | 10.0.0 | BuildingBlocks.Application 로깅 추상화(데코레이터 `ILogger<T>`, S02-T02) | MIT | net462, netstandard2.0, net8.0, net9.0, net10.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/microsoft.extensions.logging.abstractions/10.0.0/microsoft.extensions.logging.abstractions.nuspec) | net8.0 그룹 의존 DependencyInjection.Abstractions 10.0.0 · System.Diagnostics.DiagnosticSource 10.0.0. 위와 같은 이유로 10.0.0. `dotnet list package --vulnerable --include-transitive` 취약 패키지 없음 (S02-T01) |
| Scrutor | 7.0.0 | 어셈블리 검색 DI 자동 등록(ADR 0010 구체화) | MIT | net462, netstandard2.0, net8.0, net10.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/scrutor/7.0.0/scrutor.nuspec) | net8.0에서 Microsoft.Extensions.DependencyInjection.Abstractions · DependencyModel **10.0.0** 전이. 6.1.0은 8.0.x 의존 |
| FluentValidation | 12.1.1 | 입력 검증 | Apache-2.0 | net8.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/fluentvalidation/12.1.1/fluentvalidation.nuspec) | 12.x에서도 라이선스 변경 없음(저장소 라이선스 Apache-2.0). 12부터 net8.0 전용. BuildingBlocks.Application이 직접 참조(검증 데코레이터 · `WithError` · `RequestValidator`, S02-T02). net8.0 그룹 의존 없음 |
| FluentValidation.DependencyInjectionExtensions | 12.1.1 | `AddValidatorsFromAssembly` | Apache-2.0 | net8.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/fluentvalidation.dependencyinjectionextensions/12.1.1/fluentvalidation.dependencyinjectionextensions.nuspec) | |
| Serilog.AspNetCore | 10.0.0 | Serilog 통합(`UseSerilog`) | Apache-2.0 | net462, netstandard2.0/2.1, net8.0, net9.0, net10.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/serilog.aspnetcore/10.0.0/serilog.aspnetcore.nuspec) | Serilog 4.3.0, Sinks.Console 6.1.1, Sinks.File 7.0.0, Formatting.Compact 3.0.0, Settings.Configuration 10.0.0을 전이로 포함. net8.0에서 Microsoft.Extensions.* 10.0.0 전이 |
| Serilog.Sinks.Console | 6.1.1 (전이) | 콘솔 텍스트 로그 | Apache-2.0 | net462, net471, net6.0, net8.0, netstandard2.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/serilog.sinks.console/6.1.1/serilog.sinks.console.nuspec) | Serilog.AspNetCore 10.0.0에 포함 |
| Serilog.Sinks.File | 7.0.0 (전이) | 파일 JSON 로그 | Apache-2.0 | net462, net471, net6.0, net8.0, net9.0, netstandard2.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/serilog.sinks.file/7.0.0/serilog.sinks.file.nuspec) | 같음 |
| Serilog.Formatting.Compact | 3.0.0 (전이) | CLEF(JSON) 형식 | Apache-2.0 | net462, net471, net6.0, net8.0, netstandard2.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/serilog.formatting.compact/3.0.0/serilog.formatting.compact.nuspec) | 같음 |
| Serilog.Sinks.OpenTelemetry | 4.2.0 | OTLP 로그 싱크(Aspire 대시보드) | Apache-2.0 | net462, net471, net6.0, net8.0, net9.0, netstandard2.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/serilog.sinks.opentelemetry/4.2.0/serilog.sinks.opentelemetry.nuspec) | Google.Protobuf 3.30.1, Grpc.Net.Client 2.70.0 전이. OTel SDK 로그와 중복 방지는 [ADR-0020](adr/0020-logging-with-serilog-and-otlp.md) |
| Serilog.Sinks.Async | 2.1.0 | 파일 싱크 비동기 쓰기(`WriteTo.Async`) | Apache-2.0 | net462, net471, net6.0, net8.0, netstandard2.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/serilog.sinks.async/2.1.0/serilog.sinks.async.nuspec), [저장소](https://github.com/serilog/serilog-sinks-async) | 2024-10-24, 최신 안정판, 폐기 표시 없음. 의존 Serilog 4.1.0 이상(Serilog.AspNetCore 10.0.0의 4.3.0으로 충족) |
| Serilog.Enrichers.Environment | 3.0.1 | `Enrich.WithMachineName()` | Apache-2.0 | net462, net471, net6.0, net8.0, netstandard2.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/serilog.enrichers.environment/3.0.1/serilog.enrichers.environment.nuspec), [저장소](https://github.com/serilog/serilog-enrichers-environment) | 2024-06-20, 최신 안정판, 폐기 표시 없음. 의존 Serilog 4.0.0 이상 |
| Swashbuckle.AspNetCore | 10.2.3 | OpenAPI 문서 · Swagger UI | MIT | net8.0, net9.0, net10.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/swashbuckle.aspnetcore/10.2.3/swashbuckle.aspnetcore.nuspec) | Microsoft.OpenApi 2.7.5 전이(2.x API, 예전 1.x 예제와 네임스페이스가 다름: `Microsoft.OpenApi` 네임스페이스, `ISchemaFilter.Apply(IOpenApiSchema, ...)`, `JsonSchemaType`). BuildingBlocks.Api가 직접 참조(S02-T06), Microsoft.Extensions.ApiDescription.Server 8.0.0 전이. `dotnet list package --vulnerable --include-transitive` 취약 패키지 없음 |

- 상용 전환된 MediatR · AutoMapper · FluentAssertions v8+ · MassTransit v9+는 쓰지 않습니다(Mediator는 직접 구현).

## 테스트

| 패키지 | 버전 | 용도 | 라이선스 | 대상 프레임워크 | 출처 | 비고 |
|---|---|---|---|---|---|---|
| xunit.v3 | 4.0.1 | 테스트 프레임워크(v3, 채택) | Apache-2.0 | net472, net8.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/xunit.v3/4.0.1/xunit.v3.nuspec) | Microsoft Testing Platform v2 내장. xunit.analyzers 2.1.0 → **SDK 8.0.4xx 이상 필요** |
| xunit | 2.9.3 | 테스트 프레임워크(v2, 비교용 · 미사용) | Apache-2.0 | (메타 패키지) | [nuspec](https://api.nuget.org/v3-flatcontainer/xunit/2.9.3/xunit.nuspec) | NuGet 폐기(`Legacy`): "security issues만 갱신, 기능은 v3로" |
| xunit.runner.visualstudio | 4.0.0 | VSTest 어댑터(`dotnet test`) | Apache-2.0 | net472, net8.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/xunit.runner.visualstudio/4.0.0/xunit.runner.visualstudio.nuspec) | v1 · v2 · v3 모두 실행. `PrivateAssets=all` |
| Microsoft.NET.Test.Sdk | 18.10.1 | VSTest 호스트 | MIT | net8.0 외 | [nuspec](https://api.nuget.org/v3-flatcontainer/microsoft.net.test.sdk/18.10.1/microsoft.net.test.sdk.nuspec) | |
| AwesomeAssertions | 9.6.0 | 단언(FluentAssertions 7 포크) | Apache-2.0 | net6.0, net8.0, netstandard2.0/2.1 | [nuspec](https://api.nuget.org/v3-flatcontainer/awesomeassertions/9.6.0/awesomeassertions.nuspec) | 의존 없음(net8.0) |
| NSubstitute | 6.2.0 | Test Double | BSD-3-Clause | net8.0, netstandard2.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/nsubstitute/6.2.0/nsubstitute.nuspec) | Castle.Core 5.1.1 전이 |
| NSubstitute.Analyzers.CSharp | 1.0.17 | NSubstitute 오용 분석기 | MIT (패키지 내 LICENSE.md, [저장소](https://github.com/nsubstitute/NSubstitute.Analyzers) MIT) | netstandard2.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/nsubstitute.analyzers.csharp/1.0.17/nsubstitute.analyzers.csharp.nuspec) | 2024-02 이후 릴리스 없음. `PrivateAssets=all` |
| Testcontainers.PostgreSql | 4.15.0 | 통합 테스트 DB 컨테이너 | MIT | net8.0, netstandard2.0/2.1 | [nuspec](https://api.nuget.org/v3-flatcontainer/testcontainers.postgresql/4.15.0/testcontainers.postgresql.nuspec) | dba 확인. 이미지 인자 필수(TD-004) |
| Respawn | 7.0.0 | 통합 테스트 DB 초기화 | Apache-2.0 | netstandard2.1 | [nuspec](https://api.nuget.org/v3-flatcontainer/respawn/7.0.0/respawn.nuspec) | dba 확인. 의존 없음 |
| Microsoft.Extensions.TimeProvider.Testing | 10.10.0 | `FakeTimeProvider` | MIT | net8.0 외 | [nuspec](https://api.nuget.org/v3-flatcontainer/microsoft.extensions.timeprovider.testing/10.10.0/microsoft.extensions.timeprovider.testing.nuspec) | net8.0 그룹 의존 없음 → .NET 8 내장 `TimeProvider`와 그대로 호환 |
| Microsoft.Extensions.Diagnostics.Testing | 10.10.0 | `FakeLogger` · `FakeLogCollector`(로그 수준 · 이벤트 ID · 구조화 속성 · 개인정보 없음 단언) | MIT | net462, netstandard2.0, net8.0, net9.0, net10.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/microsoft.extensions.diagnostics.testing/10.10.0/microsoft.extensions.diagnostics.testing.nuspec) | S02-T02 추가(ADR 없이 기록, S02 계획 리뷰). net8.0 그룹 의존 Telemetry.Abstractions 10.10.0 · Logging 8.0.1 · Options.ConfigurationExtensions 8.0.0. 실측 해석: Logging.Abstractions · DependencyInjection.Abstractions · DiagnosticSource는 중앙 고정 10.0.0 유지(하향 NU1109 없음), Compliance.Abstractions 10.10.0 · ObjectPool 8.0.31 전이. 테스트 프로젝트만 참조. `dotnet list package --vulnerable --include-transitive` 취약 패키지 없음 |
| Microsoft.Extensions.DependencyInjection | 10.0.0 | 실제 `ServiceProvider`(`ValidateOnBuild` · `ValidateScopes`)로 DI 등록 · 데코레이터 체인 해석 확인 | MIT | net462, netstandard2.0, netstandard2.1, net8.0, net9.0, net10.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/microsoft.extensions.dependencyinjection/10.0.0/microsoft.extensions.dependencyinjection.nuspec) | S02-T03 추가, 테스트 프로젝트(Application · Infrastructure UnitTests)만 직접 참조. S02-T04 관찰: BuildingBlocks.Infrastructure가 Npgsql.EntityFrameworkCore.PostgreSQL을 참조하면서 EF Core 8.0.31의 DI 의존(8.0.x 하한)이 전이 고정으로 10.0.0에 해석된다(제품 어셈블리에도 유입, NU1109 없음, 빌드 · 단위 테스트 이상 없음, TD-008 관찰 대상). net8.0 그룹 의존 DependencyInjection.Abstractions 10.0.0(중앙 고정과 같음). 고정 전에는 Diagnostics.Testing → Logging 8.0.1 전이로 8.0.1이 해석돼 Abstractions 10.0.0과 버전이 어긋났으므로 Abstractions와 같은 10.0.0으로 맞춤. 앱 호스트는 공유 프레임워크 DI를 쓰므로 영향 없음(TD-008 관찰 대상). Scrutor 7 `Decorate`는 감싼 안쪽 단계를 keyed 등록으로 남기므로 `IKeyedServiceProvider`가 필요(8.0 이상 충족). `dotnet list package --vulnerable --include-transitive` 취약 패키지 없음 |
| Microsoft.AspNetCore.Mvc.Testing | 8.0.31 | `WebApplicationFactory<Program>`(HTTP 통합 테스트, Employee.IntegrationTests) | MIT | net8.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/microsoft.aspnetcore.mvc.testing/8.0.31/microsoft.aspnetcore.mvc.testing.nuspec) | S03-T07 추가(ADR 없이 기록). ASP.NET Core 8 런타임과 같은 패치(8.0.x 최신). net8.0 그룹 의존 Microsoft.AspNetCore.TestHost 8.0.31 · Extensions.DependencyModel 8.0.2(해석 10.0.0) · Extensions.Hosting 8.0.1. `dotnet list package --vulnerable --include-transitive` 취약 패키지 없음 |
| NetArchTest.Rules | 1.3.2 | 아키텍처 테스트 | MIT ([저장소](https://github.com/BenMorris/NetArchTest)) | netstandard2.0 | [nuspec](https://api.nuget.org/v3-flatcontainer/netarchtest.rules/1.3.2/netarchtest.rules.nuspec) | 2021-05 이후 릴리스 없음(nuspec에 라이선스 메타데이터 없음, 저장소 MIT). Mono.Cecil 0.11.3 전이 |
| coverlet.collector | 10.0.1 | VSTest 커버리지 수집(`--collect:"XPlat Code Coverage"`) | MIT | 수집기(도구 패키지) | [nuspec](https://api.nuget.org/v3-flatcontainer/coverlet.collector/10.0.1/coverlet.collector.nuspec), [릴리스](https://github.com/coverlet-coverage/coverlet/releases/tag/v10.0.1) | 8.0.0부터 .NET 8 SDK · 런타임 이상 필요. `PrivateAssets=all` |

테스트 도구 선택과 고정은 [ADR-0021](adr/0021-test-tooling-xunit-v3-and-awesomeassertions.md)(xUnit v3, AwesomeAssertions, 주변 도구), Respawn · 커버리지는 [ADR-0022](adr/0022-respawn-and-coverage-tooling.md)가 원본입니다. 목록의 `xunit` 2.9.3(v2)은 비교용으로 남긴 것이며 쓰지 않습니다.

### xUnit v2 / v3 차이

| 항목 | v2 (`xunit` 2.9.3) | v3 (`xunit.v3` 4.0.1) |
|---|---|---|
| 유지 상태 | NuGet `Legacy` 폐기, 보안 수정만 | 활발히 개발(4.0.0 2026-08-14) |
| 테스트 프로젝트 | 라이브러리 | 실행 파일(`OutputType=Exe`) |
| 실행 방식 | VSTest(`xunit.runner.visualstudio`) | Microsoft Testing Platform v2 내장 + VSTest(`xunit.runner.visualstudio`)도 가능. .NET 8 SDK의 `dotnet test`는 VSTest 경로 |
| 기능 | 기본 | `TestContext`, 비동기 수명 주기 개선, 어셈블리 fixture, 4.0에서 Native AOT · 완전 병렬화 · 클래스 / 메서드 정렬자 추가 |
| 최소 SDK | 8.0.202에서 빌드 성공 | xunit.analyzers 2.1.0(Roslyn 4.11) 때문에 8.0.4xx 필요 |
| coverlet.collector | 동작 확인 | 동작 확인(VSTest 경로) |

출처: [xUnit v3 4.0.0 릴리스 노트](https://github.com/xunit/xunit.net/blob/main/site/releases/v3/4.0.0.md), 각 nuspec. **v3로 확정했습니다**([ADR-0021](adr/0021-test-tooling-xunit-v3-and-awesomeassertions.md)).

## 도구

| 패키지 | 버전 | 용도 | 라이선스 | 대상 프레임워크 | 출처 | 비고 |
|---|---|---|---|---|---|---|
| dotnet-ef | 8.0.31 | 마이그레이션 CLI | MIT | .NET 도구 | [nuspec](https://api.nuget.org/v3-flatcontainer/dotnet-ef/8.0.31/dotnet-ef.nuspec) | EF 런타임과 같은 패치. 로컬 도구 매니페스트 |
| dotnet-reportgenerator-globaltool | 5.5.11 | 커버리지 보고서 | Apache-2.0 | .NET 도구 | [nuspec](https://api.nuget.org/v3-flatcontainer/dotnet-reportgenerator-globaltool/5.5.11/dotnet-reportgenerator-globaltool.nuspec) | 로컬 도구 매니페스트. cobertura → TextSummary 생성 확인 |

## GitHub Actions

| 액션 | 고정 버전 | 커밋 SHA | 런타임 | 라이선스 | 출처 | 비고 |
|---|---|---|---|---|---|---|
| actions/checkout | v7.0.1 (메이저 `v7`) | `3d3c42e5aac5ba805825da76410c181273ba90b1` | node24 | MIT | [releases/latest](https://github.com/actions/checkout/releases/tag/v7.0.1) | 2026-07-20 |
| actions/setup-dotnet | v6.0.0 (메이저 `v6`) | `a98b56852c35b8e3190ac28c8c2271da59106c68` | node24 | MIT | [releases/latest](https://github.com/actions/setup-dotnet/releases/tag/v6.0.0) | 2026-07-16. `global-json-file` + `latestFeature` 지원 |
| actions/upload-artifact | v7.0.1 (메이저 `v7`) | `043fb46d1a93c77aae656e7c1c64a875d1fc6a0a` | node24 | MIT | [releases/latest](https://github.com/actions/upload-artifact/releases/tag/v7.0.1) | 2026-04-10 |

**고정 방식(S01-T07 결정): 커밋 SHA 고정 + 버전 주석.** 워크플로([`.github/workflows/ci.yml`](../../.github/workflows/ci.yml))는 `uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1`처럼 전체 40자 SHA로 참조하고 줄 끝 주석에 릴리스 태그를 적습니다.

- 근거 ① 재현성: 메이저 태그(`@v7`)는 새 릴리스마다 옮겨지는 가변 참조라 같은 커밋의 CI가 날짜에 따라 다른 액션 코드로 돌 수 있습니다. 완료 조건 "액션 버전 고정"과 이 문서의 고정 버전 표(패치까지)를 그대로 지키는 방법은 SHA뿐입니다.
- 근거 ② 공급망 보안: 태그는 저장소 권한이 있으면 다른 커밋으로 다시 붙일 수 있습니다(2025-03 `tj-actions/changed-files` 태그 변조 사고). GitHub 보안 강화 가이드도 SHA 고정을 권장합니다([Security hardening for GitHub Actions](https://docs.github.com/en/actions/security-for-github-actions/security-guides/security-hardening-for-github-actions#using-third-party-actions)). 워크플로는 `permissions: contents: read`, `persist-credentials: false`로 토큰 노출도 줄입니다.
- 비용: 업데이트를 사람이 해야 하고 SHA만으로는 버전을 알기 어렵습니다. 버전 주석으로 가독성을 보완하고, 업데이트는 이 표를 먼저 고친 뒤 워크플로의 SHA · 주석을 같은 커밋에서 바꿉니다. Dependabot은 SHA + 버전 주석 형식을 갱신할 수 있으나 아직 설정하지 않았습니다.
- SHA 확인: `gh api repos/<owner>/<repo>/commits/<tag> --jq .sha`로 태그가 가리키는 커밋을 조회해 표의 SHA와 일치함을 확인했습니다(2026-09-27, 3종 모두 일치).
- NuGet 캐시: `setup-dotnet`의 `cache`는 `packages.lock.json`이 있어야 하고 저장소에 잠금 파일이 없어 쓰지 않습니다. 별도 `actions/cache` 도입도 하지 않습니다(액션 추가 없이 NFR-07 10분 안에 들어오는지 먼저 측정).

## 라이선스 점검

| 라이선스 | 패키지 |
|---|---|
| MIT | Aspire 계열(Dashboard.Sdk 제외), Microsoft.Extensions.*(HealthChecks.EntityFrameworkCore 포함), EF Core 계열, dotnet-ef, Scrutor, Swashbuckle, Microsoft.NET.Test.Sdk, Testcontainers, coverlet, NSubstitute.Analyzers, NetArchTest, MessagePack, GitHub Actions 3종, .NET SDK(소스 · Linux · macOS 배포물), PostgreSQL 이미지 빌드 파일 |
| Apache-2.0 | FluentValidation, Serilog 계열(Sinks.Async · Enrichers.Environment 포함), OpenTelemetry 계열, xUnit(v2 · v3 · runner), AwesomeAssertions, Respawn, EFCore.NamingConventions, ReportGenerator |
| BSD-3-Clause | NSubstitute |
| PostgreSQL | Npgsql, Npgsql.EntityFrameworkCore.PostgreSQL, Npgsql.OpenTelemetry, PostgreSQL 서버(이미지) |
| 0BSD | UUIDNext |
| Microsoft 사용 조건(무료) | Aspire.Dashboard.Sdk.&lt;rid&gt;(패키지 `EULA.md`), .NET SDK Windows 배포물(.NET Library License) |

상용 · 유료 라이선스는 0건입니다(NFR-05). Microsoft 사용 조건 2종은 오픈 소스가 아니지만 무료이고 개발 · 테스트 사용을 허용합니다. 확인 방법: 각 nuspec의 `<license type="expression">`, 파일 라이선스이거나 메타데이터가 없는 2건(NSubstitute.Analyzers.CSharp, NetArchTest.Rules)은 GitHub 저장소 라이선스(`gh api repos/<owner>/<repo>`의 `license.spdx_id`)로 확인했습니다.

## 스크래치 restore 검증

저장소 밖 스크래치 디렉터리에 net8.0 솔루션(AppHost, ServiceDefaults, Api, TestsV2, TestsV3)을 만들고 위 고정 버전을 CPM(`CentralPackageTransitivePinningEnabled`)으로 넣어 확인했습니다. 설정: `TreatWarningsAsErrors=true`, `NuGetAudit=true`, `NuGetAuditMode=all`, `NuGetAuditLevel=low`, nuget.org 단일 소스.

| 단계 | SDK | 결과 |
|---|---|---|
| `dotnet restore` (MessagePack 고정 없음) | 8.0.202 / 8.0.425 | **실패**: AppHost에서 MessagePack 2.5.192 NU1902 9건 · NU1903 2건 |
| `dotnet restore` (기본 `NuGetAuditMode`, 고정 없음) | 8.0.425 | 성공(경고 없음). .NET 8 SDK 기본값은 직접 참조만 감사해서 전이 취약점을 **놓침** |
| `dotnet restore` (MessagePack 2.5.305 고정) | 8.0.202 / 8.0.425 | 성공, 경고 0 |
| `dotnet build` | 8.0.202 | 실패: TestsV3만 `CS9057`(xunit.analyzers 2.1.0이 Roslyn 4.11 요구) |
| `dotnet build` | 8.0.425 | 성공, 경고 0 · 오류 0 |
| `dotnet test --collect:"XPlat Code Coverage"` | 8.0.425 | TestsV2 1/1, TestsV3 1/1 통과, cobertura 생성 |
| `dotnet tool restore` + `reportgenerator` | 8.0.425 | 성공(TextSummary 생성) |
| `dotnet list package --vulnerable --include-transitive` | 8.0.425 | 5개 프로젝트 모두 취약 패키지 없음 |
| `dotnet list package --deprecated` | 8.0.425 | AppHost: Aspire 9.5.2 4건(`Other,Legacy`), TestsV2: xunit 2.9.3(`Legacy`) |
| 클라이언트 통합 추가(Aspire.Npgsql.EFCore 9.5.2) | 8.0.425 | restore · build 경고 0, OpenTelemetry.Api 1.19.1로 통일 |

스모크 테스트는 `FakeTimeProvider`, NSubstitute, AwesomeAssertions, NetArchTest, `PostgreSqlBuilder("postgres:17")`, Respawn 타입 참조를 컴파일 · 실행했습니다. Api는 Scrutor `Scan`, `AddValidatorsFromAssemblyContaining`, `UseSerilog`(콘솔 · 파일 CLEF · OpenTelemetry 싱크), Swashbuckle, `UseNpgsql(EnableRetryOnFailure)` + `UseSnakeCaseNamingConvention`, UUIDNext를 컴파일했습니다. Docker를 쓰는 실행(컨테이너 기동, 생성 스크립트)은 확인하지 않았습니다(BL-003).

## 결정 · 미해결

| 구분 | 항목 | 처리 |
|---|---|---|
| 결정 | Aspire 9.5.2 고정(Q19 해소) | 사용자 결정. 지원 종료는 Aspire ADR(S01-T02)에 명시 |
| 결정 | EF 계열 8.0.31 / Npgsql.EFCore 8.0.11 / Npgsql 8.0.9 / NamingConventions 8.0.3, PostgreSQL `17` | dba 확인 |
| 권장 | `global.json` 8.0.400 + `latestFeature`, 로컬 SDK 8.0.425 설치 | S01-T05에서 적용 |
| 권장 | `Directory.Build.props`에 `NuGetAuditMode=all` 명시, CPM에 `MessagePack` 2.5.305 전이 고정 | S01-T05에서 적용 |
| 결정 | 클라이언트 통합 사용하지 않음 | [ADR-0011](adr/0011-use-aspire-local-orchestration.md) (BL-004 · BL-010) |
| 미해결 | `WithCreationScript` · 롤 생성 실측 | BL-003 |
| 결정 | xUnit v3 · AwesomeAssertions · 테스트 도구 고정 | [ADR-0021](adr/0021-test-tooling-xunit-v3-and-awesomeassertions.md) (TD-009 해소) |
| 결정 | ServiceDiscovery · Http.Resilience는 템플릿대로 포함 | [ADR-0011](adr/0011-use-aspire-local-orchestration.md) (BL-009) |
| 결정 | ServiceDefaults OpenTelemetry 1.19.x | [ADR-0020](adr/0020-logging-with-serilog-and-otlp.md) (BL-007) |
| 결정 | GitHub Actions는 커밋 SHA 고정 + 버전 주석, NuGet 캐시 미사용 | S01-T07 ([GitHub Actions](#github-actions)) |
| 기술부채 | TD-003 Npgsql.EFCore EF 의존 상한 없음 → 전이 고정 | 기존 |
| 기술부채 | TD-004 Testcontainers 이미지 인자 필수 | 기존 |
| 기술부채 | TD-005 Aspire 9.x 지원 종료 상태로 9.5.2 사용 | 기존, 재검토 BL-002(.NET 10 · Aspire 13 전환) |
| 기술부채 | TD-006 클라이언트 통합 사용 시 OTel 버전 혼재 | 기존. 위 판단 자료로 해소 가능 |
| 기술부채 | TD-007 Aspire.Hosting 9.5.2 전이 MessagePack 취약 → 수동 고정 유지 | 기존 |
| 기술부채 | TD-008 net8.0 앱에 Microsoft.Extensions.* · DiagnosticSource 10.0.0 전이 유입(Scrutor 7, Serilog 10, OTel 1.19) | 기존. OTel은 피할 수 없음(취약점 없는 버전이 모두 DiagnosticSource 10 요구). S02-T01 관찰: BuildingBlocks.Application이 DependencyInjection · Logging.Abstractions 10.0.0을 직접 고정(8.0.x 고정은 NU1109), net8.0 빌드 · 단위 테스트 51건에서 이상 없음. 런타임 호환은 S03 호스트 실행 때 계속 관찰 |

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | developer | 문서 생성 (S01-T01 사전 확인: Aspire 9.5.2, SDK, 패키지 버전 · 라이선스, 스크래치 restore 검증) |
| 2026-09-27 | developer | ADR 0011 · 0020 · 0021 · 0022 반영: 클라이언트 통합 미사용 · 의존 버전, 폐기 표시(클라이언트 통합 · Dashboard.Sdk · Orchestration), `WithInitFiles`, Npgsql.OpenTelemetry · HealthChecks.EFCore · Serilog.Sinks.Async · Serilog.Enrichers.Environment 행, PostgreSQL 이미지 · .NET SDK 라이선스, xUnit v3 확정, 결정 · 미해결 표 갱신 (S01-T04) |
| 2026-09-27 | developer | 테스트 표 사이 설명 문단을 표 뒤로 이동(표 끊김 수정), CI 작업 번호를 S01-T07로 정정(2곳), xUnit 용도 열 "채택" / "비교용 · 미사용" (S01-T04) |
| 2026-09-27 | developer | GitHub Actions 고정 방식 결정(커밋 SHA + 버전 주석, 태그 → SHA 일치 확인), NuGet 캐시 미사용, 결정 · 미해결 표 갱신 (S01-T07) |
| 2026-09-27 | developer | Microsoft.Extensions.DependencyInjection.Abstractions · Logging.Abstractions 10.0.0 행 추가(8.0.x 고정 시 NU1109 실측), TD-008 관찰 기록 (S02-T01) |
| 2026-09-27 | developer | Microsoft.Extensions.Diagnostics.Testing 10.10.0 행 추가(FakeLogger, 전이 해석 실측), FluentValidation Application 직접 참조 비고 (S02-T02) |
| 2026-09-27 | developer | Microsoft.Extensions.DependencyInjection 10.0.0 행 추가(테스트 전용, Abstractions와 버전 맞춤), BuildingBlocks.Infrastructure가 Scrutor · FluentValidation.DependencyInjectionExtensions · UUIDNext 직접 참조 (S02-T03) |
| 2026-09-27 | developer | BuildingBlocks.Infrastructure가 Npgsql.EntityFrameworkCore.PostgreSQL · EFCore.NamingConventions 직접 참조, EF Core 경유 DependencyInjection 10.0.0 전이 해석 관찰, Infrastructure.UnitTests가 TimeProvider.Testing 참조. `dotnet list package --vulnerable --include-transitive` 취약 패키지 없음 (S02-T04) |
| 2026-09-27 | developer | BuildingBlocks.Api(FrameworkReference `Microsoft.AspNetCore.App`)가 Swashbuckle.AspNetCore를 직접 참조, Api.UnitTests는 기존 테스트 패키지만(새 패키지 없음). 취약 패키지 없음 (S02-T06) |
| 2026-09-28 | developer | Microsoft.AspNetCore.Mvc.Testing 8.0.31 행 추가(Employee.IntegrationTests, Api 프로젝트 참조), 취약 패키지 없음 (S03-T07) |
