---
name: sprint
description: 스프린트 하나를 진행한다. developer 계획 리뷰 + orchestrator 통합 → 작업마다 dba(DB 작업만) → developer → 검증 단계(reviewer 코드 리뷰 ∥ tester 실행 검증) 파이프라인(진입 점검, 반려 시 회귀, 단계별 커밋) → orchestrator 결과 리뷰와 백로그 / 기술부채 정리 → push(CI 확인 · 태그는 PRD 마지막 스프린트 뒤 일괄). 사용자가 /sprint SNN 으로 직접 실행한다.
argument-hint: "SNN (예: S01)"
---

# /sprint: 스프린트 진행

흐름과 규칙의 원본은 `wiki/10-delivery/agents.md`(인계 계약, 회귀 규칙, 커밋 규칙)다.

- 입력: `$ARGUMENTS` = 스프린트 ID (`S01` 등). 비어 있으면 PRD의 스프린트 중 `planned`인 가장 앞 번호를 제안한다.
- 에이전트: `orchestrator`, `dba`, `developer`, `reviewer`, `tester`

## 규칙

- **사용자 승인 지점**: ① 계획 리뷰 결과, ③ 결과 리뷰 · 백로그 / 기술부채 정리안(승인 시 push까지 진행). BLOCKED가 나오면 즉시 멈추고 사용자에게 묻는다.
- **속도 규칙**(2026-09-28 사용자 지시, 스프린트 1개 5시간 이상 소요 개선):
  - 전체 `dotnet test`는 작업당 **검증 단계의 tester가 1회**만 실행한다. developer는 TDD 중 영향받는 테스트 프로젝트만 실행하고, reviewer는 빌드 · 테스트를 실행하지 않는다.
  - **진행 기록은 한 행 200자 이내**(판정 + 핵심 수치 + 커밋 해시). 상세는 커밋 메시지 본문에 둔다. 에이전트가 반환한 대조표 · 점검표는 스프린트 문서에 옮기지 않는다(판정 근거는 반환값과 커밋 본문).
  - **에이전트 입력 최소화**: 단계 호출 프롬프트에 이 작업의 작업 표 행 · 완료 조건 · 이 작업 인계 메모만 넣는다. 에이전트는 스프린트 문서 · 기준 문서 전체를 읽지 않고 필요한 절만 Grep으로 찾아 읽는다.
  - **증빙 대응표 전수 대조는 스프린트 종료 ③ DoD에서 1회**만 한다. 작업 단계에서는 이번 작업이 바꾼 행만 갱신 · 확인한다.
  - **스프린트 회고는 하지 않는다.** 회고와 스킬 · 에이전트 개선은 PRD의 모든 스프린트가 끝난 뒤 `/retro PRD-NNN`에서만 한다.
- **대리 승인**: 사용자 부재 등으로 승인 지점(계획 리뷰 · 결정 · BLOCKED · 결과 리뷰, ADR 확인 포함)을 사용자가 아닌 세션(오케스트레이션 세션 등)이 승인하면, 그때마다 스프린트 문서 "대리 승인" 목록에 한 행을 추가한다(날짜, 승인 지점, 승인 내용, 승인한 세션, 근거 진행 기록). 추인은 `/retro` ④에서 사용자가 일괄로 한다.
- **기록 수치는 명령 출력에서 옮겨 적는다.** 진행 기록 · 증빙 · 결과 리뷰의 수치(테스트 수, 커버리지, 소요 시간, 파일 · 행 수, 커밋 해시 등)는 그 기록을 쓸 때 실행한 명령의 출력에서 옮긴다. 기억이나 앞 기록에서 옮겨 쓰지 않는다.
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
6. 실행 환경을 확인한다: `docker info`(Docker 실행), 저장소 루트에서 `dotnet --version`(`global.json`을 만족하는 SDK), `gh auth status`. 스프린트 작업에 필요한 것이 빠져 있으면 사용자에게 알리고 진행 여부를 묻는다. 환경 때문에 미루는 검증은 진행 기록에 남긴다.
   - Docker Engine API 버전: `docker version --format '{{.Server.APIVersion}}'`. 이 PC처럼 `1.43`이면 로컬 통합 테스트(Testcontainers)에 `DOCKER_API_VERSION=1.43`이 필요하므로 인계 메모에 적는다(CI 무관, BL-102).
   - 개발 인증서 신뢰: `dotnet dev-certs https --check --trust`(종료 코드 `0` 신뢰, `7` 미신뢰). 미신뢰면 AppHost를 띄우는 작업은 `--launch-profile http`로 실행하도록 인계 메모에 적는다(BL-099). `--trust`는 사용자 확인 없이 실행하지 않는다.
   - 새 clone 재현 · 원격 기준 검증 작업이 있으면: `git fetch` 후 `git status -sb`와 `git rev-list --count origin/<토픽 브랜치>..HEAD`로 원격 토픽 브랜치와 로컬 HEAD의 차이를 확인한다. 원격이 뒤처져 있으면(push는 스프린트 종료 때 한 번) clone 원본(로컬 저장소 / 임시 push)을 계획 리뷰에서 정하도록 사용자에게 묻는다.
   - **dba 작업**(분할의 `db_change: true` 또는 작업별 파이프라인 표 dba 열이 "해당 없음"이 아닌 작업)이 있는데 Docker를 쓸 수 없으면 **스프린트를 시작하지 않고 멈춘다**(실측 없이 DB 결정을 하지 않는다).
   - 점검 결과(명령과 출력 값)는 ①의 계획 리뷰 절에 적는다.

### ① 계획 리뷰

1. `developer`만 호출한다(PRD 단계에서 병렬 리뷰를 이미 했으므로 dba · reviewer · tester 계획 리뷰는 하지 않는다). DB 작업이 있으면 developer에게 DB 관점(스키마 · 마이그레이션 위험)도 함께 보게 한다.
   ```
   mode: sprint-plan-review
   스프린트: wiki/10-delivery/sprints/SNN-*.md
   PRD: wiki/10-delivery/prd/PRD-NNN-*.md
   반환: 에이전트 정의의 "리뷰 반환 형식"(YAML). findings는 막히는 것 · 상 위험 위주로 10개 이내
   ```
2. `orchestrator`를 `mode: sprint-plan-integrate`로 호출하고 developer 결과를 넣는다. 수정안은 꼭 필요한 변경만(작업 분할 · 추가는 막히는 경우만) 내게 한다.
3. 사용자에게 계획 수정안(작업 추가 / 수정 / 삭제 / 순서 변경), 위험, 막히는 질문을 보고하고 **승인을 받는다.**
   - **CI 결과로만 판정할 수 있는 조건**(토픽 PR CI 통과, CI 소요 시간, 러너 환경 등)은 작업 완료 조건에 넣지 않는다. 수정안에 있으면 작업 완료 조건에서 빼고 스프린트 종료 판정(③ 6의 push → 토픽 PR CI 통과 확인)으로 옮긴다. 작업은 로컬에서 판정할 수 있는 조건으로 끝낸다.
4. 승인 내용을 스프린트 문서에 반영한다: 작업 표, "계획 리뷰" 절, `status: active`, `started: <오늘>`, `wiki/10-delivery/README.md` 스프린트 목록 상태
   - 작업을 재구성(추가 · 분할 · 병합 · 번호 변경)했으면 `backlog.md` · `tech-debt.md`에서 이 스프린트 작업 번호를 가리키는 행(`planned:SNN` 편입 항목, 발생 작업 등)을 새 작업 번호로 고친다.
   - "인계 메모"는 **작업별 하위 항목으로 나누고** 그 작업의 단계 입력에 필요한 것만 짧게 적는다(여러 작업 공통 사항은 한 번만 적고 작업 항목에서 가리킨다). 증빙 · 실측 작업이 있으면 작업마다 **"알려진 잡음 · 제외 기준"**(판정에서 뺄 알려진 로그 · 오류와 개수 기록 방식, 비밀 점검 제외 행 등)을 이 단계에서 확정해 적는다. 작업 중에 판정 기준을 늘려야 하면 진행 기록에 남기고 사용자 승인을 받는다.
   - 원칙: 증빙 틀(템플릿) 칸의 기준은 기록용이고, **인수 판정은 작업 표의 완료 조건 기준**이다(원본 규칙은 `wiki/10-delivery/agents.md`).
5. 커밋: `docs(sprint): SNN 계획 확정` + footer `Stage: orchestrator`

### ② 작업 파이프라인

작업 표의 `depends_on` 순서대로, 상태가 `done` / `moved:*`가 아닌 작업마다 반복한다.

```
stages = [dba, developer, verify]      # verify = reviewer ∥ tester (한 메시지에서 동시에 호출)
i = 0, rejections = 0, rework_reasons = []
작업 상태를 doing으로 바꾼다
작업이 스키마 · EF 매핑 · 마이그레이션 · SQL · Repository 쿼리를 바꾸지 않으면
(작업별 파이프라인 표의 dba 열이 "해당 없음"이거나 판정 · 확인만 하는 경우 포함):
    dba를 호출하지 않고 진행 기록에 "dba | 해당 없음" 한 줄만 적는다 (커밋하지 않음, developer 커밋에 포함)
    i = 1
while i < 3:
    stages[i]가 verify면:
        reviewer(코드 리뷰만)와 tester(실행 검증)를 한 메시지에서 동시에 호출 (mode: task-stage)
        결과 = 둘 중 하나라도 REJECT면 REJECT(reject_to는 더 앞 단계), 하나라도 BLOCKED면 BLOCKED, 둘 다 PASS면 PASS
    아니면 결과 = stages[i] 호출 (mode: task-stage)
    진행 기록에 한 줄 추가 (날짜, 작업, 단계, 판정, 내용 200자 이내)
    candidates가 있으면 backlog.md / tech-debt.md에 new 행 추가 (다음 ID, 스프린트 밖 대상만)
    handoff가 있으면 진행 기록에 한 줄로 적고 대상 작업의 "인계 메모"로 넘긴다 (백로그로 올리지 않음)
    PASS    → 단계 커밋 (verify는 tester 변경 + reviewer · tester 진행 기록을 한 커밋으로)
              rework_reasons = [], i += 1
    REJECT  → rejections += 1, 진행 기록 커밋 (tester 재현 테스트 포함)
              rejections >= 3 이면 작업 상태 blocked, 멈추고 사용자에게 보고
              아니면 i = stages.index(reject_to), rework_reasons = 결과.reasons (reviewer · tester 사유 합침)
    BLOCKED → 진행 기록 커밋, 멈추고 사용자에게 보고 (답을 받아 같은 단계 재실행)
작업 상태를 done으로, 작업 표의 커밋 열에 이 작업의 커밋 해시(짧은 형식)를 적고 커밋
```

- verify에서 tester가 테스트를 추가하는 동안 reviewer는 developer 커밋의 diff만 본다. reviewer가 tester 추가 테스트까지 봐야 하는 규칙 위반은 tester 자체 점검(코딩 컨벤션)으로 대신한다.
- 반려로 developer가 재작업하면 verify를 다시 한다(reviewer ∥ tester 둘 다).

**단계 호출 프롬프트**

```
mode: task-stage
작업: SNN-TNN <제목>
요구사항: PRD-NNN/FR-NN (인수 조건 원문)
완료 조건: <작업 표의 완료 조건>
스프린트: wiki/10-delivery/sprints/SNN-*.md (전체를 읽지 말 것. 필요하면 이 작업 ID로 Grep)
PRD: wiki/10-delivery/prd/PRD-NNN-*.md (해당 FR 절만)
앞 단계 결과: <이번 작업에서 앞 단계들이 반환한 YAML 요약: status, changed_files, reasons 핵심 5줄 이내>
rework_reasons: <되돌아온 경우 반려 사유, 아니면 없음>
인계 메모: <앞 작업 · 단계가 이 작업에 남긴 handoff, 없으면 없음>
테스트 범위: <도메인 로직 | 기반 · 셋팅> (developer · tester 호출에만, agents.md "테스트 범위")
검증 역할: <verify 단계에만. reviewer = 코드 리뷰만(빌드 · 테스트 실행 금지) | tester = 실행 검증(build · format · 전체 test · check-docs 1회)>
속도 규칙: 기준 문서는 필요한 절만 Grep으로 읽는다. reasons는 핵심만 10줄 이내, 대조표는 반환값에만.
반환: 에이전트 정의의 "단계 반환 형식"(YAML)
```

- 인계 메모는 **이 작업의 항목만** 넘긴다(다른 작업 항목은 넘기지 않음). 작업 중 쌓인 handoff는 대상 작업 항목에 합쳐 압축하고, 그래도 길면 스크래치 파일에 모아 두고 경로를 넘겨도 된다. 원문을 요약하다 조건을 빠뜨리지 않게 한다.
- 인계 메모의 "알려진 잡음 · 제외 기준"을 함께 넘긴다. 에이전트는 이 기준 밖의 잡음을 임의로 제외하지 않고 BLOCKED로 판단받는다.
- 에이전트가 완료 조건 항목을 `candidates`로 스프린트 밖에 내보내려 하면 기록하지 말고 사용자에게 묻는다(agents.md "구조").

**문서 · ADR 작업** (원본: `wiki/10-delivery/agents.md` "문서 작업과 ADR 확인"): 작업이 ADR을 만들면 developer 단계를 둘로 나눈다.

1. developer 1차: 단계 호출 프롬프트에 `adr_phase: draft`를 넣는다. PASS면 반환된 `adr_drafts`를 작업 단위로 한 번에 사용자에게 보여 주고 **확인을 받는다**(이 단계는 아직 커밋하지 않는다. 조사 기록 등 다른 파일 변경은 2차와 함께 커밋).
   - 보여 주는 형식: ADR마다 **결정 한 문장 · 고른 대안과 버린 대안 · 결과(부담)** 요약 표, 사용자가 고를 선택지(추천안 표시), 초안 전문 파일 경로. 전문을 대화에 붙이지 않는다. 확인이 형식적이 되지 않게 "추천과 다르게 고를 만한 지점"을 한 줄씩 적는다.
2. 수정 요청이 있으면 요청을 `rework_reasons`에 넣어 1차를 다시 호출하고 다시 확인받는다.
3. developer 2차: `adr_phase: write`와 확인된 초안 전문(수정 반영)을 넣어 호출한다. PASS면 단계 커밋한다.
4. verify(reviewer ∥ tester)가 반려할 때 사유가 형식 문제면 2차부터, 결정 내용 문제면 1차부터 다시 한다. 반려 횟수 규칙은 같다.
5. 문서 작업의 verify: reviewer는 D2 · D4 · D5(읽기 판정), tester는 D1(`check-docs`) · D3(사실 재실측)을 맡는다. 같은 항목을 둘이 중복해 보지 않는다.

**단계 커밋**

- 제품 파일(코드 · 설정 · 기준 문서 · ADR)이 바뀐 단계: 에이전트가 준 `commit_message` (없으면 `<type>(<scope>): <작업 제목> (SNN-TNN)`)
- 판정 · 기록만 남는 단계(스프린트 문서 · 백로그 · 기술부채만 바뀜, 변경 없는 dba · tester 등): `docs(sprint): SNN-TNN <단계> 판정 (SNN-TNN)`로 통일한다(에이전트의 `commit_message`는 쓰지 않음).
- footer: `Stage: <agent>` (verify 단계는 `Stage: reviewer, tester`)
- **verify는 커밋 1개**: tester 변경 + reviewer · tester 진행 기록 두 줄. dba "해당 없음"(호출 생략) 줄은 developer 단계 커밋에 함께 들어간다.
- 작업 완료 처리(작업 표 상태 · 커밋 해시)는 verify 커밋에 함께 넣어도 된다(작업당 커밋 목표: developer 1 + verify 1, dba 작업이면 + dba 1).
- 문서를 바꾼 단계는 커밋 전에 `node scripts/check-docs.js`로 결함이 늘지 않았는지 확인한다.

**반려 커밋**: `docs(sprint): SNN-TNN <단계> 반려 → <reject_to> (SNN-TNN)` + footer `Stage: <agent>`. 반려한 단계가 만든 파일 변경(재현 테스트 등)이 있으면 함께 커밋한다.

### ③ 결과 리뷰와 백로그 / 기술부채 정리

0. 진행 기록에 남은 `handoff` 중 대상 작업에서 반영되지 않은 것이 있으면 `candidates`로 바꿔 `new` 행으로 기록한다.
1. `orchestrator`를 `mode: sprint-result-review`로 호출한다. 스프린트 문서, PRD, `backlog.md`, `tech-debt.md` 경로와 이 스프린트의 커밋 범위(`git log --oneline <계획 확정 커밋>..HEAD`)를 넣는다.
2. DoD를 점검한다: 솔루션이 있으면 `dotnet build`와 `dotnet test`를 직접 실행해 결과를 확인한다. 증빙 대응표가 있으면 **여기서 1회** 전수 대조한다(스크립트 또는 trx 이름 대조).
3. 사용자에게 보고하고 **승인을 받는다.**
   - 결과 리뷰: 계획 대비 실제, FR 충족, 반려 분석
   - 백로그 / 기술부채 정리안: 항목마다 처리(`open` / `planned:SNN` / `dropped`)와 이유, 병합 대상
   - DoD 결과(로컬 build · test · 대응표 대조)
   - CI로만 판정할 조건 목록(PRD 마지막 스프린트 뒤 일괄 판정)
   - "대리 승인" 목록(있으면 행 수와 항목, `/retro` ④에서 추인 대상)
   - 승인하면 할 일: 문서 반영, 커밋, **push**(CI 대기 · 태그는 하지 않음, 6 참고)
4. 승인 내용을 반영한다.
   - `backlog.md`, `tech-debt.md`: `new` 항목을 정리안대로 바꾼다. **`new`가 남지 않아야 한다.**
   - 스프린트 문서: "결과 리뷰", "생긴 백로그 / 기술부채"(정리 결과), DoD 체크(CI 항목은 "PRD 종료 뒤 일괄"로 비워 둠), frontmatter `status: done`, `finished: <오늘>`, `adrs: [이 스프린트에서 accepted된 ADR]`. "회고" 절에는 "토픽 회고(`/retro PRD-NNN`)에서 다룸" 한 줄만 적는다.
   - `wiki/10-delivery/README.md` 스프린트 목록 상태
   - 이 스프린트에서 정해진 결정이 ADR 후보면 목록만 보고한다(ADR 파일은 사용자 확인 후).
5. 커밋: `docs(sprint): SNN 종료 - 결과 리뷰와 백로그 / 기술부채 정리` + footer `Stage: orchestrator`
6. push (**CI 대기 · 태그는 여기서 하지 않는다**, 2026-09-28 사용자 지시):
   1. `git push`. CI는 push로 시작되지만 기다리지 않는다.
   2. 진행 기록에 "종료 커밋 `<5의 커밋 해시>`(태그 대상, PRD 종료 뒤 일괄)" 한 줄을 적는다(이 줄은 worklog 커밋 또는 다음 스프린트 첫 커밋에 포함돼도 된다).
7. **PRD의 마지막 스프린트면** push 뒤 일괄 CI 판정 · 태그를 한다:
   1. 토픽 PR의 최신 HEAD CI를 기다린다: `gh pr checks <PR 번호> --watch` 또는 `gh run watch <run-id> --exit-status`
   2. 실패하면 멈추고 보고한다. 원인 작업을 재작업(새 커밋)하고 다시 push한다. 통과할 때까지 태그 보류.
   3. 통과하면 이 PRD 스프린트 문서마다 DoD의 CI 항목 · CI로만 판정할 조건을 체크하고, 실행 링크 · 소요 시간 · 러너 SDK(`gh run view` 출력)를 적어 커밋(`docs(sprint): PRD-NNN 스프린트 DoD 기록 - PR CI 통과와 소요 시간`, `Stage: orchestrator`)하고 push한다.
   4. 태그를 스프린트마다 진행 기록의 종료 커밋에 붙인다(이미 있는 태그는 건너뜀):
      ```
      git tag -a sprint/SNN <종료 커밋> -m "SNN <제목> 종료"
      git push origin --tags   # 또는 태그마다 git push origin sprint/SNN
      ```

### ④ 마무리 보고

- 완료 / 이관 작업, 반려 횟수, 정리된 백로그 / 기술부채, push 결과(마지막 스프린트면 CI · 태그 결과), 대리 승인 행 수(추인은 `/retro` ④)
- 다음 단계: PRD에 `planned` 스프린트가 남아 있으면 `/sprint <다음 ID>`, **없으면 `/retro PRD-NNN`**
- 세션을 마치기 전에 `/worklog` 실행을 안내한다(worklog의 `sprint` 필드에 SNN)
