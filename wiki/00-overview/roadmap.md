---
title: "로드맵"
type: doc
status: draft
tags: [overview]
created: 2026-09-27
updated: 2026-09-28
---

# 로드맵

> 마일스톤과 단계별 목표를 정리합니다. Phase는 토픽(PRD) 단위로 진행하며, 토픽 하나가 끝날 때마다 릴리스 `v0.N.0`을 하나씩 냅니다([개발 관리](../10-delivery/README.md)).
>
> [위키 홈](../README.md)

> 🟡 Phase 2 ~ 4의 토픽은 **후보**입니다. 실제 토픽과 순서는 사용자가 주는 PRD로 정하며, PRD를 받을 때마다 이 문서를 갱신합니다. 일정(날짜)은 정하지 않았습니다(스프린트는 범위 고정).

## Phase 1 - 프로젝트 초기 구축 (위키 / 솔루션 / 공통 기반)

| 구분 | 내용 | 상태 |
|---|---|---|
| 위키 · 개발 흐름 | 위키 구조, ADR 0001 ~ 0010, Git Flow, 개발 흐름(토픽 → 스프린트 → 회고), 에이전트 · 스킬, 기준 문서 기본판 | 🟢 완료 |
| **[PRD-001 기반 구축](../10-delivery/prd/PRD-001-foundation.md)** | 기술 결정 ADR, Aspire 9.x 로컬 인프라(AppHost + PostgreSQL + MigrationService), 솔루션 구조([Clean Architecture](../03-architecture/clean-architecture.md)), BuildingBlocks(직접 구현 Mediator 파이프라인, `AddConventionalServices`, EF 공통 규칙), Employee 샘플, 빌드 설정, 단위 · 통합 · 아키텍처 테스트, CI(GitHub Actions) | 🔵 진행 중 |

PRD-001에서 정한 방향 (S01에서 ADR로 확정 예정, [PRD-001 질문과 답변](../10-delivery/prd/PRD-001-foundation.md#질문과-답변)):

- Mediator: 직접 구현 (로깅 → 검증 → 트랜잭션 데코레이터 파이프라인)
- API 스타일: Controller
- DI 타입 검색: Scrutor
- UUID v7: `IIdGenerator` + UUIDNext
- 단언 라이브러리: AwesomeAssertions
- 로컬 인프라: .NET Aspire 9.x (docker compose 미사용)
- 도입 보류: 메시지 브로커, Outbox / Inbox, API Gateway, 로그 수집기

## Phase 2 - 핵심 도메인 서비스 개발

| 구분 | 내용 | 상태 |
|---|---|---|
| **[PRD-002 직원 연락처 조회 · 일괄 등록](../10-delivery/prd/PRD-002-employee-contacts.md)** | Employee 연락정보 모델 재설계(스키마 리셋), CSV / JSON 일괄 등록 `POST /api/employee`, 목록 · 이름 조회 `GET /api/employee` · `GET /api/employee/{name}` | 🔵 진행 중 |

🟡 후보 토픽 (PRD로 확정):

| 후보 토픽 | 내용 |
|---|---|
| 인증 / 직원 · 조직 관리 | Identity, Employee 서비스: 사용자 인증, 역할 · 권한(비트 마스킹), 직원 · 부서 · 연락처 관리 |
| 연락망 구성 | Contact Network 서비스: 연락망 그룹, 전파 순서(Call Tree) |

## Phase 3 - 서비스 연동 및 이벤트 흐름 구성

🟡 후보 토픽 (PRD로 확정):

| 후보 토픽 | 내용 |
|---|---|
| 긴급 상황 전파 · 알림 발송 | Emergency, Notification 서비스: 긴급 상황 등록, 대상자 전파, 채널별(SMS / 푸시 / 이메일) 발송, 통합 이벤트 · Outbox |
| 응답 수집 · 집계 | 직원 응답(안부) 수집, 현황 집계와 조회 |

## Phase 4 - 안정화 및 배포

🟡 후보 토픽 (PRD로 확정):

| 후보 토픽 | 내용 |
|---|---|
| 배포 · 운영 | 컨테이너 구성, 환경별 설정, 관측성(로그 수집기, 추적, 메트릭), 헬스체크, 운영 런북 |

## 일정표

토픽을 시작하거나 끝낼 때(`/prd`, `/sprint`, `/retro`) 행을 추가 · 갱신합니다.

| Phase | 스프린트 | PRD | 릴리스 | 상태 |
|---|---|---|---|---|
| 1 | [S01](../10-delivery/sprints/S01-decisions-build-ci.md) ~ [S04](../10-delivery/sprints/S04-tests-docs-evidence.md) | [PRD-001](../10-delivery/prd/PRD-001-foundation.md) 기반 구축 | `v0.1.0` (예정) | 🔵 진행 중 |
| 2 | [S05](../10-delivery/sprints/S05-rebase-decisions-schema.md) ~ [S07](../10-delivery/sprints/S07-query-api-docs.md) | [PRD-002](../10-delivery/prd/PRD-002-employee-contacts.md) 직원 연락처 조회 · 일괄 등록 | `v0.2.0` (예정) | 🔵 진행 중 |

스프린트 상세는 [개발 관리](../10-delivery/README.md)를 참고합니다.

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-27 | - | 일정표에 Phase ↔ 스프린트 연결 표 추가 |
| 2026-09-27 | - | Phase별 토픽 작성: Phase 1 = PRD-001 기반 구축(미정 ADR 항목), Phase 2 ~ 4는 후보 토픽 |
| 2026-09-27 | - | PRD-001 토픽 시작(S01~S04), 결정 방향 반영 |
| 2026-09-28 | developer | PRD-002 토픽(S05~S07) 행 추가 (S05-T01) |
