---
title: "ADR-0001: .NET 8 채택"
type: adr
adr: "0001"
status: accepted
date: 2026-09-27
deciders: []
aliases: [ADR-0001]
tags: [adr, architecture]
created: 2026-09-27
updated: 2026-09-27
---

# ADR-0001: .NET 8 채택

## 배경 (Context)

- 직원 긴급연락망 백엔드를 여러 서비스(MSA)로 나눠 개발해야 하므로, 서비스 개발 · 테스트 · 컨테이너 배포를 한 가지 스택으로 일관되게 지원하는 런타임이 필요하다.
- 긴급 상황에는 짧은 시간에 많은 전파 · 응답 요청이 몰리므로, 비동기 I/O 처리 성능이 좋아야 한다.
- 과제형 프로젝트라 설계 의도(DDD, Clean Architecture, CQRS)를 코드로 분명히 드러낼 수 있는 정적 타입 언어가 유리하다.
- 1인 개발에 AI 에이전트가 코드를 작성 · 리뷰하므로, 컴파일러 · 분석기가 규칙 위반을 빌드 단계에서 잡아 주는 환경이 필요하다.

## 검토한 대안 (Options)

1. **.NET 8 (C#)**: 장점: LTS, ASP.NET Core의 높은 비동기 처리 성능, EF Core · 분석기 · Nullable 등 정적 검증 도구, 컨테이너 친화적. 단점: LTS 지원 종료(2026-11-10)가 가까워 업그레이드 계획이 필요하다.
2. **Java 21 / Spring Boot**: 장점: MSA 생태계(Spring Cloud)가 풍부하고 자료가 많다. 단점: 설정과 보일러플레이트가 많고, 이 프로젝트에서 목표로 하는 C# 기반 역량 증빙과 맞지 않는다.
3. **Node.js / NestJS (TypeScript)**: 장점: 개발 속도가 빠르고 I/O 처리에 강하다. 단점: 런타임 타입 안전성이 약하고, 도메인 모델 중심 설계를 강제하기 어렵다.
4. **Go**: 장점: 가볍고 빠르며 배포가 단순하다. 단점: ORM · DDD 관련 생태계가 얇아 도메인 모델 표현에 손이 많이 간다.

## 결정 (Decision)

백엔드 개발 언어와 런타임으로 **.NET 8 (C#)** 을 사용합니다.

선정 사유:

- ASP.NET Core의 비동기 처리 성능이 긴급 전파 시 순간 부하에 적합하다.
- C#의 record, 패턴 매칭, Nullable 참조 형식이 DDD 모델(Value Object, 강타입 ID)과 CQRS 모델(Command / Query record)을 간결하게 표현한다.
- `TreatWarningsAsErrors`, 분석기, `.editorconfig`로 컨벤션을 빌드에서 강제할 수 있어 에이전트가 만든 코드의 품질 편차를 줄인다.
- EF Core(Npgsql), Testcontainers, OpenTelemetry 등 필요한 도구가 모두 성숙해 있다.

## 결과 (Consequences)

- 모든 서비스 프로젝트의 Target Framework는 `net8.0`으로 합니다.
- 개발 환경에 .NET 8 SDK를 설치해야 합니다. ([사전 준비 사항](../../01-getting-started/prerequisites.md))
- 긍정: 서비스 전체가 같은 언어 · 빌드 설정 · 테스트 도구를 공유한다.
- 부정: .NET 8 LTS 지원은 **2026-11-10에 종료**된다.
- 후속: .NET 10(LTS)으로의 업그레이드를 검토하고, 결정하면 이 ADR을 대체하는 새 ADR을 작성한다.

> 2026-09-27: 작성 당시 비워 둔 배경 · 대안 · 선정 사유를 보완했습니다(결정 변경 없음).
