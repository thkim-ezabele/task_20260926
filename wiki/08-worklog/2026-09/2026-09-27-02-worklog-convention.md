---
title: "WL-2026-09-27-02: Worklog 체계 · 장기기억 · Obsidian 재구성"
type: worklog
date: 2026-09-27
session: "4e7d8bf0"
model: "Claude Code · Claude Opus 5.5"
adrs: []
aliases: [WL-2026-09-27-02]
tags: [worklog]
created: 2026-09-27
updated: 2026-09-27
---

# WL-2026-09-27-02: Worklog 체계 · 장기기억 · Obsidian 재구성

- 원문 로그: [raw/2026-09-27](../raw/2026-09-27.md)
- 관련: 커밋 `a7214b1`, `54d0347`

## 목표

- 드라이브 고장으로 폴더를 옮긴 뒤 이전 작업(위키 구조, worklog 논의)을 이어간다.
- ADR과 별개로 세션마다 프롬프트와 작업 내용을 남기는 worklog 체계를 확정하고 만든다.
- 유실에 대비해 GitHub 저장소를 구성한다.
- (후반) 기본 개발 흐름의 방향을 정하고, 위키 안에 장기기억 장치를 만들고, 위키를 Obsidian 형식으로 재구성한다.

## 주요 프롬프트

1. "다른 하드 드라이브가 망가져서 폴더를 옮기고 다시 진행중이야 … adr 과는 별개로 세션마다 프롬프트 또는 작업 내용에 대해서 로그를 남기는 방안에 대해 얘기중이였어"
2. "wiki/08-worklog로 진행하고 hook이랑 git init도 같이 해줘 … 워크로그 작성은 스킬형태로 만들어서 내가 세션 종료하기전에 한번씩 진행할께"
3. "이번 세션 worklog 작성해줘" → "아직 하지 말고 일단 커밋까지 해줘" (push 없이 커밋만)
4. "난 하나의 prd 형태의 요구사항을 주면 그걸 topic 으로 해서 스프린트를 만들고 개발을 진행하게 될꺼야 이때 … 백로그 기술부채 등에 대해서도 기록을 남겨야해 … 세부적인건 다음 세션에서 설계할꺼야 / wiki 의 목적인 장기기억을 위한 장치가 있는지 확인해주고 … obsidian 형태로 되어 있는지도 확인해봐"
5. "claude.md 로 안하고 위키안에 따로 장기기억을 위한 내용 마련하고 claude.md 에서는 그 위치를 바라보도록 진행 / 장기기억은 이전 세션에서의 작업/해당 프로젝트의 개념/설계내용/정책 등등 … / obsidian 형태로 재구성할께 지금 진행하자"

## 작업 내용

### 전반: worklog 체계와 저장소

- 옮긴 폴더의 상태를 확인했다. `wiki/` 00~07 섹션과 ADR 0001~0006은 남아 있었지만 git 저장소가 아니었다.
- Worklog 체계를 제안했고, 사용자가 확정했다.
  - 위치: `wiki/08-worklog/` (기존 규칙 "문서는 모두 wiki 안"을 따름)
  - 정리본: `YYYY-MM/YYYY-MM-DD-NN-title.md`. 세션 종료 전 사용자가 `/worklog` 스킬로 작성한다.
  - 원문: `UserPromptSubmit` hook이 `raw/YYYY-MM-DD.md`에 자동으로 기록한다.
- worklog README, 템플릿, 이전 세션 복원 로그(WL-2026-09-27-01)를 작성했다.
- 프롬프트 기록 hook(Node.js)을 만들고, 가짜 입력으로 테스트했다. 코드블록이 섞인 프롬프트도 fence가 깨지지 않았다.
- `/worklog` 스킬을 만들었다. 사용자가 직접 입력할 때만 실행되도록 `disable-model-invocation` 을 설정했다.
- hook 설치 전 프롬프트 두 개를 raw 로그에 수동으로 옮겨 적었다.
- `.gitignore`를 만들고, `git init` 한 뒤 `origin/main`에 첫 커밋 `a7214b1`을 push했다.
- 이 로그의 첫 버전을 `54d0347`로 커밋했다(push 안 함).

### 후반: 개발 흐름, 장기기억, Obsidian

- **기본 개발 흐름**을 기록했다. PRD 1건이 스프린트 1개의 topic이 되고, 개발하면서 백로그와 기술부채를 기록한다. 세부 설계는 다음 세션에 한다.
- **점검 결과**: 장기기억 장치가 없었다(`CLAUDE.md` 없음, Claude auto-memory는 경로에 묶여 있고 git 밖에 있음). 위키는 Obsidian과 호환되지만 Obsidian 형식은 아니었다(frontmatter, vault 설정 없음).
- **장기기억** `wiki/09-memory/`를 만들었다. 파일은 README(작성 원칙, 갱신 시점), project(개념), design(설계 요약), policies(정책·규칙), sessions(이력, 현재 상태, 다음 할 일)이다.
- 루트 **`CLAUDE.md`**를 만들었다. 내용 없이 `@wiki/09-memory/*.md`만 import해서 세션마다 자동으로 불러온다.
- Claude auto-memory의 프로젝트 내용을 위키로 옮기고, auto-memory에는 "기억 위치는 `wiki/09-memory/`"라는 한 건만 남겼다.
- **Obsidian 재구성**
  - Node 스크립트로 모든 문서(50건)에 frontmatter(`title`, `type`, `status`, `tags`, `created`, `updated`, ADR·worklog 전용 필드, `aliases`)를 넣고, 본문의 `상태:` 표기를 없앴다.
  - 템플릿을 `wiki/_templates/`(`doc`, `adr`, `worklog`)로 옮기고, Obsidian 변수(`{{date}}`)를 쓰도록 바꿨다.
  - `wiki/.obsidian/`에 vault 설정(마크다운 링크, 상대경로, 링크 자동 갱신, `_assets/`, raw 로그 검색 제외)과 Templates 폴더 설정을 추가했다. 개인 작업 상태는 gitignore에 넣었다.
  - 위키 홈에 09 섹션, frontmatter 스키마, 상태 값 대응표, Obsidian 규칙을 적었다. ADR·worklog README의 템플릿 경로를 고쳤다.
- hook이 raw 로그를 새로 만들 때 frontmatter를 붙이도록 고쳤다. `/worklog` 스킬에 장기기억 갱신 단계를 추가했다.
- 검증 스크립트로 모든 문서에 frontmatter가 있는지, 상대 링크와 `@import` 경로가 살아 있는지 확인했다. 문제는 없었다(예시 코드 안의 링크만 오탐).

### 변경 파일

| 파일 | 변경 | 설명 |
|---|---|---|
| `CLAUDE.md` | 추가 | 장기기억 파일 `@import` |
| `wiki/09-memory/*.md` (5건) | 추가 | 장기기억 |
| `wiki/_templates/adr.md`, `worklog.md` | 이동·수정 | `adr/0000-template.md`, `08-worklog/_template.md`에서 이동, frontmatter 추가 |
| `wiki/_templates/doc.md` | 추가 | 일반 문서 템플릿 |
| `wiki/.obsidian/app.json`, `templates.json` | 추가 | vault 설정 |
| `wiki/**/*.md` (기존 문서 전체) | 수정 | frontmatter 추가, 본문 상태 표기 제거 |
| `wiki/README.md` | 수정 | 08·09 섹션, 작성 규칙, frontmatter 스키마, Obsidian 규칙, 상태 범례 |
| `wiki/03-architecture/adr/README.md` | 수정 | 템플릿 경로, 대체 절차(frontmatter) |
| `wiki/08-worklog/README.md` | 추가·수정 | worklog 규칙, 목록, 템플릿 경로 |
| `wiki/08-worklog/2026-09/*.md` | 추가·수정 | WL-01(사후 복원), WL-02(이 문서) |
| `wiki/08-worklog/raw/2026-09-27.md` | 추가 | 프롬프트 원문 (백필 + hook 자동 기록) |
| `.claude/settings.json` | 추가 | `UserPromptSubmit` hook 등록 |
| `.claude/hooks/log-prompt.js` | 추가·수정 | 프롬프트 원문 기록, 새 파일에 frontmatter |
| `.claude/skills/worklog/SKILL.md` | 추가·수정 | `/worklog` 스킬, 장기기억 갱신 단계 |
| `.gitignore` | 추가·수정 | .NET, IDE, 시크릿, 개인 Claude 설정, Obsidian 작업 상태 제외 |

## 결정 사항

| 결정 | 이유 | ADR |
|---|---|---|
| worklog 위치를 `wiki/08-worklog/`로 둔다 | 기존 규칙 "위키 문서는 모두 `wiki/` 안에서 관리"와 일관성 | - |
| 정리본은 사용자가 `/worklog`로 직접 작성한다 | 과제 증빙이라 기록 시점을 사용자가 통제 | - |
| 프롬프트 원문은 hook으로 자동 수집한다 | 누락 없이 증빙 확보. 정리본은 요약, 원문은 근거 | - |
| GitHub private 저장소(`thkim-ezabele/task_20260926`)를 사용한다 | 드라이브 고장 같은 유실 재발 방지 | - |
| 기본 개발 흐름: PRD 1건 = 스프린트 1개, 백로그·기술부채 기록 | 요구사항 → 개발의 추적성 확보 (세부 설계는 다음 세션) | - |
| 장기기억은 `wiki/09-memory/`에 두고 `CLAUDE.md`는 import만 한다 | git으로 백업되고 폴더를 옮겨도 남음. 기억의 원본을 위키 한곳에 둠 | - |
| 위키를 Obsidian vault로 운영하되 링크는 마크다운 상대 링크를 쓴다 | `[[wikilink]]`는 GitHub에서 링크로 표시되지 않음 | - |
| 문서 상태는 frontmatter `status`로 관리한다 | Obsidian Properties·Dataview로 조회 가능, Claude도 기계적으로 읽을 수 있음 | - |

> ADR 후보: 없음. 모두 작업 방식과 문서 운영에 관한 결정이라 위키 규칙(위키 홈, worklog README, 장기기억 policies)에 적는 것으로 충분하다고 판단했다.

## 이슈 / 배운 점

- 이전 세션은 git 저장소가 없어서 세션 기록을 모두 잃었다. 산출물(`wiki/`)만 남아서 WL-01은 사후 복원으로 작성했다.
- 이 PC에서 `python`은 Windows 스토어 스텁이라 실행되지 않는다. 처음에 README 수정을 python으로 했다가 적용되지 않아 다시 했다. 스크립트는 Node.js로 작성한다.
- `.claude/`를 세션 중에 새로 만들었지만, hook은 재시작 없이 바로 적용됐다.
- 처음 frontmatter를 확인할 때 `^---$`로 검색해서 본문의 구분선까지 frontmatter로 잘못 셌다. 파일 첫 줄만 확인해서 frontmatter가 없다는 것을 바로잡았다.
- Claude auto-memory는 프로젝트 경로에 묶여 있어서, 폴더를 옮기면 빈 상태로 시작한다. 이것 때문에 장기기억을 위키로 옮겼다.

## 다음 할 일

- [ ] **PRD → 스프린트 → 개발 흐름 세부 설계**: 스프린트, 백로그, 기술부채 문서의 폴더 구조, 템플릿, frontmatter(`type`, 상태 값), worklog·ADR·장기기억과의 연결 방식
- [ ] 새 세션에서 `CLAUDE.md` import로 장기기억이 자동으로 로드되는지 확인
- [ ] Obsidian에서 `wiki/`를 vault로 열고 Templates 코어 플러그인 켜기 (사용자)
- [ ] 커밋 작성자 이메일 확인 (현재 전역 설정 `taehoon365@gmail.com`)
- [ ] 아직 push하지 않은 커밋(`54d0347`과 이후 변경분) push
- [ ] 프로젝트 범위(In / Out of Scope)와 이해관계자 정의 (`00-overview/project-overview.md`)
- [ ] Git 워크플로우(브랜치 전략, 커밋 규칙) 작성 (`04-development/git-workflow.md`)
