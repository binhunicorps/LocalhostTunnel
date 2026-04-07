# GitHub Release + Auto-Update

## Goal

Thêm 2 chức năng:
1. **Push to GitHub + auto-version**: Đẩy code lên GitHub với version tag tự động (semver), user chọn major/minor/patch. Hỗ trợ cả UI button và CLI.
2. **Auto-update**: App kiểm tra version mới → tải về folder mới (ví dụ `WebhookForwarder-v1.1.0`) → copy config cũ → tự khởi động lại từ thư mục mới.

## Repo

- **URL**: `https://github.com/binhunicorns/LocalhostTunel.git`
- **Versioning**: Semantic Versioning (`major.minor.patch`)
- **Detect updates**: GitHub Releases API (`/repos/{owner}/{repo}/releases/latest`)

## Luồng hoạt động

### Push & Release (Developer)

```
User chọn bump type (major/minor/patch)
  → Tăng version trong package.json
  → Git commit + tag (v1.0.1)
  → Git push origin main + push tags
  → Tạo GitHub Release (via API)
  → Đóng gói zip artifact → upload lên Release
```

### Auto-Update (End User)

```
App khởi động / mỗi 30 phút
  → GET GitHub Releases API → so sánh version
  → Nếu có bản mới:
     → UI hiển thị nút "Cập nhật v1.1.0"
     → User bấm → tải zip từ Release assets
     → Giải nén → E:\VibeCode\WebhookForwarder-v1.1.0\
     → Copy config.json từ version cũ
     → Spawn start.bat trong folder mới
     → Tự tắt instance cũ
```

## File Structure (New/Modified)

```
WebhookForwarder/
├── server/
│   ├── updater.js          # [NEW] Check + download + apply updates
│   └── api.js              # [MODIFY] Thêm routes: /api/version, /api/update/*
├── ui/src/
│   ├── hooks/
│   │   └── useUpdater.ts   # [NEW] Hook check/apply update
│   ├── components/
│   │   └── UpdateBanner.tsx # [NEW] Banner thông báo version mới
│   └── pages/
│       └── Dashboard.tsx    # [MODIFY] Hiển thị current version + update banner
├── scripts/
│   ├── release.js          # [NEW] CLI: bump version + git push + create release
│   └── package.js          # [NEW] Đóng gói zip cho release
├── package.json            # [MODIFY] version field, thêm scripts
└── .github/                # Không cần CI — release từ local
```

## Tasks

### Phase 1: Version & Release System

- [ ] **T1: `scripts/release.js`** — CLI script:
  - Nhận arg: `major`, `minor`, hoặc `patch`
  - Bump version trong `package.json`
  - `git add . && git commit && git tag v{x.y.z} && git push origin main --tags`
  - Tạo GitHub Release via REST API (cần `GITHUB_TOKEN`)
  → Verify: `node scripts/release.js patch` → tạo release trên GitHub

- [ ] **T2: `scripts/package.js`** — Đóng gói app thành zip:
  - Bao gồm: `server/`, `src/`, `ui/dist/`, `libs/`, `node_modules/`, `start.bat`, `package.json`
  - Loại trừ: `.git/`, `ui/node_modules/`, `ui/src/`, `docs/`, `test/`
  - Upload zip lên GitHub Release assets
  → Verify: Release có file `webhook-forwarder-v1.0.1.zip` đính kèm

- [ ] **T3: API routes cho release** — Thêm vào `server/api.js`:
  - `GET /api/version` — trả current version + repo info
  - `POST /api/release` — trigger release từ UI (gọi `scripts/release.js`)
  → Verify: API respond đúng

### Phase 2: Auto-Update System

- [ ] **T4: `server/updater.js`** — Core updater module:
  - `checkForUpdate()`: GET GitHub Releases API, so sánh semver
  - `downloadUpdate(url, targetDir)`: Tải zip, giải nén vào folder mới
  - `applyUpdate(newDir)`: Copy config.json, spawn `start.bat`, exit current
  → Verify: Phát hiện version mới, tải thành công

- [ ] **T5: Update API routes** — Thêm vào `server/api.js`:
  - `GET /api/update/check` — kiểm tra version mới
  - `POST /api/update/apply` — tải + áp dụng update
  → Verify: API respond đúng

- [ ] **T6: Schedule check** — Server kiểm tra update mỗi 30 phút
  → Verify: Log hiển thị check interval

### Phase 3: UI Integration

- [ ] **T7: `UpdateBanner.tsx`** — Component hiển thị:
  - Khi có update: banner "Version v1.1.0 available" + nút "Update Now"
  - Khi đang tải: progress indicator
  - Khi xong: "Restarting..."
  → Verify: Banner hiển thị khi có version mới

- [ ] **T8: `useUpdater.ts`** — Hook:
  - `checkUpdate()`, `applyUpdate()`, `updateAvailable`, `updateProgress`
  → Verify: Hook hoạt động đúng

- [ ] **T9: Dashboard update** — Hiển thị current version + UpdateBanner
  → Verify: Dashboard hiển thị version và banner

- [ ] **T10: Release from UI** — Nút "Release" trên Settings hoặc Dashboard:
  - Dialog chọn bump type (major/minor/patch)
  - Confirm → trigger `/api/release`
  → Verify: Bấm Release → code push lên GitHub

### Phase X: Verification

- [ ] CLI: `node scripts/release.js patch` → GitHub Release created
- [ ] UI: Bấm Release → push + tag + release
- [ ] App detect version mới (mock hoặc after release)
- [ ] Bấm "Update Now" → tải → folder mới → config copy → restart
- [ ] App mới chạy từ folder `WebhookForwarder-v{mới}` với config cũ

### Verification Commands

```bash
# Release từ CLI
node scripts/release.js patch

# Check update API
curl http://localhost:3000/api/update/check

# Current version
curl http://localhost:3000/api/version
```

## Config

| Setting | Value | Nơi lưu |
|---------|-------|---------|
| `GITHUB_TOKEN` | Personal Access Token | `.env` hoặc UI Settings |
| `GITHUB_REPO` | `binhunicorps/LocalhostTunel` | Hardcode / config |
| Update check interval | 30 phút | `server/updater.js` |
| Install base dir | Parent dir (e.g. `E:\VibeCode\`) | Detect từ cwd |

## Agent Assignments

| Phase | Agent | Skills |
|-------|-------|--------|
| Phase 1 | `backend-specialist` | `nodejs-best-practices`, `clean-code` |
| Phase 2 | `backend-specialist` | `nodejs-best-practices` |
| Phase 3 | `frontend-specialist` | `frontend-design` |

## Notes

- GitHub Personal Access Token cần quyền `repo` (read/write)
- Release zip bao gồm tất cả runtime (node.exe, node_modules) để user tải là chạy
- Folder naming: `WebhookForwarder-v{version}` cùng cấp thư mục cha
- Config copy: chỉ copy `config.json` từ `%APPDATA%` — dùng chung nên không cần copy
- Thực tế config lưu ở `%APPDATA%/webhook-forwarder/config.json` → **tất cả versions dùng chung** → không cần copy thủ công!
