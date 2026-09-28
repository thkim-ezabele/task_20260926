#!/usr/bin/env node
// 위키 문서 점검: 상대 링크 대상 존재, 앵커(제목 슬러그), frontmatter 필수 키, wikilink 금지, 표 구조.
// 사용법: node scripts/check-docs.js [파일 또는 디렉터리 ...]
//   인자가 없으면 wiki/ 전체(_templates/, .obsidian/ 제외)를 점검한다.
//   결함이 하나라도 있으면 exit 1, 없으면 exit 0.
// 기준: wiki/README.md "문서 작성 규칙" (Frontmatter 표, Obsidian 규칙). 외부 의존 없음.
'use strict';

const fs = require('node:fs');
const path = require('node:path');

const repoRoot = path.resolve(__dirname, '..');
const wikiRoot = path.join(repoRoot, 'wiki');
const skipDirs = new Set(['.obsidian', '_templates', 'node_modules', '.git']);

const requiredKeys = ['title', 'type', 'tags', 'created', 'updated'];
const allowedTypes = ['doc', 'index', 'adr', 'worklog', 'raw-log', 'memory', 'prd', 'sprint', 'retro'];
const statusRequiredTypes = new Set(['doc', 'index', 'adr', 'prd', 'sprint']);
const datePattern = /^\d{4}-\d{2}-\d{2}$/;

const defects = [];
const report = (file, line, kind, message) =>
  defects.push({ file: path.relative(repoRoot, file).split(path.sep).join('/'), line, kind, message });

function collectFiles(target, out) {
  const stat = fs.statSync(target);
  if (stat.isFile()) {
    if (target.endsWith('.md')) out.push(target);
    return;
  }
  for (const entry of fs.readdirSync(target, { withFileTypes: true })) {
    if (entry.isDirectory() && skipDirs.has(entry.name)) continue;
    collectFiles(path.join(target, entry.name), out);
  }
}

// frontmatter를 떼어 내고, 본문 줄 번호 보정값을 돌려준다.
function splitFrontmatter(lines) {
  if (lines[0] !== '---') return { frontmatter: null, bodyStart: 0 };
  const end = lines.indexOf('---', 1);
  if (end < 0) return { frontmatter: null, bodyStart: 0, unterminated: true };
  return { frontmatter: lines.slice(1, end), bodyStart: end + 1 };
}

function parseFrontmatter(fmLines) {
  const keys = new Map();
  fmLines.forEach((line, i) => {
    const m = /^([A-Za-z_][\w-]*):(.*)$/.exec(line);
    if (m) keys.set(m[1], { value: m[2].trim().replace(/^["']|["']$/g, ''), line: i + 2 });
  });
  return keys;
}

function checkFrontmatter(file, lines) {
  const { frontmatter, unterminated } = splitFrontmatter(lines);
  if (unterminated) return report(file, 1, 'frontmatter', 'frontmatter 종료 구분자(---)가 없습니다');
  if (!frontmatter) return report(file, 1, 'frontmatter', 'frontmatter가 없습니다');
  const keys = parseFrontmatter(frontmatter);
  for (const key of requiredKeys) {
    const entry = keys.get(key);
    if (!entry) report(file, 1, 'frontmatter', `필수 키 '${key}'가 없습니다`);
    else if (entry.value === '' || entry.value === '[]') report(file, entry.line, 'frontmatter', `필수 키 '${key}' 값이 비어 있습니다`);
  }
  const type = keys.get('type');
  if (type && type.value && !allowedTypes.includes(type.value)) {
    report(file, type.line, 'frontmatter', `허용되지 않은 type '${type.value}'`);
  }
  if (type && statusRequiredTypes.has(type.value)) {
    const status = keys.get('status');
    if (!status || status.value === '') report(file, 1, 'frontmatter', `type '${type.value}'에는 'status'가 필요합니다`);
  }
  for (const key of ['created', 'updated']) {
    const entry = keys.get(key);
    if (entry && entry.value && !datePattern.test(entry.value)) {
      report(file, entry.line, 'frontmatter', `'${key}'는 YYYY-MM-DD 형식이어야 합니다 (현재 '${entry.value}')`);
    }
  }
}

// 코드 블록 안 줄은 null, 그 밖의 줄은 인라인 코드를 지운 텍스트로 바꾼다.
function maskCode(lines, bodyStart) {
  const masked = new Array(lines.length).fill(null);
  let fence = null;
  for (let i = bodyStart; i < lines.length; i++) {
    const line = lines[i];
    const fenceMatch = /^\s{0,3}(`{3,}|~{3,})/.exec(line);
    if (fence) {
      if (fenceMatch && fenceMatch[1][0] === fence[0] && fenceMatch[1].length >= fence.length && line.trim() === fenceMatch[1]) fence = null;
      continue;
    }
    if (fenceMatch) {
      fence = fenceMatch[1];
      continue;
    }
    masked[i] = line.replace(/(`+)[\s\S]*?\1/g, '');
  }
  return masked;
}

// GitHub 방식 제목 슬러그: 소문자, 문자 · 숫자 · 공백 · 하이픈 · 밑줄 외 제거, 공백 → 하이픈, 중복은 -1, -2.
function slugify(text) {
  const plain = text
    .replace(/!?\[([^\]]*)\]\([^)]*\)/g, '$1')
    .replace(/<[^>]+>/g, '')
    .trim()
    .toLowerCase();
  return plain.replace(/[^\p{L}\p{M}\p{N}\p{Pc} -]/gu, '').replace(/ /g, '-');
}

const anchorCache = new Map();
function anchorsOf(file) {
  if (anchorCache.has(file)) return anchorCache.get(file);
  const lines = fs.readFileSync(file, 'utf8').replace(/\r\n/g, '\n').split('\n');
  const { bodyStart } = splitFrontmatter(lines);
  const masked = maskCode(lines, bodyStart);
  const anchors = new Set();
  const counts = new Map();
  lines.forEach((raw, i) => {
    if (masked[i] === null) return;
    const heading = /^\s{0,3}#{1,6}\s+(.*?)\s*#*\s*$/.exec(raw);
    if (heading) {
      const base = slugify(heading[1]);
      const n = counts.get(base) ?? 0;
      anchors.add(n === 0 ? base : `${base}-${n}`);
      counts.set(base, n + 1);
    }
    for (const m of raw.matchAll(/<a\s+[^>]*(?:id|name)="([^"]+)"/g)) anchors.add(m[1]);
  });
  anchorCache.set(file, anchors);
  return anchors;
}

function isExternal(target) {
  return /^[a-z][a-z0-9+.-]*:/i.test(target) || target.startsWith('//');
}

function checkTarget(file, lineNo, rawTarget) {
  let target = rawTarget.trim();
  if (target.startsWith('<') && target.endsWith('>')) target = target.slice(1, -1);
  target = target.replace(/\s+["'(].*$/, '');
  if (target === '' || isExternal(target)) return;
  const hashAt = target.indexOf('#');
  const pathPart = hashAt >= 0 ? target.slice(0, hashAt) : target;
  const anchor = hashAt >= 0 ? target.slice(hashAt + 1) : null;
  let decodedPath;
  let decodedAnchor;
  try {
    decodedPath = decodeURIComponent(pathPart);
    decodedAnchor = anchor === null ? null : decodeURIComponent(anchor);
  } catch {
    return report(file, lineNo, 'link', `URL 디코딩 실패: ${rawTarget}`);
  }
  if (path.isAbsolute(decodedPath) || decodedPath.startsWith('/')) {
    return report(file, lineNo, 'link', `절대 경로 링크는 쓰지 않습니다: ${rawTarget}`);
  }
  const resolved = decodedPath === '' ? file : path.resolve(path.dirname(file), decodedPath);
  if (!fs.existsSync(resolved)) return report(file, lineNo, 'link', `대상 없음: ${rawTarget}`);
  if (decodedAnchor !== null && decodedAnchor !== '' && resolved.endsWith('.md') && fs.statSync(resolved).isFile()) {
    if (!anchorsOf(resolved).has(decodedAnchor.toLowerCase()) && !anchorsOf(resolved).has(decodedAnchor)) {
      report(file, lineNo, 'anchor', `앵커 없음: ${rawTarget}`);
    }
  }
}

function checkLinks(file, lines) {
  const { bodyStart } = splitFrontmatter(lines);
  const masked = maskCode(lines, bodyStart);
  masked.forEach((text, i) => {
    if (text === null) return;
    const lineNo = i + 1;
    if (/\[\[[^\]]+\]\]/.test(text)) report(file, lineNo, 'wikilink', 'wikilink([[...]])는 쓰지 않습니다');
    // 인라인 링크 · 이미지: [text](target). 대상 안의 괄호 한 겹까지 허용.
    for (const m of text.matchAll(/!?\[(?:[^\[\]]|\[[^\]]*\])*\]\(((?:[^()\s]|\([^()]*\))+(?:\s+"[^"]*")?)\)/g)) {
      checkTarget(file, lineNo, m[1]);
    }
    // 참조 정의: [id]: target
    const ref = /^\s{0,3}\[[^\]]+\]:\s+(\S+)/.exec(text);
    if (ref) checkTarget(file, lineNo, ref[1]);
  });
}

// 표 구조: 머리글 · 구분 줄 뒤 행의 열 수가 같은지, 머리글 없이 시작하는 표 행(문단이 표를 끊은 경우)이 없는지.
function tableCells(text) {
  return text.trim().replace(/^\|/, '').replace(/(?<!\\)\|$/, '').split(/(?<!\\)\|/).length;
}

function checkTables(file, lines) {
  const { bodyStart } = splitFrontmatter(lines);
  const masked = maskCode(lines, bodyStart);
  const isRow = (i) => i < lines.length && masked[i] !== null && /^\s{0,3}\|/.test(masked[i]);
  const isSeparator = (i) => isRow(i) && /^\s*\|?(\s*:?-{3,}:?\s*\|)+\s*(:?-{3,}:?\s*)?$/.test(masked[i]);
  for (let i = bodyStart; i < lines.length; i++) {
    if (!isRow(i)) continue;
    const start = i;
    while (isRow(i + 1)) i++;
    if (!isSeparator(start + 1)) {
      report(file, start + 1, 'table', '머리글 · 구분 줄 없이 시작하는 표 행입니다(문단이 표를 끊었는지 확인)');
      continue;
    }
    const columns = tableCells(masked[start]);
    for (let r = start + 1; r <= i; r++) {
      const n = tableCells(masked[r]);
      if (n !== columns) report(file, r + 1, 'table', `표 열 수가 머리글(${columns})과 다릅니다(${n})`);
    }
  }
}

function main() {
  const args = process.argv.slice(2);
  const targets = args.length > 0 ? args.map((a) => path.resolve(process.cwd(), a)) : [wikiRoot];
  const files = [];
  for (const target of targets) {
    if (!fs.existsSync(target)) {
      console.error(`경로 없음: ${target}`);
      process.exitCode = 2;
      return;
    }
    collectFiles(target, files);
  }
  for (const file of files) {
    const lines = fs.readFileSync(file, 'utf8').replace(/\r\n/g, '\n').split('\n');
    checkFrontmatter(file, lines);
    checkLinks(file, lines);
    checkTables(file, lines);
  }
  for (const d of defects) console.log(`${d.file}:${d.line}: [${d.kind}] ${d.message}`);
  const byKind = defects.reduce((acc, d) => ({ ...acc, [d.kind]: (acc[d.kind] ?? 0) + 1 }), {});
  const summary = Object.entries(byKind).map(([k, v]) => `${k} ${v}`).join(', ');
  console.log(`\n점검 파일 ${files.length}개, 결함 ${defects.length}건${summary ? ` (${summary})` : ''}`);
  process.exitCode = defects.length > 0 ? 1 : 0;
}

main();
