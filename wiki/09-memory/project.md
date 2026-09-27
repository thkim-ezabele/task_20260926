---
title: "프로젝트 개념"
type: memory
tags: [memory]
created: 2026-09-27
updated: 2026-09-27
---

# 프로젝트 개념

> [장기기억](README.md) · 원본: [프로젝트 개요](../00-overview/project-overview.md), [서비스 카탈로그](../03-architecture/service-catalog.md)

## 한 줄 정의

**Emergency Hub**: 재난이나 사고 같은 긴급 상황에 직원에게 빠르게 전파하고, 직원의 응답(안부)을 수집·집계하는 **직원 긴급연락망 백엔드 시스템**.

## 프로젝트 성격

- **과제형 프로젝트**입니다. 결과물뿐 아니라 요구사항 → 설계 → 개발의 **과정과 근거를 증빙**으로 남기는 것이 중요합니다.
- 백엔드만 다룹니다. 프론트엔드는 범위 밖입니다(확정 전).

## 주요 기능 (초안)

직원 / 조직 관리 · 연락망 구성(전파 순서, Call Tree) · 긴급 상황 전파 · 채널별 알림 발송(SMS / 푸시 / 이메일) · 응답 확인 및 집계 · 인증 / 권한

## 서비스 후보 (🟡 검토 중)

API Gateway · Identity · Employee · Contact Network · Emergency · Notification

## 저장소

- GitHub: `thkim-ezabele/task_20260926` (private, 기본 브랜치 `main`)
- 로컬: `C:\00. src\02. src\emergency-hub` (2026-09-27 드라이브 고장으로 이전)
- 위키: `wiki/` (Obsidian vault)
