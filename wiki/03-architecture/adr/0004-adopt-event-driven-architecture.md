---
title: "ADR-0004: 이벤트 기반 아키텍처(EDA) 채택"
type: adr
adr: "0004"
status: accepted
date: 2026-09-27
deciders: []
aliases: [ADR-0004]
tags: [adr, architecture]
created: 2026-09-27
updated: 2026-09-27
---

# ADR-0004: 이벤트 기반 아키텍처(EDA) 채택

## 배경 (Context)

> TODO: 결정이 필요했던 배경

## 검토한 대안 (Options)

> TODO: 검토한 대안과 장단점

## 결정 (Decision)

서비스 간 상태 변경 전파는 **통합 이벤트(Integration Event)** 기반의 비동기 메시징으로 처리합니다.

> TODO: 선정 사유 상세

## 결과 (Consequences)

- 메시지 브로커를 선정해야 합니다. (🟡 검토 중)
- 이벤트 유실을 막기 위해 Outbox 패턴을 적용하고, 멱등성 처리를 검토합니다.
- 이벤트 명세는 [이벤트 카탈로그](../../05-api/event-catalog.md)에서 관리합니다.
