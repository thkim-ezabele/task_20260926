---
title: "ADR-0010: 규칙 기반 DI 자동 등록"
type: adr
adr: "0010"
status: accepted
date: 2026-09-27
deciders: []
supersedes:
superseded_by:
aliases: [ADR-0010]
tags: [adr, architecture]
created: 2026-09-27
updated: 2026-09-27
---

# ADR-0010: 규칙 기반 DI 자동 등록

## 배경 (Context)

- 서비스마다 Repository, Read Repository, 서비스(외부 연동 어댑터 등)가 계속 늘어난다.
- 초기화 코드(`Program.cs`, `DependencyInjection.cs`)에 구현 타입을 하나씩 등록하면, 등록 누락과 수명(lifetime) 불일치가 생기고 병합 충돌이 잦다.
- 에이전트가 새 타입을 만들 때 초기화 코드를 건드리지 않아도 되는 구조가 필요하다.

## 검토한 대안 (Options)

1. **수동 등록**: 명시적이지만 누락 · 수명 실수가 생기고, 초기화 코드가 계속 커진다.
2. **마커 인터페이스 / 기반 클래스 + 어셈블리 검색 자동 등록**: 규칙만 지키면 등록이 자동이다. 등록 규칙이 암묵적이 되므로 문서와 테스트로 보완해야 한다.
3. **서드파티 DI 컨테이너**(Autofac 등)의 모듈 스캔: 기능은 많지만 기본 컨테이너 외 의존이 늘어난다.

## 결정 (Decision)

**대안 2: 마커 인터페이스와 기반 클래스를 상속한 타입을 어셈블리 검색으로 찾아 `Scoped`로 자동 등록한다.**

- 마커: `IRepository`, `IReadRepository`, `IService` / 기반 클래스: `RepositoryBase<TDbContext>`, `ReadRepositoryBase<TDbContext>`
- 초기화 코드는 `AddConventionalServices(어셈블리...)`만 호출한다. 구현 타입을 개별 등록하지 않는다.
- 서비스와 Repository의 수명은 **`Scoped`**. `Singleton` / `Transient`가 필요한 인프라 요소는 BuildingBlocks 공통 등록에서만 다룬다.
- 구현 클래스는 `sealed`, 서비스 인터페이스 하나만 구현, 이름은 `I` + 클래스 이름
- Handler / Validator는 Mediator / FluentValidation의 어셈블리 검색으로 등록한다.

## 결과 (Consequences)

- 긍정: 새 타입 추가 시 초기화 코드 변경이 없다. 수명이 규칙으로 통일된다.
- 부정: 등록이 암묵적이라 디버깅 시 규칙을 알아야 한다. 인터페이스를 여러 개 구현하는 타입은 설계를 나눠야 한다.
- 후속: 등록 누락을 통합 테스트(마커 구현 타입 해석)와 `ValidateOnBuild` / `ValidateScopes`로 검증한다. 타입 검색 구현(Scrutor / 직접 구현)은 기반 구축 토픽에서 정한다. 규칙 상세는 [코딩 컨벤션 · DI 규칙](../../04-development/coding-conventions.md#의존성-주입-di-규칙).
