# CSV 資料庫匯入工具

[![CI](https://github.com/ObsDIan/CsvImportTool/actions/workflows/ci.yml/badge.svg)](https://github.com/ObsDIan/CsvImportTool/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/ObsDIan/CsvImportTool?label=Windows%20Release)](https://github.com/ObsDIan/CsvImportTool/releases/latest)

這是一個 C# WPF 桌面工具，用來將 CSV 匯入 SQL Server。

## 功能

- 讀取 CSV
- 預覽 CSV 內容
- 連線 SQL Server
- 讀取資料表清單
- 讀取資料表欄位資訊
- 手動設定 CSV 欄位與 DB 欄位對應
- 依欄位名稱自動對應
- 驗證必填、型別、文字長度
- Insert 匯入資料
- 使用 SQL Transaction，任一筆失敗會整批 Rollback
- 匯出驗證錯誤 CSV

## 下載 Windows 版本

從 [Latest Release](https://github.com/ObsDIan/CsvImportTool/releases/latest) 下載 `CsvImportTool-win-x64.zip`，解壓縮後執行 `CsvImportTool.exe`。

- 支援 Windows 10 / 11 x64。
- Release 已包含 .NET 8 Runtime，不需另外安裝 Visual Studio 或 .NET。
- 可使用同一頁提供的 `SHA256SUMS.txt` 驗證下載檔案。
- 目前執行檔尚未進行商業程式碼簽章，Windows SmartScreen 可能顯示未知發行者警告。

> 建議先連接測試資料庫確認欄位映射，再操作正式資料。

## 開發環境

建議使用：

- Windows 10 / 11
- Visual Studio 2022
- .NET 8 SDK
- SQL Server / SQL Server Express

## 使用方式

1. 用 Visual Studio 開啟 `CsvImportTool.csproj`
2. 還原 NuGet 套件
3. 執行專案
4. 輸入 SQL Server 連線字串
5. 點選「讀取資料表」
6. 選擇資料表後點選「讀取欄位」
7. 選擇 CSV 並點選「讀取 CSV」
8. 到「欄位對應」確認 CSV 欄位與 DB 欄位對應
9. 點選「驗證資料」
10. 無錯誤後點選「匯入資料」

## 連線字串範例

Windows 驗證：

```text
Server=localhost\SQLEXPRESS;Database=YourDatabase;Trusted_Connection=True;TrustServerCertificate=True;
```

SQL Server 帳密驗證：

```text
Server=localhost\SQLEXPRESS;Database=YourDatabase;User Id=sa;Password=YourPassword;TrustServerCertificate=True;
```

## 測試資料

`Samples` 資料夾內有：

- `SampleTable.sql`：測試資料表
- `Sample.csv`：測試 CSV

你可以先在 SQL Server 建立 `dbo.ImportSample`，再用 `Sample.csv` 測試匯入。

## 欄位對應說明

在「欄位對應」頁籤中：

| 欄位 | 說明 |
|---|---|
| 匯入 | 是否將此 DB 欄位納入 Insert |
| DB 欄位 | 目標資料庫欄位 |
| 型別 | SQL Server 欄位型別 |
| 長度 | 文字欄位長度 |
| 必填 | 不允許 NULL、非 Identity、非 Computed、無 Default 的欄位 |
| CSV 欄位 | 對應來源 CSV 欄位 |
| 固定值 | 若填入固定值，會優先使用固定值，不使用 CSV 欄位 |

## 開發與測試

```powershell
dotnet restore CsvImportTool.slnx
dotnet build CsvImportTool.slnx --configuration Release --no-restore
dotnet test CsvImportTool.Tests/CsvImportTool.Tests.csproj --configuration Release --no-build
```

GitHub Actions 會在 Pull Request 與 `master` 更新時自動執行 build 與單元測試。

## 安全與資料正確性

- SQL table／column identifiers 會進行安全 quoting，資料值皆使用 SQL parameters。
- decimal／numeric 欄位會在匯入前檢查 precision 與 scale。
- 錯誤 CSV 會處理 `=`, `+`, `-`, `@` 開頭的內容，避免試算表公式注入。
- 連線字串可能包含密碼，不會儲存在專案檔案中；操作完成後請關閉工具並避免截圖外洩。

## 注意事項

- 第一版只支援 Insert，不支援 Update / Upsert。
- 第一版不支援二進位欄位，例如 varbinary、image。
- Identity 與 Computed 欄位預設不匯入。
- 建議先用測試資料庫測試，確認欄位對應正確後再匯入正式資料庫。
- 匯入採整批交易，任何一筆失敗會全部 Rollback。

## 下一版可以加的功能

- 儲存欄位對應設定
- Update / Upsert
- 匯入成功 / 失敗 Log
- 批次大小設定
- Excel 匯入
- 匯入前後執行 SQL
- 更多自訂轉換規則
