---
title: "ADR-0021: 테스트 도구 (xUnit v3 · AwesomeAssertions와 주변 도구 고정)"
type: adr
adr: "0021"
status: accepted
date: 2026-09-27
deciders: []
supersedes:
superseded_by:
aliases: [ADR-0021]
tags: [adr, architecture, testing]
created: 2026-09-27
updated: 2026-09-27
---

# ADR-0021: 테스트 도구 (xUnit v3 · AwesomeAssertions와 주변 도구 고정)

## 배경 (Context)

- 도메인 · 애플리케이션 로직은 테스트를 먼저 쓴다([ADR-0006](0006-adopt-tdd.md)). [테스트 전략](../../04-development/testing-strategy.md#도구)은 테스트 프레임워크를 xUnit으로 적었지만 **버전(v2 / v3)은 정하지 않았고**, 단언 라이브러리는 🟡로 남겼다. FluentAssertions는 v8부터 상용 라이선스로 바뀌었고(8.0.0 nuspec이 파일 라이선스), 이 프로젝트는 상용 라이선스 패키지를 쓰지 않는다([PRD-001](../../10-delivery/prd/PRD-001-foundation.md) NFR-05).
- [PRD-001 질문과 답변](../../10-delivery/prd/PRD-001-foundation.md#질문과-답변) Q5에서 단언은 추천안(AwesomeAssertions)으로 정했고, [S01 계획 리뷰](../../10-delivery/sprints/S01-decisions-build-ci.md#계획-리뷰)에서 xUnit 버전은 S01-T04에서 확정하기로 했다.
- S01-T01 사전 확인 결과([패키지 버전 · 라이선스 · 테스트](../package-versions.md#테스트), [xUnit v2 / v3 차이](../package-versions.md#xunit-v2--v3-차이)):
  - v2(`xunit` 2.9.3)는 NuGet에서 `Legacy`로 폐기 표시되어 보안 수정만 받는다(TD-009).
  - v3(`xunit.v3` 4.0.1)는 활발히 개발 중이지만, 함께 오는 `xunit.analyzers` 2.1.0이 Roslyn 4.11을 요구해 **SDK 8.0.4xx 이상**이 필요하다. SDK 8.0.202에서는 `CS9057`로 빌드가 실패했고 8.0.425에서는 성공했다. 권장 `global.json`은 이미 `8.0.400` + `rollForward: latestFeature`다([SDK · global.json](../package-versions.md#sdk--globaljson)). 로컬 SDK 설치는 사용자 작업(BL-005)이다.
  - .NET 8 SDK의 `dotnet test`는 VSTest 경로로 실행하며, v2 · v3 모두 `xunit.runner.visualstudio` 4.0.0 + `coverlet.collector`로 테스트 · 커버리지 수집이 스크래치에서 동작했다.
- Test Double(NSubstitute), 통합 DB(Testcontainers), 시간 고정(TimeProvider.Testing), 아키텍처 테스트(NetArchTest)는 기술 스택에서 선택은 끝났지만 버전이 고정되지 않았다.

## 검토한 대안 (Options)

테스트 프레임워크:

1. **xUnit v2 (`xunit` 2.9.3)**: 장점: SDK 8.0.202에서도 빌드되고 자료가 많다. 단점: 폐기 표시(`Legacy`) 상태로 기능 개선이 없고, 새 저장소를 폐기 패키지로 시작한다.
2. **xUnit v3 (`xunit.v3` 4.0.1)**: 장점: 유지되는 줄이고, `TestContext` · 어셈블리 fixture · 비동기 수명 주기 개선이 있다. 단점: SDK 8.0.4xx 이상이 필요하고, 테스트 프로젝트가 실행 파일(`OutputType=Exe`)이며 v2 예제와 일부 API가 다르다(`IAsyncLifetime`이 `ValueTask` 반환).

단언:

1. **FluentAssertions 7.x 고정(7.2.2, Apache-2.0)**: 장점: 가장 널리 알려진 API. 단점: 상용 전환된 8+로 올릴 수 없는 막다른 줄이고, 실수로 8+로 올리면 라이선스 위반이 된다.
2. **Shouldly 4.3.0(BSD-3-Clause)**: 장점: 단순하고 무료. 단점: 컬렉션 · 객체 그래프 비교(`BeEquivalentTo`)가 약하고, 팀이 익숙한 `Should().Be()` 체인과 다르다.
3. **AwesomeAssertions 9.6.0(Apache-2.0)**: FluentAssertions 7의 커뮤니티 포크. 장점: FluentAssertions와 같은 `Should()` 체인 · 객체 그래프 비교, net8.0에서 의존 없음, 활발히 유지된다. 단점: 네임스페이스가 `AwesomeAssertions`이고 커뮤니티 규모가 FluentAssertions보다 작다.
4. **xUnit 내장 `Assert`만 사용**: 장점: 의존이 없다. 단점: 실패 메시지와 컬렉션 · 객체 비교 표현력이 떨어진다.

## 결정 (Decision)

**xUnit v3(`xunit.v3` 4.0.1)과 AwesomeAssertions 9.6.0을 채택하고, 주변 테스트 도구의 버전을 아래 표로 고정한다.**

| 용도 | 패키지 | 버전 | 라이선스 | 참조 방식 |
|---|---|---|---|---|
| 테스트 프레임워크 | xunit.v3 | 4.0.1 | Apache-2.0 | 모든 테스트 프로젝트 |
| VSTest 어댑터 | xunit.runner.visualstudio | 4.0.0 | Apache-2.0 | 모든 테스트 프로젝트, `PrivateAssets=all` |
| VSTest 호스트 | Microsoft.NET.Test.Sdk | 18.10.1 | MIT | 모든 테스트 프로젝트 |
| 단언 | AwesomeAssertions | 9.6.0 | Apache-2.0 | 모든 테스트 프로젝트 |
| Test Double | NSubstitute | 6.2.0 | BSD-3-Clause | 단위 테스트 |
| Test Double 분석기 | NSubstitute.Analyzers.CSharp | 1.0.17 | MIT | NSubstitute를 쓰는 프로젝트, `PrivateAssets=all` |
| 시간 고정 | Microsoft.Extensions.TimeProvider.Testing | 10.10.0 | MIT | 단위 테스트(`FakeTimeProvider`) |
| 통합 DB 컨테이너 | Testcontainers.PostgreSql | 4.15.0 | MIT | 통합 테스트. 이미지 인자 필수(`postgres:17`, TD-004) |
| 아키텍처 테스트 | NetArchTest.Rules | 1.3.2 | MIT(저장소) | 아키텍처 테스트 |

- Respawn과 커버리지 도구(coverlet.collector, ReportGenerator)는 [ADR-0022](0022-respawn-and-coverage-tooling.md)에서 정한다.
- **버전 관리**: 모든 버전은 `Directory.Packages.props`(중앙 패키지 관리)에만 적는다(S01-T05). 출처와 확인 방법은 [패키지 버전 · 라이선스](../package-versions.md#테스트)가 원본이다.
- **실행 경로**: .NET 8 SDK의 `dotnet test`(VSTest) 경로를 쓴다. 테스트 프로젝트는 `OutputType=Exe`, `IsTestProject=true`이며, Microsoft Testing Platform 모드(`TestingPlatformDotnetTestSupport`)는 켜지 않는다. 이 경로에서 `--collect "XPlat Code Coverage"`가 동작한다(S01-T01 스크래치 확인).
- **SDK 하한**: `global.json`은 `8.0.400` + `rollForward: latestFeature`로 둔다(S01-T05). 8.0.4xx 미만 SDK에서는 빌드가 실패하는 것이 정상이며, 해결은 SDK 설치다(BL-005, 로컬 개발 환경 구성 문서에 안내).
- **단언 규칙**: 테스트 코드는 AwesomeAssertions의 `Should()` 체인을 쓰고, 테스트 프로젝트의 `GlobalUsings.cs`에 `using AwesomeAssertions;`를 둔다. FluentAssertions 패키지는 어떤 버전도 참조하지 않는다(8+ 상용, 7.x 막다른 줄).
- **v3 사용 규칙**: 비동기 준비 · 정리는 `IAsyncLifetime`(`ValueTask`)으로 한다. 공유 컨테이너는 컬렉션 fixture(`ICollectionFixture<T>`)로 둔다(초기화 순서는 ADR-0022). 테스트의 취소 토큰은 `TestContext.Current.CancellationToken`을 쓴다(v3 분석기 xUnit1051 경고가 경고 = 오류 설정에서 빌드를 막는다).
- **NSubstitute 분석기**: 인터페이스가 아닌 멤버 대체 같은 오용을 빌드 경고(= 오류)로 잡기 위해 함께 참조한다.

## 결과 (Consequences)

- 긍정: 유지되는 테스트 프레임워크 줄로 시작하고(TD-009 해소), 모든 테스트 패키지가 비상용 라이선스다. FluentAssertions와 같은 단언 문법이라 문서 · 예제의 `Should()` 코드를 그대로 쓴다. 버전이 한 곳에 고정되어 에이전트가 임의 버전을 넣지 않는다.
- 부정: SDK 8.0.4xx 미만 환경에서는 빌드가 실패한다(로컬 SDK 8.0.425 설치 필요, BL-005). CI는 `setup-dotnet`이 8.0 최신 SDK를 설치하므로 영향이 없다. v3는 v2 예제와 API가 조금 달라(`ValueTask`, `TestContext`, 실행 파일 프로젝트) 인터넷 예제를 그대로 옮기면 컴파일되지 않을 수 있다.
- 부정: NetArchTest.Rules(2021-05 이후 릴리스 없음)와 NSubstitute.Analyzers.CSharp(2024-02 이후 릴리스 없음)는 유지가 멈춘 상태다. 아키텍처 테스트가 막히면 ArchUnitNET으로 바꿀지 새 ADR로 검토한다.
- 부정: `xunit.v3`는 .NET 8 지원 종료(2026-11-10) 뒤에도 net8.0 대상이 유지되는지 확인이 필요하다(.NET 업그레이드와 함께, TD-002).
- 후속: 중앙 패키지 등록 · `global.json`은 S01-T05, 첫 단위 테스트 프로젝트(BuildingBlocks.Domain.UnitTests)는 S01-T06, CI 실행은 S01-T07에서 한다. [테스트 전략](../../04-development/testing-strategy.md#도구)과 [기술 스택](../tech-stack.md#테스트-tdd)의 단언 🟡를 이 ADR로 해소한다(S01-T04).
