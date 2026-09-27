---
title: "Git 워크플로우 (Git Flow)"
type: doc
status: draft
tags: [development]
aliases: [Git Flow, 브랜치 전략]
created: 2026-09-27
updated: 2026-09-27
---

# Git 워크플로우 (Git Flow)

> Git Flow 기반 브랜치 전략, 커밋 메시지, PR, 버전 태깅 규칙을 정의합니다.
> 도구는 Git for Windows에 포함된 `git flow`(AVH Edition 1.12.3)를 사용합니다.
>
> [위키 홈](../README.md)

## 브랜치 전략

| 브랜치 | 수명 | 분기 원점 | 병합 대상 | 용도 |
|---|---|---|---|---|
| `main` | 영구 | - | - | 릴리스된 코드만 둔다. 모든 커밋은 태그(`vX.Y.Z`)가 붙은 릴리스 시점이다. |
| `develop` | 영구 | `main` | - | 다음 릴리스를 통합하는 기본 개발 브랜치 |
| `feature/*` | 임시 | `develop` | `develop` | 기능 개발, 문서 작업 |
| `bugfix/*` | 임시 | `develop` | `develop` | 아직 릴리스되지 않은 버그 수정 |
| `release/*` | 임시 | `develop` | `main` + `develop` | 릴리스 준비(버전, 변경 이력, 최종 수정만) |
| `hotfix/*` | 임시 | `main` | `main` + `develop` | 릴리스된 버전의 긴급 수정 |

```mermaid
gitGraph
    commit id: "v0.1.0" tag: "v0.1.0"
    branch develop
    checkout develop
    commit
    branch feature/employee-api
    checkout feature/employee-api
    commit
    commit
    checkout develop
    merge feature/employee-api
    branch release/0.2.0
    checkout release/0.2.0
    commit id: "bump version"
    checkout main
    merge release/0.2.0 tag: "v0.2.0"
    checkout develop
    merge release/0.2.0
    checkout main
    branch hotfix/0.2.1
    commit id: "fix"
    checkout main
    merge hotfix/0.2.1 tag: "v0.2.1"
    checkout develop
    merge hotfix/0.2.1
```

### 로컬 설정 (`git flow config`)

| 항목 | 값 |
|---|---|
| Production 브랜치 | `main` |
| Development 브랜치 | `develop` |
| Prefix | `feature/`, `bugfix/`, `release/`, `hotfix/`, `support/` |
| 버전 태그 prefix | `v` |

`git flow` 설정은 `.git/config`에 저장되어 저장소를 새로 clone하면 사라집니다. 새 환경에서는 다음을 실행합니다.

```bash
git checkout develop        # 원격 develop 추적 브랜치 생성
git checkout main
git flow init -d            # 기본값 사용
git config gitflow.prefix.versiontag v
```

### 기본 명령

```bash
git flow feature start <name>      # develop에서 feature/<name> 생성
git flow feature finish <name>     # develop에 병합 후 브랜치 삭제 (PR을 쓰면 GitHub에서 병합)
git flow release start 0.2.0
git flow release finish 0.2.0      # main/develop 병합 + 태그 v0.2.0
git flow hotfix start 0.2.1
git flow hotfix finish 0.2.1
```

## 브랜치 네이밍

- 형식: `<type>/<짧은-설명>`, `kebab-case`, 영문 소문자
- 스프린트 / 백로그 항목 ID가 생기면 앞에 붙인다: `feature/<ID>-<설명>` (ID 체계는 [개발 흐름](#issue--project-관리) 설계 후 확정)
- 버전 브랜치는 SemVer 숫자만 쓴다: `release/0.2.0`, `hotfix/0.2.1`

| 예 | 설명 |
|---|---|
| `feature/employee-aggregate` | Employee Aggregate 구현 |
| `feature/wiki-git-workflow` | 문서 작업도 feature로 |
| `bugfix/notification-retry` | 미릴리스 버그 수정 |
| `release/0.1.0` | 첫 릴리스 준비 |
| `hotfix/0.1.1` | 릴리스 긴급 수정 |

## 커밋 메시지 규칙 (Conventional Commits)

```
<type>(<scope>): <제목>

<본문: 무엇을, 왜>

<footer: BREAKING CHANGE, Refs 등>
```

- 제목은 한국어로, 명령형 / 명사형으로 짧게(50자 안팎) 쓰고 끝에 마침표를 찍지 않는다.
- `scope`는 서비스명(`employee`, `notification` 등)이나 영역(`wiki`, `worklog`, `adr`, `ci`)을 쓴다.
- 호환성을 깨는 변경은 `type!:` 또는 footer의 `BREAKING CHANGE:`로 표시한다.

| type | 용도 |
|---|---|
| `feat` | 기능 추가 |
| `fix` | 버그 수정 |
| `docs` | 문서 (위키, worklog, ADR) |
| `test` | 테스트 추가 / 수정 |
| `refactor` | 동작 변경 없는 구조 개선 |
| `perf` | 성능 개선 |
| `build` | 빌드, 의존성 |
| `ci` | CI 설정 |
| `chore` | 그 밖의 잡무 |

## Pull Request 규칙 & 템플릿

- `feature/*`, `bugfix/*`는 `develop`으로 PR을 연다. `release/*`, `hotfix/*`는 `main`으로 PR을 열고, 병합 후 `develop`에 역병합한다.
- 병합 방식
  - `feature/*`, `bugfix/*` → `develop`: **Squash merge** (PR 제목을 Conventional Commits 형식으로)
  - `release/*`, `hotfix/*` → `main`: **Merge commit** (`--no-ff`, Git Flow 이력 보존)
- PR은 테스트 통과가 필수다([ADR-0006](../03-architecture/adr/0006-adopt-tdd.md)). CI 구성 후 필수 체크로 건다.
- 템플릿: 저장소 루트의 [`.github/pull_request_template.md`](../../.github/pull_request_template.md)

> 🟡 1인 과제 기준으로 리뷰어 승인은 필수로 두지 않습니다.

### GitHub 저장소 설정

| 항목 | 값 |
|---|---|
| 기본 브랜치 | `develop` (PR 기본 대상) |
| 병합 방식 | Squash merge, Merge commit 허용 / Rebase merge 끔 |
| 병합 후 브랜치 삭제 | 자동 |
| 브랜치 보호 (`main`, `develop`) | ⚪ 적용 불가: GitHub Free의 private 저장소는 브랜치 보호 / Ruleset을 지원하지 않음(HTTP 403) |

브랜치 보호가 없으므로 **`main` / `develop`에 직접 push하지 않는 것은 규칙으로 지킵니다.** GitHub Pro로 올리거나 저장소를 public으로 바꾸면 보호 규칙(PR 필수, force push / 삭제 금지)을 적용합니다.

## 코드 리뷰 가이드

> TODO: 코드가 생기면 체크리스트(레이어 의존 규칙, 테스트 유무, 도메인 용어 일치 등)를 정합니다.

## Issue / Project 관리

> TODO: PRD → 스프린트 → 개발 흐름의 세부 설계(백로그, 기술부채 기록 방식)와 함께 정합니다. 스프린트와 `release/*`의 대응 관계도 이때 정합니다.

## 버전 태깅 & 릴리스

- [SemVer](https://semver.org/lang/ko/) `vMAJOR.MINOR.PATCH`를 쓴다. 첫 기능 릴리스 전까지는 `0.x.y`로 둔다.
  - `MINOR`: 기능 추가 (release)
  - `PATCH`: 버그 수정 (hotfix)
- 태그는 `git flow release finish` / `hotfix finish`가 `main`에 만드는 annotated tag를 쓴다.
- 서비스별 독립 버전이 필요해지면(MSA) 태그 형식을 다시 정한다.

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-27 | - | GitHub Flow → Git Flow로 변경, 브랜치 전략·네이밍·커밋·PR·태깅 규칙 초안 작성 |
| 2026-09-27 | - | GitHub 저장소 설정 기록, 브랜치 보호 적용 불가(Free private)로 직접 push 금지는 규칙으로 운영 |
