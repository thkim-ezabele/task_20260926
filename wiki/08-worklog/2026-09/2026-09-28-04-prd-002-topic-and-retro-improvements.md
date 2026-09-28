---
title: "WL-2026-09-28-04: PRD-002 토픽 생성과 회고 개선안 반영"
type: worklog
date: 2026-09-28
session: "9cb573be"
model: "Claude Code · Opus 5.5"
sprint: 
adrs: []
aliases: [WL-2026-09-28-04]
tags: [worklog]
created: 2026-09-28
updated: 2026-09-28
---

# WL-2026-09-28-04: PRD-002 토픽 생성과 회고 개선안 반영

- 원문 로그: [raw/2026-09-27](../raw/2026-09-27.md) (15:45~16:57), [raw/2026-09-28](../raw/2026-09-28.md) (13:09~) · session `9cb573be`
- 관련: [PRD-002](../../10-delivery/prd/PRD-002-employee-contacts.md), 스프린트 [S05](../../10-delivery/sprints/S05-rebase-decisions-schema.md) · [S06](../../10-delivery/sprints/S06-bulk-register-api.md) · [S07](../../10-delivery/sprints/S07-query-api-docs.md)(planned), PR #8(Draft) · #13(병합), [RETRO-PRD-001](../../10-delivery/retros/RETRO-PRD-001.md#개선안)

## 목표

> PRD-001 스프린트가 다른 세션에서 진행되는 동안, 기반 구축 뒤에 개발할 실제 요구사항(직원 연락처 조회 · CSV / JSON 일괄 등록)을 충돌 없이 `/prd` 토픽으로 정리한다. PRD-001 완료 뒤에는 토픽 브랜치를 최신 `develop`에 맞추고, 회고 개선안을 반영해 `/sprint S05` 직전 상태까지 준비한다.

## 주요 프롬프트

1. `/prd` — 직원 긴급연락망의 필수 요구사항: 기본 연락정보 조회, CSV / JSON(파일 업로드 · 직접 입력) 등록, 필수 API 3개(`GET /api/employee?page&pageSize`, `GET /api/employee/{name}`, `POST /api/employee` 201). 동명이인은 백로그 · v2 · 별도 엔드포인트 중 선택. "병렬로 다른 세션 동작중이니 충돌 여부 확인하고 진행"
2. "병렬리뷰 진행" → 차단 질문 B1~B4 답변 → "승인할께"
3. "그럼 다른 세션작업 모두 끝나면 지금 현재 세션에서 다시 작업재게 하면 되지?"
4. "지금 prd-001 항목이 완료되엇어 다음 진행할수 잇는 상태인지 확인만 해줘"
5. "병합하고 스프린트는 진행하지말고 스프린트 진행 전단계까지만 진행시켜"
6. ".net10 전환은 진행하지 않을거고 bl-024 는 뭔지 설명해주고" → "BL-024는 편입해줘 3번에서도 develop 병합까지만 진행"
7. 회고 개선안 17건 "반영" → "나머지 작업 끝나면 보고해줘"

## 작업 내용

**PRD-002 토픽 생성 (2026-09-27, `/prd`)**
- 충돌 점검: 다른 세션이 같은 폴더에서 `feature/prd-001-foundation`을 수정 중 → 브랜치 전환 금지, 별도 git worktree(`emergency-hub-prd-002`, `develop` 기준)에서 작성. "토픽 한 번에 하나" 규칙 예외, 번호 자동 계산 불가(수동 PRD-002 · S05~ 가번호), 공유 문서 병합 충돌, ADR-0016 경로 규칙과 요구 경로 차이를 보고.
- 인터뷰 2회(8문항): 문서 전용 브랜치 지금 생성, 요구 경로 그대로(ADR로 예외 기록), 이름 조회 단건 + 동명이인 백로그, 입사일 빠른 순, 일괄 등록 전부 거부, multipart + raw body, CSV 헤더 없음(헤더 지원은 BL · TD), 이메일 유일 + 전화 형식 검사.
- 병렬 리뷰(orchestrator ∥ dba ∥ developer) → 통합(차단 질문 4건 B1~B4, 모두 추천안 채택) → 스프린트 분할(S05~S07, 작업 15개, unassigned 0).
- 승인 후 `feature/prd-002-employee-contacts` 생성, PRD · 스프린트 문서 3개 커밋 · push, Draft PR #8(본문에 "문서 전용, PRD-001 병합 전 스프린트 금지"). README 목록 · roadmap · backlog는 S05-T01로 연기.

**PRD-001 완료 뒤 준비 (2026-09-28)**
- 상태 점검: PR #7 · #10 · #11 병합, `v0.1.0`, 다른 세션 종료, 토픽 브랜치는 `develop`보다 173커밋 뒤. Employee 샘플은 PRD-002 모델로 바뀌지 않음 → S05-T03 · T04 필요.
- worktree 제거, 메인 폴더에서 토픽 브랜치로 전환, `develop` 병합(`1f671da`). raw 로그가 checkout을 막아 stash → 병합 → 복원.
- 점검: Release 빌드 경고 0 · 오류 0, 테스트 1,537 통과 · 1 건너뜀(`DOCKER_API_VERSION=1.43`), check-docs 결함 4(기존 BL-018).
- BL-024 편입(`211064e`): PRD NFR-04 · FR-10 · Q17, S06-T06 자동 테스트(`ActivityListener`), S07-T04 대시보드 수동 확인. .NET 10 전환 미진행 기록. push → PR #8 첫 CI 통과(4m1s).
- 회고 개선안 17건(#1~#17) 반영: `develop`에서 `feature/retro-prd-001-improvements`, 파일 단위로 나눈 병렬 작업 4개(에이전트 정의 · 스킬 · agents.md · 기준 문서), 연결 보정(`dba.md` 앵커, 새 필드 `kind` · `evidence_frame` · `apply_before_next`를 prd · sprint 스킬에 연결), 커밋 4개, PR #13 CI 통과(4m14s) → Merge commit `210b128`.
- 토픽 브랜치에 `develop` 재병합(`7160967`) · push, PR #8 CI 통과(4m4s). `/sprint S05`는 시작하지 않음.

### 변경 파일

| 파일 | 변경 | 설명 |
|---|---|---|
| `wiki/10-delivery/prd/PRD-002-employee-contacts.md` | 추가 · 수정 | PRD-002 (FR 11, NFR 6, Q1~Q17, 병행 진행 메모, 입력 경로 표), BL-024 편입 |
| `wiki/10-delivery/sprints/S05-rebase-decisions-schema.md` · `S06-bulk-register-api.md` · `S07-query-api-docs.md` | 추가 · 수정 | 스프린트 계획(작업 5 / 6 / 4), BL-024 편입 반영 |
| `wiki/10-delivery/agents.md` | 수정 | 문서 점검표 D1~D5, 반려 분류, 인계 메모, ADR 전제와 기준 문서, 빈 곳 없음 대조표 (PR #13) |
| `.claude/agents/*.md` (5개) | 수정 | 개선안 #1 · 2 · 3 · 10 · 12 · 13 · 14 (PR #13) |
| `.claude/skills/sprint` · `retro` · `prd` `SKILL.md`, `wiki/_templates/sprint.md` | 수정 | 개선안 #5 · 7 · 8 · 9 · 11, 새 필드 연결, 대리 승인 절 (PR #13) |
| `wiki/04-development/database.md` · `coding-conventions.md` · `testing-strategy.md`, `wiki/01-getting-started/troubleshooting.md` | 수정 | 개선안 #3 · 6 · 13 · 14 · 15 · 16 (PR #13) |

커밋: 토픽 `f379a76` · `ed169e1` · `1f671da`(병합) · `211064e` · `7160967`(병합), 회고 반영 `7f1cb77` · `10a3530` · `2221466` · `128edb7` → `210b128`(#13).

## 결정 사항

| 결정 | 이유 | ADR |
|---|---|---|
| PRD-002는 별도 worktree에서 작성하고 문서 전용 토픽 브랜치 · Draft PR을 먼저 만든다(병행 토픽 예외, 새 파일만 커밋) | 다른 세션이 같은 작업 트리에서 PRD-001 진행 중 | - |
| 과제 필수 경로 `/api/employee` · `page` / `pageSize` · `{name}` 그대로 | 과제 필수 명세 | 후보(S05-T02 ①) |
| 이름 조회 단건(입사일 빠른 순), 동명이인 전체 조회는 백로그 | 사용자 결정 | - |
| 일괄 등록 전부 거부, 요청 안 중복 400 · DB 중복 409 + 행 번호, 경합 23505 409는 행 번호 없음 | B3 | 후보(④ 오류 계약 확장) |
| 입력은 Employee.Api 전용 바인더, 파서 Application, 필드 규칙 Domain Value Object, 413 / 415는 ErrorType + 공통 코드 | B4 | 후보(② 입력 처리, ④) |
| 샘플 API 제거, `employee_status` 유지, `InitialCreate` 리셋 전용 작업 | B2 | - |
| 이메일은 원본 + 정규화 컬럼 유니크 | dba 권장 | 후보(③) |
| BL-024 편입, .NET 10 전환(BL-002) 미진행 | 사용자 결정(2026-09-28) | - |
| 회고 개선안 판단 5개(대조표 없으면 불인정, ADR 불일치는 대체 후보, 미추인 대리 승인이 릴리스를 막지 않음, BL-117 잡음 제외, 재현 데이터 손실 여부 계획 리뷰에서) 유지 | 사용자가 그대로 병합 지시 | - |

## 이슈 / 배운 점

- 같은 폴더에서 두 세션이 동시에 일할 때 `git checkout`은 다른 세션 작업을 망가뜨린다. worktree 분리가 안전했다. 다만 worktree에는 옛 `develop`의 `09-memory` · `CLAUDE.md`가 있어 병합 뒤 메인 폴더로 돌아왔다.
- hook이 쓰는 raw 로그가 브랜치 전환을 막는다(대상 브랜치에 없는 파일). stash → 전환 → 병합 → 복원으로 처리했고, `stash pop`은 modify/delete 충돌이 나서 백업 파일로 복원했다.
- `git merge -F -`(표준 입력)는 동작하지 않는다. 메시지는 파일로 넘긴다.
- PR 병합 때 GitHub가 원격 브랜치를 지워 `git push --delete`가 실패했다(영향 없음).
- 병렬 작업을 파일 단위로 나누고 공통 이름(D1~D5, 반려 분류, a~g)을 프롬프트에 미리 고정해 서로 어긋나지 않았다. 새 반환 필드와 스킬 연결은 따로 보정이 필요했다.

## 다음 할 일

- [ ] `/sprint S05` 실행(토픽 브랜치 `feature/prd-002-employee-contacts`). S05-T01 재기준화에서: 스프린트 번호 확정, README PRD · 스프린트 목록 · roadmap 행 추가, 범위 밖 항목 BL / TD 등록(다음 번호 BL-121 · TD-028), BL-024 `planned:S06`, TD-010 편입 여부 확인, ADR 번호(0025~) · 에러 코드(Employee 21001~21006 · 22001 · 23001 사용 중) 예약
- [ ] S05-T02: ADR 4건(API 규칙 예외, 일괄 가져오기 입력 처리, 이메일 대소문자 무시 유일, BuildingBlocks 오류 계약 확장) 사용자 확인
- [ ] RETRO-PRD-001 사용자 결정 남은 항목(대리 승인 추인, ADR 후보 (a)~(j), `disable-model-invocation` 유지 여부, BL-113 · 065, BL-117, 이메일, 익명 볼륨) 확인. .NET 10 전환은 진행 안 함으로 확정
- [ ] BL-018(raw 로그 frontmatter) 해결로 check-docs 결함 0 기준선 확보
