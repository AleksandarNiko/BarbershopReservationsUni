using BarbershopReservationsUni.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BarbershopReservationsUni.Data
{
    public class BarbershopReservationsUniDbContext : DbContext 
    {
        public BarbershopReservationsUniDbContext(DbContextOptions<BarbershopReservationsUniDbContext> options)
            : base(options)
        { 
        }
            public DbSet<Client> Clients => Set<Client>();
        public DbSet<Barber> Barbers => Set<Barber>();
        public DbSet<Service> Services => Set<Service>();
        public DbSet<Appointment> Appointments => Set<Appointment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Client>()
                .HasIndex(c => c.PhoneNumber)
                .IsUnique();

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Client)
                .WithMany(c => c.Appointments)
                .HasForeignKey(a => a.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Barber)
                .WithMany(b => b.Appointments)
                .HasForeignKey(a => a.BarberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Service)
                .WithMany(s => s.Appointments)
                .HasForeignKey(a => a.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Service>().HasData(
                new Service { Id = 1, Name = "Мъжко подстригване", Description = "Класическо подстригване с машинка и ножица", Price = 20.00m, DurationMinutes = 30 },
                new Service { Id = 2, Name = "Оформяне на брада", Description = "Подстригване и стилизиране на брада", Price = 15.00m, DurationMinutes = 20 },
                new Service { Id = 3, Name = "Подстригване + брада", Description = "Комбинирана услуга", Price = 30.00m, DurationMinutes = 45 },
                new Service { Id = 4, Name = "Детско подстригване", Description = "Подстригване за деца до 12 г.", Price = 15.00m, DurationMinutes = 25 }
            );

            modelBuilder.Entity<Barber>().HasData(
                new Barber { Id = 1, FullName = "Иван Иванов", Specialization = "Класически стилове", PhoneNumber = "0888111222", IsActive = true },
                new Barber { Id = 2, FullName = "Георги Петров", Specialization = "Брада и оформяне", PhoneNumber = "0888333444", IsActive = true }
            );
    }
    }
}
