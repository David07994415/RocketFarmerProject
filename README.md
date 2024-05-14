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


<h2 align="center" >功能介紹</h2>

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


<h2 align="center">團隊協作</h2>
<h3>► 後端 (Back-End)</h3>
 <p>

* 後端開發環境：
    * 框架：.NET Framework 4.7.2
    * 專案：ASP.NET Web API 2
      
* 後端開發技術：
  <div align="center">
    <img alt="Visual_Studio" src="https://img.shields.io/badge/Visual_Studio-5C2D91?style=for-the-badge&logo=visual%20studio&logoColor=white" />
    <img alt=".NET" src="https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
    <img alt="C#" src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white" />
    <img alt="SQL" src="https://img.shields.io/badge/Microsoft%20SQL%20Server-CC2927?style=for-the-badge&logo=microsoft%20sql%20server&logoColor=white" />
    <img alt="Entity_Framework" src="https://img.shields.io/badge/Entity_Framework-yellow?style=for-the-badge">
    <img alt="LINQ" src="https://img.shields.io/badge/LINQ-8A2BE2?style=for-the-badge">
  </div>
  <div align="center">
    <img alt="Azure" src="https://img.shields.io/badge/microsoft%20azure-0089D6?style=for-the-badge&logo=microsoft-azure&logoColor=white" />
    <img alt="SignalR" src="https://img.shields.io/badge/SignalR-007ACC?style=for-the-badge&logoColor=white" />
    <img alt="GIT" src="https://img.shields.io/badge/GIT-E44C30?style=for-the-badge&logo=git&logoColor=white" />
    <img alt="GitHUB" src="https://img.shields.io/badge/GitHub-100000?style=for-the-badge&logo=github&logoColor=white" />
    <img alt="POSTMAN" src="https://img.shields.io/badge/Postman-FF6C37?style=for-the-badge&logo=Postman&logoColor=white" />
    <img alt="OAuth2.0" src="https://img.shields.io/badge/OAuth_2.0-black?style=for-the-badge">
    <img alt="Passkey" src="https://img.shields.io/badge/Passkey-gray?style=for-the-badge&logo=webauthn">
    <img alt="Youtube" src="https://img.shields.io/badge/Youtube-FF0000?style=for-the-badge&logo=youtubestudio">
  </div>
  
  - 資料庫存取：Microsoft SQL Server 搭配 Entity Framework Code First 以及 LINQ 進行資料庫存取
  
  - 雲端服務：Azure 上建立虛擬機(VM)，並於 VM 上建立 SQL Server 與 IIS 環境，部屬 Web API Application

  - 即時通訊：透過 SignalR 建立後端 Hub，完成與前端(Client-Side)的 1-1 與 1-多 的即時通訊功能
  
  - 線上串流：介接 Youtube Api，達成個人直播串流服務
 
  - 登入服務：建立包含無密碼信件登入、OAuth2.0 Google 第三方登入以及 Passkey 非對稱加密登入

* 後端協作 Git 規範：
  * Commit


    | 類型 | 格式 | 說明 |
    | :---: | :---: | :---: |
    | 新增功能 | Feat | 新增功能 |
    | 更新功能 | Update | 更新現有功能、程式碼格式調整 |


  * Branch


    | 類型 | 格式 |
    | :---: | :---: |
    | 開發功能 | dev |
    | 新增功能 | Feature-[branch name] |
    | 更新功能 | Update-[branch name] |


  * Git Flow

    <img src="https://raw.githubusercontent.com/David07994415/RocketFarmerProject/main/gitflowchar.png">

* 後端專案結構：
```
SunLive_Backend_Project
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

</p>
<hr/>
<h3>► 前端 (Front-End)</h3>
<p>
  
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

<h3>► 設計端 (Design)</h3>
 <p>

* 設計稿製作：Figma
    * 用於製作線稿、精稿及 prototype。
    * 方便團隊之間協作，理解產品操作流程

* 繪圖工具：Procreate
   - 用於繪製插圖及 loading 動畫
   - 內建筆刷庫非常豐富，且能針對每種筆刷自由調整參數
   - 支援匯出各式檔案
</p>



