# Claude.ai 内部API調査結果

## 概要
Claude.ai（ウェブ版）の使用制限情報を取得するための内部API。
既存プロジェクトのリバースエンジニアリングにより特定。

**参照元:**
- [Claude-Usage-Extension](https://github.com/lugia19/Claude-Usage-Extension) (Chrome拡張)
- [Claude-Usage-Tracker](https://github.com/hamed-elfayome/Claude-Usage-Tracker) (macOS Swift)

---

## 認証

### セッションキー
- **Cookie名:** `sessionKey`
- **取得方法:** ブラウザのDevToolsからコピー、またはCookieストアから取得
- **有効期限:** 不明（長期間有効の模様）

```
Cookie: sessionKey=sk-ant-xxxxx...
```

---

## エンドポイント

### 1. 使用量取得 ⭐ メイン
```http
GET https://claude.ai/api/organizations/{orgId}/usage
Cookie: sessionKey={sessionKey}
Accept: application/json
```

**レスポンス例:**
```json
{
  "five_hour": {
    "utilization": 45,
    "resets_at": "2026-02-02T15:00:00.000Z"
  }
}
```

| フィールド | 説明 |
|-----------|------|
| `utilization` | 5時間ウィンドウでの使用率 (0-100%) |
| `resets_at` | リセット時刻 (ISO 8601) |

### 2. サブスクリプション情報
```http
GET https://claude.ai/api/bootstrap/{orgId}/statsig
Cookie: sessionKey={sessionKey}
```

**レスポンス (抜粋):**
```json
{
  "user": {
    "custom": {
      "orgType": "claude_pro",  // free, claude_pro, claude_team, claude_max
      "isRaven": false
    }
  }
}
```

### 3. 組織情報
```http
GET https://claude.ai/api/organizations/{orgId}
Cookie: sessionKey={sessionKey}
```

**レスポンス (抜粋):**
```json
{
  "uuid": "org_xxxxx",
  "name": "Personal",
  "rate_limit_tier": "default_claude_pro"
}
```

### 4. 組織一覧（orgId取得用）
```http
GET https://claude.ai/api/organizations
Cookie: sessionKey={sessionKey}
```

### 5. プロフィール
```http
GET https://claude.ai/api/account_profile
Cookie: sessionKey={sessionKey}
```

---

## Windows版実装方針

### 技術スタック
- **言語:** C# (.NET 8)
- **UI:** WPF または WinUI 3
- **タスクバー:** `System.Windows.Forms.NotifyIcon` または Hardcodet.NotifyIcon.Wpf

### 必要な機能
1. **セッションキー入力** - 手動入力 or ブラウザCookie読み取り
2. **組織選択** - `/api/organizations` から取得
3. **使用量ポーリング** - 30秒〜1分間隔
4. **タスクバー表示** - ゲージ + パーセント
5. **リセット時刻通知** - トースト通知

### Cookie取得オプション
1. **手動入力** - DevToolsからコピペ
2. **Chromium Cookie読み取り** - `%LocalAppData%\Google\Chrome\User Data\Default\Network\Cookies` (SQLite、暗号化済み)
3. **WebView2埋め込み** - ログイン画面を表示してCookie取得

### セキュリティ考慮
- セッションキーは Windows Credential Manager に保存
- メモリ上での平文保持を最小限に

---

## 次のステップ

1. [x] 内部API仕様の特定
2. [ ] C# プロジェクト作成
3. [ ] API クライアント実装
4. [ ] タスクバーUI実装
5. [ ] Cookie取得機能
6. [ ] 自動更新機能

---

*調査完了: 2026-02-02*
