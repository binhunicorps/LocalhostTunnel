# Webhook Forwarder — Web UI (No Build Tools Required)

## Goal

Chuyển đổi kiến trúc từ **Tauri (Rust)** sang **Web UI thuần Node.js** để:

- **Không cần** VS Build Tools, Rust, hay bất kỳ build tool hệ thống nào
- **Bundled** toàn bộ runtime trong `libs/` — user chỉ cần chạy `start.bat`
- Giữ nguyên **toàn bộ yêu cầu UI**: Dashboard, Settings, Log Viewer
- Giữ nguyên forwarder engine (`src/`) không thay đổi

## Kiến trúc mới

```
start.bat → libs/node/node.exe server.js
                ├── Express static server (serve React build)
                ├── WebSocket server (real-time logs)
                ├── REST API (config CRUD, start/stop forwarder)
                └── Spawn child process (src/index.js — forwarder engine)

Browser mở http://localhost:3000 → hiển thị UI
```

| Layer | Technology | Vai trò |
|-------|-----------|---------|
| **UI** | React 19 + Vite (build → static) | Giao diện web |
| **API Server** | Express + ws | Serve UI + REST API + WebSocket |
| **Forwarder** | Node.js child process | Engine chuyển tiếp webhook (unchanged) |
| **Runtime** | `libs/node/node.exe` (portable) | Không cần cài Node.js |

## Yêu cầu đã xác nhận

| # | Yêu cầu | Giải pháp |
|---|---------|-----------|
| 1 | Giao diện UI hiện đại, chuyên nghiệp | React + Dark Industrial Monitor theme (đã code) |
| 2 | Settings: Cloudflare Tunnel, localhost/API, bảo mật | Settings page với form 3 sections (đã code) |
| 3 | Theo dõi log real-time | WebSocket stream logs từ child process → browser |
| 4 | Bundled libs — user không cần cài đặt | `libs/node/node.exe` + `node_modules/` đi kèm |
| 5 | Nhẹ, hiệu năng cao | Không Chromium/WebView, dùng browser có sẵn |
| 6 | Không cần VS Build Tools | Pure Node.js, không Rust/C++ compilation |

## File Structure

```
WebhookForwarder/
├── libs/
│   └── node/
│       └── node.exe              # Portable Node.js v22 (standalone)
├── ui/                           # React frontend (Vite project)
│   ├── src/
│   │   ├── App.tsx / App.css
│   │   ├── main.tsx / index.css  # Design tokens (Dark Industrial Monitor)
│   │   ├── pages/
│   │   │   ├── Dashboard.tsx     # ← GIỮ NGUYÊN từ Tauri
│   │   │   ├── Settings.tsx      # ← GIỮ NGUYÊN
│   │   │   └── LogViewer.tsx     # ← CHỈNH: Tauri events → WebSocket
│   │   ├── components/
│   │   │   ├── Sidebar.tsx       # ← GIỮ NGUYÊN
│   │   │   └── TitleBar.tsx      # ← CHỈNH: bỏ Tauri window API
│   │   ├── hooks/
│   │   │   ├── useForwarder.ts   # ← CHỈNH: Tauri invoke → fetch()
│   │   │   ├── useLogs.ts       # ← CHỈNH: Tauri events → WebSocket
│   │   │   └── useConfig.ts     # ← CHỈNH: Tauri invoke → fetch()
│   │   └── lib/
│   │       ├── api.ts            # [NEW] fetch() wrappers thay tauri.ts
│   │       └── types.ts          # ← GIỮ NGUYÊN
│   ├── dist/                     # Vite build output (static files)
│   ├── package.json
│   └── vite.config.ts
├── server/                       # [NEW] API + WebSocket server
│   ├── index.js                  # Express app entry
│   ├── api.js                    # REST routes: /api/config, /api/forwarder
│   ├── forwarder-manager.js      # Manage child process (Node.js version)
│   └── config-store.js           # Read/write config.json
├── src/                          # Forwarder engine (KHÔNG THAY ĐỔI)
│   ├── index.js
│   ├── server.js, forwarder.js
│   ├── auth.js, logger.js, config.js
├── scripts/
│   └── setup-libs.ps1            # Download node.exe (dev setup)
├── start.bat                     # [NEW] 1-click launcher
├── package.json
└── .gitignore
```

## Thay đổi so với Tauri plan

| Component | Tauri (cũ) | Web UI (mới) |
|-----------|-----------|-------------|
| Desktop window | Tauri WebView | Browser tab |
| IPC | `@tauri-apps/api` invoke | `fetch()` REST API |
| Log streaming | Tauri events | WebSocket |
| Config store | Rust `config_store.rs` | Node.js `config-store.js` |
| Process manager | Rust `forwarder_manager.rs` | Node.js `forwarder-manager.js` |
| System tray | Tauri tray | Không (terminal background) |
| Build tools | Rust + MSVC + VS Build Tools | ❌ Không cần |
| Title bar | Custom (window controls) | Browser-style header (branding only) |

## Tasks

### Phase 1: Backend Server (NEW)

- [ ] **T1: `server/config-store.js`** — Port logic từ Rust sang Node.js: đọc/ghi `%APPDATA%/webhook-forwarder/config.json`
  → Verify: Unit test load/save config

- [ ] **T2: `server/forwarder-manager.js`** — Port logic từ Rust: spawn/kill child process, capture stdout/stderr
  → Verify: start → running, stop → stopped, logs captured

- [ ] **T3: `server/api.js`** — Express REST routes:
  - `GET /api/status` — forwarder status
  - `POST /api/start` — start forwarder
  - `POST /api/stop` — stop forwarder
  - `GET /api/config` — read config
  - `PUT /api/config` — save config
  - WebSocket `/ws/logs` — real-time log stream
  → Verify: curl test mỗi endpoint

- [ ] **T4: `server/index.js`** — Express app: serve `ui/dist/` + mount API + WebSocket + auto-open browser
  → Verify: `node server/index.js` → browser mở, UI hiển thị

### Phase 2: Frontend Adaptation

- [ ] **T5: `ui/src/lib/api.ts`** — Thay `tauri.ts` bằng fetch wrappers
  → Verify: TypeScript compile OK

- [ ] **T6: Update hooks** — `useForwarder.ts`, `useConfig.ts` → dùng `api.ts`; `useLogs.ts` → WebSocket
  → Verify: Hooks compile OK

- [ ] **T7: Update TitleBar** — Bỏ Tauri window controls, giữ branding header
  → Verify: Render đúng

- [ ] **T8: Vite build** — `npm run build` tạo `ui/dist/`
  → Verify: Build thành công, static files tạo đúng

### Phase 3: Launcher & Bundle

- [ ] **T9: `start.bat`** — Script 1-click: chạy `libs/node/node.exe server/index.js`
  → Verify: Double-click → app chạy, browser mở

- [ ] **T10: `scripts/setup-libs.ps1`** — Download `node.exe` + `npm install`
  → Verify: Chạy trên máy sạch → libs ready (đã có)

### Phase 4: Polish

- [ ] **T11: Error handling** — Server crash recovery, reconnect WebSocket, UI error states
  → Verify: Kill forwarder → UI hiển thị error, reconnect tự động

- [ ] **T12: Graceful shutdown** — Ctrl+C → stop forwarder child → close server
  → Verify: Ctrl+C không để orphan process

### Phase X: Verification

- [ ] `libs/node/node.exe server/index.js` chạy thành công
- [ ] Browser mở http://localhost:3000 → Dashboard hiển thị
- [ ] Start forwarder → status "Running", logs stream qua WebSocket
- [ ] Settings thay đổi → save → restart forwarder apply mới
- [ ] Log viewer: filter theo level, search text, auto-scroll
- [ ] `start.bat` double-click → hoạt động end-to-end
- [ ] Ctrl+C → shutdown sạch (không orphan process)
- [ ] UI: Dark Industrial Monitor theme, sharp edges, animations

### Verification Commands

```bash
# 1. Build UI
cd ui && npm run build

# 2. Run full app
cd .. && libs\node\node.exe server/index.js

# 3. Test API endpoints (PowerShell)
Invoke-RestMethod http://localhost:3000/api/status
Invoke-RestMethod -Method Post http://localhost:3000/api/start
Invoke-RestMethod http://localhost:3000/api/config

# 4. Test forwarder (khi đang running)
curl -X POST http://localhost:8788/ -H "x-webhook-secret: your-secret" -d '{"test":true}'
# → Log xuất hiện trên UI Log Viewer
```

## Agent Assignments

| Phase | Agent | Skills |
|-------|-------|--------|
| Phase 1 | `backend-specialist` | `nodejs-best-practices`, `clean-code` |
| Phase 2 | `frontend-specialist` | `frontend-design` |
| Phase 3 | `backend-specialist` | `clean-code` |
| Phase 4 | `backend-specialist` | `clean-code` |

## Notes

- **Giữ nguyên 90% React code** — chỉ đổi transport layer (Tauri IPC → fetch/WebSocket)
- **Xóa thư mục `ui/src-tauri/`** — không cần Rust nữa
- Node.js forwarder (`src/`) **KHÔNG thay đổi**
- Server port mặc định: `3000` (UI), forwarder port giữ nguyên config
- `libs/node/node.exe` đã download sẵn (v22.15.0, 80.5MB)
