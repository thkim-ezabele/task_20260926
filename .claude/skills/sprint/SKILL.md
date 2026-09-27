---
name: sprint
description: 스프린트 하나를 진행한다. 에이전트별 계획 리뷰 → 작업마다 dba → developer → reviewer → tester 파이프라인(진입 점검, 반려 시 회귀, 단계별 커밋) → orchestrator 결과 리뷰와 백로그 / 기술부채 정리 → push와 sprint 태그. 사용자가 /sprint SNN 으로 직접 실행한다.
disable-model-invocation: true
argument-hint: "SNN (예: S01)"
---

# /sprint: 스프린트 진행

흐름과 규칙의 원본은 `wiki/10-delivery/agents.md`(인계 계약, 회귀 규칙, 커밋 규칙)다.

- 입력: `$ARGUMENTS` = 스프린트 ID (`S01` 등). 비어 있으면 PRD의 스프린트 중 `planned`인 가장 앞 번호를 제안한다.
- 에이전트: `orchestrator`, `dba`, `developer`, `reviewer`, `tester`

## 규칙

- **사용자 승인 지점**: ① 계획 리뷰 결과, ③ 결과 리뷰 · 백로그 / 기술부채 정리안(승인 시 push와 태그까지 진행). BLOCKED가 나오면 즉시 멈추고 사용자에게 묻는다.
- **커밋은 이 스킬이 한다.** 에이전트는 커밋하지 않는다. 작업자 단계가 끝날 때마다 로컬 커밋하고, **push는 스프린트 종료 때 한 번** 한다.
- 커밋에서 `wiki/08-worklog/raw/`는 제외한다(`git add -A` 후 `git reset -q -- wiki/08-worklog/raw/`).
- 재작업은 새 커밋으로 쌓는다. `git reset`, `--amend`, force push를 쓰지 않는다.
- 에이전트 결과를 사용자에게 보고할 때는 요약한다. 단계 진행 상황은 한 줄씩 알린다(예: `S01-T02 developer PASS (테스트 12개 통과)`).

## 절차

### ⓪ 사전 점검

1. 스프린트 문서 `wiki/10-delivery/sprints/SNN-*.md`를 찾고, frontmatter의 `prd`로 PRD 문서를 찾는다.
2. 현재 브랜치가 PRD의 `branch`(`feature/prd-NNN-*`)인지 확인한다. 아니면 체크아웃을 제안한다.
3. 스프린트 `status`가 `planned`(새로 시작) 또는 `active`(이어서 진행)인지 확인한다. `active`면 작업 표의 상태를 보고 **멈춘 작업부터 이어서** 진행한다(②로 이동).
4. 같은 PRD의 앞 스프린트가 모두 `done`인지 확인한다.
5. 작업 트리가 깨끗한지 확인한다(`raw/` 제외).

### ① 계획 리뷰

1. 한 메시지에서 `dba`, `developer`, `reviewer`, `tester`를 동시에 호출한다.
   ```
   mode: sprint-plan-review
   스프린트: wiki/10-delivery/sprints/SNN-*.md
   PRD: wiki/10-delivery/prd/PRD-NNN-*.md
   반환: 에이전트 정의의 "리뷰 반환 형식"(YAML)
   ```
2. `orchestrator`를 `mode: sprint-plan-integrate`로 호출하고 네 결과를 넣는다.
3. 사용자에게 계획 수정안(작업 추가 / 수정 / 삭제 / 순서 변경), 위험, 막히는 질문을 보고하고 **승인을 받는다.**
4. 승인 내용을 스프린트 문서에 반영한다: 작업 표, "계획 리뷰" 절, `status: active`, `started: <오늘>`, `wiki/10-delivery/README.md` 스프린트 목록 상태
5. 커밋: `docs(sprint): SNN 계획 확정` + footer `Stage: orchestrator`

### ② 작업 파이프라인

작업 표의 `depends_on` 순서대로, 상태가 `done` / `moved:*`가 아닌 작업마다 반복한다.

```
stages = [dba, developer, reviewer, tester]
i = 0, rejections = 0, rework_reasons = []
작업 상태를 doing으로 바꾼다
while i < 4:
    결과 = stages[i] 호출 (mode: task-stage)
    진행 기록에 한 줄 추가 (날짜, 작업, 단계, 판정, 내용)
    candidates가 있으면 backlog.md / tech-debt.md에 new 행 추가 (다음 ID)
    PASS    → 단계 커밋, rework_reasons = [], i += 1
    REJECT  → rejections += 1, 진행 기록 커밋
              rejections >= 3 이면 작업 상태 blocked, 멈추고 사용자에게 보고
              아니면 i = stages.index(reject_to), rework_reasons = 결과.reasons
    BLOCKED → 진행 기록 커밋, 멈추고 사용자에게 보고 (답을 받아 같은 단계 재실행)
작업 상태를 done으로, 작업 표의 커밋 열에 이 작업의 커밋 해시(짧은 형식)를 적고 커밋
```

**단계 호출 프롬프트**

```
mode: task-stage
작업: SNN-TNN <제목>
요구사항: PRD-NNN/FR-NN (인수 조건 원문)
완료 조건: <작업 표의 완료 조건>
스프린트: wiki/10-delivery/sprints/SNN-*.md
PRD: wiki/10-delivery/prd/PRD-NNN-*.md
앞 단계 결과: <이번 작업에서 앞 단계들이 반환한 YAML 요약: status, changed_files, reasons>
rework_reasons: <되돌아온 경우 반려 사유, 아니면 없음>
반환: 에이전트 정의의 "단계 반환 형식"(YAML)
```

**단계 커밋**

- 제목: 에이전트가 준 `commit_message` (없으면 `<type>(<scope>): <작업 제목> (SNN-TNN)`)
- footer: `Stage: <agent>`
- reviewer는 코드를 바꾸지 않으므로, 스프린트 문서 진행 기록만 커밋한다: `docs(sprint): SNN-TNN 리뷰 판정 (SNN-TNN)`
- 변경이 전혀 없는 단계(예: dba "DB 변경 없음")도 진행 기록 한 줄을 커밋해 단계 통과를 이력에 남긴다.

**반려 커밋**: `docs(sprint): SNN-TNN <단계> 반려 → <reject_to> (SNN-TNN)` + footer `Stage: <agent>`. 반려한 단계가 만든 파일 변경(재현 테스트 등)이 있으면 함께 커밋한다.

### ③ 결과 리뷰와 백로그 / 기술부채 정리

1. `orchestrator`를 `mode: sprint-result-review`로 호출한다. 스프린트 문서, PRD, `backlog.md`, `tech-debt.md` 경로와 이 스프린트의 커밋 범위(`git log --oneline <계획 확정 커밋>..HEAD`)를 넣는다.
2. DoD를 점검한다: 솔루션이 있으면 `dotnet build`와 `dotnet test`를 직접 실행해 결과를 확인한다.
3. 사용자에게 보고하고 **승인을 받는다.**
   - 결과 리뷰: 계획 대비 실제, FR 충족, 반려 분석
   - 백로그 / 기술부채 정리안: 항목마다 처리(`open` / `planned:SNN` / `dropped`)와 이유, 병합 대상
   - 스프린트 회고 초안, DoD 결과
   - 승인하면 할 일: 문서 반영, 커밋, **push, 태그 `sprint/SNN`**
4. 승인 내용을 반영한다.
   - `backlog.md`, `tech-debt.md`: `new` 항목을 정리안대로 바꾼다. **`new`가 남지 않아야 한다.**
   - 스프린트 문서: "결과 리뷰", "생긴 백로그 / 기술부채"(정리 결과), "회고", DoD 체크, `status: done`, `finished: <오늘>`
   - `wiki/10-delivery/README.md` 스프린트 목록 상태
   - 이 스프린트에서 정해진 결정이 ADR 후보면 목록만 보고한다(ADR 파일은 사용자 확인 후).
5. 커밋: `docs(sprint): SNN 종료 - 결과 리뷰와 백로그 / 기술부채 정리` + footer `Stage: orchestrator`
6. push와 태그:
   ```
   git push
   git tag -a sprint/SNN -m "SNN <제목> 종료"
   git push origin sprint/SNN
   ```

### ④ 마무리 보고

- 완료 / 이관 작업, 반려 횟수, 정리된 백로그 / 기술부채, push와 태그 결과
- 다음 단계: PRD에 `planned` 스프린트가 남아 있으면 `/sprint <다음 ID>`, **없으면 `/retro PRD-NNN`**
- 세션을 마치기 전에 `/worklog` 실행을 안내한다(worklog의 `sprint` 필드에 SNN)
