using Microsoft.EntityFrameworkCore;

namespace API.Database
{
    public class DBPopsicle
    {
        public int Id { get; set; }
        public string SKU { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public int Quantity { get; set; } = 0;
        public string Ingredients { get; set; }

        public DateTime CreateDate { get; set; }
        public DateTime LastUpdate { get; set; }
    }


    public class PopsicleContext : DbContext
    {
        public virtual DbSet<DBPopsicle> Popsicle { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            modelBuilder.Entity<DBPopsicle>(entity =>
            {
                entity.Property(e => e.Color).IsRequired();
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.SKU).IsRequired();
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Description).IsRequired();
            });
            modelBuilder.Entity<DBPopsicle>().HasKey(i => i.Id);

            base.OnModelCreating(modelBuilder);
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseInMemoryDatabase("KorTerra");
        }
    }
}
