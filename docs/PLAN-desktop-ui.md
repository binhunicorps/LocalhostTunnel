# Webhook Forwarder — Desktop UI Upgrade (Tauri)

## Goal

Nâng cấp ứng dụng Webhook Forwarder từ CLI thành ứng dụng Windows desktop có giao diện hiện đại, chuyên nghiệp sử dụng **Tauri v2** (Rust backend + WebView). Giao diện bao gồm:

- **Dashboard** — trạng thái tổng quan, start/stop forwarder
- **Settings** — cấu hình Cloudflare Tunnel, localhost + API, bảo mật
- **Log Viewer** — theo dõi log real-time, filter, search
- **System Tray** — chạy ngầm, bật/tắt nhanh

## Project Type

**WEB** (frontend trong Tauri WebView) + **BACKEND** (Rust sidecar + existing Node.js forwarder)

## Success Criteria

- [ ] App Tauri build thành công cho Windows (`.msi` / `.exe`)
- [ ] Dashboard hiển thị trạng thái forwarder (running/stopped), uptime, request count
- [ ] Start/Stop forwarder từ UI
- [ ] Settings page lưu cấu hình vào file và apply runtime
- [ ] Log viewer hiển thị real-time, auto-scroll, filter theo level
- [ ] System tray hoạt động — minimize to tray, restore
- [ ] UI hiện đại, premium, không giống template
- [ ] App nhẹ < 15MB installed

## Tech Stack

| Technology | Rationale |
|------------|-----------|
| **Tauri v2** | Nhẹ (~5-10MB), hiệu năng cao, native Windows APIs |
| **React 19 + Vite** | Build nhanh, ecosystem UI phong phú, hot reload |
| **TypeScript** | Type safety cho frontend |
| **Vanilla CSS** (CSS Modules) | Full control, no bloat, dark mode native |
| **Node.js child process** | Forwarder engine chạy như subprocess managed bởi Tauri |
| **Tauri IPC** | Giao tiếp giữa frontend ↔ Rust backend ↔ Node forwarder |

> **Kiến trúc:** Tauri (Rust) → spawn bundled `libs/node/node.exe` forwarder subprocess → pipe logs qua IPC → render trên WebView frontend.

## Bundled Libraries (Self-Contained)

Để người dùng cuối **KHÔNG cần cài đặt** bất kỳ thư viện nào, toàn bộ runtime cần thiết được đóng gói trong thư mục `libs/`:

| Library | Path | Purpose | Kích thước |
|---------|------|---------|------------|
| **Node.js** (portable) | `libs/node/node.exe` | Runtime cho forwarder engine | ~75MB |
| **Node modules** | `node_modules/` (root) | Dependencies: pino, dotenv, uuid | ~2MB |

> **Lưu ý quan trọng:**
> - `libs/node/node.exe` là bản standalone, KHÔNG cần cài Node.js vào hệ thống
> - Tauri Rust backend spawn `libs/node/node.exe src/index.js` thay vì gọi `node` từ system PATH
> - Sau khi `npm run tauri build`, thư mục `libs/` + `src/` + `node_modules/` được bundle cùng installer
> - **Build tools (Rust, VS Build Tools)** chỉ cần cho developer — user cuối KHÔNG cần

## File Structure

```
WebhookForwarder/
├── libs/                        # 📦 Bundled runtime libraries
│   └── node/
│       └── node.exe             # Portable Node.js (standalone)
├── ui/                          # Tauri + React frontend
│   ├── src-tauri/               # Tauri Rust backend
│   │   ├── src/
│   │   │   ├── lib.rs           # Entry, setup, state management
│   │   │   ├── main.rs          # Binary entry point
│   │   │   ├── commands.rs      # IPC commands
│   │   │   ├── forwarder_manager.rs # Manage Node child process
│   │   │   └── config_store.rs  # Settings persistence
│   │   ├── Cargo.toml
│   │   └── tauri.conf.json
│   ├── src/                     # React UI source
│   │   ├── App.tsx / App.css
│   │   ├── main.tsx / index.css  # Design tokens
│   │   ├── pages/               # Dashboard, Settings, LogViewer
│   │   ├── components/          # TitleBar, Sidebar
│   │   ├── hooks/               # useForwarder, useLogs, useConfig
│   │   └── lib/                 # types.ts, tauri.ts
│   └── package.json
├── src/                         # Node.js forwarder engine (unchanged)
│   ├── index.js, server.js, forwarder.js
│   ├── auth.js, logger.js, config.js
├── node_modules/                # Forwarder dependencies (pino, dotenv, uuid)
├── package.json
└── README.md
```

## Design Concept

### 🎨 DESIGN COMMITMENT: DARK INDUSTRIAL MONITOR

- **Geometry:** Sharp edges (border-radius: 2px), crisp lines — industrial/technical feel
- **Palette:**
  - Background: `#0D0F12` (near-black), Surface: `#161A1F`, `#1C2128`
  - Accent: `#00D4AA` (teal-mint — active/running), `#FF6B35` (signal orange — errors/warnings)
  - Text: `#E6EDF3` (primary), `#8B949E` (secondary)
  - NO purple/violet
- **Typography:** `JetBrains Mono` cho logs, `Inter` cho UI text
- **Layout:** Fixed sidebar navigation + content area, compact dashboard cards
- **Effects:** Subtle glow on active elements, smooth 200ms transitions, status dot pulse animation
- **Feel:** Giống terminal monitoring tool — chuyên nghiệp, kỹ thuật, tin cậy

## Tasks

### Phase 1: Tauri Project Scaffolding

- [ ] **T1: Init Tauri + React** — `npm create tauri-app` trong thư mục `ui/`, cấu hình Vite + React + TypeScript
  → Verify: `npm run tauri dev` mở window trắng

- [ ] **T2: Cấu trúc thư mục** — Tạo folders `pages/`, `components/`, `hooks/`, `lib/`
  → Verify: Import test không lỗi

- [ ] **T3: Custom TitleBar** — Ẩn native title bar, tạo custom TitleBar component (drag, minimize, maximize, close)
  → Verify: Window có title bar custom, kéo được

### Phase 2: Design System & Layout

- [ ] **T4: Design tokens** — `index.css` với CSS variables: colors, spacing, typography, animations
  → Verify: Variables load đúng trên DevTools

- [ ] **T5: Sidebar navigation** — Component `Sidebar.tsx`: 3 items (Dashboard, Settings, Logs), icon + active state
  → Verify: Click chuyển page, active highlight đúng

- [ ] **T6: App shell** — Layout chính: Sidebar trái (60px collapsed / 200px expanded) + Content area
  → Verify: Layout responsive khi resize window

### Phase 3: Dashboard Page

- [ ] **T7: StatusCard component** — Hiển thị metric card (status, uptime, requests, errors)
  → Verify: Render đúng data với các prop

- [ ] **T8: Dashboard page** — Bố cục dashboard: status overview + start/stop button + metrics grid
  → Verify: Hiển thị đúng layout, nút start/stop toggle

### Phase 4: Settings Page

- [ ] **T9: Settings form** — Form cấu hình với 3 sections:
  - **Cloudflare Tunnel**: tunnel ID, hostname, credentials path
  - **Forwarding**: target host, port, protocol
  - **Security**: webhook secret, max body size, timeout
  → Verify: Form render đúng, validation hoạt động

- [ ] **T10: Config persistence** — Rust `config_store.rs` lưu settings vào JSON file, React hook `useConfig.ts` đọc/ghi qua Tauri IPC
  → Verify: Thay đổi settings → restart forwarder → áp dụng đúng

### Phase 5: Log Viewer Page

- [ ] **T11: LogEntry component** — Hiển thị 1 dòng log: timestamp, level badge (color-coded), message, request-id
  → Verify: Render đúng format, màu sắc theo level

- [ ] **T12: LogViewer page** — Container log: virtual scroll (hiệu năng cho hàng nghìn dòng), auto-scroll, filter theo level, search text
  → Verify: Hiển thị 1000+ logs mượt, filter/search hoạt động

### Phase 6: Tauri Backend (Rust)

- [ ] **T13: ForwarderManager** — Rust module quản lý Node.js child process: start, stop, status, restart
  → Verify: `invoke('start_forwarder')` spawn process, `invoke('stop_forwarder')` kill sạch

- [ ] **T14: Log capture** — Pipe stdout/stderr từ child process → emit event qua Tauri
  → Verify: Frontend nhận được log entries real-time

- [ ] **T15: IPC commands** — Đăng ký tất cả commands: `start_forwarder`, `stop_forwarder`, `get_status`, `get_config`, `save_config`, `get_logs`
  → Verify: Tất cả commands respond đúng

- [ ] **T16: System tray** — Tray icon + menu (Show/Hide, Start/Stop, Quit), minimize to tray on close
  → Verify: Click tray icon → toggle window, menu items hoạt động

### Phase 7: Integration & Polish

- [ ] **T17: Wire it all** — Connect Dashboard realtime status, Settings save + apply, Logs streaming
  → Verify: Full flow hoạt động end-to-end

- [ ] **T18: Animations** — Pulse animation cho status dot, smooth transitions cho page switch, hover effects
  → Verify: UI cảm giác mượt, alive

- [ ] **T19: Error states** — UI cho trường hợp lỗi: Node process crash, invalid config, connection refused
  → Verify: Thông báo lỗi rõ ràng, không crash app

### Phase X: Verification

- [ ] `npm run tauri build` tạo installer thành công
- [ ] App mở, dashboard hiển thị status
- [ ] Start forwarder → status chuyển "Running", logs bắt đầu stream
- [ ] Gửi webhook request → log xuất hiện trên Log Viewer
- [ ] Stop forwarder → status chuyển "Stopped"
- [ ] Settings thay đổi → restart forwarder áp dụng mới
- [ ] Minimize to tray → click tray icon → window hiện lại
- [ ] Close window → app chạy ngầm trong tray
- [ ] UI dark theme, sharp edges, smooth animations
- [ ] App size < 15MB

## Agent Assignments

| Phase | Agent | Skills |
|-------|-------|--------|
| Phase 1-2 | `frontend-specialist` | `frontend-design`, `clean-code` |
| Phase 3-5 | `frontend-specialist` | `frontend-design` |
| Phase 6 | `backend-specialist` | `clean-code` |
| Phase 7 | `frontend-specialist` | `frontend-design` |
| Phase X | `security-auditor` | `vulnerability-scanner` |

## Risk & Mitigation

| Risk | Mitigation |
|------|------------|
| Tauri v2 Windows build issues | Kiểm tra prerequisites trước (Rust, WebView2) |
| Node child process management | Graceful shutdown, restart policy, orphan process cleanup |
| Log performance (nhiều entries) | Virtual scrolling, buffer giới hạn (giữ 10k entries) |
| IPC serialization overhead | Batch log events, throttle UI updates |

## Notes

- Node.js forwarder (`src/`) **KHÔNG thay đổi** — giữ nguyên logic hiện tại
- Tauri Rust backend spawn `libs/node/node.exe` (portable) thay vì gọi `node` từ PATH
- User cuối **KHÔNG cần cài Node.js, Rust, hay bất kỳ dependency nào**
- Settings được lưu tại `%APPDATA%/webhook-forwarder/config.json`
- Log buffer giới hạn 10,000 entries trên RAM để tránh memory leak
- Developer cần: Rust + VS Build Tools (chỉ cho build, không cho runtime)
