<p align="center">
  <a href="https://sun-live.vercel.app">
    <img width="1200" height="800" src ="https://github.com/JHANGMING/SunLive/blob/main/public/images/cover.png">
  </a>
</p>
 
<h1 align="center" style="font-weight: 700"><img width="30" src ="https://github.com/JHANGMING/SunLive/blob/main/public/images/logo.svg"> 搶鮮購  | SunLive </h1>
<div align="center" style="margin-bottom:24px">

  <a href="https://drive.google.com/file/d/1PA-nUPBaDxbWcsjRX8_U9CmcTLuKvKSq/view?usp=drive_link">
    簡報介紹
  </a>
  <span>｜</span>
  <a href="https://sun-live.vercel.app">
  專案網址 
  </a>
  <span>｜</span>
  <a href="https://github.com/JHANGMING/SunLive">
   前端 Github Repo 
  </a>
  <span>｜</span>
  <a href="https://github.com/David07994415/RocketFarmerProject">
    後端 Github Repo 
  </a>
  <span>｜</span>
  <a href="https://liberating-dosa-c89.notion.site/a3098fd6c4a54711b28e87a9bac99dcb?v=4b9aa8addad2438389aaa9aa3dd61b1b&p=e20c4039a92d4e3a970ced34e004e791&pm=s&pvs=31">
  API swagger
  </a>
  <span>｜</span>
  <a href="https://liberating-dosa-c89.notion.site/a3098fd6c4a54711b28e87a9bac99dcb?v=4b9aa8addad2438389aaa9aa3dd61b1b&p=e20c4039a92d4e3a970ced34e004e791&pm=s&pvs=31">
  API Notion
  </a>
<p>
</p>
<p>
搶鮮購是探索有機農產品的理想平台<br>
致力於協助小農推廣並販售有機農作物，
透過即時直播和搶購功能，
讓用戶更深入了解每個美味的背後故事。
</p>

</div>
<hr/>


## 功能介紹

> 註冊之後可以依據所選擇的身份分為「一般會員」及「小農」角色

### ► 一般會員角色 (Customer)

- 個人帳號資訊設定與編輯
  
- 將特定產品加入購物車

- 與小農(商家)進行1對1即時聊天

- 加入直播間並進行多對多即時聊天

- 進行購物車結帳

- 查詢所有訂單

  

### ► 小農角色 (Farmer)

- 個人帳號資訊設定與編輯

- 建立與修改農產品資訊

- 建立與修改直播串流資訊

- 察看與管理訂單狀態(包含出貨狀態調整)

- 與一般會員角色(客戶)進行1對1即時聊天

- 主持直播間並進行多對多即時聊天


---
## Git 規範
### Commit
| 類型 | 格式 | 說明 |
| :---: | :---: | :--- |
| 新增功能 | add | 新增功能 |
| 更新功能 | update | 更新現有功能 |
| 修補錯誤 | fix | 修正現有功能的錯誤 |
| 重構程式 | refactor | 重構。針對已上線的功能程式碼調整與優化，且不改變記有邏輯。 |
| 樣式相關 | style | 程式碼格式調整 (不影響程式碼運行的變動) |
| 維護資料 | chore | 更新專案建置設定、更新版本號等。 |
### Branch
| 類型 | 格式 |
| :---: | :---: |
| 新增功能 | feature-[branch name] |
| 更新功能 | update-[branch name] |
| 重構功能 | refactor-[branch name] |
| 修正功能 | fix-[branch name] |
| 緊急修復 | hotfix-[branch name] |
<hr/>



## 技術規格 
<h2 align="center">後端技術</h2>
 <p>
 <img alt="Visual_Studio" src="https://img.shields.io/badge/Visual_Studio-5C2D91?style=for-the-badge&logo=visual%20studio&logoColor=white" />
  <img alt=".NET" src="https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
  <img alt="C#" src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white" />
  <img alt="SQL" src="https://img.shields.io/badge/Microsoft%20SQL%20Server-CC2927?style=for-the-badge&logo=microsoft%20sql%20server&logoColor=white" />
  <img alt="Azure" src="https://img.shields.io/badge/microsoft%20azure-0089D6?style=for-the-badge&logo=microsoft-azure&logoColor=white" />
  <img alt="SignalR" src="https://img.shields.io/badge/SignalR-007ACC?style=for-the-badge&logoColor=white" />
  <img alt="GIT" src="https://img.shields.io/badge/GIT-E44C30?style=for-the-badge&logo=git&logoColor=white" />
  <img alt="GItHUB" src="https://img.shields.io/badge/GitHub-100000?style=for-the-badge&logo=github&logoColor=white" />
  <img alt="POSTMAN" src="https://img.shields.io/badge/Postman-FF6C37?style=for-the-badge&logo=Postman&logoColor=white" />
 
  ### 後端框架與結構：

* 開發環境：Microsoft Visual Studio
    * 架構：使用的.net Freamwork 4.7.2 版本。
    * 專案：ASP.NET Web API 2

* 專案結構：
```
Farmer_Project
│  chathub.cs
│  FarmerPro.csproj
│  FarmerPro.csproj.user
│  favicon.ico
│  Global.asax
│  Global.asax.cs
│  packages.config
│  Startup.cs
│  Web.config
│  Web.config.template
│  Web.Debug.config
│  Web.Release.config
│  
├─App_Start
│      BundleConfig.cs
│      FilterConfig.cs
│      RouteConfig.cs
│      WebApiConfig.cs
│      
├─Areas
│    ...
│                  
├─bin
│    ...
│          
├─Content
│    ...
│      
├─Controllers
│      CartController.cs
│      ChatController.cs
│      DefaultController.cs
│      FarmerController.cs
│      FarmerInforController.cs
│      FarmerOrderController.cs
│      HomeController.cs
│      LiveController.cs
│      LiveSettingController.cs
│      LoginController.cs
│      LoginForgetController.cs
│      OrderController.cs
│      OrderTestController.cs
│      ProductController.cs
│      UserController.cs
│      UserInfoController.cs
│      UserOrderController.cs
│      ValuesController.cs
│      
├─Migrations
│      ...
│      Configuration.cs
│      
├─Models
│  │  Album.cs
│  │  Cart.cs
│  │  CartItem.cs
│  │  ChatRoom.cs
│  │  Credential.cs
│  │  EnumList.cs
│  │  FarmerProDB.cs
│  │  GlobalVariable.cs
│  │  GoogleOauth.cs
│  │  LiveAlbum.cs
│  │  LiveProduct.cs
│  │  LiveSetting.cs
│  │  Order.cs
│  │  OrderDetail.cs
│  │  OrderFarmer.cs
│  │  Photo.cs
│  │  Product.cs
│  │  Record.cs
│  │  Spec.cs
│  │  User.cs
│  │  YoutubeLive.cs
│  │  
│  └─ViewModel
│          CreateNewLiveSetting.cs
│          CreateNewOrder.cs
│          CreateProduct.cs
│          Register.cs
│          
├─obj
│    ...
│
├─Properties
│    ...
│          
├─Scripts
│    ...
│      
├─Securities
│      CryptoUtil.cs
│      JwtAuthFilter.cs
│      JwtAuthUtil.cs
│      
├─upload
│    ...
│                  
└─Views
    │  Web.config
    │  _ViewStart.cshtml
    │  
    ├─Home
    │      Index.cshtml
    │      socketviewpage.cshtml
    │      
    └─Shared
            Error.cshtml
            _Layout.cshtml
```
* 資料庫：Microsoft SQL Server
    * 關聯性資料庫管理系統，用於記錄數據、查詢資料等。

* 技術：SignalR
    * 此為Microsoft所開發的套件，針對即時通訊應用提供包裹性解決方案。

* 雲端伺服器：Azure
    * Microsoft提供一個雲端平台，選擇虛擬機台進行後端伺服器資源部署。

</p>

<h2 align="center">前端技術</h2>
<p>
  

### 技術說明：

* 開發環境：Next.js
    * 使用Next.js的SSR，可以在伺服器上完整渲染 HTML 頁面，除了有更好的 SEO 和更快的頁面加載速度，同時預先渲染的頁面，提升使用者體驗。

* 使用框架：React
    * 使用React進行前端開發，利用React的生態系來快速搭建網站並搭配生命週期特性讓網站狀態管理更加高效，進而提升用戶體驗。

* 語言：TypeScript
    * 開發過程中採用TypeScript語言，其強類型特性有效預防了許多常見錯誤，並且在元件中減少了因型別錯誤導致的衝突。

* CSS：Tailwind
    * 使用Tailwind來進行CSS的開發，通過提供大量預定義的類別來幫助開發者快速構建和設計界面，減少冗餘代碼。

* 雲端伺服器：Vercel
    * 選擇Vercel來進行部署，提供高度的穩定性和優化的整合體驗，實現快速的自動化部署流程，簡化開發到上線的過程。
</p>


<h2 align="center">設計工具</h2>
 <p>
 
  ### 工具說明：

* 設計稿製作：Figma
    * 用於製作線稿、精稿及 prototype。
    * 方便團隊之間協作，理解產品操作流程

* 繪圖工具：Procreate
   - 用於繪製插圖及 loading 動畫
   - 內建筆刷庫非常豐富，且能針對每種筆刷自由調整參數
   - 支援匯出各式檔案
</p>



