---
title: "RETRO-PRD-002: 직원 연락처 조회 · 일괄 등록 회고"
type: retro
prd: PRD-002
sprints: [S05, S06, S07]
release: "v0.2.0"
date: 2026-09-29
aliases: [RETRO-PRD-002]
tags: [delivery, retro]
created: 2026-09-29
updated: 2026-09-29
---

# RETRO-PRD-002: 직원 연락처 조회 · 일괄 등록 회고

- PRD: [PRD-002](../prd/PRD-002-employee-contacts.md)
- 스프린트: [S05](../sprints/S05-rebase-decisions-schema.md), [S06](../sprints/S06-bulk-register-api.md), [S07](../sprints/S07-query-api-docs.md)
- 토픽 PR: [#8](https://github.com/thkim-ezabele/task_20260926/pull/8) · 릴리스: `v0.2.0`

> 오케스트레이션 세션(`emergency-hub-d2`)이 사용자 지시("모든 판단은 너가 직접")에 따라 개선안 처리까지 대리로 정했습니다. 사용자만 할 수 있는 추인은 [사용자 결정 필요](#사용자-결정-필요)에 모았습니다.

## 요약

> PRD-002를 3개 스프린트(2026-09-28~29, 작업 17, 토픽 커밋 81)로 끝냈습니다. FR 11 · NFR 6은 모두 충족입니다. 테스트는 1,538건에서 2,544건(건너뜀 1)으로 늘었고, 커버리지는 라인 99.3% · 분기 96.8%, CI는 4분 59초입니다. 성능은 CI 기준 NFR-02가 281.4ms(단언 4초), NFR-03이 3.7 · 7.6 · 1.9ms(단언 400ms)입니다.
> 반려는 판정 6건(작업 기준 4회차)이고 모두 재작업 1회로 끝났습니다. 설계 · 코드 버그가 원인인 반려는 0건입니다. 여러 작업에 걸쳐 반복된 문제는 도구 인자 이스케이프 손상입니다(U+FFFD · U+FEFF · U+200D, 역슬래시 누락 추정).
> S05 뒤 속도 튜닝으로 작업당 소요가 약 65분 → 40분 → 26분으로 줄었고, 품질이 떨어졌다는 근거는 없습니다. 승인된 ADR과 구현이 다른 곳은 3건으로 늘었습니다(PRD-001 (a) · (b)와 ADR-0026 1절).

## FR 충족 표

| 요구사항 | 작업 | 커밋 / PR | 테스트 | 결과 |
|---|---|---|---|---|
| FR-01 | S05-T03 · T04 · T06, S06-T04 | - | VO 테스트, DB RoundTrip, BL-129 email 규칙 | 충족 |
| FR-02 | S05-T04 · T05 | 커밋 R 5ee04a4 | ModelMetadata, 볼륨 삭제 뒤 AppHost 실행 기록 | 충족 |
| FR-03 | S06-T02 | - | CsvImportParserTests 39 · ImportTextDecoderTests 12 | 충족 (FR Trait 없음, 개선안 12) |
| FR-04 | S06-T03 | - | JsonImportParserTests 49 | 충족 (FR Trait 없음, 개선안 12) |
| FR-05 | S06-T05 · T06 | - | InputPath · ExampleFile · Binder | 충족 (ADR-0026 1절과 다름, ADR 후보 1) |
| FR-06 | S05-T06, S06-T04 · T06, S07-T05 | a2f76ea(0행 21028) | Concurrency · UoWConflict · Handler | 충족 |
| FR-07 | S05-T06, S07-T01 · T03 | - | ListEmployeesPaging · Http · ReadRepositoryDatabase | 충족 |
| FR-08 | S05-T06, S07-T02 · T03 | - | GetEmployeeByNameLookup · EncodedFormsHttp · QueryPlan | 충족 |
| FR-09 | S05-T02, S06-T01 · T05 | bd0cbc7(ADR 0025~0028) | RegisterPipeline · StatusCodePages · UnsupportedMediaType | 충족 (ADR 대리 확인, 추인 대기) |
| FR-10 | S05~S07 코드 작업 | CI 36490909577 | 2,544 통과, PersonalData · KestrelLimit | 충족 |
| FR-11 | S05-T02 · T05, S07-T04 | evidence/S07-T04 | curl 28개 + local-setup 두 셸 | 충족 |
| NFR-01~06 | S05-T02, S06-T05 · T06, S07-T02~T04 | CI 36490909577 | KestrelLimit, Performance, QueryPerformance, ActivityCollector · RouteTemplateTracing, VO, PendingTargetRule · 아키텍처 | 충족 |

## 계획 대비 실제

| 항목 | 계획 | 실제 | 비고 |
|---|---|---|---|
| 스프린트 수 | 3 | 3 | |
| 작업 수 | 15 | 17 | S05 5→6(계획 리뷰 분할), S07 +T05(BL-137 사용자 결정). 이관 0 · BLOCKED 0 |
| 테스트 | - | 1,538 → 1,638 → 2,277 → 2,544 | 건너뜀 4 → 1(대상 대기 규칙 해제) |
| 토픽 커밋 | - | 81 | Stage: developer 22 · orchestrator 19 · reviewer, tester 14 · dba 7 · tester 6 · reviewer 1 |
| 소요 | - | S05 약 6.5시간 · S06 약 4시간 · S07 작업 2시간 11분 | S07 시작 지시(00:15경)와 계획 확정(05:00) 사이의 공백은 원인 미확인 |

## 파이프라인 분석

| 단계 | 반려 횟수 | 주요 원인 (컨벤션 / 누락 / 설계 / 버그) |
|---|---|---|
| dba → | 0 | - |
| developer → | 0 | - |
| reviewer → | 4 | 누락(문서 사실) 1: S05-T05 리셋 뒤 옛 식별자 잔존. 컨벤션 2: S06-T01 · T03 null-forgiving. 문서 사실 2: S06-T03 U+FFFD, S07-T04 경로 역슬래시 |
| tester → | 2 | 문서 사실 2: S06-T03 U+FFFD, S07-T04 경로 · 마스킹 문장(둘 다 reviewer와 같은 회차) |

작업 기준 반려는 S05 1 · S06 2 · S07 1회입니다. 코드 작업 반려는 null-forgiving 한 가지 유형뿐이고, 이 누락은 튜닝 전부터 있었습니다(점검 grep이 `!`를 보지 않음).

### 속도 튜닝 전후 (ef9eb86, S06부터)

| 항목 | S05 (전) | S06 (후) | S07 (후) |
|---|---|---|---|
| 작업당 소요 | 약 65분(계획 리뷰 70분 포함) | 약 40분 | 약 26분 |
| 스프린트 문서 | 136KB | 37KB | 29KB |
| 반려 회차 / 판정 | 1 / 1 | 2 / 3 | 1 / 2 |
| reviewer가 놓친 코드 결함 | - | 0 | 0 (U+200D는 tester가 복구) |

판정: **유지**(오케스트레이션 대리 결정). 병렬 검증에서 같은 결함이 두 번 보고된 일(S07-T04)은 비용이 작았고, tester만 찾은 결함(마스킹 문장)이 있어 둘이 서로를 보완했습니다. 대가는 스프린트 회고를 폐지하면서 S06 교훈이 S07 규칙으로 들어가지 못한 점입니다. 임시 grep으로 넘긴 선례를 정식 규칙으로 만듭니다(개선안 8). CI · 태그 일괄 판정은 첫 실행에서 통과했습니다.

## 백로그 / 기술부채 추이

| 구분 | 생김 | 해결 | 남음 | 다음 토픽 이관 |
|---|---|---|---|---|
| 백로그 | 23 (BL-121~143) + 회고 3 (BL-144~146) | 9 (done 8 · dropped 1) + 기존 BL-024 done | 17 | BL-121~127 · 131 · 136 · 138 · 140~146 (BL-145 반영 시 131 · 136 · 140 · 142 · 143 닫힘) |
| 기술부채 | 3 (TD-028~030) | 0 (TD-021 부분 상환) | 3 | TD-028 · 029 · 030, TD-010 |

## 관점별 회고

| 관점 | 잘된 점 | 문제 |
|---|---|---|
| orchestrator | 재설계 · 리셋 분리로 리셋 코드 반려 0. BL-137을 사용자 결정으로 올리고 작은 작업으로 분리. BL · TD new 0 | 인계 메모 제외 기준(BL-117)이 기준 문서와 어긋남. 회고를 폐지하면서 S06 교훈 반영이 지연됨. ADR 후보 누적 |
| dba | 명세 먼저 · 실측 뒤 확정. ADR-0012 리셋 첫 운영. handoff 재사용으로 S06 dba 0회. 인덱스로 NFR-03 충족 | database.md 밖 옛 식별자 잔존. VO 복합 인덱스 불가를 늦게 발견. --output-dir 오류 |
| developer | 바인더 → 파서 → VO 분리. NFR-02를 설계로 충족. 코드 반려 1유형 | `!` 자체 점검 없음. 이스케이프 손상(U+FFFD, 서로게이트 직렬화). 실측 옮김 오류. ADR-0026 전제 불일치 |
| reviewer | 튜닝 뒤 놓친 코드 결함 0. 중복 실행 제거 | `!` grep 누락(TD-029). U+FEFF 발견을 suggestion으로만 처리. 사실 오류 인계 기준 없음 |
| tester | FR · NFR 전부 근거 확보. 문서 재실측으로 반려 3. NFR CI 수치 확보 | FR Trait 누락 · 키 오용. 실패 허용 59건을 이름 목록 고정 전에 판정. 문서 비대 |

## 개선안

처리는 오케스트레이션 세션의 대리 결정입니다. A(1~10)는 병합 · 릴리스 뒤 `feature/retro-prd-002-*`에서 반영합니다(BL-145, RETRO-PRD-001의 PR #13 선례). B(11~12)는 다음 토픽 첫 작업, C(13)는 백로그입니다.

| # | 대상 (스킬 / 에이전트 / 기준 문서 / 템플릿) | 내용 | 처리 |
|---|---|---|---|
| 1 | 에이전트 `developer.md` · `reviewer.md` · `tester.md` | 보이지 않는 문자 · 이스케이프 손상 grep(U+FFFD · FEFF · 200D, 경로 od -c), reviewer는 발견하면 REJECT | 반영(BL-145) |
| 2 | 스크립트 `check-docs.js` | U+FFFD 검사(BL-136), raw-log frontmatter 예외(BL-018) | 반영(BL-145) |
| 3 | 기준 문서 `coding-conventions.md`, 에이전트 `developer.md` · `reviewer.md` | null-forgiving: EF 생성자 `null!`만 예외, 점검 grep 추가(TD-029 규칙 부분) | 반영(BL-145) |
| 4 | 기준 문서 `testing-strategy.md` | 서로게이트 규칙(BL-131), Trait 규칙(FR / NFR 키, 전수 대조), 성능 NFR 증빙 표 형식 | 반영(BL-145) |
| 5 | 기준 문서 `database.md`, 에이전트 `dba.md` | 리셋 뒤 옛 식별자 전체 grep 0, database.md 밖 인용 포함, --output-dir, 커밋 R footer 해석 | 반영(BL-145) |
| 6 | 기준 문서 `database.md` | VO 매핑 규칙(HasConversion, VO끼리 비교, 복합 인덱스 불가), pk · ux 동시 위반 보고 순서 | 반영(BL-145) |
| 7 | 기준 문서 `agents.md` | 예상 실패 이름 목록 고정 뒤 판정, reviewer 사실 오류는 REJECT | 반영(BL-145) |
| 8 | 스킬 `sprint`, 에이전트 `orchestrator.md` | 튜닝 유지 + 재발 방지 임시 점검 handoff, ADR 2단계 입력, 제외 기준 원본 대조, 커버리지 기록, CI 영향 스프린트 확인(선택) | 반영(BL-145) |
| 9 | 기준 문서 `coding-conventions.md` | Validator VO 결과 패턴(BL-140), 0행 Handler 예시, 소규모 internal 중복 허용 기준 | 반영(BL-145) |
| 10 | 기준 문서 troubleshooting, PRD-002 | curl CP949 한 줄(BL-142), PRD 사실 정정(BL-143, FR-05 413 괄호) | 반영(BL-145) |
| 11 | 코드 · 아키텍처 테스트 | TD-029 위반 수정 + null-forgiving 금지 기계화(BL-119와 묶음) | 반영(다음 토픽, TD-029 비고) |
| 12 | 테스트 | FR-03 · 04 Trait 추가, 키 오용 2파일 수정 | 반영(다음 토픽, BL-144) |
| 13 | 백로그 | BL-138 · TD-030 · TD-010 트리거 대기, BL-121~127 · TD-028은 다음 PRD에서 | 보류 |

## ADR 후보

ADR 파일은 사용자 확인 뒤에 만듭니다(정책). 대리 결정: 아래 (1) · (3)과 RETRO-PRD-001 (a) · (b)를 "ADR 정합화" 한 작업으로 묶어 다음 토픽 첫 스프린트에서 처리합니다(BL-146).

- (1) + (2) ADR-0026 1절 부분 대체와 ADR-0028 415 경로 분담: 한 ADR로 묶어 작성 추천(상). 승인된 ADR과 구현이 다름
- (3) ADR-0025 보완(BL-141, 호스팅 로그 RequestPath, 405 · 404 fallback 범위): 작성 추천(중)
- (4) TD-030 중복 허용, (5) 0행 Handler 판정: ADR 불필요, 개선안 9로 처리
- RETRO-PRD-001 (c)~(j): 미결 유지

## 사용자 결정 필요

1. **대리 승인 10행 추인**(S05 4 · S06 4 · S07 2). 특히 S05-T02의 ADR 0025~0028 대리 확인: 정책상 ADR은 사용자 확인 뒤에 만드므로 사용자 추인이 필요합니다. 각 스프린트 문서의 추인 열은 `대기`입니다.
2. **ADR 파일 작성 확인**: 위 (1) · (3)과 RETRO-PRD-001 (a)~(j)(BL-146 작업 전에 확인).
3. **이번 회고의 대리 결정 추인**: 속도 튜닝 규칙 유지, 개선안 1~13 처리, TD-029 예외 범위(EF 생성자 `null!` 허용).
4. `disable-model-invocation` 제거 유지 여부(RETRO-PRD-001에서 이어짐).
5. RETRO-PRD-001 남은 항목: BL-113 · 065 · 117, 커밋 작성자 이메일, Docker 익명 볼륨 2개.

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-29 | orchestrator | 회고 작성(관점별 회고 4개 통합, 오케스트레이션 세션 대리 진행) |
