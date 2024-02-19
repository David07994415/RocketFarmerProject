namespace FarmerPro.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class revisedb : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Album",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ProductId = c.Int(nullable: false),
                        UserId = c.Int(nullable: false),
                        CreatTime = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Photo",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        URL = c.String(nullable: false),
                        AlbumId = c.Int(nullable: false),
                        CreatTime = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Album", t => t.AlbumId, cascadeDelete: true)
                .Index(t => t.AlbumId);
            
            CreateTable(
                "dbo.CartItem",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Qty = c.Int(nullable: false),
                        SubTotal = c.Double(nullable: false),
                        CreateTime = c.DateTime(nullable: false),
                        CartId = c.Int(nullable: false),
                        SpecId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Cart", t => t.CartId, cascadeDelete: true)
                .ForeignKey("dbo.Spec", t => t.SpecId, cascadeDelete: true)
                .Index(t => t.CartId)
                .Index(t => t.SpecId);
            
            CreateTable(
                "dbo.Cart",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        CreateTime = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Spec",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Price = c.Int(nullable: false),
                        Stock = c.Int(nullable: false),
                        PromotePrice = c.Int(),
                        LivePrice = c.Int(),
                        Size = c.Boolean(nullable: false),
                        Weight = c.Double(nullable: false),
                        CreateTime = c.DateTime(nullable: false),
                        ProductId = c.Int(nullable: false),
                        Sales = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Product", t => t.ProductId, cascadeDelete: true)
                .Index(t => t.ProductId);
            
            CreateTable(
                "dbo.OrderDetail",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Qty = c.Int(nullable: false),
                        SubTotal = c.Double(nullable: false),
                        CreatTime = c.DateTime(nullable: false),
                        SpecId = c.Int(nullable: false),
                        OrderId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Order", t => t.OrderId, cascadeDelete: true)
                .ForeignKey("dbo.Spec", t => t.SpecId, cascadeDelete: true)
                .Index(t => t.SpecId)
                .Index(t => t.OrderId);
            
            CreateTable(
                "dbo.Order",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Receiver = c.String(nullable: false, maxLength: 100),
                        Phone = c.String(nullable: false, maxLength: 100),
                        City = c.Int(nullable: false),
                        District = c.String(nullable: false),
                        ZipCode = c.Int(nullable: false),
                        Address = c.String(nullable: false, maxLength: 300),
                        DeliveryFee = c.Double(nullable: false),
                        OrderSum = c.Double(nullable: false),
                        Shipment = c.Boolean(nullable: false),
                        Guid = c.Guid(nullable: false),
                        PaymentTime = c.DateTime(),
                        IsPay = c.Boolean(),
                        MerchantID = c.String(maxLength: 100),
                        TradeInfo = c.String(),
                        TradeSha = c.String(),
                        Version = c.String(maxLength: 20),
                        CreatTime = c.DateTime(nullable: false),
                        UserId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Product",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ProductTitle = c.String(nullable: false, maxLength: 500),
                        Category = c.Int(nullable: false),
                        Period = c.Int(nullable: false),
                        Origin = c.Int(nullable: false),
                        Storage = c.Int(nullable: false),
                        Description = c.String(maxLength: 500),
                        Introduction = c.String(),
                        ProductState = c.Boolean(nullable: false),
                        UpdateStateTime = c.DateTime(),
                        CreatTime = c.DateTime(nullable: false),
                        UserId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Category = c.Int(nullable: false),
                        Account = c.String(nullable: false, maxLength: 500),
                        EmailGUID = c.Guid(),
                        Password = c.String(nullable: false, maxLength: 500),
                        Salt = c.String(nullable: false, maxLength: 500),
                        Token = c.String(maxLength: 500),
                        NickName = c.String(maxLength: 500),
                        Photo = c.String(),
                        Birthday = c.DateTime(),
                        Sex = c.Boolean(),
                        Phone = c.String(maxLength: 100),
                        Vision = c.String(),
                        Description = c.String(),
                        CreatTime = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ChatRoom",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserIdTalker = c.Int(nullable: false),
                        CreatTime = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.UserIdTalker, cascadeDelete: true)
                .Index(t => t.UserIdTalker);
            
            CreateTable(
                "dbo.Record",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserIdSender = c.Int(nullable: false),
                        Message = c.String(nullable: false, maxLength: 500),
                        IsRead = c.Boolean(nullable: false),
                        CreatTime = c.DateTime(nullable: false),
                        ChatRoomId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.ChatRoom", t => t.ChatRoomId, cascadeDelete: true)
                .Index(t => t.ChatRoomId);
            
            CreateTable(
                "dbo.LiveProduct",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        IsTop = c.Boolean(nullable: false),
                        CreatTime = c.DateTime(nullable: false),
                        LiveSettingId = c.Int(nullable: false),
                        SpecId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.LiveSetting", t => t.LiveSettingId, cascadeDelete: true)
                .ForeignKey("dbo.Spec", t => t.SpecId, cascadeDelete: true)
                .Index(t => t.LiveSettingId)
                .Index(t => t.SpecId);
            
            CreateTable(
                "dbo.LiveSetting",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        LiveName = c.String(nullable: false, maxLength: 500),
                        LiveDate = c.DateTime(nullable: false),
                        StartTime = c.Time(nullable: false, precision: 7),
                        EndTime = c.Time(nullable: false, precision: 7),
                        LivePic = c.String(),
                        YTURL = c.String(nullable: false),
                        ShareURL = c.String(nullable: false),
                        CreatTime = c.DateTime(nullable: false),
                        UserId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.LiveProduct", "SpecId", "dbo.Spec");
            DropForeignKey("dbo.LiveProduct", "LiveSettingId", "dbo.LiveSetting");
            DropForeignKey("dbo.Product", "UserId", "dbo.Users");
            DropForeignKey("dbo.ChatRoom", "UserIdTalker", "dbo.Users");
            DropForeignKey("dbo.Record", "ChatRoomId", "dbo.ChatRoom");
            DropForeignKey("dbo.Spec", "ProductId", "dbo.Product");
            DropForeignKey("dbo.OrderDetail", "SpecId", "dbo.Spec");
            DropForeignKey("dbo.OrderDetail", "OrderId", "dbo.Order");
            DropForeignKey("dbo.CartItem", "SpecId", "dbo.Spec");
            DropForeignKey("dbo.CartItem", "CartId", "dbo.Cart");
            DropForeignKey("dbo.Photo", "AlbumId", "dbo.Album");
            DropIndex("dbo.LiveProduct", new[] { "SpecId" });
            DropIndex("dbo.LiveProduct", new[] { "LiveSettingId" });
            DropIndex("dbo.Record", new[] { "ChatRoomId" });
            DropIndex("dbo.ChatRoom", new[] { "UserIdTalker" });
            DropIndex("dbo.Product", new[] { "UserId" });
            DropIndex("dbo.OrderDetail", new[] { "OrderId" });
            DropIndex("dbo.OrderDetail", new[] { "SpecId" });
            DropIndex("dbo.Spec", new[] { "ProductId" });
            DropIndex("dbo.CartItem", new[] { "SpecId" });
            DropIndex("dbo.CartItem", new[] { "CartId" });
            DropIndex("dbo.Photo", new[] { "AlbumId" });
            DropTable("dbo.LiveSetting");
            DropTable("dbo.LiveProduct");
            DropTable("dbo.Record");
            DropTable("dbo.ChatRoom");
            DropTable("dbo.Users");
            DropTable("dbo.Product");
            DropTable("dbo.Order");
            DropTable("dbo.OrderDetail");
            DropTable("dbo.Spec");
            DropTable("dbo.Cart");
            DropTable("dbo.CartItem");
            DropTable("dbo.Photo");
            DropTable("dbo.Album");
        }
    }
}
