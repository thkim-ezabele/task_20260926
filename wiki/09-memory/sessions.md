---
title: "세션 이력"
type: memory
tags: [memory]
created: 2026-09-27
updated: 2026-09-27
---

# 세션 이력

> [장기기억](README.md) · 원본: [Worklog 목록](../08-worklog/README.md)

## 현재 상태

- **진행 중 토픽: [PRD-001 기반 구축](../10-delivery/prd/PRD-001-foundation.md)**. 브랜치 `feature/prd-001-foundation`, [Draft PR #7](https://github.com/thkim-ezabele/task_20260926/pull/7). **S01 · S02 done**(태그 `sprint/S01` · `sprint/S02`, S02 PR CI 통과 약 2분 10초), S03 · S04 `planned`.
- ADR 0001~0024 모두 승인(S02에서 0024: BuildingBlocks.Api). 기준 문서 `draft` 유지. 남은 `todo` 문서는 S04(local-setup 등 5개)와 이후 토픽.
- 코드: BuildingBlocks Domain · Application(디스패처 · 데코레이터) · Infrastructure(Scrutor DI, UUID v7, EF 공통 규칙, UoW, 23505 변환) · Api(ProblemDetails, 전역 예외 처리), ArchitectureTests. 테스트 1033 통과 + 11 건너뜀(서비스 전용 규칙, S03에서 활성화). 로컬 SDK 8.0.425, Docker 사용 가능.
- 백로그 / 기술부채: S02 종료 때 정리(`new` 0). S03 편입: BL-023 · 065 · 073 · 075(상, Serilog 예외 로그 노출) · 081~086, TD-020. S04 편입: BL-055 · 066 · 068 · 069 · 087. 상 우선순위 open: BL-002(.NET 10 전환), BL-024.
- `/sprint` 2회 운영. S02 회고 반영(dba 해당 없음 생략, reviewer 커밋 병합, 테스트 범위, 완료 조건 5~7문장, 임의 이관 금지)을 스킬 · 에이전트에 적용. `/retro`는 미실행.
- git: 토픽 브랜치 원격은 5b63ea4까지. 이 세션 worklog만 push 전.

## 다음 할 일

- [ ] `/sprint S03` 실행. 계획 리뷰에서 먼저: 아키텍처 테스트 Employee 편입 시점(MigrationService가 S03-T04에 생겨 T03 편입 시 규칙 실패, BL-085), BL-081~084를 S03-T05 · T02 완료 조건에 반영(S03-T05 분할 여부), BL-075를 S03-T04 완료 조건에, BL-065 IService 마커 결정
- [ ] ADR 후보 작성 여부 결정: S02 4건(ADR-0018 보충 추천, IStronglyTypedId · IHasDomainEvents 계약, 예외 로그 비식별화, 아키텍처 테스트 규칙 범위) + S01 6건(액션 SHA 고정, 공급망 정책, SDK 고정, 테스트 프로젝트 이름 규칙, ErrorType · Result 계약, 경고 억제 규칙)
- [ ] BL-018(raw-log frontmatter) 토픽 밖 작업으로 해결해 check-docs 결함 0 기준선 확보
- [ ] .NET 10 · Aspire 13 전환(BL-002 · TD-002) 토픽 여부 결정(.NET 8 지원 종료 2026-11-10)
- [ ] 프로젝트 범위(In / Out of Scope)와 이해관계자 정의
- [ ] 커밋 작성자 이메일 확인 (현재 전역 설정 `taehoon365@gmail.com`, S01 · S02 커밋은 이 값으로 push됨)

## 이력

| 세션 | 날짜 | 요약 |
|---|---|---|
| [WL-2026-09-27-01](../08-worklog/2026-09/2026-09-27-01-wiki-structure.md) | 2026-09-27 | 위키 기본 구조와 ADR 0001~0006 작성 (드라이브 고장으로 사후 복원) |
| [WL-2026-09-27-02](../08-worklog/2026-09/2026-09-27-02-worklog-convention.md) | 2026-09-27 | worklog 체계, 프롬프트 hook, GitHub 저장소 구성 → 개발 흐름 방향 확정, 장기기억(09-memory), Obsidian 재구성 |
| [WL-2026-09-27-03](../08-worklog/2026-09/2026-09-27-03-dev-workflow-and-agents.md) | 2026-09-27 | Git Flow 구성, 개발 흐름(토픽 → 스프린트 → 회고) 설계, 기준 문서 기본판, ADR 0007~0010, 에이전트 5개 · 스킬 3개 구현, 위키 미구축 항목 정비 |
| [WL-2026-09-27-04](../08-worklog/2026-09/2026-09-27-04-prd-001-foundation.md) | 2026-09-27 | `/prd` 첫 운영: PRD-001 기반 구축(FR 11 / NFR 7), 기술 방향 결정, 병렬 리뷰 반영, S01~S04 분할, 토픽 브랜치 · Draft PR #7 |
| [WL-2026-09-27-05](../08-worklog/2026-09/2026-09-27-05-sprint-s01.md) | 2026-09-27 | `/sprint` 첫 운영: S01 완료(ADR 0011~0023, 빌드 설정, BuildingBlocks.Domain, CI), 반려 1 · BLOCKED 1, BL · TD 정리, 태그 `sprint/S01`, 회고 반영 스킬 개선 |
| [WL-2026-09-27-06](../08-worklog/2026-09/2026-09-27-06-sprint-s02.md) | 2026-09-27 | `/sprint S02`: BuildingBlocks Application · Infrastructure · Api와 아키텍처 테스트(테스트 202 → 1033), ADR-0024, 반려 3 · BLOCKED 0, 운영 변경(dba 생략 · reviewer 커밋 병합), BL · TD 정리, 태그 `sprint/S02`, 회고 반영 스킬 개선 |
