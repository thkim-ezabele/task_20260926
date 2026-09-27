---
title: "사전 준비 사항"
type: doc
status: draft
tags: [getting-started]
created: 2026-09-27
updated: 2026-09-27
---

# 사전 준비 사항

> 개발에 필요한 도구와 버전을 안내합니다. 기준 환경은 Windows 11입니다. 저장소를 받은 뒤의 실행 방법은 [로컬 개발 환경 구성](local-setup.md)에 있습니다.
>
> [위키 홈](../README.md)

## 요약

| 도구 | 버전 / 비고 | 확인 명령 |
|---|---|---|
| .NET SDK | 8.0.x (현재 개발 환경 8.0.202) | `dotnet --list-sdks` |
| Docker Desktop | Testcontainers, 로컬 PostgreSQL | `docker version` |
| Git for Windows | git-flow(AVH Edition 1.12.3) 포함 | `git flow version` |
| GitHub CLI | 로그인 필요 | `gh auth status` |
| Node.js | 프로젝트 스크립트(hook 등) 실행 | `node -v` |
| Claude Code | 스킬(`/prd`, `/sprint`, `/retro`, `/worklog`) · 에이전트 실행 | `claude --version` |

## .NET 8 SDK

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)를 설치합니다. 모든 서비스의 Target Framework는 `net8.0`입니다([ADR-0001](../03-architecture/adr/0001-use-dotnet8.md)).
- SDK 버전은 저장소 루트의 `global.json`으로 고정합니다(기반 구축 토픽에서 추가).
- EF Core 마이그레이션을 만들거나 적용하려면 `dotnet-ef` 도구가 필요합니다.

```bash
dotnet tool install --global dotnet-ef
dotnet ef --version
```

## IDE (Visual Studio 2022 / Rider / VS Code)

| IDE | 비고 |
|---|---|
| Visual Studio 2022 (17.8 이상) | .NET 8 / C# 12 지원 버전 |
| JetBrains Rider (2023.3 이상) | .NET 8 / C# 12 지원 버전 |
| VS Code + C# Dev Kit | 가벼운 편집 · 위키 작업 |

- 저장소의 `.editorconfig`를 따르도록 IDE의 EditorConfig 지원을 켭니다(코드 스타일은 빌드에서도 검사).
- 위키는 Obsidian으로 `wiki/` 폴더를 vault로 열어 볼 수 있습니다(선택).

## Docker Desktop

- 통합 테스트는 **Testcontainers로 실제 PostgreSQL**을 띄우므로 Docker가 반드시 실행 중이어야 합니다([테스트 전략](../04-development/testing-strategy.md#통합-테스트-testcontainers)).
- 로컬 PostgreSQL · 메시지 브로커는 docker compose로 띄웁니다(구성은 [컨테이너 & 로컬 인프라](../06-deployment/containers.md), 기반 구축 토픽에서 작성).
- Windows에서는 WSL 2 백엔드를 사용합니다.

## PostgreSQL 클라이언트

- DB 확인용 클라이언트를 하나 설치합니다: `psql`(PostgreSQL 클라이언트 도구), DBeaver, DataGrip, pgAdmin 중 선택
- 서버는 로컬에 설치하지 않고 Docker로 띄웁니다.

## Git / GitHub CLI

- **Git for Windows**를 설치합니다. `git flow`(AVH Edition)가 포함되어 있습니다.
- **GitHub CLI(`gh`)**를 설치하고 로그인합니다: `gh auth login`. 스킬이 PR 생성 · 병합에 `gh`를 씁니다.
- 저장소를 새로 clone하면 `git flow` 설정이 없으므로 다시 설정합니다(`.git/config`에만 저장됨). 자세한 내용은 [Git 워크플로우](../04-development/git-workflow.md#로컬-설정-git-flow-config)를 참고합니다.

```bash
git checkout develop
git checkout main
git flow init -d
git config gitflow.prefix.versiontag v
```

## Node.js

- 프로젝트 스크립트(예: 프롬프트 원문 기록 hook `.claude/hooks/log-prompt.js`)는 **Node.js**로 작성합니다.
- Windows의 `python`은 스토어 스텁이라 실행되지 않으므로 스크립트에 쓰지 않습니다.

## Claude Code

- 개발 흐름은 Claude Code의 스킬(`/prd`, `/sprint`, `/retro`, `/worklog`)과 서브에이전트(`.claude/agents/`)로 진행합니다([에이전트 워크플로우](../10-delivery/agents.md)).
- 프로젝트 설정(`.claude/settings.json`)과 hook은 저장소에 포함되어 있습니다. 개인 설정은 `.claude/settings.local.json`에 두며 커밋하지 않습니다.

## 설치 확인

```bash
dotnet --list-sdks     # 8.0.x 포함
docker version         # Server 정보가 나와야 함 (Docker Desktop 실행 중)
git flow version       # 1.12.3 (AVH Edition)
gh auth status         # Logged in
node -v
```

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-27 | - | 도구별 설치 · 버전 · 확인 명령 작성 (.NET 8 SDK, dotnet-ef, IDE, Docker, PostgreSQL 클라이언트, Git · git-flow · gh, Node.js, Claude Code) |
