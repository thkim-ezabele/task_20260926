---
title: "프로젝트 개요"
type: doc
status: draft
tags: [overview]
created: 2026-09-27
updated: 2026-09-29
---

# 프로젝트 개요

> Emergency Hub 백엔드의 목적, 현재 범위, 주요 기능을 정리합니다. 실행 방법은 저장소 루트 [README](../../README.md)와 [로컬 개발 환경 구성](../01-getting-started/local-setup.md)에 있습니다.
>
> [위키 홈](../README.md)

## 프로젝트 목적

**직원 긴급연락망 서비스**: 재난이나 사고 같은 긴급 상황이 생겼을 때 직원에게 빠르게 전파하고, 직원의 응답(안부) 여부를 확인할 수 있는 백엔드 시스템을 구축합니다.

- **빠른 전파**: 긴급 상황을 등록하면 대상 직원에게 연락망 순서와 채널(SMS / 푸시 / 이메일)에 따라 알립니다.
- **응답 확인**: 직원의 안부 응답을 모아 누가 응답했고 누가 아직인지 현황을 집계합니다.
- **정확한 기준 데이터**: 전파의 바탕이 되는 직원 연락처를 대량으로 등록하고 검증된 상태로 유지합니다.

기능은 서비스 단위로 나눠 차례로 추가합니다([로드맵](roadmap.md)). 현재 릴리스는 `v0.2.0`입니다.

## 서비스 범위

### In Scope (현재 제공, `v0.2.0`)

| 영역 | 내용 | 근거 |
|---|---|---|
| Employee 서비스 | 직원 연락처(이름 · 이메일 · 전화번호 · 입사일) CSV / JSON 일괄 등록, 목록 조회(페이징), 이름 조회 | [PRD-002](../10-delivery/prd/PRD-002-employee-contacts.md), [직원 API](../05-api/employee-api.md) |
| 공통 기반 | BuildingBlocks(Domain · Application · Infrastructure · Api): Mediator 파이프라인, 검증, 트랜잭션, 오류 응답(ProblemDetails + 정수 코드), EF Core 공통 규칙 | [PRD-001](../10-delivery/prd/PRD-001-foundation.md), [Clean Architecture](../03-architecture/clean-architecture.md) |
| 로컬 실행 환경 | Aspire AppHost로 PostgreSQL · 마이그레이션 · API 기동, Aspire 대시보드로 로그 · 추적 확인 | [로컬 개발 환경 구성](../01-getting-started/local-setup.md) |
| 품질 · CI | 단위 · 통합 · 아키텍처 테스트, 커버리지 보고, GitHub Actions PR 검사 | [테스트 전략](../04-development/testing-strategy.md), [CI/CD](../06-deployment/ci-cd.md) |

### Out of Scope (현재 제공하지 않음)

| 항목 | 현재 상태 |
|---|---|
| 인증 / 권한 | 없음. Employee API는 인증 없이 열려 있다(Identity 서비스에서 추가) |
| Employee 외 서비스 | Identity, Contact Network, Emergency, Notification은 아직 없다 |
| 직원 수정 · 삭제, 조직 · 부서, 개인정보 보존 기간 | 없음(백로그) |
| 동명이인 전체 조회, CSV 헤더 행 · CP949 CSV, `/`가 들어간 이름 조회 | 없음(백로그 · 기술부채) |
| 서비스 간 통합 이벤트 | 메시지 브로커 · Outbox / Inbox 도입 보류([ADR-0023](../03-architecture/adr/0023-deferred-adoptions.md)). 도메인 이벤트는 Aggregate에 수집까지만 한다 |
| `Idempotency-Key` | 적용하지 않음 |
| API Gateway, 로그 수집기 · 추적 백엔드 | 도입 보류([ADR-0023](../03-architecture/adr/0023-deferred-adoptions.md)). 로컬 관측은 Aspire 대시보드 |
| 운영 배포 | 로컬 환경만 있다. 컨테이너 이미지 · CD · Kubernetes는 배포 단계에서 다룬다 |
| 프론트엔드 | 백엔드만 다룬다. 확인은 Swagger · curl · 통합 테스트로 한다 |

## 주요 기능

| 기능 | 설명 | 상태 |
|---|---|---|
| 직원 / 조직 관리 | 직원 정보, 부서, 연락처 관리 | 🟢 연락처 일괄 등록 · 조회 제공(`v0.2.0`), 수정 · 삭제 · 부서는 예정 |
| 연락망 구성 | 연락망 그룹, 전파 순서(Call Tree) 구성 | 🟡 예정 |
| 긴급 상황 전파 | 긴급 상황 등록 및 대상자 일괄 전파 | 🟡 예정 |
| 알림 발송 | SMS / 푸시 / 이메일 등 채널별 발송 | 🟡 예정 |
| 응답 확인 | 직원 응답(안부) 수집 및 현황 집계 | 🟡 예정 |
| 인증 / 권한 | 사용자 인증, 역할 기반 권한 | 🟡 예정 |

서비스 구성 후보는 [MSA 서비스 카탈로그](../03-architecture/service-catalog.md)에 있습니다.

## 이해관계자 및 담당자

> 🟡 아직 정의하지 않았습니다.

## 참고 자료

- [README](../../README.md): 프로젝트 소개, 실행, API 요약
- [로드맵](roadmap.md): 단계별 진행 상태
- [아키텍처 결정 기록(ADR)](../03-architecture/adr/README.md): 기술 선택과 설계 결정
- [기술 스택](../03-architecture/tech-stack.md): 확정 · 보류 · 이후 항목
- [개발 관리](../10-delivery/README.md): 요구사항(PRD) · 스프린트 · 회고 기록

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-27 | - | 범위(In / Out of Scope)와 이해관계자는 PRD-001 인터뷰에서 받기로 표시 |
| 2026-09-29 | - | 목적 상세, 현재 범위(In / Out of Scope, `v0.2.0` 기준), 주요 기능 상태, 참고 자료 작성 |
