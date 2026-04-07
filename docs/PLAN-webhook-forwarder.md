# Webhook Forwarder + Cloudflare Tunnel

## Goal

Xây dịch vụ trung gian (Node.js) listen tại `127.0.0.1:8788`, nhận webhook từ Internet qua Cloudflare Tunnel, giữ nguyên raw body/method/path/query/headers rồi forward sang app local tại `127.0.0.1:8765`, trả ngược response cho bên gửi.

## Project Type

**BACKEND** — Node.js HTTP proxy service, không có UI.

## Success Criteria

- [ ] Forwarder chạy ổn định tại `127.0.0.1:8788`
- [ ] Forward giữ nguyên method, path, query string, raw body, headers quan trọng
- [ ] Shared secret check (`x-webhook-secret`) → trả `401` nếu sai
- [ ] App local down → trả `502 Bad Gateway`
- [ ] Body quá lớn → bị reject
- [ ] Timeout upstream → trả lỗi có kiểm soát
- [ ] Log đầy đủ: timestamp, method, path, status, latency, request-id
- [ ] Healthcheck endpoint `/health` hoạt động
- [ ] File cấu hình Cloudflare Tunnel mẫu sẵn sàng
- [ ] App có thể chạy như Windows service / PM2

## Tech Stack

| Technology | Rationale |
|------------|-----------|
| **Node.js 18+** | Async I/O phù hợp proxy, ecosystem phong phú |
| **Vanilla `http` module** | Nhẹ, không parse body tự động, giữ raw body dễ dàng |
| **undici / node-fetch** | HTTP client gọi upstream nhanh, hỗ trợ streaming |
| **dotenv** | Load cấu hình từ `.env` |
| **uuid** | Generate request-id |
| **pino** | Structured JSON logger, nhanh |

> Không dùng Express/Fastify để tránh body parsing tự động — đảm bảo raw body cho webhook signature verification.

## File Structure

```
WebhookForwarder/
├── src/
│   ├── index.js          # Entry point, start server
│   ├── server.js         # HTTP server, routing
│   ├── forwarder.js      # Core forwarding logic
│   ├── auth.js           # Shared secret verification
│   ├── logger.js         # Pino logger setup
│   └── config.js         # Config loader (.env)
├── cloudflare/
│   └── config.yml        # Cloudflare Tunnel config template
├── .env.example          # Environment variables template
├── package.json
├── README.md
└── ecosystem.config.js   # PM2 config (optional)
```

## Tasks

### Phase 1: Foundation — Project Setup

- [ ] **T1: Init project** — `npm init`, cài dependencies (`dotenv`, `pino`, `uuid`)
  → Verify: `node -e "require('dotenv'); require('pino'); require('uuid');"` không lỗi

- [ ] **T2: Config module** — Tạo `src/config.js` load env vars (`PORT`, `TARGET_HOST`, `TARGET_PORT`, `WEBHOOK_SECRET`, `MAX_BODY_SIZE`, `UPSTREAM_TIMEOUT`)
  → Verify: `node -e "require('./src/config')"` in ra config object

- [ ] **T3: Logger module** — Tạo `src/logger.js` dùng pino, log level từ env
  → Verify: `node -e "const log = require('./src/logger'); log.info('test');"` in ra JSON log

### Phase 2: Core — Forwarding Logic

- [ ] **T4: Auth module** — Tạo `src/auth.js`, verify `x-webhook-secret` header
  → Verify: Unit scenario — secret đúng → `true`, sai → `false`, không set secret → bypass

- [ ] **T5: Forwarder module** — Tạo `src/forwarder.js`:
  - Nhận raw body (Buffer)
  - Build request tới `http://TARGET_HOST:TARGET_PORT` giữ nguyên method, path, query
  - Forward headers (loại bỏ `host`, `content-length`, `connection`)
  - Thêm `x-forwarded-by`, `x-forwarded-host`, `x-forwarded-proto`
  - Timeout xử lý
  - Trả status code + body + headers từ upstream
  → Verify: Mock upstream server, gọi forwarder, kiểm body/headers/status trả về đúng

- [ ] **T6: Server module** — Tạo `src/server.js`:
  - HTTP server listen `0.0.0.0:PORT` (mặc định `8788`)
  - Route `/health` → `200 OK`
  - Mọi route khác → auth check → body size check → forward
  - Collect raw body bằng cách buffer chunks
  - Generate request-id (uuid)
  - Log request: method, path, status, latency, request-id
  - Error handling: upstream lỗi → `502`, timeout → `504`
  → Verify: `curl http://127.0.0.1:8788/health` trả `200`

- [ ] **T7: Entry point** — Tạo `src/index.js`, import server, start listen
  → Verify: `node src/index.js` bật server, curl `/health` trả `200`

### Phase 3: Configuration & Docs

- [ ] **T8: .env.example** — Tạo file mẫu với tất cả env vars + comment
  → Verify: File tồn tại với đầy đủ vars

- [ ] **T9: Cloudflare config** — Tạo `cloudflare/config.yml` mẫu với ingress rule
  → Verify: File có hostname mapping + catch-all 404

- [ ] **T10: PM2 config** — Tạo `ecosystem.config.js`
  → Verify: File hợp lệ, `pm2 start ecosystem.config.js` khả thi

- [ ] **T11: README.md** — Hướng dẫn cài đặt, cấu hình, chạy, test, troubleshooting
  → Verify: File rõ ràng, đầy đủ sections

- [ ] **T12: package.json scripts** — Thêm `start`, `dev` scripts
  → Verify: `npm start` chạy server

### Phase 4: End-to-End Testing

- [ ] **T13: Test script** — Viết script test cơ bản (bash/PowerShell):
  - Start mock upstream ở `8765`
  - Start forwarder ở `8788`
  - Gửi các request kiểm tra:
    - `GET /test?foo=bar` → forward đúng path + query
    - `POST /webhook` với JSON body → body giữ nguyên
    - `POST /webhook` với raw body → body giữ nguyên
    - Request không có secret → `401`
    - Body quá lớn → `413`
    - Upstream down → `502`
    - `/health` → `200`
  → Verify: Tất cả assertions pass

### Phase X: Verification

- [ ] Server chạy ổn định tại `127.0.0.1:8788`
- [ ] Forward giữ nguyên raw body (binary-safe)
- [ ] Method, path, query string forward chính xác
- [ ] Headers quan trọng được forward, hop-by-hop bị loại
- [ ] `x-webhook-secret` sai → `401`
- [ ] Không set secret trong env → bypass auth (tùy chọn)
- [ ] Body > MAX_BODY_SIZE → reject
- [ ] Upstream timeout → lỗi có kiểm soát
- [ ] Upstream down → `502`
- [ ] `/health` → `200`
- [ ] Log có đủ: timestamp, method, path, status, latency, request-id
- [ ] `npm start` chạy được
- [ ] Security scan: `python .agent/skills/vulnerability-scanner/scripts/security_scan.py .`

## Agent Assignments

| Phase | Agent | Skills |
|-------|-------|--------|
| Phase 1-2 | `backend-specialist` | `nodejs-best-practices`, `clean-code` |
| Phase 3 | `backend-specialist` | `documentation-templates` |
| Phase 4 | `backend-specialist` | `testing-patterns` |
| Phase X | `security-auditor` | `vulnerability-scanner` |

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| JSON parse phá signature | Dùng raw body buffer, KHÔNG parse |
| App local crash | Timeout + `502` response + log chi tiết |
| Tunnel disconnect | `cloudflared` chạy service mode, auto-restart |
| Spam webhook | Shared secret + body size limit |

## Notes

- Forwarder bind `127.0.0.1` (chỉ local) hoặc `0.0.0.0` tùy setup; mặc định nên là `127.0.0.1` vì Cloudflare Tunnel kết nối local
- App local KHÔNG thay đổi gì — vẫn listen `127.0.0.1:8765`
- Cloudflare Tunnel config là **mẫu** — user cần tự tạo tunnel thật và điền tunnel ID
