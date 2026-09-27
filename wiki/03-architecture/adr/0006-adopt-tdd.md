---
title: "ADR-0006: TDD 개발 방법론 채택"
type: adr
adr: "0006"
status: accepted
date: 2026-09-27
deciders: []
aliases: [ADR-0006]
tags: [adr, architecture]
created: 2026-09-27
updated: 2026-09-27
---

# ADR-0006: TDD 개발 방법론 채택

## 배경 (Context)

> TODO: 결정이 필요했던 배경

## 검토한 대안 (Options)

> TODO: 검토한 대안과 장단점

## 결정 (Decision)

기능 개발은 **TDD (Red → Green → Refactor)** 사이클을 따릅니다.

> TODO: 선정 사유 상세

## 결과 (Consequences)

- 도메인 / 애플리케이션 로직은 테스트를 먼저 작성합니다.
- PR은 테스트 통과를 필수 조건으로 합니다.
- 상세 규칙은 [TDD 가이드](../../04-development/tdd-guide.md)를 참고합니다.
