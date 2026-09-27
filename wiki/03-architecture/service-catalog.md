---
title: "MSA 서비스 카탈로그"
type: doc
status: draft
tags: [architecture]
created: 2026-09-27
updated: 2026-09-27
---

# MSA 서비스 카탈로그

> 마이크로서비스 목록과 각 서비스의 책임, 소유 데이터, 의존 관계를 정의합니다.
>
> [위키 홈](../README.md)

## 서비스 분리 원칙

> TODO: 바운디드 컨텍스트 기준 분리, Database per Service 등

## 서비스 목록 (후보)

| 서비스 | 책임 | 소유 데이터 | 상태 |
|---|---|---|---|
| API Gateway | 외부 요청 라우팅, 인증 토큰 검증 | - | 🟡 검토 중 |
| Identity Service | 인증 / 인가, 사용자 계정 | 계정, 역할 | 🟡 검토 중 |
| Employee Service | 직원 / 조직 / 연락처 관리 | 직원, 부서, 연락처 | 🟡 검토 중 |
| Contact Network Service | 연락망 그룹, 전파 순서 구성 | 연락망, 전파 트리 | 🟡 검토 중 |
| Emergency Service | 긴급 상황 등록, 전파, 응답 집계 | 긴급 상황, 응답 | 🟡 검토 중 |
| Notification Service | 채널별 알림 발송 및 이력 관리 | 발송 이력, 템플릿 | 🟡 검토 중 |

## 서비스 상세

> TODO: 서비스별 책임, API, 발행/구독 이벤트, 의존 서비스

## 서비스 의존 관계도

> TODO: Mermaid 다이어그램

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
