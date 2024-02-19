using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace FarmerPro.Models
{
    public partial class FarmerProDB : DbContext
    {
        public FarmerProDB()
            : base("name=FarmerProDB")
        {
        }

        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<Spec> Specs { get; set; }
        public virtual DbSet<LiveSetting> LiveSettings { get; set; }
        public virtual DbSet<LiveProduct> LiveProducts { get; set; }

        public virtual DbSet<Album> Albums { get; set; }
        public virtual DbSet<Photo> Photos { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderDetail> OrderDetails { get; set; }
        public virtual DbSet<Cart> Carts { get; set; }
        public virtual DbSet<CartItem> CartItems { get; set; }
        public virtual DbSet<ChatRoom> ChatRooms { get; set; }
        public virtual DbSet<Record> Records { get; set; }

        public virtual DbSet<LiveAlbum> LiveAlbum { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }
    }
}