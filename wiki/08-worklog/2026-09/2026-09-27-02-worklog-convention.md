# WL-2026-09-27-02: Worklog 체계 · 프롬프트 hook · Git 저장소 구성

- 날짜: 2026-09-27
- 도구 / 모델: Claude Code · Claude Opus 5.5
- 세션 ID: `4e7d8bf0` ([원문 로그](../raw/2026-09-27.md))
- 관련: 커밋 `a7214b1`

## 목표

- 드라이브 고장으로 폴더를 옮긴 뒤 이전 작업(위키 구조, worklog 논의)을 이어간다.
- ADR과 별개로 세션마다 프롬프트와 작업 내용을 남기는 worklog 체계를 확정하고 만든다.
- 유실에 대비해 GitHub 저장소를 구성한다.

## 주요 프롬프트

1. "다른 하드 드라이브가 망가져서 폴더를 옮기고 다시 진행중이야 … adr 과는 별개로 세션마다 프롬프트 또는 작업 내용에 대해서 로그를 남기는 방안에 대해 얘기중이였어"
2. "wiki/08-worklog로 진행하고 hook이랑 git init도 같이 해줘 … 워크로그 작성은 스킬형태로 만들어서 내가 세션 종료하기전에 한번씩 진행할께"
3. "이번 세션 worklog 작성해줘"

## 작업 내용

- 옮긴 폴더의 상태를 확인했다. `wiki/` 00~07 섹션과 ADR 0001~0006은 남아 있었지만 git 저장소가 아니었다.
- Worklog 체계를 제안했고, 사용자가 확정했다.
  - 위치: `wiki/08-worklog/` (기존 규칙 "문서는 모두 wiki 안"을 따름)
  - 정리본: `YYYY-MM/YYYY-MM-DD-NN-title.md`. 세션 종료 전 사용자가 `/worklog` 스킬로 작성한다.
  - 원문: `UserPromptSubmit` hook이 `raw/YYYY-MM-DD.md`에 자동으로 기록한다.
- worklog README, 템플릿, 이전 세션 복원 로그(WL-2026-09-27-01)를 작성했다.
- 프롬프트 기록 hook(Node.js)을 만들고, 가짜 입력으로 테스트했다. 코드블록이 섞인 프롬프트도 fence가 깨지지 않았다.
- `/worklog` 스킬을 만들었다. 사용자가 직접 입력할 때만 실행되도록 `disable-model-invocation` 을 설정했다.
- hook 설치 전 프롬프트 두 개를 raw 로그에 수동으로 옮겨 적었다.
- `wiki/README.md`에 08 섹션과 작성 규칙을 추가했다.
- `.gitignore`를 만들고, `git init` 한 뒤 `origin/main`에 첫 커밋 `a7214b1`을 push했다.

### 변경 파일

| 파일 | 변경 | 설명 |
|---|---|---|
| `wiki/08-worklog/README.md` | 추가 | worklog 규칙, 폴더 구성, 로그 목록 |
| `wiki/08-worklog/_template.md` | 추가 | 세션 로그 템플릿 |
| `wiki/08-worklog/2026-09/2026-09-27-01-wiki-structure.md` | 추가 | 이전 세션 사후 복원 로그 |
| `wiki/08-worklog/2026-09/2026-09-27-02-worklog-convention.md` | 추가 | 이 문서 |
| `wiki/08-worklog/raw/2026-09-27.md` | 추가 | 프롬프트 원문 (백필 + hook 자동 기록) |
| `wiki/README.md` | 수정 | 08 섹션, worklog 작성 규칙, 변경 이력 |
| `.claude/settings.json` | 추가 | `UserPromptSubmit` hook 등록 |
| `.claude/hooks/log-prompt.js` | 추가 | 프롬프트 원문 기록 스크립트 |
| `.claude/skills/worklog/SKILL.md` | 추가 | `/worklog` 스킬 |
| `.gitignore` | 추가 | .NET, IDE, 시크릿, 개인 Claude 설정 제외 |

## 결정 사항

| 결정 | 이유 | ADR |
|---|---|---|
| worklog 위치를 `wiki/08-worklog/`로 둔다 | 기존 규칙 "위키 문서는 모두 `wiki/` 안에서 관리"와 일관성 | - |
| 정리본은 사용자가 `/worklog`로 직접 작성한다 | 과제 증빙이라 기록 시점을 사용자가 통제 | - |
| 프롬프트 원문은 hook으로 자동 수집한다 | 누락 없이 증빙 확보. 정리본은 요약, 원문은 근거 | - |
| GitHub private 저장소(`thkim-ezabele/task_20260926`)를 사용한다 | 드라이브 고장 같은 유실 재발 방지 | - |

> ADR 후보: 없음. 위 결정은 작업 방식에 관한 것이라 worklog README에 기록하는 것으로 충분하다고 판단했다.

## 이슈 / 배운 점

- 이전 세션은 git 저장소가 없어서 세션 기록을 모두 잃었다. 산출물(`wiki/`)만 남아서 WL-01은 사후 복원으로 작성했다.
- 이 PC에서 `python`은 Windows 스토어 스텁이라 실행되지 않는다. 스크립트는 Node.js로 작성한다.
- `.claude/`를 세션 중에 새로 만들었지만, hook은 재시작 없이 바로 적용됐다.

## 다음 할 일

- [ ] 커밋 작성자 이메일 확인 (현재 전역 설정 `taehoon365@gmail.com`)
- [ ] 프로젝트 개요의 In Scope / Out of Scope, 이해관계자 채우기 (`00-overview/project-overview.md`)
- [ ] Git 워크플로우(브랜치 전략, 커밋 규칙) 작성 (`04-development/git-workflow.md`)
- [ ] 도메인 정의(유비쿼터스 언어, 바운디드 컨텍스트) 착수
